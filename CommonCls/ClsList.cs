using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CommonCls
{
    public partial class ClsList : Form
    {
        public int iClsType = 1;
        public ClsList()
        {
            InitializeComponent();
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            List<string> strCls= (new FileOper()).ReadRemarkImageFile("data\\predefined_classes.txt");
            if(strCls!=null && strCls.Count>0)
            {
                listCls.Items.Clear();
                for(int i=0;i<strCls.Count;i++)
                {
                    listCls.Items.Add(strCls[i]);
                }
            }
        }
        public ClsList(int iClsType)
        {
            InitializeComponent();
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            List<string> strCls = (new FileOper()).ReadRemarkImageFile("data\\predefined_classes.txt");
            if (strCls != null && strCls.Count > 0)
            {
                listCls.Items.Clear();
                for (int i = 0; i < strCls.Count; i++)
                {
                    listCls.Items.Add(strCls[i]);
                }
            }
            if(iClsType>-1 && iClsType<strCls.Count)
            {
                listCls.SelectedIndex = iClsType;
            }
        }

        private void listCls_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listCls.SelectedIndex >= 0)
            {
                txtClsName.Text = listCls.SelectedItem.ToString();
                iClsType = listCls.SelectedIndex + 1;
            }
        }

        private void btOk_Click(object sender, EventArgs e)
        {

        }

        private void listCls_DoubleClick(object sender, EventArgs e)
        {
            iClsType = listCls.SelectedIndex + 1;
            this.DialogResult = DialogResult.OK;
        }

        private void ClsList_Load(object sender, EventArgs e)
        {

        }
    }
}
