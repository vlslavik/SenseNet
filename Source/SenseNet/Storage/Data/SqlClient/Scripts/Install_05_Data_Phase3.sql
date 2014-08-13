SET NOCOUNT ON

-------- Create Default Security entries part 2

DECLARE @RootContentId int
DECLARE @SystemFolder int
DECLARE @SurveyItemTypeId int
DECLARE @VotingItemTypeId int
DECLARE @FormItemTypeId int
DECLARE @RegistrationWorkflowTypeId int
DECLARE @WorkspaceTypeId int
DECLARE @ContentListTypeId int
DECLARE @FileTypeId int
DECLARE @ListItemTypeId int

DECLARE @AdministratorNodeId int
DECLARE @AdministratorGroupNodeId int
DECLARE @DevelopersGroupId int
DECLARE @VisitorNodeId int
DECLARE @EveryoneGroupId int
DECLARE @IdentifiedUsersGroupId int

SELECT @RootContentId = NodeId FROM Nodes WHERE Path = '/Root'
IF @RootContentId IS NULL RAISERROR ('Root content node cannot be found. Check the Install_05_Data_Phase3.sql.', 18, 1)

-- ContentType ids ----------------------------------------------------------

SELECT @SystemFolder = NodeId FROM Nodes WHERE Path LIKE '/Root/System/Schema/ContentTypes/%/SystemFolder'
IF @SystemFolder IS NULL RAISERROR ('SystemFolder node cannot be found. Check the Install_05_Data_Phase3.sql.', 18, 1)

SELECT @SurveyItemTypeId = NodeId FROM Nodes WHERE Path LIKE '/Root/System/Schema/ContentTypes/%/SurveyItem'
IF @SurveyItemTypeId IS NULL RAISERROR ('SurveyItem node cannot be found. Check the Install_05_Data_Phase3.sql.', 18, 1)

SELECT @VotingItemTypeId = NodeId FROM Nodes WHERE Path LIKE '/Root/System/Schema/ContentTypes/%/VotingItem'
IF @VotingItemTypeId IS NULL RAISERROR ('VotingItem node cannot be found. Check the Install_05_Data_Phase3.sql.', 18, 1)

SELECT @FormItemTypeId = NodeId FROM Nodes WHERE Path LIKE '/Root/System/Schema/ContentTypes/%/FormItem'
IF @FormItemTypeId IS NULL RAISERROR ('FormItem node cannot be found. Check the Install_05_Data_Phase3.sql.', 18, 1)

SELECT @RegistrationWorkflowTypeId = NodeId FROM Nodes WHERE Path LIKE '/Root/System/Schema/ContentTypes/%/RegistrationWorkflow'
IF @RegistrationWorkflowTypeId IS NULL RAISERROR ('RegistrationWorkflow node cannot be found. Check the Install_05_Data_Phase3.sql.', 18, 1)

SELECT @WorkspaceTypeId = NodeId FROM Nodes WHERE Path LIKE '/Root/System/Schema/ContentTypes/GenericContent/Folder/Workspace'
IF @WorkspaceTypeId IS NULL RAISERROR ('Workspace type node cannot be found. Check the Install_05_Data_Phase3.sql.', 18, 1)

SELECT @ContentListTypeId = NodeId FROM Nodes WHERE Path LIKE '/Root/System/Schema/ContentTypes/GenericContent/Folder/ContentList'
IF @ContentListTypeId IS NULL RAISERROR ('ContentList type node cannot be found. Check the Install_05_Data_Phase3.sql.', 18, 1)

SELECT @FileTypeId = NodeId FROM Nodes WHERE Path LIKE '/Root/System/Schema/ContentTypes/GenericContent/File'
IF @FileTypeId IS NULL RAISERROR ('File type node cannot be found. Check the Install_05_Data_Phase3.sql.', 18, 1)

SELECT @ListItemTypeId = NodeId FROM Nodes WHERE Path LIKE '/Root/System/Schema/ContentTypes/GenericContent/ListItem'
IF @ListItemTypeId IS NULL RAISERROR ('ListItem type node cannot be found. Check the Install_05_Data_Phase3.sql.', 18, 1)

-- Identity ids ----------------------------------------------------------

SELECT @AdministratorNodeId = NodeId FROM Nodes WHERE Path = '/Root/IMS/BuiltIn/Portal/Admin'
IF @AdministratorNodeId IS NULL	RAISERROR ('"Admin" node cannot be found. Check the Install_05_Data_Phase3.sql.', 18, 2)

SELECT @AdministratorGroupNodeId = NodeId FROM Nodes WHERE Path = '/Root/IMS/BuiltIn/Portal/Administrators'
IF @AdministratorGroupNodeId IS NULL RAISERROR ('"Administrators" Group node cannot be found. Check the Install_05_Data_Phase3.sql.', 18, 4)

SELECT @DevelopersGroupId = NodeId FROM Nodes WHERE Path = '/Root/IMS/BuiltIn/Demo/Developers'
IF @DevelopersGroupId IS NULL RAISERROR ('Developers Group node cannot be found. Check the Install_05_Data_Phase3.sql.', 18, 4)

SELECT @VisitorNodeId = NodeId FROM Nodes WHERE Path = '/Root/IMS/BuiltIn/Portal/Visitor'
IF @VisitorNodeId IS NULL	RAISERROR ('"Visitor" node cannot be found. Check the Install_05_Data_Phase3.sql.', 18, 2)

SELECT @EveryoneGroupId = NodeId FROM Nodes WHERE Path = '/Root/IMS/BuiltIn/Portal/Everyone'
IF @EveryoneGroupId IS NULL RAISERROR ('Everyone Group node cannot be found. Check the Install_05_Data_Phase3.sql.', 18, 4)

SELECT @IdentifiedUsersGroupId = NodeId FROM Nodes WHERE Path = '/Root/IMS/BuiltIn/Portal/IdentifiedUsers'
IF @IdentifiedUsersGroupId IS NULL RAISERROR ('IdentifiedUsers group node cannot be found. Check the Install_05_Data_Phase3.sql.', 18, 4)

-- Break the permission inheritance on the SystemFolder
UPDATE Nodes SET IsInherited = 0 WHERE NodeId = @SystemFolder

-- Allow See, Open, RunApplication on SystemFolder for Developers
INSERT INTO dbo.SecurityEntries (DefinedOnNodeId,PrincipalId,IsInheritable,PermissionValue1,PermissionValue2,PermissionValue3,PermissionValue4,PermissionValue5,PermissionValue6,PermissionValue7,PermissionValue8,PermissionValue9,PermissionValue10,PermissionValue11,PermissionValue12,PermissionValue13,PermissionValue14,PermissionValue15)
	VALUES (@SystemFolder,@DevelopersGroupId,1,1,1,0,0,0,0,0,0,0,0,0,0,0,1,0)
-- Allow full control on SystemFolder for Administrators
INSERT INTO dbo.SecurityEntries (DefinedOnNodeId,PrincipalId,IsInheritable,PermissionValue1,PermissionValue2,PermissionValue3,PermissionValue4,PermissionValue5,PermissionValue6,PermissionValue7,PermissionValue8,PermissionValue9,PermissionValue10,PermissionValue11,PermissionValue12,PermissionValue13,PermissionValue14,PermissionValue15)
	VALUES (@SystemFolder,@AdministratorGroupNodeId,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1)
-- Allow full control on SystemFolder for Admin
INSERT INTO dbo.SecurityEntries (DefinedOnNodeId,PrincipalId,IsInheritable,PermissionValue1,PermissionValue2,PermissionValue3,PermissionValue4,PermissionValue5,PermissionValue6,PermissionValue7,PermissionValue8,PermissionValue9,PermissionValue10,PermissionValue11,PermissionValue12,PermissionValue13,PermissionValue14,PermissionValue15)
	VALUES (@SystemFolder,@AdministratorNodeId,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1)

-- Allow LOCAL ONLY See, Preview, Open on Root for Developers (to be able to open Content Explorer root)
INSERT INTO dbo.SecurityEntries (DefinedOnNodeId,PrincipalId,IsInheritable,PermissionValue1,PermissionValue2,PermissionValue3,PermissionValue4,PermissionValue5,PermissionValue6,PermissionValue7,PermissionValue8,PermissionValue9,PermissionValue10,PermissionValue11,PermissionValue12,PermissionValue13,PermissionValue14,PermissionValue15,PermissionValue16,PermissionValue17,PermissionValue18)
	VALUES (@RootContentId,@DevelopersGroupId,0,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1)

-- Allow See on public content types for Visitor and Everyone
INSERT INTO dbo.SecurityEntries (DefinedOnNodeId,PrincipalId,IsInheritable,PermissionValue1,PermissionValue2,PermissionValue3,PermissionValue4,PermissionValue5,PermissionValue6,PermissionValue7,PermissionValue8,PermissionValue9,PermissionValue10,PermissionValue11,PermissionValue12,PermissionValue13,PermissionValue14,PermissionValue15)
	VALUES (@SurveyItemTypeId,@VisitorNodeId,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0)
	
INSERT INTO dbo.SecurityEntries (DefinedOnNodeId,PrincipalId,IsInheritable,PermissionValue1,PermissionValue2,PermissionValue3,PermissionValue4,PermissionValue5,PermissionValue6,PermissionValue7,PermissionValue8,PermissionValue9,PermissionValue10,PermissionValue11,PermissionValue12,PermissionValue13,PermissionValue14,PermissionValue15)
	VALUES (@SurveyItemTypeId,@EveryoneGroupId,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0)

INSERT INTO dbo.SecurityEntries (DefinedOnNodeId,PrincipalId,IsInheritable,PermissionValue1,PermissionValue2,PermissionValue3,PermissionValue4,PermissionValue5,PermissionValue6,PermissionValue7,PermissionValue8,PermissionValue9,PermissionValue10,PermissionValue11,PermissionValue12,PermissionValue13,PermissionValue14,PermissionValue15)
	VALUES (@VotingItemTypeId,@VisitorNodeId,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0)

INSERT INTO dbo.SecurityEntries (DefinedOnNodeId,PrincipalId,IsInheritable,PermissionValue1,PermissionValue2,PermissionValue3,PermissionValue4,PermissionValue5,PermissionValue6,PermissionValue7,PermissionValue8,PermissionValue9,PermissionValue10,PermissionValue11,PermissionValue12,PermissionValue13,PermissionValue14,PermissionValue15)
	VALUES (@VotingItemTypeId,@EveryoneGroupId,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0)

INSERT INTO dbo.SecurityEntries (DefinedOnNodeId,PrincipalId,IsInheritable,PermissionValue1,PermissionValue2,PermissionValue3,PermissionValue4,PermissionValue5,PermissionValue6,PermissionValue7,PermissionValue8,PermissionValue9,PermissionValue10,PermissionValue11,PermissionValue12,PermissionValue13,PermissionValue14,PermissionValue15)
	VALUES (@FormItemTypeId,@VisitorNodeId,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0)

INSERT INTO dbo.SecurityEntries (DefinedOnNodeId,PrincipalId,IsInheritable,PermissionValue1,PermissionValue2,PermissionValue3,PermissionValue4,PermissionValue5,PermissionValue6,PermissionValue7,PermissionValue8,PermissionValue9,PermissionValue10,PermissionValue11,PermissionValue12,PermissionValue13,PermissionValue14,PermissionValue15)
	VALUES (@FormItemTypeId,@EveryoneGroupId,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0)

INSERT INTO dbo.SecurityEntries (DefinedOnNodeId,PrincipalId,IsInheritable,PermissionValue1,PermissionValue2,PermissionValue3,PermissionValue4,PermissionValue5,PermissionValue6,PermissionValue7,PermissionValue8,PermissionValue9,PermissionValue10,PermissionValue11,PermissionValue12,PermissionValue13,PermissionValue14,PermissionValue15)
	VALUES (@RegistrationWorkflowTypeId,@VisitorNodeId,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0)

-- Allow See on common content types for Identified users (workspace, list, file, listitem)
INSERT INTO dbo.SecurityEntries (DefinedOnNodeId,PrincipalId,IsInheritable,PermissionValue1,PermissionValue2,PermissionValue3,PermissionValue4,PermissionValue5,PermissionValue6,PermissionValue7,PermissionValue8,PermissionValue9,PermissionValue10,PermissionValue11,PermissionValue12,PermissionValue13,PermissionValue14,PermissionValue15)
	VALUES (@WorkspaceTypeId,@IdentifiedUsersGroupId,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0)
INSERT INTO dbo.SecurityEntries (DefinedOnNodeId,PrincipalId,IsInheritable,PermissionValue1,PermissionValue2,PermissionValue3,PermissionValue4,PermissionValue5,PermissionValue6,PermissionValue7,PermissionValue8,PermissionValue9,PermissionValue10,PermissionValue11,PermissionValue12,PermissionValue13,PermissionValue14,PermissionValue15)
	VALUES (@ContentListTypeId,@IdentifiedUsersGroupId,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0)
INSERT INTO dbo.SecurityEntries (DefinedOnNodeId,PrincipalId,IsInheritable,PermissionValue1,PermissionValue2,PermissionValue3,PermissionValue4,PermissionValue5,PermissionValue6,PermissionValue7,PermissionValue8,PermissionValue9,PermissionValue10,PermissionValue11,PermissionValue12,PermissionValue13,PermissionValue14,PermissionValue15)
	VALUES (@FileTypeId,@IdentifiedUsersGroupId,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0)
INSERT INTO dbo.SecurityEntries (DefinedOnNodeId,PrincipalId,IsInheritable,PermissionValue1,PermissionValue2,PermissionValue3,PermissionValue4,PermissionValue5,PermissionValue6,PermissionValue7,PermissionValue8,PermissionValue9,PermissionValue10,PermissionValue11,PermissionValue12,PermissionValue13,PermissionValue14,PermissionValue15)
	VALUES (@ListItemTypeId,@IdentifiedUsersGroupId,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0)

SET NOCOUNT OFF