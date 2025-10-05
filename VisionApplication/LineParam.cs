using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommonCls;
using System.IO;

namespace xbDefectDect
{
    public partial class LineParam : Form
    {
        public bool bAddOrEdit;
       // public int m_iParentNode;
        int iOperType = -1;
        NodeInfo ni = new NodeInfo();
        public LineParam()
        {
            InitializeComponent();
             
        }
        int iMainNode, iSubNode;
        SqlOper sqlOper = new SqlOper();
        string strGroupName, strGroupDesc;
        public LineParam(int iMainNode, int iSubNode,int iPasteMainNode)
        {
            InitializeComponent();
            this.iMainNode = iPasteMainNode;
         //   this.iSubNode = iSubNode;
            this.iSubNode = sqlOper.GetMaxNodeIndex(iPasteMainNode) + 1; 
            iOperType = 3;
            groupBox2.Enabled = false;
            UpdateGroupInfo(iPasteMainNode);
            UpdateNodeInfo(iMainNode, iSubNode);
        }
        public LineParam(int iMainNode,int iSubNode)
        {
            InitializeComponent();
          
            this.iMainNode = iMainNode;
            this.iSubNode = iSubNode;
            if (iMainNode == 0 && iSubNode == 0) //添加父节点
            {
                this.iSubNode = sqlOper.GetMaxNodeIndex(0) + 1;
                iOperType = 1;
                groupBox1.Visible = false;
                this.Height = 170;
            }
            else if (iMainNode == 0 && iSubNode >0)  //编辑父节点
            {
                
                iOperType = 2;
                groupBox1.Visible = false;
                this.Height = 170;
                UpdateGroupInfo(iSubNode);
            }
            else if (iMainNode > 0 && iSubNode == 0)  //添加子节点
            {
                this.iSubNode = sqlOper.GetMaxNodeIndex(iMainNode) + 1;             
                iOperType = 3;
                groupBox2.Enabled = false;

                UpdateGroupInfo(iMainNode);
            }
            if (iMainNode > 0 && iSubNode > 0)       //编辑子节点
            {
                 iOperType = 4;
                groupBox2.Enabled = false;
                UpdateGroupInfo(iMainNode);
                UpdateNodeInfo(iMainNode, iSubNode);
            }
        }

        private void UpdateGroupInfo(int iSubNode)
        {
            sqlOper.ReadParNodeInfo(iSubNode, ref strGroupName, ref strGroupDesc);
            txtGroupName.Text = strGroupName;
            txtGroupDesc.Text = strGroupDesc; 
        }
        private void UpdateNodeInfo(int iMainNode, int iSubNode)
        {
            ni = sqlOper.ReadLineInfo(iMainNode, iSubNode, ref strGroupName, ref strGroupDesc);
            txtLineName.Text = ni.strLineName;
            txtLineDesc.Text = ni.strLineDesc;
            txtLineIndex.Text = ni.indexLine.ToString();
            txtLineIP.Text = ni.strIP;
            txtLineComputerName.Text = ni.strComputerName;
            if (ni.sceneImg != null && ni.sceneImg.Length != 0)
                picSence.Image = picSence.Image = Image.FromStream(new MemoryStream(ni.sceneImg));
        }
        private void picSence_Click(object sender, EventArgs e)
        {

        }
        byte[] bSqlData;
        private void picSence_DoubleClick(object sender, EventArgs e)
        {
            //获取图片路径
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
             openFileDialog1.ShowDialog();
            openFileDialog1.Filter = "图片文件(*.jpg,*.gif,*.bmp)|*.jpg;*.bmp;*.gif|*.bmp|*.bmp";
            string strPicName = openFileDialog1.FileName;          
            if (!File.Exists(strPicName))
            { 
                return;
            }
            //将图片转化为二进制文件
            FileStream file; 
            file = new FileStream(strPicName, FileMode.Open, FileAccess.Read);
            bSqlData = new byte[file.Length];
            file.Read(bSqlData, 0, (int)file.Length);
            file.Close();
            picSence.Image = Image.FromStream(new MemoryStream(bSqlData));
            ni.sceneImg = bSqlData;
        }

        private void btOK_Click(object sender, EventArgs e)
        {
            if (iOperType == 3 || iOperType == 4)
            {
                ni.iParNode = iMainNode;
                ni.iNode = iSubNode;

                ni.strLineName = txtLineName.Text.Trim();
                ni.strLineDesc = txtLineDesc.Text.Trim();
                Int32.TryParse(txtLineIndex.Text.Trim(), out ni.indexLine);

                ni.strIP = txtLineIP.Text.Trim();
                ni.strComputerName = txtLineComputerName.Text.Trim();

                if (picSence.Image == null)
                    ni.sceneImg = new byte[0];
               /* else
                    ni.sceneImg = bSqlData;*/
                ni.NodeStatus = 0;
                if(iOperType == 3)
                { 
                    sqlOper.InsertNewLineParam(ni);
                }
                else if (iOperType == 4)
                {
                     sqlOper.UpdateLineParam(ni);
                }
            }
            else
            {
                strGroupName= txtGroupName.Text.Trim();
                strGroupDesc= txtGroupDesc.Text.Trim();

                if (iOperType == 1)
                {
                    sqlOper.InsertGroupParam(iMainNode, iSubNode, strGroupName, strGroupDesc);
                }
                else
                {
                    sqlOper.UpdateGroupParam(iMainNode, iSubNode, strGroupName, strGroupDesc);
                }
            }
         //   ni.cup = new CUserParam();

        }
    }
}
