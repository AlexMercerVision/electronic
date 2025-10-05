using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.IO;
using CommonCls;
namespace xbDefectDect
{
    

    public partial class SqlServerManage : Form
    {
        public string strWorkDBName;
        private string strDataBPath;
        private string strLogPath;
        private string strSqlDBName;
        private ArrayList alSqlDBNames;         //选择的待备份的数据库名称
        public  string strPWD;
        public string strIP;
        string strUserName;
        public string strDatabase;
        public SqlServerManage()
        {
            InitializeComponent();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 1 || tabControl1.SelectedIndex == 2 || tabControl1.SelectedIndex == 3)
            {
                ArrayList DBNames;
                DBNames = (new SqlOper()).GetMSSQLDBNameList(strIP, "sa", strPWD);
                if (DBNames != null)
                {
                    if (tabControl1.SelectedIndex == 1)
                    {
                        cmbDBNames.Items.Clear();
                        for (int i = 0; i < DBNames.Count; i++)
                        {
                            cmbDBNames.Items.Add((string)DBNames[i]);
                        }
                        cmbDBNames.SelectedIndex = DBNames.Count - 1;
                    }
                    else if (tabControl1.SelectedIndex == 2)
                    {
                        cmbDBNamesbak.Items.Clear();
                        alSqlDBNames = new ArrayList();
                        for (int i = 0; i < DBNames.Count; i++)
                        {
                            cmbDBNamesbak.Items.Add((string)DBNames[i]);
                        }
                        cmbDBNamesbak.SelectedIndex = DBNames.Count - 1;

                    }
                    else if (tabControl1.SelectedIndex == 3)
                    {

                        cmbRestore.Items.Clear();
                        for (int i = 0; i < DBNames.Count; i++)
                        {
                            cmbRestore.Items.Add((string)DBNames[i]);
                        }
                        cmbRestore.SelectedIndex = DBNames.Count - 1;
                    }
                }

            }
        }

        //附加数据库*************************************************************************
        private void btOpen_Click(object sender, EventArgs e)
        {
            //先设置filter属性，再打开对话框
            openFileDialog1.Filter = "数据库文件(*.MDF)|*.mdf";
            openFileDialog1.FileName = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {

                string strDBPath = openFileDialog1.FileName;
                if (strDBPath == null)
                    return;

                strDataBPath = strDBPath;
                txtDBPath.Text = strDBPath;

                string[] strNames = strDBPath.Split('\\');
                string strDBName = strNames[strNames.Length - 1];
                txtDBName.Text = strDBName;
                string[] strNameSuffix = strDBName.Split('.');
                txtSqlDBName.Text = strNameSuffix[0];
                if (System.IO.File.Exists(strDBPath.Substring(0, strDBPath.Length - 4) + ".ldf"))
                {
                    txtLogName.Text = strNameSuffix[0] + ".ldf";
                    strLogPath = strDBPath.Substring(0, strDBPath.Length - 4) + ".ldf";
                }
                else if (System.IO.File.Exists(strDBPath.Substring(0, strDBPath.Length - 4) + "_log.ldf"))
                {
                    txtLogName.Text = strNameSuffix[0] + "_log.ldf";
                    strLogPath = strDBPath.Substring(0, strDBPath.Length - 4) + "_log.ldf";
                }
            }

        }

        private void btOk_Click(object sender, EventArgs e)
        {
            switch(tabControl1.SelectedIndex)
            {
                case 0:
                    strSqlDBName = txtSqlDBName.Text.Trim();

                    if (strDataBPath != null && strLogPath != null && strSqlDBName != null)
                    {
                        (new SqlOper()).AttachDB(strDataBPath, strLogPath, strSqlDBName);
                    }
                    break;
                case 1:
                    if (cmbDBNamesbak.Text != null)
                        if ((new SqlOper()).DetachDB(cmbDBNames.SelectedItem.ToString()))
                        {
                            cmbDBNames.Items.Remove(cmbDBNames.SelectedItem);
                            cmbDBNames.SelectedIndex = cmbDBNames.Items.Count - 1;
                        }
                    break;
                case 2:
                    if (listBox1.Items.Count > 0)
                        for (int i = 0; i < listBox1.Items.Count; i++)
                        {
                            string strDBBackPath = listBox1.Items[i].ToString();
                            (new SqlOper()).BackupDataBase((string)alSqlDBNames[i], strDBBackPath);
                        }
                    break;
                case 3:
                    if (cmbRestore.Text != null && txtRestorePath.Text != null)
                        (new SqlOper()).ReplaceDataBase(cmbRestore.SelectedItem.ToString(), txtRestorePath.Text);
           
                    break;
                case 4:
                    string OriDBPath = "D:\\Program Files\\Microsoft SQL Server\\MSSQL10.MSSQLSERVER\\MSSQL\\Backup\\MySystem_CreateNew.MDF";
                    string OriLOGPath = "D:\\Program Files\\Microsoft SQL Server\\MSSQL10.MSSQLSERVER\\MSSQL\\Backup\\MySystem_CreateNew_log.LDF";                    
                    File.Copy(OriDBPath, txtNewDBPath.Text, true);
                    File.Copy(OriLOGPath, txtNewLogPath.Text, true);
                    if (txtNewDBPath.Text != null && txtNewLogPath.Text!= null && txtNewDBName.Text != null)
                    {
                        (new SqlOper()).AttachDB(txtNewDBPath.Text.Trim(), txtNewLogPath.Text.Trim(), txtNewDBName.Text.Trim());
                    }
                    break;
            }
            
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            try
            {
                switch (tabControl1.SelectedIndex)
                {
                    case 0:
                        txtDBPath.Text = "";
                        txtDBName.Text = "";
                        txtSqlDBName.Text = "";
                        txtLogName.Text = "";
                        break;
                    case 1:
                        break;
                    case 2:
                        listBox1.Items.Clear();
                        alSqlDBNames.Clear();
                        break;
                    case 3:
                        txtRestorePath.Text = "";
                        break;
                    case 4:
                        txtNewDBName.Text = "";
                        txtNewDBPath.Text = "";
                        txtNewLogPath.Text = "";
                        break;
                }
            }
            catch
            {

            }
            
        }
        //附加数据库*************************************************************************



        //分离数据库



        //备份数据库************************************************************

        private void btAdd_Click(object sender, EventArgs e)
        {
            if (cmbDBNamesbak.Text != null)
            {
                saveFileDialog1.FileName = cmbDBNamesbak.SelectedItem.ToString() + "_bak.mdf";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string strDBBackPath = saveFileDialog1.FileName;
                    listBox1.Items.Add(strDBBackPath);
                    alSqlDBNames.Add(cmbDBNamesbak.SelectedItem.ToString());
                }
            }
           
        }

        private void btDel_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex >= 0)
            {
                listBox1.Items.Remove(listBox1.SelectedItem);
                alSqlDBNames.Remove(listBox1.SelectedIndex);
            }
        }      

        //备份数据库************************************************************


        //还原数据库************************************************************

        private void btOpenRestore_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "(备份文件)*.*|*.*";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {

                string strDBPath = openFileDialog1.FileName;
                if (strDBPath == null)
                    return;
                txtRestorePath.Text = strDBPath;
            }
        }
        //还原数据库************************************************************

        //新建数据库*****************************************************************
        private void btOpenNew_Click(object sender, EventArgs e)
        {
            if (txtNewDBName.Text == "")
                return;

            txtNewDBName.Text=txtNewDBName.Text.Trim();
             
                string strSqlDBName;
                if (txtNewDBName.Text.Length > 4 && txtNewDBName.Text.Substring(txtNewDBName.Text.Length - 4, 4) == ".mdf")
                {
                    strSqlDBName = txtNewDBName.Text.Substring(0, txtNewDBName.Text.Length - 4);
                }
                else
                {
                    strSqlDBName = txtNewDBName.Text;
                }
                saveFileDialog1.FileName = strSqlDBName;
                saveFileDialog1.Filter = "(数据库文件)*.mdf|*.mdf";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    txtNewDBPath.Text = saveFileDialog1.FileName;
                    txtNewLogPath.Text = txtNewDBPath.Text.Substring(0, txtNewDBPath.Text.Length - 4) + ".ldf";
                }
            }

        private void SqlServerManage_Load(object sender, EventArgs e)
        {
            this.Left = Screen.PrimaryScreen.Bounds.Width / 2 - this.Bounds.Width / 2;
            this.Top = Screen.PrimaryScreen.Bounds.Height / 2 - this.Bounds.Height / 2;
            strPWD = "skf";
            strIP = "202.202.3.54";
        }

        private void tabDetach_Click(object sender, EventArgs e)
        {

        }

        private void txtNewDBName_TextChanged(object sender, EventArgs e)
        {
            string strPath = "D:\\Program Files\\Microsoft SQL Server\\MSSQL10.MSSQLSERVER\\MSSQL\\Data\\";
             string[] strNames = txtNewDBName.Text.Split('.'); 
             string strNewDBName = strNames[strNames.Length - 1];
             txtNewDBPath.Text =strPath+ strNewDBName + ".mdf";
             txtNewLogPath.Text =strPath+ strNewDBName + "_log.ldf";
        }       
        //新建数据库*****************************************************************

      
    }
}