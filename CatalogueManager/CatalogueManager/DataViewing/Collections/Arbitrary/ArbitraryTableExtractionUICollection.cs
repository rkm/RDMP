// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Dashboarding;
using CatalogueManager.AutoComplete;
using CatalogueManager.ObjectVisualisation;
using FAnsi;
using FAnsi.Discovery;
using FAnsi.Discovery.QuerySyntax;
using ReusableLibraryCode;
using ReusableLibraryCode.DataAccess;

namespace CatalogueManager.DataViewing.Collections.Arbitrary
{
    internal class ArbitraryTableExtractionUICollection : PersistableObjectCollection,IViewSQLAndResultsCollection, IDataAccessPoint, IDataAccessCredentials
    {
        private DiscoveredTable _table;
        
        public DatabaseType DatabaseType { get; private set; }

        Dictionary<string, string> _arguments = new Dictionary<string, string>();
        private const string DatabaseKey = "Database";
        private const string ServerKey = "Server";
        private const string TableKey = "Table";
        private const string DatabaseTypeKey = "DatabaseType";

        public string Username { get; private set; }
        public string Password { get; set; }
        public string GetDecryptedPassword()
        {
            return Password;
        }

        /// <summary>
        /// Needed for deserialization
        /// </summary>
        public ArbitraryTableExtractionUICollection()
        {
            
        }

        public ArbitraryTableExtractionUICollection(DiscoveredTable table) :this()
        {
            _table = table;
            _arguments.Add(ServerKey,_table.Database.Server.Name);
            _arguments.Add(DatabaseKey, _table.Database.GetRuntimeName());
            _arguments.Add(TableKey,_table.GetRuntimeName());
            DatabaseType = table.Database.Server.DatabaseType;

            _arguments.Add(DatabaseTypeKey,DatabaseType.ToString());


            Username = table.Database.Server.ExplicitUsernameIfAny;
            Password = table.Database.Server.ExplicitPasswordIfAny;
        }
        /// <nheritdoc/>
        public override string SaveExtraText()
        {
            return Helper.SaveDictionaryToString(_arguments);
        }

        public override void LoadExtraText(string s)
        {
            _arguments = Helper.LoadDictionaryFromString(s);

            DatabaseType = (DatabaseType)Enum.Parse(typeof(DatabaseType), _arguments[DatabaseTypeKey]);

            var builder = DatabaseCommandHelper.For(DatabaseType).GetConnectionStringBuilder(Server,Database,null,null);

            var server = new DiscoveredServer(builder);
            _table = server.ExpectDatabase(Database).ExpectTable(_arguments[TableKey]);
        }
        
        public IEnumerable<DatabaseEntity> GetToolStripObjects()
        {
            yield break;
        }

        public IDataAccessPoint GetDataAccessPoint()
        {
            return this;
        }

        public string GetSql()
        {
            var response = _table.GetQuerySyntaxHelper().HowDoWeAchieveTopX(100);
            
            switch (response.Location)
            {
                case QueryComponent.SELECT:
                    return "Select " + response.SQL + " * from " + _table.GetFullyQualifiedName();
                case QueryComponent.WHERE:
                    return "Select * from " + _table.GetFullyQualifiedName() + " WHERE " + response.SQL;
                case QueryComponent.Postfix:
                    return "Select * from " + _table.GetFullyQualifiedName() + " " + response.SQL ;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public string GetTabName()
        {
            return "View " + _table.GetRuntimeName();
        }

        public void AdjustAutocomplete(AutoCompleteProvider autoComplete)
        {
            autoComplete.Add(_table);
        }

        public string Server { get { return _arguments[ServerKey]; } }
        public string Database { get { return _arguments[DatabaseKey]; } }

        

        public IDataAccessCredentials GetCredentialsIfExists(DataAccessContext context)
        {
            //we have our own credentials if we do
            return string.IsNullOrWhiteSpace(Username)? null:this;
        }

        public IQuerySyntaxHelper GetQuerySyntaxHelper()
        {
            return _table.GetQuerySyntaxHelper();
        }
    }
}