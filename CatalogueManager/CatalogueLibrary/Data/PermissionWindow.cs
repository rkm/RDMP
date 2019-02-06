// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using CatalogueLibrary.Data.Cache;
using CatalogueLibrary.Data.Pipelines;
using CatalogueLibrary.Repositories;
using MapsDirectlyToDatabaseTable;
using MapsDirectlyToDatabaseTable.Attributes;
using ReusableLibraryCode;
using ReusableLibraryCode.Annotations;

namespace CatalogueLibrary.Data
{
    /// <summary>
    /// Describes a period of time in which a given act can take place (e.g. only cache data from the MRI imaging web service during the hours of 11pm - 5am so as not to 
    /// disrupt routine hospital use).  Also serves as a Locking point for job control.  Once an IPermissionWindow is in use by a process (e.g. Caching Pipeline) then it
    /// is not available to other processes (e.g. loading or other caching pipelines that share the same IPermissionWindow).
    /// </summary>
    public class PermissionWindow : VersionedDatabaseEntity, IPermissionWindow
    {
        #region Database Properties

        private string _name;
        private string _description;
        private bool _requiresSynchronousAccess;
        
        /// <inheritdoc/>
        [NotNull]
        [Unique]
        public string Name
        {
            get { return _name; }
            set { SetField(ref  _name, value); }
        }

        /// <inheritdoc/>
        public string Description
        {
            get { return _description; }
            set { SetField(ref  _description, value); }
        }

        /// <inheritdoc/>
        public bool RequiresSynchronousAccess
        {
            get { return _requiresSynchronousAccess; }
            set { SetField(ref  _requiresSynchronousAccess, value); }
        }
        
        /// <summary>
        /// The serialized string of <see cref="PermissionWindowPeriods"/> which is written/read from the catalogue database
        /// </summary>
        public string PermissionPeriodConfig {
            get { return SerializePermissionWindowPeriods(); }
            set
            {
                DeserializePermissionWindowPeriods(value);
                OnPropertyChanged();
            }
        }

        #endregion

        #region Relationships

        /// <inheritdoc/>
        [NoMappingToDatabase]
        public IEnumerable<ICacheProgress> CacheProgresses {
            get { return Repository.GetAllObjectsWithParent<CacheProgress>(this); }
        }
        #endregion
        
        /// <inheritdoc/>
        [NoMappingToDatabase]
        public List<PermissionWindowPeriod> PermissionWindowPeriods { get; private set; }

        private string SerializePermissionWindowPeriods()
        {
            var serializer = new XmlSerializer(typeof (List<PermissionWindowPeriod>));
            using (var output = new StringWriter())
            {
                serializer.Serialize(output, PermissionWindowPeriods);
                return output.ToString();
            }
        }

        private void DeserializePermissionWindowPeriods(string permissionPeriodConfig)
        {
            if (string.IsNullOrWhiteSpace(permissionPeriodConfig))
                PermissionWindowPeriods = new List<PermissionWindowPeriod>();
            else
            {
                var deserializer = new XmlSerializer(typeof (List<PermissionWindowPeriod>));
                PermissionWindowPeriods = deserializer.Deserialize(new StringReader(permissionPeriodConfig)) as List<PermissionWindowPeriod>;
            }
        }
        /// <inheritdoc/>
        public bool WithinPermissionWindow()
        {
            if (!PermissionWindowPeriods.Any())
                return true;

            return WithinPermissionWindow(DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public virtual bool WithinPermissionWindow(DateTime dateTimeUTC)
        {
            
            if (!PermissionWindowPeriods.Any())
                return true;

            return PermissionWindowPeriods.Any(permissionPeriod => permissionPeriod.Contains(dateTimeUTC));
        }

        /// <summary>
        /// Create a new time window in which you can restrict things (caching, loading etc) from happening outside
        /// </summary>
        /// <param name="repository"></param>
        public PermissionWindow(ICatalogueRepository repository)
        {
            repository.InsertAndHydrate(this,new Dictionary<string, object>
            {
                {"PermissionPeriodConfig", DBNull.Value}
            });
        }

        internal PermissionWindow(ICatalogueRepository repository, DbDataReader r)
            : base(repository, r)
        {
            Name = r["Name"].ToString();
            Description = r["Description"].ToString();
            RequiresSynchronousAccess = Convert.ToBoolean(r["RequiresSynchronousAccess"]);
            PermissionPeriodConfig = r["PermissionPeriodConfig"].ToString();
        }
        
        /// <inheritdoc/>
        public override string ToString()
        {
            return (string.IsNullOrWhiteSpace(Name) ? "Unnamed" : Name) + "(ID = " + ID + ")";
        }
        
        /// <inheritdoc/>
        public void SetPermissionWindowPeriods(List<PermissionWindowPeriod> windowPeriods)
        {
            PermissionWindowPeriods = windowPeriods;
            PermissionPeriodConfig = SerializePermissionWindowPeriods();
        }
    }
}