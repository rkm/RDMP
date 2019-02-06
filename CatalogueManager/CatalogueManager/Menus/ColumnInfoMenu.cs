// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueManager.CommandExecution.AtomicCommands;
using CatalogueManager.DataViewing;
using CatalogueManager.DataViewing.Collections;

namespace CatalogueManager.Menus
{
    [System.ComponentModel.DesignerCategory("")]
    class ColumnInfoMenu : RDMPContextMenuStrip
    {
        public ColumnInfoMenu(RDMPContextMenuStripArgs args, ColumnInfo columnInfo) : base(args, columnInfo)
        {
            Items.Add("View Extract", null, (s,e)=> _activator.ViewDataSample(new ViewColumnInfoExtractUICollection(columnInfo,ViewType.TOP_100)));
            //create right click context menu
            Items.Add("View Aggreggate", null, (s, e) => _activator.ViewDataSample(new ViewColumnInfoExtractUICollection(columnInfo, ViewType.Aggregate)));
            
            Add(new ExecuteCommandAddNewLookupTableRelationship(_activator, null,columnInfo.TableInfo));

            Items.Add(new ToolStripSeparator());

            Add(new ExecuteCommandAddJoinInfo(_activator, columnInfo.TableInfo));

            Add(new ExecuteCommandAnonymiseColumnInfo(_activator, columnInfo));
            
            Add(new ExecuteCommandFindUsages(_activator,columnInfo));
        }
    }
}
