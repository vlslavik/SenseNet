using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using SenseNet.ContentRepository.Storage.Schema;
using System.Globalization;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    public class SqlNodeWriter : INodeWriter
    {
        FlatPropertyWriter _flatWriter;

        public void Open()
        {
            //
        }
        public void Close()
        {
            if (_flatWriter != null)
                _flatWriter.Execute();
        }

        private const string DELETE_AND_INSERT_BINARY_PROPERTY =
            @"
    DELETE
		BinaryProperties
	WHERE
		VersionId = @VersionId
		AND
		PropertyTypeId = @PropertyTypeId

" + INSERT_BINARY_PROPERTY;

        private const string DELETE_AND_INSERT_BINARY_PROPERTY_FILESTREAM =
            @"
    DELETE
		BinaryProperties
	WHERE
		VersionId = @VersionId
		AND
		PropertyTypeId = @PropertyTypeId

" + INSERT_BINARY_PROPERTY_FILESTREAM;

        private const string INSERT_BINARY_PROPERTY = @"
		
		  INSERT INTO BinaryProperties(
						  VersionId,
						  PropertyTypeId,
						  ContentType,
						  FileNameWithoutExtension,
						  Extension,
						  [Size],
						  [Checksum],
						  Stream)
		   VALUES (		  @VersionId,
						  @PropertyTypeId,
						  @ContentType,
						  @FileNameWithoutExtension,
						  @Extension,
						  @Size,
						  CASE @Size WHEN 0 THEN NULL ELSE @Checksum END,
						  CASE @Size WHEN 0 THEN NULL ELSE @Value END)
    DECLARE @NewId int;
    SET @NewId = @@IDENTITY;

	SELECT @NewId, Timestamp
    FROM BinaryProperties WHERE [BinaryPropertyId] = @NewId
";

        private const string INSERT_BINARY_PROPERTY_FILESTREAM = @"
		
		  INSERT INTO BinaryProperties(
						  VersionId,
						  PropertyTypeId,
						  ContentType,
						  FileNameWithoutExtension,
						  Extension,
						  [Size],
						  [Checksum],
						  Stream,
                          [FileStream])
		   VALUES (		  @VersionId,
						  @PropertyTypeId,
						  @ContentType,
						  @FileNameWithoutExtension,
						  @Extension,
						  @Size,
						  CASE @Size WHEN 0 THEN NULL ELSE @Checksum END,
						  CASE @Size WHEN 0 THEN NULL ELSE @Value END,
                          CASE @Size WHEN 0 THEN NULL ELSE (0x) END)
    
    DECLARE @NewId int;
    SET @NewId = @@IDENTITY;

	SELECT @NewId, FileStream.PathName(), GET_FILESTREAM_TRANSACTION_CONTEXT(), Timestamp
    FROM BinaryProperties WHERE [BinaryPropertyId] = @NewId
";

        private const string UPDATE_BINARY_PROPERTY_FILESTREAM = @"
    UPDATE BinaryProperties
	SET Stream = NULL
	WHERE BinaryPropertyId = @Id;

    SELECT FileStream.PathName(), GET_FILESTREAM_TRANSACTION_CONTEXT() 
    FROM BinaryProperties WHERE [BinaryPropertyId] = @Id
";
        //============================================================================ "less roundtrip methods"

        public void InsertNodeAndVersionRows(NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            using (var cmd = new SqlProcedure { CommandText = "proc_NodeAndVersion_Insert" })
            {
                cmd.Parameters.Add("@NodeTypeId", SqlDbType.Int).Value = nodeData.NodeTypeId;
                cmd.Parameters.Add("@ContentListTypeId", SqlDbType.Int).Value = (nodeData.ContentListTypeId != 0) ? (object)nodeData.ContentListTypeId : DBNull.Value;
                cmd.Parameters.Add("@ContentListId", SqlDbType.Int).Value = (nodeData.ContentListId != 0) ? (object)nodeData.ContentListId : DBNull.Value;
                cmd.Parameters.Add("@CreatingInProgress", SqlDbType.TinyInt).Value = nodeData.CreatingInProgress;
                cmd.Parameters.Add("@IsDeleted", SqlDbType.TinyInt).Value = nodeData.IsDeleted ? 1 : 0;
                cmd.Parameters.Add("@IsInherited", SqlDbType.TinyInt).Value = nodeData.IsInherited ? 1 : 0;
                cmd.Parameters.Add("@ParentNodeId", SqlDbType.Int).Value = (nodeData.ParentId > 0) ? (object)nodeData.ParentId : DBNull.Value;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 450).Value = nodeData.Name;
                cmd.Parameters.Add("@DisplayName", SqlDbType.NVarChar, 450).Value = (object)nodeData.DisplayName ?? DBNull.Value;
                cmd.Parameters.Add("@Path", SqlDbType.NVarChar, 450).Value = nodeData.Path;
                cmd.Parameters.Add("@Index", SqlDbType.Int).Value = nodeData.Index;
                cmd.Parameters.Add("@Locked", SqlDbType.TinyInt).Value = nodeData.Locked ? 1 : 0;
                cmd.Parameters.Add("@LockedById", SqlDbType.Int).Value = (nodeData.LockedById > 0) ? (object)nodeData.LockedById : DBNull.Value;
                cmd.Parameters.Add("@ETag", SqlDbType.VarChar, 50).Value = nodeData.ETag ?? String.Empty;
                cmd.Parameters.Add("@LockType", SqlDbType.Int).Value = nodeData.LockType;
                cmd.Parameters.Add("@LockTimeout", SqlDbType.Int).Value = nodeData.LockTimeout;
                cmd.Parameters.Add("@LockDate", SqlDbType.DateTime).Value = nodeData.LockDate;
                cmd.Parameters.Add("@LockToken", SqlDbType.VarChar, 50).Value = nodeData.LockToken ?? String.Empty;
                cmd.Parameters.Add("@LastLockUpdate", SqlDbType.DateTime).Value = nodeData.LastLockUpdate;
                cmd.Parameters.Add("@NodeCreationDate", SqlDbType.DateTime).Value = nodeData.CreationDate;
                cmd.Parameters.Add("@NodeCreatedById", SqlDbType.Int).Value = nodeData.CreatedById;
                cmd.Parameters.Add("@NodeModificationDate", SqlDbType.DateTime).Value = nodeData.ModificationDate;
                cmd.Parameters.Add("@NodeModifiedById", SqlDbType.Int).Value = nodeData.ModifiedById;

                cmd.Parameters.Add("@IsSystem", SqlDbType.TinyInt).Value = nodeData.IsSystem ? 1 : 0;
                cmd.Parameters.Add("@ClosestSecurityNodeId", SqlDbType.Int).Value = nodeData.ClosestSecurityNodeId;
                cmd.Parameters.Add("@SavingState", SqlDbType.Int).Value = (int)nodeData.SavingState;
                cmd.Parameters.Add("@ChangedData", SqlDbType.NText).Value = JsonConvert.SerializeObject(nodeData.ChangedData);

                cmd.Parameters.Add("@MajorNumber", SqlDbType.SmallInt).Value = nodeData.Version.Major;
                cmd.Parameters.Add("@MinorNumber", SqlDbType.SmallInt).Value = nodeData.Version.Minor;
                cmd.Parameters.Add("@Status", SqlDbType.SmallInt).Value = nodeData.Version.Status;
                cmd.Parameters.Add("@CreationDate", SqlDbType.DateTime).Value = nodeData.VersionCreationDate;
                cmd.Parameters.Add("@CreatedById", SqlDbType.Int).Value = nodeData.VersionCreatedById;
                cmd.Parameters.Add("@ModificationDate", SqlDbType.DateTime).Value = nodeData.VersionModificationDate;
                cmd.Parameters.Add("@ModifiedById", SqlDbType.Int).Value = nodeData.VersionModifiedById;

                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    nodeData.Id = Convert.ToInt32(reader[0]);
                    nodeData.VersionId = Convert.ToInt32(reader[1]);
                    nodeData.NodeTimestamp = SqlProvider.GetLongFromBytes((byte[])reader[2]);
                    nodeData.VersionTimestamp = SqlProvider.GetLongFromBytes((byte[])reader[3]);

                    lastMajorVersionId = reader.GetSafeInt32(4);
                    lastMinorVersionId = reader.GetSafeInt32(5);
                }
            }
        }

        /*============================================================================ Node Insert/Update */

        public void UpdateSubTreePath(string oldPath, string newPath)
        {
            if (oldPath == null)
                throw new ArgumentNullException("oldPath");
            if (newPath == null)
                throw new ArgumentNullException("newPath");

            if (oldPath.Length == 0)
                throw new ArgumentException("Old path cannot be empty.", "oldPath");
            if (newPath.Length == 0)
                throw new ArgumentException("New path cannot be empty.", "newPath");

            SqlProcedure cmd = null;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Node_UpdateSubTreePath" };
                cmd.Parameters.Add("@OldPath", SqlDbType.NVarChar, 450).Value = oldPath;
                cmd.Parameters.Add("@NewPath", SqlDbType.NVarChar, 450).Value = newPath;
                cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd.Dispose();
            }
        }
        public void UpdateNodeRow(NodeData nodeData)
        {
            SqlProcedure cmd = null;
            SqlDataReader reader = null;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Node_Update" };
                cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeData.Id;
                cmd.Parameters.Add("@NodeTypeId", SqlDbType.Int).Value = nodeData.NodeTypeId;
                cmd.Parameters.Add("@ContentListTypeId", SqlDbType.Int).Value = (nodeData.ContentListTypeId != 0) ? (object)nodeData.ContentListTypeId : DBNull.Value;
                cmd.Parameters.Add("@ContentListId", SqlDbType.Int).Value = (nodeData.ContentListId != 0) ? (object)nodeData.ContentListId : DBNull.Value;
                cmd.Parameters.Add("@CreatingInProgress", SqlDbType.TinyInt).Value = nodeData.CreatingInProgress ? 1 : 0;
                cmd.Parameters.Add("@IsDeleted", SqlDbType.TinyInt).Value = nodeData.IsDeleted ? 1 : 0;
                cmd.Parameters.Add("@IsInherited", SqlDbType.TinyInt).Value = nodeData.IsInherited ? 1 : 0;
                cmd.Parameters.Add("@ParentNodeId", SqlDbType.Int).Value = (nodeData.ParentId > 0) ? (object)nodeData.ParentId : DBNull.Value;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 450).Value = nodeData.Name;
                cmd.Parameters.Add("@DisplayName", SqlDbType.NVarChar, 450).Value = (object)nodeData.DisplayName ?? DBNull.Value;
                cmd.Parameters.Add("@Path", SqlDbType.NVarChar, 450).Value = nodeData.Path;
                cmd.Parameters.Add("@Index", SqlDbType.Int).Value = nodeData.Index;
                cmd.Parameters.Add("@Locked", SqlDbType.TinyInt).Value = nodeData.Locked ? 1 : 0;
                cmd.Parameters.Add("@LockedById", SqlDbType.Int).Value = (nodeData.LockedById > 0) ? (object)nodeData.LockedById : DBNull.Value;
                cmd.Parameters.Add("@ETag", SqlDbType.VarChar, 50).Value = nodeData.ETag ?? String.Empty;
                cmd.Parameters.Add("@LockType", SqlDbType.Int).Value = nodeData.LockType;
                cmd.Parameters.Add("@LockTimeout", SqlDbType.Int).Value = nodeData.LockTimeout;
                cmd.Parameters.Add("@LockDate", SqlDbType.DateTime).Value = nodeData.LockDate;
                cmd.Parameters.Add("@LockToken", SqlDbType.VarChar, 50).Value = nodeData.LockToken ?? String.Empty;
                cmd.Parameters.Add("@LastLockUpdate", SqlDbType.DateTime).Value = nodeData.LastLockUpdate;

                cmd.Parameters.Add("@IsSystem", SqlDbType.TinyInt).Value = nodeData.IsSystem ? 1 : 0;
                cmd.Parameters.Add("@ClosestSecurityNodeId", SqlDbType.Int).Value = nodeData.ClosestSecurityNodeId;
                cmd.Parameters.Add("@SavingState", SqlDbType.Int).Value = (int)nodeData.SavingState;
                //cmd.Parameters.Add("@ChangedData", SqlDbType.NText).Value = JsonConvert.SerializeObject(nodeData.ChangedData);

                cmd.Parameters.Add("@CreationDate", SqlDbType.DateTime).Value = nodeData.CreationDate;
                cmd.Parameters.Add("@CreatedById", SqlDbType.Int).Value = nodeData.CreatedById;
                cmd.Parameters.Add("@ModificationDate", SqlDbType.DateTime).Value = nodeData.ModificationDate;
                cmd.Parameters.Add("@ModifiedById", SqlDbType.Int).Value = nodeData.ModifiedById;
                cmd.Parameters.Add("@NodeTimestamp", SqlDbType.Timestamp).Value = SqlProvider.GetBytesFromLong(nodeData.NodeTimestamp);

                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    // SELECT [Timestamp] FROM Nodes WHERE NodeId = @NodeId
                    nodeData.NodeTimestamp = SqlProvider.GetLongFromBytes((byte[])reader[0]);
                }
            }
            catch (SqlException sex) //rethrow
            {
                if(sex.Message.StartsWith("Node is out of date"))
                    throw new NodeIsOutOfDateException(nodeData.Id, nodeData.Path, nodeData.VersionId, nodeData.Version, sex, nodeData.NodeTimestamp);
                throw new DataException(sex.Message, sex);
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
                cmd.Dispose();
            }
        }

        /*============================================================================ Version Insert/Update */

        //public int InsertVersionRow(NodeData nodeData)
        //{
        //    SqlProcedure cmd = null;
        //    SqlDataReader reader = null;
        //    int result = 0;
        //    try
        //    {
        //        cmd = new SqlProcedure { CommandText = "proc_Version_Insert" };
        //        cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeData.Id;
        //        cmd.Parameters.Add("@MajorNumber", SqlDbType.SmallInt).Value = nodeData.Version.Major;
        //        cmd.Parameters.Add("@MinorNumber", SqlDbType.SmallInt).Value = nodeData.Version.Minor;
        //        cmd.Parameters.Add("@Status", SqlDbType.SmallInt).Value = nodeData.Version.Status;
        //        cmd.Parameters.Add("@CreationDate", SqlDbType.DateTime).Value = nodeData.CreationDate;
        //        cmd.Parameters.Add("@CreatedById", SqlDbType.Int).Value = nodeData.CreatedById;
        //        cmd.Parameters.Add("@ModificationDate", SqlDbType.DateTime).Value = nodeData.ModificationDate;
        //        cmd.Parameters.Add("@ModifiedById", SqlDbType.Int).Value = nodeData.ModifiedById;

        //        reader = cmd.ExecuteReader();
        //        while (reader.Read())
        //        {
        //            // SELECT VersionId, [Timestamp] FROM Versions WHERE VersionId = @@IDENTITY
        //            result = Convert.ToInt32(reader[0]);
        //            nodeData.VersionTimestamp = SqlProvider.GetLongFromBytes((byte[])reader[1]);
        //        }
        //    }
        //    finally
        //    {
        //        if (reader != null && !reader.IsClosed)
        //            reader.Close();
        //        cmd.Dispose();
        //    }
        //    return result;
        //}
        public void UpdateVersionRow(NodeData nodeData, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            SqlProcedure cmd = null;
            SqlDataReader reader = null;

            lastMajorVersionId = 0;
            lastMinorVersionId = 0;

            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Version_Update" };
                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = nodeData.VersionId;
                cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeData.Id;
                if (nodeData.IsPropertyChanged("Version"))
                {
                    cmd.Parameters.Add("@MajorNumber", SqlDbType.SmallInt).Value = nodeData.Version.Major;
                    cmd.Parameters.Add("@MinorNumber", SqlDbType.SmallInt).Value = nodeData.Version.Minor;
                    cmd.Parameters.Add("@Status", SqlDbType.SmallInt).Value = nodeData.Version.Status;
                }
                cmd.Parameters.Add("@CreationDate", SqlDbType.DateTime).Value = nodeData.VersionCreationDate;
                cmd.Parameters.Add("@CreatedById", SqlDbType.Int).Value = nodeData.VersionCreatedById;
                cmd.Parameters.Add("@ModificationDate", SqlDbType.DateTime).Value = nodeData.VersionModificationDate;
                cmd.Parameters.Add("@ModifiedById", SqlDbType.Int).Value = nodeData.VersionModifiedById;
                cmd.Parameters.Add("@ChangedData", SqlDbType.NText).Value = JsonConvert.SerializeObject(nodeData.ChangedData);

                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    // SELECT [Timestamp] FROM Versions WHERE VersionId = @VersionId
                    nodeData.NodeTimestamp = SqlProvider.GetLongFromBytes((byte[])reader[0]);
                    nodeData.VersionTimestamp = SqlProvider.GetLongFromBytes((byte[])reader[1]);

                    lastMajorVersionId = reader.GetSafeInt32(2);
                    lastMinorVersionId = reader.GetSafeInt32(3);
                }
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
                cmd.Dispose();
            }
        }
        public void CopyAndUpdateVersion(NodeData nodeData, int previousVersionId, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            CopyAndUpdateVersion(nodeData, previousVersionId, 0, out lastMajorVersionId, out lastMinorVersionId);
        }
        public void CopyAndUpdateVersion(NodeData nodeData, int previousVersionId, int destinationVersionId, out int lastMajorVersionId, out int lastMinorVersionId)
        {
            SqlProcedure cmd = null;
            SqlDataReader reader = null;
            lastMajorVersionId = 0;
            lastMinorVersionId = 0;

            try
            {
                cmd = new SqlProcedure { CommandText = "proc_Version_CopyAndUpdate" };
                cmd.Parameters.Add("@PreviousVersionId", SqlDbType.Int).Value = previousVersionId;
                cmd.Parameters.Add("@DestinationVersionId", SqlDbType.Int).Value = (destinationVersionId != 0) ? (object)destinationVersionId : DBNull.Value;
                cmd.Parameters.Add("@NodeId", SqlDbType.Int).Value = nodeData.Id;
                cmd.Parameters.Add("@MajorNumber", SqlDbType.SmallInt).Value = nodeData.Version.Major;
                cmd.Parameters.Add("@MinorNumber", SqlDbType.SmallInt).Value = nodeData.Version.Minor;
                cmd.Parameters.Add("@Status", SqlDbType.SmallInt).Value = nodeData.Version.Status;
                cmd.Parameters.Add("@CreationDate", SqlDbType.DateTime).Value = nodeData.VersionCreationDate;
                cmd.Parameters.Add("@CreatedById", SqlDbType.Int).Value = nodeData.VersionCreatedById;
                cmd.Parameters.Add("@ModificationDate", SqlDbType.DateTime).Value = nodeData.VersionModificationDate;
                cmd.Parameters.Add("@ModifiedById", SqlDbType.Int).Value = nodeData.VersionModifiedById;
                cmd.Parameters.Add("@ChangedData", SqlDbType.NText).Value = JsonConvert.SerializeObject(nodeData.ChangedData);

                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    // SELECT VersionId, [Timestamp] FROM Versions WHERE VersionId = @NewVersionId
                    nodeData.VersionId = Convert.ToInt32(reader[0]);
                    nodeData.NodeTimestamp = SqlProvider.GetLongFromBytes((byte[])reader[1]);
                    nodeData.VersionTimestamp = SqlProvider.GetLongFromBytes((byte[])reader[2]);

                    lastMajorVersionId = reader.GetSafeInt32(3);
                    lastMinorVersionId = reader.GetSafeInt32(4);
                }
                if (reader.NextResult())
                {
                    // SELECT BinaryPropertyId, PropertyTypeId FROM BinaryProperties WHERE VersionId = @NewVersionId
                    while (reader.Read())
                    {
                        var binId = Convert.ToInt32(reader[0]);
                        var propId = Convert.ToInt32(reader[1]);
                        var binaryData = (BinaryDataValue)nodeData.GetDynamicRawData(propId);
                        binaryData.Id = binId;
                    }
                }

            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();

                cmd.Dispose();
            }
        }

        //============================================================================ Property Insert/Update

        public void SaveStringProperty(int versionId, PropertyType propertyType, string value)
        {
            if (_flatWriter == null)
                _flatWriter = new FlatPropertyWriter(versionId);

            _flatWriter.WriteStringProperty(value, propertyType);
        }
        public void SaveDateTimeProperty(int versionId, PropertyType propertyType, DateTime value)
        {
            if (_flatWriter == null)
                _flatWriter = new FlatPropertyWriter(versionId);

            _flatWriter.WriteDateTimeProperty(value, propertyType);
        }
        public void SaveIntProperty(int versionId, PropertyType propertyType, int value)
        {
            if (_flatWriter == null)
                _flatWriter = new FlatPropertyWriter(versionId);

            _flatWriter.WriteIntProperty(value, propertyType);
        }
        public void SaveCurrencyProperty(int versionId, PropertyType propertyType, decimal value)
        {
            if (_flatWriter == null)
                _flatWriter = new FlatPropertyWriter(versionId);

            _flatWriter.WriteCurrencyProperty(value, propertyType);
        }
        public void SaveTextProperty(int versionId, PropertyType propertyType, bool isLoaded, string value)
        {
            //if (propertyType.Name == "TextExtract" && !isLoaded)
            //    return;

            if (propertyType == null)
                throw new ArgumentNullException("propertyType");
            if (!isLoaded)
                throw new ApplicationException(); // There is no other data that could be 'IsModified'...

            SqlProcedure cmd = null;
            // Delete existing values (otherwise have to check which table used and then switch between
            // the Insert or Update of the specific table. This way only insert is necessary.)
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_TextProperty_Delete" };
                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyType.Id;
                cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd.Dispose();
            }

            if (value == null)
                return;

            if (value.Length > SqlProvider.TextAlternationSizeLimit)
            {
                // NText
                try
                {
                    cmd = new SqlProcedure { CommandText = "proc_TextProperty_InsertNText" };
                    cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                    cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyType.Id;
                    cmd.Parameters.Add("@Value", SqlDbType.NText).Value = value;
                    cmd.ExecuteNonQuery();
                }
                finally
                {
                    cmd.Dispose();
                }
            }
            else
            {
                // NVarchar
                try
                {
                    cmd = new SqlProcedure { CommandText = "proc_TextProperty_InsertNVarchar" };
                    cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                    cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyType.Id;
                    cmd.Parameters.Add("@Value", SqlDbType.NVarChar, SqlProvider.TextAlternationSizeLimit).Value = value == null ? (object)DBNull.Value : (object)value; ;
                    cmd.ExecuteNonQuery();
                }
                finally
                {
                    cmd.Dispose();
                }
            }
        }
        public void SaveReferenceProperty(int versionId, PropertyType propertyType, IEnumerable<int> value)
        {
            if (propertyType == null)
                throw new ArgumentNullException("propertyType");

            for (short tryAgain = 3; tryAgain > 0; tryAgain --)
            {
                // Optimistic approach: try to save the value as is, without checking and compensate if it fails
                try
                {
                    // Create XML
                    var referredListXml = SqlProvider.CreateIdXmlForReferencePropertyUpdate(value);

                    // Execute SQL
                    using (var cmd = new SqlProcedure { CommandText = "proc_ReferenceProperty_Update" })
                    {
                        cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                        cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyType.Id;
                        cmd.Parameters.Add("@ReferredNodeIdListXml", SqlDbType.Xml).Value = referredListXml;
                        cmd.ExecuteNonQuery();

                        // Success, don't try again
                        tryAgain = 0;
                    }
                }
                catch (SqlException exc)
                {
                    // This was the last try and it failed, throw
                    if (tryAgain == 1)
                        throw;

                    // The value contains a node ID which no longer exists in the database, let's compensate for that
                    if (exc.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint \"FK_ReferenceProperties_Nodes\"."))
                    {
                        // Get node heads for the IDs
                        var heads = DataProvider.Current.LoadNodeHeads(value);
                        // Select the IDs of the existing node heads
                        value = heads.Where(h => h != null).Select(h => h.Id);
                    }
                    else
                        // If the error is something else, just throw it up
                        throw;
                }
            }
        }

        public int InsertBinaryProperty(int versionId, int propertyTypeId, BinaryDataValue value, bool isNewNode)
        {
            SqlProcedure cmd = null;
            var id = 0;

            try
            {
                var streamSize = value.Stream != null ? Convert.ToInt32(value.Stream.Length) : 0;
                var useFileStream = RepositoryConfiguration.FileStreamEnabled &&
                                    streamSize > RepositoryConfiguration.MinimumSizeForFileStreamInBytes;

                cmd = useFileStream
                    ? new SqlProcedure { CommandText = (isNewNode ? INSERT_BINARY_PROPERTY_FILESTREAM : DELETE_AND_INSERT_BINARY_PROPERTY_FILESTREAM), CommandType = CommandType.Text } 
                    : new SqlProcedure { CommandText = (isNewNode ? INSERT_BINARY_PROPERTY : DELETE_AND_INSERT_BINARY_PROPERTY), CommandType = CommandType.Text };

                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = (versionId != 0) ? (object)versionId : DBNull.Value;
                cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = (propertyTypeId != 0) ? (object)propertyTypeId : DBNull.Value;
                cmd.Parameters.Add("@ContentType", SqlDbType.NVarChar, 450).Value = value.ContentType;
                cmd.Parameters.Add("@FileNameWithoutExtension", SqlDbType.NVarChar, 450).Value = value.FileName.FileNameWithoutExtension == null ? DBNull.Value : (object)value.FileName.FileNameWithoutExtension;
                cmd.Parameters.Add("@Extension", SqlDbType.NVarChar, 50).Value = ValidateExtension(value.FileName.Extension);
                cmd.Parameters.Add("@Size", SqlDbType.BigInt).Value = Math.Max(0, value.Size);
                cmd.Parameters.Add("@Checksum", SqlDbType.VarChar, 200).Value = (value.Checksum != null) ? (object)value.Checksum : DBNull.Value;

                if (value.Stream != null && value.Stream.Length > 0)
                {
                    value.Stream.Seek(0, SeekOrigin.Begin);

                    //use Filstream if it is enabled and the size is big enough
                    if (useFileStream)
                    {
                        //set old binary to NULL
                        cmd.Parameters.Add(new SqlParameter("@Value", SqlDbType.VarBinary)).Value = DBNull.Value;

                        string path;
                        byte[] transactionContext;

                        //insert binary row and retrieve file path and transaction context for the Filestream column
                        using (var reader = cmd.ExecuteReader())
                        {
                            reader.Read();

                            id = Convert.ToInt32(reader[0]);

                            path = reader.GetString(1);
                            transactionContext = reader.GetSqlBytes(2).Buffer;
                            value.Timestamp = DataProvider.GetLongFromBytes((byte[]) reader.GetValue(3));
                        }

                        //Write the data using SqlFileStream
                        using (var fs = new SqlFileStream(path, transactionContext, FileAccess.Write))
                        {
                            //default buffer size is 4096
                            value.Stream.CopyTo(fs);
                        }
                    }
                    else
                    {
                        if (value.Stream.Length > Int32.MaxValue)
                            throw new NotSupportedException(); // MS-SQL does not support stream size over [Int32.MaxValue]

                        //read the whole data the old way
                        var buffer = new byte[streamSize];
                        value.Stream.Read(buffer, 0, streamSize);

                        cmd.Parameters.Add(new SqlParameter("@Value", SqlDbType.VarBinary)).Value = buffer;
                    }
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@Value", SqlDbType.VarBinary)).Value = DBNull.Value;
                }

                //if Filestream is enabled but was not used due to the small file size: set it to null
                if (RepositoryConfiguration.FileStreamEnabled && !useFileStream)
                    cmd.Parameters.Add(new SqlParameter("@FileStream", SqlDbType.VarBinary)).Value = DBNull.Value;

                //If Filestream was not involved, execute the command 
                //and get the new id the old way from the db.
                if (!useFileStream)
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();

                        id = Convert.ToInt32(reader[0]);
                        value.Timestamp = DataProvider.GetLongFromBytes((byte[])reader.GetValue(1));
                    }
                }
            }
            finally
            {
                if (cmd != null)
                    cmd.Dispose();
            }

            return id;
        }

        public void UpdateBinaryProperty(int binaryDataId, BinaryDataValue value)
        {
            if (!RepositoryConfiguration.FileStreamEnabled)
            {
                // MS-SQL does not support stream size over [Int32.MaxValue],
                // but check only if Filestream is not enabled
                if (value.Stream != null && value.Stream.Length > Int32.MaxValue)
                    throw new NotSupportedException(); 
            }

            var isRepositoryStream = value.Stream is RepositoryStream || value.Stream is SenseNetSqlFileStream;
            FileStreamData fileStreamData = null;

            SqlProcedure cmd = null;

            try
            {
                cmd = new SqlProcedure { CommandText = "proc_BinaryProperty_Update" };
                cmd.Parameters.Add("@BinaryPropertyId", SqlDbType.Int).Value = binaryDataId;
                cmd.Parameters.Add("@ContentType", SqlDbType.NVarChar, 450).Value = value.ContentType;
                cmd.Parameters.Add("@FileNameWithoutExtension", SqlDbType.NVarChar, 450).Value = value.FileName.FileNameWithoutExtension == null ? (object)DBNull.Value : (object)value.FileName.FileNameWithoutExtension;
                cmd.Parameters.Add("@Extension", SqlDbType.NVarChar, 50).Value = ValidateExtension(value.FileName.Extension);
                cmd.Parameters.Add("@Size", SqlDbType.BigInt).Value = value.Size;
                
                // Do not update the stream field in the database if it is not loaded (other change happened)
                cmd.Parameters.Add("@IsStreamModified", SqlDbType.TinyInt).Value = isRepositoryStream ? 0 : 1;
                cmd.Parameters.Add("@Checksum", SqlDbType.VarChar, 200).Value = (value.Checksum != null) ? (object)value.Checksum : DBNull.Value;

                if (RepositoryConfiguration.FileStreamEnabled)
                {
                    string path;
                    byte[] transactionContext;

                    //Update row and retrieve file path and 
                    //transaction context for the Filestream column
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();

                        path = reader.GetString(0);
                        transactionContext = reader.GetSqlBytes(1).Buffer;
                    }

                    if (!string.IsNullOrEmpty(path))
                        fileStreamData = new FileStreamData { Path = path, TransactionContext = transactionContext };
                }
                else
                {
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (cmd != null)
                    cmd.Dispose();
            }

            if (!isRepositoryStream && value.Stream != null && value.Stream.Length > 0)
            {
                // Stream exists and is loaded -> write it
                WriteBinaryStream(value.Stream, binaryDataId, fileStreamData);
            }
        }
        public void DeleteBinaryProperty(int versionId, PropertyType propertyType)
        {
            if (propertyType == null)
                throw new ArgumentNullException("propertyType");
            SqlProcedure cmd = null;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_BinaryProperty_Delete" };
                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                cmd.Parameters.Add("@PropertyTypeId", SqlDbType.Int).Value = propertyType.Id;
                cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd.Dispose();
            }
        }
        internal static string ValidateExtension(string originalExtension)
        {
            return (originalExtension.Length == 0)
                ? string.Empty
                : string.Concat(".", originalExtension);
        }
        private static void WriteBinaryStream(Stream stream, int binaryPropertyId, FileStreamData fileStreamData = null)
        {
            SqlProcedure cmd = null;
            try
            {
                var longStreamSize = stream.Length;
                var useFileStream = RepositoryConfiguration.FileStreamEnabled &&
                                    longStreamSize > RepositoryConfiguration.MinimumSizeForFileStreamInBytes;

                //If possible, write the stream using the special Filestream technology
                if (useFileStream)
                {
                    WriteSqlFileStream(stream, binaryPropertyId, fileStreamData);
                    return;
                }

                //We have to work with an integer since SQL does not support
                //binary values bigger than [Int32.MaxValue].
                var streamSize = Convert.ToInt32(stream.Length);

                cmd = new SqlProcedure { CommandText = "proc_BinaryProperty_WriteStream" };
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = binaryPropertyId;

                var offsetParameter = cmd.Parameters.Add("@Offset", SqlDbType.Int);
                var valueParameter = cmd.Parameters.Add("@Value", SqlDbType.VarBinary, streamSize);

                if (RepositoryConfiguration.FileStreamEnabled)
                {
                    var useFileStreamParameter = cmd.Parameters.Add("@UseFileStream", SqlDbType.TinyInt);
                    useFileStreamParameter.Value = useFileStream;
                }

                int offset = 0;
                byte[] buffer = null;
                stream.Seek(0, SeekOrigin.Begin);

                //The 'while' loop is misleading here, because we write the whole
                //stream at once. Bigger files should go to the Filestream
                //column anyway.
                while (offset < streamSize)
                {
                    // Buffer size may be less at the end os the stream than the limit
                    int bufferSize = streamSize - offset;

                    if (buffer == null || buffer.Length != bufferSize)
                        buffer = new byte[bufferSize];

                    // Read bytes from the source
                    stream.Read(buffer, 0, bufferSize);

                    offsetParameter.Value = offset;
                    valueParameter.Value = buffer;

                    // Write full stream
                    cmd.ExecuteNonQuery();

                    offset += bufferSize;
                }
            }
            finally
            {
                if (cmd != null)
                    cmd.Dispose();
            }
        }

        private static void WriteSqlFileStream(Stream stream, int binaryPropertyId, FileStreamData fileStreamData = null)
        {
            SqlProcedure cmd = null;

            try
            {
                //If we did not receive a path and transaction context, retrieve it now from the database.
                if (fileStreamData == null)
                {
                    cmd = new SqlProcedure { CommandText = UPDATE_BINARY_PROPERTY_FILESTREAM, CommandType = CommandType.Text };
                    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = binaryPropertyId;

                    string path;
                    byte[] transactionContext;

                    //Set Stream column to NULL and retrieve file path and 
                    //transaction context for the Filestream column
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();

                        path = reader.GetString(0);
                        transactionContext = reader.GetSqlBytes(1).Buffer;
                    } 

                    fileStreamData = new FileStreamData { Path = path, TransactionContext = transactionContext };
                }
                
                stream.Seek(0, SeekOrigin.Begin);

                //Write data using SqlFileStream
                using (var fs = new SqlFileStream(fileStreamData.Path, fileStreamData.TransactionContext, FileAccess.Write))
                {
                    //default buffer size is 4096
                    stream.CopyTo(fs);
                }
            }
            finally
            {
                if (cmd != null)
                    cmd.Dispose();
            }
        }
    }
}
