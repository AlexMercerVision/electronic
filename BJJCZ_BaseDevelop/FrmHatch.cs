using System;
using System.Windows.Forms;
using System.Drawing;
namespace BJJCZ_BaseDevelop
{
    /// <summary>
    /// 填充参数结构（与多头库兼容）
    /// </summary>
    public struct HatchParam
    {
        public bool bEnableContour;         // 使能轮廓标刻
        public int bEnableHatch;            // 使能填充 (0/1)
        public bool bContourFirst;          // 轮廓优先标刻
        public bool bHatchAllCalc;          // 是否全部对象作为整体一起计算
        public bool bHatchEdge;             // 绕边一次
        public bool bHatchAverageLine;      // 自动平均分布线
        public bool bHatchLoopRev;          // 环形反转
        public bool bHatchAutoRotate;       // 是否自动旋转角度
        public bool bHatchCross;            // 交叉填充

        public int nPenNo;                  // 笔号
        public int nHatchType;              // 填充类型 0单向 1双向 2回形 3弓形 4弓形不反向
        public int nEdgeLoop;               // 边缘环线数目
        public int nParamIndex;             // 填充参数序号 1,2,3
        public int nCycCount;               // 填充数目（20200813新增）
         
        public double dHatchAngle;          // 填充线角度（弧度值）
        public double dHatchRotateAngle;    // 自动旋转角度（弧度值）
        public double dHatchLineDist;       // 填充线间距 (mm)
        public double dHatchEdgeDist;       // 填充线边距 (mm)
        public double dHatchStartOffset;    // 填充线起始偏移距离 (mm)
        public double dHatchEndOffset;      // 填充线结束偏移距离 (mm)
        public double dHatchLineReduction;  // 直线缩进 (mm)
        public double dHatchLoopDist;       // 环间距 (mm)

        public HatchParam(int init)
        {
            bEnableContour = true;
            bEnableHatch = 1;
            bContourFirst = false;
            bHatchAllCalc = false;
            bHatchEdge = true;
            bHatchAverageLine = true;
            bHatchLoopRev = false;
            bHatchAutoRotate = true;
            bHatchCross = true;

            nPenNo = 2;
            nHatchType = 0;
            nEdgeLoop = 0;
            nParamIndex = 1;
            nCycCount = 1;

            dHatchAngle = 0;
            dHatchRotateAngle = 0;
            dHatchLineDist = 0.05;
            dHatchEdgeDist = 0;
            dHatchStartOffset = 0;
            dHatchEndOffset = 0;
            dHatchLineReduction = 0;
            dHatchLoopDist = 0;
        }
    }

    public partial class FrmHatch : Form
    {
        public HatchParam hp = new HatchParam(1);
        int nParamIndex = 1;
        /// <summary>
        /// 当前操作的板卡序号（多卡支持）
        /// </summary>
        public int CardNumber { get; set; } = 0;

        // 用于界面显示的图像资源
        private Image[] imagesLoopRev;
        private Image[] imagesHatchType;
        private int indLoopRev = 0;
        private int indHatchType = 3;

        private void LoadImage()
        {
            try
            {
                imagesLoopRev = new Image[]
                {
                    Image.FromFile("ICON\\HatchFo.png"),
                    Image.FromFile("ICON\\HatchRev.png"),
                };
                imagesHatchType = new Image[]
                {
                    Image.FromFile("ICON\\HatchSi.png"),
                    Image.FromFile("ICON\\HatchBi.png"),
                    Image.FromFile("ICON\\HatchLoop.png"),
                    Image.FromFile("ICON\\HatchArched.png"),
                    Image.FromFile("ICON\\HatchArchedNoCross.png"),
                };
            }
            catch
            {
                // 图标文件不存在时忽略，不显示图片
            }
        }

        public FrmHatch()
        {
            InitializeComponent();
            LoadImage();
            SetParam(hp);
        }

        public FrmHatch(HatchParam hp)
        {
            InitializeComponent();
            this.hp = hp;
            LoadImage();
            SetParam(hp);
        }

        private void SetParam(HatchParam hp)
        {
            try
            {
                chkEnableContour.Checked = hp.bEnableContour;
                chkEnableHatch.Checked = hp.bEnableHatch == 1;

                chkAllCalc.Checked = hp.bHatchAllCalc;
                chkCrossHatch.Checked = hp.bHatchCross;
                chkFollowEdgeON.Checked = hp.bHatchEdge;

                indLoopRev = hp.bHatchLoopRev ? 1 : 0;
                picHatchLoopRev.Image = imagesLoopRev[indLoopRev % 2];

                chkAveDistr.Checked = hp.bHatchAverageLine;
                chkAutoRorateAngle.Checked = hp.bHatchAutoRotate;

                ccbPenNum.SelectedIndex = hp.nPenNo;
                indHatchType = hp.nHatchType;
                picHatchModel.Image = imagesHatchType[indHatchType % imagesHatchType.Length];
                nParamIndex = hp.nParamIndex;
                nudLoopNum.Value = hp.nEdgeLoop;

                txtLineDis.Text = hp.dHatchLineDist.ToString();
                txtEdgeOffset.Text = hp.dHatchEdgeDist.ToString();
                txtStartOffset.Text = hp.dHatchStartOffset.ToString();
                txtEndOffset.Text = hp.dHatchEndOffset.ToString();
                txtLinereduction.Text = hp.dHatchLineReduction.ToString();
                txtLoopDist.Text = hp.dHatchLoopDist.ToString();

                nudAngle.Value = (decimal)hp.dHatchAngle;
                nudRotateAngel.Value = (decimal)hp.dHatchRotateAngle;

                // 设置填充参数序号单选按钮
                switch (hp.nParamIndex)
                {
                    case 1: rbHatch1.Checked = true; break;
                    case 2: rbHatch2.Checked = true; break;
                    case 3: rbHatch3.Checked = true; break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void GetParam()
        {
            hp.bEnableContour = chkEnableContour.Checked;
            hp.bEnableHatch = chkEnableHatch.Checked ? 1 : 0;

            hp.bHatchAllCalc = chkAllCalc.Checked;
            hp.bHatchCross = chkCrossHatch.Checked;
            hp.bHatchEdge = chkFollowEdgeON.Checked;

            hp.bHatchLoopRev = indLoopRev == 1;
            hp.bHatchAverageLine = chkAveDistr.Checked;
            hp.bHatchAutoRotate = chkAutoRorateAngle.Checked;

            hp.nPenNo = ccbPenNum.SelectedIndex;
            hp.nHatchType = indHatchType;
            if (rbHatch1.Checked) hp.nParamIndex = 1;
            else if (rbHatch2.Checked) hp.nParamIndex = 2;
            else if (rbHatch3.Checked) hp.nParamIndex = 3;
            hp.nEdgeLoop = (int)nudLoopNum.Value;
            hp.nCycCount = 1;  // 默认值，可调整

            bool bNormal = true;
            bNormal &= double.TryParse(txtLineDis.Text, out hp.dHatchLineDist);
            bNormal &= double.TryParse(txtEdgeOffset.Text, out hp.dHatchEdgeDist);
            bNormal &= double.TryParse(txtStartOffset.Text, out hp.dHatchStartOffset);
            bNormal &= double.TryParse(txtEndOffset.Text, out hp.dHatchEndOffset);
            bNormal &= double.TryParse(txtLinereduction.Text, out hp.dHatchLineReduction);
            bNormal &= double.TryParse(txtLoopDist.Text, out hp.dHatchLoopDist);

            hp.dHatchAngle = (double)nudAngle.Value;
            hp.dHatchRotateAngle = (double)nudRotateAngel.Value;

            if (!bNormal)
            {
                MessageBox.Show("数字有误，请检查");
            }
        }

        private void picHatchModel_Click(object sender, EventArgs e)
        {
            indHatchType++;
            picHatchModel.Image = imagesHatchType[indHatchType % imagesHatchType.Length];
        }

        private void picHatchLoopRev_Click(object sender, EventArgs e)
        {
            indLoopRev++;
            picHatchLoopRev.Image = imagesLoopRev[indLoopRev % 2];
        }

        private void chkEnableHatch_CheckedChanged(object sender, EventArgs e)
        {
            bool enable = chkEnableHatch.Checked;
            picHatchModel.Enabled = enable;
            chkAllCalc.Enabled = enable;
            chkCrossHatch.Enabled = enable;
            chkFollowEdgeON.Enabled = enable;
        }

        private void chkAveDistr_CheckedChanged(object sender, EventArgs e)
        {
            bool enable = !chkAveDistr.Checked;
            txtStartOffset.Enabled = enable;
            txtEndOffset.Enabled = enable;
        }

        private void btOk_Click(object sender, EventArgs e)
        {
            GetParam();
            // 将填充参数应用到当前对象（使用当前卡号）
            // 注意：需要外部传入当前操作的对象名称，此处未提供，故仅保存参数到结构体，
            // 实际设置填充参数应在外部调用 SetHatchParam 并调用 HatchEnt
            DialogResult = DialogResult.OK;
        }

        private void btDefault_Click(object sender, EventArgs e)
        {
            hp = new HatchParam(1);
            SetParam(hp);
        }

        private void btDelHatch_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Abort;  // 表示删除填充
        }

        private void chkAutoRorateAngle_CheckedChanged(object sender, EventArgs e)
        {
            nudRotateAngel.Enabled = !chkAutoRorateAngle.Checked;
        }
    }
}