using CommonCls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace VisionApplication
{
    public partial class FrmExpiration : Form
    {
        private Protect mPasswordFile;
        private DateTime dtDeadLine;
        private int iModelRole;
        
        public FrmExpiration(Protect mFile,int iModelRole)
        {
            InitializeComponent();
            mPasswordFile = mFile;
            this.iModelRole = iModelRole;
        }
        public FrmExpiration( )
        {
          
            InitializeComponent();
            mPasswordFile = new Protect("authentication.txt");
            RoleCheck();
        }
        //日期验证
        private void DateVerify(int iModelRole)
        {
            int iDateVer = mPasswordFile.DateVerify(iModelRole);

            if (iDateVer == 1)
            {
                 
                iModelRole = -1;
            }
            else if (iDateVer == 2)
            {
                iModelRole = -2;
            }
            else
            {
               iModelRole = 0; 
            }
        }
        private void RoleCheck()
        {
            string strMAC = mPasswordFile.GetMacAddress();
            string strEnableMAC = mPasswordFile.GetCID();

         //   int iModelRole = -1;
            if (strMAC == strEnableMAC || strMAC == "207BD2D2CA3E" || strMAC == "00E04C782532" || strMAC == "00FFCF3A6322" || strMAC == "8C32233F2B85")  //E4:5E:37:62:A1:3D   00FFCF3A6322  
            { 
                DateVerify(iModelRole);
                if (strMAC == "00E04C782532" || strMAC == "207BD2D2CA3E" || strMAC == "00FFCF3A6322" || strMAC == "8C32233F2B85")   //调试专用功能
                {
                    iModelRole = 9;

                }
                else if (IsBetween(DateTime.Now.ToString("yyyyMMdd")))  //临时使用
                {
                    iModelRole = 8;
                }
                else
                {
                    iModelRole = 0;

                }
               
            }
         
        }

        private  bool IsBetween(string dateStr)
        {
            // 按 "yyyyMMdd" 格式解析（忽略本地文化）
            DateTime date = DateTime.ParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture);
            DateTime start = new DateTime(2026, 7, 20);
            DateTime end = new DateTime(2026, 7, 31);
            return date >= start && date <= end;  // 包含两端
        }
        private void FrmExpiration_Load(object sender, EventArgs e)
        {
            dtDeadLine = mPasswordFile.GetDate(DateType.DeadLine);
            dTPicker_DeadLIne.Value = dtDeadLine;
            if (iModelRole >= 8)
            {
                dTPicker_DeadLIne.Enabled = true;
                chkForever.Enabled = true;
                btWriteCID.Enabled = true;
                bt_RegCode.Enabled = true;
            }
            else
            {
                dTPicker_DeadLIne.Enabled = false;
                chkForever.Enabled = false;
                btWriteCID.Enabled = false;
                bt_RegCode.Enabled = false;
            }
        }

        private void chkForever_CheckedChanged(object sender, EventArgs e)
        {
            if(chkForever.Checked)
            {
                dTPicker_DeadLIne.Value = DateTime.Now.AddYears(50);
            }
            else
            {
                dTPicker_DeadLIne.Value = dtDeadLine;
            }
        }

        private void bt_OK_Click(object sender, EventArgs e)
        {
            if (!bReg)
            {
                MessageBox.Show("注册失败");
                return;
            }
            if (iModelRole>0)
            {
                mPasswordFile.SetDate(DateType.DeadLine, dTPicker_DeadLIne.Value);
                mPasswordFile.WritePasswordFile();
            }
        }


        //获取机器码，用户可执行此操作，获取机器码
        private void bt_GetCID_Click(object sender, EventArgs e)
        {
            txtCID.Text=mPasswordFile.GetCIDEncode();

        }

        //由机器码生成注册码
        private void bt_RegCode_Click(object sender, EventArgs e)
        {

            if(txtCID.Text.Trim()!=null)
            {
               string cid= mPasswordFile.GetCIDDecode(txtCID.Text.Trim());
                if(cid!="")
                    txt_RegCode.Text= mPasswordFile.GetCIDandDTEncode(cid, dTPicker_DeadLIne.Value);
                else
                {
                    MessageBox.Show("机器码有误，请重新提供机器码");
                }
            }
        }
        private void btWriteCID_Click(object sender, EventArgs e)
        {
            mPasswordFile.WriteCID();
        }
        bool bReg = false;
        private void bt_Licence_Click(object sender, EventArgs e)
        {
            DateTime dt = dTPicker_DeadLIne.Value;
            bReg=  mPasswordFile.Regist(txt_RegCode.Text.Trim(),ref dt);
            if (!bReg)
                MessageBox.Show("注册失败");
            else
            {
                dTPicker_DeadLIne.Value = dt;
                MessageBox.Show("注册成功");
            }
        }

        private void bt_Cancle_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
