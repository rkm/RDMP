// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FAnsi.Discovery;
using FAnsi.Discovery.TableCreation;

namespace Rdmp.UI.DataLoadUIs.ModuleUIs;

public partial class AdjustColumnDataTypesUI : Form, IDatabaseColumnRequestAdjuster
{
    private List<DatabaseColumnRequest> _columns;

    public AdjustColumnDataTypesUI()
    {
        InitializeComponent();
    }

    public void AdjustColumns(List<DatabaseColumnRequest> columns)
    {
        _columns = columns;

        foreach (var column in _columns)
        {
            var ui = new DatabaseColumnRequestUI(column);
            ui.Dock = DockStyle.Top;
            flowLayoutPanel1.Controls.Add(ui);
        }


        ShowDialog();
    }

    private void btnDone_Click(object sender, System.EventArgs e)
    {
        if (_columns == null)
            throw new Exception("AdjustColumns was not called yet");
            
        Close();
    }
}