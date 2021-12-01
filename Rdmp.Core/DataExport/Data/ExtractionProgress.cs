﻿using MapsDirectlyToDatabaseTable;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Rdmp.Core.DataExport.Data
{
    /// <inheritdoc cref="IExtractionProgress"/>
    public class ExtractionProgress : DatabaseEntity, IExtractionProgress
    {
        #region Database Properties

        private int _selectedDataSets_ID;
        private DateTime? _progress;
        private int _extractionInformation_ID;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private int _numberOfDaysPerBatch;
        #endregion

        /// <inheritdoc/>
        public int SelectedDataSets_ID
        {
            get { return _selectedDataSets_ID; }
            set { SetField(ref _selectedDataSets_ID, value); }
        }

        /// <inheritdoc/>
        public DateTime? ProgressDate
        {
            get { return _progress; }
            set { SetField(ref _progress, value); }
        }

        /// <inheritdoc/>
        public DateTime? StartDate
        {
            get { return _startDate; }
            set { SetField(ref _startDate, value); }
        }

        /// <inheritdoc/>
        public DateTime? EndDate
        {
            get { return _endDate; }
            set { SetField(ref _endDate, value); }
        }

        /// <inheritdoc/>
        public int NumberOfDaysPerBatch
        {
            get { return _numberOfDaysPerBatch; }
            set { SetField(ref _numberOfDaysPerBatch, value); }
        }

        /// <inheritdoc/>
        public int ExtractionInformation_ID
        {
            get { return _extractionInformation_ID; }
            set { SetField(ref _extractionInformation_ID, value); }
        }

        #region Relationships
        /// <inheritdoc/>
        [NoMappingToDatabase]
        public ISelectedDataSets SelectedDataSets { get => DataExportRepository.GetObjectByID<SelectedDataSets>(SelectedDataSets_ID); }

        /// <inheritdoc/>
        [NoMappingToDatabase]
        public ExtractionInformation ExtractionInformation { get => DataExportRepository.CatalogueRepository.GetObjectByID<ExtractionInformation>(ExtractionInformation_ID); }
        #endregion

        public ExtractionProgress(IDataExportRepository repository, ISelectedDataSets sds)
        {
            var cata = sds.GetCatalogue();
            var coverageColId = cata?.TimeCoverage_ExtractionInformation_ID;

            if (!coverageColId.HasValue)
            {
                throw new ArgumentException($"Cannot create ExtractionProgress because Catalogue {cata} does not have a time coverage column");
            }

            repository.InsertAndHydrate(this, new Dictionary<string, object>()
            {
                { "SelectedDataSets_ID",sds.ID},
                { "ExtractionInformation_ID",coverageColId},
                { "NumberOfDaysPerBatch",365}
            });

            if (ID == 0 || Repository != repository)
                throw new ArgumentException("Repository failed to properly hydrate this class");
        }
        public ExtractionProgress(IDataExportRepository repository, DbDataReader r) : base(repository, r)
        {
            SelectedDataSets_ID = Convert.ToInt32(r["SelectedDataSets_ID"]);
            ProgressDate = ObjectToNullableDateTime(r["ProgressDate"]);
            StartDate = ObjectToNullableDateTime(r["StartDate"]);
            EndDate = ObjectToNullableDateTime(r["EndDate"]);
            ExtractionInformation_ID = Convert.ToInt32(r["ExtractionInformation_ID"]);
            NumberOfDaysPerBatch = Convert.ToInt32(r["NumberOfDaysPerBatch"]);
        }
    }
}
