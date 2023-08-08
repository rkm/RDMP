// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad.Engine.DatabaseManagement.EntityNaming;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.DataLoad.Engine.LoadExecution;
using Rdmp.Core.Logging;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace Rdmp.Core.DataLoad.Engine.LoadProcess;

/// <summary>
/// Container class for an IDataLoadExecution.  This class records the ILoadMetadata that is being executed and the current state (whether it has crashed etc).
/// When you call run then an IDataLoadJob will be generated by the JobProvider will be executed by the LoadExecution (See IDataLoadExecution).
/// </summary>
public class DataLoadProcess : IDataLoadProcess, IDataLoadOperation
{
    /// <summary>
    /// Provides jobs for the data load process, allows different strategies for what jobs will be loaded e.g. single job, scheduled
    /// </summary>
    public IJobFactory JobProvider { get; set; }

    /// <summary>
    /// The load execution that will be used to load the jobs provided by the JobProvider
    /// </summary>
    public IDataLoadExecution LoadExecution { get; private set; }

    public ExitCodeType? ExitCode { get; private set; }
    public Exception Exception { get; private set; }

    private readonly IRDMPPlatformRepositoryServiceLocator _repositoryLocator;
    protected readonly ILoadMetadata LoadMetadata;
    protected readonly IDataLoadEventListener DataLoadEventListener;
    private readonly HICDatabaseConfiguration _configuration;
    protected readonly ILogManager LogManager;

    private readonly ICheckable _preExecutionChecker;

    public DataLoadProcess(IRDMPPlatformRepositoryServiceLocator repositoryLocator, ILoadMetadata loadMetadata,
        ICheckable preExecutionChecker, ILogManager logManager, IDataLoadEventListener dataLoadEventListener,
        IDataLoadExecution loadExecution, HICDatabaseConfiguration configuration)
    {
        _repositoryLocator = repositoryLocator;
        LoadMetadata = loadMetadata;
        DataLoadEventListener = dataLoadEventListener;
        _configuration = configuration;
        LoadExecution = loadExecution;
        _preExecutionChecker = preExecutionChecker;
        LogManager = logManager;
        ExitCode = ExitCodeType.Success;

        JobProvider = new JobFactory(loadMetadata, logManager);
    }

    public virtual ExitCodeType Run(GracefulCancellationToken loadCancellationToken, object payload = null)
    {
        PerformPreExecutionChecks();

        // create job
        var job = JobProvider.Create(_repositoryLocator, DataLoadEventListener, _configuration);

        // if job is null, there are no more jobs to submit
        if (job == null)
            return ExitCodeType.OperationNotRequired;

        job.Payload = payload;
        job.PersistentRaw = Rdmp.Core.Curation.Data.DataLoad.LoadMetadata.UsesPersistentRaw(LoadMetadata);

        return LoadExecution.Run(job, loadCancellationToken);
    }

    private void PerformPreExecutionChecks()
    {
        try
        {
            DataLoadEventListener.OnNotify(this,
                new NotifyEventArgs(ProgressEventType.Information, "Performing pre-execution checks"));
            var thrower = new ThrowImmediatelyCheckNotifier { WriteToConsole = false };
            _preExecutionChecker.Check(thrower);
        }
        catch (Exception e)
        {
            Exception = e;
            ExitCode = ExitCodeType.Error;
        }
    }
}