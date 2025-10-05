using System; 
using System.Windows.Forms;
using System.Net.Sockets;
 
using System.Net;
using CommonCls;

namespace VisionApplication
{
    public partial class Login : Form
    {
        public int iModelRole = 0;
        public string strID;
        private Protect mPasswordFile;
        public Login()
        {
            InitializeComponent();
        }
        public Login(Protect file)
        {
            InitializeComponent();
            mPasswordFile = file;
        }
        private void btCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btOK_Click(object sender, EventArgs e)
        {
          
             
            if (iModelRole < 3)
            {
              //  strID = cmbAccessLevel.Text.Trim();
                string strPwd = txtPassword.Text.Trim();
                switch (cmbAccessLevel.SelectedIndex)
                {
                    case 0:
                        string strMM = mPasswordFile.GetPasswordForAccessLevel(AccessLevel.Operator);
                        if (strPwd == mPasswordFile.GetPasswordForAccessLevel(AccessLevel.Operator))
                        {
                            iModelRole = 1;  //运维员
                        }
                        else
                        {
                            iModelRole = 0;
                            MessageBox.Show("密码错误，请重新输入");
                        }
                        break;
                    case 1:
                        if (strPwd == mPasswordFile.GetPasswordForAccessLevel(AccessLevel.Administrator))
                        {
                            iModelRole = 2;  //管理员
                        }
                        else
                        {
                            iModelRole = 0;
                            MessageBox.Show("密码错误，请重新输入");
                        }
                        break;
                    case 2:
                        string strPwdSuper = mPasswordFile.GetPasswordForAccessLevel(AccessLevel.Supervisor);
                        if (strPwd == mPasswordFile.GetPasswordForAccessLevel(AccessLevel.Supervisor))
                        {
                            iModelRole = 3;  //超级主管
                        }
                        else
                        {
                            strMM = mPasswordFile.GetPasswordForAccessLevel(AccessLevel.Operator);
                            iModelRole = 0;
                            MessageBox.Show("密码错误，请重新输入");
                        }
                        break;
                }
            }
            else
            {
                cmbAccessLevel.SelectedIndex = 2;
            }

            strID = cmbAccessLevel.Text.Trim();
            if (iModelRole > 0)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
             
        }

        private void Login_Load(object sender, EventArgs e)
        {
            this.Left = Screen.PrimaryScreen.Bounds.Width / 2 - this.Bounds.Width / 2;
            this.Top = Screen.PrimaryScreen.Bounds.Height / 2 - this.Bounds.Height / 2;
          //  this.Height = 390;
            FileRW frw=new FileRW();
            bool bRem=false;
            string strID="";
            string strPwd="";

      //      frw.WritePssWrd(ref bRem, ref strID, ref strPwd);
            if (bRem)
            {
                chkPssWrd.Checked = bRem;
                txtUserName.Text = strID;
                txtPassword.Text = strPwd;
            }
           /* IPAddress[] AddressList = Dns.GetHostByName(Dns.GetHostName()).AddressList;
            if (AddressList.Length < 1)
            {
                MessageBox.Show("没有检测到本机有效IP");
            }
            else
            {
                for(int i=0;i< AddressList.Length;i++)
                {
                    cbBIPAddressList.Items.Add(AddressList[i].ToString());
                }
            } */
          //  cbBIPAddressList.SelectedIndex = cbBIPAddressList.Items.Count - 1;

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
            /*int[] a = { 1, 2, 3 };
            int[] b = { 1, 2, 3 };
            AV6415 av1 = new AV6415();             
            Socket sk = av1.IPConnectSP("192.168.3.11", 8010);
            DataInfo DI = av1.GetDataSP(sk);
            uint len = DI.DataInfo_NM;
            Int16[] oriData=null;
            if (oriData == null || oriData.Length != len)
                oriData = new short[len];*/
         //   Marshal.Copy(DI.DataInfo_DATA, oriData,0,len);
          //  Marshal.Copy(DI.DataInfo_DATA, oriData, 0, (int)len);
        }

        private void btUserManage_Click(object sender, EventArgs e)
        {
            /*string strUserName;
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
                MessageBox.Show("请输入登陆账号!");*/
           
        }

        private void rBTRolesServer_CheckedChanged(object sender, EventArgs e)
        {
            /*if(rBTRolesServer.Checked)
            {
                //  groupBox3.Visible = true;
                rBtNAV6415.Visible = true;
                rBtAv6415.Visible = true;
        //        this.Height = 470;
            }
            else
            {
                //    groupBox3.Visible = false;
                rBtNAV6415.Visible = false;
                rBtAv6415.Visible = false;
            //    this.Height = 390;
            }*/
        }

        private void rBTRolesAV6415_CheckedChanged(object sender, EventArgs e)
        {
           /* if (rBTRolesAV6415.Checked)
            {
                //  groupBox3.Visible = true;
                rBtNAV6415.Visible = true;
                rBtAv6415.Visible = true;
           //     this.Height = 470;
            }
            else
            {
                //  groupBox3.Visible = false;
                rBtNAV6415.Visible = false;
                rBtAv6415.Visible = false;
            //    this.Height = 390;
            }*/
        }

        private void rBTRolesClient_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}