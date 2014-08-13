using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage.Security;
using System.Diagnostics;
using Lucene.Net.Search;
using Lucene.Net.Documents;
using SenseNet.Search.Indexing;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;

namespace SenseNet.Search
{
    public interface IQueryExecutor
    {
        int TotalCount { get; }
        IEnumerable<LucObject> Execute(LucQuery lucQuery, bool allVersions);
    }

    internal abstract class QueryExecutor : IQueryExecutor
    {
        private enum DocumentOpenLevel { Denied, See, Preview, Open, OpenMinor }

        protected LucQuery LucQuery { get; private set; }
        protected IUser User { get; private set; }
        protected bool IsCurrentUser { get; private set; }
        protected bool AllVersions { get; private set; }
        internal long FullExecutingTime { get; private set; }
        public int TotalCount { get; private set; }

        private Stopwatch timer;
        protected void BeginFullExecutingTime()
        {
            timer = Stopwatch.StartNew();
        }
        protected void FinishFullExecutingTime()
        {
            if (timer == null)
                return;
            FullExecutingTime = timer.ElapsedTicks;
            timer.Stop();
        }

        public IEnumerable<LucObject> Execute(LucQuery lucQuery, bool allVersions)
        {
            this.LucQuery = lucQuery;
            this.AllVersions = allVersions;
            using (var traceOperation = Logger.TraceOperation("Query execution", "Query: " + this.LucQuery.QueryText))
            {
                this.User = this.LucQuery.User;
                var currentUser = AccessProvider.Current.GetCurrentUser();
                if (this.User == null)
                    this.User = currentUser;
                var isCurrentUser = this.User.Id == currentUser.Id;

                Query currentQuery;

                if (LucQuery.IsAutofilterEnabled(this.LucQuery.EnableAutofilters) || LucQuery.IsLifespanFilterEnabled(this.LucQuery.EnableLifespanFilter))
                {
                    var fullQuery = new BooleanQuery();
                    fullQuery.Add(new BooleanClause(this.LucQuery.Query, BooleanClause.Occur.MUST));

                    if (LucQuery.IsAutofilterEnabled(this.LucQuery.EnableAutofilters))
                        fullQuery.Add(new BooleanClause(this.LucQuery.AutoFilterQuery, BooleanClause.Occur.MUST));
                    if (LucQuery.IsLifespanFilterEnabled(this.LucQuery.EnableLifespanFilter) && this.LucQuery.LifespanQuery != null)
                        fullQuery.Add(new BooleanClause(this.LucQuery.LifespanQuery, BooleanClause.Occur.MUST));

                    currentQuery = fullQuery;
                }
                else
                {
                    currentQuery = this.LucQuery.Query;
                }

                SearchResult r = null;
                using (var readerFrame = LuceneManager.GetIndexReaderFrame())
                {
                    BeginFullExecutingTime();

                    int top = this.LucQuery.Top != 0 ? this.LucQuery.Top : this.LucQuery.PageSize;
                    if (top == 0)
                        top = int.MaxValue;

                    var idxReader = readerFrame.IndexReader;
                    var searcher = new IndexSearcher(idxReader);

                    var p = new SearchParams
                    {
                        query = currentQuery,
                        allVersions = allVersions,
                        searcher = searcher,
                        numDocs = idxReader.NumDocs(),
                        timer = timer,
                        top = top,
                        executor = this
                    };

                    try
                    {
                        r = DoExecute(p);
                    }
                    finally
                    {
                        if (p.searcher != null)
                        {
                            p.searcher.Close();
                            p.searcher = null;
                        }
                        FinishFullExecutingTime();
                    }
                }
                TotalCount = r.totalCount;

                var searchtimer = r.searchTimer;
                var trace = lucQuery.TraceInfo;
                trace.KernelTime = searchtimer.KernelTime;
                trace.CollectingTime = searchtimer.CollectingTime;
                trace.PagingTime = searchtimer.PagingTime;
                trace.FullExecutingTime = FullExecutingTime;
                trace.Searches = r.searches;

                traceOperation.AdditionalObject = trace;
                traceOperation.IsSuccessful = true;
                return r.result;
            }
        }
        protected abstract SearchResult DoExecute(SearchParams p);
        protected SearchResult Search(SearchParams p)
        {
            var r = new SearchResult(p.timer);
            var t = r.searchTimer;

            t.BeginKernelTime();
            var collector = CreateCollector(p.collectorSize, p);
            p.searcher.Search(p.query, collector);
            t.FinishKernelTime();

            t.BeginCollectingTime();
            TopDocs topDocs = GetTopDocs(collector, p);
            r.totalCount = topDocs.TotalHits;
            var hits = topDocs.ScoreDocs;
            t.FinishCollectingTime();

            t.BeginPagingTime();
            GetResultPage(hits, p, r);
            t.FinishPagingTime();

            return r;
        }
        protected abstract void GetResultPage(ScoreDoc[] hits, SearchParams p, SearchResult r);

        protected Collector CreateCollector(int size, SearchParams searchParams)
        {
            if (this.LucQuery.HasSort)
                return new SnTopFieldCollector(size, searchParams, new Sort(this.LucQuery.SortFields));
            return new SnTopScoreDocCollector(size, searchParams);
        }
        protected TopDocs GetTopDocs(Collector collector, SearchParams p)
        {
            return ((ISnCollector)collector).TopDocs(p.skip);
        }

        protected internal bool IsPermitted(Document doc)
        {
            var path = doc.Get(LucObject.FieldName.Path);

            var createdById = IntegerIndexHandler.ConvertBack(doc.Get(LucObject.FieldName.CreatedById));
            var lastModifiedById = IntegerIndexHandler.ConvertBack(doc.Get(LucObject.FieldName.ModifiedById));
            var isLastPublic = BooleanIndexHandler.ConvertBack(doc.Get(LucObject.FieldName.IsLastPublic));
            var isLastDraft = BooleanIndexHandler.ConvertBack(doc.Get(LucObject.FieldName.IsLastDraft));

            var docLevel = GetDocumentLevel(path, createdById, lastModifiedById);
            var fieldLevel = this.LucQuery.FieldLevel;
            if (this.AllVersions)
            {
                var canAccesOldVersions = SecurityHandler.HasPermission(this.User, path, createdById, lastModifiedById, PermissionType.RecallOldVersion);
                switch (docLevel)
                {
                    case DocumentOpenLevel.Denied:
                        return false;
                    case DocumentOpenLevel.See:
                        return isLastPublic && canAccesOldVersions && fieldLevel <= QueryFieldLevel.HeadOnly;
                    case DocumentOpenLevel.Preview:
                        return isLastPublic && canAccesOldVersions && fieldLevel <= QueryFieldLevel.NoBinaryOrFullText;
                    case DocumentOpenLevel.Open:
                        return isLastPublic;
                    case DocumentOpenLevel.OpenMinor:
                        return canAccesOldVersions;
                    default:
                        throw new NotImplementedException("##Unknown DocumentOpenLevel");
                }
            }
            else
            {
                switch (docLevel)
                {
                    case DocumentOpenLevel.Denied:
                        return false;
                    case DocumentOpenLevel.See:
                        return isLastPublic && fieldLevel <= QueryFieldLevel.HeadOnly;
                    case DocumentOpenLevel.Preview:
                        return isLastPublic && fieldLevel <= QueryFieldLevel.NoBinaryOrFullText;
                    case DocumentOpenLevel.Open:
                        return isLastPublic;
                    case DocumentOpenLevel.OpenMinor:
                        return isLastDraft;
                    default:
                        throw new NotImplementedException("##Unknown DocumentOpenLevel");
                }
            }
        }
        private DocumentOpenLevel GetDocumentLevel(string path, int creatorId, int lastModifierId)
        {
            var userId = this.User.Id;
            if (userId == -1)
                return DocumentOpenLevel.OpenMinor;
            if (userId < -1)
                return DocumentOpenLevel.Denied;

            bool isCreator = userId == creatorId;
            bool isLastModifier = userId == lastModifierId;

            var identities = new List<int>(((SenseNet.ContentRepository.User)this.User).Security.GetPrincipals(isCreator, isLastModifier));

            SecurityEntry[] entries = null;
            using(new SystemAccount())
                entries = SecurityHandler.GetEffectiveEntries(path, creatorId, lastModifierId);

            uint allowBits = 0;
            uint denyBits = 0;
            foreach (var entry in entries)
            {
                if (identities.Contains( entry.PrincipalId))
                {
                    allowBits |= entry.AllowBits;
                    denyBits |= entry.DenyBits;
                }
            }
            allowBits = allowBits & ~denyBits;
            var docLevel = DocumentOpenLevel.Denied;
            if ((allowBits & PermissionType.See.Mask) > 0)
                docLevel = DocumentOpenLevel.See;
            if ((allowBits & PermissionType.Preview.Mask) > 0)
                docLevel = DocumentOpenLevel.Preview;
            if ((allowBits & PermissionType.PreviewWithoutRedaction.Mask) > 0)
                docLevel = DocumentOpenLevel.Open;
            if ((allowBits & PermissionType.OpenMinor.Mask) > 0)
                docLevel = DocumentOpenLevel.OpenMinor;
            return docLevel;
        }
    }


    internal class QueryExecutor20131012 : QueryExecutor
    {
        protected override SearchResult DoExecute(SearchParams p)
        {
            p.skip = this.LucQuery.Skip;

            var maxtop = p.numDocs - p.skip;
            if (maxtop < 1)
                return SearchResult.Empty;

            SearchResult r = null;
            SearchResult r1 = null;

            var defaultTops = SenseNet.ContentRepository.Storage.StorageContext.Search.DefaultTopAndGrowth;
            var howManyList = new List<int>(defaultTops);
            if (howManyList[howManyList.Count - 1] == 0)
                howManyList[howManyList.Count - 1] = int.MaxValue;

            if (p.top < int.MaxValue)
            {
                var howMany = p.top;        //(p.top < int.MaxValue / 2) ? p.top * 2 : int.MaxValue;          // numDocs; // * 4; // * 2;
                if ((long)howMany > maxtop)
                    howMany = maxtop - p.skip;
                while (howManyList.Count > 0)
                {
                    if (howMany < howManyList[0])
                        break;
                    howManyList.RemoveAt(0);
                }
                howManyList.Insert(0, howMany);
            }

            var top0 = p.top;
            for (var i = 0; i < howManyList.Count; i++)
            {
                var defaultTop = howManyList[i];
                if (defaultTop == 0)
                    defaultTop = p.numDocs;

                p.howMany = defaultTop;
                p.useHowMany = i < howManyList.Count - 1;
                var maxSize = i == 0 ? p.numDocs : r.totalCount;
                p.collectorSize = Math.Min(defaultTop, maxSize - p.skip) + p.skip;

                r1 = this.Search(p);

                if (i == 0)
                    r = r1;
                else
                    r.Add(r1);
                p.skip += r.nextIndex;
                p.top = top0 - r.result.Count;

                if (r.result.Count == 0 || r.result.Count >= top0 || r.result.Count >= r.totalCount)
                    break;
            }
            p.timer.Stop();
            return r;
        }
        protected override void GetResultPage(ScoreDoc[] hits, SearchParams p, SearchResult r)
        {
            var result = new List<LucObject>();
            if (hits.Length == 0)
            {
                r.result = result;
                return;
            }

            var upperBound = hits.Length;
            var index = 0;
            while (true)
            {
                Document doc = p.searcher.Doc(hits[index].Doc);
                result.Add(new LucObject(doc));
                if (result.Count == p.top)
                {
                    index++;
                    break;
                }
                if (++index >= upperBound)
                    break;
            }
            r.nextIndex = index;
            r.result = result;
        }
    }

    internal class QueryExecutor20131012CountOnly : QueryExecutor
    {
        protected override SearchResult DoExecute(SearchParams p)
        {
            p.skip = 0;

            var maxtop = p.numDocs;
            if (maxtop < 1)
                return SearchResult.Empty;

            SearchResult r = null;
            var defaultTop = p.numDocs;

            p.howMany = defaultTop;
            p.useHowMany = false;
            var maxSize = p.numDocs;
            p.collectorSize = 1;

            r = Search(p);

            p.timer.Stop();
            return r;
        }
        protected override void GetResultPage(ScoreDoc[] hits, SearchParams p, SearchResult r)
        {
            // do nothing
        }
    }
}
