using CatalogueLibrary.Data;
using CatalogueLibrary.Nodes;
using CatalogueManager.CommandExecution.AtomicCommands;
using CatalogueManager.ItemActivation;

namespace CatalogueManager.Menus
{
    public class AutomationServerSlotsMenu : RDMPContextMenuStrip
    {
        public AutomationServerSlotsMenu(IActivateItems activator, AllAutomationServerSlotsNode databaseEntity)
            : base(activator, null)
        {
            Add(new ExecuteCommandCreateNewAutomationSlot(activator));
        }
    }
}