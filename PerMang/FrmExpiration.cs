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
namespace VisionApplication
{
    public partial class FrmExpiration : Form
    {
        private Protect mPasswordFile;
        private DateTime dtDeadLine;
        private int iModelRole;
        public FrmExpiration()
        {
            InitializeComponent();
        }
        public FrmExpiration(Protect mFile,int iModelRole)
        {
            InitializeComponent();
            mPasswordFile = mFile;
            this.iModelRole = iModelRole;
        }
        private void FrmExpiration_Load(object sender, EventArgs e)
        {
            dtDeadLine = mPasswordFile.GetDate(DateType.DeadLine);
            dTPicker_DeadLIne.Value = dtDeadLine;
            if (iModelRole == 10)
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

        private void bt_Licence_Click(object sender, EventArgs e)
        {
            DateTime dt = dTPicker_DeadLIne.Value;
          bool bReg=  mPasswordFile.Regist(txt_RegCode.Text.Trim(),ref dt);
            if (!bReg)
                MessageBox.Show("注册失败");
            else
            {
                dTPicker_DeadLIne.Value = dt;
                MessageBox.Show("注册成功");
            }
        }
    }
}
