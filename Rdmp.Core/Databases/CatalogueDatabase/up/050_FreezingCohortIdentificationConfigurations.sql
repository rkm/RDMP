--Version:1.44.0.1
--Description: Adds the ability to Freeze CohortIdentificationConfigurations, Also allows you to specify a default query caching server for CohortIdentificationConfigurations
if not exists (select * from sys.columns where name ='Frozen')
begin

	ALTER TABLE CohortIdentificationConfiguration ADD [Frozen] [bit] NULL
	ALTER TABLE CohortIdentificationConfiguration ADD [FrozenDate] [datetime] NULL
	ALTER TABLE CohortIdentificationConfiguration ADD [FrozenBy] [varchar](500) NULL

end
GO

if exists (select * from sys.columns where name = 'Frozen' and is_nullable = 1)
begin
	UPDATE CohortIdentificationConfiguration SET Frozen = 0
	ALTER TABLE CohortIdentificationConfiguration alter column Frozen bit not null
	ALTER TABLE CohortIdentificationConfiguration ADD  CONSTRAINT [DF_CohortIdentificationConfiguration_Frozen]  DEFAULT ((0)) FOR [Frozen]
end

--Default 
if not exists( select * from sys.default_constraints where name = 'DF_CohortIdentificationConfiguration_QueryCachingServer_ID')
begin

ALTER TABLE CohortIdentificationConfiguration ADD  CONSTRAINT [DF_CohortIdentificationConfiguration_QueryCachingServer_ID]
DEFAULT ([dbo].[GetDefaultExternalServerIDFor]('CIC.QueryCachingServer_ID'))
FOR QueryCachingServer_ID

end