using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Util;
using System.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.Search.Indexing
{
    internal struct Timestamps : IComparable<Timestamps>
    {
        public static readonly Timestamps MaxValue = new Timestamps(long.MaxValue, long.MaxValue);
        public static readonly Timestamps MinValue = new Timestamps(long.MinValue, long.MinValue);

        public readonly long NodeTimestamp;
        public readonly long VersionTimestamp;

        public Timestamps(long nodeTimestamp, long versionTimestamp)
        {
            NodeTimestamp = nodeTimestamp;
            VersionTimestamp = versionTimestamp;
        }

        public static bool operator ==(Timestamps x, Timestamps y) { return Compare(x, y) == 0; }
        public static bool operator !=(Timestamps x, Timestamps y) { return Compare(x, y) != 0; }
        public static bool operator >(Timestamps x, Timestamps y) { return Compare(x, y) > 0; }
        public static bool operator <(Timestamps x, Timestamps y) { return Compare(x, y) < 0; }
        public static bool operator >=(Timestamps x, Timestamps y) { return Compare(x, y) >= 0; }
        public static bool operator <=(Timestamps x, Timestamps y) { return Compare(x, y) <= 0; }

        private static int Compare(Timestamps x, Timestamps y)
        {
            var q = 0;
            if ((q = x.NodeTimestamp.CompareTo(y.NodeTimestamp)) != 0)
                return q;
            return x.VersionTimestamp.CompareTo(y.VersionTimestamp);
        }
        public int CompareTo(Timestamps other)
        {
            return Compare(this, other);
        }

        public override int GetHashCode()
        {
            return (VersionTimestamp - NodeTimestamp).GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return this == (Timestamps)obj;
        }
        public override string ToString()
        {
            return "[" + NodeTimestamp + "," + VersionTimestamp + "]";
        }
    }

    internal class IndexingHistory
    {
        private object _sync = new object();

        int _limit;
        Queue<int> _queue;
        Dictionary<int, Timestamps> _storage;

        public long Count { get { return _storage.Count; } }

        public IndexingHistory()
        {
            Initialize(RepositoryConfiguration.IndexHistoryItemLimit);
        }
        private void Initialize(int size)
        {
            _limit = size;
            _queue = new Queue<int>(size);
            _storage = new Dictionary<int, Timestamps>(size);
        }

        internal int GetVersionId(Document doc)
        {
            return Int32.Parse(doc.Get(LucObject.FieldName.VersionId));
        }
        internal Timestamps GetTimestamp(Document doc)
        {
            return new Timestamps(Int64.Parse(doc.Get(LucObject.FieldName.NodeTimestamp)), Int64.Parse(doc.Get(LucObject.FieldName.VersionTimestamp)));
        }
        internal bool CheckForAdd(int versionId, Timestamps timestamps)
        {
            //Debug.WriteLine(String.Format("##> CheckForAdd. Id: {0}, time: {1}", versionId, timestamp));
            lock (_sync)
            {
                if (!Exists(versionId))
                {
                    Add(versionId, timestamps);
                    return true;
                }
                return false;
            }
        }
        internal bool CheckForUpdate(int versionId, Timestamps timestamps)
        {
            //Debug.WriteLine(String.Format("##> CheckForUpdate. Id: {0}, time: {1}", versionId, timestamp));
            Timestamps? stored;
            lock (_sync)
            {
                stored = Get(versionId);
                if (stored == null)
                {
                    Add(versionId, timestamps);
                    return true;
                }
                else
                {
                    if (stored.Value >= timestamps)
                        return false;
                    Update(versionId, timestamps);
                    return true;
                }
            }
        }
        internal void ProcessDelete(Term[] deleteTerms)
        {
            //Debug.WriteLine("##> ProcessDelete. Count: " + deleteTerms.Length);
            for (int i = 0; i < deleteTerms.Length; i++)
            {
                var term = deleteTerms[i];
                if (term.Field() != LucObject.FieldName.VersionId)
                    return;
                var versionId = NumericUtils.PrefixCodedToInt(term.Text());
                ProcessDelete(versionId);
            }
        }
        internal void ProcessDelete(int versionId)
        {
            lock (_sync)
            {
                if (!Exists(versionId))
                    Add(versionId, Timestamps.MaxValue);
                else
                    Update(versionId, Timestamps.MaxValue);
            }
        }
        internal void Remove(Term[] deleteTerms)
        {
            lock (_sync)
            {
                foreach (var deleteTerm in deleteTerms)
                {
                    //var executor = new QueryExecutor20100701();
                    var q = new TermQuery(deleteTerm);
                    var lucQuery = LucQuery.Create(q);
                    lucQuery.EnableAutofilters = FilterStatus.Disabled;
                    //var result = executor.Execute(lucQuery, true);
                    var result = lucQuery.Execute(true);
                    foreach (var lucObject in result)
                        _storage.Remove(lucObject.VersionId);
                }
            }
        }
        internal bool RemoveIfLast(int versionId, Timestamps? timestamps)
        {
            lock (_sync)
            {
                var last = Get(versionId);
                if (last == timestamps)
                {
                    _storage.Remove(versionId);
                    return true;
                }
                return false;
            }
        }
        internal bool CheckHistoryChange(int versionId, Timestamps timestamps)
        {
            lock (_sync)
            {
                var lastTimestamp = Get(versionId);
                return timestamps != lastTimestamp;
            }
        }

        internal bool Exists(int versionId)
        {
            return _storage.ContainsKey(versionId);
        }
        internal Timestamps? Get(int versionId)
        {
            Timestamps result;
            if (_storage.TryGetValue(versionId, out result))
                return result;
            return null;
        }
        internal void Add(int versionId, Timestamps timestamps)
        {
            _storage.Add(versionId, timestamps);
            _queue.Enqueue(versionId);
            if (_queue.Count <= _limit)
                return;
            var k = _queue.Dequeue();
            _storage.Remove(k);
        }
        internal void Update(int versionId, Timestamps timestamps)
        {
            _storage[versionId] = timestamps;
        }
    }
}
