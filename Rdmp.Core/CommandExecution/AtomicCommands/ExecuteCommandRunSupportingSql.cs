﻿// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataViewing;

namespace Rdmp.Core.CommandExecution.AtomicCommands
{
    /// <summary>
    /// Runs the SQL in <see cref="SupportingSQLTable"/> and displays output (if a single table is returned)
    /// </summary>
    public class ExecuteCommandRunSupportingSql : BasicCommandExecution
    {

        public ExecuteCommandRunSupportingSql(IBasicActivateItems activator, SupportingSQLTable supportingSQLTable) : base(activator)
        {
            SupportingSQLTable = supportingSQLTable;

            if(SupportingSQLTable.ExternalDatabaseServer_ID == null)
            {
                SetImpossible("No server is configured on SupportingSQLTable");
                return;
            }

            if (string.IsNullOrWhiteSpace(SupportingSQLTable.SQL))
            {
                SetImpossible($"No SQL is defined for {SupportingSQLTable}");
                return;
            }
        }

        public SupportingSQLTable SupportingSQLTable { get; }

        public override void Execute()
        {
            base.Execute();
            var collection = new ViewSupportingSqlCollection(SupportingSQLTable);

            // are we in interactive mode with a query
            if(!string.IsNullOrWhiteSpace(SupportingSQLTable.SQL) && BasicActivator.IsInteractive)
            {
                // does the query look dangerous, if so give them a choice to back out
                bool requireConfirm =
                    SupportingSQLTable.SQL.Contains("update", System.StringComparison.CurrentCultureIgnoreCase) ||
                    SupportingSQLTable.SQL.Contains("delete", System.StringComparison.CurrentCultureIgnoreCase) ||
                    SupportingSQLTable.SQL.Contains("drop", System.StringComparison.CurrentCultureIgnoreCase) ||
                    SupportingSQLTable.SQL.Contains("truncate", System.StringComparison.CurrentCultureIgnoreCase);

                if(requireConfirm)
                {
                    if(!BasicActivator.YesNo("Running this SQL may make changes to your database, really run?", "Run SQL"))
                    {
                        return;
                    }
                }
            }

            BasicActivator.ShowData(collection);
        }
    }
}
