using System;
using System.Data.SqlTypes;
using System.IO;

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Wrapper class for an SqlFilestream. Handles buffered reading
    /// and writing of a file stream, including opening and closing
    /// a transaction if no open transaction exists.
    /// </summary>
    public class SenseNetSqlFileStream : Stream
    {
        //========================================================= Properties

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return false; } }

        public override long Position { get; set; }

        long _length;
        public override long Length
        {
            get { return _length; }
        }

        private byte[] _innerBuffer;
        private long _innerBufferFirstPostion;

        internal int BinaryPropertyId { get; set; }

        protected FileStreamData _fileStreamData;

        //========================================================= Constructor

        public SenseNetSqlFileStream(long size, int binaryPropertyId, FileStreamData fileStreamData = null)
        {
            _length = size;
            _fileStreamData = fileStreamData != null && fileStreamData.TransactionContext != null ? fileStreamData : null;

            BinaryPropertyId = binaryPropertyId;
        }

        //========================================================= Overrided mthods

        public override void SetLength(long value)
        {
            throw new NotSupportedException("SenseNetSqlFileStream does not support setting length.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("SenseNetSqlFileStream does not support writing.");
        }

        public override void Flush()
        {
            throw new NotSupportedException("SenseNetSqlFileStream does not support flushing.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position = Position + offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
                default:
                    throw new NotSupportedException(String.Concat("SeekOrigin type ", origin, " is not supported."));
            }
            return Position;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset + count > buffer.Length)
                throw new ArgumentException("Offset + count must not be greater than the buffer length.");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", "The offset must be greater than zero.");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "The count must be greater than zero.");

            // Calculate the maximum count of the bytes that can be read.
            // Return immediately if nothing to read.
            var maximumReadableByteCount = Length - Position;
            if (maximumReadableByteCount < 1)
                return 0;

            var isLocalTransaction = false;
            var realCount = (int)Math.Min(count, maximumReadableByteCount);

            if (CanInnerBufferHandleReadRequest(realCount))
            {
                Array.Copy(_innerBuffer, (int)Position - _innerBufferFirstPostion, buffer, offset, realCount);
            }
            else
            {
                if (!TransactionScope.IsActive)
                {
                    //make sure we do not use an obsolete value
                    _fileStreamData = null;

                    //Start a new transaction here to serve the needs of the SqlFileStream type.
                    TransactionScope.Begin();
                    isLocalTransaction = true;
                }

                try
                {
                    //Load transaction data for SqlFilestream. If this is not a local transaction,
                    //than we will be able to use this data in the future if the client calls
                    //the Read method multiple times and will not have to execute SQL queries
                    //every time.
                    if (_fileStreamData == null || _fileStreamData.TransactionContext == null)
                        _fileStreamData = DataProvider.Current.LoadFileStreamData(this.BinaryPropertyId);
                    
                    if (_fileStreamData == null)
                        throw new InvalidOperationException("Transaction data and file path could not be retrieved for SqlFilestream");
                    
                    using (var fs = new SqlFileStream(_fileStreamData.Path, _fileStreamData.TransactionContext, FileAccess.Read, FileOptions.SequentialScan, 0))
                    {
                        fs.Seek(Position, SeekOrigin.Begin);

                        _innerBuffer = null;

                        var bytesRead = 0;
                        var bytesStoredInInnerBuffer = 0;

                        while (bytesRead < realCount)
                        {
                            var bytesToReadInThisIteration = (int)Math.Min(this.Length - Position - bytesRead, RepositoryConfiguration.BinaryChunkSize);
                            var bytesToStoreInThisIteration = Math.Min(bytesToReadInThisIteration, realCount - bytesRead);
                            var tempBuffer = new byte[bytesToReadInThisIteration];

                            //copy the bytes from the file stream to the temp buffer
                            //(it is possible that we loaded a lot more bytes than the client requested)
                            fs.Read(tempBuffer, bytesRead, bytesToReadInThisIteration);

                            //first iteration: create inner buffer for caching a part of the stream in memory
                            if (_innerBuffer == null)
                            {
                                _innerBuffer = new byte[GetInnerBufferSize(realCount)];
                                _innerBufferFirstPostion = Position;
                            }

                            //store a fragment of the data in the inner buffer if possible
                            if (bytesStoredInInnerBuffer < _innerBuffer.Length)
                            {
                                var bytesToStoreInInnerBuffer = Math.Min(bytesToReadInThisIteration, _innerBuffer.Length - bytesStoredInInnerBuffer);

                                Array.Copy(tempBuffer, 0, _innerBuffer, bytesStoredInInnerBuffer, bytesToStoreInInnerBuffer);
                                bytesStoredInInnerBuffer += bytesToStoreInInnerBuffer;
                            }

                            //copy the chunk from the temp buffer to the buffer of the caller
                            Array.Copy(tempBuffer, 0, buffer, bytesRead, bytesToStoreInThisIteration);
                            bytesRead += bytesToReadInThisIteration;
                        }
                    }
                }
                catch
                {
                    if (isLocalTransaction && TransactionScope.IsActive)
                    {
                        TransactionScope.Rollback();

                        //cleanup
                        isLocalTransaction = false;
                        _fileStreamData = null;
                    }

                    throw;
                }
                finally 
                {
                    if (isLocalTransaction && TransactionScope.IsActive)
                    {
                        TransactionScope.Commit();

                        //Set filestream data to null as this was a local transaction and we cannot use it anymore
                        _fileStreamData = null;
                    }
                }
            }

            Position += realCount;

            return realCount;
        }

        //========================================================= Helper methods

        private bool CanInnerBufferHandleReadRequest(int count)
        {
            if (_innerBuffer == null)
                return false;

            if (Position < _innerBufferFirstPostion)
                return false;

            if ((_innerBufferFirstPostion + _innerBuffer.Length) < (Position + count))
                return false;

            return true;
        }

        private static int GetInnerBufferSize(int realCount)
        {
            //determine the inner buffer size. It should not be bigger 
            //than all the data that will be loaded in this Read method call.

            return realCount <= RepositoryConfiguration.BinaryChunkSize 
                ? Math.Min(RepositoryConfiguration.BinaryChunkSize, RepositoryConfiguration.BinaryBufferSize) 
                : Math.Min((realCount / RepositoryConfiguration.BinaryChunkSize + 1) * RepositoryConfiguration.BinaryChunkSize, RepositoryConfiguration.BinaryBufferSize);
        }
    }
}
