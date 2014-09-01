using CollectionSetManager.Properties;
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
    public partial class SelectPackageDialog : Form
    {

        static ImageList _imageList;
        public static ImageList ImageList
        {
            get
            {
                if (_imageList == null)
                {
                    _imageList = new ImageList();
                    _imageList.Images.Add("package", Resources.package);
                    _imageList.Images.Add("folder", Resources.folder);
                    _imageList.Images.Add("server", Resources.server);
                }
                return _imageList;
            }
        }


        private TreeNode tnRoot = null;
        public Guid SelectedPackage { get; set; }
        public String SelectedPackagePath { get; set; }

        public SelectPackageDialog()
        {
            InitializeComponent();
            PopulateTreeView();
        }



        private void PopulateTreeView()
        {
            treeView1.ImageList = ImageList;

            //
            TreeNode[] subnodes = PopulateFolder(new Guid("00000000-0000-0000-0000-000000000000"));
            tnRoot = new TreeNode(Manager.ServerName, subnodes);
            tnRoot.ImageKey = "server";
            tnRoot.SelectedImageKey = "server";
            treeView1.Nodes.Add(tnRoot);
            treeView1.ExpandAll();


        }

        private TreeNode[] PopulateFolder(Guid folderid)
        {
            String sql = @"
                SELECT 'Package' AS Type, P.id, P.name
                FROM msdb.dbo.sysssispackages AS P
                WHERE P.folderid = '{0}'

                UNION ALL

                SELECT 'Folder' AS Type, F.folderid, F.foldername
                FROM msdb.dbo.sysssispackagefolders AS F
                WHERE F.parentfolderid = '{0}'

            ";

            sql = String.Format(sql, folderid);

            DataTable dt = CollectorUtils.GetDataTable(Manager.ServerName, "msdb", sql);
            List<TreeNode> nodes = new List<TreeNode>();
            foreach (DataRow dr in dt.Rows)
            {
                if (dr["Type"].ToString().Equals("Folder"))
                {
                    TreeNode tn = null;
                    TreeNode[] children = PopulateFolder((Guid)dr["id"]);
                    if (children.Length > 0)
                    {
                        tn = new TreeNode(dr["name"].ToString(), children);
                    }
                    else
                    {
                        tn = new TreeNode(dr["name"].ToString());
                    }
                    tn.ImageKey = "folder";
                    tn.SelectedImageKey = "folder";
                    nodes.Add(tn);
                }
                else
                {
                    TreeNode tn = new TreeNode(dr["name"].ToString());
                    tn.ImageKey = "package";
                    tn.SelectedImageKey = "package";
                    tn.Tag = dr["id"];
                    nodes.Add(tn);
                }

            }
            return nodes.ToArray();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SelectedPackage = new Guid(treeView1.SelectedNode.Tag.ToString());
            SelectedPackagePath = BuildPath(treeView1.SelectedNode);
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            button1.Enabled = (treeView1.SelectedNode.Tag != null);
        }

        private String BuildPath(TreeNode tn)
        {
            String results = "";
            while (tn != tnRoot)
            {
                results = "\\\\" + tn.Text + results;
                tn = tn.Parent;
            }
            return results;
        }

    }
}
