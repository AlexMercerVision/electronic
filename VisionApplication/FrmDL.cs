using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Cognex.VisionPro;
using Cognex.VisionPro.ImageFile;
using CommonCls;
using MvCamCtrl.NET;  // 海康相机

namespace xbDefectDect
{
    public partial class FrmDL : Form
    {
        // ======================== DLL 导入 ========================
        [DllImport("model_infer.dll", EntryPoint = "InitModel")]
        public static extern void InitModel(string model_type, string model_filename, string params_filename, string cfg_file, bool use_gpu, int gpu_id, ref byte paddlex_model_type);

        [DllImport("model_infer.dll", EntryPoint = "Det_ModelPredict")]
        public static extern int Det_ModelPredict(byte[] img, int W, int H, int C, IntPtr output, int[] BoxesNum, ref byte label);

        [DllImport("model_infer.dll", EntryPoint = "Seg_ModelPredict")]
        public static extern void Seg_ModelPredict(byte[] img, int W, int H, int C, ref byte output);

        [DllImport("model_infer.dll", EntryPoint = "Cls_ModelPredict")]
        public static extern void Cls_ModelPredict(byte[] img, int W, int H, int C, ref float score, ref byte category, ref int category_id);

        [DllImport("model_infer.dll", EntryPoint = "Mask_ModelPredict")]
        public static extern void Mask_ModelPredict(byte[] img, int W, int H, int C, IntPtr output, ref byte Mask_output, int[] BoxesNum, ref byte label);

        [DllImport("model_infer.dll", EntryPoint = "DestructModel")]
        public static extern void DestructModel();

        // ======================== 数据源枚举 ========================
        private enum DataSource
        {
            File,
            Camera
        }
        private DataSource _currentDataSource = DataSource.File;

        // ======================== 原有字段 ========================
        private string imgfile = null;
        private List<string> imgfiles = new List<string>();
        private string videofile = null;
        private bool use_gpu = false;
        private int gpu_id = 0;
        private float det_threshold = 0.5F;
        private string model_type = "det";
        private string model_filename = null;
        private string params_filename = null;
        private string cfg_file = null;
        private byte[] paddlex_model_type = new byte[10];
        private bool paddlex_doing = false;
        private List<string> classNames = new List<string>();
        public int has_model_init = 0;
        private static int is_infer = 0;
        private static int isBreakInfer = 0;
        private static int infer_many_img_flag = 0;
        private int continue_infer_delay = 50;
        private int iInferIndex = -1;
        private DLResult dlRes = new DLResult();
        private CDL cdl = new CDL();
        private CUserParam cup = new CUserParam();
        private FileOper fo = new FileOper();
        private bool bFromMainWind = true;
        private CogImage8Grey ci8gImageFile = new CogImage8Grey();
        private CogImage8Root cogImage8RootR = new CogImage8Root();
        private CogImage8Root cogImage8RootG = new CogImage8Root();
        private CogImage8Root cogImage8RootB = new CogImage8Root();
        private CogImage24PlanarColor cogImage = new CogImage24PlanarColor(3072, 2048);
        private CogImageFileTool imageFileTool = new CogImageFileTool();
        private string strSavePath = "";
        private bool bSave = false;
        private bool bSaveXml = false;
        private bool bSavePosiXml = false;
        private int img_dir_load_flag = 0;
        private int comboBox_clicked = 0;
        private int comboBox1_last_index = 0;
        private int last_gpu_id = 0;
        private int gpu_id_done = 0;
        private int comboBox2_last_index = 0;
        private int comboBox3_last_index = 0;
        private int isPauseInfer = 0;
        private int iLastIndex = 0;
        private DLParam dlParam = new DLParam();

        // ======================== 相机相关字段 ========================
        private MyCamera _selectedCamera = null;
        private MyCamera.MV_CC_DEVICE_INFO_LIST _deviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
        private IntPtr _bufferPtr = IntPtr.Zero;
        private uint _payloadSize = 0;
        private int _selectedCamIndex = -1;
        private bool _isGrabbing = false;
        private Thread _grabThread = null;
        private volatile bool _stopGrab = false;
        private volatile bool _pauseGrab = false;
        private object _grabLock = new object();

        // ======================== 构造函数 ========================
        public FrmDL()
        {
            InitializeComponent();
            bFromMainWind = false;
            fo.ReadUserParam(ref cup);
            dlParam = cup.p_DL;
        }

        public FrmDL(List<string> strCurFile)
        {
            InitializeComponent();
            imgfiles = new List<string>(strCurFile);
            ICogImage img = ReadCogImage(imgfiles[0]);
            if (img is CogImage8Grey gray) ci8gImageFile = gray;
            else if (img is CogImage24PlanarColor color)
            {
                cogRDSource.Image = color;
                cogRDTarget.Image = color;
                return;
            }
            cogRDSource.Image = ci8gImageFile;
            cogRDTarget.Image = ci8gImageFile;
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

            // 枚举相机并更新菜单
            EnumCameras();
            UpdateCameraMenuItems();
            if (_deviceList.nDeviceNum > 0)
                _selectedCamIndex = 0;

            // 默认数据源：文件
            SetDataSource(DataSource.File);
        }

        // ======================== 窗体关闭 ========================
        private void Form1_FromClosing(object sender, EventArgs e)
        {
            // 停止相机推理
            StopGrabThread();

            // 停止文件推理
            if (is_infer == 1) isBreakInfer = 1;
            Thread.Sleep(200);

            if (has_model_init == 1 && !bFromMainWind) DestructModel();
            CloseCamera();
        }

        // ======================== 相机枚举 ========================
        private void EnumCameras()
        {
            int nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref _deviceList);
            if (nRet != MyCamera.MV_OK)
            {
                MessageBox.Show($"枚举相机失败，错误码: {nRet}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _deviceList.nDeviceNum = 0;
            }
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

        // ======================== 相机初始化与关闭 ========================
        private bool InitSelectedCamera()
        {
            if (_selectedCamIndex < 0 || _selectedCamIndex >= _deviceList.nDeviceNum)
            {
                MessageBox.Show($"相机索引 {_selectedCamIndex + 1} 无效", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

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
            MessageBox.Show($"相机 {_selectedCamIndex + 1} 初始化失败，错误码: {nRet}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        // ======================== 采集单帧图像 ========================
        private ICogImage GrabOneFrame()
        {
            if (_selectedCamera == null || _bufferPtr == IntPtr.Zero) return null;
            MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();
            int nRet = _selectedCamera.MV_CC_GetOneFrameTimeout_NET(_bufferPtr, _payloadSize, ref stFrameInfo, 2000);
            if (nRet != MyCamera.MV_OK) return null;

            if (stFrameInfo.enPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8)
            {
                CogImage8Root root = new CogImage8Root();
                root.Initialize((int)stFrameInfo.nWidth, (int)stFrameInfo.nHeight, _bufferPtr, (int)stFrameInfo.nWidth, null);
                CogImage8Grey gray = new CogImage8Grey();
                gray.SetRoot(root);
                return gray.Copy();
            }
            else
            {
                if (stFrameInfo.enPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed)
                {
                    Bitmap bmp = new Bitmap((int)stFrameInfo.nWidth, (int)stFrameInfo.nHeight, (int)stFrameInfo.nWidth * 3, PixelFormat.Format24bppRgb, _bufferPtr);
                    CogImage24PlanarColor color = new CogImage24PlanarColor(bmp);
                    return color;
                }
                else
                {
                    MessageBox.Show("不支持的彩色格式", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
            }
        }

        // ======================== 统一推理方法 ========================
        private DLResult InferFromImage(ICogImage image)
        {
            DLResult result = new DLResult();
            result.AL_Rect = new List<CogRectangleAffine>();
            result.AL_score = new List<float>();
            result.AL_Type = new List<int>();
            result.Al_bDiff = new List<bool>();

            if (image == null || has_model_init != 1) return result;

            int width = 0, height = 0;
            byte[] bgrData = null;

            if (image is CogImage8Grey grayImg)
            {
                width = grayImg.Width;
                height = grayImg.Height;
                using (Bitmap bmp = grayImg.ToBitmap())
                using (Bitmap bgrBmp = new Bitmap(width, height, PixelFormat.Format24bppRgb))
                using (Graphics g = Graphics.FromImage(bgrBmp))
                {
                    g.DrawImage(bmp, 0, 0, width, height);
                    BitmapData bmpData = bgrBmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    bgrData = new byte[bmpData.Stride * height];
                    Marshal.Copy(bmpData.Scan0, bgrData, 0, bgrData.Length);
                    bgrBmp.UnlockBits(bmpData);
                }
            }
            else if (image is CogImage24PlanarColor colorImg)
            {
                width = colorImg.Width;
                height = colorImg.Height;
                using (Bitmap bmp = colorImg.ToBitmap())
                {
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    bgrData = new byte[bmpData.Stride * height];
                    Marshal.Copy(bmpData.Scan0, bgrData, 0, bgrData.Length);
                    bmp.UnlockBits(bmpData);
                }
            }
            else
            {
                return result;
            }

            float[] resultlist = new float[600];
            IntPtr results = FloatToIntptr(resultlist);
            int[] boxesInfo = new int[1];
            byte[] labellist = new byte[1000];

            TimeSpan infer_start_time = new TimeSpan(DateTime.Now.Ticks);
            try
            {
                Det_ModelPredict(bgrData, width, height, 3, results, boxesInfo, ref labellist[0]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return result;
            }
            TimeSpan infer_end_time = new TimeSpan(DateTime.Now.Ticks);

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
                    result.AL_Rect.Add(cogRect);
                    result.AL_score.Add(score);
                    result.AL_Type.Add(labelindex + 1);
                }
            }

            double cost_ms = infer_end_time.Subtract(infer_start_time).TotalMilliseconds;
            this.Invoke((Action)(() => StripStatus_InferSpeed.Text = ((int)cost_ms).ToString()));

            return result;
        }

        // ======================== 文件读取 ========================
        private ICogImage ReadCogImage(string strImageFile)
        {
            imageFileTool.Operator.Open(strImageFile, CogImageFileModeConstants.Read);
            imageFileTool.Run();
            ICogImage image = imageFileTool.OutputImage;
            imageFileTool.Operator.Close();
            return image;
        }

        // ======================== 文件推理（单张） ========================
        private void inferSetImage(string img_file)
        {
            ICogImage ci = ReadCogImage(img_file);
            if (ci == null) return;
            cogRDSource.Image = ci;
            DLResult result = InferFromImage(ci);
            cogRDTarget.Image = ci;
            AddGrap(cogRDTarget, result);
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
                MessageBox.Show("推理过程中出现异常，请检查模型或图像数据。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            isBreakInfer = 0;
            isPauseInfer = 0;
            is_infer = 0;
            infer_many_img_flag = 0;
            if (raise_ex_flag == 0) MessageBox.Show("图片文件夹推理完成!");
        }

        // ======================== 相机实时推理线程 ========================
        private void CameraInferThread()
        {
            while (!_stopGrab)
            {
                if (_pauseGrab)
                {
                    Thread.Sleep(50);
                    continue;
                }
                ICogImage frame = GrabOneFrame();
                if (frame != null)
                {
                    this.Invoke((Action)(() => cogRDSource.Image = frame));
                    DLResult result = InferFromImage(frame);
                    this.Invoke((Action)(() =>
                    {
                        cogRDTarget.Image = frame;
                        AddGrap(cogRDTarget, result);
                    }));
                }
                else
                {
                    Thread.Sleep(50);
                }
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

        // ======================== 添加图形 ========================
        private void AddGrap(CogRecordDisplay cogDisp, DLResult dlRes)
        {
            cogDisp.InteractiveGraphics.Clear();
            CogColorConstants[] colors = new CogColorConstants[]
            {
                CogColorConstants.Red, CogColorConstants.Green, CogColorConstants.Blue,
                CogColorConstants.Yellow, CogColorConstants.Purple, CogColorConstants.Orange
            };

            for (int i = 0; i < dlRes.AL_Rect.Count; i++)
            {
                int typeId = dlRes.AL_Type[i];
                CogColorConstants color = (typeId > 0 && typeId <= colors.Length) ? colors[typeId - 1] : CogColorConstants.Black;
                dlRes.AL_Rect[i].Color = color;
                CogGraphicLabel label = new CogGraphicLabel();
                label.Color = color;
                label.Font = new Font("arial", 10);
                label.X = dlRes.AL_Rect[i].CornerYX;
                label.Y = dlRes.AL_Rect[i].CornerXY;
                string className = (typeId > 0 && typeId <= classNames.Count) ? classNames[typeId - 1] : "unknown";
                label.Text = $"{className}:{dlRes.AL_score[i]:F2}";
                string groupName = $"Result_{i}";
                cogDisp.InteractiveGraphics.Add(dlRes.AL_Rect[i], groupName, false);
                cogDisp.InteractiveGraphics.Add(label, groupName, false);
            }
        }

        // ======================== 辅助 ========================
        public static IntPtr FloatToIntptr(float[] bytes)
        {
            GCHandle hObject = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            return hObject.AddrOfPinnedObject();
        }

        public List<string> getFile(string path, string extName, List<string> lst)
        {
            string[] strFiles = Directory.GetFiles(path, extName, SearchOption.AllDirectories);
            if (strFiles.Length != 0) lst.AddRange(strFiles);
            return lst;
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
            int iRet = -1;
            if (Directory.Exists(dlParam.modelPath))
            {
                try
                {
                    iRet = cdl.InitDL(dlParam);
                    has_model_init = (iRet == 0) ? 1 : 0;
                }
                catch { has_model_init = 0; }
            }
            else
            {
                has_model_init = 0;
            }
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

        // ======================== 数据源切换 ========================
        private void SetDataSource(DataSource source)
        {
            _currentDataSource = source;
            // 更新菜单选中状态
            menu_Infer_FromFile.Checked = (source == DataSource.File);
            menu_Infer_FromCam.Checked = (source == DataSource.Camera);

            // 根据数据源启用/禁用相关控件
            if (source == DataSource.File)
            {
                // 停止相机推理（如果正在运行）
                if (_isGrabbing) StopGrabThread();
                // 启用文件加载菜单
                menu_File_LoadFile.Enabled = true;
                menu_File_LoadDir.Enabled = true;
                // 启用文件推理控制
                menu_Infer_Start.Enabled = true;
                menu_Infer_Pauss.Enabled = true;
                menu_Infer_Stop.Enabled = true;
                // 禁用相机实时按钮（单帧仍可用）
                toolStart.Enabled = false;
                toolPause.Enabled = false;
                toolStop.Enabled = false;
                // 状态提示
                StripStatus_modelStatus.Text = "数据源：文件";
                StripStatus_modelStatus.ForeColor = Color.Blue;
            }
            else // Camera
            {
                // 停止文件推理（如果正在运行）
                if (is_infer == 1) isBreakInfer = 1;
                Thread.Sleep(200);
                // 禁用文件加载菜单
                menu_File_LoadFile.Enabled = false;
                menu_File_LoadDir.Enabled = false;
                // 禁用文件推理控制（暂停/停止仍可通过菜单操作，但启动要禁用）
                menu_Infer_Start.Enabled = false;
                menu_Infer_Pauss.Enabled = false;
                menu_Infer_Stop.Enabled = false;
                // 启用相机实时按钮（如果相机已连接）
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

        // ======================== 菜单事件 ========================

        // ---- 文件加载 ----
        private void menu_File_LoadFile_Click(object sender, EventArgs e)
        {
            if (_currentDataSource != DataSource.File)
            {
                MessageBox.Show("当前数据源不是文件模式，请切换至文件模式。", "提示");
                return;
            }
            if (is_infer == 1) { MessageBox.Show("正在推理中，请先停止。"); return; }
            openFileDialog1.Multiselect = true;
            DialogResult dr = openFileDialog1.ShowDialog();
            if (dr == DialogResult.OK && openFileDialog1.FileNames.Length > 0)
            {
                imgfiles.Clear();
                imgfiles.AddRange(openFileDialog1.FileNames);
                if (imgfiles.Count > 0)
                {
                    ICogImage img = ReadCogImage(imgfiles[0]);
                    cogRDSource.Image = img;
                }
            }
        }

        private void menu_File_LoadDir_Click(object sender, EventArgs e)
        {
            if (_currentDataSource != DataSource.File)
            {
                MessageBox.Show("当前数据源不是文件模式，请切换至文件模式。", "提示");
                return;
            }
            if (is_infer == 1) { MessageBox.Show("正在推理中，请先停止。"); return; }
            folderBrowserDialog1.Description = "选择图片文件夹";
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                string dir = folderBrowserDialog1.SelectedPath;
                imgfiles.Clear();
                imgfiles = getFile(dir, "*.jpg", imgfiles);
                imgfiles = getFile(dir, "*.png", imgfiles);
                imgfiles = getFile(dir, "*.bmp", imgfiles);
                imgfiles = getFile(dir, "*.jpeg", imgfiles);
                if (imgfiles.Count > 0)
                {
                    ICogImage img = ReadCogImage(imgfiles[0]);
                    cogRDSource.Image = img;
                    MessageBox.Show($"加载 {imgfiles.Count} 张图片");
                }
                else MessageBox.Show("未找到图片文件");
            }
        }

        // ---- 模型菜单 ----
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

        // ---- 推理控制（根据数据源启动/暂停/停止） ----
        private void menu_Infer_Start_Click(object sender, EventArgs e)
        {
            if (_currentDataSource == DataSource.File)
            {
                // 文件连续推理
                if (imgfiles.Count == 0) { MessageBox.Show("请先加载图片文件。"); return; }
                if (is_infer == 1) { MessageBox.Show("推理已在进行中。"); return; }
                if (has_model_init != 1) { MessageBox.Show("请先初始化模型。"); return; }
                isBreakInfer = 0;
                iLastIndex = 0;
                Thread infer_thread = new Thread(FileInferThread);
                infer_thread.Start();
                infer_many_img_flag = 1;
            }
            else if (_currentDataSource == DataSource.Camera)
            {
                // 相机实时推理
                if (_selectedCamera == null || _selectedCamIndex < 0)
                {
                    MessageBox.Show("请先通过菜单选择并连接相机。", "提示");
                    return;
                }
                if (has_model_init != 1)
                {
                    MessageBox.Show("请先初始化模型。", "提示");
                    return;
                }
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
                if (is_infer == 1)
                    isPauseInfer = 1;
            }
            else if (_currentDataSource == DataSource.Camera)
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
                if (is_infer == 1)
                    isBreakInfer = 1;
            }
            else if (_currentDataSource == DataSource.Camera)
            {
                StopGrabThread();
                toolStart.Enabled = true;
                toolPause.Enabled = false;
                toolStop.Enabled = false;
                menu_Infer_Pauss.Text = "暂停推理";
            }
        }

        // ---- 数据源切换 ----
        private void menu_Infer_FromFile_Click(object sender, EventArgs e)
        {
            SetDataSource(DataSource.File);
        }

        private void menu_Infer_FromCam_Click(object sender, EventArgs e)
        {
            SetDataSource(DataSource.Camera);
        }

        // ---- 相机选择 ----
        private void menu_Cam_1_Click(object sender, EventArgs e) { _selectedCamIndex = 0; InitSelectedCamera(); if (_currentDataSource == DataSource.Camera) SetDataSource(DataSource.Camera); }
        private void menu_Cam_2_Click(object sender, EventArgs e) { _selectedCamIndex = 1; InitSelectedCamera(); if (_currentDataSource == DataSource.Camera) SetDataSource(DataSource.Camera); }
        private void menu_Cam_3_Click(object sender, EventArgs e) { _selectedCamIndex = 2; InitSelectedCamera(); if (_currentDataSource == DataSource.Camera) SetDataSource(DataSource.Camera); }
        private void menu_Cam_4_Click(object sender, EventArgs e) { _selectedCamIndex = 3; InitSelectedCamera(); if (_currentDataSource == DataSource.Camera) SetDataSource(DataSource.Camera); }

        // ---- 刷新相机 ----
        private void menuCam_RefreshCam_Click(object sender, EventArgs e)
        {
            // 停止所有推理
            if (_isGrabbing) StopGrabThread();
            if (is_infer == 1) isBreakInfer = 1;
            Thread.Sleep(200);

            CloseCamera();
            EnumCameras();
            UpdateCameraMenuItems();
            if (_deviceList.nDeviceNum > 0)
                _selectedCamIndex = 0;
            else
                _selectedCamIndex = -1;

            // 如果当前是相机模式，重新初始化（如果选了相机）
            if (_currentDataSource == DataSource.Camera && _selectedCamIndex >= 0)
                InitSelectedCamera();

            MessageBox.Show("相机列表已刷新", "提示");
        }

        // ---- 保存路径 ----
        private void btAutoMarkPath_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                strSavePath = folderBrowserDialog1.SelectedPath;
                txtPath.Text = strSavePath;
            }
        }

        // ---- 保存选项 ----
        private void checkBox1_CheckedChanged(object sender, EventArgs e) { bSave = checkBox1.Checked; }
        private void checkBox2_CheckedChanged(object sender, EventArgs e) { bSaveXml = checkBox2.Checked; }
        private void checkBox3_CheckedChanged(object sender, EventArgs e) { bSavePosiXml = checkBox3.Checked; }

        // ---- 快捷键 ----
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

        // ---- 参数变化 ----
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

        // ---- 工具栏 ----
        private void toolInit_Click(object sender, EventArgs e) => menu_Model_Init_Click(null, null);
        private void toolDestroy_Click(object sender, EventArgs e) => menu_Model_Destroy_Click(null, null);

        // 单帧采集（相机）——始终可用
        private void CamConn_Click(object sender, EventArgs e)
        {
            if (_selectedCamera == null || _selectedCamIndex < 0)
            {
                MessageBox.Show("请先通过菜单选择并连接相机。", "提示");
                return;
            }
            if (has_model_init != 1)
            {
                MessageBox.Show("请先初始化模型。", "提示");
                return;
            }
            ICogImage frame = GrabOneFrame();
            if (frame != null)
            {
                cogRDSource.Image = frame;
                DLResult result = InferFromImage(frame);
                cogRDTarget.Image = frame;
                AddGrap(cogRDTarget, result);
            }
            else
            {
                MessageBox.Show("采集图像失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 实时推理——仅当数据源为相机时启用（但按钮可点击，我们通过SetDataSource控制其Enabled）
        private void toolStart_Click(object sender, EventArgs e)
        {
            // 调用菜单启动推理（因为逻辑已统一）
            menu_Infer_Start_Click(null, null);
        }

        private void toolPause_Click(object sender, EventArgs e)
        {
            menu_Infer_Pauss_Click(null, null);
        }

        private void toolStop_Click(object sender, EventArgs e)
        {
            menu_Infer_Stop_Click(null, null);
        }

        // ---- 窗体大小变化 ----
        private void FrmDL_SizeChanged(object sender, EventArgs e)
        {
            int width = this.Width;
            int height = this.Height;
            int xzHeight = height - 100;
            cogRDSource.Width = (width - groupBox1.Width - 40) / 2;
            cogRDTarget.Width = cogRDSource.Width;
            cogRDSource.Height = xzHeight;
            cogRDTarget.Height = xzHeight;
            cogRDSource.Left = 0; cogRDSource.Top = 30;
            cogRDTarget.Left = cogRDSource.Right + 10; cogRDTarget.Top = cogRDSource.Top;
            if (img_dir_load_flag == 1)
            {
                cogRDTarget.Fit();
                cogRDSource.Fit();
            }
        }

        private void cmbThre_SelectedIndexChanged(object sender, EventArgs e) { }
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            continue_infer_delay = (int)numericUpDown1.Value;
        }
    }
}