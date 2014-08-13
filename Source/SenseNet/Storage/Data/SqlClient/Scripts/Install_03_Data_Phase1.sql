SET NOCOUNT ON

--====================================================================================== DataTypes

INSERT INTO [dbo].[SchemaDataTypes] ([Name]) VALUES ('String')
INSERT INTO [dbo].[SchemaDataTypes] ([Name]) VALUES ('Text')
INSERT INTO [dbo].[SchemaDataTypes] ([Name]) VALUES ('Int')
INSERT INTO [dbo].[SchemaDataTypes] ([Name]) VALUES ('Currency')
INSERT INTO [dbo].[SchemaDataTypes] ([Name]) VALUES ('DateTime')
INSERT INTO [dbo].[SchemaDataTypes] ([Name]) VALUES ('Binary')
INSERT INTO [dbo].[SchemaDataTypes] ([Name]) VALUES ('Reference')

--====================================================================================== PermissionTypes

SET IDENTITY_INSERT [dbo].[SchemaPermissionTypes] ON
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES ( 1,  1, 'See')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES ( 2,  5, 'Open')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES ( 3,  6, 'OpenMinor')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES ( 4,  7, 'Save')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES ( 5,  8, 'Publish')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES ( 6,  9, 'ForceCheckin')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES ( 7, 10, 'AddNew')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES ( 8, 11, 'Approve')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES ( 9, 12, 'Delete')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (10, 13, 'RecallOldVersion')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (11, 14, 'DeleteOldVersion')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (12, 15, 'SeePermissions')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (13, 16, 'SetPermissions')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (14, 17, 'RunApplication')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (15, 18, 'ManageListsAndWorkspaces')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (16,  2, 'Preview')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (17,  3, 'PreviewWithoutWatermark')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (18,  4, 'PreviewWithoutRedaction')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (19, 19, 'Custom01')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (20, 20, 'Custom02')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (21, 21, 'Custom03')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (22, 22, 'Custom04')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (23, 23, 'Custom05')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (24, 24, 'Custom06')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (25, 25, 'Custom07')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (26, 26, 'Custom08')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (27, 27, 'Custom09')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (28, 28, 'Custom10')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (29, 29, 'Custom11')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (30, 30, 'Custom12')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (31, 31, 'Custom13')
INSERT INTO [dbo].[SchemaPermissionTypes] ([PermissionId], [PermissionIndex], [Name]) VALUES (32, 32, 'Custom14')
SET IDENTITY_INSERT [dbo].[SchemaPermissionTypes] OFF

--====================================================================================== PropertySetTypes

INSERT INTO [dbo].[SchemaPropertySetTypes] ([Name]) VALUES ('NodeType')
INSERT INTO [dbo].[SchemaPropertySetTypes] ([Name]) VALUES ('ContentListType')

--====================================================================================== Nodetypes

DECLARE @id int
SELECT @id = PropertySetTypeId FROM [dbo].[SchemaPropertySetTypes] WHERE Name = 'NodeType'

INSERT INTO [dbo].[SchemaPropertySets] ([ParentId], [Name], [PropertySetTypeId], [ClassName])
	VALUES (null, 'Folder', @id, 'SenseNet.ContentRepository.Folder')
DECLARE @folderId int
SELECT @folderId = @@IDENTITY
INSERT INTO [dbo].[SchemaPropertySets] ([ParentId], [Name], [PropertySetTypeId], [ClassName])
	VALUES (null, 'Group', @id, 'SenseNet.ContentRepository.Group')
INSERT INTO [dbo].[SchemaPropertySets] ([ParentId], [Name], [PropertySetTypeId], [ClassName])
	VALUES (null, 'User', @id, 'SenseNet.ContentRepository.User')
INSERT INTO [dbo].[SchemaPropertySets] ([ParentId], [Name], [PropertySetTypeId], [ClassName])
	VALUES (null, 'PortalRoot', @id, 'SenseNet.ContentRepository.PortalRoot')
INSERT INTO [dbo].[SchemaPropertySets] ([ParentId], [Name], [PropertySetTypeId], [ClassName])
	VALUES (@folderId, 'SystemFolder', @id, 'SenseNet.ContentRepository.SystemFolder')
INSERT INTO [dbo].[SchemaPropertySets] ([ParentId], [Name], [PropertySetTypeId], [ClassName])
	VALUES (@folderId, 'Domains', @id, 'SenseNet.ContentRepository.Folder')

DECLARE @folderPropertySetId int
SELECT @folderPropertySetId = PropertySetId FROM SchemaPropertySets WHERE Name = 'Folder'
INSERT INTO [dbo].[SchemaPropertySets] ([ParentId], [Name], [PropertySetTypeId], [ClassName])
	VALUES (@folderPropertySetId, 'Domain', @id, 'SenseNet.ContentRepository.Domain')
INSERT INTO [dbo].[SchemaPropertySets] ([ParentId], [Name], [PropertySetTypeId], [ClassName])
	VALUES (@folderPropertySetId, 'OrganizationalUnit', @id, 'SenseNet.ContentRepository.OrganizationalUnit')


--====================================================================================== Admin node

DECLARE @UserType int
SELECT @UserType   = PropertySetId FROM SchemaPropertySets WHERE [Name] = 'User'

INSERT INTO [dbo].[Nodes](
			[NodeTypeId], [IsDeleted], [IsInherited], [ParentNodeId],           [Name],                 [Path], [Index],          [Locked], [LockedById], [ETag], [LockType], [LockTimeout],   [LockDate], [LockToken], [LastLockUpdate], [LastMinorVersionId], [LastMajorVersionId], [CreationDate], [CreatedById], [ModificationDate], [ModifiedById])
     VALUES(   @UserType,           0,             1,           NULL,  'Admin',       '/Admin',       1,                 0,         null,     '',          0,             0, '1900-01-01',          '',     '1900-01-01',                 null,	             null,      getutcdate(),             1,          getutcdate(),             1)

INSERT INTO [dbo].[Versions] ([NodeId], [MajorNumber], [MinorNumber], [CreationDate], [CreatedById], [ModificationDate], [ModifiedById])
     VALUES                  (       1,             1,             0,      getutcdate(),             1,          getutcdate(),              1)

UPDATE [dbo].[Nodes] SET [LastMinorVersionId] = 1, [LastMajorVersionId] = 1 WHERE NodeId = 1

--====================================================================================== Nodes
-------- Create NodeGenerator procedure

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[xCreateNode]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[xCreateNode]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE dbo.xCreateNode
	@NodeTypeName nvarchar(1000),
	@Index int,
	@ParentPath nvarchar(1000),
	@Name nvarchar(1000)
AS
BEGIN
	DECLARE @path nvarchar(1000)
	DECLARE @nodeTypeId int
	DECLARE @nodeId int
	DECLARE @parentId int
	DECLARE @adminId int
	DECLARE @versionId int

	SELECT @path = @ParentPath + '/' + @Name
	SELECT @nodeTypeId = PropertySetId FROM SchemaPropertySets WHERE [Name] LIKE ('%' + @NodeTypeName)
	SELECT @parentId = NodeId FROM Nodes WHERE [Path] = @ParentPath
	
	SELECT @adminId = 1

	INSERT INTO [dbo].[Nodes]
			   ([NodeTypeId], [IsDeleted], [IsInherited], [ParentNodeId], [Name], [Path], [Index], [Locked], [LockedById], [ETag], [LockType], [LockTimeout],   [LockDate], [LockToken], [LastLockUpdate], [LastMinorVersionId], [LastMajorVersionId], [CreationDate], [CreatedById], [ModificationDate], [ModifiedById], [IsSystem])
		 VALUES( @nodeTypeId,           0,             1,      @parentId,  @Name,  @path, @Index,         0,         null,    '',           0,             0, '1900-01-01',          '',     '1900-01-01',                 null,                 null,      getutcdate(),      @adminId,          getutcdate(),       @adminId,          0)
	SELECT @nodeId = @@IDENTITY

	INSERT INTO [dbo].[Versions] ([NodeId], [MajorNumber], [MinorNumber], [CreationDate], [CreatedById], [ModificationDate], [ModifiedById])
		 VALUES                  ( @nodeId,             1,             0,   '2007-07-07',      @adminId,       '2007-07-08',       @adminId)
	SELECT @versionId = @@IDENTITY

	UPDATE [dbo].[Nodes] SET [LastMinorVersionId] = @versionId, [LastMajorVersionId] = @versionId WHERE NodeId = @nodeId

END
GO

SET NOCOUNT ON


--======================================================================================================================
--                    NodeTypeName,        Index, ParentPath,                     Name

EXEC dbo.xCreateNode 'PortalRoot',         1,     '',                             'Root'            -- NodeId: 2
EXEC dbo.xCreateNode 'Domains',            3,     '/Root',                        'IMS'             -- NodeId: 3

EXEC dbo.xCreateNode 'Domain',             0,     '/Root/IMS',                    'BuiltIn'            -- NodeId: 4
EXEC dbo.xCreateNode 'OrganizationalUnit', 0,     '/Root/IMS/BuiltIn',               'Portal'          -- NodeId: 5

EXEC dbo.xCreateNode 'User',               4,     '/Root/IMS/BuiltIn/Portal',        'Visitor'         -- NodeId: 6
EXEC dbo.xCreateNode 'Group',              2,     '/Root/IMS/BuiltIn/Portal',        'Administrators'  -- NodeId: 7
EXEC dbo.xCreateNode 'Group',              3,     '/Root/IMS/BuiltIn/Portal',        'Everyone'        -- NodeId: 8
EXEC dbo.xCreateNode 'Group',              5,     '/Root/IMS/BuiltIn/Portal',        'Creators'        -- NodeId: 9
EXEC dbo.xCreateNode 'Group',              5,     '/Root/IMS/BuiltIn/Portal',        'LastModifiers'   -- NodeId: 10
EXEC dbo.xCreateNode 'User',               6,     '/Root/IMS/BuiltIn/Portal',        'Somebody'        -- NodeId: 11
EXEC dbo.xCreateNode 'Group',              7,     '/Root/IMS/BuiltIn/Portal',        'Operators'        -- NodeId: 12
GO
--==== create NodeId gap for future extensions

SET IDENTITY_INSERT [dbo].[Nodes] ON
INSERT INTO [dbo].[Nodes]
	([NodeId],[NodeTypeId],[ContentListTypeId],[ContentListId],[IsDeleted],[IsInherited],[ParentNodeId],[Name],[Path],[Index],[Locked],[LockedById],[ETag],[LockType],[LockTimeout],[LockDate],[LockToken],[LastLockUpdate],[LastMinorVersionId],[LastMajorVersionId],[CreationDate],[CreatedById],[ModificationDate],[ModifiedById])
	VALUES
	(999,1,null,null,0,1,2,'','',0,0,null,'',0,0,getutcdate(),'',getutcdate(),null,null,getutcdate(),1,getutcdate(),1)
SET IDENTITY_INSERT [dbo].[Nodes] OFF
DELETE FROM [dbo].[Nodes] WHERE NodeId = 999
GO

--======================================================================================================================

DROP PROCEDURE dbo.xCreateNode
GO

--================================================================================= Update Admin
-- Put Admin and Somebody under /Root/IMS/BuiltIn/Portal

DECLARE @portalOuId int
SELECT @portalOuId = NodeId FROM [dbo].[Nodes] WHERE [Path] = '/Root/IMS/BuiltIn/Portal'
UPDATE [dbo].[Nodes] SET
	[ParentNodeId] = @portalOuId, 
	[Path] = '/Root/IMS/BuiltIn/Portal/Admin',
	[Issystem] = 0
WHERE NodeId = 1


--======================================================================================================================

SET NOCOUNT OFF