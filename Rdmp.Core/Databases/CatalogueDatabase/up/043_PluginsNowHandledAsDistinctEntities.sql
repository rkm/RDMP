--Version:1.37.0.0
--Description: Previously all plugins were loaded into the LoadModuleAssembly table as a big bag of dlls.  This patch adds a new table Plugin which has a 1 to M relationship with LoadModule Assemblies.  This allows side by side dlls with different dependencies on the same library e.g. Newtonsoft.Json.dll version 4.5.0.0 vs another plugin needing Newtonsoft.Json.dll version 6.0.0.0
--WARNING: This will delete any existing plugin dlls from the database (These will need to be uploaded again through the new mechanism in CatalogueManager)
if not exists(select * from sys.tables where name='Plugin')
begin
delete FROM [LoadModuleAssembly]

alter table [LoadModuleAssembly]
add
Plugin_ID int not null

CREATE TABLE [dbo].[Plugin](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](500) NOT NULL,
	[UploadedFromDirectory] [varchar](max) NOT NULL,
	[PluginVersion] [varchar](50) NULL

 CONSTRAINT [PK_Plugin] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)
)
end


if not exists (select * from sys.foreign_keys where name = 'FK_LoadModuleAssembly_Plugin')
begin
	ALTER TABLE LoadModuleAssembly  WITH CHECK ADD  CONSTRAINT [FK_LoadModuleAssembly_Plugin] FOREIGN KEY(Plugin_ID)
	REFERENCES Plugin (ID)
	ON DELETE CASCADE
	
	CREATE UNIQUE NONCLUSTERED INDEX [ix_PluginNamesMustBeUnique] ON [dbo].[Plugin]
	(
		[Name] ASC
	)
end

--Also bundle this fix which is due to sloppy management and bidirectional relationship columns this fixes the names of the fields to be consistent and gets rid of the reverse relationship 
if exists (select * from sys.columns where name = 'LoadSchedule_ID')
begin

exec sp_rename 'CacheProgress.LoadSchedule_ID','LoadProgress_ID','COLUMN' 

alter table LoadProgress drop column CacheProgress_ID

--Force 0 to 1 (uniquness) on CacheProgress
CREATE UNIQUE NONCLUSTERED INDEX [ix_LoadProgressRelationshipIs0To1] ON [dbo].[CacheProgress]
(
	[LoadProgress_ID] ASC
)
end