// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Linq;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.FilterImporting;
using Rdmp.Core.Curation.FilterImporting.Construction;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataExport.DataExtraction.Commands;
using Rdmp.Core.DataExport.DataExtraction.UserPicks;
using Tests.Common;
using Tests.Common.Scenarios;

namespace Rdmp.Core.Tests.DataExport.Cloning;

public class CloneExtractionConfigurationTests : TestsRequiringAnExtractionConfiguration
{
    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void CloneWithFilters(bool introduceOrphanExtractionInformation)
    {
        if (introduceOrphanExtractionInformation)
            IntroduceOrphan();

        Assert.That(_configuration.ReleaseLog, Is.Empty);

        var filter = new ExtractionFilter(CatalogueRepository, "FilterByFish", _extractionInformations[0]);
        try
        {
            //setup a filter with a parameter
            filter.WhereSQL = "Fish = @fish";

            new ParameterCreator(new ExtractionFilterFactory(_extractionInformations[0]), null, null).CreateAll(filter,
                null);
            filter.SaveToDatabase();

            Assert.That(filter.ExtractionFilterParameters.Count(), Is.EqualTo(1));

            //create a root container
            var container = new FilterContainer(DataExportRepository);
            _selectedDataSet.RootFilterContainer_ID = container.ID;
            _selectedDataSet.SaveToDatabase();

            //create a deployed filter
            var importer = new FilterImporter(new DeployedExtractionFilterFactory(DataExportRepository), null);
            var deployedFilter = (DeployedExtractionFilter)importer.ImportFilter(container, filter, null);
            deployedFilter.FilterContainer_ID = container.ID;
            deployedFilter.Name = "FilterByFishDeployed";
            deployedFilter.SaveToDatabase();

            var param = deployedFilter.ExtractionFilterParameters[0];
            param.Value = "'jormungander'";
            param.SaveToDatabase();

            var request = new ExtractDatasetCommand(_configuration, new ExtractableDatasetBundle(_extractableDataSet));
            request.GenerateQueryBuilder();
            Assert.That(
CollapseWhitespace(request.QueryBuilder.SQL), Is.EqualTo(CollapseWhitespace(
                    string.Format(
                        @"DECLARE @fish AS varchar(50);
SET @fish='jormungander';
/*The ID of the cohort in [{0}CohortDatabase]..[Cohort]*/
DECLARE @CohortDefinitionID AS int;
SET @CohortDefinitionID=-599;
/*The project number of project {0}ExtractionConfiguration*/
DECLARE @ProjectNumber AS int;
SET @ProjectNumber=1;

SELECT DISTINCT 
[{0}CohortDatabase]..[Cohort].[ReleaseID] AS ReleaseID,
[{0}ScratchArea].[dbo].[TestTable].[Name],
[{0}ScratchArea].[dbo].[TestTable].[DateOfBirth]
FROM 
[{0}ScratchArea].[dbo].[TestTable] INNER JOIN [{0}CohortDatabase]..[Cohort] ON [{0}ScratchArea].[dbo].[TestTable].[PrivateID]=[{0}CohortDatabase]..[Cohort].[PrivateID]

WHERE
(
/*FilterByFishDeployed*/
Fish = @fish
)
AND
[{0}CohortDatabase]..[Cohort].[cohortDefinition_id]=-599
"
                        , TestDatabaseNames.Prefix))
));

            var deepClone = _configuration.DeepCloneWithNewIDs();
            Assert.Multiple(() =>
            {
                Assert.That(_configuration.Cohort_ID, Is.EqualTo(deepClone.Cohort_ID));
                Assert.That(_configuration.ID, Is.Not.EqualTo(deepClone.ID));
            });
            try
            {
                var request2 = new ExtractDatasetCommand(deepClone, new ExtractableDatasetBundle(_extractableDataSet));
                request2.GenerateQueryBuilder();

                Assert.That(request2.QueryBuilder.SQL, Is.EqualTo(request.QueryBuilder.SQL));
            }
            finally
            {
                deepClone.DeleteInDatabase();
            }
        }
        finally
        {
            filter.DeleteInDatabase();
        }
    }


    [Test]
    public void CloneWithExtractionProgress()
    {
        var sds = _configuration.SelectedDataSets[0];
        var ci = sds.GetCatalogue().CatalogueItems.First();
        var origProgress = new ExtractionProgress(DataExportRepository, sds, null, DateTime.Now, 10, "fff drrr", ci.ID)
        {
            ProgressDate = new DateTime(2001, 01, 01)
        };
        origProgress.SaveToDatabase();

        var deepClone = _configuration.DeepCloneWithNewIDs();
        Assert.Multiple(() =>
        {
            Assert.That(_configuration.Cohort_ID, Is.EqualTo(deepClone.Cohort_ID));
            Assert.That(_configuration.ID, Is.Not.EqualTo(deepClone.ID));
        });

        var clonedSds = deepClone.SelectedDataSets.Single(s => s.ExtractableDataSet_ID == sds.ExtractableDataSet_ID);

        var clonedProgress = clonedSds.ExtractionProgressIfAny;

        Assert.That(clonedProgress, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(clonedProgress.StartDate, Is.Null);
            Assert.That(clonedProgress.ProgressDate, Is.Null,
                "Cloning a ExtractionProgress should reset its ProgressDate back to null in anticipation of it being extracted again");

            Assert.That(origProgress.EndDate, Is.EqualTo(clonedProgress.EndDate));
            Assert.That(origProgress.NumberOfDaysPerBatch, Is.EqualTo(clonedProgress.NumberOfDaysPerBatch));
            Assert.That(origProgress.Name, Is.EqualTo(clonedProgress.Name));
            Assert.That(origProgress.ExtractionInformation_ID, Is.EqualTo(clonedProgress.ExtractionInformation_ID));
        });


        deepClone.DeleteInDatabase();

        // remove the progress so that it doesn't trip other tests
        origProgress.DeleteInDatabase();
    }


    private void IntroduceOrphan()
    {
        var cols = _configuration.GetAllExtractableColumnsFor(_extractableDataSet).Cast<ExtractableColumn>().ToArray();

        var name = cols.Single(c => c.GetRuntimeName().Equals("Name"));

        using var con = DataExportTableRepository.GetConnection();
        DataExportTableRepository.DiscoveredServer.GetCommand(
            $"UPDATE ExtractableColumn set CatalogueExtractionInformation_ID = {int.MaxValue} where ID = {name.ID}",
            con).ExecuteNonQuery();
    }
}