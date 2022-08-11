--Version:7.0.0
--Description: Updates database to support new object Memento
--Version:7.0.0
--Description: Updates database to support new object Memento
 
 if not exists (select 1 from sys.tables where name = 'Commit')
 begin

CREATE TABLE [dbo].[Commit](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Username] [varchar](500) NOT NULL,
	[Date] [datetime] NOT NULL,	
	[Transaction] [varchar](32) NOT NULL,
	[Description] [varchar](max) NOT NULL,
 CONSTRAINT [PK_Commit] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
))
 end
 
 if not exists (select 1 from sys.tables where name = 'Memento')
 begin

CREATE TABLE [dbo].[Memento](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ReferencedObjectType] [varchar](500) NOT NULL,
	[ReferencedObjectID] [int] NOT NULL,
	[ReferencedObjectRepositoryType] [varchar](500) NOT NULL,	
	[BeforeYaml] [varchar](max) NOT NULL,
	[AfterYaml] [varchar](max) NOT NULL,
    [Commit_ID] [int] NOT NULL,
   FOREIGN KEY (Commit_ID) REFERENCES [Commit](ID),

 CONSTRAINT [PK_Memento] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
))
 end