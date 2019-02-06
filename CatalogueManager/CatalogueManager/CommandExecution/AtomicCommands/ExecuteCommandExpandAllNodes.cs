// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BrightIdeasSoftware;
using CatalogueManager.Collections;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using ReusableLibraryCode.CommandExecution.AtomicCommands;
using ReusableLibraryCode.Icons.IconProvision;

namespace CatalogueManager.CommandExecution.AtomicCommands
{
    public class ExecuteCommandExpandAllNodes : BasicUICommandExecution,IAtomicCommand
    {
        private readonly RDMPCollectionCommonFunctionality _commonFunctionality;
        private object _rootToExpandFrom;

        public ExecuteCommandExpandAllNodes(IActivateItems activator,RDMPCollectionCommonFunctionality commonFunctionality, object rootToCollapseTo) : base(activator)
        {
            _commonFunctionality = commonFunctionality;
            _rootToExpandFrom = rootToCollapseTo;
            
            if(!commonFunctionality.Tree.CanExpand(rootToCollapseTo))
                SetImpossible("Node cannot be expanded");
        }

        public override void Execute()
        {
            base.Execute();

            _commonFunctionality.Tree.BeginUpdate();
            try
            {
                _commonFunctionality.ExpandToDepth(int.MaxValue,_rootToExpandFrom);

                var index = _commonFunctionality.Tree.IndexOf(_rootToExpandFrom);
                if (index != -1)
                    _commonFunctionality.Tree.EnsureVisible(index);
            }
            finally
            {
                _commonFunctionality.Tree.EndUpdate();
            }
        }

        public Image GetImage(IIconProvider iconProvider)
        {
            return CatalogueIcons.ExpandAllNodes;
        }
    }
}
