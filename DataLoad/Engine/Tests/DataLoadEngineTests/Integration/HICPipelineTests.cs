﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.Repositories;
using DataLoadEngine.DataProvider.FromCache;
using DataLoadEngine.Job;
using FAnsi.Discovery;
using LoadModules.Generic.DataFlowSources;
using NUnit.Framework;
using RDMPAutomationService.Options;
using RDMPAutomationService.Runners;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DataAccess;
using ReusableLibraryCode.Progress;
using Tests.Common;

namespace DataLoadEngineTests.Integration
{
    public class HICPipelineTests : DatabaseTests
    {
        internal class CatalogueEntities : IDisposable
        {
            public Catalogue Catalogue { get; private set; }
            public LoadMetadata LoadMetadata { get; private set; }
            public ColumnInfo ColumnInfo { get; private set; }
            public TableInfo TableInfo { get; private set; }

            public DataAccessCredentials Credentials { get; private set; }

            public CatalogueEntities()
            {
                Catalogue = null;
                LoadMetadata = null;
                ColumnInfo = null;
                TableInfo = null;
            }

            public void Create(CatalogueRepository repository, DiscoveredDatabase database,
                IHICProjectDirectory hicProjectDirectory)
            {
                TableInfo = new TableInfo(repository, "TestData")
                {
                    Server = database.Server.Name,
                    Database = database.GetRuntimeName()
                };
                TableInfo.SaveToDatabase();

                if (!string.IsNullOrWhiteSpace(database.Server.ExplicitUsernameIfAny))
                    Credentials = new DataAccessCredentialsFactory(repository).Create(TableInfo,
                        database.Server.ExplicitUsernameIfAny, database.Server.ExplicitPasswordIfAny,
                        DataAccessContext.Any);


                ColumnInfo = new ColumnInfo(repository, "Col1", "int", TableInfo)
                {
                    IsPrimaryKey = true
                };
                ColumnInfo.SaveToDatabase();

                LoadMetadata = new LoadMetadata(repository, "HICLoadPipelineTests")
                {
                    LocationOfFlatFiles = hicProjectDirectory.RootPath.FullName
                };
                LoadMetadata.SaveToDatabase();

                Catalogue = new Catalogue(repository, "HICLoadPipelineTests")
                {
                    LoggingDataTask = "Test",
                    LoadMetadata_ID = LoadMetadata.ID
                };
                Catalogue.SaveToDatabase();

                var catalogueItem = new CatalogueItem(repository, Catalogue, "Test");
                catalogueItem.SetColumnInfo(ColumnInfo);

                SetupLoadProcessTasks(repository);
            }

            public void Dispose()
            {
                if (Catalogue != null)
                    Catalogue.DeleteInDatabase();

                if (LoadMetadata != null)
                    LoadMetadata.DeleteInDatabase();

                if (ColumnInfo != null)
                    ColumnInfo.DeleteInDatabase();

                if (TableInfo != null)
                    TableInfo.DeleteInDatabase();

                if (Credentials != null)
                    Credentials.DeleteInDatabase();
            }

            private void SetupLoadProcessTasks(ICatalogueRepository catalogueRepository)
            {
                var attacherTask = new ProcessTask(catalogueRepository, LoadMetadata, LoadStage.Mounting)
                {
                    Name = "Attach CSV file",
                    Order = 1,
                    Path = "LoadModules.Generic.Attachers.AnySeparatorFileAttacher",
                    ProcessTaskType = ProcessTaskType.Attacher
                };
                attacherTask.SaveToDatabase();

                // Not assigned to a variable as they will be magically available through the repository
                var processTaskArgs = new List<Tuple<string, string, Type>>
                {
                    new Tuple<string, string, Type>("FilePattern", "1.csv", typeof (string)),
                    new Tuple<string, string, Type>("TableName", "TestData", typeof (string)),
                    new Tuple<string, string, Type>("ForceHeaders", null, typeof (string)),
                    new Tuple<string, string, Type>("IgnoreQuotes", null, typeof (bool)),
                    new Tuple<string, string, Type>("IgnoreBlankLines", null, typeof (bool)),
                    new Tuple<string, string, Type>("ForceHeadersReplacesFirstLineInFile", null, typeof (bool)),
                    new Tuple<string, string, Type>("SendLoadNotRequiredIfFileNotFound", "false", typeof (bool)),
                    new Tuple<string, string, Type>("Separator", ",", typeof (string)),
                    new Tuple<string, string, Type>("TableToLoad", null, typeof (TableInfo)),
                    new Tuple<string, string, Type>("BadDataHandlingStrategy", BadDataHandlingStrategy.ThrowException.ToString(), typeof (BadDataHandlingStrategy)),
                    new Tuple<string, string, Type>("ThrowOnEmptyFiles", "true", typeof (bool)),
                    new Tuple<string, string, Type>("AttemptToResolveNewLinesInRecords", "true", typeof (bool)),
                    new Tuple<string, string, Type>("MaximumErrorsToReport", "0", typeof (int))
                };
                

                foreach (var tuple in processTaskArgs)
                {
                    var pta = new ProcessTaskArgument(catalogueRepository, attacherTask)
                    {
                        Name = tuple.Item1,
                        Value = tuple.Item2
                    };
                    pta.SetType(tuple.Item3);
                    pta.SaveToDatabase();
                }
            }
        }

        internal class DatabaseHelper : IDisposable
        {
            private DiscoveredServer _server;
            private string _testDatabaseSuffix;

            public DiscoveredDatabase DatabaseToLoad { get; private set; }
            public void SetUp(DiscoveredServer server)
            {
                _server = server;

                var databaseToLoadName = "HICPipelineTests" + _testDatabaseSuffix;
                
                // Create the databases
                server.ExpectDatabase(databaseToLoadName).Create(true);
                server.ChangeDatabase(databaseToLoadName);

                // Create the dataset table
                DatabaseToLoad = server.ExpectDatabase(databaseToLoadName);
                using (var con = DatabaseToLoad.Server.GetConnection())
                {
                    con.Open();
                    const string createDatasetTableQuery =
                        "CREATE TABLE TestData ([Col1] [int], [hic_dataLoadRunID] [int] NULL, [hic_validFrom] [datetime] NULL, CONSTRAINT [PK_TestData] PRIMARY KEY CLUSTERED ([Col1] ASC))";
                    const string addValidFromDefault =
                        "ALTER TABLE TestData ADD CONSTRAINT [DF_TestData__hic_validFrom]  DEFAULT (getdate()) FOR [hic_validFrom]";
                    var cmd = DatabaseCommandHelper.GetCommand(createDatasetTableQuery, con);
                    cmd.ExecuteNonQuery();

                    cmd = DatabaseCommandHelper.GetCommand(addValidFromDefault, con);
                    cmd.ExecuteNonQuery();
                }

                // Ensure the dataset table has been created
                var datasetTable = DatabaseToLoad.ExpectTable("TestData");
                Assert.IsTrue(datasetTable.Exists());
            }

            public void Dispose()
            {
                if (DatabaseToLoad == null)
                    return;
                
                if (DatabaseToLoad.Exists())
                    DatabaseToLoad.ForceDrop();

                // check if RAW has been created and remove it
                var raw = _server.ExpectDatabase(DatabaseToLoad.GetRuntimeName() + "_RAW");
                if (raw.Exists())
                    raw.ForceDrop();
            }
        }

        [Test]
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TestSingleJob(bool overrideRAW, bool sendDodgyCredentials)
        {
            if (sendDodgyCredentials && !overrideRAW)
                throw new NotSupportedException("Cannot send dodgy credentials if you aren't overriding RAW");

            ServerDefaults defaults = new ServerDefaults(CatalogueRepository);
            var oldDefault = defaults.GetDefaultFor(ServerDefaults.PermissableDefaults.RAWDataLoadServer);

            var testDirPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var testDir = Directory.CreateDirectory(testDirPath);
            var server = DiscoveredServerICanCreateRandomDatabasesAndTablesOn;

            var catalogueEntities = new CatalogueEntities();
            var databaseHelper = new DatabaseHelper();
            ExternalDatabaseServer external = null;

            try
            {
                // Set up the dataset's project directory and add the CSV file to ForLoading
                var hicProjectDirectory = HICProjectDirectory.CreateDirectoryStructure(testDir, "TestDataset");
                File.WriteAllText(Path.Combine(hicProjectDirectory.ForLoading.FullName, "1.csv"),
                    "Col1\r\n1\r\n2\r\n3\r\n4");

                databaseHelper.SetUp(server);

                // Create the Catalogue entities for the dataset
                catalogueEntities.Create(CatalogueRepository, databaseHelper.DatabaseToLoad, hicProjectDirectory);
                
                if (overrideRAW)
                {
                    external = new ExternalDatabaseServer(CatalogueRepository, "RAW Server");
                    external.SetProperties(DiscoveredServerICanCreateRandomDatabasesAndTablesOn.ExpectDatabase("master"));

                    if (sendDodgyCredentials)
                    {
                        external.Username = "IveGotaLovely";
                        external.Password = "BunchOfCoconuts";
                    }
                    external.SaveToDatabase();

                    defaults.SetDefault(ServerDefaults.PermissableDefaults.RAWDataLoadServer, external);
                }

                var options = new DleOptions();
                options.LoadMetadata = catalogueEntities.LoadMetadata.ID;
                options.Command = CommandLineActivity.check;

                //run checks (with ignore errors if we are sending dodgy credentials)
                new RunnerFactory().CreateRunner(options).Run(RepositoryLocator, new ThrowImmediatelyDataLoadEventListener(), 
                    sendDodgyCredentials?
                    (ICheckNotifier) new IgnoreAllErrorsCheckNotifier(): new AcceptAllCheckNotifier(), new GracefulCancellationToken());

                //run load
                options.Command = CommandLineActivity.run;
                var runner = new RunnerFactory().CreateRunner(options);

                
                if (sendDodgyCredentials)
                {
                    var ex = Assert.Throws<Exception>(()=>runner.Run(RepositoryLocator, new ThrowImmediatelyDataLoadEventListener(), new AcceptAllCheckNotifier(), new GracefulCancellationToken()));
                    Assert.IsTrue(ex.InnerException.Message.Contains("Login failed for user 'IveGotaLovely'"),"Error message did not contain expected text");
                    return;
                }
                else
                    runner.Run(RepositoryLocator, new ThrowImmediatelyDataLoadEventListener(), new AcceptAllCheckNotifier(), new GracefulCancellationToken());


                var archiveFile = hicProjectDirectory.ForArchiving.EnumerateFiles("*.zip").OrderByDescending(f=>f.FullName).FirstOrDefault();
                Assert.NotNull(archiveFile,"Archive file has not been created by the load.");
                Assert.IsFalse(hicProjectDirectory.ForLoading.EnumerateFileSystemInfos().Any());

            }
            finally
            {
                //reset the original RAW server
                defaults.SetDefault(ServerDefaults.PermissableDefaults.RAWDataLoadServer, oldDefault);

                if (external != null)
                    external.DeleteInDatabase();

                testDir.Delete(true);

                databaseHelper.Dispose();
                catalogueEntities.Dispose();
            }
        }
    }

    public class TestCacheFileRetriever : CachedFileRetriever
    {
        public override void Initialize(IHICProjectDirectory hicProjectDirectory, DiscoveredDatabase dbInfo)
        {
            
        }

        public override ExitCodeType Fetch(IDataLoadJob dataLoadJob, GracefulCancellationToken cancellationToken)
        {
            var hicProjectDirectory = dataLoadJob.HICProjectDirectory;
            var fileToMove = hicProjectDirectory.Cache.EnumerateFiles("*.csv").FirstOrDefault();
            if (fileToMove == null)
                return ExitCodeType.OperationNotRequired;

            File.Move(fileToMove.FullName, Path.Combine(hicProjectDirectory.ForLoading.FullName, "1.csv"));
            return ExitCodeType.Success;
        }
    }
}