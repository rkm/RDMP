﻿using System;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.Repositories;
using CatalogueLibrary.Triggers;
using DataLoadEngine.Migration;
using DataQualityEngine.Reports;
using RDMPAutomationService.Interfaces;
using ReusableLibraryCode.Progress;

namespace RDMPAutomationService.Logic.DQE
{
    /// <summary>
    /// Automation task that runs a the Data Quality Engine on a single Catalogue.
    /// </summary>
    public class AutomatedDQERun : IAutomateable
    {
        private readonly AutomationServiceSlot _slottedService;
        private readonly Catalogue _catalogueToRun;
        
        public AutomatedDQERun(AutomationServiceSlot slottedService, Catalogue catalogueToRun)
        {
            _slottedService = slottedService;
            _catalogueToRun = catalogueToRun;
        }
        public OnGoingAutomationTask GetTask()
        {
            var toReturn = new OnGoingAutomationTask(

                _slottedService.AddNewJob(AutomationJobType.DQE, "DQE evaluation of " + _catalogueToRun),this);

            toReturn.Job.LockCatalogues(new[] { _catalogueToRun });

            return toReturn;
        }

        public void RunTask(OnGoingAutomationTask task)
        {
            try
            {
                new CatalogueConstraintReport(_catalogueToRun, SpecialFieldNames.DataLoadRunID).GenerateReport(_catalogueToRun, new ToMemoryDataLoadEventListener(true), task.CancellationTokenSource.Token,task.Job);

                //if it suceeded
                if (task.Job.LastKnownStatus == AutomationJobStatus.Finished)
                    task.Job.DeleteInDatabase(); //causes implicit unlocking of Catalogues
            }
            catch (Exception e)
            {
                //log errors in the automation server
                new AutomationServiceException((ICatalogueRepository) task.Repository, e);
                task.Job.LastKnownStatus = AutomationJobStatus.Crashed;
                task.Job.SaveToDatabase();
            }
        }
    }
}