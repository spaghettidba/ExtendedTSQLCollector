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

        TreeNode tnRoot = null;
        TreeNode tnCollectorTypes;
        TreeNode tnCollectionSets; 
        TreeNode tnCollectorLogs;

        public Main()
        {
            InitializeComponent();
            PopulateTreeView();
        }

        private void PopulateTreeView()
        {
            TreeNode[] cTypes = PopulateCollectorTypes();
            TreeNode[] cSets = PopulateCollectionSets();
            TreeNode tnCollectorTypes = new TreeNode("Collector Types",cTypes);
            TreeNode tnCollectionSets = new TreeNode("Collection Sets",cSets);
            TreeNode tnCollectorLogs = new TreeNode("Logs");

            TreeNode[] subnodes = new TreeNode[] {tnCollectorTypes, tnCollectionSets, tnCollectorLogs};
            tnRoot = new TreeNode(Manager.ServerName, subnodes);
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
                nodes.Add(tn);
            }
            return nodes.ToArray();
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
    }
}
