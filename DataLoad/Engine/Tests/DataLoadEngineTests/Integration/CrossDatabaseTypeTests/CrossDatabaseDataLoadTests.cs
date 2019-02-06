// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.Data.EntityNaming;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.DataHelper;
using CatalogueLibrary.Triggers;
using DataLoadEngine.Checks;
using DataLoadEngine.Checks.Checkers;
using DataLoadEngine.DatabaseManagement.EntityNaming;
using DataLoadEngine.Job;
using DataLoadEngine.LoadExecution;
using DataLoadEngine.LoadProcess;
using FAnsi;
using FAnsi.Discovery;
using FAnsi.Discovery.TableCreation;
using FAnsi.Discovery.TypeTranslation;
using HIC.Logging;
using LoadModules.Generic.Attachers;
using NUnit.Framework;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;
using Tests.Common;

namespace DataLoadEngineTests.Integration.CrossDatabaseTypeTests
{

    /*
     Test currently requires for LowPrivilegeLoaderAccount (e.g. minion)
     ---------------------------------------------------
     
        create database DLE_STAGING

        use DLE_STAGING

        CREATE USER [minion] FOR LOGIN [minion]

        ALTER ROLE [db_datareader] ADD MEMBER [minion]
        ALTER ROLE [db_ddladmin] ADD MEMBER [minion]
        ALTER ROLE [db_datawriter] ADD MEMBER [minion]
    */

    public class CrossDatabaseDataLoadTests : DatabaseTests
    {
        public enum TestCase
        {
            Normal,
            LowPrivilegeLoaderAccount,
            ForeignKeyOrphans,
            DodgyCollation,
            AllPrimaryKeys,
            WithNonPrimaryKeyIdentityColumn,
            
            WithCustomTableNamer,

            WithDiffColumnIgnoreRegex //tests ability of the system to skip a given column when doing the DLE diff section
        }

        [TestCase(DatabaseType.Oracle,TestCase.Normal)]
        [TestCase(DatabaseType.MicrosoftSQLServer,TestCase.Normal)]
        [TestCase(DatabaseType.MicrosoftSQLServer, TestCase.WithCustomTableNamer)]
        [TestCase(DatabaseType.MicrosoftSQLServer, TestCase.WithNonPrimaryKeyIdentityColumn)]
        [TestCase(DatabaseType.MicrosoftSQLServer, TestCase.DodgyCollation)]
        [TestCase(DatabaseType.MicrosoftSQLServer, TestCase.LowPrivilegeLoaderAccount)]
        [TestCase(DatabaseType.MicrosoftSQLServer, TestCase.AllPrimaryKeys)]
        [TestCase(DatabaseType.MySql,TestCase.Normal)]
        //[TestCase(DatabaseType.MySql, TestCase.WithNonPrimaryKeyIdentityColumn)] //Not supported by MySql:Incorrect table definition; there can be only one auto column and it must be defined as a key
        [TestCase(DatabaseType.MySql, TestCase.DodgyCollation)]
        [TestCase(DatabaseType.MySql, TestCase.WithCustomTableNamer)]
        [TestCase(DatabaseType.MySql, TestCase.LowPrivilegeLoaderAccount)]
        [TestCase(DatabaseType.MySql, TestCase.AllPrimaryKeys)]
        [TestCase(DatabaseType.MySql, TestCase.WithDiffColumnIgnoreRegex)]
        public void Load(DatabaseType databaseType, TestCase testCase)
        {
            var defaults = new ServerDefaults(CatalogueRepository);
            var logServer = defaults.GetDefaultFor(ServerDefaults.PermissableDefaults.LiveLoggingServer_ID);
            var logManager = new LogManager(logServer);
            
            var db = GetCleanedServer(databaseType);

            var raw = db.Server.ExpectDatabase(db.GetRuntimeName() + "_RAW");
            if(raw.Exists())
                raw.Drop();
            
            var dt = new DataTable("MyTable");
            dt.Columns.Add("Name");
            dt.Columns.Add("DateOfBirth");
            dt.Columns.Add("FavouriteColour");
            dt.Rows.Add("Bob", "2001-01-01","Pink");
            dt.Rows.Add("Frank", "2001-01-01","Orange");

            var nameCol = new DatabaseColumnRequest("Name", new DatabaseTypeRequest(typeof (string), 20), false){IsPrimaryKey = true};

            if (testCase == TestCase.DodgyCollation)
                if(databaseType == DatabaseType.MicrosoftSQLServer)
                    nameCol.Collation = "Latin1_General_CS_AS_KS_WS";
                else if (databaseType == DatabaseType.MySql)
                    nameCol.Collation = "latin1_german1_ci";


            DiscoveredTable tbl;
            if (testCase == TestCase.WithNonPrimaryKeyIdentityColumn)
            {
                tbl = db.CreateTable("MyTable",new []
                {
                    new DatabaseColumnRequest("ID",new DatabaseTypeRequest(typeof(int)),false){IsPrimaryKey = false,IsAutoIncrement = true}, 
                    nameCol, 
                    new DatabaseColumnRequest("DateOfBirth",new DatabaseTypeRequest(typeof(DateTime)),false){IsPrimaryKey = true}, 
                    new DatabaseColumnRequest("FavouriteColour",new DatabaseTypeRequest(typeof(string))), 
                });
                
                using (var blk = tbl.BeginBulkInsert())
                    blk.Upload(dt);

                Assert.AreEqual(1,tbl.DiscoverColumns().Count(c=>c.GetRuntimeName().Equals("ID",StringComparison.CurrentCultureIgnoreCase)),"Table created did not contain ID column");
                
            }
            else
            if (testCase == TestCase.AllPrimaryKeys)
            {
                dt.PrimaryKey = dt.Columns.Cast<DataColumn>().ToArray();
                tbl = db.CreateTable("MyTable",dt,new []{nameCol}); //upload the column as is 
                Assert.IsTrue(tbl.DiscoverColumns().All(c => c.IsPrimaryKey));
            }
            else
            {
                tbl = db.CreateTable("MyTable", dt, new[]
                {
                    nameCol,
                    new DatabaseColumnRequest("DateOfBirth",new DatabaseTypeRequest(typeof(DateTime)),false){IsPrimaryKey = true}
                });
            }

            Assert.AreEqual(2, tbl.GetRowCount());
            
            //define a new load configuration
            var lmd = new LoadMetadata(CatalogueRepository, "MyLoad");

            TableInfo ti = Import(tbl, lmd,logManager);
            
            var projectDirectory = SetupLoadDirectory(lmd);

            CreateCSVProcessTask(lmd,ti,"*.csv");
            
            //create a text file to load where we update Frank's favourite colour (it's a pk field) and we insert a new record (MrMurder)
            File.WriteAllText(
                Path.Combine(projectDirectory.ForLoading.FullName, "LoadMe.csv"),
@"Name,DateOfBirth,FavouriteColour
Frank,2001-01-01,Neon
MrMurder,2001-01-01,Yella");

            
            //the checks will probably need to be run as ddl admin because it involves creating _Archive table and trigger the first time

            //clean up RAW / STAGING etc and generally accept proposed cleanup operations
            var checker = new CheckEntireDataLoadProcess(lmd, new HICDatabaseConfiguration(lmd), new HICLoadConfigurationFlags(),CatalogueRepository.MEF);
            checker.Check(new AcceptAllCheckNotifier());

            //create a reader
            if (testCase == TestCase.LowPrivilegeLoaderAccount)
            {
                SetupLowPrivilegeUserRightsFor(ti, TestLowPrivilegePermissions.Reader|TestLowPrivilegePermissions.Writer);
                SetupLowPrivilegeUserRightsFor(db.Server.ExpectDatabase("DLE_STAGING"),TestLowPrivilegePermissions.All);
            }

            var dbConfig = new HICDatabaseConfiguration(lmd,testCase == TestCase.WithCustomTableNamer? new CustomINameDatabasesAndTablesDuringLoads():null);

            if(testCase == TestCase.WithCustomTableNamer)
                new PreExecutionChecker(lmd, dbConfig).Check(new AcceptAllCheckNotifier()); //handles staging database creation etc

            if (testCase == TestCase.WithDiffColumnIgnoreRegex)
                dbConfig.UpdateButDoNotDiff = new Regex("^FavouriteColour"); //do not diff FavouriteColour

            
            var loadFactory = new HICDataLoadFactory(
                lmd,
                dbConfig,
                new HICLoadConfigurationFlags(),
                CatalogueRepository,
                logManager
                );

            try
            {
                var exe = loadFactory.Create(new ThrowImmediatelyDataLoadEventListener());
            
                var exitCode = exe.Run(
                    new DataLoadJob(RepositoryLocator,"Go go go!", logManager, lmd, projectDirectory,new ThrowImmediatelyDataLoadEventListener(),dbConfig),
                    new GracefulCancellationToken());

                Assert.AreEqual(ExitCodeType.Success,exitCode);

                if (testCase == TestCase.AllPrimaryKeys)
                {
                    Assert.AreEqual(4, tbl.GetRowCount()); //Bob, Frank, Frank (with also pk Neon) & MrMurder
                    Assert.Pass();
                }
                if (testCase == TestCase.WithDiffColumnIgnoreRegex)
                {
                    Assert.AreEqual(3, tbl.GetRowCount()); //Bob, Frank (original since the diff was skipped), & MrMurder

                    //frank should be updated to like Neon instead of Orange
                    Assert.AreEqual(3, tbl.GetRowCount());
                    var frankOld =  tbl.GetDataTable().Rows.Cast<DataRow>().Single(r => (string)r["Name"] == "Frank");
                    Assert.AreEqual("Orange", frankOld["FavouriteColour"]);
                    Assert.Pass();
                }

                //frank should be updated to like Neon instead of Orange
                Assert.AreEqual(3,tbl.GetRowCount());
                var result = tbl.GetDataTable();
                var frank = result.Rows.Cast<DataRow>().Single(r => (string) r["Name"] == "Frank");
                Assert.AreEqual("Neon",frank["FavouriteColour"]);
                AssertHasDataLoadRunId(frank);

                //MrMurder is a new person who likes Yella
                var mrmurder = result.Rows.Cast<DataRow>().Single(r => (string)r["Name"] == "MrMurder");
                Assert.AreEqual("Yella", mrmurder["FavouriteColour"]);
                Assert.AreEqual(new DateTime(2001,01,01), mrmurder["DateOfBirth"]);
                AssertHasDataLoadRunId(mrmurder);

                //bob should be untouched (same values as before and no dataloadrunID)
                var bob = result.Rows.Cast<DataRow>().Single(r => (string)r["Name"] == "Bob");
                Assert.AreEqual("Pink", bob["FavouriteColour"]);
                Assert.AreEqual(new DateTime(2001, 01, 01), bob["DateOfBirth"]);
                Assert.AreEqual(DBNull.Value,bob[SpecialFieldNames.DataLoadRunID]);

                //MySql add default of now() on a table will auto populate all the column values with the the now() date while Sql Server will leave them as nulls
                if(databaseType != DatabaseType.MySql)
                    Assert.AreEqual(DBNull.Value, bob[SpecialFieldNames.ValidFrom]);
            }
            finally
            {
                Directory.Delete(lmd.LocationOfFlatFiles, true);

                foreach (Catalogue c in RepositoryLocator.CatalogueRepository.GetAllObjects<Catalogue>())
                    c.DeleteInDatabase();

                foreach (TableInfo t in RepositoryLocator.CatalogueRepository.GetAllObjects<TableInfo>())
                    t.DeleteInDatabase();

                foreach (LoadMetadata l in RepositoryLocator.CatalogueRepository.GetAllObjects<LoadMetadata>())
                    l.DeleteInDatabase();
            }

            if(testCase == TestCase.WithCustomTableNamer)
            {
                var db2 = db.Server.ExpectDatabase("BB_STAGING");
                if(db.Exists())
                    db2.Drop();
            }
        }

        [TestCase(DatabaseType.MicrosoftSQLServer)]
        [TestCase(DatabaseType.MySql)]
        public void DLELoadTwoTables(DatabaseType databaseType)
        {
            //setup the data tables
            var defaults = new ServerDefaults(CatalogueRepository);
            var logServer = defaults.GetDefaultFor(ServerDefaults.PermissableDefaults.LiveLoggingServer_ID);
            var logManager = new LogManager(logServer);

            var db = GetCleanedServer(databaseType);

            var dtParent = new DataTable();
            dtParent.Columns.Add("ID");
            dtParent.Columns.Add("Name");
            dtParent.Columns.Add("Height");
            dtParent.PrimaryKey = new[] {dtParent.Columns[0]};

            dtParent.Rows.Add("1", "Dave", "3.5");
            
            var dtChild = new DataTable();
            dtChild.Columns.Add("Parent_ID");
            dtChild.Columns.Add("ChildNumber");
            dtChild.Columns.Add("Name");
            dtChild.Columns.Add("DateOfBirth");
            dtChild.Columns.Add("Age");
            dtChild.Columns.Add("Height");

            dtChild.Rows.Add("1","1","Child1","2001-01-01","20","3.5");
            dtChild.Rows.Add("1","2","Child2","2002-01-01","19","3.4");
            
            dtChild.PrimaryKey = new[] {dtChild.Columns[0], dtChild.Columns[1]};

            //create the parent table based on the DataTable
            var parentTbl = db.CreateTable("Parent",dtParent);

            //go find the primary key column created
            var pkParentID = parentTbl.DiscoverColumn("ID");

            //forward declare this column as part of pk (will be used to specify foreign key
            var fkParentID = new DatabaseColumnRequest("Parent_ID", "int"){IsPrimaryKey = true};

            var args = new CreateTableArgs(
                db,
                "Child",
                null,
                dtChild,
                false,
                new Dictionary<DatabaseColumnRequest, DiscoveredColumn>()
                {
                    {fkParentID, pkParentID}
                },
                true);

            args.ExplicitColumnDefinitions = new[]
            {
                fkParentID
            };

            var childTbl = db.CreateTable(args);

            Assert.AreEqual(1, parentTbl.GetRowCount());
            Assert.AreEqual(2, childTbl.GetRowCount());

            //create a new load
            var lmd = new LoadMetadata(CatalogueRepository, "MyLoading2");
            
            TableInfo childTableInfo = Import(childTbl, lmd, logManager);
            TableInfo parentTableInfo = Import(parentTbl,lmd,logManager);

            var projectDirectory = SetupLoadDirectory(lmd);

            CreateCSVProcessTask(lmd,parentTableInfo,"parent.csv");
            CreateCSVProcessTask(lmd, childTableInfo, "child.csv");

            //create a text file to load where we update Frank's favourite colour (it's a pk field) and we insert a new record (MrMurder)
            File.WriteAllText(
                Path.Combine(projectDirectory.ForLoading.FullName, "parent.csv"),
@"ID,Name,Height
2,Man2,3.1
1,Dave,3.2");

            File.WriteAllText(
                Path.Combine(projectDirectory.ForLoading.FullName, "child.csv"),
@"Parent_ID,ChildNumber,Name,DateOfBirth,Age,Height
1,1,UpdC1,2001-01-01,20,3.5
2,1,NewC1,2000-01-01,19,null");
            
            
            //clean up RAW / STAGING etc and generally accept proposed cleanup operations
            var checker = new CheckEntireDataLoadProcess(lmd, new HICDatabaseConfiguration(lmd), new HICLoadConfigurationFlags(), CatalogueRepository.MEF);
            checker.Check(new AcceptAllCheckNotifier());

            var config = new HICDatabaseConfiguration(lmd);

            var loadFactory = new HICDataLoadFactory(
                lmd,
                config,
                new HICLoadConfigurationFlags(),
                CatalogueRepository,
                logManager
                );
            try
            {
                var exe = loadFactory.Create(new ThrowImmediatelyDataLoadEventListener());

                var exitCode = exe.Run(
                    new DataLoadJob(RepositoryLocator,"Go go go!", logManager, lmd, projectDirectory, new ThrowImmediatelyDataLoadEventListener(),config),
                    new GracefulCancellationToken());

                Assert.AreEqual(ExitCodeType.Success, exitCode);

                //should now be 2 parents (the original - who was updated) + 1 new one (Man2)
                Assert.AreEqual(2, parentTbl.GetRowCount());
                var result = parentTbl.GetDataTable();
                var dave = result.Rows.Cast<DataRow>().Single(r => (string)r["Name"] == "Dave");
                Assert.AreEqual(3.2f, dave["Height"]); //should now be only 3.2 inches high
                AssertHasDataLoadRunId(dave);

                //should be 3 children (Child1 who gets updated to be called UpdC1) and NewC1
                Assert.AreEqual(3, childTbl.GetRowCount());
                result = childTbl.GetDataTable();

                var updC1 = result.Rows.Cast<DataRow>().Single(r => (string)r["Name"] == "UpdC1");
                Assert.AreEqual(1, updC1["Parent_ID"]);
                Assert.AreEqual(1, updC1["ChildNumber"]);
                AssertHasDataLoadRunId(updC1);

                var newC1 = result.Rows.Cast<DataRow>().Single(r => (string)r["Name"] == "NewC1");
                Assert.AreEqual(2, newC1["Parent_ID"]);
                Assert.AreEqual(1, newC1["ChildNumber"]);
                Assert.AreEqual(DBNull.Value, newC1["Height"]); //the "null" in the input file should be DBNull.Value in the final database
                AssertHasDataLoadRunId(newC1);

            }
            finally
            {
                Directory.Delete(lmd.LocationOfFlatFiles,true);

                foreach (Catalogue c in RepositoryLocator.CatalogueRepository.GetAllObjects<Catalogue>())
                    c.DeleteInDatabase();

                foreach (TableInfo t in RepositoryLocator.CatalogueRepository.GetAllObjects<TableInfo>())
                    t.DeleteInDatabase();

                foreach (LoadMetadata l in RepositoryLocator.CatalogueRepository.GetAllObjects<LoadMetadata>())
                    l.DeleteInDatabase();
            }
        }

        private void AssertHasDataLoadRunId(DataRow row)
        {
            var o = row[SpecialFieldNames.DataLoadRunID];
            
            Assert.IsNotNull(o,"A row which was expected to have a hic_dataLoadRunID had null instead");
            Assert.AreNotEqual(DBNull.Value,o,"A row which was expected to have a hic_dataLoadRunID had DBNull.Value instead");
            Assert.GreaterOrEqual((int)o, 0);

            var d = row[SpecialFieldNames.ValidFrom];
            Assert.IsNotNull(d, "A row which was expected to have a hic_validFrom had null instead");
            Assert.AreNotEqual(DBNull.Value,d, "A row which was expected to have a hic_validFrom had DBNull.Value instead");
            
            //expect validFrom to be after 2 hours ago (to handle UTC / BST nonesense)
            Assert.GreaterOrEqual((DateTime)d, DateTime.Now.Subtract(new TimeSpan(2,0,0)));

        }

        private void CreateCSVProcessTask(LoadMetadata lmd, TableInfo ti, string regex)
        {
            var pt = new ProcessTask(CatalogueRepository, lmd, LoadStage.Mounting);
            pt.Path = typeof(AnySeparatorFileAttacher).FullName;
            pt.ProcessTaskType = ProcessTaskType.Attacher;
            pt.Name = "Load " + ti.GetRuntimeName();
            pt.SaveToDatabase();

            pt.CreateArgumentsForClassIfNotExists<AnySeparatorFileAttacher>();
            pt.SetArgumentValue("FilePattern", regex);
            pt.SetArgumentValue("Separator", ",");
            pt.SetArgumentValue("TableToLoad", ti);

            pt.Check(new ThrowImmediatelyCheckNotifier());
        }

        private HICProjectDirectory SetupLoadDirectory(LoadMetadata lmd)
        {
            var projectDirectory = HICProjectDirectory.CreateDirectoryStructure(new DirectoryInfo(TestContext.CurrentContext.WorkDirectory), "MyLoadDir", true);
            lmd.LocationOfFlatFiles = projectDirectory.RootPath.FullName;
            lmd.SaveToDatabase();

            return projectDirectory;
        }

        private TableInfo Import(DiscoveredTable tbl, LoadMetadata lmd, LogManager logManager)
        {
            logManager.CreateNewLoggingTaskIfNotExists(lmd.Name);

            //import TableInfos
            var importer = new TableInfoImporter(CatalogueRepository, tbl);
            TableInfo ti;
            ColumnInfo[] cis;
            importer.DoImport(out ti, out cis);

            //create Catalogue
            var forwardEngineer = new ForwardEngineerCatalogue(ti, cis, true);

            Catalogue cata;
            CatalogueItem[] cataItems;
            ExtractionInformation[] eis;
            forwardEngineer.ExecuteForwardEngineering(out cata, out cataItems, out eis);

            //make the catalogue use the load configuration
            cata.LoadMetadata_ID = lmd.ID;
            cata.LoggingDataTask = lmd.Name;
            Assert.IsNotNull(cata.LiveLoggingServer_ID); //catalogue should have one of these because of system defaults
            cata.SaveToDatabase();

            return ti;
        }
    }

    public class CustomINameDatabasesAndTablesDuringLoads:INameDatabasesAndTablesDuringLoads
    {
        public string GetDatabaseName(string rootDatabaseName, LoadBubble convention)
        {
            //RAW is AA, Staging is BB
            switch (convention)
            {
                case LoadBubble.Raw:
                    return "AA_RAW";
                case LoadBubble.Staging:
                    return "BB_STAGING";
                case LoadBubble.Live:
                case LoadBubble.Archive:
                    return rootDatabaseName;
                default:
                    throw new ArgumentOutOfRangeException("convention");
            }
        }

        public string GetName(string tableName, LoadBubble convention)
        {
            //all tables get called CC
            if (convention < LoadBubble.Live)
                return "CC";

            return tableName;
        }
    }
}
