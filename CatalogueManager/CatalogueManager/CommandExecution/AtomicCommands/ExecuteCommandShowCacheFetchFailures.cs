// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Data;
using System.Drawing;
using System.Linq;
using CatalogueLibrary.Data.Cache;
using CatalogueManager.ItemActivation;
using ReusableLibraryCode.CommandExecution.AtomicCommands;
using ReusableLibraryCode.Icons.IconProvision;
using ReusableUIComponents;

namespace CatalogueManager.CommandExecution.AtomicCommands
{
    public class ExecuteCommandShowCacheFetchFailures : BasicUICommandExecution,IAtomicCommand
    {
        private CacheProgress _cacheProgress;
        private ICacheFetchFailure[] _failures;

        public ExecuteCommandShowCacheFetchFailures(IActivateItems activator, CacheProgress cacheProgress):base(activator)
        {
            _cacheProgress = cacheProgress;

            _failures = _cacheProgress.CacheFetchFailures.Where(f => f.ResolvedOn == null).ToArray();

            if(!_failures.Any())
                SetImpossible("There are no unresolved CacheFetchFailures");
        }

        public override void Execute()
        {
            base.Execute();

            // for now just show a modal dialog with a data grid view of all the failure rows
            
            DataTable dt = new DataTable();
            dt.Columns.Add("FetchRequestStart");
            dt.Columns.Add("FetchRequestEnd");
            dt.Columns.Add("ExceptionText");
            dt.Columns.Add("LastAttempt");
            dt.Columns.Add("ResolvedOn");

            foreach (ICacheFetchFailure f in _failures)
                dt.Rows.Add(f.FetchRequestStart, f.FetchRequestEnd, f.ExceptionText, f.LastAttempt, f.ResolvedOn);

            DataTableViewerUI ui = new DataTableViewerUI(dt,"Cache Failures");
            Activator.ShowWindow(ui, true);
        }

        public Image GetImage(IIconProvider iconProvider)
        {
            return iconProvider.GetImage(_cacheProgress);
        }
    }
}