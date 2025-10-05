using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using CommonCls;

namespace xbDefectDect
{
    public partial class DataBaseEdit : Form
    {
        string strDatabaseName;
        DataBaseInfo dbi;
        int iType;

        SqlOper sqlOper = new SqlOper();
        ExcelOper excelOper = new ExcelOper();
        public DataBaseEdit()
        {
            InitializeComponent();
        }
        public DataBaseEdit(string strDBName)
            : this()
        {
            strDatabaseName = strDBName;
            if (strDatabaseName == "")
            {
                iType = 0;
            }
            else
                iType = 1;
           // this.WindowState = FormWindowState.Maximized;
        }

        private void DataBaseEdit_Load(object sender, EventArgs e)
        {
            this.Left = Screen.PrimaryScreen.Bounds.Width / 2 - this.Bounds.Width / 2;
            this.Top = Screen.PrimaryScreen.Bounds.Height / 2 - this.Bounds.Height / 2;
            cmbAuthentication.SelectedIndex = 0;
            if (strDatabaseName!="")
            {
                excelOper.ReadCertainDataBaseInfo(ref dbi,strDatabaseName);
                txtDatabaseUserName.Text = dbi.DataBaseUserName;
                cmbServerName.Text = dbi.Servername;
                cmbAuthentication.SelectedIndex = dbi.AuthenticationType;
                txtUserName.Text= dbi.UserName;
                txtPassword.Text = dbi.Password;
                cmbDatabaseName.Text = dbi.DataBaseName;
                txtOnLineServer.Text = dbi.OnLineServer;
                txtOnLineServerPort.Text = dbi.OnLineServerPort.ToString();
            }
            if (cmbAuthentication.SelectedIndex==0)
            {
                txtUserName.Visible = false;
                txtPassword.Visible = false;
                lblUserName.Visible = false;
                lblPassword.Visible = false;
            }
            cmbServerName.Items.Add("(local)");
            cmbServerName.Items.Add(System.Net.Dns.GetHostName());
            cmbDatabaseName.Items.Clear();

        }

        private void btOk_Click(object sender, EventArgs e)
        {
            dbi.DataBaseUserName = txtDatabaseUserName.Text.Trim();
            dbi.DataBaseName = cmbDatabaseName.Text.Trim();
            dbi.AuthenticationType = cmbAuthentication.SelectedIndex;
            dbi.Servername = cmbServerName.Text.Trim();
            dbi.UserName = txtUserName.Text.Trim();
            dbi.Password = txtPassword.Text.Trim();
            dbi.OnLineServer = txtOnLineServer.Text.Trim();
            if (txtOnLineServerPort.Text.Trim()!="")
            {
                dbi.OnLineServerPort = Int32.Parse(txtOnLineServerPort.Text.Trim());
            }
            else
            {
                dbi.OnLineServerPort = 2001;
            }

            int iResult;
            if (iType == 0)
            {
                iResult = excelOper.AddDataBaseInfo(dbi);
                if (iResult==1)
                {
                    MessageBox.Show("数据库名重复,请重新输入");
                    return;
                }
            }
            else
            {
                iResult = excelOper.EditCertainDataBaseInfo(dbi, strDatabaseName);
                if (iResult == 1)
                {
                    MessageBox.Show("数据库名重复,请重新输入");
                    return;
                }
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void cmbAuthentication_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbAuthentication.SelectedIndex==0)
            {
                txtUserName.Visible = false;
                txtPassword.Visible = false;
                lblUserName.Visible = false;
                lblPassword.Visible = false;
            }
            else
            {
                txtUserName.Visible = true;
                txtPassword.Visible = true;
                lblUserName.Visible = true;
                lblPassword.Visible = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (cmbServerName.Text.Trim() != "")
            {
                ArrayList DBNames;
                if (cmbAuthentication.SelectedIndex==1)
                {
                    if (txtUserName.Text.Trim()!="")
                    {
                        
                        DBNames = sqlOper.GetMSSQLDBNameList(cmbServerName.Text.Trim(), txtUserName.Text.Trim(), txtPassword.Text.Trim());
                    }
                    else
                    {
                        MessageBox.Show("请输入登录用户名");
                        return;
                    }
                }
                else
                {
                    DBNames =sqlOper.AnoGetMSSQLDBNameList(cmbServerName.Text.Trim());
                }
                if (DBNames!=null)
                {
                    if (DBNames.Count > 0)
                    {
                        string strConn="";
                        cmbDatabaseName.Items.Clear();
                        if (dbi.AuthenticationType == 0)
                        {
                            strConn = "Server=(local); integrated security =sspi;database=" ;

                        }
                        else
                        {
                            strConn = "Data source=" + dbi.Servername + ";User id=" + dbi.UserName + ";Password=" + dbi.Password + "; Initial Catalog=master;database=";


                        }
                        for (int i = 0; i < DBNames.Count; i++)
                        {


                            FrmManager.strConn = strConn + (string)DBNames[i];
                            if (sqlOper.SearchTable((string)DBNames[i]))
                            {
                                cmbDatabaseName.Items.Add((string)DBNames[i]);
                            }
                            
                        }
                    }
                }
                
            }
            else
            {
                MessageBox.Show("请输入数据库服务器名");
            }
        }

        private void btCancle_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void cmbDatabaseName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbDatabaseName.Text!=null)
            {
                txtDatabaseUserName.Text = cmbDatabaseName.Text.Trim();
            }
            
        }

        private void DataBaseEdit_FormClosed(object sender, FormClosedEventArgs e)
        {
            //Application.Exit();
        }
    }
}