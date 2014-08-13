﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Search;
using Lucene.Net.Search;
using Lucene.Net.Index;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Linq
{
    public class LinqContentEnumerator<T> : IEnumerator<T>
    {
        ContentSet<T> _queryable;
        IEnumerable<T> _result;
        IEnumerator<T> _resultEnumerator;
        LucQuery _query;
        private bool _isContent;


        public LinqContentEnumerator(ContentSet<T> queryable)
        {
            _isContent = typeof(T) == typeof(Content);
            _queryable = queryable;
        }

        public void Dispose()
        {
        }
        public T Current
        {
            get { return _resultEnumerator.Current; }
        }
        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }
        public void Reset()
        {
            _resultEnumerator.Reset();
        }
        public bool MoveNext()
        {
            if (_result == null)
            {
                Compile();
                var qresult = _query.Execute();
                if(_isContent)
                    _result = (IEnumerable<T>)qresult.Select(x => Content.Load(x.NodeId));
                else
                    _result = qresult.Select(x => SenseNet.ContentRepository.Storage.Node.LoadNode(x.NodeId)).Cast<T>();
                _resultEnumerator = _result.GetEnumerator();
            }
            return _resultEnumerator.MoveNext();
        }
        private void Compile()
        {
            if (_query == null)
                _query = SnExpression.BuildQuery(_queryable.Expression, typeof(T), _queryable.ContextPath, _queryable.ChildrenDefinition);
        }
        public string GetQueryText()
        {
            Compile();
            return _query.ToString();
        }

    }
}
