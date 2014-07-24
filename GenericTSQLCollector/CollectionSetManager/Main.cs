using CollectionSetManager.Properties;
using Sqlconsulting.DataCollector.InstallCollectorType;
using Sqlconsulting.DataCollector.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Sqlconsulting.DataCollector.CollectionSetManager
{
    public partial class Main : Form
    {

        private TreeNode tnRoot = null;
        private TreeNode tnCollectorTypes;
        private TreeNode tnCollectionSets; 
        private TreeNode tnCollectorLogs;

        private TablessControl tabs = new TablessControl();

        static ImageList _imageList;
        public static ImageList ImageList
        {
            get
            {
                if (_imageList == null)
                {
                    _imageList = new ImageList();
                    _imageList.Images.Add("jobs", Resources.jobs);
                    _imageList.Images.Add("package", Resources.package);
                    _imageList.Images.Add("server", Resources.server);
                    _imageList.Images.Add("steps", Resources.steps);
                    _imageList.Images.Add("datacollection", Resources.DataCollection);
                    _imageList.Images.Add("folder", Resources.folder);
                }
                return _imageList;
            }
        }
       

       
        

        


        public Main()
        {
            InitializeComponent();
            PopulateTreeView();
            splitContainer1.Panel2.Controls.Add(tabs);
            tabs.Dock = DockStyle.Fill;
            PopulateTabs(tabs);

        }

        private void PopulateTabs(TablessControl tabs)
        {
            TabPage tabPage1 = new TabPage();
            tabPage1.Name = "installer";
            tabPage1.Text = tabPage1.Name;
            tabPage1.Controls.Add(panel1);
            panel1.Dock = DockStyle.Fill;
            tabs.TabPages.Add(tabPage1);

            TabPage tabPage2 = new TabPage();
            tabPage2.Name = "CollectorTypeEditor";
            tabPage2.Text = tabPage2.Name;
            tabPage2.Controls.Add(panel2);
            panel2.Dock = DockStyle.Fill;
            tabs.TabPages.Add(tabPage2);

            TabPage tabPage3 = new TabPage();
            tabPage3.Name = "Empty";
            tabPage3.Text = tabPage3.Name;
            tabPage3.Controls.Add(panel3);
            panel3.Dock = DockStyle.Fill;
            tabs.TabPages.Add(tabPage3);

            TabPage tabPage4 = new TabPage();
            tabPage4.Name = "CollectionSetEditor";
            tabPage4.Text = tabPage4.Name;
            tabPage4.Controls.Add(panel4);
            panel4.Dock = DockStyle.Fill;
            tabs.TabPages.Add(tabPage4);

            TabPage tabPage5 = new TabPage();
            tabPage5.Name = "CollectionItemEditor";
            tabPage5.Text = tabPage5.Name;
            tabPage5.Controls.Add(panel5);
            panel5.Dock = DockStyle.Fill;
            tabs.TabPages.Add(tabPage5);

            TabPage tabPage6 = new TabPage();
            tabPage6.Name = "CollectionItemEditor";
            tabPage6.Text = tabPage6.Name;
            tabPage6.Controls.Add(panel6);
            panel6.Dock = DockStyle.Fill;
            tabs.TabPages.Add(tabPage6); 
        }

        private void PopulateTreeView()
        {
            treeView1.ImageList = ImageList;

            TreeNode[] cTypes = PopulateCollectorTypes();
            TreeNode[] cSets = PopulateCollectionSets();
            tnCollectorTypes = new TreeNode("Collector Types",cTypes);
            tnCollectionSets = new TreeNode("Collection Sets",cSets);
            tnCollectorLogs = new TreeNode("Logs");

            tnCollectorTypes.ImageKey = "folder";
            tnCollectorTypes.SelectedImageKey = "folder";
            tnCollectionSets.ImageKey = "folder";
            tnCollectionSets.SelectedImageKey = "folder";
            tnCollectorLogs.ImageKey = "folder";
            tnCollectorLogs.SelectedImageKey = "folder";

            TreeNode[] subnodes = new TreeNode[] {tnCollectorTypes, tnCollectionSets, tnCollectorLogs};
            tnRoot = new TreeNode(Manager.ServerName, subnodes);
            tnRoot.ImageKey = "server";
            tnRoot.SelectedImageKey = "server";

            treeView1.Nodes.Add(tnRoot);
            treeView1.ExpandAll();
            foreach (TreeNode childNode in tnCollectionSets.Nodes)
            {
                childNode.Collapse();
            }

        }

        private TreeNode[] PopulateCollectionItems(int collection_set_id)
        {
            String sql = @"
                SELECT collection_item_id, name
                FROM msdb.dbo.syscollector_collection_items
                WHERE collection_set_id = '{0}'
                ORDER BY name
            ";
            sql = String.Format(sql, collection_set_id);
            DataTable dt = CollectorUtils.GetDataTable(Manager.ServerName, "msdb", sql);
            List<TreeNode> nodes = new List<TreeNode>();
            foreach (DataRow dr in dt.Rows)
            {
                TreeNode tn = new TreeNode(dr["name"].ToString());
                tn.Tag = dr["collection_item_id"];
                tn.ImageKey = "package";
                tn.SelectedImageKey = "package";
                nodes.Add(tn);
            }
            return nodes.ToArray();
        }

        private TreeNode[] PopulateCollectionSets()
        {
           String sql = @"
                SELECT name, description, is_system, collection_set_id
                FROM msdb.dbo.syscollector_collection_sets
                ORDER BY is_system DESC, name
            ";
            DataTable dt = CollectorUtils.GetDataTable(Manager.ServerName, "msdb", sql);
            List<TreeNode> nodes = new List<TreeNode>();
            foreach (DataRow dr in dt.Rows)
            {
                TreeNode tn = new TreeNode(dr["name"].ToString(), PopulateCollectionItems(Int32.Parse(dr["collection_set_id"].ToString())));
                tn.Tag = dr["collection_set_id"];
                tn.ImageKey = "steps";
                tn.SelectedImageKey = "steps";
                nodes.Add(tn);
                
            }
            return nodes.ToArray();
        }

        private TreeNode[] PopulateCollectorTypes()
        {
            String sql = @"
                SELECT name, collection_package_name, upload_package_name, is_system, collector_type_uid
                FROM msdb.dbo.syscollector_collector_types
                ORDER BY is_system DESC, name
            ";
            DataTable dt = CollectorUtils.GetDataTable(Manager.ServerName, "msdb", sql);
            List<TreeNode> nodes = new List<TreeNode>();
            foreach (DataRow dr in dt.Rows)
            {
                TreeNode tn = new TreeNode(dr["name"].ToString());
                tn.Tag = dr["collector_type_uid"];
                tn.ImageKey = "DataCollection";
                tn.SelectedImageKey = "DataCollection";
                nodes.Add(tn);
            }
            return nodes.ToArray();
        }


        private void installCollectorType(String serverName)
        {
            CollectorTypeInstaller cti = new CollectorTypeInstaller(serverName);
            try
            {
                if (!cti.checkPermissions())
                {
                    MessageBox.Show("Permission denied. The collector type can be installed only by a member of the 'sysadmin' fixed server role.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    cti.install();
                    MessageBox.Show("Collector type installed successfully.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void Main_Load(object sender, EventArgs e)
        {
            CollectorTypeInstaller cti = new CollectorTypeInstaller(Manager.ServerName);
            if (!cti.CheckCollectorTypeInstallStatus("Extended T-SQL Query Collector Type") || !cti.CheckCollectorTypeInstallStatus("Extended XE Reader Collector Type"))
            {
                DialogResult dialogResult = MessageBox.Show("The collector types are not installed on this server: install now?", "Install", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    installCollectorType(Manager.ServerName);
                }
            }
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (treeView1.SelectedNode == tnRoot || treeView1.SelectedNode == tnCollectorTypes)
            {
                tabs.SelectedIndex = 0;
            }
            else if (treeView1.SelectedNode.Parent == tnCollectorTypes)
            {
                tabs.SelectedIndex = 1;
            }
            else if (treeView1.SelectedNode == tnCollectionSets || treeView1.SelectedNode == tnCollectorLogs)
            {
                tabs.SelectedIndex = 2;
            }
            else if (treeView1.SelectedNode.Parent == tnCollectionSets)
            {
                tabs.SelectedIndex = 3;
            }
            else if (treeView1.SelectedNode.Parent == tnCollectorLogs)
            {
                tabs.SelectedIndex = 5;
            }
            else if (treeView1.SelectedNode.Parent.Parent == tnCollectionSets)
            {
                tabs.SelectedIndex = 4;
            }
        }
    }
}
