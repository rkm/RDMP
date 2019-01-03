using System.Collections.Generic;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.Data.EntityNaming;
using CatalogueLibrary.Repositories;
using DataLoadEngine.DatabaseManagement.EntityNaming;
using DataLoadEngine.DatabaseManagement.Operations;
using DataLoadEngine.LoadExecution.Components;
using HIC.Logging;
using ReusableLibraryCode.Progress;

namespace DataLoadEngine.Job
{
    /// <summary>
    /// See DataLoadJob
    /// </summary>
    public interface IDataLoadJob : IDataLoadEventListener, IDisposeAfterDataLoad
    {
        string Description { get; }
        IDataLoadInfo DataLoadInfo { get; }
        IHICProjectDirectory HICProjectDirectory { get; set; }
        int JobID { get; set; }
        ILoadMetadata LoadMetadata { get; }
        string ArchiveFilepath { get; }

        /// <summary>
        /// Optional externally provided object to drive the data load.  For example if you have an explicit list of objects in memory to process and
        /// a custom Attacher which expects to be magically provided with this list then communicate the list to the Attacher via this property.
        /// </summary>
        object Payload { get; set; }

        List<ITableInfo> RegularTablesToLoad { get; }
        List<ITableInfo> LookupTablesToLoad { get; }
        
        IRDMPPlatformRepositoryServiceLocator RepositoryLocator { get; }

        void StartLogging();
        void CloseLogging();

        HICDatabaseConfiguration Configuration { get; }
        
        /// <summary>
        /// Orders the job to create the tables it requires in the given stage (e.g. RAW/STAGING), the job will also take ownership of the cloner for the purposes
        /// of disposal (DO NOT DISPOSE OF CLONER YOURSELF)
        /// </summary>
        /// <param name="cloner"></param>
        /// <param name="stage"></param>
        void CreateTablesInStage(DatabaseCloner cloner,LoadBubble stage);

        void PushForDisposal(IDisposeAfterDataLoad disposeable);
    }
}