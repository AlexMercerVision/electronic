using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using CommonCls;
using System.Windows.Forms;
//using MathWorks.MATLAB.NET.Arrays;
//using MathWorks.MATLAB.NET.Utility;
//using TestClc;
//using Manifold_Learning;
//using Poly;
//using pca;
//using LifePre;
//using TestZ;


namespace xbDefectDect
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btOK_Click(object sender, EventArgs e)
        {
            string strUserName;
            string strID = txtUserName.Text.Trim();
            string strPwd = txtPassword.Text.Trim();
            int iRole = 0;
            if (strID != "")
            {
                strUserName = (new SqlOper()).Login(strID, strPwd,ref iRole);
                if (strUserName == "")
                {
                    MessageBox.Show("账号或密码不正确!");
                    return;
                }
                else
                {
                    this.DialogResult = DialogResult.OK;
                    FileRW frw = new FileRW();
                    frw.WritePssWrd(chkPssWrd.Checked, strID, strPwd);
                    Type typ=this.Owner.GetType();
                    if (typ.Name == "DataBaseSelectFrm")
                    {
                        DataBaseSelectFrm frmOwn = (DataBaseSelectFrm)this.Owner;
                        frmOwn.strUserName = strUserName;
                        frmOwn.strID = strID;
                        frmOwn.iRole = iRole;
                    }
                    else if (typ.Name == "MainFrm")
                    {
                        FrmManager frmOwn = (FrmManager)this.Owner;
                        
                        frmOwn.strUserName = strUserName;
                        frmOwn.strID = strID;
                        frmOwn.iRole = iRole;
                    }
               //     frw.CheckOpenGL();
                    this.Close();
                }
            }
            else
                MessageBox.Show("请输入登陆账号!");

        }

        private void Login_Load(object sender, EventArgs e)
        {
            this.Left = Screen.PrimaryScreen.Bounds.Width / 2 - this.Bounds.Width / 2;
            this.Top = Screen.PrimaryScreen.Bounds.Height / 2 - this.Bounds.Height / 2;
            
            FileRW frw=new FileRW();
            bool bRem=false;
            string strID="";
            string strPwd="";

            frw.ReadPssWrd(ref bRem, ref strID, ref strPwd);
            if (bRem)
            {
                chkPssWrd.Checked = bRem;
                txtUserName.Text = strID;
                txtPassword.Text = strPwd;
            }
        }

        private void Login_FormClosed(object sender, FormClosedEventArgs e)
        {
            //Application.Exit();
        }

        private void txtPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar==13)
            {
                btOK_Click(sender, e);
            }

        }

      

        private void button1_Click(object sender, EventArgs e)
        {

           /*
            double[] data = new double[1024];
                       Random rand = new Random();
                       for (int i = 0; i < 1024;i++ )
                       {
                           data[i] = rand.Next(100)/100.0;
                       }
                       SignalAna sa = new SignalAna();
                       int tau=0, m=0;
                       sa.C_CMethod(data, ref tau, ref m);
                       int[] a = { 1, 2, 3 };
                       int[] b = { 1, 2, 3 };*/
           
         /*   MaxAdd tst = new MaxAdd();    */

           // InfoFuse infoF = new InfoFuse();
           // infoF.main();
            /*
            MWArray result = null;
                       ORTH orth = new ORTH();
                      result= orth.Caclu();*/
           
           
            //result = tst.MAdd((MWNumericArray)a, (MWNumericArray)b);
        }

        private void btUserManage_Click(object sender, EventArgs e)
        {
            string strUserName;
            string strID = txtUserName.Text.Trim();
            string strPwd = txtPassword.Text.Trim();
            int iRole = 0;
            if (strID != "")
            {
                strUserName = (new SqlOper()).Login(strID, strPwd,ref iRole);
                if (strUserName == "")
                {
                    MessageBox.Show("账号或密码不正确!");
                    return;
                }
                else
                {
                    UserManage um = new UserManage();
                    um.iRole = iRole;
                    um.strAccount = strID;
                    um.ShowDialog();
                }
            }
            else
                MessageBox.Show("请输入登陆账号!");
           
        }
    }
}