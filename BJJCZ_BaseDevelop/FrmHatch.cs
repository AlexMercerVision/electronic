using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Globalization;
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
        public int nHatchType;              // 填充类型 0单向 1双向 2回形 3连续弓形 4带边段弓形
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
            bHatchAllCalc = true;
            bHatchEdge = false;
            bHatchAverageLine = false;
            bHatchLoopRev = false;
            bHatchAutoRotate = false;
            bHatchCross = false;

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
        private int indHatchType = 0;

        private void LoadImage()
        {
            imagesLoopRev = new Image[]
            {
                LoadIconOrFallback("HatchFo.png", "Fo"),
                LoadIconOrFallback("HatchRev.png", "Rev"),
            };

            imagesHatchType = new Image[]
            {
                LoadIconOrFallback("HatchSi.png", "Si"),
                LoadIconOrFallback("HatchBi.png", "Bi"),
                LoadIconOrFallback("HatchLoop.png", "Loop"),
                LoadIconOrFallback("HatchArched.png", "Arch"),
                LoadIconOrFallback("HatchArchedNoCross.png", "NoRev"),
            };
        }

        private Image LoadIconOrFallback(string fileName, string label)
        {
            string[] dirs = new[]
            {
                Path.Combine(Application.StartupPath, "ICON"),
                Path.Combine(Application.StartupPath, "..", "ICON"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ICON"),
                Path.Combine(Environment.CurrentDirectory, "ICON")
            };

            foreach (string dir in dirs)
            {
                string path = Path.GetFullPath(Path.Combine(dir, fileName));
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    return Image.FromFile(path);
                }
                catch
                {
                }
            }

            return CreateFallbackIcon(label);
        }

        private Image CreateFallbackIcon(string label)
        {
            Bitmap bitmap = new Bitmap(64, 48);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                using (Pen border = new Pen(Color.Gray))
                using (Pen line = new Pen(Color.Red, 2))
                {
                    g.DrawRectangle(border, 0, 0, bitmap.Width - 1, bitmap.Height - 1);
                    g.DrawLine(line, 8, bitmap.Height / 2, bitmap.Width - 8, bitmap.Height / 2);
                }
                g.DrawString(label, SystemFonts.DefaultFont, Brushes.Black, 6, 6);
            }
            return bitmap;
        }

        private int NormalizeIndex(int index, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            int normalized = index % count;
            return normalized < 0 ? normalized + count : normalized;
        }

        private decimal ClampToRange(decimal value, NumericUpDown control)
        {
            if (value < control.Minimum)
            {
                return control.Minimum;
            }

            if (value > control.Maximum)
            {
                return control.Maximum;
            }

            return value;
        }

        private string FormatNumber(double value)
        {
            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }
        private double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }
        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        private bool TryReadDouble(TextBox textBox, string fieldName, out double value)
        {
            string text = textBox.Text.Trim();
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
                double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            MessageBox.Show(fieldName + "数字有误，请检查");
            textBox.Focus();
            textBox.SelectAll();
            return false;
        }

        private int GetSelectedParamIndex()
        {
            if (rbHatch2.Checked)
            {
                return 2;
            }

            if (rbHatch3.Checked)
            {
                return 3;
            }

            return 1;
        }

        private bool IsLoopHatchType()
        {
            return NormalizeIndex(indHatchType, imagesHatchType.Length) == 2;
        }

        private void ApplyHatchTypeRules(bool resetForSelectedType)
        {
            bool loopType = IsLoopHatchType();
            chkAllCalc.Text = loopType ? "反转" : "对象整体计算";

            if (loopType)
            {
                if (resetForSelectedType)
                {
                    chkAllCalc.Checked = false;
                    chkCrossHatch.Checked = false;
                    chkFollowEdgeON.Checked = false;
                    chkAveDistr.Checked = true;
                    chkAutoRorateAngle.Checked = false;
                    indLoopRev = 0;
                    picHatchLoopRev.Image = imagesLoopRev[indLoopRev];

                    if (txtLoopDist.Text.Trim() == "0")
                    {
                        txtLoopDist.Text = "0.5";
                    }
                }

                chkCrossHatch.Checked = false;
                chkAutoRorateAngle.Checked = false;
            }
        }

        private void UpdateControlState()
        {
            bool hatchEnabled = chkEnableHatch.Checked;
            bool loopType = IsLoopHatchType();

            groupBox1.Enabled = hatchEnabled;
            picHatchModel.Enabled = hatchEnabled;
            picHatchLoopRev.Enabled = hatchEnabled;
            chkAllCalc.Enabled = hatchEnabled;
            chkCrossHatch.Enabled = hatchEnabled && !loopType;
            chkFollowEdgeON.Enabled = hatchEnabled;
            chkAveDistr.Enabled = hatchEnabled && !loopType;
            ccbPenNum.Enabled = hatchEnabled;
            nudAngle.Enabled = hatchEnabled;
            txtLineDis.Enabled = hatchEnabled;
            txtEdgeOffset.Enabled = hatchEnabled;
            txtLinereduction.Enabled = hatchEnabled;
            txtLoopDist.Enabled = hatchEnabled;
            nudLoopNum.Enabled = hatchEnabled;
            chkAutoRorateAngle.Enabled = hatchEnabled && !loopType;

            bool manualOffsetEnabled = hatchEnabled && (loopType || !chkAveDistr.Checked);
            txtStartOffset.Enabled = manualOffsetEnabled;
            txtEndOffset.Enabled = manualOffsetEnabled;

            nudRotateAngel.Enabled = hatchEnabled && !loopType && !chkAutoRorateAngle.Checked;
        }

        public FrmHatch()
        {
            InitializeComponent();
            ccbPenNum.ResetPenColors();
            LoadImage();
            SetParam(hp);
        }

        public FrmHatch(HatchParam hp)
        {
            InitializeComponent();
            ccbPenNum.ResetPenColors();
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

                if (ccbPenNum.Items.Count > 0)
                {
                    ccbPenNum.SelectedIndex = Math.Max(0, Math.Min(hp.nPenNo, ccbPenNum.Items.Count - 1));
                }
                indHatchType = NormalizeIndex(hp.nHatchType, imagesHatchType.Length);
                picHatchModel.Image = imagesHatchType[indHatchType];
                if (IsLoopHatchType())
                {
                    chkAllCalc.Checked = hp.bHatchLoopRev;
                }
                nParamIndex = Math.Max(1, Math.Min(3, hp.nParamIndex));
                nudLoopNum.Value = ClampToRange(hp.nEdgeLoop, nudLoopNum);

                txtLineDis.Text = FormatNumber(hp.dHatchLineDist);
                txtEdgeOffset.Text = FormatNumber(hp.dHatchEdgeDist);
                txtStartOffset.Text = FormatNumber(hp.dHatchStartOffset);
                txtEndOffset.Text = FormatNumber(hp.dHatchEndOffset);
                txtLinereduction.Text = FormatNumber(hp.dHatchLineReduction);
                txtLoopDist.Text = FormatNumber(hp.dHatchLoopDist);

                nudAngle.Value = ClampToRange((decimal)RadiansToDegrees(hp.dHatchAngle), nudAngle);
                nudRotateAngel.Value = ClampToRange((decimal)RadiansToDegrees(hp.dHatchRotateAngle), nudRotateAngel);

                // 设置填充参数序号单选按钮
                switch (nParamIndex)
                {
                    case 1: rbHatch1.Checked = true; break;
                    case 2: rbHatch2.Checked = true; break;
                    case 3: rbHatch3.Checked = true; break;
                }

                ApplyHatchTypeRules(false);
                UpdateControlState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private bool GetParam()
        {
            hp.bEnableContour = chkEnableContour.Checked;
            hp.bEnableHatch = chkEnableHatch.Checked ? 1 : 0;

            bool loopType = IsLoopHatchType();
            hp.bHatchAllCalc = loopType ? false : chkAllCalc.Checked;
            hp.bHatchCross = chkCrossHatch.Checked;
            hp.bHatchEdge = chkFollowEdgeON.Checked;

            indLoopRev = NormalizeIndex(indLoopRev, imagesLoopRev.Length);
            hp.bHatchLoopRev = loopType ? chkAllCalc.Checked : indLoopRev == 1;
            hp.bHatchAverageLine = chkAveDistr.Checked;
            hp.bHatchAutoRotate = chkAutoRorateAngle.Checked;

            hp.nPenNo = ccbPenNum.SelectedIndex;
            if (hp.nPenNo < 0)
            {
                hp.nPenNo = 0;
            }

            indHatchType = NormalizeIndex(indHatchType, imagesHatchType.Length);
            hp.nHatchType = indHatchType;
            hp.nParamIndex = GetSelectedParamIndex();
            hp.nEdgeLoop = (int)nudLoopNum.Value;
            hp.nCycCount = 1;  // 默认值，可调整

            if (hp.nHatchType == 2)
            {
                hp.bHatchCross = false;
                hp.bHatchAutoRotate = false;
                hp.bHatchAverageLine = true;
            }

            if (!TryReadDouble(txtLineDis, "填充线间距", out hp.dHatchLineDist) ||
                !TryReadDouble(txtEdgeOffset, "填充线边距", out hp.dHatchEdgeDist) ||
                !TryReadDouble(txtStartOffset, "起点偏移", out hp.dHatchStartOffset) ||
                !TryReadDouble(txtEndOffset, "终点偏移", out hp.dHatchEndOffset) ||
                !TryReadDouble(txtLinereduction, "线缩进", out hp.dHatchLineReduction) ||
                !TryReadDouble(txtLoopDist, "环间距", out hp.dHatchLoopDist))
            {
                return false;
            }

            hp.dHatchAngle = DegreesToRadians((double)nudAngle.Value);
            hp.dHatchRotateAngle = DegreesToRadians((double)nudRotateAngel.Value);
            return true;
        }

        private void picHatchModel_Click(object sender, EventArgs e)
        {
            indHatchType = NormalizeIndex(indHatchType + 1, imagesHatchType.Length);
            picHatchModel.Image = imagesHatchType[indHatchType];
            ApplyHatchTypeRules(true);
            UpdateControlState();
        }

        private void picHatchLoopRev_Click(object sender, EventArgs e)
        {
            indLoopRev = NormalizeIndex(indLoopRev + 1, imagesLoopRev.Length);
            picHatchLoopRev.Image = imagesLoopRev[indLoopRev];
        }

        private void chkEnableHatch_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControlState();
        }

        private void chkAveDistr_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControlState();
        }

        private void btOk_Click(object sender, EventArgs e)
        {
            if (!GetParam())
            {
                DialogResult = DialogResult.None;
                return;
            }

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
            UpdateControlState();
        }
    }
}
