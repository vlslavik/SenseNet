/**************************************************************/
/**************** Enable FILESTREAM on database ***************/
/**************************************************************/

-- set access level
EXEC sp_configure filestream_access_level, 2
RECONFIGURE

DECLARE @errorHappened tinyint;
DECLARE @Db_Data_Path	nvarchar(1000);
DECLARE @Db_Name	nvarchar(200);

SET @errorHappened = 0;

-- get current database name
SELECT @Db_Name = DB_NAME();

-- find physical path of the database and create a folder there
SELECT @Db_Data_Path =   
(  
    SELECT  LEFT(physical_name, LEN(physical_name) - CHARINDEX('\',REVERSE(physical_name)) + 1) + @Db_Name + 'Files'
    FROM sys.master_files mf  
    INNER JOIN sys.[databases] d  
        ON mf.[database_id] = d.[database_id]  
    WHERE d.[name] = @Db_Name AND type = 0  
)

BEGIN TRY
IF NOT EXISTS (SELECT * FROM sys.filegroups WHERE name = 'SNFileGroup')
	BEGIN
		DECLARE @sql			varchar(1000);
		SET @sql = N'
		ALTER DATABASE [' + @Db_Name + ']
		ADD FILEGROUP SNFileGroup CONTAINS FILESTREAM

		ALTER DATABASE [' + @Db_Name + '] 
		ADD FILE ( NAME = ''SenseNetContentRepository_files'', FILENAME = '''+ @Db_Data_Path + N''') TO FILEGROUP SNFileGroup

		PRINT(''FILEGROUP and FILE added to database.'');';

		EXEC(@sql);

	END
ELSE
	BEGIN
		PRINT('FILEGROUP already exists, filegroup and file creation skipped.');
	END
END TRY
BEGIN CATCH
	SET @errorHappened = 1;
	RAISERROR('FILEGROUP could not be created.', 1, 1);
	GOTO SKIPPED;
END CATCH

/**************************************************************/
/*** Add UNIQUE constraint to the binary table if necessary ***/
/**************************************************************/

DECLARE @GuidConstraint int;

SELECT @GuidConstraint = COUNT(CONSTRAINT_NAME)
FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE 
WHERE TABLE_NAME = 'BinaryProperties' AND COLUMN_NAME = 'RowGuid'

IF  (@GuidConstraint = 0)
BEGIN
	ALTER TABLE BinaryProperties
	ADD UNIQUE (RowGuid)

	PRINT('UNIQUE constraint added to RowGuid column.');
END
ELSE
BEGIN
	PRINT('RowGuid column already has a UNIQUE constraint.');
END


/**************************************************************/
/********* Add FILESTREAM column to the binary table **********/
/**************************************************************/

IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'FileStream' and Object_ID = Object_ID(N'BinaryProperties'))    
	BEGIN
		PRINT('FILESTREAM column already exists.');
	END
ELSE
	BEGIN
		--This needs to be a dynamic SQL, otherwise compilation would 
		--fail if the Filestream were already set and added to the table...
		execute ('ALTER TABLE dbo.BinaryProperties SET (FILESTREAM_ON = SNFileGroup)
		ALTER TABLE dbo.BinaryProperties ADD [FileStream] VARBINARY(MAX) FILESTREAM NULL');

		PRINT('FILESTREAM column added.');		
	END

/**************************************************************/
/******************** Drop stored procedures ******************/
/**************************************************************/

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_Update]') AND type in (N'P', N'PC')) ---OK
DROP PROCEDURE [dbo].[proc_BinaryProperty_Update]

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Version_CopyAndUpdate]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[proc_Version_CopyAndUpdate]

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_GetPointer]') AND type in (N'P', N'PC')) ---OK
DROP PROCEDURE [dbo].[proc_BinaryProperty_GetPointer]

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_ReadStream]') AND type in (N'P', N'PC')) ---OK
DROP PROCEDURE [dbo].[proc_BinaryProperty_ReadStream]

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_WriteStream]') AND type in (N'P', N'PC')) --OK
DROP PROCEDURE [dbo].[proc_BinaryProperty_WriteStream]

PRINT('Old stored procedures dropped.');

/**************************************************************/
/****************** Re-create stored procedures ***************/
/**************************************************************/

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_Update]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_BinaryProperty_Update]
(
	@BinaryPropertyId int,
	@ContentType nvarchar(450),
	@FileNameWithoutExtension nvarchar(450),
	@Extension nvarchar(50),
	@Size bigint,
	@IsStreamModified tinyint,
	@Checksum varchar(200)
)
AS
	IF(@IsStreamModified = 1)
		-- Stream modified
		BEGIN
			IF(@Size <= 0)
				BEGIN
				  UPDATE BinaryProperties
				  SET	ContentType = @ContentType,
						FileNameWithoutExtension = @FileNameWithoutExtension,
						Extension = @Extension,
						[Size] = @Size,
						[Checksum] = NULL,
						Stream = NULL,
						FileStream = NULL
				  WHERE BinaryPropertyId = @BinaryPropertyId
				END
			ELSE
				-- Stream not null, need a pointer for write
				BEGIN
				  UPDATE BinaryProperties
				  SET	ContentType = @ContentType,
						FileNameWithoutExtension = @FileNameWithoutExtension,
						Extension = @Extension,
						[Size] = @Size,
						[Checksum] = @Checksum,
						Stream = NULL,
						FileStream = CONVERT(varbinary, '''')
				  WHERE BinaryPropertyId = @BinaryPropertyId

				END
		END
	ELSE
		-- The stream itself is not modified (was not loaded at all), keep that field untouched
		BEGIN
				  UPDATE BinaryProperties
				  SET	ContentType = @ContentType,
						FileNameWithoutExtension = @FileNameWithoutExtension,
						Extension = @Extension,
						[Size] = @Size
				  WHERE BinaryPropertyId = @BinaryPropertyId
		END
		
		SELECT FileStream.PathName(), GET_FILESTREAM_TRANSACTION_CONTEXT() 
		FROM BinaryProperties WHERE [BinaryPropertyId] = @BinaryPropertyId' 
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_GetPointer]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_BinaryProperty_GetPointer]
(
	@VersionId int,
	@PropertyTypeId int,
	@Id int OUTPUT,
	@Length int OUTPUT,
	@TransactionContext varbinary(max) OUTPUT,
	@FilePath nvarchar(4000) OUTPUT
)
AS
	SELECT @Id = BinaryPropertyId,
		@Length = CASE WHEN bp.FileStream IS NULL
			            THEN DATALENGTH(Stream)
			            ELSE DATALENGTH(FileStream)
		          END,
		@TransactionContext = GET_FILESTREAM_TRANSACTION_CONTEXT(),
		@FilePath = FileStream.PathName()				  
	FROM BinaryProperties as bp
	WHERE VersionId = @VersionId 
	  AND PropertyTypeId = @PropertyTypeId
	  AND Staging IS NULL
' 
END


IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_ReadStream]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_BinaryProperty_ReadStream]
(
	@Id int,
	@Offset int,
	@Size int
)
AS
	SELECT 
		CASE WHEN FileStream IS NULL
							THEN SUBSTRING([Stream], @Offset, @Size)
							ELSE SUBSTRING([FileStream], @Offset, @Size)
						END
	FROM BinaryProperties
	WHERE BinaryPropertyId = @Id
' 
END



IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_BinaryProperty_WriteStream]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_BinaryProperty_WriteStream]
(
	@Id int,
	@Offset int,
	@Value varbinary(max),
	@UseFileStream tinyint
)
AS
  
  IF(@UseFileStream = 1)		
		BEGIN
			UPDATE BinaryProperties
			SET FileStream = @Value, Stream = NULL
			WHERE BinaryPropertyId = @Id;
		END
	ELSE
		BEGIN
			UPDATE BinaryProperties
			SET Stream = @Value, FileStream = NULL
			WHERE BinaryPropertyId = @Id;
		END
' 
END



IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[proc_Version_CopyAndUpdate]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[proc_Version_CopyAndUpdate] 
	@PreviousVersionId int,
	@DestinationVersionId int,
	@NodeId int,
	@MajorNumber smallint,
	@MinorNumber smallint,
	@CreationDate datetime,
	@CreatedById int,
	@ModificationDate datetime,
	@ModifiedById int,
	@Status smallint,
	@ChangedData ntext
AS
BEGIN
	DECLARE @NewVersionId int
	
	-- Before inserting set versioning status code from "Locked" to "Draft" on all older versions
	UPDATE Versions SET Status = 4 WHERE NodeId = @NodeId AND Status = 2

	IF @DestinationVersionId IS NULL
	BEGIN
		-- Insert version row
		INSERT INTO Versions
			( NodeId, MajorNumber, MinorNumber, CreationDate, CreatedById, ModificationDate, ModifiedById, Status)
			VALUES
			(@NodeId,@MajorNumber,@MinorNumber,@CreationDate,@CreatedById,@ModificationDate,@ModifiedById,@Status)
		SELECT @NewVersionId = @@IDENTITY
	END
	ELSE
	BEGIN
		-- Update existing version
		SET @NewVersionId = @DestinationVersionId;
		
		UPDATE Versions SET
			NodeId = @NodeId,
			MajorNumber = @MajorNumber,
			MinorNumber = @MinorNumber,
			CreationDate = @CreationDate,
			CreatedById = @CreatedById,
			ModificationDate = @ModificationDate,
			ModifiedById = @ModifiedById,
			Status = @Status,
			ChangedData =	@ChangedData
		WHERE VersionId = @NewVersionId
		
		-- Delete previous property values
		DELETE FROM BinaryProperties WHERE VersionId = @NewVersionId;
		DELETE FROM FlatProperties WHERE VersionId = @NewVersionId;
		DELETE FROM ReferenceProperties WHERE VersionId = @NewVersionId;
		DELETE FROM TextPropertiesNVarchar WHERE VersionId = @NewVersionId;
		DELETE FROM TextPropertiesNText WHERE VersionId = @NewVersionId;
	END	
	

	-- Copy properties
	INSERT INTO BinaryProperties
		([VersionId],[PropertyTypeId],[ContentType],[FileNameWithoutExtension],[Extension],[Size],[Checksum],[Stream],[FileStream],[CreationDate])
		SELECT @NewVersionId,[PropertyTypeId],[ContentType],[FileNameWithoutExtension],[Extension],[Size],[Checksum],[Stream],[FileStream],GETUTCDATE()
		FROM BinaryProperties WHERE VersionId = @PreviousVersionId AND Staging IS NULL
	INSERT INTO FlatProperties
		([VersionId],[Page]
			,[nvarchar_1],[nvarchar_2],[nvarchar_3],[nvarchar_4],[nvarchar_5],[nvarchar_6],[nvarchar_7],[nvarchar_8],[nvarchar_9],[nvarchar_10],[nvarchar_11],[nvarchar_12],[nvarchar_13],[nvarchar_14],[nvarchar_15],[nvarchar_16],[nvarchar_17],[nvarchar_18],[nvarchar_19],[nvarchar_20],[nvarchar_21],[nvarchar_22],[nvarchar_23],[nvarchar_24],[nvarchar_25],[nvarchar_26],[nvarchar_27],[nvarchar_28],[nvarchar_29],[nvarchar_30],[nvarchar_31],[nvarchar_32],[nvarchar_33],[nvarchar_34],[nvarchar_35],[nvarchar_36],[nvarchar_37],[nvarchar_38],[nvarchar_39],[nvarchar_40]
			,[nvarchar_41],[nvarchar_42],[nvarchar_43],[nvarchar_44],[nvarchar_45],[nvarchar_46],[nvarchar_47],[nvarchar_48],[nvarchar_49],[nvarchar_50],[nvarchar_51],[nvarchar_52],[nvarchar_53],[nvarchar_54],[nvarchar_55],[nvarchar_56],[nvarchar_57],[nvarchar_58],[nvarchar_59],[nvarchar_60],[nvarchar_61],[nvarchar_62],[nvarchar_63],[nvarchar_64],[nvarchar_65],[nvarchar_66],[nvarchar_67],[nvarchar_68],[nvarchar_69],[nvarchar_70],[nvarchar_71],[nvarchar_72],[nvarchar_73],[nvarchar_74],[nvarchar_75],[nvarchar_76],[nvarchar_77],[nvarchar_78],[nvarchar_79],[nvarchar_80]
			,[int_1],[int_2],[int_3],[int_4],[int_5],[int_6],[int_7],[int_8],[int_9],[int_10],[int_11],[int_12],[int_13],[int_14],[int_15],[int_16],[int_17],[int_18],[int_19],[int_20],[int_21],[int_22],[int_23],[int_24],[int_25],[int_26],[int_27],[int_28],[int_29],[int_30],[int_31],[int_32],[int_33],[int_34],[int_35],[int_36],[int_37],[int_38],[int_39],[int_40]
			,[datetime_1],[datetime_2],[datetime_3],[datetime_4],[datetime_5],[datetime_6],[datetime_7],[datetime_8],[datetime_9],[datetime_10],[datetime_11],[datetime_12],[datetime_13],[datetime_14],[datetime_15],[datetime_16],[datetime_17],[datetime_18],[datetime_19],[datetime_20],[datetime_21],[datetime_22],[datetime_23],[datetime_24],[datetime_25]
			,[money_1],[money_2],[money_3],[money_4],[money_5],[money_6],[money_7],[money_8],[money_9],[money_10],[money_11],[money_12],[money_13],[money_14],[money_15]
		)
		SELECT @NewVersionId,[Page]
			,[nvarchar_1],[nvarchar_2],[nvarchar_3],[nvarchar_4],[nvarchar_5],[nvarchar_6],[nvarchar_7],[nvarchar_8],[nvarchar_9],[nvarchar_10],[nvarchar_11],[nvarchar_12],[nvarchar_13],[nvarchar_14],[nvarchar_15],[nvarchar_16],[nvarchar_17],[nvarchar_18],[nvarchar_19],[nvarchar_20],[nvarchar_21],[nvarchar_22],[nvarchar_23],[nvarchar_24],[nvarchar_25],[nvarchar_26],[nvarchar_27],[nvarchar_28],[nvarchar_29],[nvarchar_30],[nvarchar_31],[nvarchar_32],[nvarchar_33],[nvarchar_34],[nvarchar_35],[nvarchar_36],[nvarchar_37],[nvarchar_38],[nvarchar_39],[nvarchar_40]
			,[nvarchar_41],[nvarchar_42],[nvarchar_43],[nvarchar_44],[nvarchar_45],[nvarchar_46],[nvarchar_47],[nvarchar_48],[nvarchar_49],[nvarchar_50],[nvarchar_51],[nvarchar_52],[nvarchar_53],[nvarchar_54],[nvarchar_55],[nvarchar_56],[nvarchar_57],[nvarchar_58],[nvarchar_59],[nvarchar_60],[nvarchar_61],[nvarchar_62],[nvarchar_63],[nvarchar_64],[nvarchar_65],[nvarchar_66],[nvarchar_67],[nvarchar_68],[nvarchar_69],[nvarchar_70],[nvarchar_71],[nvarchar_72],[nvarchar_73],[nvarchar_74],[nvarchar_75],[nvarchar_76],[nvarchar_77],[nvarchar_78],[nvarchar_79],[nvarchar_80]
			,[int_1],[int_2],[int_3],[int_4],[int_5],[int_6],[int_7],[int_8],[int_9],[int_10],[int_11],[int_12],[int_13],[int_14],[int_15],[int_16],[int_17],[int_18],[int_19],[int_20],[int_21],[int_22],[int_23],[int_24],[int_25],[int_26],[int_27],[int_28],[int_29],[int_30],[int_31],[int_32],[int_33],[int_34],[int_35],[int_36],[int_37],[int_38],[int_39],[int_40]
			,[datetime_1],[datetime_2],[datetime_3],[datetime_4],[datetime_5],[datetime_6],[datetime_7],[datetime_8],[datetime_9],[datetime_10],[datetime_11],[datetime_12],[datetime_13],[datetime_14],[datetime_15],[datetime_16],[datetime_17],[datetime_18],[datetime_19],[datetime_20],[datetime_21],[datetime_22],[datetime_23],[datetime_24],[datetime_25]
			,[money_1],[money_2],[money_3],[money_4],[money_5],[money_6],[money_7],[money_8],[money_9],[money_10],[money_11],[money_12],[money_13],[money_14],[money_15]
		FROM FlatProperties WHERE VersionId = @PreviousVersionId
	INSERT INTO ReferenceProperties
		([VersionId],[PropertyTypeId],[ReferredNodeId])
		SELECT @NewVersionId,[PropertyTypeId],[ReferredNodeId]
		FROM ReferenceProperties WHERE VersionId = @PreviousVersionId
	INSERT INTO TextPropertiesNVarchar
		([VersionId],[PropertyTypeId],[Value])
		SELECT @NewVersionId,[PropertyTypeId],[Value]
		FROM TextPropertiesNVarchar WHERE VersionId = @PreviousVersionId
	INSERT INTO TextPropertiesNText
		([VersionId],[PropertyTypeId],[Value])
		SELECT @NewVersionId,[PropertyTypeId],[Value]
		FROM TextPropertiesNText WHERE VersionId = @PreviousVersionId

	-- Set last version pointers
	EXEC proc_Node_SetLastVersion @NodeId
	
	-- Return
	DECLARE @NodeTimestamp timestamp
	DECLARE @LastMajorVersionId int
	DECLARE @LastMinorVersionId int
	SELECT @NodeTimestamp = [Timestamp], @LastMajorVersionId = LastMajorVersionId, @LastMinorVersionId = LastMinorVersionId FROM Nodes WHERE NodeId = @NodeId
    SELECT VersionId, @NodeTimestamp as NodeTimestamp, [Timestamp] as Versiontimestamp, @LastMajorVersionId as LastMajorVersionId, @LastMinorVersionId as LastMinorVersionId FROM Versions WHERE VersionId = @NewVersionId
    SELECT BinaryPropertyId, PropertyTypeId FROM BinaryProperties WHERE VersionId = @NewVersionId

END' 
END

PRINT('New stored procedures created.');

SKIPPED:
IF (@errorHappened > 0)
BEGIN
	PRINT('Script stopped with error.');
END

GO