using System;
using System.Drawing;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using MapsDirectlyToDatabaseTableUI;
using ReusableUIComponents;
using ReusableUIComponents.CommandExecution.AtomicCommands;
using ReusableUIComponents.Icons.IconProvision;

namespace CatalogueManager.CommandExecution.AtomicCommands
{
    public class ExecuteCommandAddNewLookupTableRelationship : BasicUICommandExecution,IAtomicCommand
    {
        private readonly IActivateItems _activator;
        private readonly Catalogue _catalogueIfKnown;
        private readonly TableInfo _lookupTableInfoIfKnown;

        public ExecuteCommandAddNewLookupTableRelationship(IActivateItems activator, Catalogue catalogueIfKnown, TableInfo lookupTableInfoIfKnown) : base(activator)
        {
            _activator = activator;
            _catalogueIfKnown = catalogueIfKnown;
            _lookupTableInfoIfKnown = lookupTableInfoIfKnown;

            if(catalogueIfKnown == null && lookupTableInfoIfKnown == null)
                throw new NotSupportedException("You must know either the lookup table or the Catalogue you want to configure it on");
        }

        public override void Execute()
        {
            base.Execute();
        
            if (_catalogueIfKnown == null)  
                PickCatalogueAndLaunchForTableInfo(_lookupTableInfoIfKnown);
            else
                _activator.ActivateLookupConfiguration(this, _catalogueIfKnown, _lookupTableInfoIfKnown);
        }

        private void PickCatalogueAndLaunchForTableInfo(TableInfo tbl)
        {
            try
            {
                var dr = MessageBox.Show(
@"You have chosen to make '" + tbl + @"' a Lookup Table (e.g T = Tayside, F=Fife etc).  In order to do this you will need to pick which Catalogue the column
provides a description for (a given TableInfo can be a Lookup for many columns in many datasets)."
                    , "Create Lookup", MessageBoxButtons.OKCancel);

                if (dr == DialogResult.OK)
                {
                    var selectCatalogue = new SelectIMapsDirectlyToDatabaseTableDialog(tbl.Repository.GetAllObjects<Catalogue>(), false, false);
                    if (selectCatalogue.ShowDialog() == DialogResult.OK)
                        _activator.ActivateLookupConfiguration(this, (Catalogue)selectCatalogue.Selected, tbl);
                }
            }
            catch (Exception exception)
            {
                ExceptionViewer.Show("Error creating Lookup", exception);
            }
        }

        public Image GetImage(IIconProvider iconProvider)
        {
            return iconProvider.GetImage(RDMPConcept.Lookup, OverlayKind.Add);
        }
    }
}