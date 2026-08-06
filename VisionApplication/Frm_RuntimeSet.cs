using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CommonCls;
using System.IO;
using System.IO.Ports;
using System.Runtime.Serialization.Formatters.Binary;
//using OpenCvSharp;
using System.Runtime.InteropServices;

namespace VisionApplication
{
    public partial class Frm_RuntimeSet : Form
    {

        /* [DllImport("dwmapi.dll")]
         private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

         [DllImport("dwmapi.dll")]
         private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

         [StructLayout(LayoutKind.Sequential)]
         public struct MARGINS
         {
             public int leftWidth;
             public int rightWidth;
             public int topHeight;
             public int bottomHeight;
         }

         private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
         private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
         private const int DWMWA_BORDER_COLOR = 34;
         private const int DWMWA_CAPTION_COLOR = 35;*/

        // Win32 API声明
        [DllImport("uxtheme.dll", ExactSpelling = true)]
        private static extern int SetWindowTheme(IntPtr hWnd, string subAppName, string subIdList);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        private const int WM_NCPAINT = 0x85;
        private Color _titleBarColor = Color.FromArgb(255,80,160,255);



        CUserParam cup;
        public bool bVPPChange = false;
        bool bLoadVPPChange = false; //窗体加载时VPP目录的修改
        public bool bVPPParamChanged = false;
        public bool bMaskChanged = false;
        public string strVPPFileName;
        int iLSInd=0;  //光源序号
        const int maxCameraNum =2;
        int iLastcmbRecordType = 0;

        int iLastCMIndex = 0;

        string[] strPortName;
        FileOper fo = new FileOper();
        int iType = 0;



       /* protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // 禁用主题样式
            SetWindowTheme(this.Handle, "", "");
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_NCPAINT && this.FormBorderStyle != FormBorderStyle.None)
            {
                IntPtr hdc = GetWindowDC(m.HWnd);
                using (Graphics g = Graphics.FromHdc(hdc))
                {
                    Rectangle bounds = new Rectangle(5, 5, this.Width-75, SystemInformation.CaptionHeight);
                    using (SolidBrush brush = new SolidBrush(_titleBarColor))
                    {
                        g.FillRectangle(brush, bounds);
                    }
                }
                ReleaseDC(m.HWnd, hdc);
            }
        }*/

       /* private void SetTitleBarColor(Color color)
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                var colorValue = 255;// ColorToInt(color);

                // 设置边框颜色
                DwmSetWindowAttribute(this.Handle, DWMWA_BORDER_COLOR, ref colorValue, sizeof(int));

                // 设置标题栏颜色
                DwmSetWindowAttribute(this.Handle, DWMWA_CAPTION_COLOR, ref colorValue, sizeof(int));
            }
        }

        private int ColorToInt(Color color)
        {
            return (color.B << 16) | (color.G << 8) | color.R;
        }*/



        public Frm_RuntimeSet()
        {
            Application.EnableVisualStyles();
            InitializeComponent();
        //    SetTitleBarColor(Color.Blue); // 蓝色标题栏
            ReadUserParam();
            iType = 1;
        }
        public Frm_RuntimeSet(CUserParam cup)
        {
            InitializeComponent();
           // SetTitleBarColor(Color.FromArgb(0, 0, 255, 200)); // 蓝色标题栏
            this.cup = cup;
        }
        private void ReadUserParam()
        {
            try
            { 
                bool bStatus = fo.ReadUserParam(ref cup);  
            }
            catch
            {
               
            }
           
        }
        public CUserParam _cup
        {
            set { cup = value; }
            get { return cup; }
        }
     //   List<string> strPortNameTmp=new List<string>();
        List<int> comInd=new List<int>();
        /// <summary>
        /// 获取系统中所有可用的串口列表（按COM号排序）
        /// </summary>
        private List<string> GetAvailableSerialPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            var sortedPorts = new List<string>();

            try
            {
                var comNumbers = new List<int>();
                foreach (string port in ports)
                {
                    if (port.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                    {
                        int comNumber;
                        if (int.TryParse(port.Substring(3), out comNumber))
                        {
                            comNumbers.Add(comNumber);
                        }
                    }
                }

                comNumbers.Sort();

                foreach (int num in comNumbers)
                {
                    sortedPorts.Add("COM" + num);
                }
            }
            catch
            {
                sortedPorts = new List<string>(ports);
                sortedPorts.Sort();
            }

            return sortedPorts;
        }

        /// <summary>
        /// 刷新UI中的串口选择控件
        /// </summary>
        private void RefreshSerialPortComboBoxes()
        {
            List<string> ports = GetAvailableSerialPorts();

            // 保存当前选中项
            string[] selectedItems = new string[4];
            if (cbxPortName1 != null) selectedItems[0] = cbxPortName1.Text;
            if (cbxPortName2 != null) selectedItems[1] = cbxPortName2.Text;
            if (cbxPortName3 != null) selectedItems[2] = cbxPortName3.Text;
            if (cbxPortName4 != null) selectedItems[3] = cbxPortName4.Text;

            // 刷新列表
            if (cbxPortName1 != null)
            {
                cbxPortName1.Items.Clear();
                foreach (string port in ports)
                    cbxPortName1.Items.Add(port);
                if (cbxPortName1.Items.Contains(selectedItems[0]))
                    cbxPortName1.Text = selectedItems[0];
                else if (cbxPortName1.Items.Count > 0)
                    cbxPortName1.SelectedIndex = 0;
            }

            if (cbxPortName2 != null)
            {
                cbxPortName2.Items.Clear();
                foreach (string port in ports)
                    cbxPortName2.Items.Add(port);
                if (cbxPortName2.Items.Contains(selectedItems[1]))
                    cbxPortName2.Text = selectedItems[1];
                else if (cbxPortName2.Items.Count > 0)
                    cbxPortName2.SelectedIndex = 0;
            }

            if (cbxPortName3 != null)
            {
                cbxPortName3.Items.Clear();
                foreach (string port in ports)
                    cbxPortName3.Items.Add(port);
                if (cbxPortName3.Items.Contains(selectedItems[2]))
                    cbxPortName3.Text = selectedItems[2];
                else if (cbxPortName3.Items.Count > 0)
                    cbxPortName3.SelectedIndex = 0;
            }

            if (cbxPortName4 != null)
            {
                cbxPortName4.Items.Clear();
                foreach (string port in ports)
                    cbxPortName4.Items.Add(port);
                if (cbxPortName4.Items.Contains(selectedItems[3]))
                    cbxPortName4.Text = selectedItems[3];
                else if (cbxPortName4.Items.Count > 0)
                    cbxPortName4.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 获取指定索引的串口名称（用于配置保存）
        /// </summary>
        private int GetPortIndex(string portName)
        {
            List<string> ports = GetAvailableSerialPorts();
            return ports.IndexOf(portName);
        }

        /// <summary>
        /// 验证串口配置是否有效
        /// </summary>
        private bool ValidateSerialConfiguration()
        {
            List<string> ports = GetAvailableSerialPorts();

            if (ports.Count == 0)
            {
                MessageBox.Show("未检测到任何可用的串口设备，请检查硬件连接。", "警告",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private bool ValidateBoardAssignParam()
        {
            if (cmbBoardA.SelectedIndex < 0 || cmbBoardB.SelectedIndex < 0)
            {
                MessageBox.Show("请先完成A面和B面的板卡分配。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (cmbBoardA.SelectedIndex == cmbBoardB.SelectedIndex)
            {
                MessageBox.Show("A面和B面不能分配到同一张板卡。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }
        private void Frm_RuntimeSet_Load(object sender, EventArgs e)
        {
            bMaskChanged = true;

            // 获取排序后的串口列表
            List<string> sortedPorts = GetAvailableSerialPorts();
            strPortName = sortedPorts.ToArray();

            setParam();
            bMaskChanged = false;
            for (int i = 0; i <4; i++)
                tabControl1.TabPages.Remove(tabControl1.TabPages[7]);
        }

        private void btOK_Click(object sender, EventArgs e)
        {
            if (!ValidateBoardAssignParam())
                return;

            getParam();

            if(iType==1)
            {
                FileOper.SaveUserParam(cup);
                this.Close();

            }
            
        }

       
        private void cmbLightSource_SelectedIndexChanged(object sender, EventArgs e)
        {
          //  int ind = iLSInd;
            GetFXParam(iLSInd);
            iLSInd = cmbLightSource.SelectedIndex;
            SetFXParam(iLSInd);
        }

        #region  路径设置
        string strFatherPath;
        private void btPathChoose_Click(object sender, EventArgs e)
        {

            FolderBrowserDialog path = new FolderBrowserDialog();
            switch (((Button)sender).Name)
            {
                case "btVpp":                                   //视觉模型VPP文件路径
                    path.SelectedPath= tbVPPPath.Text;
                    break;
                case "btSingleFrame":                           //单帧图像存储路径
                    path.SelectedPath= tbSingleFramePath.Text ;
                    break;
                case "btContFrame":                             //图像组存储路径
                    path.SelectedPath= tbContFramePath.Text;
                    break;
                case "btLog":                                   //日志存储路径
                    path.SelectedPath= tbLogPath.Text;
                    break;
                case "btResultSave":                            //结果存储
                    path.SelectedPath= tbResultSave.Text;
                    break;
                case "btPlayBack":                              //数据回放路径
                    path.SelectedPath = tbPlayback.Text ;
                    break;
                case "btContErr":                               //误判存储路径
                     path.SelectedPath = tbContErrPath.Text;
                    break;
                case "btMarkPic":                               //待标注图片路径
                     path.SelectedPath = tbMarkPicPath.Text;
                    break;
                case "btManuMarkFile":                              //
                     path.SelectedPath = tbManuMarkFilePath.Text;
                    break;
                case "btAutoMarkFile":                           //
                     path.SelectedPath =tbAutoMarkPath.Text;
                    break;
                case "btExport":                                    //导出图片路径
                    path.SelectedPath = tbExportPath.Text;
                    break;
                case "bt9PointCalibFile":
                    path.SelectedPath = tb9PointCalibFile.Text;
                    break;


            }

            path.ShowNewFolderButton = true;
            if (path.ShowDialog() == DialogResult.OK)
            {
                //   sp.strPath = path.SelectedPath + "\\";
                //   txtPath.Text = sp.strPath;
                switch (((Button)sender).Name)
                {
                    case "btVpp":
                        tbVPPPath.Text = path.SelectedPath;
                        break;
                    case "btSingleFrame":
                        tbSingleFramePath.Text = path.SelectedPath;
                        break;
                    case "btContFrame":
                        tbContFramePath.Text = path.SelectedPath;
                        break;
                    case "btLog":
                        tbLogPath.Text = path.SelectedPath;
                        break;
                    case "btResultSave":
                        tbResultSave.Text = path.SelectedPath;
                        break;
                    case "btPlayBack":
                        tbPlayback.Text= path.SelectedPath;
                        break;
                    case "btContErr":
                        tbContErrPath.Text= path.SelectedPath;
                        break;
                    case "btMarkPic":
                        tbMarkPicPath.Text = path.SelectedPath;
                        break;
                    case "btManuMarkFile":
                        tbManuMarkFilePath.Text = path.SelectedPath;
                        break;
                  /*  case "btAutoMarkFile":
                        tbManuMarkFilePath.Text = path.SelectedPath;
                        break;*/
                    case "btErrMark":
                        tbAutoMarkPath.Text = path.SelectedPath;
                        break;
                    case "btExport":
                        tbExportPath.Text= path.SelectedPath;
                        break;
                    case "btFatherPath":
                        tbFatherPath.Text = path.SelectedPath;
                        strFatherPath = tbFatherPath.Text;
                        if (chkAutoSetPath.Checked)
                        {
                            //groupPath.Enabled = false;
                            AutoSetPath(strFatherPath);
                        }
                        break;
                    case "bt9PointCalibFile":
                        tb9PointCalibFile.Text = path.SelectedPath;
                        break;
                }

            }

        }

        private void btVpp_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //  openFileDialog.Filter=
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                strVPPFileName = openFileDialog.FileName;
                tbVPPPath.Text = strVPPFileName;
                bVPPChange = true;
            }

        }
        #endregion  路径设置

        #region  记录选择排序
        private void chkAll_CheckedChanged(object sender, EventArgs e)
        { 
        }

        private void btFilter_Click(object sender, EventArgs e)
        {
            string strFilter = txtFilter.Text;
            string itemName;
            for (int i = 0; i < chkList_SubRecordsShow.Items.Count; i++)
            {
                if (chkAll.Checked == true)
                {
                    if (strFilter == "")
                        chkList_SubRecordsShow.SetItemChecked(i, true);
                    else
                    {
                        
                        itemName = chkList_SubRecordsShow.Items[i].ToString();
                        if (itemName.IndexOf(strFilter) != -1)
                        {
                            chkList_SubRecordsShow.SetItemChecked(i, true);
                        }
                        // strFilter.IndexOf()
                        //  if(chkList_SubRecordsShow.GetItemText[i].)
                    }
                }
                else
                {
                    if (strFilter == "")
                        chkList_SubRecordsShow.SetItemChecked(i, false);
                    else
                    {
                        itemName = chkList_SubRecordsShow.Items[i].ToString();
                        if (itemName.IndexOf(strFilter) != -1)
                        {
                            chkList_SubRecordsShow.SetItemChecked(i, false);
                        }
                    }
                }
            }
        }
         
        private void btAdd_Click(object sender, EventArgs e)
        {
            if (chkList_SubRecordsShow.GetItemChecked(chkList_SubRecordsShow.SelectedIndex))
            {
                if (lstBox_GUIRecord.SelectedIndex < 0)
                    lstBox_GUIRecord.Items.Add(chkList_SubRecordsShow.Items[chkList_SubRecordsShow.SelectedIndex].ToString());
                else
                    lstBox_GUIRecord.Items.Insert(lstBox_GUIRecord.SelectedIndex + 1, chkList_SubRecordsShow.Items[chkList_SubRecordsShow.SelectedIndex].ToString());
            }
        }

        private void btRemove_Click(object sender, EventArgs e)
        {
            if (lstBox_GUIRecord.SelectedItem != null)
                lstBox_GUIRecord.Items.Remove(lstBox_GUIRecord.SelectedItem);
        }
        private void btMoveUp_Click(object sender, EventArgs e)
        {

            int index = lstBox_GUIRecord.SelectedIndex;

            if (index == -1)//没选中
            {
                return;
            }
            else if (index > 0)
            {
                string str = lstBox_GUIRecord.Items[index].ToString();
                lstBox_GUIRecord.Items.RemoveAt(index);
                lstBox_GUIRecord.Items.Insert(index - 1, str);
                lstBox_GUIRecord.SelectedIndex = index - 1;
            } 
        }

        private void btMoveDown_Click(object sender, EventArgs e)
        {
            int index = lstBox_GUIRecord.SelectedIndex;

            if (index == -1) //没选中
            {
                return;
            }
            else if (index < lstBox_GUIRecord.Items.Count - 1)
            {
                string str = lstBox_GUIRecord.Items[index].ToString();
                lstBox_GUIRecord.Items.RemoveAt(index);
                lstBox_GUIRecord.Items.Insert(index + 1, str);
                lstBox_GUIRecord.SelectedIndex = index + 1;
            } 
        }
        #endregion  记录选择排序

        private void cmbRecordType_SelectedIndexChanged(object sender, EventArgs e)
        {
            int iNum = maxCameraNum;
             
            //保存修改记录
            iNum = maxCameraNum <= lstBox_GUIRecord.Items.Count ? maxCameraNum : lstBox_GUIRecord.Items.Count;
            switch (iLastcmbRecordType)
            {
                case 0:                   
                    //默认显示记录
                    if (cup.p_SRN.strDefaultGUIName == null)
                        cup.p_SRN.strDefaultGUIName = new string[maxCameraNum]; 
                    for (int i = 0; i < iNum; i++)
                    {
                        cup.p_SRN.strDefaultGUIName[i] = lstBox_GUIRecord.Items[i].ToString();
                    }
                    break;
                case 1:
                    
                    //灰度图显示记录
                    if (cup.p_SRN.strGreyGUIName == null)
                        cup.p_SRN.strGreyGUIName = new string[3];
                  
                    for (int i = 0; i < iNum; i++)
                    {
                        cup.p_SRN.strGreyGUIName[i] = lstBox_GUIRecord.Items[i].ToString();
                    }
                    break;
                case 2:
                    // 光度图显示记录
                    if (cup.p_SRN.strFXGUIName == null)
                        cup.p_SRN.strFXGUIName = new string[3];
                   
                    for (int i = 0; i < iNum; i++)
                    {
                        cup.p_SRN.strFXGUIName[i] = lstBox_GUIRecord.Items[i].ToString();
                    }
                    break;
                case 3:
                    // 定位图显示记录
                    if (cup.p_SRN.strPMAGUIName == null)
                        cup.p_SRN.strPMAGUIName = new string[3];
                   
                    for (int i = 0; i < iNum; i++)
                    {
                        cup.p_SRN.strPMAGUIName[i] = lstBox_GUIRecord.Items[i].ToString();
                    }
                    break;
                default:
                      //默认显示记录
                    if (cup.p_SRN.strDefaultGUIName == null)
                        cup.p_SRN.strDefaultGUIName = new string[maxCameraNum];
                    for (int i = 0; i < iNum; i++)
                    {
                        cup.p_SRN.strDefaultGUIName[i] = lstBox_GUIRecord.Items[i].ToString();
                    }
                    break;
            }
             
            //更新记录
            iLastcmbRecordType = cmbRecordType.SelectedIndex;
            switch (iLastcmbRecordType)
            {
                case 0:
                    lstBox_GUIRecord.Items.Clear();
                    if (cup.p_SRN.strDefaultGUIName != null)
                    {
                        for (int i = 0; i < maxCameraNum; i++)
                        {
                            if (cup.p_SRN.strDefaultGUIName[i] != null)
                                lstBox_GUIRecord.Items.Add(cup.p_SRN.strDefaultGUIName[i]);
                        }
                    }
                    break;
                case 1:
                    lstBox_GUIRecord.Items.Clear();
                    if (cup.p_SRN.strGreyGUIName != null)
                    {
                        for (int i = 0; i < maxCameraNum; i++)
                        {
                            if (cup.p_SRN.strGreyGUIName[i] != null)
                                lstBox_GUIRecord.Items.Add(cup.p_SRN.strGreyGUIName[i]);
                        }
                    }
                    break;
                case 2:
                    lstBox_GUIRecord.Items.Clear();
                    if (cup.p_SRN.strFXGUIName != null)
                    {
                        for (int i = 0; i < maxCameraNum; i++)
                        {
                            if (cup.p_SRN.strFXGUIName[i] != null)
                                lstBox_GUIRecord.Items.Add(cup.p_SRN.strFXGUIName[i]);
                        }
                    }
                    break;
                case 3:
                    lstBox_GUIRecord.Items.Clear();
                    if (cup.p_SRN.strPMAGUIName != null)
                    {
                        for (int i = 0; i < maxCameraNum; i++)
                        {
                            if (cup.p_SRN.strPMAGUIName[i] != null)
                                lstBox_GUIRecord.Items.Add(cup.p_SRN.strPMAGUIName[i]);
                        }
                    }
                    break;
                 default:
                    lstBox_GUIRecord.Items.Clear();
                    if (cup.p_SRN.strDefaultGUIName != null)
                    {
                        for (int i = 0; i < maxCameraNum; i++)
                        {
                            if (cup.p_SRN.strDefaultGUIName[i] != null)
                                lstBox_GUIRecord.Items.Add(cup.p_SRN.strDefaultGUIName[i]);
                        }
                    }
                    break;
            }
              
        }

        //获取现场参数
        private void GetSceneParam()
        {
            try
            {
                cup.p_LI.strLineName = txtLineName.Text;
                cup.p_LI.strLineDesc = txtLineDesc.Text;
                cup.p_LI.LineNum = cmbLine.SelectedIndex + 1;
                if (chkLineNumPrefix.Checked || chkSelfPrefix.Checked)
                    cup.p_LI.strLinePrefix = txtLineSelfPrefix.Text;
                else
                    cup.p_LI.strLinePrefix = "";
                cup.p_LI.strLineScence = "Scene" + cup.p_LI.LineNum.ToString() + "#.jpg";
                cup.p_LI.strCamPrefix[0] = txtMainPrefix.Text;
                cup.p_LI.strCamPrefix[1] = txtSub1Prefix.Text;
              //  cup.p_LI.strCamPrefix[2] = txtSub2Prefix.Text;
             //   cup.p_LI.strCamPrefix[3] = txtSub3Prefix.Text;
             //   cup.p_LI.strCamPrefix[4] = txtSub4Prefix.Text;
                for (int i = 0; i < maxCameraNum; i++)
                {
                    if (cup.p_LI.strLinePrefix != "")
                        cup.p_LI.strPrefix[i] = cup.p_LI.strLinePrefix + "_" + cup.p_LI.strCamPrefix[i] + "_";
                    else
                        cup.p_LI.strPrefix[i] = cup.p_LI.strCamPrefix[i] + "_";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("现场参数获取有误：" + ex.Message.ToString());
            }
        }

        //获取相机分配参数
        private void GetCamDistParam()
        {
            //相机分配参数
            try
            {
                cup.p_CD.camInd[0] = cmbCamInd1.SelectedIndex;
                cup.p_CD.camInd[1] = cmbCamInd2.SelectedIndex;
              //  cup.p_CD.camInd[2] = cmbCamInd3.SelectedIndex;
             //   cup.p_CD.camInd[3] = cmbCamInd4.SelectedIndex;
             //   cup.p_CD.camInd[4] = cmbCamInd5.SelectedIndex;

                if (rd1.Checked)
                {
                    cup.p_CD.triggleCam = 0;
                }
                else if (rd2.Checked)
                {
                    cup.p_CD.triggleCam = 1;
                }
                else if (rd3.Checked)
                {
                    cup.p_CD.triggleCam = 2;
                }
                else if (rd4.Checked)
                {
                    cup.p_CD.triggleCam = 3;
                }
                else if (rd5.Checked)
                {
                    cup.p_CD.triggleCam = 4;
                }

                if (cup.p_CD.bListProcess == null)
                    cup.p_CD.bListProcess = new bool[maxCameraNum];
                for (int i = 0; i < maxCameraNum; i++)
                {//((CheckBox)this.Controls.Find("ckb" + (i + 1).ToString(), true)[0]).Checked;
                    cup.p_CD.bListProcess[i] = ((CheckBox)this.Controls.Find("chkCam" + (i + 1).ToString() + "Used", true)[0]).Checked;
                }

                cup.p_CD.triggleMode = cmbTriggleModel.SelectedIndex;
                cup.p_CD.bTriggle = chbTriggleOpen.Checked;
            }
            catch (Exception ex)
            {
                MessageBox.Show("相机分配参数获取有误：" + ex.Message.ToString());
            }
        }

        //获取吹气参数
        private void GetBlowParam()
        {
            //吹气参数
            try
            {
                cup.p_BP.blowType = cmbblowType.SelectedIndex;
                cup.p_BP.bUsed = chkBlowUsed.Checked;
                cup.p_BP.perBlowTime = (cmbBlowPerTime.SelectedIndex + 1) * 200;
                cup.p_BP.intervTimes = Int32.Parse(txtPerTimes.Text);
                if (rBPerDay.Checked)
                {
                    cup.p_BP.intervType = 0;
                }
                else if (rBPerHour.Checked)
                {
                    cup.p_BP.intervType = 1;
                }
                else if (rBPerMin.Checked)
                {
                    cup.p_BP.intervType = 2;
                }
                if (rBBefore.Checked)
                {
                    cup.p_BP.reguType = 0;
                }
                else
                {
                    cup.p_BP.reguType = 1;
                }
                cup.p_BP.perExeTimes = Int32.Parse(txtPerExeTimes.Text);
                txtPerExeTimes.Text = cup.p_BP.perExeTimes.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("吹气参数获取有误：" + ex.Message.ToString());
            }
        }

        //获取串口通信参数
        private void GetSerialParam()
        {
            //串口分配
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    if (strPortName.Length > 0)
                        cup.p_SD.serInd[i] = ((ComboBox)this.Controls.Find("cbxPortName" + (i + 1).ToString(), true)[0]).SelectedIndex;

                    cup.p_SD.bUsed[i] = ((CheckBox)this.Controls.Find("chkSerUsed" + (i + 1).ToString(), true)[0]).Checked;
                }
                cup.p_SD.BaudRate = int.Parse(cbxBaudRate1.Text);
                cup.p_SD.DataBits = int.Parse(cbxDataBit.Text);
                cup.p_SD.parity = (int)Enum.Parse(typeof(Parity), cbxParity1.Text);
                cup.p_SD.stopBits = (int)Enum.Parse(typeof(StopBits), cbxStopBits1.Text);
                cup.p_SD.WriteTimeout = int.Parse(txtTimeOut.Text);
                if (cup.p_SD.stationCardMap == null || cup.p_SD.stationCardMap.Length < 2)
                    cup.p_SD.stationCardMap = new int[2];
                cup.p_SD.stationCardMap[0] = cmbBoardA.SelectedIndex;
                cup.p_SD.stationCardMap[1] = cmbBoardB.SelectedIndex;
            }
            catch (Exception ex)
            {
                MessageBox.Show("串口参数获取有误：" + ex.Message.ToString());
            }
        }

        //获取路径参数
        private void GetPathParm()
        {
            //路径设置参数
            try
            {
                cup.p_Paf.strVPPFilePath = tbVPPPath.Text;
                cup.p_Paf.strSigSavePath = tbSingleFramePath.Text;
                cup.p_Paf.strConSavePath = tbContFramePath.Text;
                cup.p_Paf.strLogPath = tbLogPath.Text;
                cup.p_Paf.strResSavePath = tbResultSave.Text;
                cup.p_Paf.strPlaybackPath = tbPlayback.Text;
                cup.p_Paf.strErrPath = tbContErrPath.Text;
                cup.p_Paf.MarkPicPath = tbMarkPicPath.Text;
                cup.p_Paf.AutoMarkPath = tbAutoMarkPath.Text;
                cup.p_Paf.ManuMarkPath = tbManuMarkFilePath.Text;
                cup.p_Paf.strExportPath = tbExportPath.Text;

                cup.p_Paf.strExportPath = tb9PointCalibFile.Text;
            }
            catch (Exception ex)
            {
                MessageBox.Show("路径参数获取有误：" + ex.Message.ToString());
            }
        }
       
        //获取光度立体重构参数
        private void GetFXParam(int ind)
        {
            try
            {
                switch (ind)
                {
                    case 0:
                        for (int i = 0; i < 4; i++)
                        {
                            cup.p_FX.Slants[i] = (double)((NumericUpDown)this.Controls.Find("num_Slants" + (i + 1).ToString(), true)[0]).Value;
                            cup.p_FX.Tilts[i] = (double)((NumericUpDown)this.Controls.Find("num_Tilts" + (i + 1).ToString(), true)[0]).Value;
                        }
                        cup.p_FX.sigma = double.Parse(tbSigma.Text);
                        cup.p_FX.dMult = double.Parse(tbFactor.Text);
                        cup.p_FX.dAdd = double.Parse(tbOffset.Text);
                        break;
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        for (int i = 0; i < 4; i++)
                        {
                            cup.p_subFX[ind - 1].Slants[i] = (double)((NumericUpDown)this.Controls.Find("num_Slants" + (i + 1).ToString(), true)[0]).Value;
                            cup.p_subFX[ind - 1].Tilts[i] = (double)((NumericUpDown)this.Controls.Find("num_Tilts" + (i + 1).ToString(), true)[0]).Value;
                        }
                        cup.p_subFX[ind - 1].sigma = double.Parse(tbSigma.Text);
                        cup.p_subFX[ind - 1].dMult = double.Parse(tbFactor.Text);
                        cup.p_subFX[ind - 1].dAdd = double.Parse(tbOffset.Text);
                        break;
                }
            }
            catch (Exception ex)
            {
                cup.InitSubFXParam();
                MessageBox.Show("光度立体参数获取有误：" + ex.Message.ToString());
            }
        }
      
        //获取IO口参数
        private void GetIOParam()
        {
            try
            {
                cup.p_IOPP.APlane = cmbAPlane.SelectedIndex;
                cup.p_IOPP.BPlane = cmbBPlane.SelectedIndex;
                cup.p_IOPP.EndAPlane = cmbEndAPlane.SelectedIndex;
                cup.p_IOPP.EndBPlane = cmbEndBPlane.SelectedIndex;
                cup.p_IOPP.OkOut = cmbOkOut.SelectedIndex;
                cup.p_IOPP.NGOut = cmbNgOut.SelectedIndex;
                cup.p_IOPP.IOTimeDelay =Int32.Parse( cmbIOTimeDelay.Text);
            }
            catch(Exception ex)
            {
                MessageBox.Show("IO端口参数获取有误：" + ex.Message.ToString());
            }
            
        }
        //vpp 运行参数
        private void GetRunParam()
        {
            try
            {
                cup.p_VPPRTP.bAcqFromCam = chkAcqFromCam.Checked;
                cup.p_VPPRTP.bAcqFromIDB = chkAcqFromIDB.Checked;
                cup.p_VPPRTP.bUseFX = chkUseFX.Checked;
                cup.p_VPPRTP.bRemarkResult = chkRemarkResult.Checked;
                cup.p_VPPRTP.bRemarkArea = chkRemarkArea.Checked;
                cup.p_VPPRTP.bUseWeakCrack = chkUseWeakCrack.Checked;
              
            }
            catch (Exception ex)
            {
                MessageBox.Show("运行参数获取有误：" + ex.Message.ToString());
            }
        }

        //获取诊断参数
        private void GetCMDiagParam()
        {
            //保存上次记录
            try
            {
                cup.p_DP.cmParam[iLastCMIndex].edgeAbnormCount = double.Parse(txt_edgeAbnormCount.Text);
                cup.p_DP.cmParam[iLastCMIndex].edgeLeftAbnormCount = double.Parse(txt_edgeLeftAbnormCount.Text);
                cup.p_DP.cmParam[iLastCMIndex].edgeRightAbnormCount = double.Parse(txt_edgeRightAbnormCount.Text);
                cup.p_DP.cmParam[iLastCMIndex].lwArea = double.Parse(txt_lwArea.Text);

                {
                    cup.p_DP.dmParam.greyAbnormArea = double.Parse(txt_greyAbnormArea.Text);
                    cup.p_DP.dmParam.greyAbnormCount = double.Parse(txt_greyAbnormCount.Text);
                    cup.p_DP.dmParam.FXAbnormArea1 = double.Parse(txt_FXAbnormArea1.Text);
                    cup.p_DP.dmParam.FXAbnormArea2 = double.Parse(txt_FXAbnormArea2.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("视觉诊断参数获取有误：" + ex.Message.ToString());
            }
        }
        private void GetViewListParam()
        {
            //记录清单
            try
            {
                if (cup.p_SRN.bList == null || cup.p_SRN.bList.Length != cup.p_SRN.count)
                {
                    cup.p_SRN.bList = new bool[cup.p_SRN.count];
                }
                int iSelectCount = 0;
                for (int i = 0; i < cup.p_SRN.count; i++)
                {
                    cup.p_SRN.bList[i] = chkList_SubRecordsShow.GetItemChecked(i);
                    if (cup.p_SRN.bList[i])
                        iSelectCount++;
                }
                cup.p_SRN.strSelectName = new string[iSelectCount];
                iSelectCount = 0;
                for (int i = 0; i < cup.p_SRN.count; i++)
                {
                    if (cup.p_SRN.bList[i])
                    {
                        cup.p_SRN.strSelectName[iSelectCount] = cup.p_SRN.strName[i];
                        iSelectCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("视图清单参数获取有误：" + ex.Message.ToString());
            }
        }
        private void SetDLParam()
        {
            tbModelPath.Text = cup.p_DL.modelPath;
            cmb_Threshold.Text = cup.p_DL.fThreshold.ToString();
            cmb_ThresholdGray_crack.Text = cup.p_DL.fThresholdCrackGrey.ToString();
            cmb_ThresholdFX_posz.Text = cup.p_DL.fThresholdPoszFX.ToString();
            cmb_ThresholdGray_posz.Text = cup.p_DL.fThresholdPoszGrey.ToString();

            cmb_ThresholdCrack.Text = cup.p_DL.fThresholdCrack.ToString();
            cmb_ThresholdPosz.Text = cup.p_DL.fThresholdPosz.ToString();
            cmb_ThresholdIoU.Text = cup.p_DL.fIoUTh.ToString();
            cmb_ThresholdPoszNum.Text = cup.p_DL.iPoszNum.ToString();

            txtDL_CrackLenTh.Text = cup.p_DL.fThresholdCrackLen.ToString();
            txtDL_PoszAreaTh.Text = cup.p_DL.fThresholdPoszArea.ToString();
            txtDL_PoszSideTh.Text = cup.p_DL.fThresholdPoszSide.ToString();

            /*cup.p_DL.fThresholdCrackGrey = float.Parse(cmb_ThresholdGray_crack.Text);
            cup.p_DL.fThresholdPoszFX = float.Parse(cmb_ThresholdFX_posz.Text);
            cup.p_DL.fThresholdPoszGrey = float.Parse(cmb_ThresholdGray_posz.Text);*/
            cmb_CertThresholdCrack.Text = cup.p_CP.fSureCrackParam[0].ToString();
            cmb_CertThresholdPosz.Text = cup.p_CP.fPoszParam[0].ToString();

            cmb_ModelType.Text = cup.p_DL.modelType;
            if (cup.p_DL.bUseGPU)
                cmb_EvnType.SelectedIndex = 1;
            else
                cmb_EvnType.SelectedIndex = 0;
            txtGPUID.Text = cup.p_DL.gpuID.ToString();
        }
        private void GetDLParam()
        {
            cup.p_DL.modelPath = tbModelPath.Text;
            cup.p_DL.fThreshold = float.Parse(cmb_Threshold.Text);
            cup.p_DL.fThresholdCrackGrey = float.Parse(cmb_ThresholdGray_crack.Text);
            cup.p_DL.fThresholdPoszFX = float.Parse(cmb_ThresholdFX_posz.Text);
            cup.p_DL.fThresholdPoszGrey = float.Parse(cmb_ThresholdGray_posz.Text);

            cup.p_DL.fThresholdCrack = float.Parse(cmb_ThresholdCrack.Text);
            cup.p_DL.fThresholdPosz = float.Parse(cmb_ThresholdPosz.Text);



            cup.p_DL.fIoUTh = float.Parse(cmb_ThresholdIoU.Text);
            cup.p_DL.iPoszNum = int.Parse(cmb_ThresholdPoszNum.Text);

            cup.p_DL.fThresholdCrackLen = float.Parse(txtDL_CrackLenTh.Text);
            cup.p_DL.fThresholdPoszArea = float.Parse(txtDL_PoszAreaTh.Text);
            cup.p_DL.fThresholdPoszSide = float.Parse(txtDL_PoszSideTh.Text);


            cup.p_CP.fSureCrackParam[0] = float.Parse(cmb_CertThresholdCrack.Text);
            cup.p_CP.fSureCrackParam[1] = float.Parse(cmb_CertThresholdCrack.Text);

            cup.p_CP.fPoszParam[0] = float.Parse(cmb_CertThresholdPosz.Text);
            cup.p_CP.fPoszParam[1] = float.Parse(cmb_CertThresholdPosz.Text);


            cup.p_DL.modelType = cmb_ModelType.Text;

            if (cmb_EvnType.SelectedIndex == 1)
                cup.p_DL.bUseGPU = true;
            else
                cup.p_DL.bUseGPU = false;
            cup.p_DL.gpuID = Int32.Parse(txtGPUID.Text);

        }

        private void GetHightLevelCrackParam()
        {
            if (iLstFX1 >= 0)
                cup.p_CP.fCrackParam[iLstFX1] = float.Parse(txtCrackGroupParamFX1.Text);
            if (iLstGrey1 >= 0)
                cup.p_CP.fCrackParam[iLstGrey1 + 10] = float.Parse(txtCrackGroupParamGrey1.Text);
            if (iLstFX2 >= 0)
                cup.p_CP.fCrackParam[iLstFX2 + 20] = float.Parse(txtCrackGroupParamFX2.Text);
            if (iLstGrey2 >= 0)
                cup.p_CP.fCrackParam[iLstGrey2 + 40] = float.Parse(txtCrackGroupParamGrey2.Text);

        }
        private void SetHightLevelCrackParam()
        {
            txtCrackGroupParamFX1.Text = cup.p_CP.fCrackParam[iLstFX1].ToString();
            txtCrackGroupParamGrey1.Text = cup.p_CP.fCrackParam[iLstGrey1 + 10].ToString();
            txtCrackGroupParamFX2.Text = cup.p_CP.fCrackParam[iLstFX2 + 20].ToString();
            txtCrackGroupParamGrey2.Text = cup.p_CP.fCrackParam[iLstGrey2 + 40].ToString();
        }

        private void SaveViewList()
        {
            //保存修改记录
            int iNum = maxCameraNum <= lstBox_GUIRecord.Items.Count ? maxCameraNum : lstBox_GUIRecord.Items.Count;
            switch (iLastcmbRecordType)
            {
                case 0:
                    //默认显示记录
                    if (cup.p_SRN.strDefaultGUIName == null)
                        cup.p_SRN.strDefaultGUIName = new string[maxCameraNum];
                    for (int i = 0; i < iNum; i++)
                    {
                        cup.p_SRN.strDefaultGUIName[i] = lstBox_GUIRecord.Items[i].ToString();
                    }
                    break;
                case 1:

                    //灰度图显示记录
                    if (cup.p_SRN.strGreyGUIName == null)
                        cup.p_SRN.strGreyGUIName = new string[3];

                    for (int i = 0; i < iNum; i++)
                    {
                        cup.p_SRN.strGreyGUIName[i] = lstBox_GUIRecord.Items[i].ToString();
                    }
                    break;
                case 2:
                    // 光度图显示记录
                    if (cup.p_SRN.strFXGUIName == null)
                        cup.p_SRN.strFXGUIName = new string[3];

                    for (int i = 0; i < iNum; i++)
                    {
                        cup.p_SRN.strFXGUIName[i] = lstBox_GUIRecord.Items[i].ToString();
                    }
                    break;
                case 3:
                    // 定位图显示记录
                    if (cup.p_SRN.strPMAGUIName == null)
                        cup.p_SRN.strPMAGUIName = new string[3];

                    for (int i = 0; i < iNum; i++)
                    {
                        cup.p_SRN.strPMAGUIName[i] = lstBox_GUIRecord.Items[i].ToString();
                    }
                    break;
                default:
                    //默认显示记录
                    if (cup.p_SRN.strDefaultGUIName == null)
                        cup.p_SRN.strDefaultGUIName = new string[maxCameraNum];
                    for (int i = 0; i < iNum; i++)
                    {
                        cup.p_SRN.strDefaultGUIName[i] = lstBox_GUIRecord.Items[i].ToString();
                    }
                    break;
            }
        }

        //获取运行状态参数
        private void GetRunTimeStatusParam()
        {
            try
            {
                cup.p_RTSP.bAutoConnCam = chkConnCam.Checked ;
                cup.p_RTSP.bAutoConnSerial = chkConnSerial.Checked;
                cup.p_RTSP.bAutoConnJCZ = chkConnJCZ.Checked;
                cup.p_RTSP.bAutoConnMotionCard = chkConnMotionCard.Checked;
                cup.p_RTSP.bAutoRunVPP = chkRunVPP.Checked;

                cup.p_RTSP.bUpdateImage = chkUpdateImage.Checked;
                cup.p_RTSP.bUpdateRecord = chkUpdateRecord.Checked;
                cup.p_RTSP.bUpdateLog = chkUpdateLog.Checked;
                cup.p_RTSP.bUpdateIO = chkUpdateIO.Checked;
                cup.p_RTSP.bSaveRes = chkSaveRes.Checked;
                cup.p_RTSP.runTimeOut = Int32.Parse(cmbRunTimeOut.Text);

            }
            catch (Exception ex)
            {
                MessageBox.Show("运行状态参数获取有误：" + ex.Message.ToString());
            }

        }
        private void getParam()
        {
            GetSceneParam();        //获取现场参数
            GetCamDistParam();      //获取相机分配参数
            GetSerialParam();       //获取串口通信参数
            GetPathParm();          //获取路径参数
            GetViewListParam();
            
            SaveViewList();  //默认显示记录
            GetIOParam();
            GetRunTimeStatusParam();
            // GetFXParam(cmbLightSource.SelectedIndex);  //光度立体算法参数
            // GetRunParam();   //运行参数
            // GetCMDiagParam();    //诊断参数
             GetDLParam();        //深度学习参数
            //GetHightLevelCrackParam();    //高级别裂纹诊断参数
            // GetBlowParam();       //获取吹气参数

        }
        private void SetCMDiagParam()
        {
            try
            {
                txt_edgeAbnormCount.Text = cup.p_DP.cmParam[iLastCMIndex].edgeAbnormCount.ToString();
                txt_edgeLeftAbnormCount.Text = cup.p_DP.cmParam[iLastCMIndex].edgeLeftAbnormCount.ToString();
                txt_edgeRightAbnormCount.Text = cup.p_DP.cmParam[iLastCMIndex].edgeRightAbnormCount.ToString();
                txt_lwArea.Text = cup.p_DP.cmParam[iLastCMIndex].lwArea.ToString();

                //缺陷诊断参数
                //  if(cup.p_DP.dmParam == null)
                {
                    txt_greyAbnormArea.Text = cup.p_DP.dmParam.greyAbnormArea.ToString();
                    txt_greyAbnormCount.Text = cup.p_DP.dmParam.greyAbnormCount.ToString();
                    txt_FXAbnormArea1.Text = cup.p_DP.dmParam.FXAbnormArea1.ToString();
                    txt_FXAbnormArea2.Text = cup.p_DP.dmParam.FXAbnormArea2.ToString();
                }

                cmbCMIndex.SelectedIndex = 0;
                if (cup.p_DP.cmParam != null)
                {
                    txt_edgeAbnormCount.Text = cup.p_DP.cmParam[0].edgeAbnormCount.ToString();
                    txt_edgeLeftAbnormCount.Text = cup.p_DP.cmParam[0].edgeLeftAbnormCount.ToString();
                    txt_edgeRightAbnormCount.Text = cup.p_DP.cmParam[0].edgeRightAbnormCount.ToString();
                    txt_lwArea.Text = cup.p_DP.cmParam[0].lwArea.ToString();
                }

                //高级诊断参数
                cmbCrackGroupParamGrey1.SelectedIndex = 0;
                cmbCrackGroupParamFX1.SelectedIndex = 0;
                cmbCrackGroupParamGrey2.SelectedIndex = 0;
                cmbCrackGroupParamFX2.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("诊断参数设置有误：" + ex.Message.ToString());
            }
        }


        private void SetFXParam(int ind)
        {
            try
            {
                switch (ind)
                {
                    case 0:
                        for (int i = 0; i < 4; i++)
                        {
                            ((NumericUpDown)this.Controls.Find("num_Slants" + (i + 1).ToString(), true)[0]).Value = (decimal)cup.p_FX.Slants[i];
                            ((NumericUpDown)this.Controls.Find("num_Tilts" + (i + 1).ToString(), true)[0]).Value = (decimal)cup.p_FX.Tilts[i];
                        }
                        tbSigma.Text = cup.p_FX.sigma.ToString();
                        tbFactor.Text = cup.p_FX.dMult.ToString();
                        tbOffset.Text = cup.p_FX.dAdd.ToString();
                        //   num_Tilts4.Visible = true;
                        //   num_Slants4.Visible = true;
                        //    label13.Visible = true;
                        lblLSType.Text = "四分区光源";
                        break;
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        for (int i = 0; i < 4; i++)
                        {
                            ((NumericUpDown)this.Controls.Find("num_Slants" + (i + 1).ToString(), true)[0]).Value = (decimal)cup.p_subFX[ind - 1].Slants[i];
                            ((NumericUpDown)this.Controls.Find("num_Tilts" + (i + 1).ToString(), true)[0]).Value = (decimal)cup.p_subFX[ind - 1].Tilts[i];
                        }
                        tbSigma.Text = cup.p_subFX[ind - 1].sigma.ToString();
                        tbFactor.Text = cup.p_subFX[ind - 1].dMult.ToString();
                        tbOffset.Text = cup.p_subFX[ind - 1].dAdd.ToString();
                        //   num_Tilts4.Visible = false;
                        //   num_Slants4.Visible = false;
                        //    label13.Visible = false;
                        lblLSType.Text = "三分区光源";
                        break;
                }
            }
            catch (Exception ex)
            {
                cup.InitSubFXParam();
                MessageBox.Show("光度立体参数设置有误：" + ex.Message.ToString());
            }
        }

        //设置现场参数
        private void SetSceneParam()
        {
            try
            {

                txtLineName.Text = cup.p_LI.strLineName;
                txtLineDesc.Text = cup.p_LI.strLineDesc;
                if (cup.p_LI.LineNum > 0 && cup.p_LI.LineNum <= cmbLine.Items.Count)
                    cmbLine.SelectedIndex = cup.p_LI.LineNum - 1;
                else
                    cmbLine.SelectedIndex = 0;
                if (cup.p_LI.bPreType == 1)
                {
                    chkLineNumPrefix.Checked = true;
                }
                else if (cup.p_LI.bPreType == 2)
                {
                    chkSelfPrefix.Checked = true;
                }
                txtLineSelfPrefix.Text = cup.p_LI.strLinePrefix;

                string strLineScene = "Scene\\" + cup.p_LI.strLineScence;
                if (File.Exists(strLineScene))
                {
                    picScene.Image = Image.FromFile(strLineScene);
                }
                txtMainPrefix.Text = cup.p_LI.strCamPrefix[0];
                txtSub1Prefix.Text = cup.p_LI.strCamPrefix[1];
             //   txtSub2Prefix.Text = cup.p_LI.strCamPrefix[2];
             //   txtSub3Prefix.Text = cup.p_LI.strCamPrefix[3];
             //   txtSub4Prefix.Text = cup.p_LI.strCamPrefix[4];
            }
            catch (Exception ex)
            {
                MessageBox.Show("现场参数设置有误：" + ex.Message.ToString());
            }
        }

        //设置相机分布参数
        private void SetCamDistuParam()
        {
            try
            {
                //相机分配参数 
                cmbCamInd1.SelectedIndex = cup.p_CD.camInd[0];
                cmbCamInd2.SelectedIndex = cup.p_CD.camInd[1];
              //  cmbCamInd3.SelectedIndex = cup.p_CD.camInd[2];
             //   cmbCamInd4.SelectedIndex = cup.p_CD.camInd[3];
             //   cmbCamInd5.SelectedIndex = cup.p_CD.camInd[4];

                switch (cup.p_CD.triggleCam)
                {
                    case 0:
                        rd1.Checked = true;
                        break;
                    case 1:
                        rd2.Checked = true;
                        break;
                    case 2:
                        rd3.Checked = true;
                        break;
                    case 3:
                        rd4.Checked = true;
                        break;
                    case 4:
                        rd5.Checked = true;
                        break;
                }

                if (cup.p_CD.bListProcess != null)
                {
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        ((CheckBox)this.Controls.Find("chkCam" + (i + 1).ToString() + "Used", true)[0]).Checked = cup.p_CD.bListProcess[i];
                    }
                }
                cmbTriggleModel.SelectedIndex = cup.p_CD.triggleMode;
                chbTriggleOpen.Checked = cup.p_CD.bTriggle;
            }
            catch (Exception ex)
            {
                MessageBox.Show("相机分配参数设置有误：" + ex.Message.ToString());
            }
        }

        //设置吹气参数
        private void SetBlowParam()
        {
            try
            {
                if (cup.p_BP.blowType >= 0 || cup.p_BP.blowType < 2)
                    cmbblowType.SelectedIndex = cup.p_BP.blowType;
                else
                    cup.p_BP.blowType = 0;
                chkBlowUsed.Checked = cup.p_BP.bUsed;
                cmbBlowPerTime.SelectedIndex = cup.p_BP.perBlowTime / 200 - 1;
                txtPerTimes.Text = cup.p_BP.intervTimes.ToString();
                switch (cup.p_BP.intervType)
                {
                    case 0:
                        rBPerDay.Checked = true;
                        break;
                    case 1:
                        rBPerHour.Checked = true;
                        break;
                    case 2:
                        rBPerMin.Checked = true;
                        break;
                }

                switch (cup.p_BP.reguType)
                {
                    case 0:
                        rBBefore.Checked = true;
                        break;
                    case 1:
                        rBAfter.Checked = true;
                        break;
                }
                txtPerExeTimes.Text = cup.p_BP.perExeTimes.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("吹气参数设置有误：" + ex.Message.ToString());
            }
        }

        //设置串口参数
        private void SetSerialParam()
        {
            //串口分配
            try
            {
                for (int i = 0; i < strPortName.Length; i++)
                {
                    cbxPortName1.Items.Add(strPortName[i]);
                    cbxPortName2.Items.Add(strPortName[i]);
                    cbxPortName3.Items.Add(strPortName[i]);
                    cbxPortName4.Items.Add(strPortName[i]);
                }
                for (int i = 0; i < 4; i++)
                {
                    if (cup.p_SD.serInd[i] < strPortName.Length)
                        ((ComboBox)this.Controls.Find("cbxPortName" + (i + 1).ToString(), true)[0]).SelectedIndex = cup.p_SD.serInd[i];
                    else if (strPortName.Length > 0)
                        ((ComboBox)this.Controls.Find("cbxPortName" + (i + 1).ToString(), true)[0]).SelectedIndex = 0;
                    ((CheckBox)this.Controls.Find("chkSerUsed" + (i + 1).ToString(), true)[0]).Checked = cup.p_SD.bUsed[i];
                }
                cbxBaudRate1.Text = cup.p_SD.BaudRate.ToString();
                cbxDataBit.Text = cup.p_SD.DataBits.ToString();
                cbxParity1.Text = Enum.GetName(typeof(Parity), cup.p_SD.parity);
                cbxStopBits1.Text = Enum.GetName(typeof(StopBits), cup.p_SD.stopBits);
                txtTimeOut.Text = cup.p_SD.WriteTimeout.ToString();
                if (cup.p_SD.stationCardMap == null || cup.p_SD.stationCardMap.Length < 2)
                {
                    cmbBoardA.SelectedIndex = 0;
                    cmbBoardB.SelectedIndex = 1;
                }
                else
                {
                    int boardA = cup.p_SD.stationCardMap[0];
                    int boardB = cup.p_SD.stationCardMap[1];
                    if (boardA < 0 || boardA > 1) boardA = 0;
                    if (boardB < 0 || boardB > 1) boardB = 1;
                    if (boardA == boardB)
                    {
                        boardA = 0;
                        boardB = 1;
                    }
                    cmbBoardA.SelectedIndex = boardA;
                    cmbBoardB.SelectedIndex = boardB;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("串口参数设置有误：" + ex.Message.ToString());
            }
        }

        //设置路径参数
        private void SetPathParam()
        {
            //路径设置参数
            try
            {
                bLoadVPPChange = true;
                tbVPPPath.Text = cup.p_Paf.strVPPFilePath;
                tbSingleFramePath.Text = cup.p_Paf.strSigSavePath;
                tbContFramePath.Text = cup.p_Paf.strConSavePath;
                tbLogPath.Text = cup.p_Paf.strLogPath;
                tbResultSave.Text = cup.p_Paf.strResSavePath;
                tbPlayback.Text = cup.p_Paf.strPlaybackPath;
                tbContErrPath.Text = cup.p_Paf.strErrPath;
                tbMarkPicPath.Text = cup.p_Paf.MarkPicPath;
                tbManuMarkFilePath.Text = cup.p_Paf.ManuMarkPath;
                tbAutoMarkPath.Text = cup.p_Paf.AutoMarkPath;
                tbExportPath.Text = cup.p_Paf.strExportPath;
                tbFatherPath.Text = Path.GetDirectoryName(cup.p_Paf.strConSavePath);
                tb9PointCalibFile.Text = cup.p_Paf.strExportPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("路径参数设置有误：" + ex.Message.ToString());
            }
        }

        //设置运行参数
        private void SetRunParam()
        {
            //vpp 运行参数
            try
            {
                chkAcqFromCam.Checked = cup.p_VPPRTP.bAcqFromCam;
                chkAcqFromIDB.Checked = cup.p_VPPRTP.bAcqFromIDB;

                chkUseFX.Checked = cup.p_VPPRTP.bUseFX;
                chkRemarkResult.Checked = cup.p_VPPRTP.bRemarkResult;
                chkRemarkArea.Checked = cup.p_VPPRTP.bRemarkArea;
                chkUseWeakCrack.Checked = cup.p_VPPRTP.bUseWeakCrack;
            }
            catch (Exception ex)
            {
                MessageBox.Show("运行参数设置有误：" + ex.Message.ToString());
            }
        }

        private void SetViewListParam()
        {
            // 记录清单设置
            try
            {
                int count = cup.p_SRN.count;
                chkList_SubRecordsShow.Items.Clear();
                for (int i = 0; i < count; i++)
                {
                    chkList_SubRecordsShow.Items.Add(cup.p_SRN.strName[i], cup.p_SRN.bList == null ? true : cup.p_SRN.bList[i]);

                }
                cmbRecordType.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("显示记录参数设置有误：" + ex.Message.ToString());
            }
        }

        //设置IO口参数
        private void SetIOParam()
        {
            try
            {
                cmbAPlane.SelectedIndex= cup.p_IOPP.APlane;
                cmbBPlane.SelectedIndex = cup.p_IOPP.BPlane;
                cmbEndAPlane.SelectedIndex = cup.p_IOPP.EndAPlane;
                cmbEndBPlane.SelectedIndex = cup.p_IOPP.EndBPlane;
                cmbOkOut.SelectedIndex = cup.p_IOPP.OkOut;
                cmbNgOut.SelectedIndex = cup.p_IOPP.NGOut;

                cmbIOTimeDelay.Text = cup.p_IOPP.IOTimeDelay.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("IO端口参数设置有误：" + ex.Message.ToString());
            }

        }

        //设置运行状态参数
        private void SetRunTimeStatusParam()
        {
            try
            {
                chkConnCam.Checked = cup.p_RTSP.bAutoConnCam;
                chkConnSerial.Checked = cup.p_RTSP.bAutoConnSerial;
                chkConnJCZ.Checked = cup.p_RTSP.bAutoConnJCZ;
                chkConnMotionCard.Checked = cup.p_RTSP.bAutoConnMotionCard;
                chkRunVPP.Checked = cup.p_RTSP.bAutoRunVPP;

                chkUpdateImage.Checked = cup.p_RTSP.bUpdateImage;
                chkUpdateRecord.Checked = cup.p_RTSP.bUpdateRecord;
                chkUpdateLog.Checked = cup.p_RTSP.bUpdateLog;
                chkUpdateIO.Checked = cup.p_RTSP.bUpdateIO;
                chkSaveRes.Checked = cup.p_RTSP.bSaveRes;

                cmbRunTimeOut.Text = cup.p_RTSP.runTimeOut.ToString();

            }
            catch (Exception ex)
            {
                MessageBox.Show("运行状态参数设置有误：" + ex.Message.ToString());
            }

        }

        private void setParam()
        { 
            SetSceneParam();            //设置现场参数
            SetCamDistuParam();         //设置相机分布参数
            //SetBlowParam()  ;          //吹气参数
            SetSerialParam();            //设置串口参数
            SetPathParam();             //路径设置参数
            SetViewListParam();
            //光度立体算法参数
            /* iLSInd = 0;
             SetFXParam(iLSInd);
             cmbLightSource.SelectedIndex = 0;*/

            // SetCMDiagParam();

             SetDLParam();
            SetIOParam();
            SetRunTimeStatusParam();
        }

        private void cmbCMIndex_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                //保存上次记录
                GetCMDiagParam();
                //更新当前选择侧面的记录
               
                iLastCMIndex = cmbCMIndex.SelectedIndex;

                bMaskChanged = true;
                SetCMDiagParam();
                bMaskChanged = false;
            }
            catch
            {

            }

        }

        private void btSave_Click(object sender, EventArgs e)
        {
            SaveRecordList(cup);

        }

        private void btLoad_Click(object sender, EventArgs e)
        {
            ReadRecordList(ref cup);

            cmbRecordType.SelectedIndex = 0;
            lstBox_GUIRecord.Items.Clear();
            if (cup.p_SRN.strDefaultGUIName != null)
            {
                for (int i = 0; i < maxCameraNum; i++)
                {
                    if (cup.p_SRN.strDefaultGUIName[i] != null)
                        lstBox_GUIRecord.Items.Add(cup.p_SRN.strDefaultGUIName[i]);
                }
            }
        }

        public bool SaveRecordList(CUserParam cup)
        {
            try
            {

                FileStream fs = new FileStream("UserData\\serRecordList.dat", FileMode.Create);

                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, cup.p_SRN);
                fs.Close();
                //  operLog.iEventStatus = 0;
            }
            catch
            {
                return false;
            }
            // AddLog();
            return true;
        }

        public bool ReadRecordList(ref CUserParam cup)
        {

            FileStream fs;
            try
            {
                //      SaveUserParam(cup);
                //   operLog.strOperEvent = "读取配置参数";
                if (File.Exists("UserData\\serRecordList.dat"))
                {
                    fs = new FileStream("UserData\\serRecordList.dat", FileMode.Open);
                    BinaryFormatter bf = new BinaryFormatter();
                    cup.p_SRN =(SubRecordsName) bf.Deserialize(fs)  ;
                    fs.Close();
                    chkList_SubRecordsShow.Items.Clear();
                    for (int i = 0; i < cup.p_SRN.count; i++)
                    {
                        chkList_SubRecordsShow.Items.Add(cup.p_SRN.strName[i], cup.p_SRN.bList == null ? true : cup.p_SRN.bList[i]);

                    }
                    //  SaveUserParam(cup);
                }
                else
                {
                  //  SaveUserParam(cup);
                    return false;
                }
            }
            catch
            {
                // if(f)
                // fs.Close();
                //   cup = new CUserParam();
                SaveRecordList(cup);
                return false;
            }
            return true;
        }

        private void VPPParamChanged(object sender, EventArgs e)
        {
            if (chkAcqFromCam.Checked)
            {
                chkAcqFromIDB.Enabled = false;
            }
            else
            {
                chkAcqFromIDB.Enabled = true;
            }
            if (!bMaskChanged)
                bVPPParamChanged = true;
        }

        private void VPPThresholdChanged(object sender, EventArgs e)
        {
            if (!bMaskChanged)
                bVPPParamChanged = true;
        }

        private void UsedCamChange(object sender, EventArgs e)
        {
            if (!bMaskChanged)
                bVPPParamChanged = true;
        }



        //深度学习参数相关
        public static List<FileInfo> getFile(string path, string extName, List<FileInfo> lst)
        {
            try
            {
                DirectoryInfo fdir = new DirectoryInfo(path);
                FileInfo[] file = fdir.GetFiles();
                //FileInfo[] file = Directory.GetFiles(path);
                if (file.Length != 0)
                {
                    foreach (FileInfo f in file)
                    {
                        if (extName.ToLower().IndexOf(f.Extension.ToLower()) >= 0)
                        {
                            lst.Add(f);
                        }
                    }
                }
                return lst;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        
        [DllImport("model_infer.dll", EntryPoint = "DestructModel")]  // Segmentation, detection, identification model destruction method
        public static extern void DestructModel();
        [DllImport("model_infer.dll", EntryPoint = "InitModel")] // Model unified initialization method: need yml、pdmodel、pdiparams
        public static extern void InitModel(string model_type, string model_filename, string params_filename, string cfg_file, bool use_gpu, int gpu_id, ref byte paddlex_model_type);


        int has_model_init = 0; 
        string model_type = "paddlex";
        bool use_gpu = true;
        int gpu_id = 0;
        bool paddlex_doing = false;
        int comboBox_clicked = 0;
        int is_infer = 0;
        int last_Model_index = 0;
        int last_Env_Type = 0;
        int last_Threshold_inddex = 5;
        int last_gpu_id = 0;
        int gpu_id_done = 0;
        byte[] paddlex_model_type = new byte[10];  // det/seg/clas + \0

        double det_threshold = 0.5;
        private void btModel_Click(object sender, EventArgs e)
        {
            string dir_path = null;
            int dir_load_flag = 0;
            string model_filename = null; // *.pdmodel
            string params_filename = null;  // *.pdiparams
            string cfg_file = null;  // *.yml

            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            folderBrowserDialog1.SelectedPath = tbModelPath.Text;
            folderBrowserDialog1.Description = "请选择模型文件夹";
            DialogResult folder = folderBrowserDialog1.ShowDialog();
            if (folder == DialogResult.OK || folder == DialogResult.Yes)
            {
                dir_path = folderBrowserDialog1.SelectedPath;
                if (string.IsNullOrEmpty(dir_path))
                {
                    MessageBox.Show("请选择模型路径/模型路径为空!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dir_load_flag = 0;
                }
                else
                {
                    dir_load_flag = 1;
                }
            }

            if (dir_load_flag == 1)
            {
                tbModelPath.Text = dir_path;
                List<FileInfo> model_lst = new List<FileInfo>();
                List<FileInfo> params_lst = new List<FileInfo>();
                List<FileInfo> cfg_lst = new List<FileInfo>();
                model_lst = getFile(dir_path, ".pdmodel", model_lst);
                params_lst = getFile(dir_path, ".pdiparams", params_lst);
                cfg_lst = getFile(dir_path, ".yml", cfg_lst);
                if (cfg_lst.Count == 0)
                    cfg_lst = getFile(dir_path, ".yaml", cfg_lst);

                if (model_lst.Count != 1 || params_lst.Count != 1)
                {
                    MessageBox.Show("模型文件加载失败!\n请注意模型文件夹下应包含以下文件各一个:\n*.pdmodel, *.pdiparams", "提示");
                    return;
                }

                model_filename = model_lst[0].FullName;
                params_filename = params_lst[0].FullName;

                if (cfg_lst.Count == 0)
                {
                    MessageBox.Show("模型文件加载失败!\n请注意模型文件夹下应包含以下文件:\nmodel.yml/model.yaml", "提示");
                    return;
                }
                else if (cfg_lst.Count > 2) // Excessive YML， > 2 
                {
                    MessageBox.Show("模型文件加载失败!\n请注意模型文件夹下应至多包含以下文件(yml文件个数不得超过2):\nmodel.yml/model.yaml，pipeline.yml/pipeline.yaml", "提示");
                    return;
                }
                else if (cfg_lst.Count == 2) // Processing for the case of multiple YML files
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (cfg_lst[i].Name == "model.yml" || cfg_lst[i].Name == "model.yaml")
                        {
                            cfg_file = cfg_lst[i].FullName;
                            break;
                        }
                    }
                }
                else if (cfg_lst.Count == 1)
                {
                    cfg_file = cfg_lst[0].FullName;
                }

                int raise_ex_flag = 0;
                int is_Mask = 0;
                if (has_model_init == 1)
                {
                    DestructModel();

                    try
                    {
                        if (paddlex_doing == true) model_type = "paddlex";
                        if (model_type == "mask")
                        {
                            model_type = "paddlex";
                            is_Mask = 1;
                        }
                        InitModel(model_type, model_filename, params_filename, cfg_file, use_gpu, gpu_id, ref paddlex_model_type[0]);

                        if (is_Mask == 1)
                        {
                            model_type = "mask";
                            is_Mask = 0;
                        }
                        if (model_type == "paddlex")
                        {
                            paddlex_doing = true;
                            model_type = System.Text.Encoding.UTF8.GetString(paddlex_model_type).Split('\0')[0];
                        }
                    }
                    catch 
                    {
                        raise_ex_flag = 1;
                        MessageBox.Show("1.请确定文件中包含有效的模型文件(*.pdmodel, *.pdiparams, *.yml)!\n2.请检查模型文件与模型类型是否一致!\n3.其它原因:GPU号有误，yml中预处理有误...", "模型初始化失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {

                    try
                    {

                        if (paddlex_doing == true) model_type = "paddlex";
                        if (model_type == "mask")
                        {
                            model_type = "paddlex";
                            is_Mask = 1;
                        }
                        InitModel(model_type, model_filename, params_filename, cfg_file, use_gpu, gpu_id, ref paddlex_model_type[0]);

                        if (is_Mask == 1)
                        {
                            model_type = "mask";
                            is_Mask = 0;
                        }
                        if (model_type == "paddlex")
                        {
                            paddlex_doing = true;

                            model_type = System.Text.Encoding.UTF8.GetString(paddlex_model_type).Split('\0')[0];
                        }
                    }
                    catch 
                    {
                        raise_ex_flag = 1;
                        MessageBox.Show("1.请确定文件中包含有效的模型文件(*.pdmodel, *.pdiparams, *.yml)!\n2.请检查模型文件与模型类型是否一致!\n3.其它原因:GPU号有误，yml中预处理有误...", "模型初始化失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                if (raise_ex_flag == 0)
                {
                    has_model_init = 1;
                    if (use_gpu)
                    {
                        if (is_Mask == 1)
                        {
                            model_type = "mask";
                            is_Mask = 0;
                        }
                        MessageBox.Show($"模型文件已加载到GPU:{gpu_id}!\n(模型类型为: {model_type.Split('\0')[0]})", "提示");
                    }
                    else
                    {
                        MessageBox.Show($"模型类型为: {model_type.Split('\0')[0]}", "提示");
                    }
                  //  button1.Text = "模型已初始化";
                }
            }
        }

        private void cmb_ModelType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (is_infer == 1 && comboBox_clicked == 0)
            {
                MessageBox.Show("正在推理中，请推理完成后再选择模型类型重新初始化!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBox_clicked = 1;
                cmb_ModelType.SelectedIndex = last_Model_index;
                return;
            }

            if (has_model_init == 1 && comboBox_clicked == 0)
            {
                MessageBox.Show("模型已初始化，请销毁模型后再进行模型类型选择!\n(det,seg,clas)", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBox_clicked = 1;
                cmb_ModelType.SelectedIndex = last_Model_index;
                return;
            }

            // There are three types
            if (cmb_ModelType.SelectedItem.ToString() == "det")  // Load detection model
            {
                model_type = cmb_ModelType.SelectedItem.ToString();
                paddlex_doing = false;  // Enter non-PADDLEX mode -- detection
            }
            else if (cmb_ModelType.SelectedItem.ToString() == "seg") // Load segmentation model
            {
                model_type = cmb_ModelType.SelectedItem.ToString();
                paddlex_doing = false;
            }
            else if (cmb_ModelType.SelectedItem.ToString() == "clas") // Load recognition model
            {
                model_type = cmb_ModelType.SelectedItem.ToString();
                paddlex_doing = false;
            }
            else if (cmb_ModelType.SelectedItem.ToString() == "mask") // Loading instance split Maskrcnn model
            {
                model_type = cmb_ModelType.SelectedItem.ToString();
                paddlex_doing = false;
            }
            else if (cmb_ModelType.SelectedItem.ToString() == "paddlex") // Load paddlex model
            {
                model_type = cmb_ModelType.SelectedItem.ToString();
                // Repeat Paddlex, do not modify the status
            }
            last_Model_index = cmb_ModelType.SelectedIndex;


            comboBox_clicked = 0;
        }

        private void cmb_EvnType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (is_infer == 1 && comboBox_clicked == 0)
            {
                MessageBox.Show("正在推理中，请推理完成后再选择模型类型重新初始化!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBox_clicked = 1;
                cmb_EvnType.SelectedIndex = last_Env_Type;
                return;
            }

            if (has_model_init == 1 && comboBox_clicked == 0)
            {
                MessageBox.Show("模型已初始化，请销毁模型后再进行模型类型选择!\n(det,seg,clas)", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBox_clicked = 1;
                cmb_EvnType.SelectedIndex = last_Env_Type;
                return;
            }

            // There are three types
            if (cmb_EvnType.SelectedItem.ToString() == "det")  // Load detection model
            {
                model_type = cmb_EvnType.SelectedItem.ToString();
                paddlex_doing = false;  // Enter non-PADDLEX mode -- detection
            }
            else if (cmb_EvnType.SelectedItem.ToString() == "seg") // Load segmentation model
            {
                model_type = cmb_EvnType.SelectedItem.ToString();
                paddlex_doing = false;
            }
            else if (cmb_EvnType.SelectedItem.ToString() == "clas") // Load recognition model
            {
                model_type = cmb_EvnType.SelectedItem.ToString();
                paddlex_doing = false;
            }
            else if (cmb_EvnType.SelectedItem.ToString() == "mask") // Loading instance split Maskrcnn model
            {
                model_type = cmb_EvnType.SelectedItem.ToString();
                paddlex_doing = false;
            }
            else if (cmb_EvnType.SelectedItem.ToString() == "paddlex") // Load paddlex model
            {
                model_type = cmb_EvnType.SelectedItem.ToString();
                // Repeat Paddlex, do not modify the status
            }
            last_Env_Type = cmb_EvnType.SelectedIndex;


            comboBox_clicked = 0;
        }

        private void cmb_Threshold_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (is_infer == 1 && comboBox_clicked == 0)
            {
                MessageBox.Show("正在推理中，检测阈值修改将在本次模型推理完成后生效!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);

                comboBox_clicked = 1;
                cmb_Threshold.SelectedIndex = last_Threshold_inddex;
                return;
            }
            if (cmb_Threshold.SelectedIndex > -1)
            {
                det_threshold = float.Parse(cmb_Threshold.SelectedItem.ToString());
                last_Threshold_inddex = cmb_Threshold.SelectedIndex;

            }
            else
            {
                det_threshold = float.Parse(cmb_Threshold.Text.ToString());
                last_Threshold_inddex = -1;
            }


            comboBox_clicked = 0;
        }

        private void txtGPUID_TextChanged(object sender, EventArgs e)
        {
            if (is_infer == 1 && gpu_id_done == 0)
            {
                MessageBox.Show("正在推理中，请推理完成后再选择指定GPU重新初始化!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                gpu_id_done = 1;
                gpu_id = last_gpu_id;
                tbModelPath.Text = $"{gpu_id}";
                return;
            }

            if (has_model_init == 1 && gpu_id_done == 0)
            {
                MessageBox.Show("模型已初始化，请销毁模型后再进行GPU指定!\n(GPU:x)", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                gpu_id_done = 1;
                gpu_id = last_gpu_id;
                tbModelPath.Text = $"{gpu_id}";
                return;
            }

            if (gpu_id_done == 0)
            {
                string gpu_id_str = tbModelPath.Text.ToString();
                if (gpu_id_str.Length != 0)
                {
                    last_gpu_id = gpu_id;
               /*     try
                    {
                        gpu_id = Int32.Parse(gpu_id_str);
                    }
                    catch
                    {
                        gpu_id = last_gpu_id;
                        tbModelPath.Text = $"{gpu_id}";
                        MessageBox.Show("GPU_ID只能输入数字!");
                    }*/
                }
            }

            gpu_id_done = 0;
        }

      
        private void btRecovery_Click(object sender, EventArgs e)
        {
            CUserParam cupTmp = new CUserParam();

            cup.p_CP.fSureCrackParam = (float[])cupTmp.p_CP.fSureCrackParam.Clone();
            cup.p_CP.fCrackParam = (float[])cupTmp.p_CP.fCrackParam.Clone();

            SetHightLevelCrackParam();
            /*cmbCrackGroupParamGrey1.SelectedIndex = iLstGrey1;
            cmbCrackGroupParamFX1.SelectedIndex = iLstFX1;
            cmbCrackGroupParamFX2.SelectedIndex = iLstFX2;
            cmbCrackGroupParamGrey2.SelectedIndex = iLstGrey2;*/
        }


        int iLstFX1 = -1, iLstFX2 = -1, iLstGrey1 = -1, iLstGrey2 = -1;
        private void cmbCrackGroupParamFX1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (iLstFX1 >= 0)
                cup.p_CP.fCrackParam[iLstFX1] = float.Parse(txtCrackGroupParamFX1.Text);

            iLstFX1 = cmbCrackGroupParamFX1.SelectedIndex;
            txtCrackGroupParamFX1.Text = cup.p_CP.fCrackParam[iLstFX1].ToString();
        }

        private void btUpdateParamFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string strParamFile = openFileDialog.FileName;

                PathAndFile paf = cup.p_Paf;
                try
                {
                    bool bStatus = fo.ReadUserParam(strParamFile,ref cup);
                    cup.p_Paf = paf;
                    if(!bStatus)
                        fo.ReadUserParam( ref cup); 
                }
                catch
                {

                }
                setParam();
                bVPPParamChanged = true;
                bMaskChanged = true;
            }
        }
        private void AutoSetPath(string strFatherPath)
        {
            //   path.SelectedPath = tbVPPPath.Text;

          
             tbSingleFramePath.Text = strFatherPath + "\\SingleSave"; 
             tbContFramePath.Text = strFatherPath + "\\ContSave";
             tbLogPath.Text = strFatherPath + "\\Log"; ;
             tbResultSave.Text = strFatherPath + "\\calibImage"; 
             tbContErrPath.Text = strFatherPath + "\\Err";
             tbExportPath.Text = strFatherPath + "\\export";
          //  tb9PointCalibFile.Text= strFatherPath + "\\export";

        }
        private void chkAutoSetPath_CheckedChanged(object sender, EventArgs e)
        {
            
            if(chkAutoSetPath.Checked )
            {
                groupPath.Enabled = false;
                AutoSetPath(tbFatherPath.Text.Trim());
            }
            else
            {
                groupPath.Enabled = true;
            }
        }

        private void btCancle_Click(object sender, EventArgs e)
        {
            if (iType == 1)
                this.Close();
        }

        private void chkLineNumPrefix_CheckedChanged(object sender, EventArgs e)
        {
           // chkLineNumPrefix.Checked = !chkLineNumPrefix.Checked;
           if(chkLineNumPrefix.Checked)
            {
                chkSelfPrefix.Checked = false;
                txtLineSelfPrefix.Enabled = false;
                txtLineSelfPrefix.Text = cmbLine.Text;
                cup.p_LI.bPreType = 1;
            }
           else if(chkSelfPrefix.Checked)
            {
                cup.p_LI.bPreType = 2;
            }
           else
            {
                cup.p_LI.bPreType = 0;
            }
        }

        private void chkSelfPrefix_CheckedChanged(object sender, EventArgs e)
        {
            if(chkSelfPrefix.Checked)
            {
                chkLineNumPrefix.Checked = false;
                txtLineSelfPrefix.Enabled = true;
                cup.p_LI.bPreType = 2;
            }
            else if(chkLineNumPrefix.Checked)
            {
                cup.p_LI.bPreType = 1;
            }
            else
            {
                cup.p_LI.bPreType = 0;
            }
        }

        private void btLineScene_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|All Files (*.*)|*.*";
            openFileDialog.Title = "Select an Image";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // 在这里处理选中的文件  
                 string selectedFilePath = openFileDialog.FileName;
                if (picScene.Image != null)
                {
                    picScene.Image.Dispose();
                    picScene.Image = null;
                }
                File.Copy(selectedFilePath, "Scene\\" + cup.p_LI.strLineScence,true);

                // 例如，将图片显示在PictureBox中  
                picScene.Image = Image.FromFile(selectedFilePath);
            }


          /*  OpenFileDialog openFileDialog = new OpenFileDialog(); 
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                strVPPFileName = openFileDialog.FileName;
                tbVPPPath.Text = strVPPFileName;
                bVPPChange = true;
            }*/

        }

        private void cmbLine_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(chkLineNumPrefix.Checked)
            {
                txtLineSelfPrefix.Text = cmbLine.Text;
            }
        }

        private void cmbblowType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbblowType.SelectedIndex == 0)
            {
                groupBox10.Enabled = false;
                groupBox9.Enabled = true;
            }                
            else if (cmbblowType.SelectedIndex == 1)
            {
                groupBox9.Enabled = false;
                groupBox10.Enabled = true;
            }
                
        }

        private void cbxPortName1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void chkCurSavePath_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCurSavePath.Checked)
                tbPlayback.Text = cup.p_Paf.strConSavePath + "\\" + DateTime.Now.ToString("yyyyMMdd");
            else
                tbPlayback.Text = cup.p_Paf.strPlaybackPath;
        }

        private void bt9PointCalibFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //  openFileDialog.Filter=
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
               string  strCalibFileName = openFileDialog.FileName;
                tb9PointCalibFile.Text = strCalibFileName;
                cup.p_Paf.strExportPath = strCalibFileName;
               // bVPPChange = true;
            }
        }

        private void tbVPPPath_TextChanged(object sender, EventArgs e)
        {
            if (!bLoadVPPChange)
            {
                bVPPChange = true;                
            }
            bLoadVPPChange = false;
        }
        TextBox serialTextBox;
        private void cmbCamInd_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 1. 获取触发事件的 ComboBox
            ComboBox cmb = sender as ComboBox;
            if (cmb == null) return;

            // 2. 从控件名称提取工位编号（如 "cmbCamInd1" -> 1）
            string name = cmb.Name;
            if (!name.StartsWith("cmbCamInd")) return;
            string numStr = name.Substring("cmbCamInd".Length);
            if (!int.TryParse(numStr, out int jobNumber)) return;
            int jobIndex = jobNumber - 1; // 转为0基索引

            // 3. 获取选中的物理相机索引（下拉框的 SelectedIndex）
            int camIndex = cmb.SelectedIndex;

            // 4. 先查找对应的序列号文本框（只调用一次，避免重复声明相同变量名）
            TextBox serialTextBox = FindTextBoxByJob(jobIndex);

            if (camIndex < 0 || cup.p_Cam == null || camIndex >= cup.p_Cam.Length)
            {
                // 未选择或索引无效，清空对应文本框
                if (serialTextBox != null) serialTextBox.Text = "";
                return;
            }

            // 5. 更新工位映射
            if (cup.p_CD.camInd != null && jobIndex < cup.p_CD.camInd.Length)
            {
                cup.p_CD.camInd[jobIndex] = camIndex;

            }

            // 6. 更新序列号文本框
            if (serialTextBox != null )
            {
                int ind = 0;
                try { ind = cup.p_Cam[camIndex].strCamName.IndexOf("MV-"); }
                catch { }
                 
                serialTextBox.Text = cup.p_Cam[camIndex].strCamName.Substring(ind); // strCamName 存储序列号
            }

        }
        /// <summary>
        /// 根据工位索引查找对应的序列号文本框
        /// </summary>
        private TextBox FindTextBoxByJob(int jobIndex)
        {
            string txtName = "txtJobCamSerial" + (jobIndex + 1).ToString();
            // 注意：文本框可能直接位于 TabPage 或 GroupBox 内，使用 Controls.Find 递归查找
            var controls = this.Controls.Find(txtName, true);
            if (controls.Length > 0 && controls[0] is TextBox)
                return controls[0] as TextBox;
            return null;
        }
        private void cmbCrackGroupParamGrey1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (iLstGrey1 >= 0)
                cup.p_CP.fCrackParam[iLstGrey1+10] = float.Parse(txtCrackGroupParamGrey1.Text);

            iLstGrey1 = cmbCrackGroupParamGrey1.SelectedIndex;
            txtCrackGroupParamGrey1.Text = cup.p_CP.fCrackParam[iLstGrey1+10].ToString();
        }

        private void cmbCrackGroupParamFX2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (iLstFX2 >= 0)
                cup.p_CP.fCrackParam[iLstFX2+20] = float.Parse(txtCrackGroupParamFX2.Text);

            iLstFX2 = cmbCrackGroupParamFX2.SelectedIndex;
            txtCrackGroupParamFX2.Text = cup.p_CP.fCrackParam[iLstFX2+20].ToString();
        }

        private void cmbCrackGroupParamGrey2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (iLstGrey2 >= 0)
                cup.p_CP.fCrackParam[iLstGrey2+40] = float.Parse(txtCrackGroupParamGrey2.Text);

            iLstGrey2 = cmbCrackGroupParamGrey2.SelectedIndex;
            txtCrackGroupParamGrey2.Text = cup.p_CP.fCrackParam[iLstGrey2+40].ToString();
        }
    }
}
