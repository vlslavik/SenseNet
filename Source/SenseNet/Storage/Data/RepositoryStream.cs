using System;
using System.IO;

namespace SenseNet.ContentRepository.Storage.Data
{
    public class BinaryCacheEntity
    {
        public byte[] RawData { get; set; }
        public long Length { get; set; }
        public int BinaryPropertyId { get; set; }
        public bool UseFileStream { get; set; }

        public static string GetCacheKey(int versionId, int propertyTypeId)
        {
            return string.Concat("RawBinary.", versionId, ".", propertyTypeId);
        }
    }

    internal class RepositoryStream : Stream
    {
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

        private int _binaryPropertyId;
        internal int BinaryPropertyId
        {
            get { return _binaryPropertyId; }
            set { _binaryPropertyId = value; }
        }

        public RepositoryStream(int binaryPropertyId, long size, byte[] binary)
        {
            if (binary == null)
                throw new ArgumentNullException("binary", "Binary cannot be null. If the binary cannot be fully loaded - therefore is null - use the (long size, int binaryPropertyId) ctor.");
            _length = size;
            _innerBuffer = binary;
            _binaryPropertyId = binaryPropertyId;
        }

        public RepositoryStream(long size, int binaryPropertyId)
        {
            _length = size;
            _binaryPropertyId = binaryPropertyId;
        } 

        public override void SetLength(long value)
		{ throw new NotSupportedException("RepositoryStream does not support setting length."); }

        public override void Write(byte[] buffer, int offset, int count)
		{ throw new NotSupportedException("RepositoryStream does not support writing."); }

        public override void Flush()
		{ throw new NotSupportedException("RepositoryStream does not support flushing."); }
        
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

            var realCount = (int)Math.Min(count, maximumReadableByteCount);

            if (CanInnerBufferHandleReadRequest(realCount))
            {
                Array.Copy(_innerBuffer, (int)Position - _innerBufferFirstPostion, buffer, offset, realCount);
            }
            else
            {
                _innerBuffer = null;

                var bytesRead = 0;
                var bytesStoredInInnerBuffer = 0;

                while (bytesRead < realCount)
                {
                    var bytesToReadInThisIteration = (int)Math.Min(this.Length - Position - bytesRead, RepositoryConfiguration.BinaryChunkSize);   //bytes to load from the db
                    var bytesToStoreInThisIteration = Math.Min(bytesToReadInThisIteration, realCount - bytesRead);                                  //bytes that we will copy to the buffer of the caller
                    var tempBuffer = DataProvider.Current.LoadBinaryFragment(_binaryPropertyId, Position + bytesRead, bytesToReadInThisIteration);  //stores the current chunk

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

            Position += realCount;

            return realCount;
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
                    Position = Length + offset;
                    break;
                default:
                    throw new NotSupportedException(String.Concat("SeekOrigin type ", origin, " is not supported."));
            }
            return Position;
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
