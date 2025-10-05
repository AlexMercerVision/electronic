using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using CommonCls;
namespace VisionApplication
{
    public partial class FrmHatch : Form
    {
        HatchParam hp;
        int nHatchType = 0;
        public FrmHatch()
        {
            InitializeComponent();
        }
        public FrmHatch(HatchParam hp)
        {
            InitializeComponent();
            this.hp = hp;
        }

        private void GetParam()
        {
            hp.bEnableContour = chkEnableContour.Checked;
            hp.nPenNo = ccbPenNum.SelectedIndex;
           
            hp.nHatchType = nHatchType;
            hp.bEnableHatch = chkEnableHatch.Checked;
            hp.bHatchAllCalc = chkAllCalc.Checked;
            hp.bHatchCross = chkCrossHatch.Checked;
            hp.bHatchEdge = chkFollowEdgeON.Checked;
             

            hp.bHatchAverageLine = chkAveDistr.Checked;

            bool bNormal = true;
            bNormal &= double.TryParse(txtLineDis.Text,out hp.dHatchLineDist);
            bNormal &= double.TryParse(txtEdgeOffset.Text, out hp.dHatchEdgeDist);

            bNormal &= double.TryParse(txtStartOffset.Text, out hp.dHatchStartOffset);
            bNormal &= double.TryParse(txtEndOffset.Text, out hp.dHatchEndOffset);

            bNormal &= double.TryParse(txtLinereduction.Text, out hp.dHatchLineReduction);
            bNormal &= double.TryParse(txtLoopDist.Text, out hp.dHatchLoopDist);

            hp.dHatchAngle = (double)nudAngle.Value;
            hp.dHatchRotateAngle = (double)nudRotateAngel.Value;

        }
    }
}
