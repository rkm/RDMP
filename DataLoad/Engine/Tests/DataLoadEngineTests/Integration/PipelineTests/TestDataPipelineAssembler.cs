// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using CatalogueLibrary.Data.Cache;
using CatalogueLibrary.Data.Pipelines;
using CatalogueLibrary.Repositories;
using DataLoadEngineTests.Integration.Cache;
using NUnit.Framework;

namespace DataLoadEngineTests.Integration.PipelineTests
{
    public class TestDataPipelineAssembler
    {
        public PipelineComponent Destination { get; set; }
        public PipelineComponent Source { get; set; }
        public Pipeline Pipeline { get; set; }

        public TestDataPipelineAssembler(string pipeName, CatalogueRepository catalogueRepository)
        {
            Pipeline = new Pipeline(catalogueRepository, pipeName);
            Source = new PipelineComponent(catalogueRepository, Pipeline, typeof(TestDataInventor), 1, "DataInventorSource");
            Destination = new PipelineComponent(catalogueRepository, Pipeline, typeof(TestDataWriter), 2, "DataInventorDestination");

            Destination.CreateArgumentsForClassIfNotExists<TestDataWriter>();
            
            Pipeline.SourcePipelineComponent_ID = Source.ID;
            Pipeline.DestinationPipelineComponent_ID = Destination.ID;
            Pipeline.SaveToDatabase();

            var args = Source.CreateArgumentsForClassIfNotExists<TestDataInventor>();
            args[0].SetValue(TestContext.CurrentContext.WorkDirectory);
            args[0].SaveToDatabase();
        }

        public void ConfigureCacheProgressToUseThePipeline(CacheProgress cp)
        {
            cp.Pipeline_ID = Pipeline.ID;
            cp.ChunkPeriod = new TimeSpan(12, 0, 0);
            cp.SaveToDatabase();
        }


        public void Destroy()
        {
            Pipeline.DeleteInDatabase();
        }
    }
}
