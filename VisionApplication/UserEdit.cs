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
    public partial class UserEdit : Form
    {
       struUserInfo stru_UserInfo;
        bool bAddorEdit;
        public int iRole = 0;
        public UserEdit()
        {
            InitializeComponent();
        }
        public UserEdit(struUserInfo ui)
            : this()
        {
             stru_UserInfo = ui;
        }
        private void btOk_Click(object sender, EventArgs e)
        {
           
            if (txtPwd.Text != txtPwdConfirm.Text)
            {
                MessageBox.Show("密码不一致，请重新输入!");
                return;
            }

            if (!bAddorEdit && stru_UserInfo.strID=="admin") //管理员编辑自己的信息
            {
                if (!rdBtSupperUser.Checked)
                {
                    MessageBox.Show("不能降低管理员权限");
                    return;
                }
            }
            else 
            {
                if (rdBtSupperUser.Checked)
                {
                    MessageBox.Show("不能设置管理员权限");
                    return;
                }
            }
          
            stru_UserInfo.strID=txtAccount.Text.Trim();
            if (stru_UserInfo.strID=="")
            {
                MessageBox.Show("用户名不能为空");
                return;
            }
            stru_UserInfo.strUserName=txtUserName.Text;
            stru_UserInfo.strPwd = txtPwdConfirm.Text;
            if (stru_UserInfo.strPwd == "")
            {
                MessageBox.Show("密码不能为空");
                return;
            }

            stru_UserInfo.strEamil=txtEmail.Text;
            
            stru_UserInfo.strTel=txtPhone.Text;
          
            
            /*if (rdBtSupperUser.Checked)
            {
                stru_UserInfo.iRole = 1;

            }
            else if (rdBtOperator.Checked)
            {
                stru_UserInfo.iRole = 2;
            }
            else if (rdBtAnalyzer.Checked)
            {
                stru_UserInfo.iRole = 3;
            }
            else if (rdBtCustom.Checked)
            {
                stru_UserInfo.iRole = 4;
            }*/
            int iRight0 = 1, iRight1 = 1, iRight2 = 1;
            if (chkUserManage.Checked)
            {
                iRight0 += 4;
            }
            if (chkDatabaseManage.Checked)
            {
                iRight0 += 2;
            }
            if (chkNodeConfig.Checked)
            {
                iRight1 += 4;
            }
            if (chkDataAnasys.Checked)
            {
                iRight1 += 2;
            }
            if (chkReportEmail.Checked)
            {
                iRight2 += 4;
            }
            if (chkDataBrowse.Checked)
            {
                iRight2 += 2;
            }
            stru_UserInfo.strAccess = iRight0.ToString() + iRight1.ToString() + iRight2.ToString();
            (new SqlOper()).AddorEditUserInfo(stru_UserInfo, bAddorEdit);
            
            this.DialogResult = DialogResult.OK;
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void UserEdit_Load(object sender, EventArgs e)
        {
            this.Left = Screen.PrimaryScreen.Bounds.Width / 2 - this.Bounds.Width / 2;
            this.Top = Screen.PrimaryScreen.Bounds.Height / 2 - this.Bounds.Height / 2;
            if (stru_UserInfo.strID!=null)
            {
                bAddorEdit = false;
                txtAccount.Text = stru_UserInfo.strID;
                txtAccount.Enabled = false;
                txtUserName.Text = stru_UserInfo.strUserName;
                txtPwd.Text =txtPwdConfirm.Text= stru_UserInfo.strPwd;
                txtEmail.Text = stru_UserInfo.strEamil;
                txtPhone.Text = stru_UserInfo.strTel;
                switch(stru_UserInfo.iRole)
                {
                    case 1:
                        rdBtSupperUser.Checked = true;
                        break;
                    case 2:
                        rdBtOperator.Checked = true;
                        break;
                    case 3:
                        rdBtAnalyzer.Checked = true;
                        break;
                    case 4:
                        rdBtCustom.Checked = true;
                        break;
                    default:
                        rdBtAnalyzer.Checked = true;
                        break;
                }
                if (iRole!=1)
                {
                    groupBox2.Enabled = false;
                }
                /*int iRight0 = Int32.Parse(stru_UserInfo.strAccess.Substring(0, 1));
                int iRight1 = Int32.Parse(stru_UserInfo.strAccess.Substring(1, 1));
                int iRight2 = Int32.Parse(stru_UserInfo.strAccess.Substring(2, 1));

                if (iRight0==7)
                {
                    chkUserManage.Checked = true;
                    chkDatabaseManage.Checked = true;                   
                }
                else if (iRight0==3)
                {
                    chkDatabaseManage.Checked = true; 
                }
                if (iRight1==7)
                {
                    chkNodeConfig.Checked = true;
                    chkDataAnasys.Checked = true;
                }
                else if (iRight1==3)
                {
                    chkDataAnasys.Checked = true;
                }
                if (iRight2==7)
                {
                    chkReportEmail.Checked = true;
                    chkDataBrowse.Checked = true;
                }
                else if (iRight2==3)
                {
                    chkDataBrowse.Checked = true;
                }*/
            }
            else
            {
                bAddorEdit = true;
                rdBtCustom.Checked = true;
            }
            
        }

        private void rdBtSupperUser_CheckedChanged(object sender, EventArgs e)
        {
            if (rdBtSupperUser.Checked)
            {
                stru_UserInfo.iRole = 1;
                chkUserManage.Checked = true;
                chkDatabaseManage.Checked = true;
                chkDataBrowse.Checked = true;
                chkDataAnasys.Checked = true;
            }
        }

        private void rdBtOperator_CheckedChanged(object sender, EventArgs e)
        {
            if (rdBtOperator.Checked)
            {
                stru_UserInfo.iRole = 2;
                chkUserManage.Checked = false;
                chkDatabaseManage.Checked = true;
                chkDataBrowse.Checked = true;
                chkDataAnasys.Checked = true;
            }
        }

        private void rdBtAnalyzer_CheckedChanged(object sender, EventArgs e)
        {
            if (rdBtAnalyzer.Checked)
            {
                stru_UserInfo.iRole = 3;
                chkUserManage.Checked = false;
                chkDatabaseManage.Checked = false;
                chkDataBrowse.Checked = true;
                chkDataAnasys.Checked = true;
            }
        }

        private void rdBtCustom_CheckedChanged(object sender, EventArgs e)
        {
            if (rdBtCustom.Checked)
            {
                stru_UserInfo.iRole = 4;
                chkUserManage.Checked = false;
                chkDatabaseManage.Checked = false;
                chkDataBrowse.Checked = true;
                chkDataAnasys.Checked = false;
            }
        }
    }
}