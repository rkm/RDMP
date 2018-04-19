using System.Drawing;
using CatalogueLibrary.CommandExecution.AtomicCommands;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.Repositories;
using CatalogueManager.Icons.IconOverlays;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using CatalogueManager.Refreshing;
using ReusableLibraryCode.CommandExecution;
using ReusableLibraryCode.CommandExecution.AtomicCommands;
using ReusableLibraryCode.Icons.IconProvision;
using ReusableUIComponents.CommandExecution;
using ReusableUIComponents.CommandExecution.AtomicCommands;

namespace CatalogueManager.CommandExecution.AtomicCommands
{
    internal class ExecuteCommandCreateNewLoadProgress : BasicUICommandExecution,IAtomicCommand
    {
        private readonly LoadMetadata _loadMetadata;

        public ExecuteCommandCreateNewLoadProgress(IActivateItems activator, LoadMetadata loadMetadata) : base(activator)
        {
            _loadMetadata = loadMetadata;

            if(loadMetadata.LoadPeriodically != null)
                SetImpossible("Cannot create a LoadProgress when there is already a LoadPeriodically");
        }

        public override void Execute()
        {
            base.Execute();

            var lp = new LoadProgress((ICatalogueRepository) _loadMetadata.Repository, _loadMetadata);
            Publish(_loadMetadata);
            Emphasise(lp);
        }

        public Image GetImage(IIconProvider iconProvider)
        {
            return iconProvider.GetImage(RDMPConcept.LoadProgress, OverlayKind.Add);
        }
    }
}