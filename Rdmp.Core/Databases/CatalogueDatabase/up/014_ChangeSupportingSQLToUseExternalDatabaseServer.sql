--Version:1.12.0.0
--Description:Replaces the existing field ConnectionString to a reference to ExternalDatabaseServer instead
if exists (select 1 from sys.columns where name ='ConnectionString')
begin
alter table [SupportingSQLTable] drop column ConnectionString
alter table [SupportingSQLTable] add ExternalDatabaseServer_ID int null
end

if not exists( select * from sys.foreign_keys where name ='FK_SupportingSQLTable_ExternalDatabaseServer')
begin 
ALTER TABLE [SupportingSQLTable]  WITH CHECK ADD  CONSTRAINT [FK_SupportingSQLTable_ExternalDatabaseServer] FOREIGN KEY([ExternalDatabaseServer_ID])
REFERENCES [ExternalDatabaseServer] ([ID])
end