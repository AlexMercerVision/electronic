using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using CommonCls;

namespace xbDefectDect
{
    public partial class DateChoose : Form
    {
       public DateTime dtB, dtE;
        
        public DateChoose()
        {
            InitializeComponent();
        }

        private void btOk_Click(object sender, EventArgs e)
        {
            if (cmbStart.SelectedIndex==0)
            {
                dtB = DateTime.Now;
            }
            else
            dtB = new DateTime(dtDateB.Value.Year, dtDateB.Value.Month, dtDateB.Value.Day, dtTimeB.Value.Hour,
                dtTimeB.Value.Minute, dtTimeB.Value.Second);
            if (cmbEnd.SelectedIndex==0)
            {
                dtE = DateTime.Now;
            }
            else
             dtE = new DateTime(dtDateE.Value.Year, dtDateE.Value.Month, dtDateE.Value.Day, dtTimeE.Value.Hour,
                dtTimeE.Value.Minute, dtTimeE.Value.Second);  
          //  if (dtB>dtE)
          //  {
           //     (new FileRW()).SaveDateBuf(dtE, dtB);
          //  }
            //else
          //  {
                (new FileRW()).SaveDateBuf(dtB, dtE);
          //  }
            this.DialogResult = DialogResult.OK;
        }

        private void DateChoose_Load(object sender, EventArgs e)
        {
            //System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width
            this.Left = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / 2 - this.Bounds.Width / 2;
            this.Top = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / 2 - this.Bounds.Height / 2;
            if (new FileRW().ReadDateBuf(ref dtB, ref dtE))
            {
                cmbStart.SelectedIndex = 1;
                cmbEnd.SelectedIndex = 1;
                dtDateB.Value = dtB;
                dtTimeB.Value = dtB;
                dtDateE.Value = dtE;
                dtTimeE.Value = dtE;
            }
            else
            {
                cmbStart.SelectedIndex = 0;
                cmbEnd.SelectedIndex = 0;
            }
        }
      
        private void chooseDate()
        {
          //  DateTime dtB, dtE;
            
        }

        private void dtDate_ValueChanged(object sender, EventArgs e)
        {
         //   chooseDate();
        }
        private void dtTimeB_ValueChanged(object sender, EventArgs e)
        {
          //  chooseDate();
        }

        private void dtTimeE_ValueChanged(object sender, EventArgs e)
        {
          //  chooseDate();           
        }

        private void cmbDataFile_SelectedIndexChanged(object sender, EventArgs e)
        {
            /*
            int k = cmbDataFile.SelectedIndex;
                        lblDT.Text = ScreenForm.dataInfos[k].dt.ToString();*/
            
        }

        private void cmbStart_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(cmbStart.SelectedIndex==0)
            {
                dtDateB.Visible = false;
                dtTimeB.Visible = false;
            }
            else
            {
                dtDateB.Visible = true;
                dtTimeB.Visible = true;
            }
        }

        private void cmbEnd_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbEnd.SelectedIndex == 0)
            {
                dtDateE.Visible = false;
                dtTimeE.Visible = false;
            }
            else
            {
                dtDateE.Visible = true;
                dtTimeE.Visible = true;
            }
        }
    }
}