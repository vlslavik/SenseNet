using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Caching.Dependency;
using System.Diagnostics;
using System.Globalization;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Storage
{
	public class BinaryDataValue
	{
        private int _id;
        internal int Id
        {
            get { return _id; }
            set
            {
                _id = value;
                SetStreamId(value);
            }
        }
		internal long Size { get; set; }
		internal BinaryFileName FileName { get; set; }
		internal string ContentType { get; set; }
        internal string Checksum { get; set; }
		internal Stream Stream { get; set; }
        internal long Timestamp { get; set; }

		internal bool IsEmpty
		{
			get
			{
				if (Id > 0) return false;
				if (Size >= 0) return false;
				if (!String.IsNullOrEmpty(FileName)) return false;
				if (!String.IsNullOrEmpty(ContentType)) return false;
				return Stream == null;
			}
		}

        private void SetStreamId(int id)
        {
            var repoStream = Stream as RepositoryStream;
            if (repoStream == null)
                return;
            repoStream.BinaryPropertyId = id;
        }
	}

    /// <summary>
    /// BinaryData class handles the data of binary properties.
    /// </summary>
	public class BinaryData : IDynamicDataAccessor
	{
		BinaryDataValue __privateValue;

		//=============================================== Accessor Interface

		Node IDynamicDataAccessor.OwnerNode
		{
			get { return OwnerNode; }
			set { OwnerNode = value; }
		}
		PropertyType IDynamicDataAccessor.PropertyType
		{
			get { return PropertyType; }
			set { PropertyType = value; }
		}
		object IDynamicDataAccessor.RawData { 
			get { return RawData; }
			set { RawData = (BinaryDataValue)value; }
		}
		object IDynamicDataAccessor.GetDefaultRawData() { return GetDefaultRawData(); }

		//=============================================== Accessor Implementation

		internal Node OwnerNode { get; set; }
		internal PropertyType PropertyType { get; set; }
		internal static BinaryDataValue GetDefaultRawData()
		{
            return new BinaryDataValue
            {
                Id = 0,
                ContentType = String.Empty,
                FileName = String.Empty,
                Size = -1,
                Checksum = string.Empty,
                Stream = null
            };
		}
		BinaryDataValue RawData
		{
			get
			{
				if (OwnerNode == null)
					return __privateValue;

                // csak ez lesz, belül switchel shared/private-re
				var value = (BinaryDataValue)OwnerNode.Data.GetDynamicRawData(PropertyType);
				return value;
				//return value ?? __privateValue;
			}
			set
			{
                __privateValue = new BinaryDataValue
                {
                    Id = value.Id,
                    ContentType = value.ContentType,
                    FileName = value.FileName,
                    Size = value.Size,
                    Checksum = value.Checksum,
                    Stream = CloneStream(value.Stream),
                    Timestamp = value.Timestamp
                };
			}
		}
		public bool IsEmpty
		{
			get
			{
				if (OwnerNode == null)
					return __privateValue.IsEmpty;
				if (RawData == null)
					return true;
				return RawData.IsEmpty;
			}
		}

		//=============================================== Data

		public bool IsModified
		{
			get
			{
				if (OwnerNode == null)
					return true;
				return OwnerNode.Data.IsModified(PropertyType);
			}
		}
		private void Modifying()
		{
			//if (OwnerNode != null)
			//    OwnerNode.BackwardCompatibilityPropertySet(PropertyType.Name, this);

			if (IsModified)
				return;

			//-- Clone
			var orig = (BinaryDataValue)OwnerNode.Data.GetDynamicRawData(PropertyType);
			BinaryDataValue data;
			if (orig == null)
			{
				data = GetDefaultRawData();
			}
			else
			{
				data = new BinaryDataValue
				{
					Id = orig.Id,
					ContentType = orig.ContentType,
					FileName = orig.FileName,
					Size = orig.Size,
                    Checksum = orig.Checksum,
					Stream = orig.Stream
                    //Timestamp = orig.Timestamp
				};
			}
            OwnerNode.MakePrivateData();
            OwnerNode.Data.SetDynamicRawData(PropertyType, data, false);
		}
        private void Modified()
        {
            if(OwnerNode != null)
                if(OwnerNode.Data.SharedData != null)
                    OwnerNode.Data.CheckChanges(PropertyType);
        }

		//=============================================== Accessors

        public int Id
        {
			get { return RawData == null ? 0 : RawData.Id; }
			internal set
			{
				Modifying();
				RawData.Id = value;
                Modified();
			}
        }
		public long Size
        {
			get { return RawData == null ? -1 : RawData.Size; }
			internal set
			{
				Modifying();
				RawData.Size = value;
                Modified();
            }
        }
		public BinaryFileName FileName
		{
            get { return RawData == null ? new BinaryFileName("") : RawData.FileName; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				Modifying();
                var rawData = this.RawData;
                value = NormalizeFileName(value);
                rawData.FileName = value;
                rawData.ContentType = GetMimeType(value);
                Modified();
            }
		}
		public string ContentType
		{
			get { return RawData == null ? string.Empty : RawData.ContentType; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				Modifying();
				RawData.ContentType = value;
                Modified();
            }
		}
        public string Checksum
        {
            get
            {
                var raw = RawData;
                if (raw == null)
                    return null;
                return raw.Checksum;
            }
        }
        public long Timestamp
        {
            get
            {
                var raw = RawData;
                return raw == null ? 0 : raw.Timestamp;
            }
        }

		public Stream GetStream()
		{
			var raw = RawData;
			if (raw == null)
				return null;
			var stream = raw.Stream;
			if (stream != null)
				return CloneStream(stream);
			if (OwnerNode == null)
				return null;

            if (this.OwnerNode.SavingState != ContentSavingState.Finalized)
                throw new InvalidOperationException(SR.GetString(SR.Exceptions.General.Error_AccessToNotFinalizedBinary_2, this.OwnerNode.Path, this.PropertyType.Name));

            return DataBackingStore.GetBinaryStream2(OwnerNode.Id, OwnerNode.VersionId, PropertyType.Id);
            //return DataBackingStore.GetBinaryStream(OwnerNode.VersionId, PropertyType.Id);
		}
		public void SetStream(Stream stream)
		{
			Modifying();
            var rawData = this.RawData;
			if (stream == null)
			{
                rawData.Size = -1;
                rawData.Checksum = null;
                rawData.Stream = null;
			    rawData.Timestamp = 0;
			}
			else
			{
                rawData.Size = stream.Length;
                rawData.Stream = stream;
			    rawData.Checksum = null; //CalculateChecksum(stream);
                rawData.Timestamp = 0;
            }
            Modified();
		}
        public static string CalculateChecksum(Stream stream)
        {
            var pos = stream.Position;
            stream.Position = 0;
            var b64 = Convert.ToBase64String(new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash(stream));
            stream.Position = pos;
            return b64;
        }

        private BinaryFileName NormalizeFileName(BinaryFileName fileName)
        {
            if (fileName.FullFileName.Contains("\\"))
                return Path.GetFileName(fileName.FullFileName);
            if (fileName.FullFileName.Contains("/"))
                return RepositoryPath.GetFileName(fileName.FullFileName);
            return fileName;
        }

		//===============================================

        public BinaryData()
        {
			__privateValue = GetDefaultRawData();
        }

        public string ToBase64()
        {
            Stream stream = null;
            MemoryStream ms = null;

            try
            {
                stream = this.GetStream();

                if (stream is MemoryStream)
                {
                    ms = (MemoryStream)stream;
                    stream = null;
                }
                else
                {
                    ms = new MemoryStream();
                    stream.CopyTo(ms);
                }

                var arr = ms.ToArray();
                return Convert.ToBase64String(arr);
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
                if (ms != null)
                    ms.Dispose();
            }
        }

		public void Reset()
		{
			Id = 0;
			FileName = String.Empty;
			ContentType = String.Empty;
			Size = -1;
			this.SetStream(null);
		}
		public void CopyFrom(BinaryData data)
		{
			//Id = data.Id;
			FileName = data.FileName;
			ContentType = data.ContentType;
			Size = data.Size;
			this.SetStream(data.GetStream());
		}

		private static Stream CloneStream(Stream stream)
		{
			if (stream == null || !stream.CanRead)
				return null;

		    var repoStream = stream as RepositoryStream;
            if (repoStream != null)
                return new RepositoryStream(repoStream.Length, repoStream.BinaryPropertyId);

            var snFileStream = stream as SenseNetSqlFileStream;
            if (snFileStream != null)
                return new SenseNetSqlFileStream(snFileStream.Length, snFileStream.BinaryPropertyId);

		    long pos = stream.Position;
			stream.Seek(0, SeekOrigin.Begin);
			Stream clone = new MemoryStream(new BinaryReader(stream).ReadBytes((int)stream.Length));
			clone.Seek(0, SeekOrigin.Begin);
			stream.Seek(pos, SeekOrigin.Begin);

			return clone;
		}
		private static string GetMimeType(BinaryFileName value)
		{
            if (value == null)
                return string.Empty;
			string ext = value.Extension;
            if (ext == null)
                return string.Empty;
			if (ext.Length > 0 && ext[0] == '.')
				ext = ext.Substring(1);
            var mimeType = MimeTable.GetMimeType(ext.ToLower(CultureInfo.InvariantCulture));
			return mimeType;
		}

        //=============================================== Chunk upload/download

        /// <summary>
        /// Starts the chunk saving process by providing the token that is necessary for saving 
        /// chunks and committing the changes. It is possible to start the process without
        /// providing parameters: in this case the content does not exist yet, it will be
        /// created just before the commit method call.
        /// </summary>
        /// <param name="contentId">Id of the content</param>
        /// <param name="fieldName">Name of the binary field. Default: Binary</param>
        /// <returns>The token that is needed for chunk upload. This token must be passed to the SaveChunk method when adding binary chunks.</returns>
        public static string StartChunk(int contentId, string fieldName = "Binary")
        {
            Node node;
            PropertyType pt;

            AssertChunk(contentId, fieldName, out node, out pt);

            return DataProvider.Current.StartChunk(node.VersionId, pt.Id);
        }

        /// <summary>
        /// Inserts a set of bytes into a binary field. Can be used to upload large files in chunks. After calling this method with all the chunks, CommitChunk method must be called to finalize the process.
        /// </summary>
        /// <param name="token">The token received from the StartChunk method that needs to be called before the chunk saving operation starts.</param>
        /// <param name="fullStreamSize">Full size of the binary stream</param>
        /// <param name="buffer">Byte array that contains the chunk to write</param>
        /// <param name="offset">The position where the write operation should start</param>
        /// <param name="count">Number of bytes to write. If -1, the full buffer will be written</param>
        public static void SaveChunk(string token, long fullStreamSize, byte[] buffer, long offset, int count = -1)
        {
            if (count < 0)
                count = buffer.Length;

            DataProvider.Current.SaveChunk(token, buffer, offset, count, fullStreamSize);
        }

        /// <summary>
        /// Finalizes a chunk saving process: sets the stream size and length for the binary.
        /// </summary>
        /// <param name="contentId">Id of the content</param>
        /// <param name="token">The token received from the StartChunk method that needs to be called before the chunk saving operation starts.</param>
        /// <param name="fullStreamSize">Full size of the binary stream</param>
        /// <param name="fieldName">Name of the field. Default: Binary</param>
        /// <param name="binaryMetadata">Additional metadata for the binary row: file name, extension, content type.</param>
        public static void CommitChunk(int contentId, string token, long fullStreamSize, string fieldName = "Binary", BinaryData binaryMetadata = null)
        {
            Node node;
            PropertyType pt;

            AssertChunk(contentId, fieldName, out node, out pt);

            DataProvider.Current.CommitChunk(node.VersionId, pt.Id, token, fullStreamSize, binaryMetadata == null ? null : binaryMetadata.RawData);

            NodeIdDependency.FireChanged(node.Id);
            StorageContext.L2Cache.Clear();
        }

        private static void AssertChunk(int contentId, string fieldName, out Node node, out PropertyType propertyType)
        {
            if (contentId < 1)
                throw new ContentNotFoundException("Unknown content during chunk upload. Id: " + contentId);

            node = Node.LoadNode(contentId);
            if (node == null)
                throw new ContentNotFoundException(contentId.ToString());

            //check if the content is locked by the current user
            var currentUser = AccessProvider.Current.GetOriginalUser();

            if (node.LockedById != currentUser.Id)
                throw new SenseNetSecurityException(contentId, "It is only allowed to upload a binary chunk if the content is locked by the current user.");

            //check the destination property type
            propertyType = node.PropertyTypes[fieldName];
            if (propertyType == null || propertyType.DataType != DataType.Binary)
                throw new InvalidOperationException("Binary property not found with the name: " + fieldName);
        }

        ////TODO: Not used (de meg kellhet)
        //private static bool BinaryEquals(Stream binary1, Stream binary2)
        //{
        //    if (binary1 == null && binary2 == null)
        //        return true;
        //    else if (binary1 == null)
        //        return false;
        //    else if (binary2 == null)
        //        return false;
        //    else
        //    {
        //        if (binary1.Length != binary2.Length)
        //            return false;

        //        long pos1, pos2;
        //        pos1 = binary1.Position;
        //        pos2 = binary2.Position;
        //        binary1.Seek(0, SeekOrigin.Begin);
        //        binary2.Seek(0, SeekOrigin.Begin);
        //        bool ret = true;
        //        int i1, i2;
        //        while ((i1 = binary1.ReadByte()) > -1)
        //        {
        //            i2 = binary2.ReadByte();
        //            if (i1 != i2)
        //            {
        //                ret = false;
        //                break;
        //            }
        //        }
        //        binary1.Seek(pos1, SeekOrigin.Begin);
        //        binary2.Seek(pos2, SeekOrigin.Begin);
        //        return ret;
        //    }
        //}
	}
}