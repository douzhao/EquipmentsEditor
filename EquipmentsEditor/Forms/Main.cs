﻿using EquipmentsEditor.Helper;
using EquipmentsEditor.Model;
using EquipmentsEditor.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace EquipmentsEditor.Forms
{
    public partial class Main : Form
    {
        LoadDataService loadDataService = new LoadDataService();
        DataModel dataModel = new DataModel();
        CountryModel country = new CountryModel();
        private string originalFilePath = string.Empty;

        public Main()
        {
            InitializeComponent();
            this.dataGridView1.AutoGenerateColumns = false;

            //loadDataService.LoadBacisData(dataModel, System.IO.Directory.GetCurrentDirectory() + "\\autosave.hoi4");
            //foreach (CountryModel country in dataModel.CountriesList)
            //{
            //    ListViewItem item = new ListViewItem(country.Name);
            //    item.ToolTipText = country.ShortName;
            //    this.LV_Countries.Items.Add(item);
            //}
        }

        private void 打开存档ToolStripMenuItem_Click_1(object sender, EventArgs e)
        {

            FileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "所有文件(*.hoi4*)|*.hoi4*";
            fileDialog.Filter = "所有文件(*.*)|*.*";
            fileDialog.InitialDirectory = "";

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                string saveGamePath = fileDialog.FileName;
                if (!string.IsNullOrEmpty(saveGamePath))
                {
                    this.LV_Countries.Items.Clear();
                    this.tv_EquipmentType.Nodes.Clear();
                    originalFilePath = saveGamePath;
                    loadDataService.LoadBacisData(dataModel, saveGamePath);
                    foreach (CountryModel country in dataModel.CountriesList)
                    {
                        ListViewItem item = new ListViewItem(country.Name);
                        item.ToolTipText = country.ShortName;
                        this.LV_Countries.Items.Add(item);
                    }
                }
            }
        }


        private void 生成存档ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "所有文件(*.hoi4*)|*.hoi4*";
            fileDialog.Filter = "所有文件(*.*)|*.*";
            fileDialog.InitialDirectory = "";

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                string saveGamePath = fileDialog.FileName;
                if (!string.IsNullOrEmpty(saveGamePath))
                {
                    string fileStr = new GenerateServices(originalFilePath, dataModel, country).GenerateCode();


                    StreamWriter sw = null;
                    try
                    {
                        UTF8Encoding utf8 = new UTF8Encoding(false);

                        using (sw = new StreamWriter(saveGamePath, false, utf8))
                        {
                            sw.Write(fileStr);
                        }

                    }

                    catch
                    {
                        throw new Exception("存储路径错误,请检查路径" + saveGamePath + "是否存在!");
                    }

                    finally
                    {
                        sw.Close();
                        CommonService.ShowErrorMessage("保存成功！");
                    }

                    //   FileStream fs1 = new FileStream(saveGamePath, FileMode.Create, FileAccess.Write);//创建写入文件 
                    //   UTF8Encoding utf8 = new UTF8Encoding(false);

                    //UTF8Encoding utf8 = new UTF8Encoding(false);

                    //   StreamWriter sw = new StreamWriter(fs1, false, utf8);
                    //   sw.WriteLine(fileStr);//开始写入值

                    //   sw.Close();
                    //   fs1.Close();
                }
            }
        }

        private void LV_Countries_Click(object sender, EventArgs e)
        {
            string shortName = this.LV_Countries.FocusedItem.ToolTipText;

            country = dataModel.CountriesList.Find(c => c.ShortName == shortName);

            if (country != null)
            {
                this.tv_EquipmentType.Nodes.Clear();
                this.dataGridView1.DataSource = new List<EquipmentModel>();
                if (country.EquipmentsList.Count == 0)
                    loadDataService.LoadDataByCountry(dataModel, country);

                XDocument xdoc = XDocument.Load(System.IO.Directory.GetCurrentDirectory() + "\\ConfigFile\\Equipment.xml");
                XElement xeRoot = xdoc.Root;//2.获取根节点
                //3.把根节点加到TreeView上。
                TreeNode treeViewRoot = this.tv_EquipmentType.Nodes.Add("类型");
                LoadNodes(xeRoot, treeViewRoot, country);//4.递归加载
                this.tv_EquipmentType.Nodes[0].Expand();
            }
        }

        #region 后勤
        private void LoadNodes(XElement xeRoot, TreeNode treeViewRoot, CountryModel country)
        {
            //把xeRoot下面的内容循环绑定到treeViewRoot下面
            foreach (XElement ele in xeRoot.Elements())
            {
                XAttribute attrId = ele.Attribute("id");
                XAttribute attrName = ele.Attribute("name");

                if (country.EquipmentsList.Find(e => e.TypeStr.IndexOf(attrId.Value) > -1) != null)//判断当前选择国家中是否拥有此装备类别
                {
                    if (ele.Elements().Count() > 0)
                    {
                        TreeNode node = new TreeNode();
                        node.Text = attrName.Value;
                        node.Name = attrId.Value;
                        treeViewRoot.Nodes.Add(node);
                        LoadNodes(ele, node, country);
                    }
                    else
                    {
                        TreeNode node = new TreeNode();
                        if (attrName != null)
                        {
                            node.Text = attrName.Value;
                        }
                        else
                        {
                            node.Text = loadDataService.GetEquipmentName(attrId.Value);
                        }
                        node.Name = attrId.Value;
                        treeViewRoot.Nodes.Add(node);
                    }
                }
            }
        }

        private void tv_EquipmentType_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string equipTypeStr = this.tv_EquipmentType.SelectedNode.Name.ToString();
            this.dataGridView1.DataSource = new List<EquipmentModel>();
            BindGridview(equipTypeStr);

        }
        private void BindGridview(string equipTypeStr)
        {
            List<EquipmentModel> list = FindEquipmentByTreeNode(this.tv_EquipmentType.SelectedNode, equipTypeStr);
            this.dataGridView1.DataSource = list;
        }

        private List<EquipmentModel> FindEquipmentByTreeNode(TreeNode node, string equipTypeStr)
        {
            List<EquipmentModel> list = new List<EquipmentModel>();
            if (node.Nodes.Count > 0)
            {
                foreach (TreeNode childNode in node.Nodes)
                {
                    list.AddRange(FindEquipmentByTreeNode(childNode, childNode.Name));
                }
            }
            else
            {
                list = country.EquipmentsList.FindAll(eq => eq.TypeStr.IndexOf(equipTypeStr) == 0 && eq.IsDeleted == false);
            }
            return list;
        }

        private void btn_DeleteAll_Click(object sender, EventArgs e)
        {
            List<EquipmentModel> foreignLeaseList = country.EquipmentsList.FindAll(f => f.IsForeignLease == true && f.IsDeleted == false);
            foreach (EquipmentModel model in foreignLeaseList)
            {
                model.IsDeleted = true;

                EquipmentModel localModel = country.EquipmentsList.Find(f => f.IsForeignLease == false && f.TypeStr == model.TypeStr);
                if (localModel != null)
                {
                    localModel.Quantity += model.Quantity;
                }
            }
            string equipTypeStr = this.tv_EquipmentType.SelectedNode.Name.ToString();
            BindGridview(equipTypeStr);
        }

        private void btn_RecoverAll_Click(object sender, EventArgs e)
        {
            List<EquipmentModel> foreignLeaseList = country.EquipmentsList.FindAll(f => f.IsForeignLease == true && f.IsDeleted == true);
            foreach (EquipmentModel model in foreignLeaseList)
            {
                model.IsDeleted = false;

                EquipmentModel localModel = country.EquipmentsList.Find(f => f.IsForeignLease == false && f.TypeStr == model.TypeStr);
                if (localModel != null)
                {
                    localModel.Quantity -= model.Quantity;
                }
            }
            string equipTypeStr = this.tv_EquipmentType.SelectedNode.Name.ToString();
            BindGridview(equipTypeStr);
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            //dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].d.ToString();
            if (dataGridView1.Rows.Count > 0)
            {
                string id = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString();
                EquipmentModel model = country.EquipmentsList.Find(eq => eq.Id == id);

                string quantityStr = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                double quantity = 0;
                double.TryParse(quantityStr, out quantity);
                model.Quantity = quantity;
            }
        }
        #endregion

    }
}
