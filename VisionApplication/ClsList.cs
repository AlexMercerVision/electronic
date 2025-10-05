using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xbDefectDect
{
    public partial class ClsList : Form
    {
        int iClsType = 1;
        public ClsList()
        {
            InitializeComponent();
        }

        private void listCls_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtClsName.Text = listCls.SelectedItem.ToString();
            iClsType = listCls.SelectedIndex +1;
        }

        private void btOk_Click(object sender, EventArgs e)
        {

        }
    }
}
