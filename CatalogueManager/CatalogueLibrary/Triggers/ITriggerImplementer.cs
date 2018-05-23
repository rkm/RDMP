using ReusableLibraryCode.Checks;

namespace CatalogueLibrary.Triggers
{
    public interface ITriggerImplementer
    {

        void DropTrigger(out string problemsDroppingTrigger, out string thingsThatWorkedDroppingTrigger);
        string CreateTrigger(ICheckNotifier notifier, int timeout = 30);
        TriggerStatus GetTriggerStatus();
        bool CheckUpdateTriggerIsEnabledAndHasExpectedBody();
    }
}