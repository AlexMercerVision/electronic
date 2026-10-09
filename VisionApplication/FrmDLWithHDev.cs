using Cognex.VisionPro;
using CommonCls;
using HalconDotNet;
using MvCamCtrl.NET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace xbDefectDect
{
    public partial class FrmDL_Halcon : Form
    {
        // ======================== DLL 导入 ========================
        [DllImport("model_infer.dll", EntryPoint = "InitModel")]
        public static extern void InitModel(string model_type, string model_filename, string params_filename, string cfg_file, bool use_gpu, int gpu_id, ref byte paddlex_model_type);

        [DllImport("model_infer.dll", EntryPoint = "Det_ModelPredict")]
        public static extern int Det_ModelPredict(byte[] img, int W, int H, int C, IntPtr output, int[] BoxesNum, ref byte label);

        [DllImport("model_infer.dll", EntryPoint = "DestructModel")]
        public static extern void DestructModel();

        // ======================== 数据源枚举 ========================
        private enum DataSource { File, Camera }
        private DataSource _currentDataSource = DataSource.File;

        // ======================== 原有字段 ========================
        private List<string> imgfiles = new List<string>();
        private int iInferIndex = -1;
        private int iLastIndex = 0;
        private static int is_infer = 0;
        private static int isBreakInfer = 0;
        private static int isPauseInfer = 0;
        private static int infer_many_img_flag = 0;
        private DLResult dlRes = new DLResult();
        private CDL cdl = new CDL();
        private CUserParam cup = new CUserParam();
        private FileOper fo = new FileOper();
        private bool bFromMainWind = true;
        private DLParam dlParam = new DLParam();

        // ======================== 模型相关 ========================
        private bool use_gpu = false;
        private int gpu_id = 0;
        private float det_threshold = 0.5F;
        private string model_type = "det";
        private List<string> classNames = new List<string>();
        public int has_model_init = 0;

        // ======================== 保存选项 ========================
        private string strSavePath = "";
        private bool bSaveXml = false;
        private bool bSavePosiXml = false;

        // ======================== 相机相关 ========================
        private MyCamera _selectedCamera = null;
        private MyCamera.MV_CC_DEVICE_INFO_LIST _deviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
        private IntPtr _bufferPtr = IntPtr.Zero;
        private uint _payloadSize = 0;
        private int _selectedCamIndex = -1;
        private bool _isGrabbing = false;
        private Thread _grabThread = null;
        private volatile bool _stopGrab = false;
        private volatile bool _pauseGrab = false;

        // ======================== Halcon 显示控件 ========================
        private HWindowControl hWindowSource;
        private HWindowControl hWindowTarget;
        private Dictionary<HWindowControl, HImage> _displayedImage = new Dictionary<HWindowControl, HImage>();

        // ======================== 构造函数 ========================
        public FrmDL_Halcon()
        {
            InitializeComponent();

            // 手动创建 Halcon 显示控件
            hWindowSource = new HalconDotNet.HWindowControl();
            hWindowTarget = new HalconDotNet.HWindowControl();

            // 设置位置、大小（根据您的布局调整）
            int width = this.Width;
            int height = this.Height;
            int xzHeight = height - 100;
            hWindowSource.Width = (width - groupBox1.Width - 40) / 2;
            hWindowTarget.Width = hWindowSource.Width;
            hWindowSource.Height = xzHeight;
            hWindowTarget.Height = xzHeight;
            hWindowSource.Left = 0; hWindowSource.Top = 30;
            hWindowTarget.Left = hWindowSource.Right + 10; hWindowTarget.Top = hWindowSource.Top;

            // 将控件添加到窗体
            this.Controls.Add(hWindowSource);
            this.Controls.Add(hWindowTarget);

            // 移除原有的 VisionPro 控件（如果存在）
            // 例如：this.Controls.Remove(cogRDSource);
            //       this.Controls.Remove(cogRDTarget);

            // 其余初始化代码...
            bFromMainWind = false;
            fo.ReadUserParam(ref cup);
            dlParam = cup.p_DL;
        }

        public FrmDL_Halcon(List<string> strCurFile)
        {
            InitializeComponent();
            if (hWindowSource == null)
            {
                hWindowSource = new HWindowControl();
                hWindowTarget = new HWindowControl();
                this.Controls.Add(hWindowSource);
                this.Controls.Add(hWindowTarget);
            }
            imgfiles = new List<string>(strCurFile);
            if (imgfiles.Count > 0)
            {
                HImage img = ReadHalconImage(imgfiles[0]);
                if (img != null)
                {
                    hWindowSource.HalconWindow.DispImage(img);
                    _displayedImage[hWindowSource] = img;
                    hWindowTarget.HalconWindow.DispImage(img);
                    _displayedImage[hWindowTarget] = img;
                }
            }
        }

        // ======================== Form Load ========================
        private void Form1_Load(object sender, EventArgs e)
        {
            if (cup.p_DL.bUseGPU) cmb_Env.SelectedIndex = 1; else cmb_Env.SelectedIndex = 0;
            cmb_ModelType.Text = cup.p_DL.modelType;
            cmb_Thre.SelectedIndex = 5;
            numericUpDown1.Value = 20;
            StripStatus_InferSpeed.Text = "0.00";
            txtGPUID.Text = "0";
            strSavePath = cup.p_Paf.AutoMarkPath;
            txtPath.Text = strSavePath;

            if (!bFromMainWind)
            {
                InitDL(cup.p_DL);
                if (has_model_init != 1) MessageBox.Show("加载深度推理模型失败");
            }

            EnumCameras();
            UpdateCameraMenuItems();
            if (_deviceList.nDeviceNum > 0) _selectedCamIndex = 0;
            SetDataSource(DataSource.File);
        }

        // ======================== 窗体关闭 ========================
        private void Form1_FromClosing(object sender, EventArgs e)
        {
            StopGrabThread();
            if (is_infer == 1) isBreakInfer = 1;
            Thread.Sleep(200);
            if (has_model_init == 1 && !bFromMainWind) DestructModel();
            CloseCamera();
        }

        // ======================== Halcon 图像读取 ========================
        private HImage ReadHalconImage(string filePath)
        {
            try
            {
                HImage img = new HImage();
                img.ReadImage(filePath);
                return img;
            }
            catch { return null; }
        }

        // ======================== 显示图像 ========================
        private void DisplayImage(HWindowControl hwin, HImage image)
        {
            if (image != null)
            {
                hwin.HalconWindow.DispImage(image);
                _displayedImage[hwin] = image;
            }
        }

        // ======================== 绘制检测结果 ========================
        /*private void DrawHalconResults(HWindowControl hwin, DLResult result)
        {
            HWindow window = hwin.HalconWindow;
            window.ClearWindow();

            if (_displayedImage.TryGetValue(hwin, out HImage img) && img != null)
                window.DispImage(img);

            string[] colors = { "red", "green", "blue", "yellow", "magenta", "orange" };

            for (int i = 0; i < result.AL_Rect.Count; i++)
            {
                int typeId = result.AL_Type[i];
                string color = (typeId > 0 && typeId <= colors.Length) ? colors[typeId - 1] : "black";
                float score = result.AL_score[i];
                CogRectangleAffine rect = result.AL_Rect[i];

                HTuple row = rect.CenterY;
                HTuple col = rect.CenterX;
                HTuple phi = rect.Rotation;
                HTuple len1 = rect.SideXLength / 2.0;
                HTuple len2 = rect.SideYLength / 2.0;

                window.SetColor(color);
                window.DispRectangle2(row, col, phi, len1, len2);

                string className = (typeId > 0 && typeId <= classNames.Count) ? classNames[typeId - 1] : "unknown";
                string labelText = $"{className}:{score:F2}";
                HTuple textRow = rect.CornerXY - 15;
                HTuple textCol = rect.CornerYX + 5;
                window.SetTposition(textRow, textCol);
                window.WriteString(labelText);
            }
        }*/
        private void DrawHalconResults(HWindowControl hwin, DLResult result)
        {
            HWindow window = hwin.HalconWindow;
            window.ClearWindow();

            if (_displayedImage.TryGetValue(hwin, out HImage img) && img != null)
                window.DispImage(img);

            string[] colors = { "red", "green", "blue", "yellow", "magenta", "orange" };

            for (int i = 0; i < result.AL_Rect.Count; i++)
            {
                int typeId = result.AL_Type[i];
                string color = (typeId > 0 && typeId <= colors.Length) ? colors[typeId - 1] : "black";
                float score = result.AL_score[i];
                CogRectangleAffine rect = result.AL_Rect[i];

                HTuple row = rect.CenterY;
                HTuple col = rect.CenterX;
                HTuple phi = rect.Rotation;
                HTuple len1 = rect.SideXLength / 2.0;
                HTuple len2 = rect.SideYLength / 2.0;

                window.SetColor(color);
                window.DispRectangle2(row, col, phi, len1, len2);

                string className = (typeId > 0 && typeId <= classNames.Count) ? classNames[typeId - 1] : "unknown";
                string labelText = $"{className}:{score:F2}";
                window.SetColor(color);
                window.SetTposition((int)Math.Round(rect.CornerXY - 15), (int)Math.Round(rect.CornerYX + 5));
                window.WriteString(labelText);
            }
        }
        // ======================== 统一推理（修正版 - 手动交错） ========================
        private DLResult InferFromHalconImage(HImage image)
        {
            DLResult result = new DLResult();
            result.AL_Rect = new List<CogRectangleAffine>();
            result.AL_score = new List<float>();
            result.AL_Type = new List<int>();
            result.Al_bDiff = new List<bool>();

            if (image == null || has_model_init != 1) return result;

            HTuple width, height;
            image.GetImageSize(out width, out height);
            int W = width.I, H = height.I;
            int dataLen = W * H;

            byte[] bgrData = null;
            try
            {
                int channels = image.CountChannels();
                if (channels == 1)
                {
                    // 灰度图 -> 三通道 BGR（复制三份）
                    HTuple ptr, type, w, h;
                    HOperatorSet.GetImagePointer1(image, out ptr, out type, out w, out h);
                    byte[] grayData = new byte[dataLen];
                    Marshal.Copy(ptr, grayData, 0, dataLen);
                    bgrData = new byte[dataLen * 3];
                    for (int i = 0; i < dataLen; i++)
                    {
                        bgrData[i * 3] = grayData[i];     // B
                        bgrData[i * 3 + 1] = grayData[i]; // G
                        bgrData[i * 3 + 2] = grayData[i]; // R
                    }
                }
                else if (channels == 3)
                {
                    // 使用完整重载，获取 R,G,B 平面指针及宽高
                    HTuple ptrR, ptrG, ptrB, type, w, h;
                    HOperatorSet.GetImagePointer3(image, out ptrR, out ptrG, out ptrB, out type, out w, out h);
                    // 验证宽高一致
                    if (w.I != W || h.I != H)
                        throw new Exception("图像尺寸不一致");

                    // 每个平面数据长度 = W * H
                    byte[] rData = new byte[dataLen];
                    byte[] gData = new byte[dataLen];
                    byte[] bData = new byte[dataLen];
                    Marshal.Copy(ptrR, rData, 0, dataLen);
                    Marshal.Copy(ptrG, gData, 0, dataLen);
                    Marshal.Copy(ptrB, bData, 0, dataLen);

                    // 交错为 BGR 格式（B, G, R 顺序）
                    bgrData = new byte[dataLen * 3];
                    for (int i = 0; i < dataLen; i++)
                    {
                        bgrData[i * 3] = bData[i];     // B
                        bgrData[i * 3 + 1] = gData[i]; // G
                        bgrData[i * 3 + 2] = rData[i]; // R
                    }
                }
                else
                {
                    MessageBox.Show("不支持的图像通道数: " + channels);
                    return result;
                }
            }
            catch (HalconException ex)
            {
                MessageBox.Show($"Halcon 转换失败: {ex.Message}");
                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"转换失败: {ex.Message}");
                return result;
            }

            // 推理（不变）
            float[] resultlist = new float[600];
            IntPtr results = FloatToIntptr(resultlist);
            int[] boxesInfo = new int[1];
            byte[] labellist = new byte[1000];

            try
            {
                Det_ModelPredict(bgrData, W, H, 3, results, boxesInfo, ref labellist[0]);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"推理失败: {ex.Message}");
                return result;
            }

            string strGet = System.Text.Encoding.Default.GetString(labellist, 0, labellist.Length);
            string[] predict_Label_List = strGet.Split(' ');

            for (int i = 0; i < boxesInfo[0]; i++)
            {
                int labelindex = Convert.ToInt32(resultlist[i * 6 + 0]);
                float score = resultlist[i * 6 + 1];
                float left = resultlist[i * 6 + 2];
                float top = resultlist[i * 6 + 3];
                float right = resultlist[i * 6 + 4];
                float down = resultlist[i * 6 + 5];

                if (score > det_threshold && labelindex >= 0 && labelindex < classNames.Count)
                {
                    CogRectangleAffine cogRect = new CogRectangleAffine();
                    cogRect.CenterX = left + right / 2;
                    cogRect.CenterY = top + down / 2;
                    cogRect.SideXLength = right;
                    cogRect.SideYLength = down;
                    cogRect.Rotation = 0;
                    result.AL_Rect.Add(cogRect);
                    result.AL_score.Add(score);
                    result.AL_Type.Add(labelindex + 1);
                }
            }

            return result;
        }

        // ======================== 文件推理（单张） ========================
        private void inferSetImage(string img_file)
        {
            HImage img = ReadHalconImage(img_file);
            if (img == null) return;

            DisplayImage(hWindowSource, img);
            DLResult result = InferFromHalconImage(img);
            DisplayImage(hWindowTarget, img);
            DrawHalconResults(hWindowTarget, result);

            if (bSaveXml && result.AL_Rect.Count > 0 || bSavePosiXml && result.AL_Rect.Count == 0)
            {
                string strFileName = Path.GetFileNameWithoutExtension(img_file);
                string strDestPath = strSavePath + "\\" + strFileName + ".xml";
                cdl.CreateRemarkXML_En("NullMark_00.xml", strFileName + ".jpg", strDestPath, result);
            }
            StripStatus_CurFile.Text = Path.GetFileName(img_file) + "；  " + (iInferIndex + 1).ToString() + "/" + imgfiles.Count.ToString();
        }

        // ======================== 文件连续推理线程 ========================
        private void FileInferThread()
        {
            is_infer = 1;
            int raise_ex_flag = 0;
            try
            {
                for (iInferIndex = iLastIndex; iInferIndex < imgfiles.Count; iInferIndex++)
                {
                    if (isBreakInfer == 1) { iLastIndex = 0; break; }
                    if (isPauseInfer == 1) { iLastIndex = iInferIndex; break; }
                    inferSetImage(imgfiles[iInferIndex]);
                }
            }
            catch (Exception)
            {
                raise_ex_flag = 1;
                MessageBox.Show("推理过程中出现异常，请检查模型或图像数据。", "错误");
            }
            isBreakInfer = 0;
            isPauseInfer = 0;
            is_infer = 0;
            infer_many_img_flag = 0;
            if (raise_ex_flag == 0) MessageBox.Show("图片文件夹推理完成!");
        }

        // ======================== 相机采集 ========================
        private HImage GrabOneFrame()
        {
            if (_selectedCamera == null || _bufferPtr == IntPtr.Zero) return null;
            MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();
            int nRet = _selectedCamera.MV_CC_GetOneFrameTimeout_NET(_bufferPtr, _payloadSize, ref stFrameInfo, 2000);
            if (nRet != MyCamera.MV_OK) return null;

            int width = (int)stFrameInfo.nWidth;
            int height = (int)stFrameInfo.nHeight;

            if (stFrameInfo.enPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8)
            {
                HImage img = new HImage();
                img.GenImage1("byte", width, height, _bufferPtr);
                return img;
            }
            else if (stFrameInfo.enPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed)
            {
                HImage img = new HImage();
                img.GenImageInterleaved(_bufferPtr, "bgr", width, height, -1, "byte", width, height, 0, 0, -1, 0);
                return img;
            }
            else
            {
                MessageBox.Show("不支持的相机像素格式", "错误");
                return null;
            }
        }

        // ======================== 相机实时推理线程 ========================
        private void CameraInferThread()
        {
            while (!_stopGrab)
            {
                if (_pauseGrab) { Thread.Sleep(50); continue; }
                HImage frame = GrabOneFrame();
                if (frame != null)
                {
                    this.Invoke((Action)(() => DisplayImage(hWindowSource, frame)));
                    DLResult result = InferFromHalconImage(frame);
                    this.Invoke((Action)(() =>
                    {
                        DisplayImage(hWindowTarget, frame);
                        DrawHalconResults(hWindowTarget, result);
                    }));
                }
                else Thread.Sleep(50);

                int delay = (int)numericUpDown1.Value;
                if (delay > 0) Thread.Sleep(delay);
            }
        }

        private void StopGrabThread()
        {
            if (_isGrabbing)
            {
                _stopGrab = true;
                if (_grabThread != null && _grabThread.IsAlive)
                {
                    _grabThread.Join(1000);
                    if (_grabThread.IsAlive) _grabThread.Abort();
                    _grabThread = null;
                }
                _isGrabbing = false;
                _pauseGrab = false;
                toolStart.Enabled = true;
                toolPause.Enabled = false;
                toolStop.Enabled = false;
                menu_Infer_Start.Enabled = true;
            }
        }

        // ======================== 相机枚举/初始化 ========================
        private void EnumCameras()
        {
            int nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref _deviceList);
            if (nRet != MyCamera.MV_OK) _deviceList.nDeviceNum = 0;
        }

        private void UpdateCameraMenuItems()
        {
            ToolStripMenuItem[] camMenus = { menu_Cam_1, menu_Cam_2, menu_Cam_3, menu_Cam_4 };
            for (int i = 0; i < camMenus.Length; i++)
            {
                if (i < _deviceList.nDeviceNum)
                    camMenus[i].Text = $"相机{i + 1} (已连接)";
                else
                    camMenus[i].Text = $"相机{i + 1} (未找到)";
            }
        }

        private bool InitSelectedCamera()
        {
            if (_selectedCamIndex < 0 || _selectedCamIndex >= _deviceList.nDeviceNum) return false;
            CloseCamera();

            _selectedCamera = new MyCamera();
            var device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(_deviceList.pDeviceInfo[_selectedCamIndex], typeof(MyCamera.MV_CC_DEVICE_INFO));
            int nRet = _selectedCamera.MV_CC_CreateDevice_NET(ref device);
            if (nRet != MyCamera.MV_OK) goto error;
            nRet = _selectedCamera.MV_CC_OpenDevice_NET();
            if (nRet != MyCamera.MV_OK) goto error;

            MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
            nRet = _selectedCamera.MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
            if (nRet != MyCamera.MV_OK) goto error;
            _payloadSize = stParam.nCurValue;
            if (_bufferPtr != IntPtr.Zero) Marshal.FreeHGlobal(_bufferPtr);
            _bufferPtr = Marshal.AllocHGlobal((int)_payloadSize);

            _selectedCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
            _selectedCamera.MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);

            if (cup.p_Cam != null && _selectedCamIndex < cup.p_Cam.Length)
            {
                var camParam = cup.p_Cam[_selectedCamIndex];
                _selectedCamera.MV_CC_SetFloatValue_NET("ExposureTime", camParam.iExposeTime);
                _selectedCamera.MV_CC_SetFloatValue_NET("Gain", camParam.dGain);
                _selectedCamera.MV_CC_SetEnumValue_NET("PixelFormat", (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8);
            }

            nRet = _selectedCamera.MV_CC_StartGrabbing_NET();
            if (nRet != MyCamera.MV_OK) goto error;

            StripStatus_modelStatus.Text = $"相机 {_selectedCamIndex + 1} 已连接";
            StripStatus_modelStatus.ForeColor = Color.Green;
            return true;

        error:
            MessageBox.Show($"相机 {_selectedCamIndex + 1} 初始化失败", "错误");
            CloseCamera();
            return false;
        }

        private void CloseCamera()
        {
            if (_selectedCamera != null)
            {
                _selectedCamera.MV_CC_StopGrabbing_NET();
                _selectedCamera.MV_CC_CloseDevice_NET();
                _selectedCamera.MV_CC_DestroyDevice_NET();
                _selectedCamera = null;
            }
            if (_bufferPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_bufferPtr);
                _bufferPtr = IntPtr.Zero;
            }
            _payloadSize = 0;
            StripStatus_modelStatus.Text = "相机未连接";
            StripStatus_modelStatus.ForeColor = Color.Red;
        }

        // ======================== 数据源切换 ========================
        private void SetDataSource(DataSource source)
        {
            _currentDataSource = source;
            menu_Infer_FromFile.Checked = (source == DataSource.File);
            menu_Infer_FromCam.Checked = (source == DataSource.Camera);

            if (source == DataSource.File)
            {
                if (_isGrabbing) StopGrabThread();
                menu_File_LoadFile.Enabled = true;
                menu_File_LoadDir.Enabled = true;
                menu_Infer_Start.Enabled = true;
                menu_Infer_Pauss.Enabled = true;
                menu_Infer_Stop.Enabled = true;
                toolStart.Enabled = false;
                toolPause.Enabled = false;
                toolStop.Enabled = false;
                StripStatus_modelStatus.Text = "数据源：文件";
                StripStatus_modelStatus.ForeColor = Color.Blue;
            }
            else
            {
                if (is_infer == 1) isBreakInfer = 1;
                Thread.Sleep(200);
                menu_File_LoadFile.Enabled = false;
                menu_File_LoadDir.Enabled = false;
                menu_Infer_Start.Enabled = false;
                menu_Infer_Pauss.Enabled = false;
                menu_Infer_Stop.Enabled = false;
                if (_selectedCamera != null && _selectedCamIndex >= 0)
                {
                    toolStart.Enabled = true;
                    toolPause.Enabled = true;
                    toolStop.Enabled = true;
                }
                else
                {
                    toolStart.Enabled = false;
                    toolPause.Enabled = false;
                    toolStop.Enabled = false;
                }
                StripStatus_modelStatus.Text = "数据源：相机";
                StripStatus_modelStatus.ForeColor = Color.Blue;
            }
        }

        // ======================== 模型初始化 ========================
        private void LoadClassNames(string filePath)
        {
            classNames.Clear();
            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    string trim = line.Trim();
                    if (!string.IsNullOrEmpty(trim)) classNames.Add(trim);
                }
            }
            else
            {
                classNames = new List<string> { "aotu", "diaoqi", "sunhaui", "true" };
            }
        }

        private void InitDL(DLParam dlParam)
        {
            if (Directory.Exists(dlParam.modelPath))
            {
                try
                {
                    int iRet = cdl.InitDL(dlParam);
                    has_model_init = (iRet == 0) ? 1 : 0;
                }
                catch { has_model_init = 0; }
            }
            else has_model_init = 0;

            if (has_model_init == 1)
            {
                StripStatus_modelStatus.Text = "模型已初始化";
                StripStatus_modelStatus.ForeColor = Color.Green;
                StripStatus_ModelFile.Text = dlParam.modelPath;
                string classFilePath = Path.Combine(Path.GetDirectoryName(dlParam.modelPath), "inference_model\\predefined_classes.txt");
                LoadClassNames(classFilePath);
            }
            else
            {
                StripStatus_modelStatus.Text = "模型未初始化";
                StripStatus_modelStatus.ForeColor = Color.Red;
                StripStatus_ModelFile.Text = "--";
            }
        }

        // ======================== 菜单事件 ========================

        private void menu_File_LoadFile_Click(object sender, EventArgs e)
        {
            if (_currentDataSource != DataSource.File) SetDataSource(DataSource.File);
            if (is_infer == 1) { MessageBox.Show("正在推理中，请先停止。"); return; }
            openFileDialog1.Multiselect = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                imgfiles.Clear();
                imgfiles.AddRange(openFileDialog1.FileNames);
                if (imgfiles.Count > 0)
                {
                    HImage img = ReadHalconImage(imgfiles[0]);
                    if (img != null) DisplayImage(hWindowSource, img);
                }
            }
        }

        private void menu_File_LoadDir_Click(object sender, EventArgs e)
        {
            if (_currentDataSource != DataSource.File) SetDataSource(DataSource.File);
            if (is_infer == 1) { MessageBox.Show("正在推理中，请先停止。"); return; }
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                string dir = folderBrowserDialog1.SelectedPath;
                imgfiles.Clear();
                string[] exts = { "*.jpg", "*.png", "*.bmp", "*.jpeg" };
                foreach (var ext in exts)
                    imgfiles.AddRange(Directory.GetFiles(dir, ext, SearchOption.AllDirectories));
                if (imgfiles.Count > 0)
                {
                    HImage img = ReadHalconImage(imgfiles[0]);
                    if (img != null) DisplayImage(hWindowSource, img);
                    MessageBox.Show($"加载 {imgfiles.Count} 张图片");
                }
                else MessageBox.Show("未找到图片文件");
            }
        }

        private void menu_Model_Init_Click(object sender, EventArgs e)
        {
            if (is_infer == 1 || _isGrabbing) { MessageBox.Show("请先停止推理。"); return; }
            if (has_model_init == 1) DestructModel();
            dlParam.modelType = model_type;
            dlParam.bUseGPU = use_gpu;
            dlParam.gpuID = gpu_id;
            InitDL(dlParam);
        }

        private void menu_Model_Destroy_Click(object sender, EventArgs e)
        {
            if (is_infer == 1 || _isGrabbing) { MessageBox.Show("请先停止推理。"); return; }
            if (has_model_init == 1)
            {
                try { DestructModel(); } catch { }
                has_model_init = 0;
                StripStatus_modelStatus.Text = "模型已销毁";
                StripStatus_modelStatus.ForeColor = Color.Red;
            }
        }

        private void menu_Model_ModelPath_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                dlParam.modelPath = folderBrowserDialog1.SelectedPath;
                StripStatus_ModelFile.Text = dlParam.modelPath;
            }
        }

        private void menu_Infer_Start_Click(object sender, EventArgs e)
        {
            if (_currentDataSource == DataSource.File)
            {
                if (imgfiles.Count == 0) { MessageBox.Show("请先加载图片文件。"); return; }
                if (is_infer == 1) return;
                if (has_model_init != 1) { MessageBox.Show("请先初始化模型。"); return; }
                isBreakInfer = 0;
                iLastIndex = 0;
                Thread infer_thread = new Thread(FileInferThread);
                infer_thread.Start();
                infer_many_img_flag = 1;
            }
            else
            {
                if (_selectedCamera == null || _selectedCamIndex < 0)
                {
                    MessageBox.Show("请先通过菜单选择并连接相机。", "提示");
                    return;
                }
                if (has_model_init != 1) { MessageBox.Show("请先初始化模型。", "提示"); return; }
                if (_isGrabbing) return;

                _stopGrab = false;
                _pauseGrab = false;
                _isGrabbing = true;
                _grabThread = new Thread(CameraInferThread);
                _grabThread.IsBackground = true;
                _grabThread.Start();
                toolStart.Enabled = false;
                toolPause.Enabled = true;
                toolStop.Enabled = true;
            }
        }

        private void menu_Infer_Pauss_Click(object sender, EventArgs e)
        {
            if (_currentDataSource == DataSource.File)
            {
                if (is_infer == 1) isPauseInfer = 1;
            }
            else
            {
                if (_isGrabbing)
                {
                    _pauseGrab = !_pauseGrab;
                    menu_Infer_Pauss.Text = _pauseGrab ? "继续推理" : "暂停推理";
                }
            }
        }

        private void menu_Infer_Stop_Click(object sender, EventArgs e)
        {
            if (_currentDataSource == DataSource.File)
            {
                if (is_infer == 1) isBreakInfer = 1;
            }
            else
            {
                StopGrabThread();
                toolStart.Enabled = true;
                toolPause.Enabled = false;
                toolStop.Enabled = false;
                menu_Infer_Pauss.Text = "暂停推理";
            }
        }

        private void menu_Infer_FromFile_Click(object sender, EventArgs e) => SetDataSource(DataSource.File);
        private void menu_Infer_FromCam_Click(object sender, EventArgs e) => SetDataSource(DataSource.Camera);

        private void menu_Cam_1_Click(object sender, EventArgs e) { _selectedCamIndex = 0; InitSelectedCamera(); if (_currentDataSource == DataSource.Camera) SetDataSource(DataSource.Camera); }
        private void menu_Cam_2_Click(object sender, EventArgs e) { _selectedCamIndex = 1; InitSelectedCamera(); if (_currentDataSource == DataSource.Camera) SetDataSource(DataSource.Camera); }
        private void menu_Cam_3_Click(object sender, EventArgs e) { _selectedCamIndex = 2; InitSelectedCamera(); if (_currentDataSource == DataSource.Camera) SetDataSource(DataSource.Camera); }
        private void menu_Cam_4_Click(object sender, EventArgs e) { _selectedCamIndex = 3; InitSelectedCamera(); if (_currentDataSource == DataSource.Camera) SetDataSource(DataSource.Camera); }

        private void menuCam_RefreshCam_Click(object sender, EventArgs e)
        {
            if (_isGrabbing) StopGrabThread();
            if (is_infer == 1) isBreakInfer = 1;
            Thread.Sleep(200);
            CloseCamera();
            EnumCameras();
            UpdateCameraMenuItems();
            if (_deviceList.nDeviceNum > 0) _selectedCamIndex = 0;
            else _selectedCamIndex = -1;
            if (_currentDataSource == DataSource.Camera && _selectedCamIndex >= 0)
                InitSelectedCamera();
            MessageBox.Show("相机列表已刷新", "提示");
        }

        private void CamConn_Click(object sender, EventArgs e)
        {
            if (_selectedCamera == null || _selectedCamIndex < 0)
            {
                MessageBox.Show("请先通过菜单选择并连接相机。", "提示");
                return;
            }
            if (has_model_init != 1) { MessageBox.Show("请先初始化模型。", "提示"); return; }
            HImage frame = GrabOneFrame();
            if (frame != null)
            {
                DisplayImage(hWindowSource, frame);
                DLResult result = InferFromHalconImage(frame);
                DisplayImage(hWindowTarget, frame);
                DrawHalconResults(hWindowTarget, result);
            }
            else MessageBox.Show("采集图像失败", "错误");
        }

        private void toolStart_Click(object sender, EventArgs e) => menu_Infer_Start_Click(null, null);
        private void toolPause_Click(object sender, EventArgs e) => menu_Infer_Pauss_Click(null, null);
        private void toolStop_Click(object sender, EventArgs e) => menu_Infer_Stop_Click(null, null);
        private void toolInit_Click(object sender, EventArgs e) => menu_Model_Init_Click(null, null);
        private void toolDestroy_Click(object sender, EventArgs e) => menu_Model_Destroy_Click(null, null);

        private void btAutoMarkPath_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                strSavePath = folderBrowserDialog1.SelectedPath;
                txtPath.Text = strSavePath;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e) { }
        private void checkBox2_CheckedChanged(object sender, EventArgs e) => bSaveXml = checkBox2.Checked;
        private void checkBox3_CheckedChanged(object sender, EventArgs e) => bSavePosiXml = checkBox3.Checked;

        // 快捷键
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (_currentDataSource != DataSource.File || imgfiles.Count < 1) return base.ProcessDialogKey(keyData);
            if (keyData == Keys.A || keyData == Keys.Left)
            {
                if (is_infer == 1) isBreakInfer = 1;
                iInferIndex--;
                if (iInferIndex < 0) iInferIndex = imgfiles.Count - 1;
                inferSetImage(imgfiles[iInferIndex]);
                return true;
            }
            else if (keyData == Keys.D || keyData == Keys.Right)
            {
                if (is_infer == 1) isBreakInfer = 1;
                iInferIndex++;
                if (iInferIndex >= imgfiles.Count) iInferIndex = 0;
                inferSetImage(imgfiles[iInferIndex]);
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        private void cmbEnv_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (is_infer == 1 || _isGrabbing || has_model_init == 1) return;
            use_gpu = cmb_Env.SelectedIndex == 1;
        }

        private void cmbModelType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (is_infer == 1 || _isGrabbing || has_model_init == 1) return;
            model_type = cmb_ModelType.Text;
        }

        private void txtGPUID_TextChanged(object sender, EventArgs e)
        {
            if (is_infer == 1 || _isGrabbing || has_model_init == 1) return;
            int.TryParse(txtGPUID.Text, out gpu_id);
        }

        private void cmb_Thre_TextChanged(object sender, EventArgs e)
        {
            if (float.TryParse(cmb_Thre.Text, out float th)) det_threshold = th;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e) { }
        private void cmbThre_SelectedIndexChanged(object sender, EventArgs e) { }

        private void FrmDL_SizeChanged(object sender, EventArgs e)
        {
            int width = this.Width;
            int height = this.Height;
            int xzHeight = height - 100;
            if (hWindowSource != null && hWindowTarget != null)
            {
                hWindowSource.Width = (width - groupBox1.Width - 40) / 2;
                hWindowTarget.Width = hWindowSource.Width;
                hWindowSource.Height = xzHeight;
                hWindowTarget.Height = xzHeight;
                hWindowSource.Left = 0; hWindowSource.Top = 30;
                hWindowTarget.Left = hWindowSource.Right + 10; hWindowTarget.Top = hWindowSource.Top;
            }
        }

        // ======================== 辅助 ========================
        public static IntPtr FloatToIntptr(float[] bytes)
        {
            GCHandle hObject = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            return hObject.AddrOfPinnedObject();
        }
    }
}
