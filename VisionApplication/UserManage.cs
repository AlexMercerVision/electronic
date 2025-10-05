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
    public partial class UserManage : Form
    {

        ArrayList alUserInfo;
        string strID;
        int m_iSelectIndex;
        SqlOper m_sqlOper;
        public int iRole = 0;
        public string strAccount;
        public UserManage()
        {
            InitializeComponent();
        }

       
        private void ShowUserInfo()
        {
            dataGridView1.Rows.Clear();
            alUserInfo = new ArrayList();
            m_sqlOper.ReadWholeUserInfo(alUserInfo);
            int i;
            struUserInfo stru_UserInfo;
            for (i = 0; i < alUserInfo.Count - 1; i++)
            {
                dataGridView1.Rows.Add();
                stru_UserInfo = (struUserInfo)alUserInfo[i];
                dataGridView1.Rows[i].Cells[0].Value = stru_UserInfo.strUserName;
                dataGridView1.Rows[i].Cells[1].Value = stru_UserInfo.iRole;
                dataGridView1.Rows[i].Cells[3].Value = stru_UserInfo.strTel;
                dataGridView1.Rows[i].Cells[2].Value = stru_UserInfo.strEamil;
                dataGridView1.Rows[i].Cells[4].Value = stru_UserInfo.strID;
            }
            stru_UserInfo = (struUserInfo)alUserInfo[i];
            dataGridView1.Rows[i].Cells[0].Value = stru_UserInfo.strUserName;
            dataGridView1.Rows[i].Cells[1].Value = stru_UserInfo.iRole;
            dataGridView1.Rows[i].Cells[3].Value = stru_UserInfo.strTel;
            dataGridView1.Rows[i].Cells[2].Value = stru_UserInfo.strEamil;
            dataGridView1.Rows[i].Cells[4].Value = stru_UserInfo.strID;
        }
        private void UserManage_Load(object sender, EventArgs e)
        {
            this.Left = Screen.PrimaryScreen.Bounds.Width / 2 - this.Bounds.Width / 2;
            this.Top = Screen.PrimaryScreen.Bounds.Height / 2 - this.Bounds.Height / 2;
            m_sqlOper = new SqlOper();
            ShowUserInfo();
        }

        private void btDelete_Click(object sender, EventArgs e)
        {
            //先判断当前用户的权限
            if (iRole!=1)
            {
                MessageBox.Show("对不起，您不是管理员，不能删除用户");
                return;
            }
            //---------------------------------------------------------
            string strUserName = dataGridView1.SelectedRows[0].Cells[0].Value.ToString();
            if (strUserName == "admin")
            {
                MessageBox.Show("对不起，不能删除管理员");
                return;
            }
            //---------------------------------------------------------
            if (MessageBox.Show("确定要删除吗", "提醒",MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                m_sqlOper.DeleteUserInfo(strID);
                ShowUserInfo();                 
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            //dataGridView1.
            if (dataGridView1.SelectedRows.Count>0)
            {
                strID = dataGridView1.SelectedRows[0].Cells[4].Value.ToString();
            }
        }

        private void btAdd_Click(object sender, EventArgs e)
        {
            if (iRole != 1)
            {
                MessageBox.Show("对不起，您不是管理员，不能添加用户");
                return;
            }
            UserEdit ue = new UserEdit();
            if (ue.ShowDialog()==DialogResult.OK)
            {
                ShowUserInfo();
            }
             
        }

        private void btEdit_Click(object sender, EventArgs e)
        {
            if (iRole!=1 && strAccount!=strID)
            {
                MessageBox.Show("对不起，您只能编辑自己的用户信息!");
                return;
            }
            struUserInfo ui=new struUserInfo();
            for (int i = 0; i < alUserInfo.Count;i++ )
            {
                if (((struUserInfo)alUserInfo[i]).strID==strID)
                {
                    ui = (struUserInfo)alUserInfo[i];
                                    }
            }
            UserEdit ue = new UserEdit(ui);
            ue.iRole = iRole;
            if (ue.ShowDialog() == DialogResult.OK)
            {
                ShowUserInfo();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}