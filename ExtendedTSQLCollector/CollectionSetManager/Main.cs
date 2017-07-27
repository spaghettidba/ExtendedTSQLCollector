using CollectionSetManager.Properties;
using Sqlconsulting.DataCollector.InstallCollectorType;
using Sqlconsulting.DataCollector.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
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

        private ContextMenuStrip menuStrip;

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

            menuStrip = new ContextMenuStrip();
            this.treeView1.ContextMenuStrip = menuStrip;
            menuStrip.Items.Add("Add");
            menuStrip.Items.Add("Delete");
            menuStrip.Items.Add("Query");
            menuStrip.ItemClicked += new ToolStripItemClickedEventHandler(menuStrip_ItemClicked);
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
                SELECT name, description, is_system, collection_set_uid, collection_set_id
                FROM msdb.dbo.syscollector_collection_sets
                ORDER BY is_system DESC, name
            ";
            DataTable dt = CollectorUtils.GetDataTable(Manager.ServerName, "msdb", sql);
            List<TreeNode> nodes = new List<TreeNode>();
            foreach (DataRow dr in dt.Rows)
            {
                TreeNode tn = new TreeNode(dr["name"].ToString(), PopulateCollectionItems(Int32.Parse(dr["collection_set_id"].ToString())));
                tn.Tag = dr["collection_set_uid"];
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


        public void RefreshTreeView()
        {
            String CurrentText = null;
            if (treeView1.SelectedNode != null)
            {
                CurrentText = treeView1.SelectedNode.Text;
            }
            treeView1.Nodes.Clear();
            PopulateTreeView();
            if(CurrentText != null)
                foreach (TreeNode tn in treeView1.Nodes)
                {
                    if(tn.Text.Equals(CurrentText)){
                        treeView1.SelectedNode = tn;
                    }
                }
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
           
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //TODO: Add code to prevent data loss when changing node
            if (treeView1.SelectedNode == tnRoot || treeView1.SelectedNode == tnCollectorTypes)
            {
                tabs.SelectedIndex = 0;
                checkInstallation();
            }
            else if (treeView1.SelectedNode.Parent == tnCollectorTypes)
            {
                tabs.SelectedIndex = 1;
                panel2.Controls.Clear();
                CollectorTypeControl ctc = null;
                if (treeView1.SelectedNode.Tag != null)
                {
                    ctc = new CollectorTypeControl(new Guid(treeView1.SelectedNode.Tag.ToString()), this);
                }
                else
                {
                    ctc = new CollectorTypeControl(this);
                }
                panel2.Controls.Add(ctc);
                ctc.Dock = DockStyle.Fill;
            }
            else if (treeView1.SelectedNode == tnCollectionSets || treeView1.SelectedNode == tnCollectorLogs)
            {
                tabs.SelectedIndex = 2;
            }
            else if (treeView1.SelectedNode.Parent == tnCollectionSets)
            {
                tabs.SelectedIndex = 3;
                panel4.Controls.Clear();
                CollectionSetControl csc = null;
                if (treeView1.SelectedNode.Tag != null)
                {
                    csc = new CollectionSetControl(new Guid(treeView1.SelectedNode.Tag.ToString()), this);
                }
                else
                {
                    csc = new CollectionSetControl(this);
                }
                panel4.Controls.Add(csc);
                csc.Dock = DockStyle.Fill;
            }
            else if (treeView1.SelectedNode.Parent == tnCollectorLogs)
            {
                tabs.SelectedIndex = 5;
            }
            else if (treeView1.SelectedNode.Parent.Parent == tnCollectionSets)
            {
                tabs.SelectedIndex = 4;
                panel5.Controls.Clear();
                CollectionItemControl cic = null;
                if (treeView1.SelectedNode.Tag != null)
                {
                    cic = new CollectionItemControl(Convert.ToInt32(treeView1.SelectedNode.Tag.ToString()), this);
                }
                else
                {
                    cic = new CollectionItemControl(new Guid(treeView1.SelectedNode.Parent.Tag.ToString()), this);
                }
                
                panel5.Controls.Add(cic);
                cic.Dock = DockStyle.Fill;
            }
        }


        private void checkInstallation()
        {
            CollectorTypeInstaller cti = new CollectorTypeInstaller(Manager.ServerName);
            if (!cti.CheckCollectorTypeInstallStatus("Extended T-SQL Query Collector Type") || !cti.CheckCollectorTypeInstallStatus("Extended XE Reader Collector Type"))
            {
                label1.Text = "The extended collector types are not installed.";
                buttonInstall.Text = "Install";
            }
            else
            {
                label1.Text = "The extended collector types are installed.";
                buttonInstall.Text = "Update";
            }
        }

        private void buttonInstall_Click(object sender, EventArgs e)
        {
            installCollectorType(Manager.ServerName);
        }


   

        void menuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem.Text)
            {
                case "Add":
                    TreeNode childNode = new TreeNode("Node " + treeView1.SelectedNode.Nodes.Count);
                    treeView1.SelectedNode.Nodes.Add(childNode);

                    if (treeView1.SelectedNode == tnCollectionSets)
                    {
                        childNode.ImageKey = "steps";
                        childNode.SelectedImageKey = "steps";
                    }
                    else
                    {
                        if (treeView1.SelectedNode == tnCollectorTypes)
                        {
                            childNode.ImageKey = "DataCollection";
                            childNode.SelectedImageKey = "DataCollection";
                        }
                        else
                        {
                            childNode.ImageKey = "package";
                            childNode.SelectedImageKey = "package";
                        }
                    }
                    treeView1.SelectedNode = childNode;

                    break;
                case "Delete":
                    if (MessageBox.Show("Are you sure you want to delete this item?", "CollectionSetEditor", MessageBoxButtons.YesNo, MessageBoxIcon.Question).Equals(DialogResult.Yes))
                    {
                        if(deleteItem(treeView1.SelectedNode))
                            treeView1.SelectedNode.Remove();
                    }
                    
                    break;
                case "Query":
                    CollectionSetDataForm dialog = new CollectionSetDataForm((Int32)treeView1.SelectedNode.Tag);
                    dialog.ShowDialog(this);


                    break;
            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            foreach (ToolStripMenuItem tsmi in menuStrip.Items)
            {
                tsmi.Enabled = true;
            }

            if (e.Button == MouseButtons.Right)
            {
                // Select the clicked node
                treeView1.SelectedNode = treeView1.GetNodeAt(e.X, e.Y);

                foreach (ToolStripMenuItem tsmi in menuStrip.Items)
                {
                    if (tsmi.Text.Equals("Query"))
                        tsmi.Enabled = false;
                }

                if (treeView1.SelectedNode == tnRoot || treeView1.SelectedNode == tnCollectorLogs)
                {
                    foreach (ToolStripMenuItem tsmi in menuStrip.Items)
                    {
                        tsmi.Enabled = false;
                    }
                    return;
                }

                if (treeView1.SelectedNode == tnCollectionSets || treeView1.SelectedNode == tnCollectorTypes)
                {
                    foreach (ToolStripMenuItem tsmi in menuStrip.Items)
                    {
                        if (tsmi.Text.Equals("Delete") || tsmi.Text.Equals("Query"))
                            tsmi.Enabled = false;
                    }
                    return;
                }

                if (treeView1.SelectedNode.Parent == tnCollectorTypes || treeView1.SelectedNode.Parent.Parent == tnCollectionSets)
                {
                    foreach (ToolStripMenuItem tsmi in menuStrip.Items)
                    {
                        if (tsmi.Text.Equals("Add"))
                            tsmi.Enabled = false;
                        if (tsmi.Text.Equals("Query") && treeView1.SelectedNode.Parent.Parent == tnCollectionSets)
                            tsmi.Enabled = true;

                    }
                    return;
                }

            }

        }

        private Boolean deleteItem(TreeNode selectedNode)
        {
            String sql;
            int ConnectionTimeout = 15;
            int QueryTimeout = 600;

            String ConnectionString = String.Format("Server={0};Database={1};Integrated Security=True;Connect Timeout={2}", Manager.ServerName, "msdb", ConnectionTimeout);

            try
            {

                if (selectedNode.Parent == tnCollectionSets)
                {
                    // delete collection set
                    using (SqlConnection conn = new SqlConnection())
                    {
                        conn.ConnectionString = ConnectionString;
                        conn.Open();

                        sql = "dbo.sp_syscollector_delete_collection_set";

                        SqlCommand cmd = new SqlCommand(sql, conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = QueryTimeout;
                        SqlCommandBuilder.DeriveParameters(cmd);

                        DataTable dt = CollectorUtils.GetDataTable(Manager.ServerName, "msdb", String.Format("SELECT collection_set_id FROM syscollector_collection_sets WHERE collection_set_uid = '{0}'",selectedNode.Tag));
                        int collection_set_id = Convert.ToInt32(dt.Rows[0]["collection_set_id"]);

                        cmd.Parameters["@collection_set_id"].Value = collection_set_id;
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }

                if (selectedNode.Parent == tnCollectorTypes)
                {
                    // delete collector type
                    using (SqlConnection conn = new SqlConnection())
                    {
                        conn.ConnectionString = ConnectionString;
                        conn.Open();

                        sql = "dbo.sp_syscollector_delete_collector_type";

                        SqlCommand cmd = new SqlCommand(sql, conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = QueryTimeout;
                        SqlCommandBuilder.DeriveParameters(cmd);

                        cmd.Parameters["@collector_type_uid"].Value = new Guid(selectedNode.Tag.ToString());
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }

                if (selectedNode.Parent.Parent == tnCollectionSets)
                {
                    // delete collection item
                    using (SqlConnection conn = new SqlConnection())
                    {
                        conn.ConnectionString = ConnectionString;
                        conn.Open();

                        sql = "dbo.sp_syscollector_delete_collection_item";

                        SqlCommand cmd = new SqlCommand(sql, conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = QueryTimeout;
                        SqlCommandBuilder.DeriveParameters(cmd);

                        cmd.Parameters["@collection_item_id"].Value = Convert.ToInt32(selectedNode.Tag.ToString());
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }

       
    }
}
