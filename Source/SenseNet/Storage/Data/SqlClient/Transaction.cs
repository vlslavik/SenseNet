using System;
using System.Data;
using System.Data.SqlClient;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    /// <summary>
    /// Represents a database transaction.
    /// </summary>
    internal class Transaction : ITransactionProvider
    {
        private static long _lastId;

        private bool _disposed;

        private long _id;
        private DateTime _started;
        private SqlConnection _conn;
        private SqlTransaction _tran;

        internal Transaction()
        {
            _id = System.Threading.Interlocked.Increment(ref _lastId);
        }

        internal SqlConnection Connection
        {
            get { return _conn; }
        }

        internal SqlTransaction Tran
        {
            get { return _tran; }
        }


        #region ITransactionProvider Members

        public long Id { get { return _id; } }

        public DateTime Started { get { return _started; } }

        public IsolationLevel IsolationLevel
        {
            get { return (_tran != null) ? _tran.IsolationLevel : IsolationLevel.Unspecified; }
        }

        public void Begin(IsolationLevel isolationLevel)
        {
            _conn = new SqlConnection(RepositoryConfiguration.ConnectionString);
            _conn.Open();
            _tran = _conn.BeginTransaction(isolationLevel);
            _started = DateTime.UtcNow;
        }

        public void Commit()
        {
            _tran.Commit();
        }

        public void Rollback()
        {
            _tran.Rollback();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Transaction()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this._disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    this.Close();
                }

                // Call the appropriate methods to clean up 
                // unmanaged resources here.
                // If disposing is false, 
                // only the following code is executed.
                // (code goes here...)
            }
            _disposed = true;
        }

        #endregion

        private void Close()
        {
            if (_conn != null)
                _conn.Dispose();

            _conn = null;
            _tran = null;
        }
    }
}