using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage.Search;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using System.Collections;
using SenseNet.ContentRepository.Storage.Security;
using Lucene.Net.Index;
using Lucene.Net.Util;
using SenseNet.Diagnostics;
using System.Diagnostics;
using SenseNet.Search.Parser;
using SenseNet.Search.Indexing;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.Search
{
    internal enum QueryFieldLevel { NotDefined = 0, HeadOnly = 1, NoBinaryOrFullText = 2, BinaryOrFullText = 3 }
    public class LucQuery
    {
        private static string[] _headOnlyFields = SenseNet.ContentRepository.Storage.Node.GetHeadOnlyProperties();

        public QueryTraceInfo TraceInfo { get; private set; }
        public static Query FullSetQuery = NumericRangeQuery.NewIntRange("Id", 0, null, false, false); //MachAllDocsQuery in 3.0.3
        public static readonly string NullReferenceValue = "null";

        private Query __query;
        public Query Query
        {
            get { return __query; }
            private set
            {
                __query = value;
                TraceInfo.Query = value;
            }
        }
        public string QueryText { get { return QueryToString(Query); } }
        internal QueryFieldLevel FieldLevel { get; private set; }

        public IUser User { get; set; }
        public SortField[] SortFields { get; set; }
        public bool HasSort { get { return SortFields != null && SortFields.Length > 0; } }
        public string Projection { get; private set; }

        [Obsolete("Use Skip instead. Be aware that StartIndex is 1-based but Skip is 0-based.")]
        public int StartIndex
        {
            get
            {
                return Skip + 1;
            }
            set
            {
                Skip = Math.Max(0, value - 1);
            }
        }
        public int Skip { get; set; }
        public int PageSize { get; set; }
        public int Top { get; set; }
        public bool CountOnly { get; set; }
        public FilterStatus EnableAutofilters { get; set; }
        public FilterStatus EnableLifespanFilter { get; set; }
        public static readonly FilterStatus EnableAutofilters_DefaultValue = FilterStatus.Enabled;
        public static readonly FilterStatus EnableLifespanFilter_DefaultValue = FilterStatus.Disabled;
        internal bool ThrowIfEmpty { get; set; }  //only carries: linq visitor sets, executor reads
        internal bool ExistenceOnly { get; set; } //only carries: linq visitor sets, executor reads

        public int TotalCount { get; private set; }

        private Query _autoFilterQuery;
        internal Query AutoFilterQuery
        {
            get
            {
                if (_autoFilterQuery == null)
                {
                    var parser = new SnLucParser();
                    _autoFilterQuery = parser.Parse("IsSystemContent:no");
                }
                return _autoFilterQuery;
            }
        }

        private Query _lifespanQuery;
        internal Query LifespanQuery
        {
            get
            {
                if (_lifespanQuery == null)
                {
                    var parser = new SnLucParser();
                    var lfspText = LucQueryTemplateReplacer.ReplaceTemplates("EnableLifespan:no OR (+ValidFrom:<@@CurrentTime@@ +(ValidTill:>@@CurrentTime@@ ValidTill:'0001-01-01 00:00:00'))");
                    _lifespanQuery = parser.Parse(lfspText);
                }
                return _lifespanQuery;
            }
        }

        private LucQuery()
        {
            TraceInfo = new QueryTraceInfo();
        }

        public static LucQuery Create(NodeQuery nodeQuery)
        {
            NodeQueryParameter[] parameters;
            var result = new LucQuery();
            result.TraceInfo.BeginCrossCompilingTime();

            SortField[] sortFields;
            string oldQueryText;
            try
            {
                var compiler = new SnLucCompiler();
                var compiledQueryText = compiler.Compile(nodeQuery, out parameters);

                sortFields = (from order in nodeQuery.Orders
                              select new SortField(
                                  GetFieldNameByPropertyName(order.PropertyName),
                                  GetSortType(order.PropertyName), //SortField.STRING,
                                  order.Direction == OrderDirection.Desc)).ToArray();

                oldQueryText = compiler.CompiledQuery.ToString();
                oldQueryText = oldQueryText.Replace("[*", "[ ").Replace("*]", " ]").Replace("{*", "{ ").Replace("*}", " }");
                result.TraceInfo.InputText = oldQueryText;
            }
            finally
            {
                result.TraceInfo.FinishCrossCompilingTime();
            }
            result.TraceInfo.BeginParsingTime();
            Query newQuery;
            try
            {
                newQuery = new SnLucParser().Parse(oldQueryText);
            }
            finally
            {
                result.TraceInfo.FinishParsingTime();
            }
            result.Query = newQuery; // compiler.CompiledQuery,
            result.User = nodeQuery.User;
            result.SortFields = sortFields;
            result.StartIndex = nodeQuery.Skip;
            result.PageSize = nodeQuery.PageSize;
            result.Top = nodeQuery.Top;
            result.EnableAutofilters = FilterStatus.Disabled;
            result.EnableLifespanFilter = FilterStatus.Disabled;

            return result;
        }
        private static string GetFieldNameByPropertyName(string propertyName)
        {
            if (propertyName == "NodeId") return "Id";
            return propertyName;
        }
        private static int GetSortType(string propertyName)
        {
            var x = SenseNet.ContentRepository.Schema.ContentTypeManager.GetPerFieldIndexingInfo(GetFieldNameByPropertyName(propertyName));
            if (x != null)
                return x.IndexFieldHandler.SortingType;
            return SortField.STRING;
        }
        public static LucQuery Create(Query luceneQuery)
        {
            return Create(luceneQuery, QueryFieldLevel.NotDefined);
        }
        internal static LucQuery Create(Query luceneQuery, QueryFieldLevel level)
        {
            var query = new LucQuery { Query = luceneQuery };
            query.TraceInfo.InputText = "";
            query.FieldLevel = level;
            return query;
        }

        public static LucQuery Parse(string luceneQueryText)
        {
            var result = new LucQuery();
            result.TraceInfo.InputText = luceneQueryText;

            result.TraceInfo.BeginParsingTime();
            var parser = new SnLucParser();
            Query query;
            try
            {
                var replacedText = LucQueryTemplateReplacer.ReplaceTemplates(luceneQueryText);
                query = parser.Parse(replacedText);
            }
            finally
            {
                result.TraceInfo.FinishParsingTime();
            }
            //Run EmptyTermVisitor if the parser created empty query term.
            if (parser.ParseEmptyQuery)
            {
                var visitor = new EmptyTermVisitor();
                result.Query = visitor.Visit(query);
            }
            else
            {
                result.Query = query;
            }

            var sortFields = new List<SortField>();
            foreach (var control in parser.Controls)
            {
                switch (control.Name)
                {
                    case SnLucLexer.Keywords.Select:
                        result.Projection = control.Value;
                        break;
                    case SnLucLexer.Keywords.Top:
                        result.Top = Convert.ToInt32(control.Value);
                        break;
                    case SnLucLexer.Keywords.Skip:
                        result.Skip = Convert.ToInt32(control.Value);
                        break;
                    case SnLucLexer.Keywords.Sort:
                        sortFields.Add(CreateSortField(control.Value, false));
                        break;
                    case SnLucLexer.Keywords.ReverseSort:
                        sortFields.Add(CreateSortField(control.Value, true));
                        break;
                    case SnLucLexer.Keywords.Autofilters:
                        result.EnableAutofilters = control.Value == SnLucLexer.Keywords.On ? FilterStatus.Enabled : FilterStatus.Disabled;
                        break;
                    case SnLucLexer.Keywords.Lifespan:
                        result.EnableLifespanFilter = control.Value == SnLucLexer.Keywords.On ? FilterStatus.Enabled : FilterStatus.Disabled;
                        break;
                    case SnLucLexer.Keywords.CountOnly:
                        result.CountOnly = true;
                        break;
                }
            }
            result.SortFields = sortFields.ToArray();
            result.FieldLevel = parser.FieldLevel;
            return result;
        }
        internal static SortField CreateSortField(string fieldName, bool reverse)
        {
            var info = SenseNet.ContentRepository.Schema.ContentTypeManager.GetPerFieldIndexingInfo(fieldName);
            var sortType = SortField.STRING;
            if (info != null)
            {
                sortType = info.IndexFieldHandler.SortingType;
                fieldName = info.IndexFieldHandler.GetSortFieldName(fieldName);
            }
            if (sortType == SortField.STRING)
                return new SortField(fieldName, System.Threading.Thread.CurrentThread.CurrentCulture, reverse);
            return new SortField(fieldName, sortType, reverse);
        }

        public static bool IsAutofilterEnabled(FilterStatus value)
        {
            switch (value)
            {
                case FilterStatus.Default:
                    return EnableAutofilters_DefaultValue == FilterStatus.Enabled;
                case FilterStatus.Enabled:
                    return true;
                case FilterStatus.Disabled:
                    return false;
                default:
                    throw new NotImplementedException("Unknown FilterStatus: " + value);
            }
        }
        public static bool IsLifespanFilterEnabled(FilterStatus value)
        {
            switch (value)
            {
                case FilterStatus.Default:
                    return EnableLifespanFilter_DefaultValue == FilterStatus.Enabled;
                case FilterStatus.Enabled:
                    return true;
                case FilterStatus.Disabled:
                    return false;
                default:
                    throw new NotImplementedException("Unknown FilterStatus: " + value);
            }
        }

        //========================================================================================

        public IEnumerable<LucObject> Execute()
        {
            return Execute(false);
        }
        public IEnumerable<LucObject> Execute(bool allVersions)
        {
            //if (CountOnly)
            //    return Execute(allVersions, new QueryExecutor20100701CountOnly());
            //return Execute(allVersions, new QueryExecutor20100701());
            if (CountOnly)
                return Execute(allVersions, new QueryExecutor20131012CountOnly());
            return Execute(allVersions, new QueryExecutor20131012());
        }
        internal IEnumerable<LucObject> Execute(IQueryExecutor executor)
        {
            return Execute(false, executor);
        }
        internal IEnumerable<LucObject> Execute(bool allVersions, IQueryExecutor executor)
        {
            if (this.FieldLevel == QueryFieldLevel.NotDefined)
                this.FieldLevel = DetermineFieldLevel();
            var result = executor.Execute(this, allVersions);
            TotalCount = executor.TotalCount;
            return result == null ? new LucObject[0] : result;
        }
        private QueryFieldLevel DetermineFieldLevel()
        {
            var v = new FieldNameVisitor();
            v.Visit(this.Query);
            return GetFieldLevel(v.FieldNames);
        }
        internal static QueryFieldLevel GetFieldLevel(IEnumerable<string> fieldNames)
        {
            var fieldLevel = QueryFieldLevel.NotDefined;
            foreach (var fieldName in fieldNames)
            {
                var indexingInfo = SenseNet.ContentRepository.Schema.ContentTypeManager.GetPerFieldIndexingInfo(fieldName);
                var level = GetFieldLevel(fieldName, indexingInfo);
                fieldLevel = level > fieldLevel ? level : fieldLevel;
            }
            return fieldLevel;
        }
        internal static QueryFieldLevel GetFieldLevel(string fieldName, PerFieldIndexingInfo indexingInfo)
        {
            QueryFieldLevel level;

            if (fieldName == LucObject.FieldName.AllText)
                level = QueryFieldLevel.BinaryOrFullText;
            else if (indexingInfo == null)
                level = QueryFieldLevel.BinaryOrFullText;
            else if (indexingInfo.FieldDataType == typeof(SenseNet.ContentRepository.Storage.BinaryData))
                level = QueryFieldLevel.BinaryOrFullText;
            else if (fieldName == LucObject.FieldName.InFolder || fieldName == LucObject.FieldName.InTree
                || fieldName == LucObject.FieldName.Type || fieldName == LucObject.FieldName.TypeIs
                || _headOnlyFields.Contains(fieldName))
                level = QueryFieldLevel.HeadOnly;
            else
                level = QueryFieldLevel.NoBinaryOrFullText;

            return level;
        }

        public override string ToString()
        {
            var result = new StringBuilder(QueryText);
            if (CountOnly)
                result.Append(" ").Append(SnLucLexer.Keywords.CountOnly);
            if (Top != 0)
                result.Append(" ").Append(SnLucLexer.Keywords.Top).Append(":").Append(Top);
            if (Skip != 0)
                result.Append(" ").Append(SnLucLexer.Keywords.Skip).Append(":").Append(Skip);
            if (this.HasSort)
            {
                foreach (var sortField in this.SortFields)
                    if (sortField.GetReverse())
                        result.Append(" ").Append(SnLucLexer.Keywords.ReverseSort).Append(":").Append(sortField.GetField());
                    else
                        result.Append(" ").Append(SnLucLexer.Keywords.Sort).Append(":").Append(sortField.GetField());
            }
            if (EnableAutofilters != FilterStatus.Default && EnableAutofilters != EnableAutofilters_DefaultValue)
                result.Append(" ").Append(SnLucLexer.Keywords.Autofilters).Append(":").Append(EnableAutofilters_DefaultValue == FilterStatus.Enabled ? SnLucLexer.Keywords.Off : SnLucLexer.Keywords.On);
            if (EnableLifespanFilter != FilterStatus.Default && EnableLifespanFilter != EnableLifespanFilter_DefaultValue)
                result.Append(" ").Append(SnLucLexer.Keywords.Lifespan).Append(":").Append(EnableLifespanFilter_DefaultValue == FilterStatus.Enabled ? SnLucLexer.Keywords.Off : SnLucLexer.Keywords.On);
            return result.ToString();
        }
        private string QueryToString(Query query)
        {
            try
            {
                var visitor = new ToStringVisitor();
                visitor.Visit(query);
                return visitor.ToString();
            }
            catch (Exception e)
            {
                Logger.WriteException(e);

                var c = query.ToString().ToCharArray();
                for (int i = 0; i < c.Length; i++)
                    if (c[i] < ' ')
                        c[i] = '.';
                return new String(c);
            }
        }

        internal void SetSort(IEnumerable<SortInfo> sort)
        {
            var sortFields = new List<SortField>();
            if (sort != null)
                foreach (var field in sort)
                    sortFields.Add(CreateSortField(field.FieldName, field.Reverse));
            this.SortFields = sortFields.ToArray();
        }

        public void AddAndClause(LucQuery q2)
        {
            var boolQ = new BooleanQuery();
            boolQ.Add(Query, BooleanClause.Occur.MUST);
            boolQ.Add(q2.Query, BooleanClause.Occur.MUST);
            Query = boolQ;
        }
        public void AddOrClause(LucQuery q2)
        {
            var boolQ = new BooleanQuery();
            boolQ.Add(Query, BooleanClause.Occur.SHOULD);
            boolQ.Add(q2.Query, BooleanClause.Occur.SHOULD);
            Query = boolQ;
        }
    }

    internal class SearchParams
    {
        internal int collectorSize;
        internal Searcher searcher;
        internal int numDocs;
        internal Query query;
        internal int skip;
        internal int top;
        internal int howMany;
        internal bool useHowMany;
        internal bool allVersions;
        internal Stopwatch timer;
        internal QueryExecutor executor;
    }

    internal class SearchResult
    {
        public static readonly SearchResult Empty;

        static SearchResult()
        {
            Empty = new SearchResult(null) { searches = 0 };
        }

        internal SearchResult(Stopwatch timer)
        {
            searchTimer = new SearchTimer(timer);
        }

        internal SearchTimer searchTimer;
        internal List<LucObject> result;
        internal int totalCount;
        internal int nextIndex;
        internal int searches = 1;

        internal void Add(SearchResult other)
        {
            result.AddRange(other.result);
            nextIndex = other.nextIndex;
            searches += other.searches;

            searchTimer.CollectingTime += other.searchTimer.CollectingTime;
            searchTimer.KernelTime += other.searchTimer.KernelTime;
            searchTimer.PagingTime += other.searchTimer.PagingTime;
        }
    }

    internal class SearchTimer
    {
        public long KernelTime { get; internal set; }
        public long CollectingTime { get; internal set; }
        public long PagingTime { get; internal set; }

        public SearchTimer(Stopwatch timer)
        {
            this.timer = timer;
        }

        private Stopwatch timer;

        private long kernelStart;
        internal void BeginKernelTime()
        {
            kernelStart = timer.ElapsedTicks;
        }
        internal void FinishKernelTime()
        {
            KernelTime = timer.ElapsedTicks - kernelStart;
        }

        private long collectingStart;
        internal void BeginCollectingTime()
        {
            collectingStart = timer.ElapsedTicks;
        }
        internal void FinishCollectingTime()
        {
            CollectingTime = timer.ElapsedTicks - collectingStart;
        }

        private long pagingStart;
        public void BeginPagingTime()
        {
            pagingStart = timer.ElapsedTicks;
        }
        public void FinishPagingTime()
        {
            PagingTime = timer.ElapsedTicks - pagingStart;
        }
    }
}
