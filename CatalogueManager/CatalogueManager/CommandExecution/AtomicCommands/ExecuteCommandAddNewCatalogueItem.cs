using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueManager.Copying.Commands;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using ReusableLibraryCode.CommandExecution.AtomicCommands;
using ReusableLibraryCode.Icons.IconProvision;

namespace CatalogueManager.CommandExecution.AtomicCommands
{
    public class ExecuteCommandAddNewCatalogueItem : BasicUICommandExecution,IAtomicCommand
    {
        private IActivateItems _activator;
        private Catalogue _catalogue;
        private ColumnInfo[] _columnInfos;
        private HashSet<int> _existingColumnInfos;

        public ExecuteCommandAddNewCatalogueItem(IActivateItems activator, Catalogue catalogue,ColumnInfoCommand colInfo) : this(activator,catalogue,colInfo.ColumnInfos)
        {
            
        }

        public ExecuteCommandAddNewCatalogueItem(IActivateItems activator, Catalogue catalogue,params ColumnInfo[] columnInfos) : base(activator)
        {
            _activator = activator;
            _catalogue = catalogue;

            _existingColumnInfos = new HashSet<int>(_catalogue.CatalogueItems.Select(ci=>ci.ColumnInfo_ID).Where(col=>col.HasValue).Select(v=>v.Value).Distinct().ToArray());

            _columnInfos = columnInfos;

            if(_columnInfos.Length > 0 && _columnInfos.All(AlreadyInCatalogue))
                SetImpossible("ColumnInfo(s) are already in Catalogue");
        }


        public override string GetCommandHelp()
        {
            return "Creates a new virtual column in the dataset, this is the first stage to making a new column extractable or defining a new extraction transform";
        }

        public override void Execute()
        {
            base.Execute();
        
            //if we have not got an explicit one to import let the user pick one
            if (_columnInfos.Length == 0)
            {
                MessageBox.Show("Select which column the new CatalogueItem will describe/extract", "Choose underlying Column");

                ColumnInfo columnInfo;
                string text;

                if(SelectOne(_activator.CoreChildProvider.AllColumnInfos,out columnInfo))
                    if(TypeText("Name", "Type a name for the new CatalogueItem", 500, columnInfo.GetRuntimeName(),out text))
                    {
                        var ci = new CatalogueItem(_activator.RepositoryLocator.CatalogueRepository, _catalogue, "New CatalogueItem " + Guid.NewGuid());
                        ci.Name = text;
                        ci.SetColumnInfo(columnInfo);
                        ci.SaveToDatabase();

                        Publish(_catalogue);
                        Emphasise(ci,int.MaxValue);
                    }
            }
            else
            {
                foreach (ColumnInfo columnInfo in _columnInfos)
                {
                    if(AlreadyInCatalogue(columnInfo))
                        continue;
                    
                    var ci = new CatalogueItem(_activator.RepositoryLocator.CatalogueRepository, _catalogue, columnInfo.Name);
                    ci.SetColumnInfo(columnInfo);
                    ci.SaveToDatabase();
                }

                Publish(_catalogue);
            }
        }

        private bool AlreadyInCatalogue(ColumnInfo candidate)
        {
            return _existingColumnInfos.Contains(candidate.ID);
        }

        public Image GetImage(IIconProvider iconProvider)
        {
            return iconProvider.GetImage(RDMPConcept.CatalogueItem, OverlayKind.Add);
        }
    }
}