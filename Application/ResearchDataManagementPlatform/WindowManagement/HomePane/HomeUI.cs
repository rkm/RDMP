// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Rdmp.Core;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Providers;
using Rdmp.UI.Collections.Providers;
using Rdmp.UI.CommandExecution.AtomicCommands;
using Rdmp.UI.CommandExecution.AtomicCommands.CohortCreationCommands;
using Rdmp.UI.CommandExecution.AtomicCommands.UIFactory;
using Rdmp.UI.CommandExecution.AtomicCommands.WindowArranging;
using Rdmp.UI.ItemActivation;
using Rdmp.UI.Refreshing;
using Rdmp.UI.TestsAndSetup.ServicePropogation;

namespace ResearchDataManagementPlatform.WindowManagement.HomePane
{
    /// <summary>
    /// The starting page of RDMP.  Provides a single easy access entry point into RDMP functionality for common tasks e.g. Data Management, Project Extraction etc.  Click the links of commands
    /// you want to carry out to access wizards that offer streamlined access to the RDMP functionality.
    /// 
    /// <para>You can access the HomeUI at any time by clicking the home icon in the top left of the RDMP tool bar.</para>
    /// </summary>
    public partial class HomeUI : RDMPUserControl,ILifetimeSubscriber
    {
        private readonly IActivateItems _activator;
        private readonly AtomicCommandUIFactory _uiFactory;

        public HomeUI(IActivateItems activator)
        {
            _activator = activator;
            _uiFactory = new AtomicCommandUIFactory(activator);
            InitializeComponent();
        }

        private void BuildCommandLists()
        {
            tlpDataManagement.Controls.Clear();
            tlpCohortCreation.Controls.Clear();
            tlpDataExport.Controls.Clear();
            tlpDataLoad.Controls.Clear();
            tlpRecent.Controls.Clear();


            /////////////////////////////////////Data Management/////////////////////////////////
            //AddLabel("New Catalogue", tlpDataManagement);

            AddCommand(new ExecuteCommandCreateNewCatalogueByImportingFile(_activator), tlpDataManagement);

            AddCommand(new ExecuteCommandCreateNewCatalogueByImportingExistingDataTable(_activator), tlpDataManagement);

            AddCommand(new ExecuteCommandEditExistingCatalogue(_activator),tlpDataManagement);

            AddCommand(new ExecuteCommandRunDQEOnCatalogue(_activator),tlpDataManagement);

            /////////////////////////////////////Cohort Creation/////////////////////////////////

            AddCommand(new ExecuteCommandCreateNewCohortFromFile(_activator),tlpCohortCreation);

            AddCommand(new ExecuteCommandCreateNewCohortIdentificationConfiguration(_activator)
            {
                OverrideCommandName = GlobalStrings.CreateNewCohortIdentificationQuery
            },tlpCohortCreation);

            AddCommand(new ExecuteCommandCreateNewCohortByExecutingACohortIdentificationConfiguration(_activator)
            {
                OverrideCommandName = "Create New Cohort From Cohort Identification Query"
            }, tlpCohortCreation);

            AddCommand(new ExecuteCommandEditExistingCohortIdentificationConfiguration(_activator)
            {
                OverrideCommandName = "Edit Cohort Identification Query"
            }, tlpCohortCreation);

            
            /////////////////////////////////////Data Export/////////////////////////////////
            
            var dataExportChildProvider = _activator.CoreChildProvider as DataExportChildProvider;
            if (dataExportChildProvider != null)
            {
                AddCommand(new ExecuteCommandCreateNewDataExtractionProject(_activator), tlpDataExport);
                AddCommand(new ExecuteCommandEditDataExtractionProject(_activator), tlpDataExport);
            }

            //////////////////////////////////Data Loading////////////////////////////////////
            AddCommand(new ExecuteCommandCreateNewLoadMetadata(_activator),tlpDataLoad);
            AddCommand(new ExecuteCommandExecuteLoadMetadata(_activator), tlpDataLoad);

            foreach (var entry in _activator.HistoryProvider.History.OrderByDescending(e=>e.Date).Take(10))
                if(((DatabaseEntity)entry.Object).Exists())
                    AddCommand(new ExecuteCommandActivate(_activator, entry.Object)
                    {
                        OverrideCommandName = entry.Object.ToString(),
                        AlsoShow = true
                    },tlpRecent);
            
            FixSizingOfTableLayoutPanel(tlpDataManagement);
            FixSizingOfTableLayoutPanel(tlpCohortCreation);
            FixSizingOfTableLayoutPanel(tlpDataExport);
            FixSizingOfTableLayoutPanel(tlpDataLoad);
            FixSizingOfTableLayoutPanel(tlpRecent);
        }

        private void AddCommand(IAtomicCommand command, TableLayoutPanel tableLayoutPanel)
        {
            var control = _uiFactory.CreateLinkLabel(command);

            SetBackgroundColor(tableLayoutPanel, control);

            tableLayoutPanel.Controls.Add(control, 0, tableLayoutPanel.Controls.Count);
            
            //extend the size to match
            var panel = (Panel)tableLayoutPanel.Parent;
            panel.Width = Math.Max(panel.Width, control.Width + 10);
        }

        readonly Dictionary<TableLayoutPanel, int> _alternateBackgroundColours = new Dictionary<TableLayoutPanel, int>();

        private void SetBackgroundColor(TableLayoutPanel tableLayoutPanel, Control control)
        {
            if (!_alternateBackgroundColours.ContainsKey(tableLayoutPanel))
                _alternateBackgroundColours.Add(tableLayoutPanel, 0);

            control.BackColor = _alternateBackgroundColours[tableLayoutPanel]++ % 2 == 0 ? Color.AliceBlue : Color.White;
        }

        private void FixSizingOfTableLayoutPanel(TableLayoutPanel tableLayoutPanel)
        {
            foreach (RowStyle style in tableLayoutPanel.RowStyles)
                style.SizeType = SizeType.AutoSize;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            SetItemActivator(_activator);

            BuildCommandLists();

            _activator.RefreshBus.EstablishLifetimeSubscription(this);
        }

        public void RefreshBus_RefreshObject(object sender, RefreshObjectEventArgs e)
        {
            BuildCommandLists();
        }
    }
}
