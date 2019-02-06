// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Cache;
using CatalogueLibrary.Nodes.LoadMetadataNodes;
using CatalogueManager.DataLoadUIs.LoadMetadataUIs.LoadProgressAndCacheUIs;
using CatalogueManager.ItemActivation;
using ReusableLibraryCode.CommandExecution;
using ReusableUIComponents.CommandExecution;

namespace CatalogueManager.CommandExecution.Proposals
{
    class ProposeExecutionWhenTargetIsPermissionWindowUsedByCacheProgressNode : RDMPCommandExecutionProposal<PermissionWindowUsedByCacheProgressNode>
    {
        public ProposeExecutionWhenTargetIsPermissionWindowUsedByCacheProgressNode(IActivateItems itemActivator) : base(itemActivator)
        {
        }

        public override bool CanActivate(PermissionWindowUsedByCacheProgressNode target)
        {
            return true;
        }

        public override void Activate(PermissionWindowUsedByCacheProgressNode target)
        {
            if (target.DirectionIsCacheToPermissionWindow)
                ItemActivator.Activate<PermissionWindowUI, PermissionWindow>(target.PermissionWindow);
            else
                ItemActivator.Activate<CacheProgressUI, CacheProgress>(target.CacheProgress);
        }

        public override ICommandExecution ProposeExecution(ICommand cmd, PermissionWindowUsedByCacheProgressNode target,InsertOption insertOption = InsertOption.Default)
        {
            //no drag and drop
            return null;
        }
    }
}