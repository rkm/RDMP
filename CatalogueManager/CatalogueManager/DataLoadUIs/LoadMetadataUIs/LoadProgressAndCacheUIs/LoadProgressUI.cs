﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Cache;
using CatalogueLibrary.Data.DataLoad;
using CatalogueManager.ItemActivation;
using CatalogueManager.SimpleControls;
using CatalogueManager.TestsAndSetup.ServicePropogation;
using MapsDirectlyToDatabaseTable.Revertable;
using ReusableUIComponents;

namespace CatalogueManager.DataLoadUIs.LoadMetadataUIs.LoadProgressAndCacheUIs
{
    /// <summary>
    /// Part of LoadProgressManagement, let's you configure the settings of a LoadProgress (see LoadProgressManagement for a description of what a LoadProgress is).
    /// 
    /// Each LoadProgress can be tied to a Cache progress.  If you are using a LoadProgress without a cache then it is up to your load implementation to respect the time period being loaded 
    /// (e.g. when using RemoteSQLTableAttacher you should make use of the @startDate and @endDate parameters in your fetch query).  See CacheProgressUI for a description of caching and 
    /// permission windows.
    /// </summary>
    public partial class LoadProgressUI : LoadProgressUI_Design, ISaveableUI
    {
        private LoadProgress _loadProgress;
        private bool bLoading;
        
        public event EventHandler Saved;

        public LoadProgressUI()
        {
            InitializeComponent();
            loadProgressDiagram1.LoadProgressChanged += ReloadUIFromDatabase;
        }
        
        public LoadProgress LoadProgress
        {
            get { return _loadProgress; }
            set
            {
                bLoading = true;
                
                _loadProgress = value;

                ReloadUIFromDatabase();

                
                bLoading = false;
            }
        }

        private void ReloadUIFromDatabase()
        {
            if (_loadProgress != null)
            {
                loadProgressDiagram1.SetLoadProgress(_loadProgress,_activator);
                loadProgressDiagram1.Visible = true;
            }
            else
                loadProgressDiagram1.Visible = false;

            tbDataLoadProgress.ReadOnly = true;

            if (_loadProgress == null)
            {
                tbID.Text = "";
                //dtpOriginDate.Value = DateTime.MinValue;
                tbOriginDate.Text = null;
                tbName.Text = null;

                groupBox1.Visible = false;
            }
            else
            {
                groupBox1.Visible = true;
                
                if(_loadProgress.OriginDate != null)
                    tbOriginDate.Text = _loadProgress.OriginDate.ToString();
                
                tbID.Text = LoadProgress.ID.ToString();
                tbName.Text = LoadProgress.Name;

                tbDataLoadProgress.Text = LoadProgress.DataLoadProgress != null ? LoadProgress.DataLoadProgress.ToString() : "";

                nDefaultNumberOfDaysToLoadEachTime.Value = LoadProgress.DefaultNumberOfDaysToLoadEachTime;
                cbEnableAutomation.Checked = _loadProgress.AllowAutomation;
            }
        }

        private void tbName_TextChanged(object sender, EventArgs e)
        {
            if(bLoading)
                return;

            LoadProgress.Name = tbName.Text;
            
        }
        
        private void nDefaultNumberOfDaysToLoadEachTime_ValueChanged(object sender, EventArgs e)
        {

            _loadProgress.DefaultNumberOfDaysToLoadEachTime = Convert.ToInt32(nDefaultNumberOfDaysToLoadEachTime.Value);
        }

        private void btnEditLoadProgress_Click(object sender, EventArgs e)
        {
            tbDataLoadProgress.ReadOnly = false;
        }

        private void tbDataLoadProgress_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tbDataLoadProgress.Text))
                    _loadProgress.DataLoadProgress = null;
                else
                    _loadProgress.DataLoadProgress = DateTime.Parse(tbDataLoadProgress.Text);

                tbDataLoadProgress.ForeColor = Color.Black;
            }
            catch (Exception)
            {
                tbDataLoadProgress.ForeColor = Color.Red;
            }
        }

        private void tbOriginDate_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(tbOriginDate.Text))
                    _loadProgress.OriginDate = null;
                else
                    _loadProgress.OriginDate = DateTime.Parse(tbOriginDate.Text);

                tbOriginDate.ForeColor = Color.Black;
            }
            catch (Exception)
            {
                tbOriginDate.ForeColor = Color.Red;
            }
        }

        private void cbEnableAutomation_CheckedChanged(object sender, EventArgs e)
        {
            _loadProgress.AllowAutomation = cbEnableAutomation.Enabled;
            _loadProgress.SaveToDatabase();
        }

        public override void SetDatabaseObject(IActivateItems activator, LoadProgress databaseObject)
        {
            base.SetDatabaseObject(activator, databaseObject);

            LoadProgress = databaseObject;

            objectSaverButton1.SetupFor(LoadProgress,activator.RefreshBus);

        }

        public ObjectSaverButton GetObjectSaverButton()
        {
            return objectSaverButton1;
        }
    }

    [TypeDescriptionProvider(typeof(AbstractControlDescriptionProvider<LoadProgressUI_Design, UserControl>))]
    public abstract class LoadProgressUI_Design:RDMPSingleDatabaseObjectControl<LoadProgress>
    {
    }
}
