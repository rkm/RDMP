﻿// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Windows.Forms;
using Rdmp.Core.Curation.Data;
using ReusableLibraryCode.Settings;

namespace Rdmp.UI.Collections
{
    public partial class CatalogueCollectionFilterUI : UserControl
    {
        public CatalogueCollectionFilterUI()
        {
            InitializeComponent();

            cbShowInternal.Checked = UserSettings.ShowInternalCatalogues;
            cbShowDeprecated.Checked = UserSettings.ShowDeprecatedCatalogues ;
            cbShowColdStorage.Checked = UserSettings.ShowColdStorageCatalogues;
            cbProjectSpecific.Checked = UserSettings.ShowProjectSpecificCatalogues;
            cbShowNonExtractable.Checked = UserSettings.ShowNonExtractableCatalogues;
        }

        public event EventHandler<EventArgs> FiltersChanged;

        private void OnCheckboxChanged(object sender, System.EventArgs e)
        {
            UserSettings.ShowInternalCatalogues = cbShowInternal.Checked;
            UserSettings.ShowDeprecatedCatalogues = cbShowDeprecated.Checked;
            UserSettings.ShowColdStorageCatalogues = cbShowColdStorage.Checked;
            UserSettings.ShowProjectSpecificCatalogues = cbProjectSpecific.Checked;
            UserSettings.ShowNonExtractableCatalogues = cbShowNonExtractable.Checked;

            FiltersChanged?.Invoke(this,new EventArgs());
        }

        public void EnsureVisible(Catalogue c)
        {
            if (c.IsColdStorageDataset || c.IsDeprecated || c.IsInternalDataset)
            {
                //trouble is our flags might be hiding it so make sure it is visible
                cbShowColdStorage.Checked = cbShowColdStorage.Checked || c.IsColdStorageDataset;
                cbShowDeprecated.Checked = cbShowDeprecated.Checked || c.IsDeprecated;
                cbShowInternal.Checked = cbShowInternal.Checked || c.IsInternalDataset;
            }

            var isExtractable = c.GetExtractabilityStatus(null);

            cbShowNonExtractable.Checked = cbShowNonExtractable.Checked || isExtractable == null || isExtractable.IsExtractable == false;

        }
    }
}
