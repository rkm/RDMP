// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Linq;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.Data.Pipelines;
using CatalogueLibrary.Repositories;
using CatalogueManager.PipelineUIs.DemandsInitializationUIs.ArgumentValueControls;

namespace CatalogueManager.PipelineUIs.Pipelines.PluginPipelineUsers
{
    public class PipelineSelectionUIFactory
    {
        private readonly CatalogueRepository _repository;
        private readonly IPipelineUser _user;
        private readonly IPipelineUseCase _useCase;

        private IPipelineSelectionUI _pipelineSelectionUIInstance;

        public PipelineSelectionUIFactory(CatalogueRepository repository, IPipelineUser user, IPipelineUseCase useCase)
        {
            _repository = repository;
            _user = user;
            _useCase = useCase;
        }

        public PipelineSelectionUIFactory(CatalogueRepository repository, RequiredPropertyInfo requirement, ArgumentValueUIArgs args, object demanderInstance)
        {
            _repository = repository;

            var pluginUserAndCase = new PluginPipelineUser(requirement, args, demanderInstance);
            _user = pluginUserAndCase;
            _useCase = pluginUserAndCase;
        }

        public IPipelineSelectionUI Create(string text = null, DockStyle dock = DockStyle.None, Control containerControl = null)
        {
            //setup getter as an event handler for the selection ui
            _pipelineSelectionUIInstance = new PipelineSelectionUI(_useCase,_repository);

            if (_user != null)
            {
                _pipelineSelectionUIInstance.Pipeline = _user.Getter();

                _pipelineSelectionUIInstance.PipelineChanged += 
                    (sender, args) =>
                        _user.Setter(((IPipelineSelectionUI)sender).Pipeline as Pipeline);
            }

            var c = (Control)_pipelineSelectionUIInstance;

            if (dock == DockStyle.None)
                c.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            else
                c.Dock = dock;

            if (text != null)
                c.Text = text;

            if (containerControl != null)
                containerControl.Controls.Add(c);

            return _pipelineSelectionUIInstance;
        }

    }
}