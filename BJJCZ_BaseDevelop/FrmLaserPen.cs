using System;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;

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
            pStrName = "\u6BDB\u523A\u5207\u5272";
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
        private bool _updatingControls;
        private bool _handlersRegistered;
        private string _ezdFilePath;
        /// <summary>
        /// 当前操作的板卡序号（多卡支持）
        /// </summary>
        public int CardNumber { get; private set; } = 0;
        public int SelectedPenNo { get { return pP.nPenNo; } }

        public FrmLaserPen()
        {
            InitializeComponent();
            btOk.DialogResult = DialogResult.None;
            RegisterParamChangeHandlers();
            pP = LoadPenParam(CardNumber, pP.nPenNo, pP, null);
            SetParam(pP);
        }

        public FrmLaserPen(LaserpenParam pP)
            : this(pP, 0, null)
        {
        }

        public FrmLaserPen(LaserpenParam pP, int cardNumber)
            : this(pP, cardNumber, null)
        {
        }

        public FrmLaserPen(LaserpenParam pP, int cardNumber, string ezdFilePath)
        {
            InitializeComponent();
            btOk.DialogResult = DialogResult.None;
            RegisterParamChangeHandlers();
            CardNumber = cardNumber;
            _ezdFilePath = ezdFilePath;
            this.pP = LoadPenParam(cardNumber, pP.nPenNo, pP, ezdFilePath);
            SetParam(this.pP);
        }

        private void SetParam(LaserpenParam pP)
        {
            _updatingControls = true;
            try
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
            finally
            {
                _updatingControls = false;
            }
        }

        private void RegisterParamChangeHandlers()
        {
            if (_handlersRegistered)
            {
                return;
            }

            _handlersRegistered = true;
            txtPenInd.TextChanged += ParamControlChanged;
            txtPenName.TextChanged += ParamControlChanged;
            txtPenColor.TextChanged += ParamControlChanged;
            txtMarkLoop.TextChanged += ParamControlChanged;
            txtMarkSpeed.TextChanged += ParamControlChanged;
            txtPowerRatio.TextChanged += ParamControlChanged;
            txtCurrent.TextChanged += ParamControlChanged;
            txtFreq.TextChanged += ParamControlChanged;
            txtQPulseWidth.TextChanged += ParamControlChanged;
            txtStartTC.TextChanged += ParamControlChanged;
            txtLaserOffTC.TextChanged += ParamControlChanged;
            txtEndTC.TextChanged += ParamControlChanged;
            txtPolyTC.TextChanged += ParamControlChanged;
            txtJumpSpeed.TextChanged += ParamControlChanged;
            txtMinJumpDelayTC.TextChanged += ParamControlChanged;
            txtMaxJumpDelayTC.TextChanged += ParamControlChanged;
            txtJumpLenngthLimit.TextChanged += ParamControlChanged;
            txtPointTime.TextChanged += ParamControlChanged;
            txtSpiWave.TextChanged += ParamControlChanged;
            txtYagMarkMode.TextChanged += ParamControlChanged;
            txtPulseNum.TextChanged += ParamControlChanged;
            txtEndComp.TextChanged += ParamControlChanged;
            txtAccDist.TextChanged += ParamControlChanged;
            txtBreakAngle.TextChanged += ParamControlChanged;
            txtWobbleDiameter.TextChanged += ParamControlChanged;
            txtWobbleDist.TextChanged += ParamControlChanged;
            chkDisablePen.CheckedChanged += ParamControlChanged;
            chkSPIContinueMode.CheckedChanged += ParamControlChanged;
            chkPulsePointMode.CheckedChanged += ParamControlChanged;
            chkEnableACCMode.CheckedChanged += ParamControlChanged;
            chkWobbleMode.CheckedChanged += ParamControlChanged;
            chkUseDefaultParam.CheckedChanged += chkUseDefaultParam_CheckedChanged;
        }

        private void ParamControlChanged(object sender, EventArgs e)
        {
            if (_updatingControls)
            {
                return;
            }

            chkUseDefaultParam.Checked = false;
        }

        private void chkUseDefaultParam_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingControls || !chkUseDefaultParam.Checked)
            {
                return;
            }

            pP = CreateDefaultParam(pP.nPenNo);
            SetParam(pP);
        }

        private bool GetParam(ref LaserpenParam pP)
        {
            bool bNormal = true;
            bNormal &= TryReadInt(txtPenInd, out pP.nPenNo);
            pP.pStrName = txtPenName.Text;
            bNormal &= TryReadInt(txtPenColor, out pP.clr);
            pP.bDisableMark = chkDisablePen.Checked;
            pP.bUseDefParam = chkUseDefaultParam.Checked;
            bNormal &= TryReadInt(txtMarkLoop, out pP.nMarkLoop);
            bNormal &= TryReadDouble(txtMarkSpeed, out pP.dMarkSpeed);
            bNormal &= TryReadDouble(txtPowerRatio, out pP.dPowerRatio);
            bNormal &= TryReadDouble(txtCurrent, out pP.dCurrent);
            bNormal &= TryReadInt(txtFreq, out pP.nFreq);
            bNormal &= TryReadDouble(txtQPulseWidth, out pP.dQPulseWidth);
            bNormal &= TryReadInt(txtStartTC, out pP.nStartTC);
            bNormal &= TryReadInt(txtLaserOffTC, out pP.nLaserOffTC);
            bNormal &= TryReadInt(txtEndTC, out pP.nEndTC);
            bNormal &= TryReadInt(txtPolyTC, out pP.nPolyTC);
            bNormal &= TryReadDouble(txtJumpSpeed, out pP.dJumpSpeed);
            bNormal &= TryReadInt(txtMinJumpDelayTC, out pP.nMinJumpDelayTCUs);
            bNormal &= TryReadInt(txtMaxJumpDelayTC, out pP.nMaxJumpDelayTCUs);
            bNormal &= TryReadDouble(txtJumpLenngthLimit, out pP.dJumpLengthLimit);
            bNormal &= TryReadDouble(txtPointTime, out pP.dPointTimeMs);
            pP.nSpiSpiContinueMode = chkSPIContinueMode.Checked;
            bNormal &= TryReadInt(txtSpiWave, out pP.nSpiWave);
            bNormal &= TryReadInt(txtYagMarkMode, out pP.nYagMarkMode);
            pP.bPulsePointMode = chkPulsePointMode.Checked;
            bNormal &= TryReadInt(txtPulseNum, out pP.nPulseNum);
            pP.bEnableACCMode = chkEnableACCMode.Checked;
            bNormal &= TryReadDouble(txtEndComp, out pP.dEndComp);
            bNormal &= TryReadDouble(txtAccDist, out pP.dAccDist);
            bNormal &= TryReadDouble(txtBreakAngle, out pP.dBreakAngle);
            pP.bWobbleMode = chkWobbleMode.Checked;
            bNormal &= TryReadDouble(txtWobbleDiameter, out pP.bWobbleDiameter);
            bNormal &= TryReadDouble(txtWobbleDist, out pP.bWobbleDist);

            if (!bNormal)
            {
                MessageBox.Show("数据格式有误，请检查");
            }
            return bNormal;
        }

        private bool TryReadInt(TextBox textBox, out int value)
        {
            string text = textBox.Text.Trim();
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value) ||
                   int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private bool TryReadDouble(TextBox textBox, out double value)
        {
            string text = textBox.Text.Trim();
            return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
                   double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private void btOk_Click(object sender, EventArgs e)
        {
            if (!GetParam(ref pP))
            {
                DialogResult = DialogResult.None;
                return;
            }

            if (!ApplyAndSaveParam(pP))
            {
                DialogResult = DialogResult.None;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btCancle_Click(object sender, EventArgs e)
        {
            // 取消不做任何操作
        }

        private void btDefault_Click(object sender, EventArgs e)
        {
            int penNo;
            if (!int.TryParse(txtPenInd.Text, out penNo))
            {
                penNo = pP.nPenNo;
            }

            pP = CreateDefaultParam(penNo);
            SetParam(pP);
        }

        private bool ApplyAndSaveParam(LaserpenParam param)
        {
            bool hasEzdFile = !string.IsNullOrEmpty(_ezdFilePath);
            if (hasEzdFile)
            {
                LaserpenParam boardParam;
                if (TryLoadBoardPenParam(CardNumber, param.nPenNo, out boardParam))
                {
                    param.clr = boardParam.clr;
                }
            }

            int ret = JczLmc.lmc1_SetPenParam4(CardNumber, param.nPenNo, param.pStrName, param.clr,
                param.bDisableMark, param.bUseDefParam, param.nMarkLoop, param.dMarkSpeed, param.dPowerRatio,
                param.dCurrent, param.nFreq, param.dQPulseWidth, param.nStartTC, param.nLaserOffTC,
                param.nEndTC, param.nPolyTC, param.dJumpSpeed, param.nMinJumpDelayTCUs, param.nMaxJumpDelayTCUs,
                param.dJumpLengthLimit, param.dPointTimeMs, param.nSpiSpiContinueMode, param.nSpiWave,
                param.nYagMarkMode, param.bPulsePointMode, param.nPulseNum, param.bEnableACCMode,
                param.dEndComp, param.dAccDist, param.dBreakAngle, param.bWobbleMode, param.bWobbleDiameter,
                param.bWobbleDist);

            if (!hasEzdFile)
            {
                try
                {
                    SaveDefaultPenParam(CardNumber, param);
                    if (ret != 0)
                    {
                        MessageBox.Show($"默认笔号参数已保存，但写入板卡失败: {JczLmc.GetErrorText(ret)}");
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存默认笔号参数失败: {ex.Message}");
                    return false;
                }
            }

            if (ret != 0)
            {
                MessageBox.Show($"设置笔号参数失败: {JczLmc.GetErrorText(ret)}");
            }

            if (ret == 0 && !string.IsNullOrEmpty(_ezdFilePath))
            {
                ret = JczLmc.lmc1_SaveEntLibToFile(CardNumber, _ezdFilePath);
                if (ret == 0)
                {
                    SavePenParam(CardNumber, _ezdFilePath, param);
                }
                if (ret != 0)
                {
                    MessageBox.Show($"保存EZD文件失败: {JczLmc.GetErrorText(ret)}");
                }
            }

            return ret == 0;
        }

        private static LaserpenParam CreateDefaultParam(int penNo)
        {
            LaserpenParam param = new LaserpenParam(1);
            param.nPenNo = penNo;
            param.bUseDefParam = true;
            return param;
        }

        private static LaserpenParam LoadPenParam(int cardNumber, int penNo, LaserpenParam defaultParam, string ezdFilePath)
        {
            LaserpenParam cached;
            LaserpenParam savedParam = new LaserpenParam(1);
            bool hasSavedParam = !string.IsNullOrEmpty(ezdFilePath)
                && TryLoadPenParam(cardNumber, ezdFilePath, penNo, out savedParam);
            bool hasDefaultParam = string.IsNullOrEmpty(ezdFilePath)
                && TryLoadDefaultPenParam(cardNumber, penNo, out savedParam);

            if (hasDefaultParam)
            {
                return savedParam;
            }

            if (hasSavedParam)
            {
                LaserpenParam boardParam;
                if (TryLoadBoardPenParam(cardNumber, penNo, out boardParam))
                {
                    savedParam.clr = boardParam.clr;
                }

                return savedParam;
            }

            if (TryLoadBoardPenParam(cardNumber, penNo, out cached))
            {
                if (hasDefaultParam && (IsSdkBuiltInDefault(cached) || cached.bUseDefParam))
                {
                    return savedParam;
                }

                if (IsSdkBuiltInDefault(cached))
                {
                    return CreateDefaultParam(penNo);
                }

                return cached;
            }

            if (hasDefaultParam)
            {
                return savedParam;
            }

            return CreateDefaultParam(penNo);
        }

        private static bool IsSdkBuiltInDefault(LaserpenParam param)
        {
            return string.Equals(param.pStrName, "Default", StringComparison.OrdinalIgnoreCase)
                && param.clr != 1
                && param.nYagMarkMode == 256;
        }

        private static bool TryLoadBoardPenParam(int cardNumber, int penNo, out LaserpenParam param)
        {
            param = new LaserpenParam(1);
            param.nPenNo = penNo;

            StringBuilder name = new StringBuilder(256);
            int ret = JczLmc.lmc1_GetPenParam4(cardNumber, penNo, name, ref param.clr,
                ref param.bDisableMark, ref param.bUseDefParam, ref param.nMarkLoop, ref param.dMarkSpeed,
                ref param.dPowerRatio, ref param.dCurrent, ref param.nFreq, ref param.dQPulseWidth,
                ref param.nStartTC, ref param.nLaserOffTC, ref param.nEndTC, ref param.nPolyTC,
                ref param.dJumpSpeed, ref param.nMinJumpDelayTCUs, ref param.nMaxJumpDelayTCUs,
                ref param.dJumpLengthLimit, ref param.dPointTimeMs, ref param.nSpiSpiContinueMode,
                ref param.nSpiWave, ref param.nYagMarkMode, ref param.bPulsePointMode, ref param.nPulseNum,
                ref param.bEnableACCMode, ref param.dEndComp, ref param.dAccDist, ref param.dBreakAngle,
                ref param.bWobbleMode, ref param.bWobbleDiameter, ref param.bWobbleDist);
            if (ret != 0)
            {
                return false;
            }

            param.pStrName = name.ToString();
            return true;
        }

        private static bool TryLoadPenParam(int cardNumber, string ezdFilePath, int penNo, out LaserpenParam param)
        {
            param = new LaserpenParam(1);
            string[] paths = new[]
            {
                GetPenParamPath(cardNumber, ezdFilePath, penNo),
                GetLegacyPenParamPath(cardNumber, ezdFilePath, penNo),
                GetCardPenParamPath(cardNumber, ezdFilePath)
            };

            foreach (string path in paths)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    using (FileStream stream = File.OpenRead(path))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(LaserpenParam));
                        param = (LaserpenParam)serializer.Deserialize(stream);
                    }

                    if (param.nPenNo == penNo || path == GetCardPenParamPath(cardNumber, ezdFilePath))
                    {
                        param.nPenNo = penNo;
                        return true;
                    }
                }
                catch
                {
                }
            }

            return false;
        }

        private static bool TryLoadDefaultPenParam(int cardNumber, int penNo, out LaserpenParam param)
        {
            param = new LaserpenParam(1);
            string path = GetDefaultPenParamPath(cardNumber, penNo);
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(LaserpenParam));
                    param = (LaserpenParam)serializer.Deserialize(stream);
                }

                param.nPenNo = penNo;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryLoadLegacyPenParam(int cardNumber, string ezdFilePath, int penNo, out LaserpenParam param)
        {
            param = new LaserpenParam(1);
            string path = GetLegacyPenParamPath(cardNumber, ezdFilePath, penNo);
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(LaserpenParam));
                    param = (LaserpenParam)serializer.Deserialize(stream);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void SavePenParam(int cardNumber, string ezdFilePath, LaserpenParam param)
        {
            if (string.IsNullOrEmpty(ezdFilePath))
            {
                return;
            }

            string path = GetPenParamPath(cardNumber, ezdFilePath, param.nPenNo);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (FileStream stream = File.Create(path))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(LaserpenParam));
                serializer.Serialize(stream, param);
            }
        }

        private static void SaveDefaultPenParam(int cardNumber, LaserpenParam param)
        {
            string path = GetDefaultPenParamPath(cardNumber, param.nPenNo);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (FileStream stream = File.Create(path))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(LaserpenParam));
                serializer.Serialize(stream, param);
            }
        }

        private static string GetPenParamPath(int cardNumber, string ezdFilePath, int penNo)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData", "LaserPenParams");
            string ezdKey = GetStableFileKey(ezdFilePath);
            return Path.Combine(dir, $"{ezdKey}_card_{cardNumber}_pen_{penNo}.xml");
        }

        private static string GetCardPenParamPath(int cardNumber, string ezdFilePath)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData", "LaserPenParams");
            string ezdKey = GetStableFileKey(ezdFilePath);
            return Path.Combine(dir, $"{ezdKey}_card_{cardNumber}.xml");
        }

        private static string GetLegacyPenParamPath(int cardNumber, string ezdFilePath, int penNo)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData", "LaserPenParams");
            string ezdKey = GetStableFileKey(ezdFilePath);
            return Path.Combine(dir, $"{ezdKey}_card_{cardNumber}_pen_{penNo}.xml");
        }

        private static string GetDefaultPenParamPath(int cardNumber, int penNo)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData", "LaserPenParams");
            return Path.Combine(dir, $"default_card_{cardNumber}_pen_{penNo}.xml");
        }


        private static string GetStableFileKey(string filePath)
        {
            string fullPath = Path.GetFullPath(filePath).ToUpperInvariant();
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(fullPath));
                StringBuilder sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
