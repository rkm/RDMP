﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.EntityNaming;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.DataHelper;
using CatalogueLibrary.Triggers.Implementations;
using DataLoadEngine.Job;
using DataLoadEngine.Migration;
using HIC.Logging;
using HIC.Logging.PastEvents;
using NUnit.Framework;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using Rhino.Mocks;
using Tests.Common;

namespace DataLoadEngineTests.Integration.CrossDatabaseTypeTests
{
    public class CrossDatabaseMergeCommandTest:DatabaseTests
    {
        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MYSQLServer)]
        public void TestMerge(DatabaseType databaseType)
        {
            var dbFrom = GetCleanedServer(databaseType, "CrossDatabaseMergeCommandFrom");
            var dbTo = GetCleanedServer(databaseType, "CrossDatabaseMergeCommandTo");

            var dt = new DataTable();
            var colName = new DataColumn("Name");
            var colAge = new DataColumn("Age");
            dt.Columns.Add(colName);
            dt.Columns.Add(colAge);
            dt.Columns.Add("Postcode");

            //Data in live awaiting to be updated
            dt.Rows.Add(new object[]{"Dave",18,"DD3 1AB"});
            dt.Rows.Add(new object[] {"Dave", 25, "DD1 1XS" });
            dt.Rows.Add(new object[] {"Mango", 32, DBNull.Value});
            dt.Rows.Add(new object[] { "Filli", 32,"DD3 78L" });
            dt.Rows.Add(new object[] { "Mandrake", 32, DBNull.Value });

            dt.PrimaryKey = new[]{colName,colAge};

            var to = dbTo.CreateTable("ToTable", dt);

            Assert.IsTrue(to.DiscoverColumn("Name").IsPrimaryKey);
            Assert.IsTrue(to.DiscoverColumn("Age").IsPrimaryKey);
            Assert.IsFalse(to.DiscoverColumn("Postcode").IsPrimaryKey);

            dt.Rows.Clear();
            
            //new data being loaded
            dt.Rows.Add(new object[] { "Dave", 25, "DD1 1PS" }); //update to change postcode to "DD1 1PS"
            dt.Rows.Add(new object[] { "Chutney", 32, DBNull.Value }); //new insert Chutney
            dt.Rows.Add(new object[] { "Mango", 32, DBNull.Value }); //ignored because already present in dataset
            dt.Rows.Add(new object[] { "Filli", 32, DBNull.Value }); //update from "DD3 78L" null
            dt.Rows.Add(new object[] { "Mandrake", 32, "DD1 1PS" }); //update from null to "DD1 1PS"
            dt.Rows.Add(new object[] { "Mandrake", 31, "DD1 1PS" }); // insert because Age is unique (and part of pk)
            
            var from = dbFrom.CreateTable("CrossDatabaseMergeCommandTo_ToTable_STAGING", dt);
            

            //import the to table as a TableInfo
            var importer = new TableInfoImporter(CatalogueRepository, to);
            TableInfo ti;
            ColumnInfo[] cis;
            importer.DoImport(out ti, out cis);

            //put the backup trigger on the live table (this will also create the needed hic_ columns etc)
            var triggerImplementer = new TriggerImplementerFactory(databaseType).Create(to);
            triggerImplementer.CreateTrigger(new ThrowImmediatelyCheckNotifier());

            var configuration = new MigrationConfiguration(dbFrom, LoadBubble.Staging, LoadBubble.Live,new FixedStagingDatabaseNamer(to.Database.GetRuntimeName(),from.Database.GetRuntimeName()));
            
            var migrationHost = new MigrationHost(dbFrom, dbTo, configuration);

            //set up a logging task
            var logServer = new ServerDefaults(CatalogueRepository).GetDefaultFor(ServerDefaults.PermissableDefaults.LiveLoggingServer_ID);
            var logManager = new LogManager(logServer);
            logManager.CreateNewLoggingTaskIfNotExists("CrossDatabaseMergeCommandTest");
            var dli = logManager.CreateDataLoadInfo("CrossDatabaseMergeCommandTest", "tests", "running test", "", true);

            var job = new ThrowImmediatelyDataLoadJob();
            job.DataLoadInfo = dli;
            job.RegularTablesToLoad = new List<TableInfo>(new[]{ti});
            
            migrationHost.Migrate(job, new GracefulCancellationToken());
            
            var resultantDt = to.GetDataTable();
            Assert.AreEqual(7,resultantDt.Rows.Count);

            AssertRowEquals(resultantDt, "Dave", 25, "DD1 1PS");
            AssertRowEquals(resultantDt, "Chutney", 32, DBNull.Value);
            AssertRowEquals(resultantDt, "Mango", 32, DBNull.Value);
            
            AssertRowEquals(resultantDt,"Filli",32,DBNull.Value);
            AssertRowEquals(resultantDt, "Mandrake", 32, "DD1 1PS");
            AssertRowEquals(resultantDt, "Mandrake", 31, "DD1 1PS");
            
            AssertRowEquals(resultantDt, "Dave", 18, "DD3 1AB");


            var archival = logManager.GetArchivalLoadInfoFor("CrossDatabaseMergeCommandTest", new CancellationToken());
            var log = archival.First();


            Assert.AreEqual(dli.ID,log.ID);
            Assert.AreEqual(2,log.TableLoadInfos.Single().Inserts);
            Assert.AreEqual(3, log.TableLoadInfos.Single().Updates);
        }

        private void AssertRowEquals(DataTable resultantDt,string name,int age, object postcode)
        {
            Assert.AreEqual(
                1, resultantDt.Rows.Cast<DataRow>().Count(r => Equals(r["Name"], name) && Equals(r["Age"], age) && Equals(r["Postcode"], postcode)),
                "Did not find expected record:" + string.Join(",",name,age,postcode));
        }
    }
}
