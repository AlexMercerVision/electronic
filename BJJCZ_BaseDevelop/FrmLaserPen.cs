using System;
using System.Text;
using System.Windows.Forms;

namespace BJJCZ_BaseDevelop
{
    public struct LaserpenParam
    {
        public int nPenNo;              // 笔号
        public string pStrName;        // 名称
        public int clr;                // 颜色
        public bool bDisableMark;      // 使能加工
        public bool bUseDefParam;      // 使用默认参数
        public int nMarkLoop;          // 加工次数
        public double dMarkSpeed;      // 加工速度
        public double dPowerRatio;     // 功率 %
        public double dCurrent;        // 电流,A
        public int nFreq;              // 频率 HZ
        public double dQPulseWidth;    // 脉宽，yag us    ylpm ns
        public int nStartTC;           // 开光延时
        public int nLaserOffTC;        // 关光延时
        public int nEndTC;             // 结束延时
        public int nPolyTC;            // 拐角延时
        public double dJumpSpeed;      // 跳转速度
        public int nMinJumpDelayTCUs;  // 最小跳转延时
        public int nMaxJumpDelayTCUs;  // 最大跳转延时
        public double dJumpLengthLimit;// 跳转距离阈值
        public double dPointTimeMs;    // 打点时间
        public bool nSpiSpiContinueMode;// SPI连续模式
        public int nSpiWave;           // SPI波形编号
        public int nYagMarkMode;       // YAG优化模式
        public bool bPulsePointMode;   // 脉冲打点模式
        public int nPulseNum;          // 脉冲打点脉冲数量
        public bool bEnableACCMode;    // 启用加减速优化
        public double dEndComp;        // 加速
        public double dAccDist;        // 减速
        public double dBreakAngle;     // 中断角度
        public bool bWobbleMode;       // 抖动模式
        public double bWobbleDiameter; // 抖动直径
        public double bWobbleDist;     // 抖动线间距

        public LaserpenParam(int p)
        {
            nPenNo = 1;
            pStrName = "毛刺切割";
            clr = 1;
            bDisableMark = true;
            bUseDefParam = true;
            nMarkLoop = 1;
            dMarkSpeed = 1000;
            dPowerRatio = 75;
            dCurrent = 1;
            nFreq = 115;
            dQPulseWidth = 50;
            nStartTC = -300;
            nLaserOffTC = 100;
            nEndTC = 100;
            nPolyTC = 100;
            dJumpSpeed = 500;
            nMinJumpDelayTCUs = 50;
            nMaxJumpDelayTCUs = 200;
            dJumpLengthLimit = 10;
            dPointTimeMs = 5;
            nSpiSpiContinueMode = true;
            nSpiWave = 0;
            nYagMarkMode = 0;
            bPulsePointMode = false;
            nPulseNum = 1;
            bEnableACCMode = false;
            dEndComp = 10;
            dAccDist = 10;
            dBreakAngle = 0;
            bWobbleMode = false;
            bWobbleDiameter = 0.5;
            bWobbleDist = 0.5;
        }
    }

    public partial class FrmLaserPen : Form
    {
        public LaserpenParam pP = new LaserpenParam(1);
        /// <summary>
        /// 当前操作的板卡序号（多卡支持）
        /// </summary>
        public int CardNumber { get; set; } = 0;

        public FrmLaserPen()
        {
            InitializeComponent();
            SetParam(new LaserpenParam(1));
        }

        public FrmLaserPen(LaserpenParam pP)
        {
            InitializeComponent();
            SetParam(pP);
            this.pP = pP;
        }

        private void SetParam(LaserpenParam pP)
        {
            txtPenInd.Text = pP.nPenNo.ToString();
            txtPenName.Text = pP.pStrName;
            txtPenColor.Text = pP.clr.ToString();
            chkDisablePen.Checked = pP.bDisableMark;
            chkUseDefaultParam.Checked = pP.bUseDefParam;
            txtMarkLoop.Text = pP.nMarkLoop.ToString();
            txtMarkSpeed.Text = pP.dMarkSpeed.ToString();
            txtPowerRatio.Text = pP.dPowerRatio.ToString();
            txtCurrent.Text = pP.dCurrent.ToString();
            txtFreq.Text = pP.nFreq.ToString();
            txtQPulseWidth.Text = pP.dQPulseWidth.ToString();
            txtStartTC.Text = pP.nStartTC.ToString();
            txtLaserOffTC.Text = pP.nLaserOffTC.ToString();
            txtEndTC.Text = pP.nEndTC.ToString();
            txtPolyTC.Text = pP.nPolyTC.ToString();
            txtJumpSpeed.Text = pP.dJumpSpeed.ToString();
            txtMinJumpDelayTC.Text = pP.nMinJumpDelayTCUs.ToString();
            txtMaxJumpDelayTC.Text = pP.nMaxJumpDelayTCUs.ToString();
            txtJumpLenngthLimit.Text = pP.dJumpLengthLimit.ToString();
            txtPointTime.Text = pP.dPointTimeMs.ToString();
            chkSPIContinueMode.Checked = pP.nSpiSpiContinueMode;
            txtSpiWave.Text = pP.nSpiWave.ToString();
            txtYagMarkMode.Text = pP.nYagMarkMode.ToString();
            chkPulsePointMode.Checked = pP.bPulsePointMode;
            txtPulseNum.Text = pP.nPulseNum.ToString();
            chkEnableACCMode.Checked = pP.bEnableACCMode;
            txtEndComp.Text = pP.dEndComp.ToString();
            txtAccDist.Text = pP.dAccDist.ToString();
            txtBreakAngle.Text = pP.dBreakAngle.ToString();
            chkWobbleMode.Checked = pP.bWobbleMode;
            txtWobbleDiameter.Text = pP.bWobbleDiameter.ToString();
            txtWobbleDist.Text = pP.bWobbleDist.ToString();
        }

        private void GetParam(ref LaserpenParam pP)
        {
            bool bNormal = true;
            bNormal &= int.TryParse(txtPenInd.Text, out pP.nPenNo);
            pP.pStrName = txtPenName.Text;
            bNormal &= int.TryParse(txtPenColor.Text, out pP.clr);
            pP.bDisableMark = chkDisablePen.Checked;
            pP.bUseDefParam = chkUseDefaultParam.Checked;
            bNormal &= int.TryParse(txtMarkLoop.Text, out pP.nMarkLoop);
            bNormal &= double.TryParse(txtMarkSpeed.Text, out pP.dMarkSpeed);
            bNormal &= double.TryParse(txtPowerRatio.Text, out pP.dPowerRatio);
            bNormal &= double.TryParse(txtCurrent.Text, out pP.dCurrent);
            bNormal &= int.TryParse(txtFreq.Text, out pP.nFreq);
            bNormal &= double.TryParse(txtQPulseWidth.Text, out pP.dQPulseWidth);
            bNormal &= int.TryParse(txtStartTC.Text, out pP.nStartTC);
            bNormal &= int.TryParse(txtLaserOffTC.Text, out pP.nLaserOffTC);
            bNormal &= int.TryParse(txtEndTC.Text, out pP.nEndTC);
            bNormal &= int.TryParse(txtPolyTC.Text, out pP.nPolyTC);
            bNormal &= double.TryParse(txtJumpSpeed.Text, out pP.dJumpSpeed);
            bNormal &= int.TryParse(txtMinJumpDelayTC.Text, out pP.nMinJumpDelayTCUs);
            bNormal &= int.TryParse(txtMaxJumpDelayTC.Text, out pP.nMaxJumpDelayTCUs);
            bNormal &= double.TryParse(txtJumpLenngthLimit.Text, out pP.dJumpLengthLimit);
            bNormal &= double.TryParse(txtPointTime.Text, out pP.dPointTimeMs);
            pP.nSpiSpiContinueMode = chkSPIContinueMode.Checked;
            bNormal &= int.TryParse(txtSpiWave.Text, out pP.nSpiWave);
            bNormal &= int.TryParse(txtYagMarkMode.Text, out pP.nYagMarkMode);
            pP.bPulsePointMode = chkPulsePointMode.Checked;
            bNormal &= int.TryParse(txtPulseNum.Text, out pP.nPulseNum);
            pP.bEnableACCMode = chkEnableACCMode.Checked;
            bNormal &= double.TryParse(txtEndComp.Text, out pP.dEndComp);
            bNormal &= double.TryParse(txtAccDist.Text, out pP.dAccDist);
            bNormal &= double.TryParse(txtBreakAngle.Text, out pP.dBreakAngle);
            pP.bWobbleMode = chkWobbleMode.Checked;
            bNormal &= double.TryParse(txtWobbleDiameter.Text, out pP.bWobbleDiameter);
            bNormal &= double.TryParse(txtWobbleDist.Text, out pP.bWobbleDist);

            if (!bNormal)
            {
                MessageBox.Show("数据格式有误，请检查");
            }
        }

        private void btOk_Click(object sender, EventArgs e)
        {
            GetParam(ref pP);
            // 将参数写回板卡（使用当前卡号）
            int ret = JczLmc.lmc1_SetPenParam4(CardNumber, pP.nPenNo, pP.pStrName, pP.clr,
                pP.bDisableMark, pP.bUseDefParam, pP.nMarkLoop, pP.dMarkSpeed, pP.dPowerRatio,
                pP.dCurrent, pP.nFreq, pP.dQPulseWidth, pP.nStartTC, pP.nLaserOffTC,
                pP.nEndTC, pP.nPolyTC, pP.dJumpSpeed, pP.nMinJumpDelayTCUs, pP.nMaxJumpDelayTCUs,
                pP.dJumpLengthLimit, pP.dPointTimeMs, pP.nSpiSpiContinueMode, pP.nSpiWave,
                pP.nYagMarkMode, pP.bPulsePointMode, pP.nPulseNum, pP.bEnableACCMode,
                pP.dEndComp, pP.dAccDist, pP.dBreakAngle, pP.bWobbleMode, pP.bWobbleDiameter,
                pP.bWobbleDist);
            if (ret != 0)
            {
                MessageBox.Show($"设置笔号参数失败: {JczLmc.GetErrorText(ret)}");
            }
        }

        private void btCancle_Click(object sender, EventArgs e)
        {
            // 取消不做任何操作
        }

        private void btDefault_Click(object sender, EventArgs e)
        {
            SetParam(new LaserpenParam(1));
        }
    }
}