// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data.Common;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Cohort;
using CatalogueLibrary.Repositories;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.Repositories;
using MapsDirectlyToDatabaseTable;

namespace DataExportLibrary.Data
{

    /// <summary>
    /// Records the fact that a given Cohort Identification Configuration (query that identifies a cohort) is associated with a given Project.  You can have multiple
    /// associated configurations in a given project (e.g. cases, controls, time based etc).  You can also associate the same configuration with multiple Projects if
    /// you need to.
    /// </summary>
    public class ProjectCohortIdentificationConfigurationAssociation : DatabaseEntity, IMasqueradeAs,IDeletableWithCustomMessage
    {
        #region Database Properties

        private int _project_ID;
        private int _cohortIdentificationConfiguration_ID;
        #endregion

        public int Project_ID
        {
            get { return _project_ID; }
            set { SetField(ref _project_ID, value); }
        }
        public int CohortIdentificationConfiguration_ID
        {
            get { return _cohortIdentificationConfiguration_ID; }
            set { SetField(ref _cohortIdentificationConfiguration_ID, value); }
        }


        #region Relationships
        [NoMappingToDatabase]
        public Project Project { get { return Repository.GetObjectByID<Project>(Project_ID); } }
        
        [NoMappingToDatabase]
        public CohortIdentificationConfiguration CohortIdentificationConfiguration { get { return ((DataExportRepository)Repository).CatalogueRepository.GetObjectByID<CohortIdentificationConfiguration>(CohortIdentificationConfiguration_ID); } }

        #endregion


        public ProjectCohortIdentificationConfigurationAssociation(IDataExportRepository repository, Project project, CohortIdentificationConfiguration cic)
        {
            repository.InsertAndHydrate(this, new Dictionary<string, object>()
            {
                {"Project_ID",project.ID},
                {"CohortIdentificationConfiguration_ID",cic.ID}
            });

            if (ID == 0 || Repository != repository)
                throw new ArgumentException("Repository failed to properly hydrate this class");
        }
        internal ProjectCohortIdentificationConfigurationAssociation(IDataExportRepository repository, DbDataReader r)
            : base(repository, r)
        {
            Project_ID = Convert.ToInt32(r["Project_ID"]);
            CohortIdentificationConfiguration_ID = Convert.ToInt32(r["CohortIdentificationConfiguration_ID"]);
        }

        private CohortIdentificationConfiguration _cachedCic = null;
        public override string ToString()
        {
            return GetCohortIdentificationConfigurationCached().Name;
        }

        public string GetDeleteMessage()
        {
            return "disassociate this CohortIdentificationConfiguration from the Project";
        }

        public object MasqueradingAs()
        {
            return GetCohortIdentificationConfigurationCached();
        }

        public CohortIdentificationConfiguration GetCohortIdentificationConfigurationCached()
        {
            //if we never knew it or it changed
            if (_cachedCic == null || _cachedCic.ID != CohortIdentificationConfiguration_ID)
                _cachedCic = CohortIdentificationConfiguration;//fetch it
            
            return _cachedCic;
        }

        public void InjectKnownCohortIdentificationConfiguration(CohortIdentificationConfiguration cic)
        {
            _cachedCic = cic;
        }
    }
}