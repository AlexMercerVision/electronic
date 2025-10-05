using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.OleDb;
using System.Collections;
using CommonCls;
namespace xbDefectDect
{
    
    public partial class DataBaseSelectFrm : Form
    {
        //  public static string strConnection;
        ExcelOper eo = new ExcelOper();
        
        public DataBaseSelectFrm()
        {
            InitializeComponent();
        }

        int iSelectIndex=-1;
        string strDatabase;

        public string strUserName;
        public int iRole = 0;
        public string strID;

        private void ShowDatabaseInfo()
        {

            dgvDataBase.Rows.Clear();
            ArrayList al = new ArrayList();
            eo.ReadDataBaseInfo(al);
            DataBaseInfo dbi;
            int i;
            for (i = 0; i < al.Count; i++)
            {
                dgvDataBase.Rows.Add();
                dbi = (DataBaseInfo)al[i];
                dgvDataBase.Rows[i].Cells[0].Value = dbi.DataBaseUserName;
                dgvDataBase.Rows[i].Cells[1].Value = dbi.Servername;
                dgvDataBase.Rows[i].Cells[2].Value = dbi.DataBaseName;
                dgvDataBase.Rows[i].Cells[3].Value = dbi.OnLineServer;
            }
            strDatabase = "";

        }
        private void DataBaseSelectFrm_Load(object sender, EventArgs e)
        {
            this.Left = Screen.PrimaryScreen.Bounds.Width / 2 - this.Bounds.Width / 2;
            this.Top = Screen.PrimaryScreen.Bounds.Height / 2 - this.Bounds.Height / 2;

          
            ShowDatabaseInfo();
                        
                        dgvDataBase.Rows[0].Selected=true;
            
        }

        private void btAdd_Click(object sender, EventArgs e)
        {
            DataBaseEdit dbe=new DataBaseEdit("");
            
            if (dbe.ShowDialog() == DialogResult.OK)
            {
                ShowDatabaseInfo();
            }
             
             
        }

        private void btEdit_Click(object sender, EventArgs e)
        {
            DataBaseEdit dbe = new DataBaseEdit(strDatabase);
            
            if (strDatabase!="")
            {
                if (dbe.ShowDialog() == DialogResult.OK)
                {
                    ShowDatabaseInfo();
                }
            }
            else
            {
                MessageBox.Show("请选择数据库");
            }
           
        }

        private void btDel_Click(object sender, EventArgs e)
        {
            if (iSelectIndex !=-1)
            {
              int res=  eo.DelCertainDataBaseInfo(iSelectIndex+2);
                ShowDatabaseInfo();
            }
        }

        private void btOk_Click(object sender, EventArgs e)
        {
            DataBaseInfo dbi = new DataBaseInfo();
            eo.ReadCertainDataBaseInfo(ref dbi, strDatabase);
            if (dbi.AuthenticationType == 0)
            {
                FrmManager.strConn = "Server=(local); integrated security =sspi;database=" + dbi.DataBaseName;
                FrmManager.strConnection = "Server=(local); integrated security =sspi;Initial Catalog=master";
            }
            else
            {
                FrmManager.strConn = "Data source=" + dbi.Servername + ";User id=" + dbi.UserName + ";Password=" + dbi.Password + "; Initial Catalog=master;database=" + dbi.DataBaseName;

                FrmManager.strConnection="Data source=" + dbi.Servername + ";User id=" + dbi.UserName + ";Password=" + dbi.Password + "; Initial Catalog=master";
            }
         //   FrmManager.iBit = dbi.iBit;
           
            Login loginFrm=new Login();
            loginFrm.Owner = this;
            if (loginFrm.ShowDialog()==DialogResult.OK)
            {
                FrmManager mf = (FrmManager)this.Owner;
                mf.Visible = true;
                mf.strUserName = strUserName;
                mf.strServer = dbi.Servername;
                mf.strDatabase = dbi.DataBaseName;
                mf.strID = strID;
                mf.iRole = iRole;
             /*   mf.strIP = dbi.OnLineServer;
                mf.iPort = dbi.OnLineServerPort;*/
                this.DialogResult = DialogResult.OK;
            }
        }

        private void btCancle_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
           
        }

        private void dgvDataBase_SelectionChanged(object sender, EventArgs e)
        {



            if (dgvDataBase.SelectedRows.Count > 0 && dgvDataBase.SelectedRows[0].Cells[0].Value != null)
            {
                strDatabase = dgvDataBase.SelectedRows[0].Cells[0].Value.ToString();
                iSelectIndex = dgvDataBase.SelectedRows[0].Index;
            }
            
        }

        private void DataBaseSelectFrm_FormClosed(object sender, FormClosedEventArgs e)
        {
           eo.CloseExcelApp();
          //  Application.Exit();
        }

        private void BTDbEdit_Click(object sender, EventArgs e)
        {
          //  MainFrm.strConnection = "Server=(local); integrated security =sspi;Initial Catalog=master";

         //   MainFrm.strConnection = "Data source=(local);User id=sa;Password=skf; Initial Catalog=master";
            
            SqlServerManage ssm = new SqlServerManage();
            ssm.strDatabase = strDatabase;
            ssm.ShowDialog();
        }
    }
}