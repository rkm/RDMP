using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueManager.Collections;
using CatalogueManager.ItemActivation;
using CatalogueManager.SimpleControls;
using CatalogueManager.TestsAndSetup.ServicePropogation;
using ReusableUIComponents;

namespace CatalogueManager.DataLoadUIs.LoadMetadataUIs.LoadProgressAndCacheUIs
{
    /// <summary>
    /// Allows you to mark a LoadMetadata for periodic automated loading.  This will only work if you have a computer running the RDMPAutomationService (the routine loader).  If the automation
    /// service is not running you can still configure periodic loading in which case the DueLoads Dashboard will simply show that a load is due. 
    /// 
    /// <para>You can set how often to run the load and also chain it to another load e.g. you might want 'Load GP data' to always trigger 'Load Practice data'.  Chained loads will only ever launch
    /// if the primary load was successful.</para>
    /// </summary>
    public partial class LoadPeriodicallyUI : LoadPeriodicallyUI_Design, ISaveableUI
    {
        private LoadPeriodically _loadPeriodically;
        
        private LoadPeriodically LoadPeriodically
        {
            get { return _loadPeriodically; }
            set
            {
                _loadPeriodically = value;

                if (value != null)
                {
                    tbDaysToWaitBetweenLoads.Text = value.DaysToWaitBetweenLoads.ToString();
                    tbDaysToWaitBetweenLoads.Enabled = true;
                    
                    tbLastLoaded.Enabled = true;

                    if (value.LastLoaded == null)
                        tbLastLoaded.Text = "";
                    else
                    {

                        tbLastLoaded.Text = value.LastLoaded.ToString();
                        tbDaysToWaitBetweenLoads.Text = value.DaysToWaitBetweenLoads.ToString();

                        CalculateWhenNextLoadDue();
                    }

                    if (value.OnSuccessLaunchLoadMetadata_ID == null)
                        ddOnsuccessfulLoadLaunch.SelectedItem = null;
                    else
                        ddOnsuccessfulLoadLaunch.SelectedItem = ddOnsuccessfulLoadLaunch.Items.Cast<LoadMetadata>().SingleOrDefault(lmd=>lmd.ID == value.OnSuccessLaunchLoadMetadata_ID);

                }
                else
                {
                    tbDaysToWaitBetweenLoads.Enabled = false;
                    tbDaysToWaitBetweenLoads.Text = "";
                    tbLastLoaded.Text = "";
                    tbLastLoaded.Enabled = false;
                    tbNextLoadWillBe.Text = "";
                    
                    ddOnsuccessfulLoadLaunch.SelectedItem = null;
                }
                
            }
        }

        private void CalculateWhenNextLoadDue()
        {

            if (LoadPeriodically.IsLoadDue(null))
                tbNextLoadWillBe.Text = "Load Due!";
            else
                tbNextLoadWillBe.Text = LoadPeriodically.WhenIsNextLoadDue().ToString();

        }

        public LoadPeriodicallyUI()
        {
            InitializeComponent();

            AssociatedCollection = RDMPCollection.DataLoad;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if(VisualStudioDesignMode)
                return;

            ddOnsuccessfulLoadLaunch.Items.AddRange(RepositoryLocator.CatalogueRepository.GetAllObjects<LoadMetadata>().ToArray());

        }
        
        private void tbLastLoaded_TextChanged(object sender, EventArgs e)
        {
            if (LoadPeriodically != null)
            {
                try
                {

                    if (string.IsNullOrWhiteSpace(tbLastLoaded.Text))
                    {
                        LoadPeriodically.LastLoaded = null;
                    }
                    else
                    {
                        LoadPeriodically.LastLoaded = DateTime.Parse(tbLastLoaded.Text);
                    }

                    tbLastLoaded.ForeColor = Color.Black;
                }
                catch (Exception)
                {
                    tbLastLoaded.ForeColor = Color.Red;
                }

                CalculateWhenNextLoadDue();
            }

        }

        private void tbDaysToWaitBetweenLoads_TextChanged(object sender, EventArgs e)
        {
            if (LoadPeriodically != null)
            {
                try
                {
                    LoadPeriodically.DaysToWaitBetweenLoads = int.Parse(tbDaysToWaitBetweenLoads.Text);
                    tbDaysToWaitBetweenLoads.Text = LoadPeriodically.DaysToWaitBetweenLoads.ToString();//lets us deal with 1 thresholding

                    tbDaysToWaitBetweenLoads.ForeColor = Color.Black;
                    
                }
                catch (Exception)
                {
                    tbDaysToWaitBetweenLoads.ForeColor = Color.Red;
                }

                CalculateWhenNextLoadDue();
            }
        }

        private void ddOnsuccessfulLoadLaunch_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (LoadPeriodically != null)
            {
                try
                {
                    var lmd = ddOnsuccessfulLoadLaunch.SelectedItem as LoadMetadata;

                    if (lmd == null)
                        return;

                    LoadPeriodically.OnSuccessLaunchLoadMetadata_ID = lmd.ID;
                    LoadPeriodically.CheckForCircularReferences();
                }
                catch (Exception exception)
                {
                    ExceptionViewer.Show(exception);
                    ddOnsuccessfulLoadLaunch.SelectedItem = null;
                }
            }
        }

        private void btnNone_Click(object sender, EventArgs e)
        {
            if (LoadPeriodically != null)
            {
                LoadPeriodically.OnSuccessLaunchLoadMetadata_ID = null;
                ddOnsuccessfulLoadLaunch.SelectedItem = null;
            }
        }

        public override void SetDatabaseObject(IActivateItems activator, LoadPeriodically databaseObject)
        {
            base.SetDatabaseObject(activator, databaseObject);
            LoadPeriodically = databaseObject;
            objectSaverButton1.SetupFor(LoadPeriodically, activator.RefreshBus);
        }

        public ObjectSaverButton GetObjectSaverButton()
        {
            return this.objectSaverButton1;
        }
    }

    [TypeDescriptionProvider(typeof(AbstractControlDescriptionProvider<LoadPeriodicallyUI_Design, UserControl>))]
    public class LoadPeriodicallyUI_Design : RDMPSingleDatabaseObjectControl<LoadPeriodically>
    {

    }
}
