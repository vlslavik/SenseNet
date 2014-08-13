IF  EXISTS (SELECT * FROM sys.default_constraints WHERE object_id = OBJECT_ID(N'[dbo].[DF_Tasks_RegisteredAt]') AND parent_object_id = OBJECT_ID(N'[dbo].[Tasks]'))
Begin
IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_Tasks_RegisteredAt]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Tasks] DROP CONSTRAINT [DF_Tasks_RegisteredAt]
END
End
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Tasks]') AND type in (N'U'))
DROP TABLE [dbo].[Tasks]
GO

CREATE TABLE [dbo].[Tasks](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Type] [nvarchar](450) NOT NULL,
	[Order] [float] NOT NULL,
	[RegisteredAt] [datetime] NOT NULL CONSTRAINT DF_Tasks_RegisteredAt DEFAULT getutcdate(),
	[LastLockUpdate] [datetime] NULL,
	[LockedBy] [nvarchar](450) NULL,
	[Hash] [int] NOT NULL,
	[TaskData] [ntext] NULL,
 CONSTRAINT [PK_dbo.Tasks] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Tasks] ON [dbo].[Tasks] 
(
	[Hash] ASC 
) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = ON, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
