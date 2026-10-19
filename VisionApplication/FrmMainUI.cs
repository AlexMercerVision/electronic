 
using Cognex.VisionPro.Blob;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.Caliper;
using Cognex.VisionPro.PMAlign;
using CommonCls;
using MvCamCtrl.NET;  // 海康相机
using PipeChanel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
 
using System.Diagnostics;
using System.Drawing;
 
using System.Drawing.Imaging;
using System.IO;
using System.IO.Pipes;
using System.IO.Ports;
using System.Linq;
using System.Management; 
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Threading;   
using System.Threading.Tasks;   // 可选，如果使用 Task.Run 则需要，上述代码未使用 Task
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml; 
using CameraHandle = System.Int32;

namespace VisionApplication
{


    public partial class FrmMainUI : Form  
    {
        #region       
        // 1. 常量
        internal const string ApplicationName = "电极帽缺陷检测";
        private const int maxCameraNum = 2;                     // 最大支持的相机数量（工位数量）
        private const int maxJobNum = maxCameraNum;
        private const int ioFeedbackJobCount = 2;               // 当前COM3控制器只接收Job0、Job1结果
        private const int lightControllerSlotCount = 3;         // 前三路串口槽可用于光源，第4路固定为外部IO
        private const int lightChannelCount = 4;                // 单台光源控制器固定四通道
        private const string authorizationManagerMac = "345A60F4B131"; // 授权端物理主机
        // 2. 字段 - UI相关
        private Frm_Welcome fw;                                 // 欢迎窗体对象，用于显示启动进度
        private Protect mPasswordFile = new Protect("C:\\Users\\GearDet\\authentication.txt");  // 授权文件保护类

        // 3. 字段 - 核心业务对象
        private CUserParam cup = new CUserParam();              // 用户配置参数
        private FileOper fo = new FileOper();                   // 文件操作辅助类
        private OperLog operLog = new OperLog();                // 操作日志记录对象
        private DetectionResult dr = new DetectionResult();    // 视觉检测结果
        private CSerialDef csd = new CSerialDef();              // 串口通信辅助类
        private SerialPort sPortMain = new SerialPort();        // 主光源串口
        private SerialPort sPortCy = new SerialPort();          // IO通信串口
        private SerialPort[] sPortSub = new SerialPort[2];      // 侧面光源串口数组(2个)
        private SerialPort[] sPortAll = new SerialPort[4];      // 所有串口数组(4个)
        private int[] serialInd = { 0, 1, 2, 3 };               // 串口索引数组

        // 4. 字段 - 文件回放
        private int iFileIndex = 0, iNextFileIndex = 0;        // 当前、下一个回放文件索引
        private int iFileGroupLen = 0;                          // 回放文件组的总数量
        private int iPerGroupLen = 1;                           // 每组图像包含的文件数量（普通1，光度立体6）
        private string[][] strFiles = new string[maxCameraNum][]; // 回放文件路径数组
         // 错误日志缓存（最近20条）
        private readonly List<string> _recentErrorLogs = new List<string>();
        private readonly object _errorLogLock = new object();

        // 5. 字段 - 线程
        private Thread contThread = null, oneTimeThread = null; // 连续、单次运行线程
        private Thread UpdateThread = null;                     // 更新VP记录清单线程
        private Thread fxRestThread = null;                     // 光度立体重构线程
        private Thread UIStartThread = null;                    // 启动界面线程
        private Thread hikGrabThread = null;                    // 海康相机图像采集线程
        private Thread LaserMarkThread = null;                  // 激光标刻线程
        private Thread jczPipThread = null;                     // 金橙子板卡管道通信线程
        private Thread queryEntThread = null;                   // 查询实体信息线程
        private Thread ReLoadThread = null;                     // 重新加载资源线程

        // 6. 字段 - 相机采集
        private bool chooseHIK, hikCanGrab;                     // 是否选用海康相机、是否允许采集
        private ImgAcqFIFO acqFIFO = new ImgAcqFIFO();          // 图像采集FIFO封装类
        private HIKVisionCamera hIKVisionCamera = new HIKVisionCamera(); // 海康相机操作类
        private MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX(); // 帧信息
        private MyCamera[] camera2 = null;                      // 相机设备对象数组
        private MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE(); // 整数值参数
        private MyCamera.MV_FRAME_OUT stFrameInfo1 = new MyCamera.MV_FRAME_OUT(); // 帧信息（备用）
        private int nRet = 0;                                   // SDK返回值
        private UInt32 nPayloadSize = 0;                        // 单帧图像数据字节数
        private int iCamType = 0;                               // 相机类型
        private List<string> items;                             // 枚举到的相机名称列表
        private IntPtr[] m_BufForDriverS = new IntPtr[maxCameraNum]; // 驱动缓冲区指针数组
        private UInt32[] nPayloadSizeS = new UInt32[maxCameraNum];   // 每个相机的载荷大小数组
        private UInt32[] m_nBufSizeForDriver = new UInt32[maxCameraNum]; // 每个相机缓冲区大小数组
        private int mainWindWidth, mainWindHeight;              // 主窗口显示区域尺寸
        private IntPtr m_BufForDriver;                          // 单相机驱动缓冲区指针（未使用）
        
        private DateTime _lastReconnectAttempt = DateTime.MinValue;
        private bool _isReconnecting = false;
        private readonly TimeSpan _reconnectInterval = TimeSpan.FromSeconds(30); // 重试间隔
        private readonly object _cameraConnectionSync = new object();
        private bool _cameraConnectionInProgress;
        private volatile bool _cameraManuallyDisconnected;
        private readonly bool[] _cameraAutoExposure = new bool[maxCameraNum];
        private readonly bool[] _cameraGammaReadable = new bool[maxCameraNum];

        // 7. 字段 - 状态标志
        private uint cameraNum = 0;                             // 实际搜索到的相机数量
        private int iUsedCamraNum = 2;                          // 实际启用的相机数量
        private int[] iEventStatus = new int[6];                // 各相机事件状态
        private string[] strStatus = new string[12];            // 统计信息字符串数组
        private int iShowType = 0;                              // 显示类型（0默认/1灰度/2光度/3PMA）
        private int iCurrentIndex, iNextIndex;                  // 当前、下一个图像索引（IDB模式）
        private int iLayoutType = 1;                            // 窗口布局类型
        private bool bVPPParamChanged = false;                  // VisionPro参数改变标志
        private RunTimeStatus rts = new RunTimeStatus();        // 运行时状态
        private AlignImParam[] alImParam = new AlignImParam[2];    // 图像对齐参数
        private CDL cdl = new CDL();                            // 深度学习推理类
        private ResultInfo strResInfo = new ResultInfo();       // 结果信息
        private DetStat detStat = new DetStat();                // 检测统计

        // 8. 字段 - PMAlign编辑
        private bool bPMAEdit = false;                          // PMAlign编辑模式标志
        private int iTmpType = 0;                               // 临时任务类型
        private bool bTrig = false;                             // 触发模式标志
        private CogPMAlignEditV2[] pmaV2 = new CogPMAlignEditV2[2]; // PMAlign编辑控件

        // 9. 字段 - Blob编辑
        private CogBlobEditV2 blobV2;                           // Blob分析编辑控件
        private bool bBlobEdit = false;                         // Blob编辑模式标志

        // 10. 字段 - 其他标志
        private bool bVPWinAttached = false;                    // VisionPro控件是否已添加到窗体
        private bool bLoadFinish = false;                       // 主窗体加载完成标志
        private bool bfirstTime = true;                         // 定时器首次触发标志
        private bool bSingleFrameModel = false;                 // 单帧模式标志
        private bool bLoadVPRes = false;                        // 是否直接加载已有VP结果
        private bool bCrackPlayback = false;                    // 回放破解模式标志
        private string strPlaybackFileList = "";                // 回放文件列表路径
        private int iProcess = 0;                               // 启动进度百分比
        private System.Timers.Timer timerIOIn;                  // IO输入检测定时器
        private List<int> comInd = new List<int>();              // 排序后的串口COM号列表
        private readonly bool[] serialConfigErrorLogged = new bool[4]; // 防止定时重连重复记录同一配置错误

        // 11. 字段 - 统计相关
        private int iAcquisitionsTimes = 0;                     // 图像采集总次数
        private int iErrAcquisitionsTimes = 0;                  // 采集失败次数
        private int iErrExecuteTimes = 0;                       // 检测执行失败次数
        private readonly object warningMessageSync = new object();
        private DateTime lastTaskBusyWarningTime = DateTime.MinValue;
                                                                // 当前选择的作业索引                                                        // 当前选择的作业索引

        // 12. 字段 - 标定相关
        private PMARes[] pmaRes = new PMARes[maxCameraNum];     // PMAlign模板匹配结果
        private string strLogPath = "";                         // 日志文件保存路径
        private string strOrder = "";                           // 当前执行的指令
        private int icamSelectInd = -1;                         // 当前选中的相机索引（UI用）
        private readonly List<int> cameraListPhysicalIndices = new List<int>(); // 相机列表项对应的物理相机索引
        private readonly List<int> cameraListJobIndices = new List<int>();      // 相机列表项对应的工位索引
        private readonly bool[,] lightOnStates = new bool[lightControllerSlotCount, lightChannelCount]; // 控制器槽×CH1~CH4最近一次成功状态
        private readonly object ioFeedbackSync = new object();
        private readonly bool[] ioFeedbackResult = new bool[ioFeedbackJobCount];
        private readonly bool[] ioFeedbackReady = new bool[ioFeedbackJobCount];
        private readonly bool[] ioFeedbackStarted = new bool[ioFeedbackJobCount];
        private readonly bool[] ioFeedbackParticipating = new bool[ioFeedbackJobCount];
        private readonly long[] ioFeedbackSingleJobCycles = new long[ioFeedbackJobCount];
        private readonly bool[] ioFeedbackSingleJobOutputInProgress = new bool[ioFeedbackJobCount];
        private bool ioFeedbackCycleActive = false;
        private bool ioFeedbackOutputInProgress = false;
        private int manualSingleRunActive = 0;
        private long ioFeedbackCycleId = 0;
        private PMAlignParam pmaParam = new PMAlignParam();     // PMAlign参数
        private double[] dCalibPoint = new double[36];          // 标定点数据（图像坐标+物理坐标）
        private double[] dCalibPointIm = new double[18];        // 图像坐标（9点）
        private double[] dCalibPointLaser = new double[18];     // 激光坐标（9点）
        private calibBlobParam cbp = new calibBlobParam();      // Blob标定参数
        private CalibRes cr = new CalibRes();                   // 标定计算结果
        private int featurePointNum = 9;                        // 特征点数量
        private LoadingForm loadingForm;                        // 加载提示窗体
        private int iEntIndex = -1;                             // 实体索引

        // 13. 委托
        private delegate void delegateUIUpdate();                // 委托：更新UI
        private delegateUIUpdate updateUItmp;                   // 委托实例（未初始化，默认为null）
                                                                // ★★★ 新增：静默运行标志 ★★★
        private bool _skipStatUpdateForCurrentRun = false;


        bool bUserLaser = false;                                    // 用户是否使用激光标刻
        #endregion

        // 14. 构造函数
        public FrmMainUI()
        {            
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            Text = ApplicationName;
            DisableUnusedCameraControls();
            hIKVisionCamera.CameraDiagnostic += detail => LogCameraConnection("相机SDK", false, detail);
            InitializeIOFeedbackMenu();
            tabControl1.TabPages.Remove(tabPageBase); // 主界面不显示未使用的模板匹配参数页
            InitUserParam(); 
        }

        private void InitializeIOFeedbackMenu()
        {
            // 菜单可见性由WinForms Designer维护；这里只调整现有菜单项的业务含义。
            menuTask_OK.Text = "IO组合输出测试...";
            menuTask_NG.Text = "Y1～Y8全部关闭";
            menuTask_NG2.Visible = false;
        }

        // 15. 初始化方法
        #region 初始化相关
        //保存用户参数
        private void SaveUserParam()
        {
            try
            {
                bool bStatus = FileOper.SaveUserParam(cup);
                if (bStatus)
                    operLog.iEventStatus = 0;
                else
                    operLog.iEventStatus = -1;
            }
            catch
            {
                operLog.iEventStatus = -1;
            }

        }
         
        //窗口布局初始化  主窗体加载以及窗体大小调整时调用
        private void ViewLayout()
        {
            mainWindWidth = splitContainer2.Panel1.Width;
            mainWindHeight = splitContainer2.Panel1.Height;
            splitContainer4.SplitterDistance = 100;
         //   splitContainer5.Panel1Collapsed = false;
            //**20220409  主窗口大小调整
            if (vpWin != null)
            {
                vpWin.Width = mainWindWidth;
                vpWin.Height = mainWindHeight;
                if (iJobIndex == 0 || iJobIndex == 1 || iJobIndex == -1)
                    vpWin.SetWorkstationDisplayMode(iJobIndex);
                else
                    vpWin.ViewLayout(iLayoutType); //调整控件分布
            }            
        } 
      
         //2025年10月4日 
        private string VPPinitErr(int iErrType)
        {
            string strErr = "";
            switch(iErrType)
            {
                case 1:
                    strErr = "未能找到文件";
                    break;
                case 2:
                    strErr = "密码文件错误";
                    break;
                case 3:
                    strErr = "文件序列化错误";
                    break;
                case 4:
                    strErr = "AcqFifo状态异常"; 
                    break;
                case 5:
                    strErr = "VPP长度测量接口不匹配";
                    if (vpWin != null && !string.IsNullOrWhiteSpace(vpWin.VppInterfaceValidationError))
                        strErr += "：" + vpWin.VppInterfaceValidationError;
                    break;
                 default:
                    strErr = "未知原因";
                    break;
            }
            return strErr;
        }

        private static void WriteStartupLog(string message)
        {
            try
            {
                string logDir = "UserData";
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                string logPath = Path.Combine(logDir, "dl_init_debug.log");
                File.AppendAllText(
                    logPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + Environment.NewLine);
            }
            catch
            {
            }
        }
        
        private void LoadVPFile(bool bReLoad)  //2025年10月4日  修改
        {
            string strVPPFile = "";
            string configuredVPP = cup.p_Paf.strVPPFilePath;
            string vppFileName = Path.GetFileName(configuredVPP);
            string bundledVPP = string.IsNullOrWhiteSpace(vppFileName)
                ? ""
                : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VPP", vppFileName));

            WriteStartupLog("[VPP] load start");
            WriteStartupLog("[VPP] configured=" + (configuredVPP ?? ""));
            WriteStartupLog("[VPP] bundled=" + bundledVPP);

            // 优先使用用户配置的完整路径；配置文件不存在时才回退到程序 bin\VPP。
            if (!string.IsNullOrWhiteSpace(configuredVPP) && File.Exists(configuredVPP))
            {
                strVPPFile = Path.GetFullPath(configuredVPP);
                rts.initStatus.bVPPFileExist = true;
            }
            else if (!string.IsNullOrEmpty(bundledVPP) && File.Exists(bundledVPP))
            {
                strVPPFile = bundledVPP;
                rts.initStatus.bVPPFileExist = true;
            }
            else
                rts.initStatus.bVPPFileExist = false;

            WriteStartupLog("[VPP] selected=" + strVPPFile);

            if (rts.initStatus.bVPPFileExist)  //文件存在
            {
                try
                {
                    if (bReLoad)
                    {
                        vpWin.ReLoadVisionControl(strVPPFile);
                    }
                    else
                        this.vpWin = new VisionControl.VisionControl(strVPPFile);
                   
                    rts.initStatus.iVPPStatus = vpWin.mInitError; //vpp文件初始化错误代码
                                                                  //    vpWin.cdl = cdl;
                    if(rts.initStatus.iVPPStatus==0)
                    {
                        rts.initStatus.bVPPInit = true;  //vpp文件完成初始化
                        WriteStartupLog("[VPP] load success");
                        if (!bVPWinAttached)
                        {
                            bVPWinAttached = true;
                            this.splitContainer2.Panel1.Controls.Add(this.vpWin);
                           /* this.vpWin.Dock = DockStyle.Fill;
                            this.vpWin.BringToFront();*/
                        }
                        vpWin.cdl = cdl;
                        this.vpWin.Location = new System.Drawing.Point(-2, 6);
                        this.vpWin.Name = "XB视图";
                 //       this.vpWin.Size = new System.Drawing.Size(393, 357);
                        this.vpWin.TabIndex = 2;
                         
                    }
                    else
                    {
                        rts.initStatus.bVPPInit = false;
                        WriteStartupLog("[VPP] load failed; status=" + rts.initStatus.iVPPStatus
                            + "; reason=" + VPPinitErr(rts.initStatus.iVPPStatus));
                        operLog.strOperEvent = "加载视觉算法文件";
                        operLog.iEventStatus = -1;
                        operLog.strFailReason =  VPPinitErr(rts.initStatus.iVPPStatus);
                        AddLog();
                    }
                    strEngineerName = Path.GetFileName(cup.p_DL.modelPath);
                    //  toolStripStatus_Engineer.Text = "机种:" + strEngineerName;
                    if (cup.p_VPPRTP.bVisionInfer)
                    {
                        this.Text = ApplicationName + "   " + strVPPFile;
                    }
                    else
                        this.Text = ApplicationName + "   " + cup.p_DL.modelPath;

                }
                catch (Exception ex)
                {
                    rts.initStatus.bVPPInit = false;
                    WriteStartupLog("[VPP] load failed; exception=" + ex.GetType().FullName + ": " + ex.Message);
                    operLog.strOperEvent = "加载视觉算法文件";
                    operLog.iEventStatus = -1;
                    operLog.strFailReason =  VPPinitErr(rts.initStatus.iVPPStatus);
                    AddLog();
                }

            }
            else    //**2023年7月30日  //文件不存在
            {
                WriteStartupLog("[VPP] load failed; file not found");

                this.vpWin = new VisionControl.VisionControl();
                
                if(cup.p_VPPRTP.bVisionInfer)
                {
                    rts.initStatus.bVPPInit = false;  //vpp文件完成初始化
                    rts.initStatus.iVPPStatus = vpWin.mInitError; //vpp文件初始化错误代码
                                                                  //   vpWin.cdl = cdl;
                                                                  //  this.splitContainer3.Panel1.Controls.Add(this.vpWin);
                    this.vpWin.Location = new System.Drawing.Point(-2, 6);
                    this.vpWin.Name = "XB视图";
                    this.vpWin.Size = new System.Drawing.Size(393, 357);
                    this.vpWin.TabIndex = 2;

                    operLog.strOperEvent = "加载视觉算法文件";
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "未能找到文件";
                    AddLog();
                }
                else if(cup.p_VPPRTP.bDeepInfer)
                {
                    
                    this.splitContainer2.Panel1.Controls.Add(this.vpWin);
                    /*this.vpWin.Dock = DockStyle.Fill;
                    this.vpWin.BringToFront();*/

                    vpWin.cdl = cdl;
                    this.vpWin.Location = new System.Drawing.Point(-2, 6);
                    this.vpWin.Name = "XB视图";
                    //       this.vpWin.Size = new System.Drawing.Size(393, 357);
                    this.vpWin.TabIndex = 2;
                }
                
            }
        }

        //采集类初始化
        private void InitAcqFIFOParam()
        {  
            acqFIFO._HikCam = camera2;
            acqFIFO.CogFlipRotateImInit();
        }
        //初始化VisionControl
        private void InitVisionControl()
        {
            WriteStartupLog("[VisionControl] init start");
            try
            {
                csd.p_LS = cup.p_LS;
                csd.p_LS2 = cup.p_LS2;
                InitAcqFIFOParam();
                acqFIFO._cs = rts.cs;
                acqFIFO._cup = cup;
                //    acqFIFO.InitFX();
                acqFIFO._csd = csd;
                // acqFIFO.cdl = cdl;
                acqFIFO.SetCameraStatusGetter(() => rts.cs);
                vpWin._iJobIndex = iJobIndex;
                vpWin._cup = cup;
                vpWin.SetGUIShow(0);
                vpWin._cameraNum = cameraNum;
                vpWin.SetAcqFIFO(acqFIFO);
                vpWin.JobResultReady -= OnJobResultReady;
                vpWin.JobResultReady += OnJobResultReady;
                vpWin.PreviewStatusChanged -= OnPreviewStatusFromVP;
                vpWin.PreviewStatusChanged += OnPreviewStatusFromVP;
                vpWin.InitBG();
                operLog.iEventStatus = 0;

                // 订阅视觉错误事件
                if (vpWin != null)
                {
                    vpWin.VisionErrorOccurred -= OnVisionErrorFromVP; // 防止重复订阅
                    vpWin.VisionErrorOccurred += OnVisionErrorFromVP;
                }
                // 订阅 AcqFIFO 错误事件
                acqFIFO.ErrorOccurred += OnAcqFIFOError;
                WriteStartupLog("[VisionControl] init completed");
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                WriteStartupLog("[VisionControl] init failed; exception=" + ex.GetType().FullName + ": " + ex.Message);
            }
           
        }
        // 事件处理方法（直接打日志）
        private void OnVisionErrorFromVP(int jobIndex, string errorMessage)
        {
            // AddLog 内部已处理跨线程 Invoke，可直接调用
            operLog.iEventStatus = -1;
            operLog.strOperEvent = jobIndex >= 0 && jobIndex < maxJobNum
                ? $"视觉工位{jobIndex + 1}异常"
                : "视觉操作异常";
            operLog.strFailReason = errorMessage;
            AddLog();
        }

        private void OnPreviewStatusFromVP(int jobIndex, int cameraIndex, int status, string message)
        {
            WriteCameraDiagnostic("实时预览", status, message);
            ShowPreviewStatusOnUi(jobIndex, cameraIndex, status, message);
        }

        private void ShowPreviewStatusOnUi(int jobIndex, int cameraIndex, int status, string message)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<int, int, int, string>(ShowPreviewStatusOnUi),
                        jobIndex, cameraIndex, status, message);
                }
                catch (InvalidOperationException)
                {
                    // 窗体关闭过程中不再写入界面日志。
                }
                return;
            }

            operLog.iEventStatus = status;
            operLog.strOperEvent = $"实时预览-工位{jobIndex + 1}/相机{cameraIndex + 1}";
            operLog.strFailReason = message;
            AddLog();
        }
        private void OnAcqFIFOError(object sender, string errorMessage)
        {
            // 调用日志记录（AddLog 内部已处理跨线程）
            operLog.iEventStatus = -1;
            operLog.strOperEvent = "采集错误";
            operLog.strFailReason = errorMessage;
            AddLog();
        }
        //初始化深度学习模型
        //初始化深度学习模型
        private int InitDL()
        {
            int iRet = -1;
            operLog.strOperEvent = "加载深度学习模型";
            if (Directory.Exists(cup.p_DL.modelPath))
            {
                rts.initStatus.bDLModelPathExist = true;
                try
                {
                    iRet = cdl.InitDL(cup.p_DL);
                    if (iRet != 0)
                    {
                        rts.initStatus.bDLModelInit = false;
                        cup.p_VPPRTP.bDeepInfer = false;
                        menuTask_DeepInfer.Checked = false;
                        operLog.strFailReason = "深度推理模型初始化异常";
                    }
                    else
                    {
                        rts.initStatus.bDLModelInit = true;
                        // ========== 新增：加载类别文件并设置给 cdl ==========
                        string classFilePath = Path.Combine(cup.p_DL.modelPath, "inference_model", "predefined_classes.txt");
                        if (!File.Exists(classFilePath))
                            classFilePath = Path.Combine(cup.p_DL.modelPath, "predefined_classes.txt");
                        if (File.Exists(classFilePath))
                        {
                            List<string> classNames = File.ReadAllLines(classFilePath)
                                .Where(line => !string.IsNullOrWhiteSpace(line))
                                .Select(line => line.Trim())
                                .ToList();
                            cdl.SetClassNames(classNames);
                        }
                        else
                        {
                            operLog.iEventStatus = -1;
                            operLog.strFailReason = "未找到类别文件 predefined_classes.txt";
                            AddLog();
                        }
                    }
                }
                catch
                {
                    rts.initStatus.bDLModelInit = false;
                    cup.p_VPPRTP.bDeepInfer = false;
                    menuTask_DeepInfer.Checked = false;
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "深度推理模型初始化异常";

                }

            }
            else
            {
                rts.initStatus.bDLModelInit = false;
                cup.p_VPPRTP.bDeepInfer = false;
                rts.initStatus.bDLModelPathExist = false;
                menuTask_DeepInfer.Checked = false;
                operLog.iEventStatus = -1;
                operLog.strFailReason = "未找到深度推理模型文件";
                // operLog.strOperEvent = "查找深度学习模型文件";
            }

            if (iRet == 0)
            {
                operLog.iEventStatus = 0;
            }
            else
            {
                operLog.iEventStatus = -1;
            }
            AddLog();
            return iRet;
        }
        private void WarmupDL()
        {
            string failReason = "未找到已启用且相机映射、图像尺寸有效的Job0/Job1";
            for (int jobIndex = 0; jobIndex < maxJobNum; jobIndex++)
            {
                if (cup.p_CD.bListProcess == null || jobIndex >= cup.p_CD.bListProcess.Length)
                {
                    WriteStartupLog($"[Warmup] skip; jobIndex={jobIndex}; 工位启用状态数组无效");
                    continue;
                }
                if (!cup.p_CD.bListProcess[jobIndex])
                    continue;

                int cameraIndex = cup.p_CD.camInd != null && jobIndex < cup.p_CD.camInd.Length
                    ? cup.p_CD.camInd[jobIndex] : -1;
                if (cameraIndex < 0 || cameraIndex >= maxCameraNum || cup.p_Cam == null || cameraIndex >= cup.p_Cam.Length)
                {
                    WriteStartupLog($"[Warmup] skip; jobIndex={jobIndex}; cameraIndex={cameraIndex}; 相机映射无效");
                    continue;
                }
                uint width = cup.p_Cam[cameraIndex].iWidth;
                uint height = cup.p_Cam[cameraIndex].iHeight;
                if (width == 0 || height == 0 || width > int.MaxValue || height > int.MaxValue ||
                    (long)width * height > int.MaxValue / 3)
                {
                    WriteStartupLog($"[Warmup] skip; jobIndex={jobIndex}; cameraIndex={cameraIndex}; 图像尺寸无效: {width}x{height}");
                    continue;
                }

                WriteStartupLog($"[Warmup] selected; jobIndex={jobIndex}; cameraIndex={cameraIndex}; width={width}; height={height}");
                bool success = cdl.det_infer_img((int)width, (int)height);
                operLog.strOperEvent = "启动推理预热";
                operLog.iEventStatus = success ? 0 : -1;
                operLog.strFailReason = success ? "" : $"jobIndex={jobIndex}, cameraIndex={cameraIndex}，预热失败，详见UserData/dl_init_debug.log";
                AddLog();
                return;
            }
            WriteStartupLog("[Warmup] skipped; " + failReason);
            operLog.strOperEvent = "启动推理预热";
            operLog.iEventStatus = -1;
            operLog.strFailReason = failReason;
            AddLog();
        }
        //工程拷贝后可能存在盘符不一致情况，修改路径盘符
        private void ReplaceDrive()
        {
            string oldVppPath = cup.p_Paf.strVPPFilePath;
            string oldConSavePath = cup.p_Paf.strConSavePath;
            string oldErrPath = cup.p_Paf.strErrPath;
            string oldExportPath = cup.p_Paf.strExportPath;
            string oldLogPath = cup.p_Paf.strLogPath;
            string oldPlaybackPath = cup.p_Paf.strPlaybackPath;
            string oldResSavePath = cup.p_Paf.strResSavePath;
            string oldSigSavePath = cup.p_Paf.strSigSavePath;

            cup.p_Paf.strVPPFilePath = ReplaceDriveIfExists(oldVppPath, true);
            cup.p_Paf.strConSavePath = ReplaceDriveIfExists(oldConSavePath, false);
            cup.p_Paf.strErrPath = ReplaceDriveIfExists(oldErrPath, false);
            cup.p_Paf.strExportPath = ReplaceDriveIfExists(oldExportPath, false);
            cup.p_Paf.strLogPath = ReplaceDriveIfExists(oldLogPath, false);
            cup.p_Paf.strPlaybackPath = ReplaceDriveIfExists(oldPlaybackPath, false);
            cup.p_Paf.strResSavePath = ReplaceDriveIfExists(oldResSavePath, false);
            cup.p_Paf.strSigSavePath = ReplaceDriveIfExists(oldSigSavePath, false);

            if (!string.Equals(oldVppPath, cup.p_Paf.strVPPFilePath, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(oldConSavePath, cup.p_Paf.strConSavePath, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(oldErrPath, cup.p_Paf.strErrPath, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(oldExportPath, cup.p_Paf.strExportPath, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(oldLogPath, cup.p_Paf.strLogPath, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(oldPlaybackPath, cup.p_Paf.strPlaybackPath, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(oldResSavePath, cup.p_Paf.strResSavePath, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(oldSigSavePath, cup.p_Paf.strSigSavePath, StringComparison.OrdinalIgnoreCase))
            {
                SaveUserParam();
            }
        }

        private string ReplaceDriveIfExists(string path, bool isFile)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                !Path.IsPathRooted(path) ||
                path.Length < 3 ||
                path[1] != ':')
            {
                return path;
            }

            bool originalExists = isFile ? File.Exists(path) : Directory.Exists(path);
            if (originalExists)
                return path;

            string currentRoot = Path.GetPathRoot(Environment.CurrentDirectory);
            string sourceRoot = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(currentRoot) ||
                string.IsNullOrEmpty(sourceRoot) ||
                string.Equals(currentRoot, sourceRoot, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            string candidate = currentRoot + path.Substring(sourceRoot.Length);
            bool candidateExists = isFile ? File.Exists(candidate) : Directory.Exists(candidate);
            return candidateExists ? candidate : path;
        }
        //初始化用户参数
        private void InitUserParam()
        {
            FileOper.MigrateOldBinaryIfNeeded();
            ReadUserParam();

            //初始化运行时状态，开机可用
            {
                cup.p_VPPRTP.bRecord = false;
                 
            }
            ReplaceDrive();  //目录不存在时的处理
            //      cup.p_VPPRTP.iXBType = 0;
            iUsedCamraNum = 0;

            for (int i = 0; i < maxCameraNum; i++)
            {
                if (cup.p_CD.bListProcess[i])
                    iUsedCamraNum++;
            }
            iLayoutType = cup.p_VPPRTP.iLayoutType;
            if (iLayoutType != 1 && iLayoutType != 2 && iLayoutType != 3 && iLayoutType != 4 && iLayoutType != 5)
                iLayoutType = 3;
            rts.cs.iStatus = new int[maxCameraNum];
            serialInd = cup.p_SD.serInd;

            switch (iJobIndex)
            {
                case 0:
                    menuWS_Job1.Checked = true;
                    break;
                case 1:
                    menuWS_Job2.Checked = true;
                    break;
                case 9:
                    menuWS_Job3.Checked = true;
                    break;
            }
            UpdateJobMenuText();
            rts.onLS.iFeedbackType = cup.p_VPPRTP.iFeedbackType;
            switch (rts.onLS.iFeedbackType)
            {
                case 1:
                    menuTask_FeedBack_Det.Checked = true;
                    break;
                case 2:
                    menuTask_FeedBack_OK.Checked = true;
                    break;
                case 3:
                    menuTask_FeedBack_NG.Checked = true;
                    break;
                case 0:
                    menuTask_FeedBack_NO.Checked = true;
                    break;
                case 5:
                    menuTask_FeedBack__Direct.Checked = true;
                    break;
                case 6:
                    menuTask_FeedBack_NG2.Checked = true;
                    break;
            }
        }

        private void ReadUserParam()
        {
            bool bUserParamInit;
            try
            {
                bUserParamInit = fo.ReadUserParam(ref cup);

            }
            catch
            {
                bUserParamInit = false;
            }
            string strPath = cup.p_Paf.strLogPath + "\\RTS_0000.txt";
            fo.ReadRunTimeStatus(strPath, ref rts);
            rts.initStatus.bSerInit = false; // 本轮串口状态由 InitSerial 重新确认，不能沿用历史连接状态。
            rts.initStatus.bUserParamFileInit = bUserParamInit;
            detStat = rts.detS;
            

        }
    

        // 验证相机是否都处于就位状态
        private void CheckCamPrepared()
        {
            rts.initStatus.bCamPrepared = true;
            for (int i = 0; i < maxCameraNum; i++)
            {
                if (!cup.p_CD.bListProcess[i])
                    continue;

                int cameraIndex = cup.p_CD.camInd[i];
                if (cameraIndex < 0 || rts.cs.iStatus == null || cameraIndex >= rts.cs.iStatus.Length)
                {
                    rts.initStatus.bCamPrepared = false;
                    continue;
                }

                if (rts.cs.iStatus[cameraIndex] < 3)
                    rts.initStatus.bCamPrepared = false;
            }
            if (!rts.initStatus.bCamPrepared)
            {
                //  toolBtSta_FromCam.Checked = false;
                //  toolBtSta_FromCam.Enabled = false;
                //  toolBt_Record.Enabled = false;
                //   cup.p_VPPRTP.bAcqFromCam = false;
            }
            else
            {
                rts.initStatus.bCamInit = true;
                toolBtSta_FromCam.Enabled = true;
                toolBt_Record.Enabled = true;
            }
        }
        //日期验证
        private void DateVerify(int iModelRole)
        {

          
            int iDateVer = mPasswordFile.DateVerify(iModelRole);

            if (iDateVer == 1)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "系统时间被篡改";
                AddLog();
                rts.iModelRole = -1;
            }
            else if (iDateVer == 2)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "授权已过期";
                AddLog();
                rts.iModelRole = -2;
            }
            else
            {
                rts.iModelRole = 0;
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "计算机已授权";
                AddLog();
            }
        }

        //获取本地回放数据
        private bool InitLocalFile()
        {
            int len = 0;
            int expectedGroupLen = -1;
            bool bRet = true;
            bool hasEnabledCamera = false;
            iFileGroupLen = 0;
            strFiles = new string[maxCameraNum][];
            acqFIFO.strFiles = strFiles;
            rts.initStatus.bLocalFileExist = false;
            rts.initStatus.bLocalFileRegular = false;
            operLog.strOperEvent = "回放数据检查";
            operLog.strFailReason = string.Empty;
            if (!Directory.Exists(cup.p_Paf.strPlaybackPath))
            {
                bRet = false;
                operLog.iEventStatus = -1;
                operLog.strFailReason = "回放数据目录不存在: " + cup.p_Paf.strPlaybackPath;
            }
            else
            {
                try
                {
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (cup.p_CD.bListProcess[i])
                        {
                            hasEnabledCamera = true;
                            string strPathDir = Path.Combine(
                                cup.p_Paf.strPlaybackPath,
                                cup.p_LI.strCamPrefix[i] ?? string.Empty);
                            if (!Directory.Exists(strPathDir))  //不存在子文件夹
                            {
                                bRet = false;
                                operLog.iEventStatus = -1;
                                operLog.strFailReason = "回放数据子目录不存在: " + strPathDir;
                            }
                            else
                            {
                                string[] allFiles = Directory.GetFiles(strPathDir, "*", SearchOption.AllDirectories);
                                strFiles[i] = allFiles
                                    .Where(file =>
                                        file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                        file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                        file.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                                    )
                                    .ToArray();

                                //     strFiles[i] = Directory.GetFiles(strPathDir, "*.bmp");


                                Array.Sort(strFiles[i]);

                                if (strFiles[i] != null && strFiles[i].Length > 0)
                                {
                                    if (iPerGroupLen <= 0 || strFiles[i].Length % iPerGroupLen != 0)
                                    {
                                        bRet = false;
                                        rts.initStatus.bLocalFileRegular = false;
                                        operLog.strFailReason = "回放图片数量无法组成完整图片组: " + strPathDir;
                                    }
                                    else
                                    {
                                        int currentGroupLen = strFiles[i].Length / iPerGroupLen;
                                        if (expectedGroupLen < 0)
                                        {
                                            expectedGroupLen = currentGroupLen;
                                            len = currentGroupLen;
                                        }
                                        else if (currentGroupLen != expectedGroupLen)
                                        {
                                            bRet = false;
                                            rts.initStatus.bLocalFileRegular = false;
                                            operLog.strFailReason = "各回放子目录图片数量不一致: " + strPathDir;
                                        }
                                        acqFIFO.cmAcqNum = 1;
                                    }
                                }
                                else
                                {
                                    bRet = false;
                                    operLog.strFailReason = "子目录图像文件不存在: " + strPathDir;
                                }
                            }

                        }
                    }
                    if (!hasEnabledCamera)
                    {
                        bRet = false;
                        operLog.strFailReason = "没有启用任何回放工位";
                    }
                    else if (expectedGroupLen <= 0)
                    {
                        bRet = false;
                        operLog.strFailReason = "回放图片组数量为 0";
                    }
                }
                catch (Exception ex)
                {
                    operLog.strFailReason = ex.Message;
                    bRet = false;
                }
            }

            if (!bRet)
            {
                operLog.iEventStatus = -1;
                AddLog();
            }
            else
            {
                iFileGroupLen = len;
                acqFIFO.strFiles = strFiles;
                rts.initStatus.bLocalFileExist = true;
                rts.initStatus.bLocalFileRegular = true;
                //   operLog.strOperEvent = "回放数据检查";
                operLog.iEventStatus = 0;
                //   AddLog();
            }


            try
            {
               // operLog.strOperEvent = "创建文件夹";
                if (!Directory.Exists(cup.p_Paf.strLogPath))
                {
                    Directory.CreateDirectory(cup.p_Paf.strLogPath);
                }
                if (!Directory.Exists(cup.p_Paf.strErrPath))
                {
                    Directory.CreateDirectory(cup.p_Paf.strErrPath);
                }
                /*if (!Directory.Exists(cup.p_Paf.strExportPath))
                {
                    Directory.CreateDirectory(cup.p_Paf.strExportPath);
                }*/
                if (!Directory.Exists(cup.p_Paf.strResSavePath))
                {
                    Directory.CreateDirectory(cup.p_Paf.strResSavePath);
                }
                operLog.iEventStatus = 0;
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = ex.Message;
            }
           /* if (operLog.iEventStatus == -1)
                AddLog();*/

            return bRet;
        }
         
        private bool InitLiveFile()
        {
            int len = 0;
            bool bRet = true;
            string strDate = DateTime.Now.ToString("yyyyMMdd");
            string strLivePath = cup.p_Paf.strConSavePath + "\\" + strDate + "\\";
            operLog.strOperEvent = "回放数据检查";
            if (!Directory.Exists(strLivePath))
            {
                rts.initStatus.bLocalFileExist = false;
                bRet = false;
                operLog.iEventStatus = -1;
                operLog.strFailReason = "实时数据目录不存在";
            }
            else
            {
                try
                {
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (cup.p_CD.bListProcess[i])
                        {
                            string strPathDir = strLivePath + cup.p_LI.strCamPrefix[i];
                            if (strLivePath.Substring(strLivePath.Length - 1, 1) != "\\")
                            {
                                strPathDir = strLivePath + "\\" + cup.p_LI.strCamPrefix[i];
                            }
                            //单帧模式
                            if (bSingleFrameModel)
                            {
                                strPathDir = strLivePath;
                            }

                            if (!Directory.Exists(strPathDir))  //不存在子文件夹
                            {
                                rts.initStatus.bLocalFileExist = false;
                                bRet = false;
                                operLog.iEventStatus = -1;
                                operLog.strFailReason = "回放数据子目录不存在";
                            }
                            else
                            {
                                strFiles[i] = Directory.GetFiles(strPathDir, "*.jpg");
                                Array.Sort(strFiles[i]);

                                if (strFiles[i] != null && strFiles[i].Length > 0)
                                {
                                    if (i == 0)
                                    {
                                        len = strFiles[i].Length / 6;
                                    }
                                    else
                                    {
                                        if (strFiles[i].Length / 6 == len) //兼容考虑，后期可去除5的情况
                                        {
                                            acqFIFO.cmAcqNum = 6;
                                        }
                                        else
                                        {
                                            acqFIFO.cmAcqNum = 5;
                                        }
                                    }
                                }
                                else   //没有图像文件
                                {
                                    rts.initStatus.bLocalFileRegular = false;
                                    bRet = false;
                                    operLog.strFailReason = "子目录图像文件不存在";
                                    //operLog.strOperEvent = "回放数据图像文件检查";
                                }
                            }

                        }
                    }
                    if (bRet)  //文件夹存在，图像文件存在，比较文件是否数量是否正常
                    {
                        for (int i = 0; i < maxCameraNum; i++)
                        {
                            if (cup.p_CD.bListProcess[i])
                            {
                                if (strFiles[i].Length % 6 != 0 || strFiles[i].Length / 6 != len)
                                {
                                    rts.initStatus.bLocalFileRegular = false;
                                    bRet = false;
                                    operLog.strFailReason = "子目录图像文件数量异常";
                                    // operLog.strOperEvent = "回放数据图像文件检查";
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    rts.initStatus.bLocalFileExist = false;
                    rts.initStatus.bLocalFileRegular = false;
                    // operLog.strOperEvent = "回放数据文件夹检查";
                    operLog.strFailReason = ex.Message;
                    bRet = false;
                }
            }

            if (!bRet)
            {
                operLog.iEventStatus = -1;
                AddLog();
            }
            else
            {
                iFileGroupLen = len;
                acqFIFO.strFiles = strFiles;

                //   operLog.strOperEvent = "回放数据检查";
                operLog.iEventStatus = 0;
                //   AddLog();
            }


            try
            {
                operLog.strOperEvent = "创建文件夹";
                if (!Directory.Exists(cup.p_Paf.strLogPath))
                {
                    Directory.CreateDirectory(cup.p_Paf.strLogPath);
                }
                if (!Directory.Exists(cup.p_Paf.strErrPath))
                {
                    Directory.CreateDirectory(cup.p_Paf.strErrPath);
                }
                if (!Directory.Exists(cup.p_Paf.strExportPath))
                {
                    Directory.CreateDirectory(cup.p_Paf.strExportPath);
                }
                if (!Directory.Exists(cup.p_Paf.strResSavePath))
                {
                    Directory.CreateDirectory(cup.p_Paf.strResSavePath);
                }
                operLog.iEventStatus = 0;
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = ex.Message;
            }
            if (operLog.iEventStatus == -1)
                AddLog();

            return bRet;
        }

        #endregion  初始化相关

        #region Hik摄像头相关

        // 添加缓存字段
        private Dictionary<int, CameraParamCache> _cameraParamCache = new Dictionary<int, CameraParamCache>();

        private class CameraParamCache
        {
            public uint LastSetWidth { get; set; } = 0;
            public uint LastSetHeight { get; set; } = 0;
            public int LastSetPixelFormat { get; set; } = -1;
            public float LastSetExposure { get; set; } = -1;
            public float LastSetGain { get; set; } = -1;
            public uint LastSetSharpness { get; set; } = uint.MaxValue;
            public float LastSetGamma { get; set; } = -1;
            public uint LastSetBrightness { get; set; } = uint.MaxValue;
            public uint[] LastSetROI { get; set; } = new uint[4];
        }

        /// <summary>
        /// 检查相机分辨率是否需要更新
        /// </summary>
        private bool NeedUpdateResolution(int iCamIndex, uint width, uint height)
        {
            if (!_cameraParamCache.ContainsKey(iCamIndex))
                return true;

            var cache = _cameraParamCache[iCamIndex];
            return cache.LastSetWidth != width || cache.LastSetHeight != height;
        }

        /// <summary>
        /// 检查相机像素格式是否需要更新
        /// </summary>
        private bool NeedUpdatePixelFormat(int iCamIndex, int monoType)
        {
            if (!_cameraParamCache.ContainsKey(iCamIndex))
                return true;

            return _cameraParamCache[iCamIndex].LastSetPixelFormat != monoType;
        }

        /// <summary>
        /// 更新相机参数缓存
        /// </summary>
        private void UpdateCameraParamCache(int iCamIndex)
        {
            if (!_cameraParamCache.ContainsKey(iCamIndex))
            {
                _cameraParamCache[iCamIndex] = new CameraParamCache();
            }

            var cache = _cameraParamCache[iCamIndex];
            cache.LastSetWidth = cup.p_Cam[iCamIndex].iWidth;
            cache.LastSetHeight = cup.p_Cam[iCamIndex].iHeight;
            cache.LastSetPixelFormat = cup.p_Cam[iCamIndex].iMonoType;
            cache.LastSetExposure = cup.p_Cam[iCamIndex].iExposeTime;
            cache.LastSetGain = (float)cup.p_Cam[iCamIndex].dGain;
            cache.LastSetSharpness = cup.p_Cam[iCamIndex].iSharp;
            cache.LastSetGamma = cup.p_Cam[iCamIndex].dGamma;
            cache.LastSetBrightness = cup.p_Cam[iCamIndex].iBrightness;
            Array.Copy(cup.p_Cam[iCamIndex].iROI, cache.LastSetROI, 4);
        }
        //自动搜索相机,查找相机的数量
        private void SearchCam()
        {
            try
            {
                // 相机数量
                cameraNum = hIKVisionCamera.CameraNum();
                if (cameraNum > 0)
                {
                    chooseHIK = true;
                    //   rts.initStatus.bCamInit = true;
                }
                else
                {
                    chooseHIK = false;
                    rts.initStatus.bCamInit = false;
                }


                // 枚举相机
            }
            catch
            {
                rts.initStatus.bCamInit = false;
                cameraNum = 0;
                rts.cs.camNum = 0;
                chooseHIK = false;
            }
            rts.cs.camNum = (int)cameraNum;
            if(vpWin!=null)
                vpWin._cameraNum = cameraNum;
        }

        private void InitCamLog()
        {
            for (int iCamInd = 0; iCamInd < maxCameraNum; iCamInd++)
            {
                if (rts.cs.iStatus[iCamInd] < 2)
                {
                    operLog.strOperEvent = rts.initStatus.strCamInit;
                    operLog.iEventStatus = -1;
                }
                else
                {
                    operLog.strOperEvent = items[iCamInd] + "-" + (iCamInd + 1) + "初始化成功;分辨率：" + cup.p_Cam[iCamInd].iWidth.ToString() + "×" + cup.p_Cam[iCamInd].iHeight.ToString();
                    operLog.iEventStatus = 0;
                }

                // 更新状态栏颜色
                if (rts.cs.iStatus[iCamInd] >= 2)
                    ((ToolStripStatusLabel)statusStrip1.Items[iCamInd + 1]).ForeColor = Color.Green;
                else if (rts.cs.iStatus[iCamInd] == 1)
                    ((ToolStripStatusLabel)statusStrip1.Items[iCamInd + 1]).ForeColor = Color.Orange;
                else
                {
                    if (cup.p_CD.bListProcess[iCamInd])
                        ((ToolStripStatusLabel)statusStrip1.Items[iCamInd + 1]).ForeColor = Color.Red;
                    else
                        ((ToolStripStatusLabel)statusStrip1.Items[iCamInd + 1]).ForeColor = Color.Gray;
                }

                if (!cup.p_CD.bListProcess[iCamInd])
                    continue;

                // 检查 AcqFIFO 中的缓冲区是否有效（替代原来的 m_BufForDriverS 检查）
                if (acqFIFO.GetBufferPtr(iCamInd) == IntPtr.Zero && iCamInd < cameraNum)
                {
                    operLog.strOperEvent = "相机初始化";
                    operLog.strFailReason = "获取图像帧缓冲失败";
                    operLog.iEventStatus = -1;
                    AddLog();
                }
                // 原代码在缓冲区有效时也会调用 AddLog()，但未设置 operLog 内容，会导致空日志，此处保留注释
                // else if (acqFIFO.GetBufferPtr(iCamInd) != IntPtr.Zero)
                // {
                //     AddLog();
                // }
            }
        }

        private void ProcessInitCamStatus(int iInitType)
        {
            switch (iInitType)
            {
                case 0:     // 0：正常初始化
                    rts.initStatus.bCamPrepared = true;
                    rts.initStatus.bCamInit = true;
                    rts.initStatus.strCamInit = "相机初始化成功";
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = "初始化相机";
                    AddLog();
                    break;
                case -1:
                    rts.initStatus.bCamPrepared = false;
                    rts.initStatus.bCamInit = false;
                    rts.initStatus.strCamInit = "没有可用相机";
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "初始化相机";
                    operLog.strFailReason = "没有可用相机";
                    AddLog();
                    break;
                case -2:        // 相机编号超出范围，已禁用相应任务
                    rts.initStatus.bCamPrepared = false;
                    rts.initStatus.bCamInit = false;
                    rts.initStatus.strCamInit = "启用的检测任务无有效相机";
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "初始化相机";
                    operLog.strFailReason = "没有启用的检测任务（所有任务配置的相机编号均无效）";
                    AddLog();
                    break;
                case -3:
                    rts.initStatus.bCamPrepared = false;
                    rts.initStatus.bCamInit = false;
                    rts.initStatus.strCamInit = "获取相机数据量大小失败";
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "初始化相机";
                    operLog.strFailReason = "获取相机数据量大小失败";
                    AddLog();
                    break;
                case -7:
                    rts.initStatus.bCamPrepared = false;
                    rts.initStatus.bCamInit = false;
                    rts.initStatus.strCamInit = "相机序列号映射失败";
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "初始化相机";
                    operLog.strFailReason = "A/B工位序列号缺失、重复或对应相机未连接";
                    AddLog();
                    break;
                    // ... 其他 case 保持不变 ...
            }

            // ✅ 更新相机列表信息（只显示启用的相机）
            cbDeviceList.Items.Clear();
            cameraListPhysicalIndices.Clear();
            cameraListJobIndices.Clear();
            for (int i = 0; i < maxCameraNum; i++)
            {
                // 只有当任务启用且相机索引有效时才添加到列表
                if (cup.p_CD.bListProcess[i] && cup.p_CD.camInd[i] < cameraNum && items.Count > cup.p_CD.camInd[i])
                {
                    cbDeviceList.Items.Add(items[cup.p_CD.camInd[i]]);
                    cameraListPhysicalIndices.Add(cup.p_CD.camInd[i]);
                    cameraListJobIndices.Add(i);
                }
            }

            // 如果有启用的相机，默认选中第一个
            if (cbDeviceList.Items.Count > 0)
            {
                cbDeviceList.SelectedIndex = 0;
            }

            // 更新状态栏显示
            for (int ind = 0; ind < maxCameraNum; ind++)
            {
                if (!cup.p_CD.bListProcess[ind])
                {
                    ((ToolStripStatusLabel)statusStrip1.Items[ind + 1]).ForeColor = Color.Gray;
                    continue;
                }

                int iCamInd = cup.p_CD.camInd[ind];
                if (iCamInd >= 0 && iCamInd < cameraNum)
                {
                    if (rts.cs.iStatus[iCamInd] == 3)
                        ((ToolStripStatusLabel)statusStrip1.Items[ind + 1]).ForeColor = Color.Green;
                    else if (rts.cs.iStatus[iCamInd] == 2)
                        ((ToolStripStatusLabel)statusStrip1.Items[ind + 1]).ForeColor = Color.Orange;
                    else
                        ((ToolStripStatusLabel)statusStrip1.Items[ind + 1]).ForeColor = Color.Red;
                }
                else
                {
                    ((ToolStripStatusLabel)statusStrip1.Items[ind + 1]).ForeColor = Color.Gray;
                }
            }
        }
        /// <summary>
        /// 初始化单个相机（用于轮询检测过程中重新连接相机）
        /// </summary>
        /// <param name="ind">工位索引（逻辑工位）</param>
        /// <returns>是否初始化成功</returns>
        /// <summary>
        /// 初始化单个相机（用于轮询检测过程中重新连接相机）
        /// </summary>
        /// <param name="ind">工位索引（逻辑工位）</param>
        /// <returns>是否初始化成功</returns>
        private bool InitJobCamInd(int ind)
        {
            List<string> latestItems;
            if (!RefreshCameraDevices("自动重连", out latestItems))
                return false;

            bool mappingChanged;
            bool mappingValid = TryMapJobsByCameraSerial(latestItems, out mappingChanged);
            if (mappingChanged)
                SaveUserParam();

            // 某一工位的绑定缺失时只拒绝该工位，不影响其他已经能唯一匹配的相机。
            return ConnectJobCamera(ind, latestItems, "自动重连", mappingValid);
        }

        private void LogCameraConnection(string operation, bool success, string detail)
        {
            WriteCameraDiagnostic(operation, success ? 0 : -1, detail);
            operLog.iEventStatus = success ? 0 : -1;
            operLog.strOperEvent = operation;
            operLog.strFailReason = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + detail;
            AddLog();
        }

        private readonly object _cameraDiagnosticLogSync = new object();
        private void WriteCameraDiagnostic(string operation, int status, string detail)
        {
            try
            {
                string directory = cup.p_Paf.strLogPath;
                if (string.IsNullOrEmpty(directory)) return;
                lock (_cameraDiagnosticLogSync)
                {
                    Directory.CreateDirectory(directory);
                    File.AppendAllText(Path.Combine(directory, $"CameraLog_{DateTime.Now:yyyyMMdd}.txt"),
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {operation} | status={status} | {detail}{Environment.NewLine}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"相机诊断日志写入失败: {ex}");
            }
        }

        /// <summary>
        /// 主动连接和自动重连共用的最新设备枚举入口。
        /// </summary>
        private bool RefreshCameraDevices(string operation, out List<string> latestItems)
        {
            latestItems = null;
            try
            {
                latestItems = hIKVisionCamera.EnumDevices();
                int enumRet = hIKVisionCamera.LastEnumResult;
                cameraNum = (uint)latestItems.Count;
                rts.cs.camNum = latestItems.Count;
                if (vpWin != null)
                    vpWin._cameraNum = cameraNum;

                if (enumRet != MyCamera.MV_OK)
                {
                    LogCameraConnection(operation, false,
                        $"设备枚举失败，SDK=0x{unchecked((uint)enumRet):X8}");
                    return false;
                }

                for (int i = 0; i < latestItems.Count; i++)
                {
                    LogCameraConnection(operation, true,
                        $"枚举设备 cameraIndex={i}, serial={GetCameraSerialKey(latestItems[i])}, info={latestItems[i]}");
                }

                if (latestItems.Count == 0)
                {
                    LogCameraConnection(operation, false, "未发现在线相机");
                    return false;
                }

                chooseHIK = true;
                return true;
            }
            catch (Exception ex)
            {
                cameraNum = 0;
                rts.cs.camNum = 0;
                LogCameraConnection(operation, false, "设备枚举异常: " + ex);
                return false;
            }
        }

        /// <summary>
        /// 按当前序列号映射连接一个工位。已就绪的对象不重复 Create/Open/Start。
        /// </summary>
        private bool ConnectJobCamera(int jobIndex, List<string> latestItems,
            string operation, bool mappingValid)
        {
            if (jobIndex < 0 || jobIndex >= maxJobNum || !cup.p_CD.bListProcess[jobIndex])
                return false;

            int cameraIndex = -1;
            bool createdNewConnection = false;
            try
            {
                EnsureCameraSerialBindingArray();
                string expectedSerial = GetCameraSerialKey(cup.p_CD.camSerial[jobIndex]);
                if (!string.IsNullOrEmpty(expectedSerial))
                {
                    for (int i = 0; i < latestItems.Count; i++)
                    {
                        if (string.Equals(expectedSerial, GetCameraSerialKey(latestItems[i]),
                            StringComparison.OrdinalIgnoreCase))
                        {
                            cameraIndex = i;
                            break;
                        }
                    }
                }
                else if (cup.p_CD.camInd[jobIndex] >= 0 && cup.p_CD.camInd[jobIndex] < latestItems.Count)
                {
                    cameraIndex = cup.p_CD.camInd[jobIndex];
                }

                if (cameraIndex < 0 || cameraIndex >= latestItems.Count)
                {
                    LogCameraConnection(operation, false,
                        $"工位Job{jobIndex}未找到唯一绑定相机，serial={expectedSerial}");
                    return false;
                }

                cup.p_CD.camInd[jobIndex] = cameraIndex;
                string currentSerial = GetCameraSerialKey(latestItems[cameraIndex]);
                if (string.IsNullOrEmpty(cup.p_CD.camSerial[jobIndex]))
                {
                    cup.p_CD.camSerial[jobIndex] = currentSerial;
                    SaveUserParam();
                }

                camera2 = hIKVisionCamera.GetCameraArray();
                if (cameraIndex < 0 || cameraIndex >= camera2.Length)
                    return false;

                bool connected = false;
                if (camera2[cameraIndex] != null)
                {
                    try
                    {
                        connected = hIKVisionCamera.IsCamConn(cameraIndex);
                    }
                    catch (Exception ex)
                    {
                        LogCameraConnection(operation, false, $"cameraIndex={cameraIndex}, serial={currentSerial}, phase=IsCamConn, exception={ex}");
                        connected = false;
                    }
                }
                if (connected)
                {
                    string activeSerial = GetCameraSerialKey(hIKVisionCamera.GetConnectedCameraSerial(cameraIndex));
                    if (!string.IsNullOrEmpty(activeSerial) && !string.IsNullOrEmpty(currentSerial) &&
                        !string.Equals(activeSerial, currentSerial, StringComparison.OrdinalIgnoreCase))
                    {
                        LogCameraConnection(operation, false,
                            $"拒绝重绑定以避免工位交换: Job{jobIndex}, slot={cameraIndex}, 已连接={activeSerial}, 期望={currentSerial}");
                        return false;
                    }

                    if (rts.cs.iStatus[cameraIndex] == 3)
                    {
                        if (!ReadCamParam(cameraIndex)) return false;
                        LogCameraConnection(operation, true,
                            $"设备已连接，保留现有句柄: Job{jobIndex}, cameraIndex={cameraIndex}, serial={currentSerial}");
                        return true;
                    }
                }

                int createRet;
                int openRet;
                int releaseStopRet;
                int releaseCloseRet;
                int releaseDestroyRet;
                if (vpWin != null && !vpWin.StopPreviewForCamera(cameraIndex))
                {
                    LogCameraConnection(operation, false,
                        $"cameraIndex={cameraIndex}, serial={currentSerial}, 旧预览线程未退出，拒绝重连及释放句柄");
                    return false;
                }
                // 先撤销旧就绪状态，避免后台重连发布新对象时预览抢先进入。
                SyncCameraStatus(cameraIndex, 0);
                bool connectedNow = hIKVisionCamera.TryConnectCamera(cameraIndex, cameraIndex,
                    out createRet, out openRet, out releaseStopRet, out releaseCloseRet, out releaseDestroyRet);
                camera2 = hIKVisionCamera.GetCameraArray();

                if (releaseStopRet != int.MinValue || releaseCloseRet != int.MinValue || releaseDestroyRet != int.MinValue)
                {
                    LogCameraConnection(operation,
                        releaseStopRet == MyCamera.MV_OK && releaseCloseRet == MyCamera.MV_OK && releaseDestroyRet == MyCamera.MV_OK,
                        $"释放旧句柄 cameraIndex={cameraIndex}, Stop=0x{unchecked((uint)releaseStopRet):X8}, Close=0x{unchecked((uint)releaseCloseRet):X8}, Destroy=0x{unchecked((uint)releaseDestroyRet):X8}");
                }

                if (!connectedNow)
                {
                    SyncCameraStatus(cameraIndex, 0);
                    LogCameraConnection(operation, false,
                        $"连接失败 cameraIndex={cameraIndex}, serial={currentSerial}, CreateDevice=0x{unchecked((uint)createRet):X8}, OpenDevice=0x{unchecked((uint)openRet):X8}");
                    return false;
                }

                createdNewConnection = !connected;
                bool configured = ReadCamParam(cameraIndex);
                if (!configured)
                {
                    SyncCameraStatus(cameraIndex, 0);
                    ReleaseNewCameraAfterFailure(cameraIndex, createdNewConnection, operation);
                    LogCameraConnection(operation, false,
                        $"参数读取失败 cameraIndex={cameraIndex}, serial={currentSerial}");
                    return false;
                }

                if (cup.p_CD.triggleCam == cameraIndex) SetTrigger(cameraIndex);
                MyCamera.MVCC_INTVALUE payloadParam = new MyCamera.MVCC_INTVALUE();
                int payloadRet = camera2[cameraIndex].MV_CC_GetIntValue_NET("PayloadSize", ref payloadParam);
                if (payloadRet != MyCamera.MV_OK || payloadParam.nCurValue == 0)
                {
                    SyncCameraStatus(cameraIndex, 0);
                    ReleaseNewCameraAfterFailure(cameraIndex, createdNewConnection, operation);
                    LogCameraConnection(operation, false,
                        $"获取PayloadSize失败 cameraIndex={cameraIndex}, SDK=0x{unchecked((uint)payloadRet):X8}");
                    return false;
                }

                SyncCameraStatus(cameraIndex, 1);
                if (!acqFIFO.ReallocateBuffer(cameraIndex, payloadParam.nCurValue))
                {
                    SyncCameraStatus(cameraIndex, 0);
                    ReleaseNewCameraAfterFailure(cameraIndex, createdNewConnection, operation);
                    LogCameraConnection(operation, false,
                        $"图像缓冲区分配失败 cameraIndex={cameraIndex}, PayloadSize={payloadParam.nCurValue}");
                    return false;
                }

                SyncCameraStatus(cameraIndex, 2);
                if (!StartGrab(cameraIndex))
                {
                    ReleaseNewCameraAfterFailure(cameraIndex, createdNewConnection, operation);
                    return false;
                }

                LogCameraConnection(operation, true,
                    $"连接成功 Job{jobIndex}, cameraIndex={cameraIndex}, serial={currentSerial}, CreateDevice=0x{unchecked((uint)createRet):X8}, OpenDevice=0x{unchecked((uint)openRet):X8}");
                return true;
            }
            catch (Exception ex)
            {
                LogCameraConnection(operation, false,
                    $"Job{jobIndex}, cameraIndex={cameraIndex}, serial={cup.p_CD.camSerial[jobIndex]}, 连接初始化异常: {ex}");
                if (cameraIndex >= 0)
                {
                    SyncCameraStatus(cameraIndex, 0);
                    ReleaseNewCameraAfterFailure(cameraIndex, createdNewConnection, operation);
                }
                return false;
            }
        }

        private void ReleaseNewCameraAfterFailure(int cameraIndex, bool createdNewConnection, string operation)
        {
            if (!createdNewConnection)
                return;

            int stopRet;
            int closeRet;
            int destroyRet;
            bool released = hIKVisionCamera.ReleaseCamera(cameraIndex, out stopRet, out closeRet, out destroyRet);
            LogCameraConnection(operation, released,
                $"清理本次新建句柄 cameraIndex={cameraIndex}, Stop=0x{unchecked((uint)stopRet):X8}, Close=0x{unchecked((uint)closeRet):X8}, Destroy=0x{unchecked((uint)destroyRet):X8}");
            camera2 = hIKVisionCamera.GetCameraArray();
        }

        /// <summary>
        /// 手动“连接相机”入口：只刷新枚举并补齐缺失/失效相机，不关闭已正常工作的相机。
        /// </summary>
        private bool TryConnectCameras(string operation)
        {
            lock (_cameraConnectionSync)
            {
                if (_cameraConnectionInProgress)
                {
                    LogCameraConnection(operation, false, "已有连接操作正在进行，忽略本次重复进入");
                    return false;
                }
                if (operation == "自动重连" && _cameraManuallyDisconnected) return false;
                if (operation == "手动连接") _cameraManuallyDisconnected = false;
                _cameraConnectionInProgress = true;
            }

            try
            {
                List<string> latestItems;
                if (!RefreshCameraDevices(operation, out latestItems))
                    return false;

                bool mappingChanged;
                bool mappingValid = TryMapJobsByCameraSerial(latestItems, out mappingChanged);
                if (mappingChanged)
                    SaveUserParam();

                camera2 = hIKVisionCamera.GetCameraArray();
                acqFIFO._HikCam = camera2;

                int successCount = 0;
                int enabledCount = 0;
                for (int jobIndex = 0; jobIndex < maxJobNum; jobIndex++)
                {
                    if (!cup.p_CD.bListProcess[jobIndex])
                        continue;
                    enabledCount++;
                    if (ConnectJobCamera(jobIndex, latestItems, operation, mappingValid))
                        successCount++;
                }

                bool allSucceeded = mappingValid && enabledCount > 0 && successCount == enabledCount;
                rts.initStatus.bCamPrepared = allSucceeded;
                rts.initStatus.bCamInit = allSucceeded;
                LogCameraConnection(operation, allSucceeded,
                    $"连接结果: 成功={successCount}, 启用工位={enabledCount}, 枚举设备={latestItems.Count}, 映射有效={mappingValid}");
                return allSucceeded;
            }
            finally
            {
                lock (_cameraConnectionSync)
                {
                    _cameraConnectionInProgress = false;
                }
            }
        }
       
        private static string GetCameraSerialKey(string cameraIdentity)
        {
            if (string.IsNullOrWhiteSpace(cameraIdentity))
                return string.Empty;

            string[] parts = cameraIdentity.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? string.Empty : parts[parts.Length - 1];
        }

        private void EnsureCameraSerialBindingArray()
        {
            int requiredLength = cup.p_CD.camInd != null ? cup.p_CD.camInd.Length : cup.p_Cam.Length;
            if (cup.p_CD.camSerial != null && cup.p_CD.camSerial.Length >= requiredLength)
                return;

            string[] bindings = new string[requiredLength];
            if (cup.p_CD.camSerial != null)
                Array.Copy(cup.p_CD.camSerial, bindings, cup.p_CD.camSerial.Length);
            cup.p_CD.camSerial = bindings;
        }

        /// <summary>
        /// 使用每个工位保存的硬件序列号生成本次运行的物理索引。
        /// 旧参数没有工位序列号时，只迁移一次，并确保同一物理相机不会分配给两个启用工位。
        /// </summary>
        private bool TryMapJobsByCameraSerial(List<string> deviceNames, out bool mappingChanged)
        {
            mappingChanged = false;
            EnsureCameraSerialBindingArray();

            string[] legacyPhysicalNames = new string[cup.p_Cam.Length];
            for (int i = 0; i < cup.p_Cam.Length; i++)
                legacyPhysicalNames[i] = cup.p_Cam[i].strCamName;

            Dictionary<string, int> serialToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            string[] enumeratedSerials = new string[deviceNames.Count];
            bool mappingValid = true;

            for (int i = 0; i < deviceNames.Count; i++)
            {
                string serial = GetCameraSerialKey(deviceNames[i]);
                enumeratedSerials[i] = serial;
                if (string.IsNullOrEmpty(serial) || serialToIndex.ContainsKey(serial))
                {
                    mappingValid = false;
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "相机序列号异常";
                    operLog.strFailReason = string.IsNullOrEmpty(serial)
                        ? $"无法从设备信息中提取序列号: {deviceNames[i]}"
                        : $"序列号 {serial} 出现多次";
                    AddLog();
                    continue;
                }
                serialToIndex.Add(serial, i);
            }

            bool[] usedPhysicalIndices = new bool[deviceNames.Count];
            bool[] mappedJobs = new bool[maxJobNum];

            // 第一阶段：优先恢复已经保存的工位序列号，枚举顺序变化不会改变工位归属。
            for (int jobIndex = 0; jobIndex < maxJobNum; jobIndex++)
            {
                if (!cup.p_CD.bListProcess[jobIndex])
                    continue;

                string savedSerial = GetCameraSerialKey(cup.p_CD.camSerial[jobIndex]);
                if (string.IsNullOrEmpty(savedSerial))
                    continue;

                if (!serialToIndex.TryGetValue(savedSerial, out int physicalIndex))
                {
                    mappingValid = false;
                    // 当前设备缺失时清除运行态物理索引，避免继续把该工位当成另一台在线相机。
                    // 不保存此临时缺失状态；设备恢复后重新枚举即可恢复绑定。
                    cup.p_CD.camInd[jobIndex] = -1;
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = $"工位{jobIndex + 1}相机映射失败";
                    operLog.strFailReason = $"未找到绑定序列号:{savedSerial}; 当前相机:{string.Join(",", enumeratedSerials)}";
                    AddLog();
                    continue;
                }

                if (usedPhysicalIndices[physicalIndex])
                {
                    mappingValid = false;
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = $"工位{jobIndex + 1}相机映射失败";
                    operLog.strFailReason = $"序列号 {savedSerial} 已被其他启用工位占用";
                    AddLog();
                    continue;
                }

                if (cup.p_CD.camInd[jobIndex] != physicalIndex ||
                    !string.Equals(cup.p_CD.camSerial[jobIndex], savedSerial, StringComparison.Ordinal))
                    mappingChanged = true;

                cup.p_CD.camInd[jobIndex] = physicalIndex;
                cup.p_CD.camSerial[jobIndex] = savedSerial;
                usedPhysicalIndices[physicalIndex] = true;
                mappedJobs[jobIndex] = true;
            }

            // 第二阶段：仅迁移没有工位序列号的旧参数。优先使用旧槽位中仍有效且未占用的序列号，
            // 否则分配唯一的剩余相机；迁移后以后启动不再依赖相机1/相机2编号。
            for (int jobIndex = 0; jobIndex < maxJobNum; jobIndex++)
            {
                if (!cup.p_CD.bListProcess[jobIndex] || mappedJobs[jobIndex] ||
                    !string.IsNullOrEmpty(GetCameraSerialKey(cup.p_CD.camSerial[jobIndex])))
                    continue;

                int physicalIndex = -1;
                int oldIndex = cup.p_CD.camInd[jobIndex];
                string legacySerial = oldIndex >= 0 && oldIndex < legacyPhysicalNames.Length
                    ? GetCameraSerialKey(legacyPhysicalNames[oldIndex])
                    : string.Empty;

                if (!string.IsNullOrEmpty(legacySerial) &&
                    serialToIndex.TryGetValue(legacySerial, out int legacyIndex) &&
                    !usedPhysicalIndices[legacyIndex])
                {
                    physicalIndex = legacyIndex;
                }
                else
                {
                    for (int i = 0; i < usedPhysicalIndices.Length; i++)
                    {
                        if (!usedPhysicalIndices[i])
                        {
                            physicalIndex = i;
                            break;
                        }
                    }
                }

                if (physicalIndex < 0)
                {
                    mappingValid = false;
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = $"工位{jobIndex + 1}相机映射失败";
                    operLog.strFailReason = "没有未占用的物理相机可用于旧参数迁移";
                    AddLog();
                    continue;
                }

                cup.p_CD.camInd[jobIndex] = physicalIndex;
                cup.p_CD.camSerial[jobIndex] = enumeratedSerials[physicalIndex];
                usedPhysicalIndices[physicalIndex] = true;
                mappedJobs[jobIndex] = true;
                mappingChanged = true;

                operLog.iEventStatus = 0;
                operLog.strOperEvent = $"工位{jobIndex + 1}相机序列号迁移";
                operLog.strFailReason = $"绑定序列号:{enumeratedSerials[physicalIndex]}, 物理索引:{physicalIndex}";
                AddLog();
            }

            // p_Cam 仍按物理索引保存参数，只刷新该槽位当前真实设备名称，不改变其他参数语义。
            for (int i = 0; i < deviceNames.Count && i < cup.p_Cam.Length; i++)
            {
                if (!string.Equals(cup.p_Cam[i].strCamName, deviceNames[i], StringComparison.Ordinal))
                {
                    cup.p_Cam[i].strCamName = deviceNames[i];
                    mappingChanged = true;
                }
            }

            return mappingValid;
        }

        private void InitCam()
        {
            int iInitType = 0; // 0:正常初始化 -1:没有可用相机
            bool bNeedCleanup = false;
            bool bStatusChanged = false;

            for (int cameraIndex = 0; cameraIndex < maxCameraNum; cameraIndex++)
            {
                if (vpWin != null && !vpWin.StopPreviewForCamera(cameraIndex))
                {
                    LogCameraConnection("初始化相机", false,
                        $"cameraIndex={cameraIndex}, 旧预览线程未退出，拒绝重新初始化句柄");
                    return;
                }
            }

            if (cameraNum < 1)
            {
                iInitType = -1;
                goto outofInit;
            }

            try
            {
                // 1. 枚举相机名称
                items = hIKVisionCamera.EnumDevices();


                // 按工位保存的硬件序列号重建本次运行的物理索引。
                if (items.Count > 0)
                {
                    bool mappingValid = TryMapJobsByCameraSerial(items, out bool mappingChanged);
                    if (mappingChanged)
                        SaveUserParam();

                    if (!mappingValid)
                    {
                        iInitType = -7;
                        bNeedCleanup = true;
                        goto outofInit;
                    }
                }
                // 2. 检查每个工位配置的相机编号是否有效，无效则禁用
                bool bAnyProcessEnabled = false;
                for (int i = 0; i < maxJobNum; i++)
                {
                    if (cup.p_CD.bListProcess[i])
                    {
                        int iCamIndex = cup.p_CD.camInd[i];
                        if (iCamIndex >= cameraNum)
                        {
                         //   cup.p_CD.bListProcess[i] = false;
                            bStatusChanged = true;
                        }
                        else
                        {
                            bAnyProcessEnabled = true;
                        }
                    }
                }
                if (!bAnyProcessEnabled)
                {
                    iInitType = -2;
                    goto outofInit;
                }

                // 3. 创建相机连接对象
                camera2 = hIKVisionCamera.MultiCameraInit((int)cameraNum, ref rts.cs.iStatus);

                // 4. 循环处理每个启用的工位
                for (int ind = 0; ind < maxCameraNum; ind++)
                {
                    if (!cup.p_CD.bListProcess[ind])
                        continue;

                    int iCamInd = cup.p_CD.camInd[ind];
                    if (iCamInd < 0 || iCamInd >= cameraNum ||
                        camera2 == null || iCamInd >= camera2.Length ||
                        rts.cs.iStatus == null || iCamInd >= rts.cs.iStatus.Length)
                    {
                        bStatusChanged = true;
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = $"初始化相机-工位{ind + 1}";
                        operLog.strFailReason =
                            $"无效相机索引: jobIndex={ind}, cameraIndex={iCamInd}, " +
                            $"cameraNum={cameraNum}, cameraArrayLength={(camera2 == null ? 0 : camera2.Length)}, " +
                            $"statusLength={(rts.cs.iStatus == null ? 0 : rts.cs.iStatus.Length)}";
                        AddLog();
                        continue;
                    }

                    if (camera2[iCamInd] == null)
                    {
                     //   cup.p_CD.bListProcess[ind] = false;
                        bStatusChanged = true;
                        continue;
                    }

                    // 5. 只读取当前设备成像参数，不恢复项目保存值。
                    if (!ReadCamParam(iCamInd))
                    {
                    //    cup.p_CD.bListProcess[ind] = false;
                        bStatusChanged = true;
                        continue;
                    }
                    
                    if (cup.p_CD.triggleCam == iCamInd) SetTrigger(iCamInd);
                    // 6. 获取 PayloadSize（图像数据量）
                    MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
                    int ret = camera2[iCamInd].MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
                    if (ret == MyCamera.MV_OK)
                    {
                        nPayloadSizeS[iCamInd] = stParam.nCurValue;
                        rts.cs.iStatus[iCamInd] = 1;
                    }
                    else
                    {
                        // 回退方式：抓一帧获取大小
                        MyCamera.MV_FRAME_OUT stFrameInfo = new MyCamera.MV_FRAME_OUT();
                        ret = camera2[iCamInd].MV_CC_GetImageBuffer_NET(ref stFrameInfo, 10000);
                        if (ret == MyCamera.MV_OK)
                        {
                            nPayloadSizeS[iCamInd] = stFrameInfo.stFrameInfo.nFrameLen;
                            rts.cs.iStatus[iCamInd] = 1;
                        }
                        else
                        {
                         //   cup.p_CD.bListProcess[ind] = false;
                            bStatusChanged = true;
                            continue;
                        }
                    }

                    // 7. 为相机分配匹配的缓冲区（由 AcqFIFO 管理）
                    if (!acqFIFO.ReallocateBuffer(iCamInd, nPayloadSizeS[iCamInd]))
                    {
                     //   cup.p_CD.bListProcess[ind] = false;
                        bStatusChanged = true;
                        continue;
                    }

                    // 8. 相机就绪
                    rts.cs.iStatus[iCamInd] = 3;
                }

                // 9. 再次检查是否还有启用的任务
                bool anyEnabled = false;
                for (int i = 0; i < maxCameraNum; i++)
                {
                    if (cup.p_CD.bListProcess[i])
                    {
                        anyEnabled = true;
                        break;
                    }
                }
                if (!anyEnabled)
                {
                    iInitType = -3;
                    bNeedCleanup = true;
                    goto outofInit;
                }

                // 如果状态有变化，保存配置
                if (bStatusChanged)
                    SaveUserParam();

                iInitType = 0;
            }
            catch (Exception ex)
            {
                iInitType = -6;
                bNeedCleanup = true;
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "初始化相机";
                operLog.strFailReason =
                    $"阶段=InitCam, cameraNum={cameraNum}, " +
                    $"cameraArrayLength={(camera2 == null ? 0 : camera2.Length)}, " +
                    $"statusLength={(rts.cs.iStatus == null ? 0 : rts.cs.iStatus.Length)}, " +
                    ex.ToString();
                AddLog();
            }

        outofInit:
            if (bNeedCleanup)
            {
                acqFIFO.FreeAllBuffers();
                for (int i = 0; i < maxCameraNum; i++)
                    SyncCameraStatus(i, 0);
                bStatusChanged = true;
            }

            if (bStatusChanged)
                SaveUserParam();

            SyncAllCameraStatus();
            ProcessInitCamStatus(iInitType);
        }
        private void Reallocate(int iCamInd)
        {
            // 优先通过标准属性获取PayloadSize
            if (camera2[iCamInd].MV_CC_GetIntValue_NET("PayloadSize", ref stParam) == MyCamera.MV_OK)
            {
                nPayloadSizeS[iCamInd] = stParam.nCurValue;
                rts.cs.iStatus[iCamInd] = 1;
            }
        }
    
        //断开相机
        private bool StopDevice()
        {
            if (!StopGrab()) return false;
            // 关闭海康相机
            if (chooseHIK)
            {
                try
                {
                    // 停止采集线程
                    if (hikCanGrab == true)
                    {
                        hikCanGrab = false;
                        if (hikGrabThread != null && hikGrabThread.IsAlive)
                            hikGrabThread.Join();
                    }

                    // 销毁所有相机连接
                    bool released = true;
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        int stopRet, closeRet, destroyRet;
                        hIKVisionCamera.ReleaseCamera(i, out stopRet, out closeRet, out destroyRet);
                        // 未采集时 Stop 可返回调用顺序错误；断开以 Close/Destroy 真实结果为准。
                        if ((closeRet != int.MinValue && closeRet != MyCamera.MV_OK) ||
                            (destroyRet != int.MinValue && destroyRet != MyCamera.MV_OK))
                        {
                            released = false;
                            LogCameraConnection("断开相机", false, $"cameraIndex={i}, Stop=0x{unchecked((uint)stopRet):X8}, Close=0x{unchecked((uint)closeRet):X8}, Destroy=0x{unchecked((uint)destroyRet):X8}");
                        }
                    }
                    if (!released) return false;
                }
                catch (Exception ex)
                {
                    LogCameraConnection("断开相机", false, $"exception={ex}");
                    return false;
                }
            }

            // 关闭 Basler 相机（如有，可在此处添加对应代码）
            // ...

            // 释放 AcqFIFO 管理的所有图像缓冲区（非托管内存）
            if (acqFIFO != null)
            {
                acqFIFO.FreeAllBuffers();
            }
            return true;
        }
        //停止采集
        private bool StopGrab()
        {
            if (!chooseHIK) return true;
            try
            {
                bool stopped = true;
                for (int i = 0; i < maxCameraNum; i++)
                    stopped &= vpWin.previewOnlive(false, i);
                if (stopped) hikCanGrab = false;
                return stopped;
            }
            catch (Exception ex)
            {
                LogCameraConnection("停止预览", false, $"exception={ex}");
                return false;
            }
        }

        private void CloseCamStatus()
        {
            for (int i = 0; i < rts.cs.iStatus.Length; i++)
                rts.cs.iStatus[i] = 0;         
        }
        private bool CloseCam()
        {
            if (!StopDevice()) return false;
            CloseCamStatus();
            return true;
        }
 
        private bool ReadCamParam(int iCamIndex)
        {
            try
            {
                if (iCamIndex < 0 || iCamIndex >= maxCameraNum || cup.p_Cam == null ||
                    iCamIndex >= cup.p_Cam.Length || !chooseHIK || camera2 == null ||
                    iCamIndex >= camera2.Length || camera2[iCamIndex] == null)
                {
                    LogCameraConnection("读取相机参数", false, $"cameraIndex={iCamIndex}, 相机对象或索引无效，未调用SDK");
                    return false;
                }
                CameraParam local = cup.p_Cam[iCamIndex];
                bool autoExposure, gammaReadable;
                if (!hIKVisionCamera.GetCamParam(ref local, camera2[iCamIndex], out autoExposure, out gammaReadable)) return false;
                cup.p_Cam[iCamIndex] = local;
                _cameraAutoExposure[iCamIndex] = autoExposure;
                _cameraGammaReadable[iCamIndex] = gammaReadable;
                UpdateCameraParamCache(iCamIndex);
                if (IsHandleCreated && InvokeRequired)
                    BeginInvoke(new Action(() => { if (icamSelectInd == iCamIndex) UpdateCamParamToUI(); }));
                else if (icamSelectInd == iCamIndex)
                    UpdateCamParamToUI();
                return true;
            }
            catch (Exception ex)
            {
                LogCameraConnection("读取相机参数", false, $"cameraIndex={iCamIndex}, exception={ex}");
                return false;
            }
        }

        private void DisableUnusedCameraControls()
        {
            chkGamma_Enable.CheckedChanged -= chkGamma_Enable_CheckedChanged;
            chkGamma_Enable.Click -= chkAutoExposure_Click;
            chkGamma_UserMode.CheckedChanged -= chkGamma_UserMode_CheckedChanged;
            chkGamma_UserMode.Click -= chkAutoExposure_Click;
            chkGamma_Enable.Visible = chkGamma_UserMode.Visible = false;
            chkGamma_Enable.Enabled = chkGamma_UserMode.Enabled = false;
            tbSharp.ValueChanged -= tbSharp_ValueChanged;
            nudSharp.ValueChanged -= nudSharp_ValueChanged;
            label39.Visible = tbSharp.Visible = nudSharp.Visible = false;
            tbSharp.Enabled = nudSharp.Enabled = false;
        }

        public bool SetTrigger(int iCamIndex)
        {
            try
            {
                MyCamera camera = camera2[iCamIndex];
                if (camera == null) return false;
                bool success = hIKVisionCamera.SetEnumValueChecked(camera, "TriggerMode", cup.p_CD.bTriggle
                    ? (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON
                    : (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                if (cup.p_CD.bTriggle)
                    success &= hIKVisionCamera.SetEnumValueChecked(camera, "TriggerSource", cup.p_CD.triggleMode == 4
                        ? (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE
                        : (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                return success;
            }
            catch (Exception ex)
            {
                string serial = cup.p_Cam != null && iCamIndex >= 0 && iCamIndex < cup.p_Cam.Length
                    ? cup.p_Cam[iCamIndex].strCamName : string.Empty;
                LogCameraConnection("触发设置", false, $"cameraIndex={iCamIndex}, serial={serial}, exception={ex}");
                return false;
            }
        }

        private bool StartGrab(int iCamInd)
        {
            try
            {
                int sdkResult = hIKVisionCamera.StartGrabbing(iCamInd);
                bool success = sdkResult == MyCamera.MV_OK;

                string serial = cup.p_Cam != null && iCamInd >= 0 && iCamInd < cup.p_Cam.Length
                    ? cup.p_Cam[iCamInd].strCamName
                    : string.Empty;
                string runtimeState;
                GetCameraRuntimeState(iCamInd, out runtimeState);
                SyncCameraStatus(iCamInd, success ? 3 : 1);

                operLog.strOperEvent = $"启动相机采集-Cam{iCamInd}";
                operLog.iEventStatus = success ? 0 : -1;
                operLog.strFailReason =
                    $"cameraIndex={iCamInd}, serial={serial}, StartGrabbing SDK=0x{unchecked((uint)sdkResult):X8}";
                WriteCameraDiagnostic(operLog.strOperEvent, operLog.iEventStatus, operLog.strFailReason);
                AddLog();
                return success;
            }
            catch (Exception ex)
            {
                SyncCameraStatus(iCamInd, 1);
                operLog.strOperEvent = $"启动相机采集-Cam{iCamInd}";
                operLog.iEventStatus = -1;
                operLog.strFailReason =
                    $"阶段=StartGrabbing, Cam{iCamInd}, cameraNum={cameraNum}, " +
                    $"cameraArrayLength={(camera2 == null ? 0 : camera2.Length)}, exception={ex}";
                WriteCameraDiagnostic(operLog.strOperEvent, -1, operLog.strFailReason);
                AddLog();
                return false;
            }
        }

        private bool StartGrab()
        {
            bool allSucceeded = true;
            for (int iCamInd = 0; iCamInd < cameraNum; iCamInd++)
            {
                if (!StartGrab(iCamInd))
                    allSucceeded = false;
            }
            bool anyEnabled = false;
            for (int job = 0; job < maxJobNum; job++)
            {
                if (!cup.p_CD.bListProcess[job]) continue;
                anyEnabled = true;
                int cameraIndex = cup.p_CD.camInd[job];
                allSucceeded &= cameraIndex >= 0 && cameraIndex < rts.cs.iStatus.Length && rts.cs.iStatus[cameraIndex] == 3;
            }
            rts.initStatus.bCamInit = anyEnabled && allSucceeded;
            rts.initStatus.bCamPrepared = rts.initStatus.bCamInit;
            return rts.initStatus.bCamInit;
        }

        private string GetJobsForCamera(int cameraIndex)
        {
            List<string> jobs = new List<string>();
            for (int jobIndex = 0; jobIndex < maxCameraNum; jobIndex++)
            {
                if (cup.p_CD.bListProcess[jobIndex] && cup.p_CD.camInd[jobIndex] == cameraIndex)
                    jobs.Add($"Job{jobIndex}");
            }
            return jobs.Count > 0 ? string.Join(",", jobs) : "未分配启用工位";
        }

        private bool GetCameraRuntimeState(int cameraIndex, out string state)
        {
            state = "相机对象无效";
            if (camera2 == null || cameraIndex < 0 || cameraIndex >= camera2.Length || camera2[cameraIndex] == null)
                return false;
            try
            {
                MyCamera camera = camera2[cameraIndex];
                string[] nodes = { "AcquisitionMode", "TriggerMode", "TriggerSource", "PixelFormat" };
                List<string> values = new List<string>();
                bool success = true;
                foreach (string node in nodes)
                {
                    MyCamera.MVCC_ENUMVALUE value = new MyCamera.MVCC_ENUMVALUE();
                    int ret = camera.MV_CC_GetEnumValue_NET(node, ref value);
                    success &= hIKVisionCamera.CheckSdkResult(camera, node + ".ReadBack", "read", ret);
                    values.Add(ret == MyCamera.MV_OK ? $"{node}={value.nCurValue}" : $"{node}=读取失败(0x{unchecked((uint)ret):X8})");
                }
                state = string.Join(", ", values);
                return success;
            }
            catch (Exception ex)
            {
                state = $"cameraIndex={cameraIndex}, 状态读回异常: {ex}";
                LogCameraConnection("采集状态读回", false, state);
                return false;
            }
        }

        /// <summary>
        /// 同步相机状态到 rts.cs 和 acqFIFO._cs
        /// </summary>
        /// <param name="iCamInd">物理相机索引</param>
        /// <param name="iStatus">状态值: 0=未连接, 1=已获取载荷, 2=参数已设置, 3=就绪</param>
        private void SyncCameraStatus(int iCamInd, int iStatus)
        {
            // 更新运行时的相机状态
            if (iCamInd >= 0 && iCamInd < rts.cs.iStatus.Length)
            {
                rts.cs.iStatus[iCamInd] = iStatus;
            }

            // 同步到 AcqFIFO
            if (acqFIFO != null)
            {
                acqFIFO._cs = rts.cs;
            }
        }

        /// <summary>
        /// 批量同步所有相机状态
        /// </summary>
        private void SyncAllCameraStatus()
        {
            // 同步到 AcqFIFO
            if (acqFIFO != null)
            {
                acqFIFO._cs = rts.cs;
            }
        }

        /// <summary>
        /// 获取相机状态描述字符串
        /// </summary>
        private string GetCameraStatusDesc(int iStatus)
        {
            switch (iStatus)
            {
                case 0: return "未连接";
                case 1: return "已获取载荷";
                case 2: return "参数已设置";
                case 3: return "就绪";
                default: return "未知状态";
            }
        }

        #endregion Hik摄像头相关 

        #region 自义定功能函数去
       
        
        #endregion

        #region 自定义操作区
        private List<int> GetCurrentJobTargets()
        {
            List<int> jobIndices = new List<int>();
            if (iJobIndex >= 0 && iJobIndex < maxCameraNum)
            {
                if (cup.p_CD.bListProcess[iJobIndex])
                    jobIndices.Add(iJobIndex);
                return jobIndices;
            }

            if (iJobIndex == -1)
            {
                for (int jobIndex = 0; jobIndex < maxCameraNum; jobIndex++)
                {
                    if (cup.p_CD.bListProcess[jobIndex])
                        jobIndices.Add(jobIndex);
                }
            }
            return jobIndices;
        }

        private bool OpenOrCloseCam(bool bOpen)
        {
            if (bOpen)
            {
                // 用线程更新显示
                if (chooseHIK)
                {
                    operLog.strOperEvent = "启动图像预览线程";
                    try
                    {
                        bool anyEnabled = false;
                        bool anyStarted = false;
                        bool allStarted = true;
                        List<int> targetJobs = GetCurrentJobTargets();
                        foreach (int i in targetJobs)
                        {
                            anyEnabled = true;
                            bool started = vpWin.previewOnlive(true, i);
                            anyStarted |= started;
                            allStarted &= started;
                        }

                        hikCanGrab = anyStarted;
                        operLog.iEventStatus = anyEnabled && allStarted ? 0 : -1;
                        operLog.strFailReason = !anyEnabled
                            ? "当前工位模式没有启用的目标工位"
                            : allStarted
                                ? "目标工位的Preview线程已启动，等待逐路首帧确认"
                                : "至少一个目标工位的Preview线程启动失败；各路结果见独立日志";
                        menuCam_Connect.Text = anyStarted ? "关闭图像预览" : "启动图像预览";
                        AddLog();
                        return anyStarted;
                    }
                    catch (Exception ex)
                    {
                        hikCanGrab = false;
                        menuCam_Connect.Text = "启动图像预览";
                        operLog.iEventStatus = -1;
                        operLog.strFailReason = "启动预览线程失败: " + ex;
                        WriteCameraDiagnostic(operLog.strOperEvent, -1, operLog.strFailReason);
                        AddLog();
                        return false;
                    }
                }
            }
            else
            {
                operLog.strOperEvent = "关闭图像预览";
                try
                {
                    if (!StopGrab()) return false;
                    // vpWin.OpenOrCloseCam(false);
                    operLog.iEventStatus = 0;
                    menuCam_Connect.Text = "启动图像预览";
                    //  toolBt_Cam.Checked = false;
                }
                catch
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "关闭预览线程失败";
                    AddLog();
                    return false;
                }
                AddLog();

            }
            return true;
        }

        private void CamSet()
        {
            CloseCam();
            //  FrmCam_Set fc_s = new FrmCam_Set();
            FrmCam_Debug fc_s = new FrmCam_Debug();

            fc_s._cup = cup;
            fc_s.ShowDialog();
            cup = fc_s._cup;
            SaveUserParam();
            InitCam();
            StartGrab();
            operLog.strOperEvent = "设置相机参数";
            AddLog();
        }

        /// <summary>
        /// 分析单个工位的运行结果并记录日志。
        /// 支持深度推理（缺陷检测）和视觉推理（长度测量）。
        /// </summary>
        /// <param name="jobIndex">工位索引（0=A面，1=B面）</param>
        private void AnaEventForJob(int jobIndex)
        {
            // 参数有效性检查
            if (jobIndex < 0 || jobIndex >= maxCameraNum || !cup.p_CD.bListProcess[jobIndex])
                return;

            string faceName = (jobIndex == 0) ? "A面" : $"B面";

            if (cup.p_VPPRTP.bDeepInfer)
            {
                // ========== 深度推理模式：缺陷检测 ==========
                DLResult dlRes = vpWin.DL_result[jobIndex];   // 假设 DL_result 已公开
                int defectCount = dlRes.AL_Rect?.Count ?? 0;
                bool bOk = (defectCount == 0);

                // 构建缺陷详情（如果有）
                string defectDetail = "";
                if (!bOk)
                {
                    for (int i = 0; i < dlRes.AL_Type.Count && i < dlRes.AL_score.Count; i++)
                    {
                        int typeId = dlRes.AL_Type[i];
                        float score = dlRes.AL_score[i];
                        string className = cdl.GetClassName(typeId) ?? $"类型{typeId}";
                        defectDetail += $"{className}({score:F2}) ";
                    }
                }

                operLog.strOperEvent = $"{faceName} 缺陷检测完成";
                operLog.iEventStatus = bOk ? 2 : -2;
                operLog.strFailReason = bOk ? "OK" : $"NG，缺陷: {defectDetail.Trim()}";

                // 如果定位失败且未保存图像，可自动保存（可选）
                if (!bOk && !cup.p_VPPRTP.bRecord)
                {
                    // 调用保存方法（已在其他部分实现）
                }
            }
            else if (cup.p_VPPRTP.bVisionInfer)
            {
                // ========== 视觉推理模式：长度测量 ==========
                PMARes res = vpWin._PMARes[jobIndex];    // 假设 _PMARes 已公开
                bool bOk = res.bOk;
                double length = res.dPMAScore;           // 或 res.dTX，取决于存储字段

                operLog.strOperEvent = $"{faceName} 长度测量完成";
                operLog.iEventStatus = bOk ? 2 : -2;
                operLog.strFailReason = bOk ? $"OK，长度: {length:F2}" : $"NG，长度: {length:F2}";
            }
            else
            {
                // 未启用任何推理引擎
                operLog.strOperEvent = $"{faceName} 无推理";
                operLog.iEventStatus = 0;
                operLog.strFailReason = "未启用推理引擎";
            }

            // 记录日志
            AddLog();

            // IO结果由各运行完成路径在取得最终pmaRes[jobIndex].bOk后提交。
            // 日志/UI更新函数不再重复提交，避免同一检测结果跨批次污染。
        }
        /*private void AnaEventForJob(int jobIndex)
        {
            bool ok = pmaRes[jobIndex].bLocated;
            string faceName = (jobIndex == 0) ? "A面" : "B面";
            operLog.strOperEvent = $"{faceName} 检测完成";
            operLog.iEventStatus = ok ? 2 : -2;
          //  operLog.strFailReason = ok ? "OK" : "NG";
            operLog.strFailReason = "分数:" + pmaRes[jobIndex].dPMAScore.ToString("0.00")
                    + "，X:" + pmaRes[jobIndex].dTX.ToString("0.00") + "，Y:" + pmaRes[jobIndex].dTY.ToString("0.00")
                    + "，R:" + pmaRes[jobIndex].dTR.ToString("0.00"); 
            AddLog();
            // ---------- 定位失败自动保存图像 ----------
            if (!ok && !cup.p_VPPRTP.bRecord )
            {
                try
                {
                    if (acqFIFO != null &&
                        !string.IsNullOrEmpty(acqFIFO.strTime[jobIndex]) &&
                        strErrPathS != null &&
                        strErrPathS.Length > jobIndex)
                    {
                        // 调用新方法保存当前图像
                        acqFIFO.SaveCurrentImageForErr(strErrPathS, acqFIFO.strTime[jobIndex], jobIndex);

                       *//* operLog.iEventStatus = 0;
                        operLog.strOperEvent = "自动保存失败图像";
                        operLog.strFailReason = $"{faceName} 图像已保存";
                        AddLog();*//*
                    }
                }
                catch (Exception ex)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "保存定位失败图像";
                    operLog.strFailReason = $"{faceName} 图像保存失败";
                    AddLog();
                    System.Diagnostics.Debug.WriteLine($"自动保存失败图像异常: {ex.Message}");
                }
            }
        }*/
        /// <summary>
        /// 分析所有工位的检测结果，生成日志并更新统计。
        /// 适用于同步执行所有工位后的统一处理。
        /// </summary>
        /// <summary>
        /// 分析本次运行结果：收集所有启用工位的检测状态，生成综合日志。
        /// 支持 VP 视觉推理（长度测量）和深度推理（缺陷检测）。
        /// </summary>
        private void AnaEvent()
        {
            // 1. 如果没有任何工位启用，直接返回
            bool anyJobEnabled = false;
            for (int i = 0; i < maxCameraNum; i++)
            {
                if (cup.p_CD.bListProcess[i])
                {
                    anyJobEnabled = true;
                    break;
                }
            }
            if (!anyJobEnabled)
                return;

            // 2. 根据推理模式分别处理
            if (cup.p_VPPRTP.bDeepInfer)
            {
                // ---------- 深度推理模式 ----------
                bool anyNG = false;
                string detailInfo = "";

                for (int i = 0; i < maxCameraNum; i++)
                {
                    if (!cup.p_CD.bListProcess[i]) continue;

                    // 获取该工位的推理结果（DL_result 是数组，元素为 struct，无需判空）
                    DLResult dlRes = vpWin.DL_result[i];
                    int defectCount = dlRes.AL_Rect?.Count ?? 0;
                    bool bOk = (defectCount == 0);      // 无检测框视为 OK

                    if (!bOk) anyNG = true;

                    string faceName = (i == 0) ? "A面" : $"B面";
                    detailInfo += $"{faceName}: {(bOk ? "OK" : "NG")} (缺陷数:{defectCount}) ";
                }

                operLog.strOperEvent = "完成一次运行 (深度推理)";
                operLog.iEventStatus = anyNG ? -2 : 2;
                operLog.strFailReason = anyNG ? $"NG。{detailInfo}" : $"OK。{detailInfo}";
                AddLog();
            }
            else if (cup.p_VPPRTP.bVisionInfer)
            {
                // ---------- VP 视觉推理模式 ----------
                bool anyNG = false;
                string detailInfo = "";

                for (int i = 0; i < maxCameraNum; i++)
                {
                    if (!cup.p_CD.bListProcess[i]) continue;

                    // 从 pmaRes 获取该工位结果（pmaRes 为 struct 数组，直接访问）
                    PMARes res = vpWin._PMARes[i];
                    bool bOk = res.bOk;
                    double length = res.dPMAScore;      // 假设长度存储在 dPMAScore，也可用 dTX

                    if (!bOk) anyNG = true;

                    string faceName = (i == 0) ? "A面" : $"B面";
                    detailInfo += $"{faceName}: {(bOk ? "OK" : "NG")} (长度:{length:F2}) ";
                }

                operLog.strOperEvent = "完成一次运行 (视觉推理)";
                operLog.iEventStatus = anyNG ? -2 : 2;
                operLog.strFailReason = anyNG ? $"NG。{detailInfo}" : $"OK。{detailInfo}";
                AddLog();
            }
            else
            {
                // 未启用任何推理引擎（仅采集或回放）
                operLog.strOperEvent = "完成一次运行 (无推理)";
                operLog.iEventStatus = 0;
                operLog.strFailReason = "未启用任何推理引擎";
                AddLog();
            }
        }
        // 提交单个工位本轮真实结果；全部参与工位完成后一次输出组合位图。
        private void ExeIOOut(int jobIndex, long cycleId)
        {
            if (jobIndex < 0 || jobIndex >= ioFeedbackJobCount)
                return;

            if (cup.p_VPPRTP.bVisionInfer && !vpWin.IsVppJobResultValid(jobIndex))
            {
                AbortIOFeedbackCycle(jobIndex, cycleId, "本轮长度测量结果无效");
                return;
            }
            SubmitIOFeedbackResult(jobIndex, pmaRes[jobIndex].bOk, cycleId);
        }

        private bool TryBeginIOFeedbackRun(int jobIndex, bool singleJobCycle,
            out long cycleId, out string errorMessage)
        {
            cycleId = 0;
            errorMessage = string.Empty;
            if (!cup.p_RTSP.bUpdateIO)
                return true;
            if (jobIndex < 0 || jobIndex >= ioFeedbackJobCount)
            {
                errorMessage = $"Job{jobIndex}不属于当前COM3 IO反馈范围";
                return false;
            }

            lock (ioFeedbackSync)
            {
                if (singleJobCycle)
                {
                    if (ioFeedbackCycleActive || ioFeedbackSingleJobCycles[jobIndex] != 0)
                    {
                        errorMessage = $"Job{jobIndex}的IO反馈尚未结束";
                        return false;
                    }
                    cycleId = ++ioFeedbackCycleId;
                    ioFeedbackSingleJobCycles[jobIndex] = cycleId;
                    return true;
                }
                if (ioFeedbackSingleJobCycles.Any(activeCycle => activeCycle != 0))
                {
                    errorMessage = "单工位IO反馈尚未结束";
                    return false;
                }
                if (!ioFeedbackCycleActive)
                {
                    bool anyParticipant = false;
                    for (int i = 0; i < ioFeedbackJobCount; i++)
                    {
                        ioFeedbackParticipating[i] = singleJobCycle
                            ? i == jobIndex
                            : cup.p_CD.bListProcess != null &&
                              i < cup.p_CD.bListProcess.Length &&
                              cup.p_CD.bListProcess[i];
                        anyParticipant |= ioFeedbackParticipating[i];
                        ioFeedbackStarted[i] = false;
                        ioFeedbackReady[i] = false;
                        ioFeedbackResult[i] = false;
                    }

                    if (!anyParticipant || !ioFeedbackParticipating[jobIndex])
                    {
                        ResetIOFeedbackCycleNoLock();
                        errorMessage = $"Job{jobIndex}不属于当前IO反馈批次";
                        return false;
                    }

                    ioFeedbackCycleId++;
                    ioFeedbackCycleActive = true;
                    System.Threading.Interlocked.Exchange(ref completedJobCount, 0);
                    allJobsDone = false;
                }

                if (ioFeedbackOutputInProgress)
                {
                    errorMessage = $"IO反馈批次{ioFeedbackCycleId}正在输出";
                    return false;
                }
                if (!ioFeedbackParticipating[jobIndex])
                {
                    errorMessage = $"Job{jobIndex}未参与IO反馈批次{ioFeedbackCycleId}";
                    return false;
                }
                if (ioFeedbackStarted[jobIndex])
                {
                    errorMessage = $"Job{jobIndex}已开始IO反馈批次{ioFeedbackCycleId}，拒绝跨轮重复运行";
                    return false;
                }

                ioFeedbackStarted[jobIndex] = true;
                cycleId = ioFeedbackCycleId; // 仅成功加入的请求取得取消/提交权限。
                return true;
            }
        }

        private void SubmitIOFeedbackResult(int jobIndex, bool isOk, long cycleId)
        {
            if (!cup.p_RTSP.bUpdateIO || jobIndex < 0 || jobIndex >= ioFeedbackJobCount)
                return;

            byte outputBitmap = 0;
            string prepareError = string.Empty;
            bool shouldSend = false;
            bool singleJobCycle = false;

            lock (ioFeedbackSync)
            {
                if (cycleId > 0 && ioFeedbackSingleJobCycles[jobIndex] == cycleId)
                {
                    singleJobCycle = true;
                    if (ioFeedbackSingleJobOutputInProgress[jobIndex])
                    {
                        prepareError = $"Job{jobIndex}的IO反馈批次{cycleId}正在输出";
                    }
                    else
                    {
                        bool[] results = new bool[ioFeedbackJobCount];
                        bool[] participants = new bool[ioFeedbackJobCount];
                        results[jobIndex] = isOk;
                        participants[jobIndex] = true;
                        if (TryBuildIOOutputBitmap(results, participants, out outputBitmap, out prepareError))
                        {
                            ioFeedbackSingleJobOutputInProgress[jobIndex] = true;
                            shouldSend = true;
                        }
                        else
                        {
                            ioFeedbackSingleJobCycles[jobIndex] = 0;
                        }
                    }
                }
                else if (!ioFeedbackCycleActive || cycleId <= 0 || cycleId != ioFeedbackCycleId)
                {
                    prepareError = $"Job{jobIndex}提交了已失效的IO反馈批次{cycleId}，当前批次为" +
                        (ioFeedbackCycleActive ? ioFeedbackCycleId.ToString() : "无");
                }
                else if (!ioFeedbackParticipating[jobIndex] || !ioFeedbackStarted[jobIndex])
                {
                    prepareError = $"Job{jobIndex}未开始IO反馈批次{cycleId}";
                }
                else if (ioFeedbackReady[jobIndex])
                {
                    prepareError = $"Job{jobIndex}已提交IO反馈批次{cycleId}，拒绝重复结果";
                }
                else
                {
                    ioFeedbackResult[jobIndex] = isOk;
                    ioFeedbackReady[jobIndex] = true;

                    bool allReady = true;
                    for (int i = 0; i < ioFeedbackJobCount; i++)
                    {
                        if (ioFeedbackParticipating[i] && !ioFeedbackReady[i])
                        {
                            allReady = false;
                            break;
                        }
                    }

                    if (allReady && !ioFeedbackOutputInProgress)
                    {
                        if (TryBuildIOOutputBitmap(ioFeedbackResult, ioFeedbackParticipating,
                            out outputBitmap, out prepareError))
                        {
                            ioFeedbackOutputInProgress = true;
                            shouldSend = true;
                        }
                        else
                        {
                            ResetIOFeedbackCycleNoLock();
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(prepareError))
            {
                WriteIOFeedbackLog(-1, "IO组合结果", prepareError);
                return;
            }
            if (!shouldSend)
            {
                WriteIOFeedbackLog(0, "IO结果就绪",
                    $"批次{cycleId} Job{jobIndex}={(isOk ? "OK" : "NG")}，等待其他参与工位");
                return;
            }

            string sendError = string.Empty;
            bool sendSucceeded = false;
            try
            {
                sendSucceeded = csd.SendIOOutputBitmapPulse(
                    sPortCy, outputBitmap, cup.p_IOPP.IOTimeDelay, out sendError);
            }
            catch (Exception ex)
            {
                sendError = ex.ToString();
            }
            finally
            {
                lock (ioFeedbackSync)
                {
                    if (singleJobCycle && ioFeedbackSingleJobCycles[jobIndex] == cycleId)
                    {
                        ioFeedbackSingleJobCycles[jobIndex] = 0;
                        ioFeedbackSingleJobOutputInProgress[jobIndex] = false;
                    }
                    else if (!singleJobCycle && ioFeedbackCycleActive && ioFeedbackCycleId == cycleId)
                        ResetIOFeedbackCycleNoLock();
                }
            }

            if (sendSucceeded)
            {
                WriteIOFeedbackLog(0, "IO组合输出",
                    $"批次{cycleId} 位图0x{outputBitmap:X2}发送完成，保持{cup.p_IOPP.IOTimeDelay}ms后Y1～Y8已关闭");
            }
            else
            {
                WriteIOFeedbackLog(-1, "IO组合输出失败",
                    $"批次{cycleId} 位图0x{outputBitmap:X2}：{sendError}");
            }
        }

        private bool TryBuildIOOutputBitmap(bool[] results, bool[] participants,
            out byte outputBitmap, out string errorMessage)
        {
            outputBitmap = 0;
            errorMessage = string.Empty;

            if (!ValidateIOOutputMapping(out errorMessage))
                return false;

            for (int jobIndex = 0; jobIndex < ioFeedbackJobCount; jobIndex++)
            {
                if (!participants[jobIndex])
                    continue;

                int outputNo = results[jobIndex]
                    ? cup.p_IOPP.JobOkOut[jobIndex]
                    : cup.p_IOPP.JobNGOut[jobIndex];
                outputBitmap |= (byte)(1 << (outputNo - 1));
            }
            return outputBitmap != 0;
        }

        private bool ValidateIOOutputMapping(out string errorMessage)
        {
            errorMessage = string.Empty;
            if (cup.p_IOPP.JobOkOut == null || cup.p_IOPP.JobOkOut.Length < ioFeedbackJobCount ||
                cup.p_IOPP.JobNGOut == null || cup.p_IOPP.JobNGOut.Length < ioFeedbackJobCount)
            {
                errorMessage = "IO输出映射缺失";
                return false;
            }

            HashSet<int> usedOutputs = new HashSet<int>();
            for (int jobIndex = 0; jobIndex < ioFeedbackJobCount; jobIndex++)
            {
                int okOutput = cup.p_IOPP.JobOkOut[jobIndex];
                int ngOutput = cup.p_IOPP.JobNGOut[jobIndex];
                if (okOutput < 1 || okOutput > 8 || ngOutput < 1 || ngOutput > 8)
                {
                    errorMessage = $"Job{jobIndex}输出必须在Y1～Y8范围内";
                    return false;
                }
                if (okOutput == ngOutput)
                {
                    errorMessage = $"Job{jobIndex}的OK与NG不能使用同一输出Y{okOutput}";
                    return false;
                }
                if (!usedOutputs.Add(okOutput) || !usedOutputs.Add(ngOutput))
                {
                    errorMessage = "Job0/Job1的OK、NG输出通道不能重复";
                    return false;
                }
            }
            if (cup.p_IOPP.JobOkOut[0] != 1 || cup.p_IOPP.JobNGOut[0] != 2 ||
                cup.p_IOPP.JobOkOut[1] != 3 || cup.p_IOPP.JobNGOut[1] != 4)
            {
                errorMessage = "本轮IO映射固定为Job0 OK/NG→Y1/Y2、Job1 OK/NG→Y3/Y4";
                return false;
            }
            return true;
        }

        private bool CanAcceptIOFeedbackRun(int jobIndex, out string reason)
        {
            reason = string.Empty;
            if (!cup.p_RTSP.bUpdateIO || jobIndex < 0 || jobIndex >= ioFeedbackJobCount)
                return true;

            lock (ioFeedbackSync)
            {
                if (ioFeedbackSingleJobCycles[jobIndex] != 0)
                {
                    reason = $"Job{jobIndex}的IO反馈尚未结束";
                    return false;
                }
                if (!ioFeedbackCycleActive)
                    return true;
                reason = $"IO组合反馈批次{ioFeedbackCycleId}尚未结束";
                return false;
            }
        }

        private void AbortIOFeedbackCycle(int failedJobIndex, long cycleId, string reason)
        {
            long abortedCycleId = 0;
            lock (ioFeedbackSync)
            {
                if (cycleId <= 0 || failedJobIndex < 0 || failedJobIndex >= ioFeedbackJobCount)
                    return;
                if (ioFeedbackSingleJobCycles[failedJobIndex] == cycleId)
                {
                    if (ioFeedbackSingleJobOutputInProgress[failedJobIndex])
                        return;
                    abortedCycleId = cycleId;
                    ioFeedbackSingleJobCycles[failedJobIndex] = 0;
                }
                else
                {
                    if (!ioFeedbackCycleActive || ioFeedbackCycleId != cycleId || ioFeedbackOutputInProgress)
                        return;
                    abortedCycleId = ioFeedbackCycleId;
                    ResetIOFeedbackCycleNoLock();
                }
            }
            WriteIOFeedbackLog(-1, "IO反馈批次取消",
                $"批次{abortedCycleId}因Job{failedJobIndex}失败而清除：{reason}");
        }

        private void ResetIOFeedbackCycleNoLock()
        {
            for (int i = 0; i < ioFeedbackJobCount; i++)
            {
                ioFeedbackResult[i] = false;
                ioFeedbackReady[i] = false;
                ioFeedbackStarted[i] = false;
                ioFeedbackParticipating[i] = false;
            }
            ioFeedbackCycleActive = false;
            ioFeedbackOutputInProgress = false;
        }

        private void ResetIOFeedbackCycle(string reason)
        {
            lock (ioFeedbackSync)
            {
                ResetIOFeedbackCycleNoLock();
                Array.Clear(ioFeedbackSingleJobCycles, 0, ioFeedbackSingleJobCycles.Length);
                Array.Clear(ioFeedbackSingleJobOutputInProgress, 0, ioFeedbackSingleJobOutputInProgress.Length);
            }
            if (!string.IsNullOrEmpty(reason))
                WriteIOFeedbackLog(0, "IO反馈批次复位", reason);
        }

        private void WriteIOFeedbackLog(int status, string operEvent, string detail)
        {
            operLog.iEventStatus = status;
            operLog.strOperEvent = operEvent;
            operLog.strFailReason = detail;
            AddLog();
        }
        private void menuTask_Clear_Click(object sender, EventArgs e)
        {
            strOrder = "清除";
            string errorMessage;
            if (csd.ClearIOOutputs(sPortCy, out errorMessage))
            {
                ResetIOFeedbackCycle("手动全部关闭");
                WriteIOFeedbackLog(0, "IO全部关闭", "Y1～Y8已发送全部OFF");
            }
            else
            {
                WriteIOFeedbackLog(-1, "IO全部关闭失败", errorMessage);
            }
        }
         
        //  DateTime dtOperLog = new DateTime();
        private string _lastLogHash = "";  // 记录最后一条日志的哈希值
        private void AddLog()
        {
            // 1. 如果在非 UI 线程，则同步切换到 UI 线程
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(AddLog));
                return;
            }

            // 2. 更新 UI 表格（直接操作，不检查重复）
            try
            {
                // 限制行数最多 20 行（可选）
                if (dgvLog.RowCount > 20)
                    dgvLog.Rows.RemoveAt(19);
                operLog.dtOperTime = DateTime.Now;
                int ind = 0;
                dgvLog.Rows.Insert(0, 1);

                dgvLog.Rows[ind].Cells[0].Value = operLog.dtOperTime.ToString("yyyy-MM-dd HH:mm:ss");
                dgvLog.Rows[ind].Cells[1].Value = operLog.strOperEvent;

                switch (operLog.iEventStatus)
                {
                    case 0:
                    case 2:
                    case 9:
                    case 10:
                        dgvLog.Rows[ind].Cells[2].Value = "执行正常";
                        dgvLog.Rows[ind].DefaultCellStyle.ForeColor = Color.Black;
                        break;
                    case -1:
                        dgvLog.Rows[ind].Cells[2].Value = "执行失败";
                        dgvLog.Rows[ind].DefaultCellStyle.ForeColor = Color.Red;
                        break;
                    case -2:
                        dgvLog.Rows[ind].Cells[2].Value = "执行正常";
                        dgvLog.Rows[ind].DefaultCellStyle.ForeColor = Color.Red;
                        break;
                    case 1:
                        dgvLog.Rows[ind].Cells[2].Value = "执行超时";
                        dgvLog.Rows[ind].DefaultCellStyle.ForeColor = Color.Red;
                        break;
                    default:
                        dgvLog.Rows[ind].Cells[2].Value = "执行异常";
                        dgvLog.Rows[ind].DefaultCellStyle.ForeColor = Color.Red;
                        break;
                }

                dgvLog.Rows[ind].Cells[4].Value = operLog.strUserName;

                if (operLog.iEventStatus < 0 || operLog.iEventStatus == 1 ||
                    operLog.iEventStatus == 2 || operLog.iEventStatus == 9)
                {
                    dgvLog.Rows[ind].Cells[3].Value = operLog.strFailReason;
                }
            }
            catch (Exception ex)
            {
                // 忽略 UI 更新异常，不影响主程序
                System.Diagnostics.Debug.WriteLine($"更新日志表格失败: {ex.Message}");
            }

            // 3. 写入文件（简单捕获异常，不因文件写入失败影响日志功能）
            // 判断是否为错误日志（状态为"执行失败"、"执行超时"、"执行异常"等）
            string status = (dgvLog.Rows[0].Cells[2].Value?.ToString() ?? "");
            bool isError = status != "执行正常" && !string.IsNullOrEmpty(status);

            if (isError)
            {
                string errorLine = $"{operLog.dtOperTime:yyyy-MM-dd HH:mm:ss} | {operLog.strOperEvent} | {status} | {operLog.strFailReason}";

                lock (_errorLogLock)
                {
                    // 1. 写入文件（追加）
                    try
                    {
                        string logDir = cup.p_Paf.strLogPath;
                        if (!string.IsNullOrEmpty(logDir))
                        {
                            if (!Directory.Exists(logDir))
                                Directory.CreateDirectory(logDir);
                            string errorLogFile = Path.Combine(logDir, $"ErrorLog_{DateTime.Now:yyyyMMdd}.txt");
                            File.AppendAllText(errorLogFile, errorLine + Environment.NewLine);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 避免写入错误日志时再触发异常导致死循环
                        System.Diagnostics.Debug.WriteLine($"写入错误日志失败: {ex.Message}");
                    }

                    // 2. 更新内存缓存（最近20条）
                    _recentErrorLogs.Add(errorLine);
                    if (_recentErrorLogs.Count > 20)
                        _recentErrorLogs.RemoveAt(0);
                }
            }

            /* try
            {
                string logDir = cup.p_Paf.strLogPath;
                if (!string.IsNullOrEmpty(logDir))
                {
                    if (!Directory.Exists(logDir))
                        Directory.CreateDirectory(logDir);
                    string logFile = Path.Combine(logDir, "Oper_" + DateTime.Now.ToString("yyyyMMdd") + ".txt");
                    fo.WriteOperLog(logFile, operLog);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"写日志文件失败: {ex.Message}");
            }*/
        }
       
        #endregion  自定义操作区

        #region  状态变化性事件

        private void InitAddLog()
        {
            //  InitLog();

            //  operLog.strOperEvent = "读取配置参数";
            if (rts.initStatus.bUserParamFileInit)
                operLog.iEventStatus = 0;
            else
            {
                operLog.iEventStatus = -1;
            }
            //  AddLog();

            //  operLog.strOperEvent = "加载视觉算法文件";
            if (rts.initStatus.bVPPInit)
                operLog.iEventStatus = 0;
            else
                operLog.iEventStatus = -1;
            //   AddLog();


            //  InitCamLog();


            if (rts.iModelRole >= 4)
            {
                /*  txt_NextImageIndex.Visible = true;
                  label5.Visible = true;*/
              //  btFunSet.Visible = true;
                groupBox1.Visible = true;
                menuManage_SuperOrNormal.Visible = true;
            }


            if (!rts.initStatus.bModelRoleCert)
            {
                /* operLog.strOperEvent = "系统主机未获授权";
                 operLog.iEventStatus = -1;               
                 AddLog();*/
                rts.iModelRole = -1;                //物理主机验证未通过
            }
            /* for(int i=0;i<19;i++)
                 dgvEventLog.Rows.Insert(0, 1);*/
        }


        
        private void StartWelcome()
        {
            fw = new Frm_Welcome();
            fw.SetProgress(10, "正在启动软件");
            iProcess = 10;
            fw.ShowDialog();



            while (iProcess < 100)
            {
                switch (iProcess)
                {
                    case 20:
                        fw.SetProgress(20, "正在进行软件初始化");
                        break;
                    case 30:
                        fw.SetProgress(30, "正在进行机器合法性验证");
                        break;
                    case 40:
                        fw.SetProgress(40, "正在搜索并初始化相机");
                        break;
                    case 50:
                        fw.SetProgress(50, "正在加载视觉推理参数");
                        break;
                    case 60:
                        fw.SetProgress(60, "正在搜索并初始化串口");
                        break;
                    case 70:
                        fw.SetProgress(70, "正在进行历史数据加载");
                        break;
                    case 80:
                        fw.SetProgress(80, "正在加深度载推理环境");
                        break;
                    case 90:
                        fw.SetProgress(90, "正在启动相机采集");
                        break;
                }


                Thread.Sleep(20);
            }

        }
        //授权验证
        private void RoleCheck()
        {
            //用户app数据目录，不要求管理员权限
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (!Directory.Exists(appData+"\\GearDet"))
            {
                Directory.CreateDirectory(appData + "\\GearDet");
            }

            if (!File.Exists(appData + "\\GearDet\\authentication.txt"))
            {
                File.Copy("authentication.txt", appData + "\\GearDet\\authentication.txt", true);
             
            }
            mPasswordFile = new Protect(appData + "\\GearDet\\authentication.txt");
            string strMAC = mPasswordFile.GetMacAddress();
            string strEnableMAC = mPasswordFile.GetCID();

            int iModelRole = -1;
            if (strMAC == strEnableMAC || strMAC == authorizationManagerMac || strMAC == "207BD2D2CA3E" || strMAC == "00E04C782532" || strMAC == "00FFCF3A6322" || strMAC == "8C32233F2B85" || strMAC == "00E269A99B04")  //E4:5E:37:62:A1:3D   00FFCF3A6322
            {
                rts.initStatus.bModelRoleCert = true;

                DateVerify(iModelRole);
                if (rts.iModelRole == 0)
                {
                    if (strMAC == authorizationManagerMac || strMAC == "207BD2D2CA3E")   //授权端/调试专用功能
                    {
                        iModelRole = 9;
                        btFunSet.Visible = true;
                    }
                    else if (DateTime.Now.ToString("yyyyMMdd") == "20260505")  //临时使用
                    {
                        iModelRole = 9;
                        btFunSet.Visible = true;
                    }
                    else
                    {
                        iModelRole = 0;

                    }
                    rts.iModelRole = iModelRole;
                }
                
            }
            else
            {
                rts.initStatus.bModelRoleCert = false;
            }
            operLog.iEventStatus = iModelRole;
            operLog.strOperEvent = "身份验证";
            operLog.strFailReason = iModelRole.ToString();
            AddLog();
        }

       
        bool bLoadLaser = false;
        private void FrmMainUI_Load(object sender, EventArgs e)
        {

            UIStartThread = new Thread(StartWelcome);
            UIStartThread.Start();
            while (iProcess < 10)
            {
                Thread.Sleep(20);
            }
            iProcess = 10;
            fw.SetProgress(10, "正在进行机器合法性验证");
            RoleCheck(); //授权用户验证

            bool bSucc = true;
            if (rts.initStatus.bModelRoleCert)  // 是合法授权用户，但有可能授权过期或者篡改日期
            { 
                 
                iProcess = 20;
                fw.SetProgress(20, "正在加载深度推理环境");
                rts.initStatus.bDLModelInit = false;
                if (cup.p_VPPRTP.bDeepInfer)
                {
                    menuTask_DeepInfer.Checked = true;
                    menuTask_VisionInfer.Checked = false;
                    WriteStartupLog("[Startup] DeepInfer init start");
                    int dlInitRet = InitDL();
                    WriteStartupLog("[Startup] DeepInfer InitDL returned=" + dlInitRet);
                }
                else
                {
                    menuTask_VisionInfer.Checked = true;
                    menuTask_DeepInfer.Checked = false;
                }

                iProcess = 30;
                fw.SetProgress(30, "正在加载机器视觉环境");
                LoadVPFile(false);            //VP控件初始化，加载VPP文件和VisionControl控件

                iProcess = 40;
                fw.SetProgress(40, "正在加载硬件驱动"); 

                iProcess = 50;
                fw.SetProgress(50, "正在搜索并初始化相机");
                SearchCam();   // 搜搜相机数量                //
                InitCam();      //

                if (cup.p_VPPRTP.bDeepInfer && rts.initStatus.bDLModelInit)
                {
                    iProcess = 55;
                    fw.SetProgress(55, "正在预热深度推理");
                    WarmupDL();
                }

                iProcess = 60;
                fw.SetProgress(60, "正在搜索并初始化串口");
                InitSerial();                                           //串口初始化
                InitTimer(); //IO口状态刷新                                                  //    fw.SetProgress(90, "正在初始化算法");

                if (cup.p_VPPRTP.bDeepInfer || rts.initStatus.bVPPInit)
                {
                    InitVisionControl(); 
                }
                if (!bSucc)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "设置流程参数";
                    AddLog();
                } 
                 
                iProcess = 70;
                fw.SetProgress(70, "正在进行历史数据加载");
                InitRecord();       //初始化路径
                InitLocalFile();    //加载回放数据                            //  bSucc = vpWin.SetDiagParam(cup);

                iProcess = 80;
                fw.SetProgress(80, "正在检查初始化状态");
                btFunSet.Visible = cup.p_VPPRTP.bVisionInfer;


                InitCheckStatus(); 
                iProcess = 90;
                fw.SetProgress(90, "正在启动相机采集");
                StartGrab(); 
                
          //      vpWin.iModelRole = rts.iModelRole; 

                fw.SetProgress(95, "UI初始化");
                InitParamUI();  //必要的参数初始化 
                // 生产环境关闭模拟触发入口
                // InitIOSimulateTriggerUI();

                //  runonetime(); // 启动默认运行一次  2024年7月22日，处理运行初始化界面 
                 

                this.Visible = true;
                this.Show();
                iProcess = 100;
                Thread.Sleep(20);
                InitAddLog();
            }
            else
            {
                rts.initStatus.bModelRoleCert = false;
                // 生产环境关闭模拟触发入口
                // InitIOSimulateTriggerUI();

            }
            fw.Close();
            this.WindowState = FormWindowState.Maximized;
            bLoadFinish = true;
            timer1.Enabled = true;

        }
        // 模拟触发入口已关闭，保留空方法避免后续误接入
        private void InitIOSimulateTriggerUI()
        {
            lblIn1.DoubleClick -= lblIn1_DoubleClick;
            lblIn2.DoubleClick -= lblIn2_DoubleClick;
            lblIn1.Cursor = Cursors.Default;
            lblIn2.Cursor = Cursors.Default;
            toolTip1.SetToolTip(lblIn1, string.Empty);
            toolTip1.SetToolTip(lblIn2, string.Empty);
        }

        private void lblIn1_DoubleClick(object sender, EventArgs e)
        {
            SimulateIOTrigger(GetTriggerTypeForFace(0));
        }

        private void lblIn2_DoubleClick(object sender, EventArgs e)
        {
            SimulateIOTrigger(GetTriggerTypeForFace(1));
        }

        private int GetTriggerTypeForFace(int faceIndex)
        {
            return faceIndex == 0 ? 1 : 2;
        }

        private void SimulateIOTrigger(int trigType)
        {
            operLog.iEventStatus = 0;
            operLog.strOperEvent = trigType == 1 ? "模拟In1触发" : "模拟In2触发";
            operLog.strFailReason = trigType == 1 ? "按当前代码映射触发A面" : "按当前代码映射触发B面";
            AddLog();
            ExeIOTrig(trigType);
        }

     
        private void FrmMainUI_FormClosing(object sender, FormClosingEventArgs e)
        {
           
            SaveUserParam();

            // 1. 停止所有 WinForms 定时器
            timer1.Stop();
            timer1.Dispose();
            timerIOOut?.Stop();
            timerIOOut?.Dispose();
            timerIOIn?.Stop();
            timerIOIn?.Dispose();

            //  _parentCheckTimer?.Stop();
            //    _parentCheckTimer?.Dispose();
            string strPath = cup.p_Paf.strLogPath + "\\RTS_0000.txt";
            fo.SaveRunTimeStatus(strPath, rts);

            //      CloseLaser();
            for (int i = 0; i < MaxLaserCards; i++)
            {
                laserDispatchers[i]?.Dispose();
               // pipeClients[i]?.Dispose();   // 假设 Pipechanel 实现了 IDisposable
                pipeClients[i] = null;
            }

            if (vpWin != null && vpWin._iTaskType == 1)  //如果在实时预览视频，则先停止
            {
                OpenOrCloseCam(false);
                vpWin.bMainTaskFinished = true;
            }
            if (vpWin != null && vpWin._iTaskType == 3)  //如果在连续运行，则先停止
            {
                operLog.strOperEvent = "退出程序";
                operLog.strFailReason = "连续运行任务没有结束";
                operLog.iEventStatus = -1;
                return;
            }
            int count = 0;
            while (vpWin != null && !vpWin.bMainTaskFinished && count < 50)
            {
                count++;
                Thread.Sleep(20);
            }

            CloseCam();
            disconnect();
            if (vpWin != null)
            {
                vpWin.Close(bVPPParamChanged);
                vpWin = null;
            }

            // cdl.CloseDL();

            // 9. 强制垃圾回收（可选）
            GC.Collect();
            GC.WaitForPendingFinalizers();
           
            if (Debugger.IsAttached == false) // 非调试环境
            {
                Environment.Exit(0);
            }

        }

        
        private void splitContainer2_Panel1_SizeChanged(object sender, EventArgs e)
        {
            ViewLayout();
        }
        #endregion 

        #region   交互操作事件区
        //采集停止采集
        private void menuCam_Cap_Click(object sender, EventArgs e)
        {
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "启动图像预览";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                //     MessageBox.Show("当前任务没有结束，不能执行该操作");
                return;
            }
            if (menuCam_Connect.Text == "启动图像预览")
            {
                OpenOrCloseCam(true);
            }
            else
            {
                OpenOrCloseCam(false);
            }
        }
        //重新连接相机
        private void toolBt_Cam_Click(object sender, EventArgs e)
        {
            if (rts.onLS.bTrigg)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "工位切换";
                operLog.strFailReason = "自动模式禁止操作";
                AddLog();
                return;
            }
            if (rts.iModelRole < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "连接相机";
                operLog.strFailReason = "用户身份未验证";
                AddLog();
                   
                //      MessageBox.Show("用户身份未验证，无法操作");
                return;
            }
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "连接相机";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();

                //     MessageBox.Show("当前任务没有结束，不能执行该操作");
                return;
            }

            TryConnectCameras("手动连接");
        }

       
        private void btFun_Click(object sender, EventArgs e)
        {
            //未登录，禁止操作
            if (rts.iModelRole < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "执行功能操作";
                operLog.strFailReason = "用户身份未验证";
                AddLog();
                MessageBox.Show("用户身份未验证，无法操作");
                return;
            }
            if (rts.onLS.bTrigg)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "功能操作";
                operLog.strFailReason = "自动模式禁止操作";
                AddLog();
                return;
            }
            bool bChecked = false;
            try
            {
                switch (((Button)sender).Name)
                {
                    case "btFunAcqOneImage":    //采集单帧图像
                        {
                          
                            operLog.strOperEvent = "采集图像";
                            PeviewOneImage();
                            break;
                        }
                    case "btFun_Preview":   // 实时预览  2024年7月11日 待验证
                        {
                            //需要添加条件
                            /*if (!bRunCondiction(0))
                                return;*/
                            operLog.strOperEvent = "预览图像";
                            PreviewImage(0);
                            break;
                        }
                    case "btFunRunOneTime":
                        if(bRunCondiction(1)) //运行条件判断
                            runonetime();
                        break;
                    case "btFunRunCont":
                        {
                            //执行条件验证
                            if (!rts.onLS.bTrigg)  //如果当前没有执行，要启动执行，则进行条件检测
                            {
                                if (!bRunCondiction(3))
                                    return;
                            }
                            if (cup.p_VPPRTP.bAcqFromCam)     //从相机获取，则启用触发模式
                            {
                                rts.onLS.bTrigg = !rts.onLS.bTrigg;
                                // rts.onLS.bTrigg = true;
                                if (rts.onLS.bTrigg)  //执行连续运行
                                {
                                    vpWin.bMainTaskFinished = false;

                                    vpWin._iTaskType = 3;
                                    operLog.iEventStatus = 0;
                                    operLog.strOperEvent = "启动连续运行";
                                    ((Button)sender).FlatStyle = FlatStyle.Flat;
                                    ((Button)sender).Image = global::VisionApplication.Properties.Resources.Pause;

                                }
                                else  //关闭连续运行
                                {
                                    vpWin._iTaskType = -1;
                                    //st 2022年11月1日  camera2[cup.p_CD.camInd[cup.p_CD.triggleCam]].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                                    //    menuTask_EnableTriggle.Text = "启用触发";
                                    //   toolBtSta_Triggle.Checked = false;
                                    ((Button)sender).Image = global::VisionApplication.Properties.Resources.LoopRun2;
                                    ((Button)sender).FlatStyle = FlatStyle.Popup;
                                    vpWin.bMainTaskFinished = true;
                                    operLog.iEventStatus = 0;
                                    operLog.strOperEvent = "关闭触发模式";
                                    //   ((Button)sender).Image = global::VisionApplication.Properties.Resources.LoopRun2;

                                }

                                /*vpWin._iTaskType = 3;
                                if (vpWin.bMainTaskFinished)
                                {
                                    bTrig = false;
                                    vpWin._iTaskType = 3;
                                    operLog.strOperEvent = "启动连续运行";
                                    ((Button)sender).FlatStyle = FlatStyle.Flat;
                                    ((Button)sender).Image = global::VisionApplication.Properties.Resources.Pause;
                                    vpWin.bMainTaskFinished = false;
                                    //2024年7月13日
                                    vpWin._bContRun = true;
                                    contThread = new Thread(RunContinuousThread);
                                    contThread.Start();
                                    operLog.iEventStatus = 0;
                                }
                                else
                                {
                                    vpWin._bContRun = false;
                                    operLog.strOperEvent = "关闭连续运行";
                                    operLog.iEventStatus = 0;
                                    ((Button)sender).Enabled = false;
                                  
                                }*/


                            }
                            else      //回放模式
                            {
                                /* if (rts.initStatus.iVPPStatus != 0)
                                 {
                                     operLog.iEventStatus = -1;
                                     operLog.strOperEvent = "流程文件初始化";
                                 }
                                 else*/
                                {

                                    if (vpWin.bMainTaskFinished)
                                    {
                                        vpWin._iTaskType = 3;
                                        operLog.strOperEvent = "启动连续运行";
                                        ((Button)sender).FlatStyle = FlatStyle.Flat;
                                        ((Button)sender).Image = global::VisionApplication.Properties.Resources.Pause;
                                        vpWin.bMainTaskFinished = false;
                                        //2024年7月13日
                                        vpWin._bContRun = true;
                                        contThread = new Thread(RunContinuousThread);
                                        //  contThread = new Thread(RunCont);

                                        contThread.Start();
                                        operLog.iEventStatus = 0;
                                    }
                                    else
                                    {
                                        vpWin._bContRun = false;
                                        operLog.strOperEvent = "关闭连续运行";
                                        operLog.iEventStatus = 0;
                                        ((Button)sender).Enabled = false;
                                        /*vpWin._iTaskType = -1;
                                       
                                        //  ((Button)sender).FlatStyle = FlatStyle.Flat;
                                        ((Button)sender).Image = global::VisionApplication.Properties.Resources.LoopRun2;
                                        vpWin.bMainTaskFinished = true;
                                        
                                        operLog.iEventStatus = 0;
                                        btFunRunCont.FlatStyle = FlatStyle.Popup;*/
                                    }


                                }
                            }
                            AddLog();
                            break;
                        }
                    case "btFunMark":  //激光标刻
                       
 
                            break;
                        
                    case "btRedLight_Preview":  //红光轮廓
                        {
                            
                            break;
                        }
                    case "btFunSet":
                        if (!vpWin.bMainTaskFinished)
                        {
                            operLog.iEventStatus = -1;
                            operLog.strOperEvent = "流程设置";
                            operLog.strFailReason = "当前任务未结束";
                            AddLog();
                            ShowCurrentTaskNotFinishedWarning();
                            return;
                        }

                        if (rts.iModelRole < 8)
                        {
                            operLog.iEventStatus = -1;
                            operLog.strOperEvent = "流程设置";
                            operLog.strFailReason = "用户权限不够";
                            AddLog();
                            return;
                        }

                        if (rts.initStatus.iVPPStatus != 0)
                        {
                            operLog.iEventStatus = -1;
                            operLog.strOperEvent = "流程设置";
                            operLog.strFailReason = "流程文件初始化失败";
                            // operLog.strOperEvent = "流程文件初始化";
                        }
                        else
                        {
                            operLog.strOperEvent = "流程设置";
                            if (rts.iModelRole >=8)
                            {
                                int iTmpType = vpWin._iTaskType;
                                vpWin._iTaskType = 5;
                                vpWin.bEnableActivate = false;
                                operLog.iEventStatus = 0;
                                ((Button)sender).FlatStyle = FlatStyle.Flat;
                                vpWin.bMainTaskFinished = false;
                                int iRet = vpWin.VPConfig();
                                //   cup.p_Paf.strVPPFilePath
                                if (iRet % 10 != 0)
                                {
                                    operLog.iEventStatus = -1;
                                    switch (iRet)
                                    {
                                        case 11:
                                            operLog.strOperEvent = "文件正在运行";
                                            break;
                                        case 12:
                                            operLog.strOperEvent = "流程文件保存冲突";
                                            break;
                                        case -1:
                                            operLog.strOperEvent = "未知错误";
                                            break;
                                    }
                                }
                                vpWin._iTaskType = iTmpType;
                                vpWin.bEnableActivate = true;
                                ((Button)sender).FlatStyle = FlatStyle.Popup;

                                vpWin.bMainTaskFinished = true;
                            }


                        }
                        break;
                }
                rts.iTaskType = vpWin._iTaskType;

            }
            catch(Exception ex)
            {
                vpWin._iTaskType = iTmpType;
                ((Button)sender).FlatStyle = FlatStyle.Popup;
                vpWin.bMainTaskFinished = true;
            }
        }

      
        private void menuCam_Set_Click(object sender, EventArgs e)
        {
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "相机参数设置";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                return;
            }
            CamSet();
        }



        private void menuTask_RunOneTime_Click(object sender, EventArgs e)
        {

            btFun_Click(btFunRunOneTime, null);

            /*if (iModelRole < 0)
            {

                MessageBox.Show("用户身份未验证，无法操作");
                return;
            }
            runonetime();*/
        }

        private void toolBt_Preview_Click(object sender, EventArgs e)
        {
            if (rts.iModelRole < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "启动图像预览";
                operLog.strFailReason = "用户身份未验证";
                AddLog();

                //    MessageBox.Show("，无法操作");
                return;
            }
            if (vpWin._iTaskType != 1 && !vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "启动图像预览";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                //    MessageBox.Show("当前任务没有结束，不能执行该操作");
                return;
            }
            if (menuCam_Connect.Text == "启动图像预览")
            {
                vpWin._iTaskType = 1;
                rts.iTaskType = vpWin._iTaskType;
                if (OpenOrCloseCam(true))
                {
                    toolBt_Preview.Checked = true;
                    vpWin.bMainTaskFinished = false;
                }
            }
            else
            {
                if (OpenOrCloseCam(false))
                {
                    toolBt_Preview.Checked = false;
                    vpWin.bMainTaskFinished = true;
                }
            }
        }


        //**20220410 串口打开或者关闭
        private void toolBt_ConnSer_Click(object sender, EventArgs e)
        {
            if (rts.onLS.bTrigg)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "连接串口";
                operLog.strFailReason = "自动模式禁止操作";
                AddLog();
                return;
            }
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "连接串口";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                //    MessageBox.Show("当前任务没有结束，不能执行该操作");
                return;
            }

            bool bConn = false;
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    if (sPortAll[i] != null && sPortAll[i].IsOpen)
                    {
                        bConn = true;
                    }
                }
                if (bConn)
                {
                    disconnect();
                }
                else
                {
                    InitSerial();
                }
            }
            catch(Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "连接串口";
                operLog.strFailReason = ex.Message;
                AddLog();
            }

        }

        public void InitSaveRes()
        {
            SaveResdirJudge(true);


            cup.p_VPPRTP.bSaveRes = !cup.p_VPPRTP.bSaveRes;
            toolBtSta_SaveRes.Checked = cup.p_VPPRTP.bSaveRes;
            rts.onLS.bSaveRes = cup.p_VPPRTP.bSaveRes;


            //   vpWin.SetSaveRes(cup.p_VPPRTP.bSaveRes);
            if (vpWin != null)
            {
                vpWin._strSaveResPath = strSaveResPath;
                vpWin._cup = cup;
            }

            SaveUserParam();

        }
        public void InitRecord()
        {
            try
            {
                dirJudge(cup.p_VPPRTP.bAcqFromCam);
                if (vpWin != null)
                {
                    vpWin._strSigSavePathS = strSigSavePathS;
                    vpWin._strConSavePathS = strConSavePathS;
                    
                }

                acqFIFO._strSigSavePathS = strSigSavePathS;
                acqFIFO._strConSavePathS = strConSavePathS;



                toolBt_Record.Checked = cup.p_VPPRTP.bRecord;
                vpWin._bRecord = cup.p_VPPRTP.bRecord;
                SaveUserParam();
                operLog.iEventStatus = 0;
            }
            catch
            {
                operLog.iEventStatus = -1;
            }
            //    operLog.strOperEvent = "初始化存储目录";

            //   AddLog();
        }

        string strSaveResPath = "";
        string[] strSigSavePathS = new string[maxCameraNum];
        string[] strConSavePathS = new string[maxCameraNum];
        string[] strErrPathS = new string[maxCameraNum];

        string strSigSavePath, strConSavePath;
        string strErrPath;

        //保存结果路径初始化
        public bool SaveResdirJudge(bool bSaveRes)
        {
            string strDate;
            string strPath;
            strPath = cup.p_Paf.strResSavePath;
            strDate = DateTime.Now.ToString("yyyyMMdd");
            strSaveResPath = strPath + strDate + "\\";
            if (Directory.Exists(strPath) == false)//如果不存在就创建file文件夹
            {
                Directory.CreateDirectory(strSaveResPath);
            }

            return true;
        }
        public bool dirJudge(bool bAcq)
        {
            string strDate;
            string strPath;
            try
            {
                if (bAcq)   //实时采集
                {
                    //单帧存储目录
                    strSigSavePath = cup.p_Paf.strSigSavePath;
                    strDate = DateTime.Now.ToString("yyyyMMdd");

                    if (strSigSavePath.Substring(strSigSavePath.Length - 1, 1) != "\\")
                        strPath = strSigSavePath + "\\" + strDate + "\\";
                    else
                        strPath = strSigSavePath + strDate + "\\";

                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        strSigSavePathS[i] = strPath + cup.p_LI.strCamPrefix[i];
                        if (Directory.Exists(strSigSavePathS[i]) == false)//如果不存在就创建file文件夹
                        {
                            Directory.CreateDirectory(strSigSavePathS[i]);
                        }
                    }

                    //成组数据存储目录
                    strConSavePath = cup.p_Paf.strConSavePath;

                    if (strConSavePath.Substring(strConSavePath.Length - 1, 1) != "\\")
                        strPath = strConSavePath + "\\" + strDate + "\\";
                    else
                        strPath = strConSavePath + strDate + "\\";

                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        strConSavePathS[i] = strPath + cup.p_LI.strCamPrefix[i];
                        if (Directory.Exists(strConSavePathS[i]) == false)//如果不存在就创建file文件夹
                        {
                            Directory.CreateDirectory(strConSavePathS[i]);
                        }
                    }


                    //错误识别图片存储
                    strErrPath = cup.p_Paf.strConSavePath;
                    if (strErrPath.Substring(strErrPath.Length - 1, 1) != "\\")
                        strPath = strErrPath + "\\" + strDate + "\\";
                    else
                        strPath = strErrPath + strDate + "\\";

                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        strErrPathS[i] = strPath + cup.p_LI.strCamPrefix[i];
                        if (Directory.Exists(strErrPathS[i]) == false)//如果不存在就创建file文件夹
                        {
                            Directory.CreateDirectory(strErrPathS[i]);
                        }
                    }

                }
                else    // 回放
                {
                    //单帧存储目录
                    strSigSavePath = cup.p_Paf.strSigSavePath;
                    string strPlayBack = cup.p_Paf.strPlaybackPath;
                    if (strPlayBack.Substring(strPlayBack.Length - 1, 1) == "\\")
                        strPlayBack = strPlayBack.Substring(0, strPlayBack.Length - 1);
                    // else
                    //    strPlayBack = strPlayBack.Substring(0, strPlayBack.Length - 1);

                    strDate = strPlayBack.Substring(strPlayBack.Length - 8, 8);

                    if (strSigSavePath.Substring(strSigSavePath.Length - 1, 1) != "\\")
                        strPath = strSigSavePath + "\\" + strDate + "\\";
                    else
                        strPath = strSigSavePath + strDate + "\\";

                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        strSigSavePathS[i] = strPath + cup.p_LI.strCamPrefix[i];
                        if (Directory.Exists(strSigSavePathS[i]) == false)//如果不存在就创建file文件夹
                        {
                            Directory.CreateDirectory(strSigSavePathS[i]);
                        }
                    }

                    //成组数据存储目录
                    strConSavePath = cup.p_Paf.strConSavePath;

                    if (strConSavePath.Substring(strConSavePath.Length - 1, 1) != "\\")
                        strPath = strConSavePath + "\\" + strDate + "\\";
                    else
                        strPath = strConSavePath + strDate + "\\";

                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        strConSavePathS[i] = strPath + cup.p_LI.strCamPrefix[i];
                        if (Directory.Exists(strConSavePathS[i]) == false)//如果不存在就创建file文件夹
                        {
                            Directory.CreateDirectory(strConSavePathS[i]);
                        }
                    }


                    //错误识别图片存储
                    strErrPath = cup.p_Paf.strErrPath;
                    if (strErrPath.Substring(strErrPath.Length - 1, 1) != "\\")
                        strPath = strErrPath + "\\" + strDate + "\\";
                    else
                        strPath = strErrPath + strDate + "\\";

                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        strErrPathS[i] = strPath + cup.p_LI.strCamPrefix[i];
                        if (Directory.Exists(strErrPathS[i]) == false)//如果不存在就创建file文件夹
                        {
                            Directory.CreateDirectory(strErrPathS[i]);
                        }
                    }
                }



            }
            catch
            {
                return false;
            }
            return true;
        }


        private void menuTask_RunCont_Click(object sender, EventArgs e)
        {

            btFun_Click(btFunRunCont, null);


            /*if (iModelRole < 0)
            {

                MessageBox.Show("用户身份未验证，无法操作");
                return;
            }
            RunCont();*/
            //  vpWin.RunCont();
        }
      
        private void menu_View_EventStatus_Click(object sender, EventArgs e)
        {
            menu_View_EventStatus.Checked = !menu_View_EventStatus.Checked;
            ViewChange();
        }

        private void menu_View_SystemLog_Click(object sender, EventArgs e)
        {
            menu_View_SystemLog.Checked = !menu_View_SystemLog.Checked;
            ViewChange();
        }
        private void menu_View_LaserWin_Click(object sender, EventArgs e)
        {
            menu_View_LaserWin.Checked = !menu_View_LaserWin.Checked;
            ViewChange();
        }
        private void button_ResetStatistics_Click(object sender, EventArgs e)
        {
            iAcquisitionsTimes = 0;
            iErrAcquisitionsTimes = 0;
            iErrExecuteTimes = 0;
            vpWin.ResetStatisticsForJobClick();
            UpdateStatisticsForJob(0);
        }

        private void menuCam_OpenCloseDev_Click(object sender, EventArgs e)
        {
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "连接相机";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                //   MessageBox.Show("当前任务没有结束，不能执行该操作");
                return;
            }

            TryConnectCameras("手动连接");
        }

        private void 软触发一次ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!cup.p_CD.bTriggle || camera2[cup.p_CD.triggleCam] == null)
                return;

            if (hikCanGrab)
            {
                OpenOrCloseCam(false);
            }
            //  acqFIFO._HikCam[cup.p_CD.triggleCam].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);
            vpWin._iTaskType = 6;
            nRet = acqFIFO._HikCam[cup.p_CD.camInd[cup.p_CD.triggleCam]].MV_CC_SetCommandValue_NET("TriggerSoftware");
            if (MyCamera.MV_OK != nRet)
            {
                // richTextBox.Text += String.Format("No.{0} Set software trigger failed! nRet=0x{1}\r\n", (i + 1).ToString(), nRet.ToString("X"));
            }

            vpWin.SetTrigger(rts.onLS.bTrigg, cup.p_CD.triggleMode, cup.p_CD.camInd[cup.p_CD.triggleCam]);
        }

        private void toolBt_LS_Click(object sender, EventArgs e)
        {
            if (rts.iModelRole < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "光源调试";
                operLog.strFailReason = "用户身份未验证";
                AddLog();

                //     MessageBox.Show("用户身份未验证，无法操作");
                return;
            }
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "光源调试";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                //   MessageBox.Show("当前任务没有结束，不能执行该操作");
                return;
            }
            frm_Serial fs = new frm_Serial(csd);
            fs.ShowDialog();
            csd.p_LS = fs.p_LS;
            csd.p_LS2 = fs.p_LS2;
            cup.p_LS = fs.p_LS;
            cup.p_LS2 = fs.p_LS2;
            SaveUserParam();


        }

        private void menuCam_Close_Click(object sender, EventArgs e)
        {
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "断开相机";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                //   MessageBox.Show("当前任务没有结束，不能执行该操作");
                return;
            }
            lock (_cameraConnectionSync)
            {
                if (_cameraConnectionInProgress)
                {
                    LogCameraConnection("断开相机", false, "相机连接操作尚未结束");
                    return;
                }
                _cameraManuallyDisconnected = true;
                _cameraConnectionInProgress = true;
            }
            try
            {
                if (!CloseCam()) return;
                rts.initStatus.bCamInit = false;
                rts.initStatus.bCamPrepared = false;
                LogCameraConnection("断开相机", true, "设备连接已释放，等待手动连接");
            }
            finally
            {
                lock (_cameraConnectionSync) _cameraConnectionInProgress = false;
            }
        }

        private void toolBt_RuntimeSet_Click(object sender, EventArgs e)
        {
            if (rts.onLS.bTrigg)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "参数设置";
                operLog.strFailReason = "自动模式禁止操作";
                AddLog();
                return;
            }
            if (rts.iModelRole < 2)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "用户参数设置";
                operLog.strFailReason = "用户权限等级不够";
                AddLog();
                return;
            }

            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "用户参数设置";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                return;
            }
            Frm_RuntimeSet frs = new Frm_RuntimeSet(cup);
            //  PickSubRecordsName();
            //  frs._cup = cup;
            if (frs.ShowDialog() == DialogResult.OK)
            {
                bool bSucc;
                cup = frs._cup;
                vpWin._cup = cup;
                serialInd = cup.p_SD.serInd;
                SaveUserParam();
                ApplyJobSelection(iJobIndex);

                //VPP文件改变，则重新加载VPP
                if (frs.bVPPChange)
                {

                    // rts.initStatus.bVPPInit = vpWin.ReLoadVPP(frs.strVPPFileName);
                    //2025年10月21日 重新加载VPP文件
                    LoadVPFile(true);
                    if (rts.initStatus.bVPPInit)
                    {
                         InitVisionControl();
                        //   bSucc = vpWin.SetVPPRuntimeParam();             //visionpro控件初始化
                    }

                    /* rts.initStatus.iVPPStatus = vpWin.mInitError;
                     if (rts.initStatus.bVPPInit)
                         operLog.iEventStatus = 0;
                     else
                         operLog.iEventStatus = 1;
                     operLog.strOperEvent = "重新加载流程文件";
                     vpWin.bChkSubrecord = false;
                     AddLog();*/
                }
                //  vpWin.SetDiagParam(cup);
                bVPPParamChanged = frs.bVPPParamChanged;
                bSucc = true;
                if (bVPPParamChanged)
                {
                    iUsedCamraNum = 0;
                }
                InitLocalFile();
                InitRecord();
                if (!bSucc)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "设置流程参数";

                }
                else
                {
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = "保存配置文件";
                }


                //     cdl.InitDL(cup.p_DL);
                AddLog();
            }
        }

        private void button_ResetStatisticsForAllJobs_Click(object sender, EventArgs e)
        {
            vpWin.ResetStatisticsForAllJobsClick();
            UpdateStatisticsForJob(0);
        }

        private void SetShowType(int iShowType)
        {
            this.iShowType = iShowType;
            switch (iShowType)
            {
                case 0:

                    menuDisp_Default.Checked = true;
                    menuDisp_FX.Checked = false;
                    menuDisp_Grey.Checked = false;
                    menuDisp_PMA.Checked = false;
                    break;
                case 1:

                    menuDisp_Default.Checked = false;
                    menuDisp_FX.Checked = false;
                    menuDisp_Grey.Checked = true;
                    menuDisp_PMA.Checked = false;
                    break;
                case 2:
                    menuDisp_Default.Checked = false;
                    menuDisp_FX.Checked = true;
                    menuDisp_Grey.Checked = false;
                    menuDisp_PMA.Checked = false;
                    break;
                case 3:
                    menuDisp_Default.Checked = false;
                    menuDisp_FX.Checked = false;
                    menuDisp_Grey.Checked = false;
                    menuDisp_PMA.Checked = true;
                    break;
            }
            if (vpWin._iTaskType == 2 || vpWin._iTaskType == 3)           //运行一次操作
                vpWin.SetGUIShow(iShowType);
            else if (vpWin._iTaskType == 0)      //采集图像，只能显示灰度图和光度图
            {
                if (iShowType == 0)
                    vpWin.PreViewOneImageFromFile(iFileIndex, false, iJobIndex);
               
            }
        }
        private void menuDisp_Default_Click(object sender, EventArgs e)
        {

            SetShowType(0);
        }

        private void menuDisp_FX_Click(object sender, EventArgs e)
        {
            SetShowType(2);

        }

        private void menuDisp_Grey_Click(object sender, EventArgs e)
        {
            SetShowType(1);
        }

        private void menuDisp_PMA_Click(object sender, EventArgs e)
        {
            SetShowType(3);
        }

        private void menuDisp_Update_Click(object sender, EventArgs e)
        {
          //  InitSubRecordsList();
        }

        private void menuManage_Job_Click(object sender, EventArgs e)
        {

        }

        private byte[] LoadImage()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "(*.jpg;*.JPEG;*.jpeg)|*.*";
            DialogResult load = openFileDialog1.ShowDialog();
            string filename = openFileDialog1.FileName;

            if (load != DialogResult.OK || string.IsNullOrEmpty(filename))
            {
                MessageBox.Show("未选择图片！", "提示");
            }
            else
            {

                Image image = Image.FromFile(filename);
                MemoryStream ms = new MemoryStream();

                image.Save(ms, image.RawFormat);
                byte[] byteArray = ms.ToArray();
                return byteArray;

            }
            return null;
        }

        public static byte[] GetBGRValues(Bitmap bmp, out int stride)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            stride = bmpData.Stride;
            var rowBytes = bmpData.Width * Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
            var imgBytes = bmp.Height * rowBytes;
            byte[] rgbValues = new byte[imgBytes];
            IntPtr ptr = bmpData.Scan0;
            for (var i = 0; i < bmp.Height; i++)
            {
                Marshal.Copy(ptr, rgbValues, i * rowBytes, rowBytes);
                ptr += bmpData.Stride;
            }
            bmp.UnlockBits(bmpData);
            return rgbValues;
        }

        private void menuManage_User_Click(object sender, EventArgs e)
        {
            if (rts.iModelRole > 2)
            {
                FormSetPasswords fsp = new FormSetPasswords(mPasswordFile);
                fsp.ShowDialog();
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "用户管理";
                operLog.strFailReason = "权限不够";
                AddLog();
                //   MessageBox.Show("你的权限不够");
            }
        }

        private void menuManage_UserLogin_Click(object sender, EventArgs e)
        {
            if (rts.iModelRole < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "用户登录";
                operLog.strFailReason = "用户身份未验证";
                AddLog();
                // rts.iModelRole = iModelRole;
                //  MessageBox.Show("用户身份未验证，无法登录");
                return;
            }
          //  int tmpRole=
            Login login = new Login(mPasswordFile);
            if (login.ShowDialog() == DialogResult.OK)
            {
                rts.iModelRole = Math.Max(login.iModelRole, rts.iModelRole);
                //   rts.iModelRole = iModelRole;
                operLog.strUserName = login.strID;
                operLog.strOperEvent = "用户登录";
                if (rts.iModelRole > 0)
                {
                    operLog.iEventStatus = 0;
                }
                else
                {
                    operLog.iEventStatus = -1;
                }
                if(rts.iModelRole>3)
                {
                    btFunSet.Visible = true;
                }
                else
                {
                    btFunSet.Visible = false;
                }
                AddLog();
            }
        }

        private void menuTask_AcqOneImage_Click(object sender, EventArgs e)
        {
            btFun_Click(btFunAcqOneImage, null);


        }

        private void menuLight_Step_Click(object sender, EventArgs e)
        {
            if (cup.p_VPPRTP.bAcqFromCam && (vpWin._iTaskType == 2 || vpWin._iTaskType == 3) && !vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "时序打光";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                //    MessageBox.Show("当前任务没有结束，不能执行该操作");
                return;
            }
            operLog.iEventStatus = 0;
            operLog.strOperEvent = "时序打光";

            string menuName = sender.ToString();
            bool bAcq = menuLight_Acq.Checked;
            menuLight_Step0.Checked = false;
            menuLight_Step1.Checked = false;
            menuLight_Step2.Checked = false;
            menuLight_Step3.Checked = false;
            menuLight_Step4.Checked = false;
            menuLight_Step5.Checked = false;
            menuLight_StepAll.Checked = false;
            ((ToolStripMenuItem)sender).Checked = true;
            switch (menuName)
            {
                case "时序0":
                    vpWin.StepTest(0, bAcq);
                    break;
                case "时序1":
                    vpWin.StepTest(1, bAcq);
                    break;
                case "时序2":
                    vpWin.StepTest(2, bAcq);
                    break;
                case "时序3":
                    vpWin.StepTest(3, bAcq);
                    break;
                case "时序4":
                    vpWin.StepTest(4, bAcq);
                    break;
                case "时序5":
                    vpWin.StepTest(5, bAcq);
                    break;
                case "全流程":
                    vpWin.StepTest(0, bAcq);
                    Thread.Sleep(100);
                    vpWin.StepTest(1, bAcq);
                    Thread.Sleep(100);
                    vpWin.StepTest(2, bAcq);
                    Thread.Sleep(100);
                    vpWin.StepTest(3, bAcq);
                    Thread.Sleep(100);
                    vpWin.StepTest(4, bAcq);
                    Thread.Sleep(100);
                    vpWin.StepTest(5, bAcq);
                    Thread.Sleep(100);

                    break;
            }
            AddLog();
        }

        private void menuLight_Acq_Click(object sender, EventArgs e)
        {
            menuLight_Acq.Checked = !menuLight_Acq.Checked;
        }

        private void lblRes0_Click(object sender, EventArgs e)
        {

        }

        private void lblRes1_Click(object sender, EventArgs e)
        {

        }

        private void lblRes2_Click(object sender, EventArgs e)
        {

        }


        private void toolBtSta_UseFX_Click(object sender, EventArgs e)
        {
            toolBtSta_UseFX.Checked = !toolBtSta_UseFX.Checked;
            cup.p_VPPRTP.bUseFX = toolBtSta_UseFX.Checked;
            vpWin.SetUseFX(cup.p_VPPRTP.bUseFX);
            SaveUserParam();
        }

        
        private void toolBtSta_LockImage_Click(object sender, EventArgs e)
        {
            /* if (toolBtSta_LockImage.Checked)  //关闭板卡
             {
                 lczParam.cmdType = 2;
                 pipeClient.SendParam(lczParam);
             }
             else
             {
                 jczPipThread = new Thread(ReConnToJCZ);
                 jczPipThread.Start();
             }*/

            toolBtSta_LockImage.Checked = !toolBtSta_LockImage.Checked;
            if (toolBtSta_LockImage.Checked)
            {
                toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.Lock;
                //  toolTip1.SetToolTip(toolBtSta_LockImage, "自动运行模式");
                LockWithFromCam();
            }
            else
            {
                toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.UnLock;
              //  toolTip1.SetToolTip(toolBtSta_LockImage, "手动运行模式");
            }
            rts.onLS.bTrigg = toolBtSta_LockImage.Checked;
            tabControl1.Enabled = !rts.onLS.bTrigg;
            menuStrip1.Enabled = !rts.onLS.bTrigg;

        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {

            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        //标志性状态
        private void InitCheckStatus()
        {
            toolBtSta_MarkAfterLocated.Checked = rts.onLS.bLock;
            if (toolBtSta_MarkAfterLocated.Checked)
            {
                toolBtSta_MarkAfterLocated.Image = global::VisionApplication.Properties.Resources.WithMark;
                toolBtSta_MarkAfterLocated.ToolTipText = "定位后标刻";
            }
            else
            {
                toolBtSta_MarkAfterLocated.Image = global::VisionApplication.Properties.Resources.WithOutMark;
                toolBtSta_MarkAfterLocated.ToolTipText = "定位后不标刻";
            }
            toolBtSta_LockImage.Checked = rts.onLS.bTrigg;
            if (toolBtSta_LockImage.Checked)
            {
                toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.Lock;
                //  toolTip1.SetToolTip(toolBtSta_LockImage, "自动运行模式");
            }
            else
            {
                toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.UnLock;
                //  toolTip1.SetToolTip(toolBtSta_LockImage, "手动运行模式");
            }
            
            toolBtSta_FromCam.Checked = cup.p_VPPRTP.bAcqFromCam;
            if (toolBtSta_FromCam.Checked)
                toolBtSta_FromCam.Image = global::VisionApplication.Properties.Resources.ImageAcqFromCam;
            else
                toolBtSta_FromCam.Image = global::VisionApplication.Properties.Resources.ImageAcqFromFIle;
            toolBtSta_UseFX.Checked = cup.p_VPPRTP.bUseFX;
            toolBt_Record.Checked = cup.p_VPPRTP.bRecord;
            if (toolBt_Record.Checked)
                toolBt_Record.Image = global::VisionApplication.Properties.Resources.record;
            else
                toolBt_Record.Image = global::VisionApplication.Properties.Resources.recordStop;


            toolBtSta_SaveRes.Checked = cup.p_VPPRTP.bSaveRes;
            if (cup.p_VPPRTP.bDeepInfer)
                menuTask_VisionInfer.Checked = true;
            else
                menuTask_VisionInfer.Checked = false;
            if (cup.p_VPPRTP.bFXRestruct)
            {
                menuTask_FXRestruct.Checked = true;
            }
            else
            {
                menuTask_FXRestruct.Checked = false;
                menuDisp_FX.Checked = false;
            }

            if (cup.p_VPPRTP.bVisionInfer)
                menuTask_VisionInfer.Checked = true;
            else
                menuTask_VisionInfer.Checked = false;


            if (cup.p_VPPRTP.bSaveRes)
            {
                InitSaveRes();
            }

            switch (cup.p_VPPRTP.iLayoutType)
            {
                case 1:
                    menu_View_Layout1.Checked = true;
                    break;
                case 2:
                    menu_View_Layout2.Checked = true;
                    break;
                case 3:
                    menu_View_Layout3.Checked = true;
                    break;
                case 4:                              // 新增
                    menu_View_Layout4.Checked = true;
                  
                    break;
                case 5:
                    menu_View_Layout5.Checked = true;
                    break;
            }
        }

        private void toolBt_Record_Click(object sender, EventArgs e)
        {

            cup.p_VPPRTP.bRecord = !cup.p_VPPRTP.bRecord;
            toolBt_Record.Checked = cup.p_VPPRTP.bRecord;
            rts.onLS.bRecord = cup.p_VPPRTP.bRecord;
            vpWin._bRecord = cup.p_VPPRTP.bRecord;
            if (toolBt_Record.Checked)
            {
                toolBt_Record.Image = global::VisionApplication.Properties.Resources.record;
                InitRecord();
            }
            else
            {
                toolBt_Record.Image = global::VisionApplication.Properties.Resources.recordStop;
            }

        }

        private void toolBtSta_FromCam_Click(object sender, EventArgs e)
        {
            if (rts.onLS.bTrigg)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "修改数据源";
                operLog.strFailReason = "自动模式禁止操作";
                AddLog();
                return;
            }
            //toolBtSta_FromCam.Checked = !toolBtSta_FromCam.Checked;
            cup.p_VPPRTP.bAcqFromCam = !cup.p_VPPRTP.bAcqFromCam;
            toolBtSta_FromCam.Checked = cup.p_VPPRTP.bAcqFromCam;
            if (!cup.p_VPPRTP.bAcqFromCam)   //回放状态
            {
                toolBtSta_FromCam.Image = global::VisionApplication.Properties.Resources.ImageAcqFromFIle;
                //  btFunRunCont.Enabled = true;
                //   btFunTrigg.Enabled = false;
            }
            else  //实时状态
            {
                toolBtSta_FromCam.Image = global::VisionApplication.Properties.Resources.ImageAcqFromCam;
                //   btFunRunCont.Enabled = false;
                //   btFunTrigg.Enabled = true;
            }

            rts.onLS.bAcqFromCam = cup.p_VPPRTP.bAcqFromCam;
            if (vpWin != null)
                vpWin.SetFromCam(cup.p_VPPRTP.bAcqFromCam || !cup.p_VPPRTP.bAcqFromIDB);

            InitLocalFile();
            InitRecord();
        }

        private void LockWithFromCam()
        {
            cup.p_VPPRTP.bAcqFromCam = true;
            toolBtSta_FromCam.Checked = cup.p_VPPRTP.bAcqFromCam;
            
            {
                toolBtSta_FromCam.Image = global::VisionApplication.Properties.Resources.ImageAcqFromCam;
               
            }
            rts.onLS.bAcqFromCam = cup.p_VPPRTP.bAcqFromCam;
            if (vpWin != null)
                vpWin.SetFromCam(cup.p_VPPRTP.bAcqFromCam);
        }

        private void toolBtSta_SaveRes_Click(object sender, EventArgs e)
        {
            InitSaveRes();
        }

        private void toolBt_BackWard_Click(object sender, EventArgs e)
        {
            if (!cup.p_VPPRTP.bAcqFromCam && cup.p_VPPRTP.bAcqFromIDB)
            {
                iNextIndex = vpWin.SetPreviewImage();
                toolStripStatus_No.Text = iCurrentIndex.ToString() + "|" + iNextIndex.ToString() + "||" + iFileGroupLen;
            }
            else if (!cup.p_VPPRTP.bAcqFromCam && !cup.p_VPPRTP.bAcqFromIDB)
            {
                //  iNextFileIndex = iNextFileIndex++; 
                iNextFileIndex--;
                if (iNextFileIndex < 0)
                    iNextFileIndex = 0;
                // iFileIndex = iNextFileIndex;
                toolStripStatus_No.Text = iFileIndex.ToString() + "|" + iNextFileIndex.ToString() + "||" + iFileGroupLen;
            }
        }

        private void menuManage_UserLogout_Click(object sender, EventArgs e)
        {
            if (rts.iModelRole < 0)
                return;
          //  if (rts.iModelRole < 4)
            {
                rts.iModelRole = 0;
                btFunSet.Visible = false;
            }
            operLog.strUserName = "";
            operLog.strOperEvent = "用户注销";
            operLog.iEventStatus = 0;

            AddLog();
        }

        private void txt_NextImageIndex_KeyPress(object sender, KeyPressEventArgs e)
        {
            /* if (e.KeyChar == 13)
             {

                 Int32.TryParse(txt_NextImageIndex.Text, out iNextIndex);

                 if (!cup.p_VPPRTP.bAcqFromCam && cup.p_VPPRTP.bAcqFromIDB)
                 {
                     vpWin.SetNextImageIndex(iNextIndex);
                     toolStripStatus_No.Text = iCurrentIndex.ToString() + "|" + iNextIndex.ToString() + "||" + iFileGroupLen;
                 }
                 else if (!cup.p_VPPRTP.bAcqFromCam && !cup.p_VPPRTP.bAcqFromIDB)
                 {
                    iNextFileIndex=iNextIndex;
                     toolStripStatus_No.Text = iFileIndex.ToString() + "|" + iNextFileIndex.ToString() + "||" + iFileGroupLen + "  Time:" + acqFIFO.strTime;
                 }
             } */
        }

        private void menu_View_Layout1_Click(object sender, EventArgs e)
        {
            vpWin.ViewLayout(1);
            iLayoutType = 1;
            menu_View_Layout1.Checked = true;
            menu_View_Layout2.Checked = false;
            menu_View_Layout4.Checked = false;
            menu_View_Layout3.Checked = false;
            menu_View_Layout5.Checked = false;
            cup.p_VPPRTP.iLayoutType = iLayoutType;
            SaveUserParam();
        }
        private void menu_View_Layout2_Click(object sender, EventArgs e)
        {
            vpWin.ViewLayout(2);
            iLayoutType = 2;

            menu_View_Layout1.Checked = false;
            menu_View_Layout2.Checked = true;
            menu_View_Layout3.Checked = false;
            menu_View_Layout4.Checked = false;
            menu_View_Layout5.Checked = false;

            cup.p_VPPRTP.iLayoutType = iLayoutType;
            SaveUserParam();
        }

        private void menu_View_Layout4_Click(object sender, EventArgs e)
        {
            vpWin.ViewLayout(4);
            iLayoutType = 4;

            menu_View_Layout1.Checked = false;
            menu_View_Layout2.Checked = false;
            menu_View_Layout3.Checked = false;
            menu_View_Layout4.Checked = true;
            menu_View_Layout5.Checked = false;

            cup.p_VPPRTP.iLayoutType = iLayoutType;
            SaveUserParam();
        }

        private void menu_View_Layout3_Click(object sender, EventArgs e)
        {
            vpWin.ViewLayout(3);
            iLayoutType = 3;
            menu_View_Layout1.Checked = false;
            menu_View_Layout2.Checked = false;
            menu_View_Layout4.Checked = false;
            menu_View_Layout3.Checked = true;
            menu_View_Layout5.Checked = false;
            cup.p_VPPRTP.iLayoutType = iLayoutType;
            SaveUserParam();
        }
        private void menu_View_Layout5_Click(object sender, EventArgs e)
        {
            vpWin.ViewLayout(5);
            iLayoutType = 5;
            menu_View_Layout1.Checked = false;
            menu_View_Layout2.Checked = false;
            menu_View_Layout4.Checked = false;
            menu_View_Layout3.Checked = false;
            menu_View_Layout5.Checked = true;
            cup.p_VPPRTP.iLayoutType = iLayoutType;
            SaveUserParam();
        }

        private void menu_View_LayoutEq_Click(object sender, EventArgs e)
        {
            menu_View_LayoutEq.Checked = !menu_View_LayoutEq.Checked;
            vpWin.bEqualDisr = menu_View_LayoutEq.Checked;
            vpWin.ViewLayout(iLayoutType);
        }


        /// ///////////// ///////////// ///////////// ///////////// ///////////// //////////
        /// 
        private void menuTask_blow_Click(object sender, EventArgs e)
        {
            operLog.strOperEvent = "吹气";
            strOrder = "吹气";
            if (csd.Cylinderblow(sPortCy, cup.p_BP.perBlowTime))
            {
                operLog.iEventStatus = 0;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "通信异常";
            }
            AddLog();
        }


        //线体启动
        private void menuTask_Start_Click(object sender, EventArgs e)
        {
            csd.MachineStartOrStop(sPortCy, true);
        }
        //线体暂停
        private void menuTask_Pause_Click(object sender, EventArgs e)
        {
          //  csd.MachineStartOrStop(sPortCy, false);
        }
         
        private async void menuTask_OK_Click(object sender, EventArgs e)
        {
            bool[] testResults;
            DialogResult dialogResult = ShowIOCombinationTestDialog(out testResults);
            if (dialogResult == DialogResult.Abort)
            {
                await ClearIOOutputsFromMenuAsync("测试窗口全部关闭");
                return;
            }
            if (dialogResult != DialogResult.OK)
                return;

            bool[] participants = { true, true };
            byte outputBitmap;
            string errorMessage;
            if (!TryBuildIOOutputBitmap(testResults, participants, out outputBitmap, out errorMessage))
            {
                WriteIOFeedbackLog(-1, "IO测试组合无效", errorMessage);
                MessageBox.Show(errorMessage, "IO输出测试", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool succeeded = await Task.Run(() =>
                csd.SendIOOutputBitmapPulse(sPortCy, outputBitmap, cup.p_IOPP.IOTimeDelay, out errorMessage));
            if (succeeded)
            {
                WriteIOFeedbackLog(0, "IO测试组合",
                    $"位图0x{outputBitmap:X2}发送完成，保持{cup.p_IOPP.IOTimeDelay}ms后已全部关闭");
            }
            else
            {
                WriteIOFeedbackLog(-1, "IO测试组合失败", errorMessage);
                MessageBox.Show(errorMessage, "IO输出测试", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void menuTask_FeedBack_Click(object sender, EventArgs e)
        {

        }

        //检测反馈NG
        private async void menuTask_NG_Click(object sender, EventArgs e)
        {
            await ClearIOOutputsFromMenuAsync("反馈控制菜单全部关闭");
        }
        private void menuTask_NG2_Click(object sender, EventArgs e)
        {
            menuTask_NG_Click(sender, e);
        }

        private DialogResult ShowIOCombinationTestDialog(out bool[] testResults)
        {
            testResults = new bool[ioFeedbackJobCount];
            using (Form dialog = new Form())
            using (ComboBox cmbJob0 = new ComboBox())
            using (ComboBox cmbJob1 = new ComboBox())
            using (Button btSend = new Button())
            using (Button btClear = new Button())
            using (Button btCancel = new Button())
            {
                dialog.Text = "IO组合输出测试";
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(330, 165);
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;

                Label lblJob0 = new Label { Text = "工位1 / Job0", AutoSize = true, Location = new Point(24, 24) };
                Label lblJob1 = new Label { Text = "工位2 / Job1", AutoSize = true, Location = new Point(24, 62) };
                cmbJob0.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbJob1.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbJob0.Items.AddRange(new object[] { "OK", "NG" });
                cmbJob1.Items.AddRange(new object[] { "OK", "NG" });
                cmbJob0.SelectedIndex = 0;
                cmbJob1.SelectedIndex = 0;
                cmbJob0.Location = new Point(135, 20);
                cmbJob1.Location = new Point(135, 58);
                cmbJob0.Width = cmbJob1.Width = 150;

                btSend.Text = "发送测试组合";
                btSend.Location = new Point(24, 110);
                btSend.Size = new Size(100, 30);
                btSend.DialogResult = DialogResult.OK;
                btClear.Text = "全部关闭";
                btClear.Location = new Point(132, 110);
                btClear.Size = new Size(80, 30);
                btClear.DialogResult = DialogResult.Abort;
                btCancel.Text = "取消";
                btCancel.Location = new Point(220, 110);
                btCancel.Size = new Size(65, 30);
                btCancel.DialogResult = DialogResult.Cancel;

                dialog.Controls.AddRange(new Control[]
                {
                    lblJob0, lblJob1, cmbJob0, cmbJob1, btSend, btClear, btCancel
                });
                dialog.AcceptButton = btSend;
                dialog.CancelButton = btCancel;

                DialogResult result = dialog.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    testResults[0] = cmbJob0.SelectedIndex == 0;
                    testResults[1] = cmbJob1.SelectedIndex == 0;
                }
                return result;
            }
        }

        private async Task ClearIOOutputsFromMenuAsync(string reason)
        {
            string errorMessage = string.Empty;
            bool succeeded = await Task.Run(() => csd.ClearIOOutputs(sPortCy, out errorMessage));
            if (succeeded)
            {
                ResetIOFeedbackCycle(reason);
                WriteIOFeedbackLog(0, "IO全部关闭", "Y1～Y8已发送全部OFF");
            }
            else
            {
                WriteIOFeedbackLog(-1, "IO全部关闭失败", errorMessage);
                MessageBox.Show(errorMessage, "IO输出", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void menuTask_EnableTriggle_Click(object sender, EventArgs e)
        {
            // rts.onLS.bTrigg = !rts.onLS.bTrigg;
            if (!rts.onLS.bTrigg)
            {
                //  if (RunTrigModeInit())
                {
                    //st 2022年11月1日    camera2[cup.p_CD.camInd[cup.p_CD.triggleCam]].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                    rts.onLS.bTrigg = !rts.onLS.bTrigg;
                    menuTask_EnableTriggle.Text = "关闭触发";
                }
            }
            else
            {
                vpWin._iTaskType = -1;
                //st 2022年11月1日  camera2[cup.p_CD.camInd[cup.p_CD.triggleCam]].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                menuTask_EnableTriggle.Text = "启用触发";
                rts.onLS.bTrigg = !rts.onLS.bTrigg;
            }
        }
        private void toolBtSta_Triggle_Click(object sender, EventArgs e)
        {
            /*if (!toolBtSta_Triggle.Checked)
            {
                lczParam.cmdType = 3;
                lczParam.bOpen = true;
                pipeClient.SendParam(lczParam);
            }
            else
            {
                lczParam.cmdType = 3;
                lczParam.bOpen = false;
                pipeClient.SendParam(lczParam);
            }*/
            LightControlTurnOn();

        }
        /// <summary>
        /// 控制右侧当前所选相机对应的光源。
        /// 普通模式只切换当前CH；同步模式同时切换CH1～CH4。
        /// </summary>
        private void LightControlTurnOn()
        {
            int cameraIndex;
            int lightJobIndex;
            if (!TryGetSelectedCameraBinding(out cameraIndex, out lightJobIndex))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "光源控制";
                operLog.strFailReason = "未选择相机";
                AddLog();
                return;
            }

            string failureReason;
            int controllerSlot = ResolveLightControllerSlot(lightJobIndex, out failureReason);
            if (controllerSlot < 0)
            {
                LogLightControllerResolution(lightJobIndex);
                RefreshSelectedCameraLightUI();
                return;
            }

            SerialPort port = GetLightPortForJob(lightJobIndex);
            if (port == null || !port.IsOpen)
            {
                LogLightControllerResolution(lightJobIndex);
                RefreshSelectedCameraLightUI();
                return;
            }

            int ch1, ch2, ch3, ch4;
            if (!TryGetLightValues(lightJobIndex, out ch1, out ch2, out ch3, out ch4))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "光源控制";
                operLog.strFailReason = "当前相机光源参数无效";
                AddLog();
                return;
            }

            int channel = cmbLs_Channel.SelectedIndex + 1;
            if (channel < 1 || channel > lightChannelCount)
                channel = 1;

            bool sync = chkLS_Sync.Checked;
            bool[] nextStates = new bool[lightChannelCount];
            bool allOn = true;
            for (int channelIndex = 0; channelIndex < lightChannelCount; channelIndex++)
            {
                nextStates[channelIndex] = lightOnStates[controllerSlot, channelIndex];
                allOn &= nextStates[channelIndex];
            }

            bool open = sync ? !allOn : !nextStates[channel - 1];
            if (sync)
            {
                for (int channelIndex = 0; channelIndex < lightChannelCount; channelIndex++)
                    nextStates[channelIndex] = open;
            }
            else
            {
                nextStates[channel - 1] = open;
            }

            int sendCh1 = nextStates[0] ? ch1 : 0;
            int sendCh2 = nextStates[1] ? ch2 : 0;
            int sendCh3 = nextStates[2] ? ch3 : 0;
            int sendCh4 = nextStates[3] ? ch4 : 0;
            string target = sync ? $"SYNC-{(open ? "ON" : "OFF")}" : $"CH{channel}-{(open ? "ON" : "OFF")}";
            if (!SendAllChannelsCommand(lightJobIndex, sendCh1, sendCh2, sendCh3, sendCh4, target))
            {
                RefreshSelectedCameraLightUI();
                return;
            }

            // 串口写入成功后才提交新的通道开关状态。
            for (int channelIndex = 0; channelIndex < lightChannelCount; channelIndex++)
                lightOnStates[controllerSlot, channelIndex] = nextStates[channelIndex];
            SetLightButtonState(lightJobIndex, true);
        }
        /// <summary>
        /// 获取当前工位对应的光源亮度配置值
        /// </summary>
        /// <summary>
        /// 获取当前工位对应的光源亮度配置值
        /// </summary>
        private int GetLightBrightnessForCurrentJob()
        {
            int defaultBrightness = 128;  // 默认亮度

            switch (iJobIndex)
            {
                case 0:  // A面 - 主光源
                         // p_LS 是结构体，不能与 null 比较，直接检查 indent 数组
                    if (csd.p_LS.indent != null && csd.p_LS.indent.Length > 0)
                        return csd.p_LS.indent[0];
                    break;
                case 1:  // B面 - 侧光源1
                         // p_LS2 是数组，可以检查是否为 null，但数组元素是结构体
                    if (csd.p_LS2 != null && csd.p_LS2.Length > 0)
                    {
                        // 直接访问结构体成员，检查 indent 数组
                        if (csd.p_LS2[0].indent != null && csd.p_LS2[0].indent.Length > 0)
                            return csd.p_LS2[0].indent[0];
                    }
                    break;
                case 2:  // 其他 - 侧光源2
                    if (csd.p_LS2 != null && csd.p_LS2.Length > 1)
                    {
                        if (csd.p_LS2[1].indent != null && csd.p_LS2[1].indent.Length > 0)
                            return csd.p_LS2[1].indent[0];
                    }
                    break;
            }

            return defaultBrightness;
        }
        /// <summary>
        /// 根据视觉检测结果自动设置光源亮度（可选扩展）
        /// </summary>
        public bool AutoSetLightBrightnessByVisionResult(int jobIndex)
        {
            // 可以根据当前图像亮度自动调节
            // 或者根据预设的光源参数表调节
            int brightness = GetLightBrightnessForCurrentJob();
            return csd.SetLightBrightnessByJob(jobIndex, brightness);
        }

        private void menuTask_FeedBack_Det_Click(object sender, EventArgs e)
        {
            rts.onLS.iFeedbackType = 1;
            menuTask_FeedBack_Det.Checked = true;
            menuTask_FeedBack_OK.Checked = false;
            menuTask_FeedBack_NG.Checked = false;
            menuTask_FeedBack_NG2.Checked = false;
            menuTask_FeedBack_NO.Checked = false;
            menuTask_FeedBack__Direct.Checked = false;
            cup.p_VPPRTP.iFeedbackType = rts.onLS.iFeedbackType;
        }

        private void menuTask_FeedBack_OK_Click(object sender, EventArgs e)
        {
            rts.onLS.iFeedbackType = 2;
            menuTask_FeedBack_Det.Checked = false;
            menuTask_FeedBack_OK.Checked = true;
            menuTask_FeedBack_NG.Checked = false;
            menuTask_FeedBack_NG2.Checked = false;
            menuTask_FeedBack_NO.Checked = false;
            menuTask_FeedBack__Direct.Checked = false;
            cup.p_VPPRTP.iFeedbackType = rts.onLS.iFeedbackType;
        }

        private void menuTask_FeedBack_NG_Click(object sender, EventArgs e)
        {
            rts.onLS.iFeedbackType = 3;
            menuTask_FeedBack_Det.Checked = false;
            menuTask_FeedBack_OK.Checked = false;
            menuTask_FeedBack_NG.Checked = true;
            menuTask_FeedBack_NG2.Checked = false;
            menuTask_FeedBack_NO.Checked = false;
            menuTask_FeedBack__Direct.Checked = false;
            cup.p_VPPRTP.iFeedbackType = rts.onLS.iFeedbackType;
        }

        private void menuTask_FeedBack_NO_Click(object sender, EventArgs e)
        {
            rts.onLS.iFeedbackType = 0;
            menuTask_FeedBack_Det.Checked = false;
            menuTask_FeedBack_OK.Checked = false;
            menuTask_FeedBack_NG.Checked = false;
            menuTask_FeedBack_NO.Checked = true;
            menuTask_FeedBack__Direct.Checked = false;
            menuTask_FeedBack_NG2.Checked = false;
            cup.p_VPPRTP.iFeedbackType = rts.onLS.iFeedbackType;
        }

        private void menuTask_FeedBack__Direct_Click(object sender, EventArgs e)
        {
            rts.onLS.iFeedbackType = 5;
            menuTask_FeedBack_NG2.Checked = false;
            menuTask_FeedBack_Det.Checked = false;
            menuTask_FeedBack_OK.Checked = false;
            menuTask_FeedBack_NG.Checked = false;
            menuTask_FeedBack_NO.Checked = false;
            menuTask_FeedBack__Direct.Checked = true;
            cup.p_VPPRTP.iFeedbackType = rts.onLS.iFeedbackType;
        }
        private void menuTask_FeedBack_NG2_Click(object sender, EventArgs e)
        {
            rts.onLS.iFeedbackType = 6;
            menuTask_FeedBack_Det.Checked = false;
            menuTask_FeedBack_OK.Checked = false;
            menuTask_FeedBack_NG.Checked = false;
            menuTask_FeedBack_NO.Checked = false;
            menuTask_FeedBack__Direct.Checked = false;
            menuTask_FeedBack_NG2.Checked = true;
            cup.p_VPPRTP.iFeedbackType = rts.onLS.iFeedbackType;
        }
        private void toolBtSta_ErrMark_Click(object sender, EventArgs e)
        {

            operLog.strOperEvent = "误识别记录";
            operLog.iEventStatus = 0;
            if (!bSingleFrameModel)
            {
                try
                {
                    strResInfo.strTime = acqFIFO.strTime[0];
                    strResInfo.strResCode = vpWin._iResCode.ToString();
                    strResInfo.strMark = "1";
                    fo.WriteMark(strLogPath, strResInfo);
                }
                catch
                {
                    operLog.iEventStatus = -1;
                }
                //st 2023年3月30日 acqFIFO.SaveImageForErr(strErrPathS, acqFIFO.strTime);
            }
            else
            {
                try
                {
                    //  int ind = 0;
                    string fileName = Path.GetFileName(strFiles[0][iFileIndex]);
                    string strSourceFileImage = cup.p_Paf.MarkPicPath + "\\" + fileName;
                    string strSourceFileMark = cup.p_Paf.ManuMarkPath + "\\" + fileName.Substring(0, fileName.Length - 3) + "xml";

                    string strDestFileImage = cup.p_Paf.AutoMarkPath + "\\" + fileName;
                    string strDestFileMark = strDestFileImage.Substring(0, strDestFileImage.Length - 3) + "xml";

                    if (File.Exists(strSourceFileImage))
                        File.Move(strSourceFileImage, strDestFileImage);
                    if (File.Exists(strSourceFileMark))
                        File.Move(strSourceFileMark, strDestFileMark);
                }
                catch
                {
                    operLog.iEventStatus = -1;
                }
            }
            AddLog();
        }

        private void toolBtSta_WithOutMark_Click(object sender, EventArgs e)
        {
            operLog.strOperEvent = "漏识别记录";
            operLog.iEventStatus = 0;
            if (!bSingleFrameModel)
            {
                try
                {
                    strResInfo.strTime = acqFIFO.strTime[0];
                    strResInfo.strResCode = vpWin._iResCode.ToString();
                    strResInfo.strMark = "2";
                    fo.WriteMark(strLogPath, strResInfo);
                }
                catch
                {
                    operLog.iEventStatus = -1;
                }
            //   st2023年3月30日   
              //      acqFIFO.SaveImageForErr(strErrPathS, acqFIFO.strTime);
            }
            else
            {
                try
                {
                    //   csd.ExeProcessAll(0);
                    //  int ind = 0;
                    string fileName = Path.GetFileName(strFiles[0][iFileIndex]);
                    string strSourceFileImage = cup.p_Paf.MarkPicPath + "\\" + fileName;
                    string strSourceFileMark = cup.p_Paf.ManuMarkPath + "\\" + fileName.Substring(0, fileName.Length - 3) + "xml";

                    string strDestFileImage = cup.p_Paf.AutoMarkPath + "\\" + fileName;
                    string strDestFileMark = strDestFileImage.Substring(0, strDestFileImage.Length - 3) + "xml";

                    if (File.Exists(strSourceFileImage))
                        File.Move(strSourceFileImage, strDestFileImage);
                    if (File.Exists(strSourceFileMark))
                        File.Move(strSourceFileMark, strDestFileMark);
                }
                catch
                {
                    operLog.iEventStatus = -1;
                }
            }
            AddLog();
        }

       
        private void menuTask_SingleFrame_Click(object sender, EventArgs e)
        {
            if (!bSingleFrameModel)
            {
                bool hasOtherProcessEnabled = false;
                if (cup.p_CD.bListProcess != null)
                {
                    for (int i = 1; i < cup.p_CD.bListProcess.Length; i++)
                    {
                        if (cup.p_CD.bListProcess[i])
                        {
                            hasOtherProcessEnabled = true;
                            break;
                        }
                    }
                }

                if (hasOtherProcessEnabled)
                {
                    MessageBox.Show("请确保只有底面开启识别");
                    return;
                }
            }
            bSingleFrameModel = !bSingleFrameModel;
            menuTask_SingleFrame.Checked = bSingleFrameModel;
            if (bSingleFrameModel)
                InitLocalFile();
        }

        private void 创建空标注ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            // int ind = 0;
            for (int iFileIndex = 0; iFileIndex < strFiles[0].Length; iFileIndex++)
            {
                string fileName = Path.GetFileName(strFiles[0][iFileIndex]);
                string strSourceFileImage = cup.p_Paf.MarkPicPath + "\\" + fileName;

                string strDestFileImage = cup.p_Paf.AutoMarkPath + "\\" + fileName;
                string strDestFileMark = strDestFileImage.Substring(0, strDestFileImage.Length - 3) + "xml";
                //      if (File.Exists(strSourceFileImage))

                modifyNullXML("NullMark_00.xml", fileName, strDestFileMark);
            }

        }
        /// <summary>
        /// 获取指定节点所有内容
        /// </summary>
        /// <param name="path">文件位置</param>
        /// <param name="TwoNode">二级父节点名称</param>
        /// <param name="ThreeNode">三级节点名称</param>
        /// <returns></returns>
        public (bool, XmlNode, string) ReadXMLNode(string Filepath, string Node)
        {
            string portName = string.Empty;
            try
            {
                XmlDocument xml = new XmlDocument();
                //打开一个xml
                xml.Load(Filepath);
                //选择匹配 XPath 表达式的第一个 XmlNode
                XmlNode Configure = xml.SelectSingleNode(Node);
                return (true, Configure, "");
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }

        }


        #region 读取XML配置信息获取指定节点内容
        /// <summary>
        /// 读取XML配置信息获取指定节点内容
        /// </summary>
        /// <param name="path">文件位置</param>
        /// <param name="TwoNode">二级父节点名称</param>
        /// <param name="ThreeNode">三级节点名称</param>
        /// <returns></returns>
        public (bool, string) ReadXMLNodeContent(string Filepath, string Node)
        {
            string portName = string.Empty;
            try
            {
                XmlDocument xml = new XmlDocument();
                //打开一个xml

                xml.Load(Filepath);
                //选择匹配 XPath 表达式的第一个 XmlNode
                XmlNode Configure = xml.SelectSingleNode(Node);
                //读取节点数据
                if (Configure != null)
                {
                    portName = Configure.InnerXml;
                }
                return (true, portName);
            }
            catch (Exception ex)
            {
                portName = ex.Message;
                return (false, portName);
            }

        }

        private bool modifyNullXML(string strName, string strSourcePath, string strDestPath)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                if (File.Exists(strName))
                {//判断文件是否存在   存在则修改
                    xmlDoc.Load(strName);
                }
                //读取二级节点   如果不存在则新增二级节点
                XmlNode nodeName = xmlDoc.SelectSingleNode($"annotation/filename");
                nodeName.InnerText = strSourcePath;
                nodeName = xmlDoc.SelectSingleNode($"annotation/path");
                nodeName.InnerText = strDestPath;
                xmlDoc.Save(strDestPath);
            }
            catch { }

            return true;
        }
        private (bool, string) SaveXmlSetUp(string NodeName, string SetUpJson)
        {
            if (string.IsNullOrWhiteSpace(SetUpJson)) return (false, "值不能为空");

            try
            {
                string FilePath = $@"D:\settings.xml";
                XmlDocument xmlDoc = new XmlDocument();
                if (File.Exists(FilePath))
                {//判断文件是否存在   存在则修改
                    xmlDoc.Load(FilePath);

                    //读取二级节点   如果不存在则新增二级节点
                    XmlNode 二级 = xmlDoc.SelectSingleNode($"Configure/{NodeName}");
                    if (二级 == null)
                    {
                        XmlNode memberlist = xmlDoc.SelectSingleNode($"Configure");
                        XmlElement lq = xmlDoc.CreateElement(NodeName);
                        memberlist.AppendChild(lq);
                    }

                    //读取节点数据
                    if (二级 != null)
                    {//将二级节点的数据删掉  重新把节点数据添加进去
                        //二级.RemoveAll();//也可以直接把二级节点的所有数据全部删掉   这里只删除部分节点
                        XmlNode node = xmlDoc.SelectSingleNode($"Configure/{NodeName}/CheckItemSetUp");
                        if (node != null) 二级.RemoveChild(node);
                    }
                    //创建三级节点  为三级赋值
                    XmlElement el1 = xmlDoc.CreateElement("CheckItemSetUp");
                    el1.InnerText = SetUpJson;

                    //将三级节点添加到二级节点下
                    XmlNode 二级节点 = xmlDoc.SelectSingleNode($"Configure/{NodeName}");
                    二级节点.AppendChild(el1);
                    //保存
                    xmlDoc.Save(FilePath);
                }
                else
                {//如果xml文件不存在则创建xml文件
                    //加入XML的声明段落，Save方法不再xml上写出独立属性
                    xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null));
                    //加入根元素 
                    XmlElement root = xmlDoc.CreateElement("Configure");
                    xmlDoc.AppendChild(root);

                    //创建二级节点
                    XmlElement memberlist = xmlDoc.CreateElement($"{NodeName}");

                    //创建三级节点
                    XmlElement el1 = xmlDoc.CreateElement("SqlServerInstanceName");
                    el1.InnerText = SetUpJson;

                    //将三级节点添加到二级节点下
                    memberlist.AppendChild(el1);

                    root.AppendChild(memberlist);
                    xmlDoc.Save(FilePath);
                }

                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private void toolBtSta_Remove_Click(object sender, EventArgs e)
        {
            /*if (MessageBox.Show("确定要删除历史记录吗", "提醒", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                bool bDelSucc = acqFIFO.DelOneGroupImageFile();
                operLog.strOperEvent = "删除一组数据";
                if (bDelSucc)
                    operLog.iEventStatus = 0;
                else
                    operLog.iEventStatus = -1;
                AddLog();
            }*/


        }
        private void toolBtSta_MarkAfterLocated_Click(object sender, EventArgs e)
        {
            toolBtSta_MarkAfterLocated.Checked = !toolBtSta_MarkAfterLocated.Checked;
            rts.onLS.bLock = toolBtSta_MarkAfterLocated.Checked;
            if (toolBtSta_MarkAfterLocated.Checked)
            {
                toolBtSta_MarkAfterLocated.Image = global::VisionApplication.Properties.Resources.WithMark;
                toolBtSta_MarkAfterLocated.ToolTipText = "定位后标刻";
            }
            else
            {
                toolBtSta_MarkAfterLocated.Image = global::VisionApplication.Properties.Resources.WithOutMark;
                toolBtSta_MarkAfterLocated.ToolTipText = "定位后不标刻";
            }
        }

        private List<string> GetCurFile()
        {
            List<string> strCurFile = new List<string>();

            int ind = iFileIndex * 6;


            for (int i = 0; i < maxCameraNum; i++)
            {
                if (cup.p_CD.bListProcess[i])
                {
                    for (int j = 0; j < 6; j++)
                        strCurFile.Add(strFiles[i][ind + j]);

                }
            }
            return strCurFile;
        }

        private void menuTool_Remark_Click(object sender, EventArgs e)
        {

        }

       

       
        private void timer1_Tick(object sender, EventArgs e)
        {          
            if (iProcess < 100)
                return;

            timer1.Stop();
            if (bfirstTime)
            {
                //第三步  查询文件对象
                /*  lczParam.cmdType = 8;
                    lczParam.strFileName = Path.Combine(cup.p_Paf.ManuMarkPath, "route.ezd.");  //"route.ezd"
                    lczParam.ind3 = 0;
                    bCmdEnd = false;
                    if (pipeClient != null)
                        pipeClient.SendParam(lczParam);*/
                /*queryEntThread = new Thread(QueryEntInfo);
                queryEntThread.Start();*/

                /* JCZServerLayout();
                 bfirstTime = false;*/

                vpWin.iModelRole = rts.iModelRole;
                timer1.Interval = 10000;
                bfirstTime = false;
            }
            try
            {
                if (cup.p_RTSP.bAutoConnCam)
                {
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        //判断相机是否在线
                        int iCamIndex = cup.p_CD.camInd[i];
                        if (iCamIndex >= 0 && iCamIndex < maxCameraNum && !acqFIFO.IsCamConn(iCamIndex))
                            SyncCameraStatus(iCamIndex, 0);
                    }
                }
                //2026年7月2日 暂时不要更新信息
                TipInfo();
                if (DateTime.Now.Hour == 0 && DateTime.Now.Minute < 5) //0点到0:05分，
                {
                    dirJudge(true);
                }

               /* if (cup.p_RTSP.bAutoRunVPP)
                {
                    if (rts.initStatus.bVPPInit && vpWin._iTaskType != 3)
                        vpWin.ActivateRun();
                }*/
                vpWin.iModelRole = rts.iModelRole;
            }
            catch
            { };
            // 刷新激光UI状态
            RefreshLaserUIState();
            timer1.Start();
        }
        public void equidistantBlow()
        {
            //吹气判断

            if (cup.p_BP.bUsed && cup.p_BP.blowType == 0)  //等间隔吹气
            {
                if (cup.p_BP.intervType == 0) //每天
                {
                    int mod = 24 / cup.p_BP.intervTimes;

                    if (DateTime.Now.Hour % mod == 0 && DateTime.Now.Minute == 0 && (DateTime.Now.Second < 10 || DateTime.Now.Second > 50))
                    {
                        csd.Cylinderblow(sPortCy, cup.p_BP.perBlowTime);
                    }
                }
                else if (cup.p_BP.intervType == 1) //每小时
                {
                    int mod = 60 / cup.p_BP.intervTimes;

                    if (DateTime.Now.Minute % mod == 0 && (DateTime.Now.Second < 10 || DateTime.Now.Second > 50))
                    {
                        csd.Cylinderblow(sPortCy, cup.p_BP.perBlowTime);
                    }
                }
                else if (cup.p_BP.intervType == 2) //每小时
                {
                    int mod = 60 / cup.p_BP.intervTimes;

                    if ((DateTime.Now.Second < 10 || DateTime.Now.Second > 50))
                    {
                        csd.Cylinderblow(sPortCy, cup.p_BP.perBlowTime);
                    }
                }
            }

        }

        private void MonitorJCZProcess()
        {
            Process proc = GetJCZServerProcess();
            if (proc != null)
            {
                proc.EnableRaisingEvents = true;
                proc.Exited += (s, e) =>
                {
                    // 进程退出时，立即更新 UI 状态
                    this.BeginInvoke(new Action(() =>
                    {
                        rts.initStatus.bJCZ = -2;
                        // 更新 btSpace 图标为红色
                        btSpace.BackgroundImage = global::VisionApplication.Properties.Resources.RedLight;
                    }));
                };
            }
        }

        //界面提示信息
        private void TipInfo()
        {
            //界面加载完成之前不更新信息
            if (!bLoadFinish)
                return;

            string path = cup.p_Paf.strConSavePath;

            //板卡判断
            if (rts.initStatus.bJCZ == 0)
            {
                btSpace.BackgroundImage = global::VisionApplication.Properties.Resources.GreenLight1;
                toolTip1.SetToolTip(btSpace, "板卡初始化成功");
            }
            else if (rts.initStatus.bJCZ < -1)
            {
                btSpace.BackgroundImage = global::VisionApplication.Properties.Resources.RedLight;
                toolTip1.SetToolTip(btSpace, "连接板卡或管道失败");
            }
            else
            {
                btSpace.BackgroundImage = global::VisionApplication.Properties.Resources.YellowLight;
                toolTip1.SetToolTip(btSpace, "加载激光打标文件失败");
            }

           /* DriveInfo di = new DriveInfo(path.Substring(0, 1)); //"path.Substring(0,1)
            if (di.IsReady)
            {
                long totalSize = di.TotalSize / 1024 / 1024 / 1024;
                long FreeSize = di.TotalFreeSpace / 1024 / 1024 / 1024;
                toolTip1.SetToolTip(btSpace, "剩余磁盘空间：" + FreeSize + "G");
                if (FreeSize > 40)
                {
                    rts.driveS = 0;
                    btSpace.BackgroundImage = global::VisionApplication.Properties.Resources.GreenLight1;
                }
                else if (FreeSize > 10)
                {
                    rts.driveS = 1;
                    btSpace.BackgroundImage = global::VisionApplication.Properties.Resources.YellowLight;
                }
                else
                {
                    rts.driveS = 2;
                    btSpace.BackgroundImage = global::VisionApplication.Properties.Resources.RedLight;
                }
            }
            else
            {
                rts.driveS = -1;
                btSpace.BackgroundImage = global::VisionApplication.Properties.Resources.EventUn;
                toolTip1.SetToolTip(btSpace, "存储磁盘不存在或者磁盘异常");
            }*/

            int iCamSuccNum = 0;
            int iCamFailNum = 0;
            for (int jobIndex = 0; jobIndex < maxJobNum; jobIndex++)
            {
                if (!cup.p_CD.bListProcess[jobIndex])
                    continue;

                int cameraIndex = cup.p_CD.camInd[jobIndex];
                bool bCamReady = cameraIndex >= 0 && cameraIndex < maxCameraNum &&
                    acqFIFO.IsCamConn(cameraIndex) && rts.cs.iStatus[cameraIndex] == 3;
                if (bCamReady)
                    iCamSuccNum++;
                else
                    iCamFailNum++;
            }

            // 自动重连（主动断开后等待手动连接）
            if (cup.p_RTSP.bAutoConnCam && !_cameraManuallyDisconnected)
            {
                for (int i = 0; i < maxJobNum; i++)
                {
                    int iCamInd = cup.p_CD.camInd[i];
                    bool cameraNeedsReconnect = iCamInd < 0 || iCamInd >= rts.cs.iStatus.Length ||
                        rts.cs.iStatus[iCamInd] < 2;
                    if (cup.p_VPPRTP.bAcqFromCam && cup.p_CD.bListProcess[i] && cameraNeedsReconnect)
                    {
                        if (!_isReconnecting && (DateTime.Now - _lastReconnectAttempt) > _reconnectInterval)
                        {
                            _lastReconnectAttempt = DateTime.Now;
                            _isReconnecting = true;
                            ThreadPool.QueueUserWorkItem(state =>
                            {
                                try
                                {
                                    // 自动重连也走与手动按钮相同的“重新枚举+按序列号补齐”路径，
                                    // 但不会关闭仍然在线的相机。
                                    TryConnectCameras("自动重连");
                                }
                                catch (Exception ex)
                                {
                                    operLog.iEventStatus = -1;
                                    operLog.strOperEvent = "相机后台重连";
                                    operLog.strFailReason = ex.ToString();
                                    AddLog();
                                }
                                finally
                                {
                                    _isReconnecting = false;
                                }
                            }, i);
                        }
                    }
                }
            }
            if (iCamSuccNum == 0)
            {
                btCam.BackgroundImage = global::VisionApplication.Properties.Resources.RedLight;
                toolTip1.SetToolTip(btCam, "所有相机连接失败");
                toolStripStatus_CamConnectStatus.Text = "相机未连接";
                toolStripStatus_CamConnectStatus.ForeColor = Color.Red;

            }
            else if (iCamFailNum > 0)
            {
                btCam.BackgroundImage = global::VisionApplication.Properties.Resources.YellowLight;
                toolTip1.SetToolTip(btCam, iCamFailNum + "个相机连接失败");
                toolStripStatus_CamConnectStatus.Text = "部分相机连接成功";
                toolStripStatus_CamConnectStatus.ForeColor = Color.Orange;
            }
            else
            {
                btCam.BackgroundImage = global::VisionApplication.Properties.Resources.GreenLight1;
                toolTip1.SetToolTip(btCam, "所有相机连接成功");

                toolStripStatus_CamConnectStatus.Text = "相机连接成功";
                toolStripStatus_CamConnectStatus.ForeColor = Color.Green;
            }
            CheckCamPrepared();

            toolStripStatus_ExcuteTimes.Text = "执行次数：" + strStatus[0] + "/" +  rts.detS.iTotal;

            int iSerFailNum = 0;
            int iSerSuccNum = 0;
            //  int ind = 0;
            for (int i = 0; i < 3; i++)
            {
                if (cup.p_SD.bUsed[i] && (sPortAll[i] == null || !sPortAll[i].IsOpen))
                {

                    //尝试连接
                    bool bSucc = InitSerialIndex(i);

                    if (!bSucc)
                    {
                        iSerFailNum++;
                        switch (i)
                        {
                            case 0:
                                toolStripStatus_Ser1.ForeColor = Color.Red;
                                break;
                            case 1:
                                toolStripStatus_Ser2.ForeColor = Color.Red;
                                break;
                            case 2:
                                toolStripStatus_Ser3.ForeColor = Color.Red;
                                break;
                        }
                    }

                }
                if (cup.p_SD.bUsed[i] && (sPortAll[i] != null && sPortAll[i].IsOpen))
                {
                    iSerSuccNum++;
                    switch (i)
                    {
                        case 0:
                            toolStripStatus_Ser1.ForeColor = Color.Green;
                            break;
                        case 1:
                            toolStripStatus_Ser2.ForeColor = Color.Green;
                            break;
                        case 2:
                            toolStripStatus_Ser3.ForeColor = Color.Green;
                            break;
                    }
                }
                else if (!cup.p_SD.bUsed[i] && (sPortAll[i] == null || !sPortAll[i].IsOpen))
                {
                    switch (i)
                    {
                        case 0:
                            toolStripStatus_Ser1.ForeColor = Color.Gray;
                            break;
                        case 1:
                            toolStripStatus_Ser2.ForeColor = Color.Gray;
                            break;
                        case 2:
                            toolStripStatus_Ser3.ForeColor = Color.Gray;
                            break;
                    }
                }
            }
            if (iSerFailNum == 0)
            {
                btSer.BackgroundImage = global::VisionApplication.Properties.Resources.GreenLight1;
                toolTip1.SetToolTip(btSer, "所有串口连接正常");

            }
            else if (iSerFailNum > 0 && iSerFailNum < 3)
            {
                btSer.BackgroundImage = global::VisionApplication.Properties.Resources.YellowLight;
                toolTip1.SetToolTip(btSer, "串口连接正常");
                toolStripStatus_Ser.Text = "串口连接正常";
                toolStripStatus_Ser.ForeColor = Color.Yellow;

            }
            else if (iSerFailNum == 3)
            {
                btSer.BackgroundImage = global::VisionApplication.Properties.Resources.RedLight;
                toolTip1.SetToolTip(btSer, iSerFailNum + "个串口连接失败");
                toolStripStatus_Ser.Text = "部分串口连接正常";
                toolStripStatus_Ser.ForeColor = Color.Red;

            }
            else
            {
                btSer.BackgroundImage = global::VisionApplication.Properties.Resources.EventUn;
                toolTip1.SetToolTip(btSer, "串口连接状态未知");
            }

            if (cup.p_SD.bUsed[3] && (sPortAll[3] == null || !sPortAll[3].IsOpen))
            {
                //尝试连接
                bool bSucc = InitSerialIndex(3);
                if (!bSucc)
                {
                    toolStripStatus_Ser4.ForeColor = Color.Red;

                    btMachine.BackgroundImage = global::VisionApplication.Properties.Resources.RedLight;
                    toolTip1.SetToolTip(btMachine, "线体连接失败");

                    toolStripStatus_Ser.Text = "串口连接失败";
                    toolStripStatus_Ser.ForeColor = Color.Red;
                }

            }
            else if (!cup.p_SD.bUsed[3] && (sPortAll[3] == null || !sPortAll[3].IsOpen))
            {
                toolStripStatus_Ser4.ForeColor = Color.Gray;

                btMachine.BackgroundImage = global::VisionApplication.Properties.Resources.YellowLight;
                toolTip1.SetToolTip(btMachine, "线体连接未启用");
                if (iSerSuccNum == 3 || iSerFailNum == 0)
                {
                    toolStripStatus_Ser.Text = "串口连接正常";
                    toolStripStatus_Ser.ForeColor = Color.Green;
                }
                else if (iSerFailNum > 0)
                {
                    toolStripStatus_Ser.Text = "串口连接失败";
                    toolStripStatus_Ser.ForeColor = Color.Red;
                }
            }
            else if (cup.p_SD.bUsed[3] && sPortAll[3] != null && sPortAll[3].IsOpen)
            {
                toolStripStatus_Ser4.ForeColor = Color.Green;

                btMachine.BackgroundImage = global::VisionApplication.Properties.Resources.GreenLight1;
                toolTip1.SetToolTip(btMachine, "线体连接成功");
                if (iSerSuccNum == 3 || iSerFailNum == 0)
                {
                    toolStripStatus_Ser.Text = "串口连接正常";
                    toolStripStatus_Ser.ForeColor = Color.Green;
                }
                else if (iSerFailNum > 0)
                {
                    toolStripStatus_Ser.Text = "串口连接失败";
                    toolStripStatus_Ser.ForeColor = Color.Red;
                }
            }
            else if (sPortAll[3] != null && sPortAll[3].IsOpen)
            {
                toolStripStatus_Ser4.ForeColor = Color.Green;
                if (iSerSuccNum == 3 || iSerFailNum == 0)
                {
                    toolStripStatus_Ser.Text = "串口连接正常";
                    toolStripStatus_Ser.ForeColor = Color.Green;
                }
                else if (iSerFailNum > 0)
                {
                    toolStripStatus_Ser.Text = "串口连接失败";
                    toolStripStatus_Ser.ForeColor = Color.Red;
                }
            }

            for (int i = 0; i < 4; i++)
            {

                if (sPortAll[i] == null)
                {
                    rts.iSerStatus[i] = -1;
                }
                else if (!sPortAll[i].IsOpen)
                {
                    rts.iSerStatus[i] = 1;
                }
                else if (sPortAll[i].IsOpen)
                {
                    rts.iSerStatus[i] = 0;
                }
            }

            //身份信息
            if (rts.iModelRole == -1)
            {
                btID.BackgroundImage = global::VisionApplication.Properties.Resources.RedLight;
                toolTip1.SetToolTip(btID, "用户身份未验证");
            }
            else if (rts.iModelRole == -2)
            {
                btID.BackgroundImage = global::VisionApplication.Properties.Resources.YellowLight;
                toolTip1.SetToolTip(btID, "用户权限已过期");
            }
            else if (rts.iModelRole >= 0)
            {
                btID.BackgroundImage = global::VisionApplication.Properties.Resources.GreenLight1;
                toolTip1.SetToolTip(btID, "用户已验证");
            }
            else
            {
                btID.BackgroundImage = global::VisionApplication.Properties.Resources.EventUn;
                toolTip1.SetToolTip(btID, "用户身份未知");
            }

         //   string strPath = cup.p_Paf.strLogPath + "\\RTS_0000.txt";
         //   fo.SaveRunTimeStatus(strPath, rts);
        }

        private void menuTask_Click(object sender, EventArgs e)
        {

        }

        private void menuTask_Paint_Click(object sender, EventArgs e)
        {
       //     btFun_Click(btRedLight_Preview, null);
            /*
                        if (iModelRole < 0)
                        {

                            MessageBox.Show("用户身份未验证，无法操作");
                            return;
                        }*/
        }

        private void menu_View_Fit_Click(object sender, EventArgs e)
        {
            vpWin.FitAllWin();
        }

        private void menuTool_ImageMarkCmp_Click(object sender, EventArgs e)
        {

        }

        private void toolBtSta_Export_Click(object sender, EventArgs e)
        {
            string[,] strCurFile = new string[maxCameraNum, 6];

            int ind = iFileIndex * 6;


            for (int i = 0; i < maxCameraNum; i++)
            {
                if (cup.p_CD.bListProcess[i])
                {
                    for (int j = 0; j < 6; j++)
                        strCurFile[i, j] = strFiles[i][ind + j];
                }
            }
            operLog.strOperEvent = "导出文件";

            if (acqFIFO.ExportCurImage(cup.p_Paf.strExportPath, strCurFile))
            {

                operLog.iEventStatus = 0;
            }
            else
            {
                operLog.iEventStatus = -1;
            }
            AddLog();
        }
         
        private void menuTask_CrackPlayback_Click(object sender, EventArgs e)
        {


            if (!bCrackPlayback)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "(*.txt)|*.*";
                openFileDialog.FileName = strPlaybackFileList;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    strPlaybackFileList = openFileDialog.FileName;
                    bCrackPlayback = !bCrackPlayback;
                    menuTask_CrackPlayback.Checked = bCrackPlayback;
                }
            }
            else
            {
                bCrackPlayback = !bCrackPlayback;
                menuTask_CrackPlayback.Checked = bCrackPlayback;
            }


        }
        #endregion

        /// ///////////// ///////////// ///////////// ///////////// ///////////// //////////

        private void menu_View_ResultStatics_Click(object sender, EventArgs e)
        {
            menu_View_ResultStatics.Checked = !menu_View_ResultStatics.Checked;
            splitContainer2.Panel2Collapsed = !menu_View_ResultStatics.Checked;
        }

        private void menuTask_Refresh_Click(object sender, EventArgs e)
        {
            //vpWin.Refresh();
           // vpWin.ReLoadVPP(cup.p_Paf.strVPPFilePath);
        }

        private void menuManage_authorize_Click(object sender, EventArgs e)
        {
            FrmExpiration fe = new FrmExpiration(mPasswordFile, rts.iModelRole);
            if(fe.ShowDialog()==DialogResult.OK)
            {
                RoleCheck();
            }
            operLog.iEventStatus = rts.iModelRole;
            operLog.strOperEvent = "身份验证";
            operLog.strFailReason = rts.iModelRole.ToString();
            AddLog();
        
            TipInfo();
        }

        private void menuTask_PreView_Click(object sender, EventArgs e)
        {
            toolBt_Preview_Click(null, null);
            /*if (iModelRole < 0)
            {

                MessageBox.Show("用户身份未验证，无法操作");
                return;
            }*/
        }

        private void menuCam_Open_Click(object sender, EventArgs e)
        {

        }

        private void menuParam_SerialDebug_Click(object sender, EventArgs e)
        {
            if (rts.iModelRole < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "串口调试";
                operLog.strFailReason = "用户身份未验证";
                AddLog();
                return;
            }

            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "串口调试";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                return;
            }

            frm_Serial fs = new frm_Serial();
            fs.ShowDialog();
        }
        //是否启用光度图重构
        private void menuTask_FXRestruct_Click(object sender, EventArgs e)
        {
            menuTask_FXRestruct.Checked = !menuTask_FXRestruct.Checked;
            cup.p_VPPRTP.bFXRestruct = menuTask_FXRestruct.Checked;
            InitCheckStatus();
        }
        //是否启用VP视觉推理
        private void menuTask_VisionInfer_Click(object sender, EventArgs e)
        {
            /*if(!menuTask_VisionInfer.Checked)
            { 
            }*/
            menuTask_VisionInfer.Checked = !menuTask_VisionInfer.Checked;
            cup.p_VPPRTP.bVisionInfer = menuTask_VisionInfer.Checked;
            if (cup.p_VPPRTP.bVisionInfer)  //如果启用了VP视觉推理，则不加载历史结果
            {
                bLoadVPRes = false;
                menuTask_LoadRes.Checked = bLoadVPRes;
                vpWin.bLoadVPRes = bLoadVPRes;
            }
            InitCheckStatus();


            if (cup.p_VPPRTP.bVisionInfer)
            {
                vpWin.SetVPInferModel(1 + (cup.p_VPPRTP.bDeepInfer ? 1 : 0) * 10);
            }
            else
            {
                vpWin.SetVPInferModel(0 + (bLoadVPRes ? 1 : 0) * 10);
            }
        }
       
        //直接加载结果
       
        private void menuTask_LoadRes_Click(object sender, EventArgs e)
        {
            bLoadVPRes = !bLoadVPRes;
            menuTask_LoadRes.Checked = bLoadVPRes;
            vpWin.bLoadVPRes = bLoadVPRes;
            if (vpWin.bLoadVPRes)  //如果加载结果，则关闭深度推理和VP视觉推理
            {
                cup.p_VPPRTP.bVisionInfer = false;
                menuTask_VisionInfer.Checked = false;
                cup.p_VPPRTP.bDeepInfer = false;
                menuTask_VisionInfer.Checked = false;
            }
            if (cup.p_VPPRTP.bVisionInfer)
            {
                vpWin.SetVPInferModel(1 + (cup.p_VPPRTP.bDeepInfer ? 1 : 0) * 10);
            }
            else
            {
                vpWin.SetVPInferModel(0 + (bLoadVPRes ? 1 : 0) * 10);
            }

        }
        

        private void menuTool_LightDebug_Click(object sender, EventArgs e)
        {
            if (rts.iModelRole < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "打光流程设置";
                operLog.strFailReason = "用户身份未验证";
                AddLog();

                //    MessageBox.Show("用户身份未验证，无法操作");
                return;
            }
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "打光流程设置";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                //   MessageBox.Show("当前任务没有结束，不能执行该操作");
                return;
            }

            frm_Serial fs = new frm_Serial(csd);

            if (fs.ShowDialog() == DialogResult.OK)
            {
                csd.p_LS = fs.p_LS;
                csd.p_LS2 = fs.p_LS2;
                cup.p_LS = fs.p_LS;
                cup.p_LS2 = fs.p_LS2;
                SaveUserParam();
            }

        }

        private void menuHelp_Click(object sender, EventArgs e)
        {

        }
        bool DebugRole = false;
        private void menuManage_SuperOrNormal_Click(object sender, EventArgs e)
        {
            if (rts.iModelRole >= 9)
            {
                DebugRole = true;
                rts.iModelRole = 0;
                vpWin.iModelRole = 0;
                menuManage_SuperOrNormal.Text = "切换至超级用户";
            }
            else if(DebugRole)
            {
                rts.iModelRole = 9;
                vpWin.iModelRole = 9;
                menuManage_SuperOrNormal.Text = "切换至普通用户";
            }
            vpWin.SiftRDMain();
            if (rts.iModelRole >=4)
            {
                /* txt_NextImageIndex.Visible = true;
                 label5.Visible = true;*/
             //   btFunSet.Visible = true;
                groupBox1.Visible = true;
            }
            else
            {
                /*txt_NextImageIndex.Visible = false;
                label5.Visible = false;*/
                btFunSet.Visible = false;
                groupBox1.Visible = false;
            }
        }

        private void menuFile_SaveDirectory_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", cup.p_Paf.strConSavePath);
        }

        private void menuFile_PlaybackDirectory_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", cup.p_Paf.strPlaybackPath);
        }

        private void menuFile_UpdatePlaybackDirectory_Click(object sender, EventArgs e)
        {
            InitLocalFile();
        }

        private void menuFile_LogDirectory_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", cup.p_Paf.ManuMarkPath);
        }

        private void menuFile_ResultDirectory_Click(object sender, EventArgs e)
        {
            //Process.Start("explorer.exe", cup.p_Paf.strResSavePath);
            Process.Start("explorer.exe", Path.GetDirectoryName(cup.p_DL.modelPath));
        }

        private void splitContainer6_Panel2_Resize(object sender, EventArgs e)
        {
            /* if (embeddedHwnd != IntPtr.Zero)
             {
                 MoveWindow(
                     embeddedHwnd,
                     0, 0,
                     splitContainer6.Panel2.ClientSize.Width,
                     splitContainer6.Panel2.ClientSize.Height,
                     true
                 );
             }*/
        }

        #endregion  交互操作事件区


        #region   
        
        private void UpdateStatisticsForJob(int i)
        {
            // don't update statistics when showing live video
            /**/
            strStatus = vpWin.RetStatisticsForJob(i);
            toolStripStatus_ExcuteTimes.Text = "执行次数：" + strStatus[0]+"/"+rts.detS.iTotal;
            
            /*textBox_JobN_TotalIterations.Text = strStatus[0];
            textBox_JobN_TotalAccept.Text = strStatus[1];
            textBox_JobN_TotalReject.Text = (Int32.Parse(strStatus[6]) + iAcquisitionsTimes).ToString();
*/
            /* textBox_JobN_TotalAccept_Percent.Text = strStatus[2];
             textBox_JobN_TotalReject.Text = strStatus[3];
             textBox_JobN_TotalWarning.Text = strStatus[4];
             textBox_JobN_TotalError.Text = (Int32.Parse(strStatus[5]) + iErrExecuteTimes).ToString();*/ // strStatus[5];

            /*   textBox_JobN_TotalAcquisitions.Text = (Int32.Parse(strStatus[6])+ iAcquisitionsTimes).ToString();
               textBox_JobN_TotalAcquisitionErrors.Text = (Int32.Parse(strStatus[7]) + iErrAcquisitionsTimes).ToString();  
               textBox_JobN_TotalAcquisitionOverruns.Text = strStatus[8];*/

            /* textBox_JobN_Throughput.Text = strStatus[9];
             textBox_JobN_MinThroughput.Text = strStatus[10];
             textBox_JobN_MaxThroughput.Text = strStatus[11];*/

          //  toolStripStatus_inferTime.Text = "推理时间：" + vpWin.cost_milliseconds;

            rts.detS.iTotal  = detStat.iTotal+ Int32.Parse(strStatus[0]);

            if (!cup.p_VPPRTP.bVisionInfer)
                return;
            rts.detS.iOKNum  = detStat.iTotal + Int32.Parse(strStatus[1]);
            rts.detS.iOKPer  = float.Parse(strStatus[2]);
            rts.detS.iNGNum  = detStat.iTotal + Int32.Parse(strStatus[3]);
            rts.detS.iWarningNum  = detStat.iTotal +  Int32.Parse(strStatus[4]);
            //   rts.detS.iErrNum = Int32.Parse(textBox_JobN_TotalError.Text);

            /* */

        }
        //更新统计数据
        private void UpdateStatic()
        {
            rts.detS.iTotal += Int32.Parse(strStatus[0]);
            rts.detS.iOKNum += Int32.Parse(strStatus[1]);
            rts.detS.iOKPer = float.Parse(strStatus[2]);
            rts.detS.iNGNum += Int32.Parse(strStatus[3]);
            rts.detS.iWarningNum = +Int32.Parse(strStatus[4]);
         //   rts.detS.iErrNum = +Int32.Parse(textBox_JobN_TotalError.Text);

            rts.acqS.iTotal += Int32.Parse(strStatus[6]) + iAcquisitionsTimes;
            rts.acqS.iLossNum += Int32.Parse(strStatus[7]) + iErrAcquisitionsTimes;
            rts.acqS.iTotal += Int32.Parse(strStatus[8]);

        }
        private void ViewChange()
        {
            // 各子区域根据菜单项 Checked 状态控制折叠/展开
            splitContainer4.Panel1Collapsed = !menu_View_EventStatus.Checked;
            splitContainer6.Panel1Collapsed = !menu_View_SystemLog.Checked;
            splitContainer6.Panel2Collapsed = !menu_View_LaserWin.Checked;

            // 如果系统日志和激光窗口都未选中，折叠 splitContainer4.Panel2
            splitContainer4.Panel2Collapsed = !menu_View_SystemLog.Checked && !menu_View_LaserWin.Checked;
            // 如果三个菜单项都未选中，折叠整个 splitContainer3.Panel2；否则展开
            bool anyChecked = menu_View_EventStatus.Checked || menu_View_SystemLog.Checked || menu_View_LaserWin.Checked;
            splitContainer3.Panel2Collapsed = !anyChecked;
        }

        //20220410串口操作
        //初始化串口

        private bool InitSerialIndex(int i)
        {
            if (i < 0 || i >= sPortAll.Length || cup.p_SD.serInd == null || i >= cup.p_SD.serInd.Length)
                return false;
            if (i == 3 && !cup.p_SD.bUsed[3])
                return false;

            List<string> availablePorts = GetSortedPorts();
            int configIndex = cup.p_SD.serInd[i];
            string portName;
            if (!TryGetPortNameByConfigIndex(configIndex, availablePorts, out portName))
            {
                LogInvalidSerialConfigurationOnce(i, configIndex, availablePorts.Count);
                return false;
            }

            serialConfigErrorLogged[i] = false;
            if (i == 3 && sPortAll[i] == null)
                sPortAll[i] = new SerialPort();
            bool opened = OpenSerialPort(sPortAll[i], portName);
            if (i == 3)
                RegisterIOReceive();
            return opened;
        }

        private void RegisterIOReceive()
        {
            SerialPort ioPort = sPortAll[3];
            if (sPortCy != null)
                sPortCy.DataReceived -= PortGetCylinder;
            sPortCy = ioPort;
            csd._sPortCy = ioPort;
            if (ioPort == null)
                return;
            ioPort.DataReceived -= PortGetCylinder;
            if (cup.p_SD.bUsed[3] && ioPort.IsOpen)
                ioPort.DataReceived += PortGetCylinder;
        }

        private void LogInvalidSerialConfigurationOnce(int serialSlot, int configIndex, int availableCount)
        {
            if (serialSlot < 0 || serialSlot >= serialConfigErrorLogged.Length ||
                serialConfigErrorLogged[serialSlot])
                return;

            serialConfigErrorLogged[serialSlot] = true;
            operLog.iEventStatus = -1;
            operLog.strOperEvent = $"串口{serialSlot + 1}配置";
            operLog.strFailReason = $"配置索引{configIndex}超出已排序可用串口范围(共{availableCount}个)，未打开其他串口";
            AddLog();
        }

        /// <summary>
        /// 获取按COM号排序的串口列表
        /// 去除原有的错误过滤逻辑，只做排序
        /// </summary>
        private List<string> GetSortedPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            List<string> sortedPorts = new List<string>();

            try
            {
                // 提取COM号并排序
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
                // 如果解析失败，直接按字符串排序
                sortedPorts = new List<string>(ports);
                sortedPorts.Sort();
            }

            return sortedPorts;
        }

        /// <summary>
        /// 根据配置的索引获取串口名称
        /// </summary>
        /// <param name="configIndex">配置索引(0-3)</param>
        /// <returns>串口名称，如果未找到返回空字符串</returns>
        private bool TryGetPortNameByConfigIndex(int configIndex, IList<string> sortedPorts, out string portName)
        {
            portName = "";
            if (sortedPorts == null)
                return false;

            if (configIndex >= 0 && configIndex < sortedPorts.Count)
            {
                portName = sortedPorts[configIndex];
                return !string.IsNullOrWhiteSpace(portName);
            }

            return false;
        }

        /// <summary>
        /// 初始化串口（替换原有的InitSerial方法中的PortProc调用）
        /// </summary>
        private void InitSerial()
        {
            // 初始化串口数组
            for (int i = 0; i < 4; i++)
            {
                sPortAll[i] = new SerialPort();
            }
            RegisterIOReceive();

            // 获取可用串口列表
            List<string> availablePorts = GetSortedPorts();

            operLog.strOperEvent = "串口初始化";

            if (availablePorts.Count == 0)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "未搜索到可用串口";
                AddLog();
                return;
            }

            // 更新状态栏显示可用串口信息
            toolStripStatus_Ser.Text = $"可用串口: {string.Join(", ", availablePorts)}";

            int openedCount = 0;

            for (int i = 0; i < 4; i++)
            {
                if (!cup.p_SD.bUsed[i])
                {
                    // 未使用的串口，状态显示为灰色
                    UpdateSerialStatusIndicator(i, SerialStatus.Disabled);
                    continue;
                }

                // 获取配置的串口索引
                int configIndex = cup.p_SD.serInd[i];

                // 获取对应的串口名称
                string portName;

                if (!TryGetPortNameByConfigIndex(configIndex, availablePorts, out portName))
                {
                    LogInvalidSerialConfigurationOnce(i, configIndex, availablePorts.Count);
                    UpdateSerialStatusIndicator(i, SerialStatus.Failed);
                    continue;
                }

                serialConfigErrorLogged[i] = false;

                // 打开串口
                if (OpenSerialPort(sPortAll[i], portName))
                {
                    openedCount++;
                    UpdateSerialStatusIndicator(i, SerialStatus.Success);

                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = $"串口{i + 1}({portName})打开成功";
                    AddLog();
                }
                else
                {
                    UpdateSerialStatusIndicator(i, SerialStatus.Failed);
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = $"串口{i + 1}({portName})打开失败";
                    operLog.strFailReason = "请检查设备连接";
                    AddLog();
                }
            }

            // 设置特殊串口引用
            if (sPortAll[0] != null) sPortMain = sPortAll[0];
            if (sPortAll[1] != null) sPortSub[0] = sPortAll[1];
            if (sPortAll[2] != null) sPortSub[1] = sPortAll[2];

            csd._sPort = sPortMain;
            csd._sPortSub = sPortSub;

            // 注册数据接收事件
            RegisterIOReceive();

            rts.initStatus.bSerInit = (openedCount > 0);

            // 更新UI显示
            UpdateSerialUIStatus();
            InitializeLightBrightness();
        }

        /// <summary>
        /// 串口状态枚举
        /// </summary>
        private enum SerialStatus
        {
            Disabled,   // 未使用
            Success,    // 成功
            Failed      // 失败
        }

        /// <summary>
        /// 更新串口状态指示器
        /// </summary>
        private void UpdateSerialStatusIndicator(int index, SerialStatus status)
        {
            Color color;
            switch (status)
            {
                case SerialStatus.Success:
                    color = Color.Green;
                    break;
                case SerialStatus.Failed:
                    color = Color.Red;
                    break;
                default:
                    color = Color.Gray;
                    break;
            }

            switch (index)
            {
                case 0:
                    toolStripStatus_Ser1.ForeColor = color;
                    break;
                case 1:
                    toolStripStatus_Ser2.ForeColor = color;
                    break;
                case 2:
                    toolStripStatus_Ser3.ForeColor = color;
                    break;
                case 3:
                    toolStripStatus_Ser4.ForeColor = color;
                    break;
            }
        }

        /// <summary>
        /// 打开串口
        /// </summary>
        private bool OpenSerialPort(SerialPort serialPort, string portName)
        {
            if (serialPort == null)
                return false;

            try
            {
                // 如果已经打开，先关闭
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }

                serialPort.PortName = portName;
                serialPort.BaudRate = cup.p_SD.BaudRate;
                serialPort.DataBits = cup.p_SD.DataBits;
                serialPort.Parity = (Parity)cup.p_SD.parity;
                serialPort.StopBits = (StopBits)cup.p_SD.stopBits;
                serialPort.WriteTimeout = cup.p_SD.WriteTimeout;
              //  serialPort.ReadTimeout = cup.p_SD.ReadTimeout;

                serialPort.Open();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打开串口{portName}失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 更新串口相关的UI状态
        /// </summary>
        private void UpdateSerialUIStatus()
        {
            // 检查所有串口状态
            bool allOpen = true;
            bool anyOpen = false;

            for (int i = 0; i < 4; i++)
            {
                if (cup.p_SD.bUsed[i])
                {
                    if (sPortAll[i] != null && sPortAll[i].IsOpen)
                    {
                        anyOpen = true;
                    }
                    else
                    {
                        allOpen = false;
                    }
                }
            }

            if (allOpen && anyOpen)
            {
                toolStripStatus_Ser.Text = "所有串口已连接";
                toolStripStatus_Ser.ForeColor = Color.Green;
                toolBt_ConnSer.Checked = true;
            }
            else if (anyOpen)
            {
                toolStripStatus_Ser.Text = "部分串口已连接";
                toolStripStatus_Ser.ForeColor = Color.Orange;
                toolBt_ConnSer.Checked = true;
            }
            else
            {
                toolStripStatus_Ser.Text = "串口未连接";
                toolStripStatus_Ser.ForeColor = Color.Red;
                toolBt_ConnSer.Checked = false;
            }
        }
        private void InitTimer()
        {
            // 初始化定时器
            timerIOIn = new System.Timers.Timer(300);
            timerIOIn.Elapsed += timerIOIn_Tick;
            timerIOIn.AutoReset = false; // 只触发一次
            updateUItmp = new delegateUIUpdate(UpdateUIExe); //将委托实例化
        }

        //接受数据
        /*private void PortGetCylinder(object sender, SerialDataReceivedEventArgs e)
        {
           
            // 获取接收缓冲区长度
            Thread.Sleep(60);
            int Readlen = sPortCy.BytesToRead;
            // 新建一个 byte[] 数组 用来存放接收的数据
            byte[] inf = new byte[Readlen];
            // 接收数据到by里面
            sPortCy.Read(inf, 0, Readlen);
         *//*   string receivedText = Encoding.ASCII.GetString(inf);
            operLog.iEventStatus = -1;
            operLog.strOperEvent = "收到触发信号"+ Readlen.ToString();
            operLog.strFailReason = receivedText;
            AddLog();*//*
            //设置Y口输出命令后的反馈
            if ((Readlen == 8 || Readlen == 16) && inf[0] == 0x01 && inf[1] == 0x0f)
            {
                toolStripStatus_Hand.Text = DateTime.Now.ToString() + ":发送命令：" + strOrder;
                //  lblOrderStatus.Text = "命令发送成功";
            }
            //发送查询X口输入命令后的反馈
            else if (Readlen == 6 && inf[0] == 0x01 && inf[1] == 0x02 && inf[2] == 0x01)
            {
                if ((inf[3] & 0x01) == 0x01)  //气缸抬升完成
                {

                }
                else if ((inf[3] & 0x02) == 0x02)  //气缸下降完成
                {

                }
            }
            //主动上报X口输入的反馈
            //01:模块地址
            //45:指令号，0x45 固定为主动上报指令号
            // 05:后面要发送的字节数(不含 CRC 校验部分)
            // 01:上报的内容掩码,0x01 代表上报 DI
            //  10:换成二进制就是 0001 0000 = X8 - X7 - X6 - X5 - X4 - X3 - X2 - X1 输入口的当前状态 0 = OFF，1 = ON
            //  20:换成二进制就是 0010 0000 = X16 - X15 - X14 - X13 - X12 - X11 - X10 - X9 输入口的当前状态 0 = OFF，1 = ON
            // 40:换成二进制就是 0100 0000 = X24 - X23 - X22 - X21 - X20 - X19 - X18 - X17输入口的当前状态0 = OFF，1 = ON
            // 00:换成二进制就是 0000 0000 = X32 - X31 - X30 - X29 - X28 - X27 - X26 - X25输入口的当前状态0 = OFF，1 = ON
            //  9C 5C: 前面所有字节的 CRC 校验码

            else if (Readlen%10==0 && inf[0] == 0x01 && inf[1] == 0x45
                && inf[2] == 0x05 && inf[3] == 0x01)
            {
                if (!rts.onLS.bTrigg)
                    return;
                
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "收到有效触发信号";
                operLog.strFailReason = "-";
                AddLog();
                // 根据设置的触发条件，判断IO输入
                Byte bOrder = 0;
                int OrderType = 0;

                if (cup.p_IOPP.APlane > 0)
                {
                    bOrder = (Byte)(1 << (cup.p_IOPP.APlane - 1));
                    if ((inf[4] & bOrder) == bOrder)
                    {
                        SetFaceTriggerIndicator(0, true);
                        OrderType = 1;
                    }
                }
                
                if (cup.p_IOPP.BPlane > 0)
                {
                    bOrder = (Byte)(1 << (cup.p_IOPP.BPlane - 1));
                    if ((inf[4] & bOrder) == bOrder)
                    {
                        SetFaceTriggerIndicator(1, true);
                        OrderType = 2;
                    }
                }

                if (cup.p_IOPP.EndAPlane > 0)
                {
                    bOrder = (Byte)(1 << (cup.p_IOPP.EndAPlane - 1));
                    if ((inf[4] & bOrder) == bOrder)
                        OrderType = 3;
                }
                
                if (cup.p_IOPP.EndBPlane > 0)
                {
                    bOrder = (Byte)(1 << (cup.p_IOPP.EndBPlane - 1));
                    if ((inf[4] & bOrder) == bOrder)
                        OrderType = 4;
                }

                if (OrderType != 0)
                {
                    Thread.Sleep(5);

                    ExeIOTrig(OrderType);  //执行触发任务

                    if (!timerIOIn.Enabled)
                        timerIOIn.Start();
                }
            }
            else
            {

            }

        }
*/
        // 串口接收缓存（用于处理粘包/拆包）
        private readonly List<byte> _rxBuffer = new List<byte>();
        private readonly object _lockBuffer = new object();
        private byte lastIOInputState;
        // 修改后的串口数据接收事件
        private void PortGetCylinder(object sender, SerialDataReceivedEventArgs e)
        {           
            SerialPort ioPort = sender as SerialPort;
            if (ioPort == null || !ReferenceEquals(ioPort, sPortCy) || !cup.p_SD.bUsed[3] || !ioPort.IsOpen)
                return;
            // 获取接收缓冲区长度
            Thread.Sleep(60);
            // 1. 读取所有可用数据
            int bytesToRead = ioPort.BytesToRead;
            if (bytesToRead == 0) return;
            byte[] temp = new byte[bytesToRead];
            int bytesRead = ioPort.Read(temp, 0, bytesToRead);

            // 2. 追加到缓存，并加锁防止多线程冲突
            lock (_lockBuffer)
            {
                _rxBuffer.AddRange(temp.Take(bytesRead));
                ProcessSerialBuffer();   // 尝试解析缓存中的完整包
            }
        }

        /// <summary>
        /// 从缓存中循环解析完整的数据包，直到缓存不足一个包为止。
        /// </summary>
        /// 
        private void ProcessSerialBuffer()
        {
            while (_rxBuffer.Count >= 6)  // 最小包长为6（查询反馈）
            {
                // ---- 每次循环重新判断当前包类型 ----
                int packLen = 0;
                int diOffset = -1;
                bool bIOInput = false;

                // 1. 判断 IO 主动上报
                if (_rxBuffer.Count >= 4 &&
                    _rxBuffer[0] == 0x01 && _rxBuffer[1] == 0x45 &&
                    _rxBuffer[2] == 0x09 && _rxBuffer[3] == 0x03)
                {
                    packLen = 14;
                    diOffset = 4;
                    bIOInput = true;
                }
                else if (_rxBuffer.Count >= 5 &&
                         _rxBuffer[0] == 0x01 && _rxBuffer[1] == 0x45 &&
                         _rxBuffer[2] == 0x01 && _rxBuffer[3] == 0x00 && _rxBuffer[4] == 0x01)
                {
                    packLen = 25;
                    diOffset = 5;
                    bIOInput = true;
                }
                else if (_rxBuffer.Count >= 8 && _rxBuffer[0] == 0x01 && _rxBuffer[1] == 0x0F)
                {
                    // 设置输出反馈（长度可能8或16，这里示例取8，实际可根据协议动态）
                    packLen = 8;
                }
                else if (_rxBuffer.Count >= 6 && _rxBuffer[0] == 0x01 && _rxBuffer[1] == 0x02 && _rxBuffer[2] == 0x01)
                {
                    packLen = 6;
                }
                else
                {
                    // 无法识别：丢弃一个字节，尝试同步
                    byte unknownByte = _rxBuffer[0];
                    string hexStr = BitConverter.ToString(_rxBuffer.Take(Math.Min(10, _rxBuffer.Count)).ToArray());
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "串口未知数据";
                    operLog.strFailReason = $"跳过字节 0x{unknownByte:X2}，缓存前10字节: {hexStr}";
                    AddLog();
                    _rxBuffer.RemoveAt(0);
                    continue;  // 继续下一轮循环
                }

                // 检查完整包是否到达
                if (_rxBuffer.Count < packLen)
                    break;  // 数据不足，等待下一次接收

                // 取出完整包
                byte[] packet = _rxBuffer.GetRange(0, packLen).ToArray();
                _rxBuffer.RemoveRange(0, packLen);

                // 根据类型分发处理
                if (bIOInput)
                {
                    ProcessActiveReport(packet, packLen, diOffset);
                }
                else if (packLen == 8)  // 设置输出反馈
                {
                    ProcessSetOutputResponse(packet);
                }
                else if (packLen == 6)  // 查询反馈
                {
                    ProcessQueryResponse(packet);
                }
                // 循环继续，自动处理下一个包
            }
        }

        // 处理主动上报包（原主动上报逻辑，但改为收集多个触发类型）
        private void ProcessActiveReport(byte[] packet, int packetLength, int diOffset)
        {
            if (packet == null || packet.Length != packetLength || diOffset < 0 || diOffset >= packetLength)
                return;
            byte diByte = packet[diOffset];
            byte risingInputs = (byte)(diByte & ~lastIOInputState);
            lastIOInputState = diByte;
            if (!rts.onLS.bTrigg)
                return;

            operLog.iEventStatus = 0;
            operLog.strOperEvent = "收到有效触发信号";
            operLog.strFailReason = "-";
            AddLog();

            // 收集所有触发的类型
            List<int> triggerTypes = new List<int>();

            // A面触发
            if (cup.p_IOPP.APlane > 0)
            {
                byte mask = (byte)(1 << (cup.p_IOPP.APlane - 1));
                if ((risingInputs & mask) == mask)
                {
                    SetFaceTriggerIndicator(0, true);
                    triggerTypes.Add(1);
                }
            }

            // B面触发
            if (cup.p_IOPP.BPlane > 0)
            {
                byte mask = (byte)(1 << (cup.p_IOPP.BPlane - 1));
                if ((risingInputs & mask) == mask)
                {
                    SetFaceTriggerIndicator(1, true);
                    triggerTypes.Add(2);
                }
            }

            // EndA触发
            if (cup.p_IOPP.EndAPlane > 0)
            {
                byte mask = (byte)(1 << (cup.p_IOPP.EndAPlane - 1));
                if ((diByte & mask) == mask)
                    triggerTypes.Add(3);
            }

            // EndB触发
            if (cup.p_IOPP.EndBPlane > 0)
            {
                byte mask = (byte)(1 << (cup.p_IOPP.EndBPlane - 1));
                if ((diByte & mask) == mask)
                    triggerTypes.Add(4);
            }

            // 执行所有触发任务（按顺序，每个任务异步执行）
            if (triggerTypes.Count > 0)
            {
                Thread.Sleep(5);   // 保留短延时保证稳定性
                foreach (int type in triggerTypes)
                {
                    ExeIOTrig(type);   // ExeIOTrig 内部已经用 Task.Run 异步执行，所以可以循环调用
                    Thread.Sleep(20);
                }

                if (timerIOIn != null && !timerIOIn.Enabled)
                    timerIOIn.Start();
            }
        }

        // 处理设置输出反馈（原代码中的第一个 if）
        private void ProcessSetOutputResponse(byte[] packet)
        {
            toolStripStatus_Hand.Text = DateTime.Now.ToString() + ":发送命令：" + strOrder;
        }

        // 处理查询反馈（原代码中的 else if）
        private void ProcessQueryResponse(byte[] packet)
        {
            if ((packet[3] & 0x01) == 0x01)  // 气缸抬升完成
            {
                // 可添加业务逻辑
            }
            else if ((packet[3] & 0x02) == 0x02)  // 气缸下降完成
            {
                // 可添加业务逻辑
            }
        } 



        private void ExeIOTrig(int TrigType)
        {
            //未登录，禁止操作
            if (rts.iModelRole < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "执行功能操作";
                operLog.strFailReason = "用户身份未验证";
                AddLog();
                MessageBox.Show("用户身份未验证，无法操作");
                return;
            }
            else
            {
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "触发检测";
                operLog.strFailReason = "";
                AddLog();
            }

            switch (TrigType)
            {
                case 1:  //A 面触发
                   // if (cup.p_IOPP.EndBPlane == 0)  //没有检测结束信号，则认为每次触发仅检测一帧图像
                    {
                        if (TryReserveAutoTriggerRun(0, "A面触发运行"))
                        {
                            Task.Run(() => RunOneTimeForFace(0));
                        }
                           
                    }
                    
                    break;

                case 2:  //B面触发
                    {
                        if (TryReserveAutoTriggerRun(1, "B面触发运行"))
                        {
                            Task.Run(() => RunOneTimeForFace(1));
                        }

                    }
                    break;
                case 3:  //停止采集
                    PreviewImage(2);  //停止实时预览
                    break;

                case 4:  //停止检测
                    StopRun();
                    break;
            }

        }
       
        
        //st 2022年10月31日
        private bool openSer(SerialPort sPortS, string com)
        {
            if (sPortS == null)
                return false;
            sPortS.PortName = com;//串口的portname 
            sPortS.BaudRate = cup.p_SD.BaudRate;// 19200;//串口的波特率 
            sPortS.DataBits = cup.p_SD.DataBits;//数据位 
            sPortS.Parity = (Parity)cup.p_SD.parity;// Parity.None;////奇偶校验 
            sPortS.StopBits = (StopBits)cup.p_SD.stopBits;// StopBits.One; //停止位 1 bit
            sPortS.WriteTimeout = cup.p_SD.WriteTimeout;//写超时  

            try
            {
                sPortS.Open();

                return true;
            }
            catch
            {
                return false;
            }
        }
        //打开串口

        //关闭串口
        public bool disconnect()
        {
            bool allClosed = true;
            try {
                for (int i = 0; i < 4; i++)
                {
                    if (sPortAll[i] != null && sPortAll[i].IsOpen)
                    {
                        sPortAll[i].Close();
                        sPortAll[i].Dispose();
                        sPortAll[i] = null;
                       /* operLog.strOperEvent = "关闭串口" + (i + 1) + "成功";
                        operLog.iEventStatus = 0;
                        AddLog();*/
                        ((ToolStripStatusLabel)statusStrip1.Items[i + 8]).ForeColor = Color.Red;
                        //    (this.Controls.Find("toolStripStatus_" + (i + 1).ToString(), true)[0]).ForeColor = Color.Red;
                    }
                    else
                    {
                        //  (this.Controls.Find("toolStripStatus_" + (i + 1).ToString(), true)[0]).ForeColor = Color.Gray;
                    }
                }
              
                {
                    operLog.iEventStatus =0;
                    operLog.strOperEvent = "全部串口关闭";
                  //  operLog.strFailReason = "自动模式禁止操作";
                    AddLog();
 
                }
                toolStripStatus_Ser.Text = "串口未连接";
                toolStripStatus_Ser.ForeColor = Color.Red;
                Array.Clear(lightOnStates, 0, lightOnStates.Length);
                RefreshSelectedCameraLightUI();
                 
            }
            catch(Exception ex)
            {
                allClosed = false;
                operLog.strOperEvent = "关闭串口失败";
                operLog.strFailReason = ex.Message;
                operLog.iEventStatus = -1;
                AddLog();

            }
            return allClosed;
        

        }


        //实现接口方法

        #endregion

        // 
        #region    Runtime implementation 

        private void PeviewOneImage()
        {
            if (!bRunCondiction(0))
                return;

            vpWin._iTaskType = 0;
            rts.iTaskType = vpWin._iTaskType;
            iAcquisitionsTimes++;
            bool bRet = false;
            bool anyEnabledJob = false;
            bool allEnabledJobsSucceeded = true;
            List<int> targetJobs = GetCurrentJobTargets();
            operLog.strOperEvent = "采集图像";
            vpWin.bMainTaskFinished = false;
            if (cup.p_VPPRTP.bAcqFromCam)  //实时采集
            {
                rts.dtLastAcq = DateTime.Now;
                foreach (int wsInd in targetJobs)
                {
                    anyEnabledJob = true;
                    bool jobResult = vpWin.PreViewOneImage(wsInd);
                    allEnabledJobsSucceeded &= jobResult;
                    LogSingleImageCaptureResult(wsInd, jobResult, "真实相机");
                }
            }
            else if (!bSingleFrameModel)  //回放模式
            {
                if (iFileGroupLen <= 0)
                {
                    operLog.strFailReason = "回放图片未初始化或图片组数量为 0";
                    operLog.iEventStatus = -1;
                    AddLog();
                    return;
                }
                iFileIndex = iNextFileIndex;
                iFileIndex = iFileIndex % iFileGroupLen;
                vpWin.iFileIndex = iFileIndex;
                if (!bCrackPlayback)
                    iNextFileIndex = (++iNextFileIndex) % iFileGroupLen;

                foreach (int wsInd in targetJobs)
                {
                    anyEnabledJob = true;
                    bool jobResult = vpWin.PreViewOneImageFromFile(iFileIndex, false, wsInd);
                    allEnabledJobsSucceeded &= jobResult;
                    LogSingleImageCaptureResult(wsInd, jobResult, "回放文件");
                } 
            }
            else
            {
                if (iFileGroupLen <= 0)
                {
                    operLog.strFailReason = "回放图片未初始化或图片组数量为 0";
                    operLog.iEventStatus = -1;
                    AddLog();
                    return;
                }
                iFileIndex = iNextFileIndex;
                iFileIndex = iFileIndex % (iFileGroupLen * 6);

                foreach (int wsInd in targetJobs)
                {
                    anyEnabledJob = true;
                    bool jobResult = vpWin.PreViewOneImageFromFile(iFileIndex, false, wsInd);
                    allEnabledJobsSucceeded &= jobResult;
                    LogSingleImageCaptureResult(wsInd, jobResult, "单帧回放文件");
                }  
                iNextFileIndex = (iFileIndex + 1) % (iFileGroupLen * 6);
                
            }

            bRet = anyEnabledJob && allEnabledJobsSucceeded;
            operLog.strOperEvent = "采集图像";
            operLog.strFailReason = bRet
                ? "当前工位模式的目标工位均已采集并显示原图"
                : anyEnabledJob ? "至少一个目标工位采集失败，详见逐工位日志" : "当前工位模式没有启用的目标工位";
            if (bRet)
            {
                operLog.iEventStatus = 0;
            }
            else
            {
                operLog.iEventStatus = -1;
            }
            if (!cup.p_VPPRTP.bAcqFromCam && cup.p_VPPRTP.bAcqFromIDB)
            {
                iCurrentIndex = vpWin.GetCurImageIndex();
                iNextIndex = vpWin.GetNextImageIndex();
                toolStripStatus_No.Text = iCurrentIndex.ToString() + "|" + iNextIndex.ToString() + "||" + iFileGroupLen;
            }
            else if (!cup.p_VPPRTP.bAcqFromCam && !cup.p_VPPRTP.bAcqFromIDB)
            {
                toolStripStatus_No.Text = iFileIndex.ToString() + "|" + iNextFileIndex.ToString() + "||" + iFileGroupLen;
            }
            if (!bRet)
                iErrAcquisitionsTimes++;
            vpWin.bMainTaskFinished = true;
            UpdateStatisticsForJob(0);
            vpWin.FitAllWin();
            AddLog();

        }

        private void LogSingleImageCaptureResult(int jobIndex, bool succeeded, string source)
        {
            int cameraIndex = cup.p_CD.camInd[jobIndex];
            operLog.strOperEvent = $"采集图像-工位{jobIndex + 1}";
            operLog.iEventStatus = succeeded ? 0 : -1;
            operLog.strFailReason = succeeded
                ? $"{source}: Job{jobIndex} 已取得并显示本轮原图"
                : $"{source}: Job{jobIndex}, Cam{cameraIndex} 本轮采集失败";
            AddLog();
        }
        private void StartPreview()
        {
            if (vpWin._iTaskType == 1 && !vpWin.bMainTaskFinished)  //已经在实时预览
            {
                return;
            }
            else if (vpWin._iTaskType == 5)  //VP相关设置
            {
                return;
            }
            else   //不是，则开启实时预览
            {
                btFun_Preview.FlatStyle = FlatStyle.Flat;
                if (OpenOrCloseCam(true))
                {
                    vpWin._iTaskType = 1;

                    vpWin.bMainTaskFinished = false;
                    operLog.iEventStatus = 0;
                }
                else
                {
                    btFun_Preview.FlatStyle = FlatStyle.Popup;
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "启动预览异常";
                }
                rts.iTaskType = vpWin._iTaskType;
            }
        }
        private void StopPreview()
        {
            if (vpWin._iTaskType == -1)  //已经处于停止状态
            {
                return;
            }
            else
            {
                operLog.strOperEvent = "关闭图像预览";
                if (OpenOrCloseCam(false))
                {
                    vpWin._iTaskType = -1;
                    btFun_Preview.FlatStyle = FlatStyle.Popup;
                    vpWin.bMainTaskFinished = true;
                    operLog.iEventStatus = 0;
                }
                else
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "关闭预览异常";
                }
            }
        }
        private bool IsLivePreviewRunning()
        {
            return vpWin != null && vpWin._iTaskType == 1 && !vpWin.bMainTaskFinished;
        }

        private void ShowWarningOnUiThread(string message, string caption)
        {
            Action show = () => MessageBox.Show(this, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (InvokeRequired)
                BeginInvoke(show);
            else
                show();
        }

        private void ShowCurrentTaskNotFinishedWarning()
        {
            lock (warningMessageSync)
            {
                if ((DateTime.Now - lastTaskBusyWarningTime).TotalSeconds < 2)
                    return;

                lastTaskBusyWarningTime = DateTime.Now;
            }

            ShowWarningOnUiThread("\u5f53\u524d\u4efb\u52a1\u6ca1\u6709\u7ed3\u675f\uff0c\u8bf7\u5148\u505c\u6b62\u6216\u7b49\u5f85\u5f53\u524d\u4efb\u52a1\u5b8c\u6210\u540e\u518d\u64cd\u4f5c\u3002", "\u4efb\u52a1\u672a\u7ed3\u675f");
        }

        private bool StopLivePreviewForTrigger(string operEvent)
        {
            if (!IsLivePreviewRunning())
                return true;

            bool stopped = false;
            Action stopPreview = () =>
            {
                operLog.iEventStatus = 0;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "\u89e6\u53d1\u68c0\u6d4b\u524d\u81ea\u52a8\u5173\u95ed\u5b9e\u65f6\u9884\u89c8";
                AddLog();
                StopPreview();
                stopped = !IsLivePreviewRunning();
            };

            if (InvokeRequired)
                Invoke(stopPreview);
            else
                stopPreview();

            if (!stopped)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "\u5b9e\u65f6\u9884\u89c8\u672a\u80fd\u81ea\u52a8\u5173\u95ed";
                AddLog();
                ShowWarningOnUiThread("\u5b9e\u65f6\u9884\u89c8\u672a\u80fd\u81ea\u52a8\u5173\u95ed\uff0c\u8bf7\u624b\u52a8\u5173\u95ed\u9884\u89c8\u540e\u518d\u68c0\u6d4b\u3002", "\u9884\u89c8\u672a\u5173\u95ed");
            }

            return stopped;
        }
        private void PreviewImage(int StartOrStop)
        {
            if (!rts.initStatus.bCamInit)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "启动图像预览";
                operLog.strFailReason = "相机未初始化";
                AddLog();
                return;
            }
            if (StartOrStop == 1)  //启动预览
            {
                StartPreview();
            }
            else if (StartOrStop == 2)  //停止预览
            {
                StopPreview();
            }
            else
            {
                if (menuCam_Connect.Text == "启动图像预览")
                {
                    StartPreview();
                }
                else
                {
                    StopPreview();
                }
            }

        }
        //触发采集
        private void StartRun()
        {
            if (vpWin._iTaskType == 3 && !vpWin.bMainTaskFinished)
                return;
            vpWin._iTaskType = 3;

            if (btFunRunCont.InvokeRequired)
            {
                btFunRunCont.Invoke(new Action(() =>
                {
                    if (btFunRunCont.Image != null)
                    {
                        btFunRunCont.Image.Dispose();
                    }
                    //btFunRunCont.Image = global::VisionApplication.Properties.Resources.LoopRun2.Clone() as Image;
                    btFunRunCont.FlatStyle = FlatStyle.Flat;
                    btFunRunCont.Image = global::VisionApplication.Properties.Resources.Pause;
                    btFunRunCont.Enabled = true;

                }));
            }
            else
            {
                if (btFunRunCont.Image != null)
                {
                    btFunRunCont.Image.Dispose();
                }
                btFunRunCont.FlatStyle = FlatStyle.Flat;
                btFunRunCont.Image = global::VisionApplication.Properties.Resources.Pause;
                btFunRunCont.Enabled = true;
                //  btFunRunCont.Image = global::VisionApplication.Properties.Resources.LoopRun2.Clone() as Image;
            }

            
            vpWin.bMainTaskFinished = false;
            //2024年7月13日
            vpWin._bContRun = true;
            bTrig = true;
            contThread = new Thread(RunContinuousThread);
            contThread.Start();

            operLog.strOperEvent = "启动连续运行";
            operLog.iEventStatus = 0;
        }
        private void StopRun()
        {
            vpWin._bContRun = false;
           /* btFunRunCont.FlatStyle = FlatStyle.Popup;
            btFunRunCont.Image = global::VisionApplication.Properties.Resources.LoopRun2;*/
            //  btFunRunCont.Enabled = false;
            operLog.strOperEvent = "关闭连续运行";
            operLog.iEventStatus = 0;
        }

        private void RunContinue(int StartOrStop)
        {
            if (vpWin.bMainTaskFinished)
            {
                StartRun();
            }
            else
            {
                StopRun();
            }
        }


        private bool CheckLaserRunInterlock(string operEvent, int jobIndex)
        {
            if (!rts.onLS.bLock)
                return true;

            if (rts.initStatus.bJCZ != 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "板卡未初始化";
                AddLog();
                return false;
            }

            if (jobIndex < 0 || jobIndex >= maxCameraNum)
                return true;

            int cardSN = GetCardSNByJob(jobIndex);
            if (cardSN >= 0 && cardSN < MaxLaserCards && iLaserWorkingTypes[cardSN] != 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "当前工位对应激光任务未结束";
                AddLog();
                return false;
            }

            return true;
        }

        private bool CheckLaserRunInterlockForEnabledJobs(string operEvent)
        {
            if (!rts.onLS.bLock)
                return true;

            if (rts.initStatus.bJCZ != 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "板卡未初始化";
                AddLog();
                return false;
            }

            bool[] checkedCards = new bool[MaxLaserCards];
            for (int jobIndex = 0; jobIndex < maxCameraNum; jobIndex++)
            {
                if (!cup.p_CD.bListProcess[jobIndex])
                    continue;

                int cardSN = GetCardSNByJob(jobIndex);
                if (cardSN < 0 || cardSN >= MaxLaserCards || checkedCards[cardSN])
                    continue;

                checkedCards[cardSN] = true;
                if (iLaserWorkingTypes[cardSN] != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = operEvent;
                    operLog.strFailReason = $"板卡{cardSN + 1}激光任务未结束";
                    AddLog();
                    return false;
                }
            }

            return true;
        }

        private bool bRunCondiction(int iType, int jobIndex = -1) 
        {
            //需要的工作相机没有就绪
            if (cup.p_VPPRTP.bAcqFromCam &&  (!rts.initStatus.bCamInit || !rts.initStatus.bCamPrepared))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "采集图像";
                operLog.strFailReason = "部分相机没有准备就绪";
                AddLog();
                return false;
            }
            //回放模式，文件没有就绪
            if (!cup.p_VPPRTP.bAcqFromCam &&
                (!rts.initStatus.bLocalFileExist || iFileGroupLen <= 0))
            {
                operLog.strOperEvent = "数据检测";
                operLog.iEventStatus = -1;
                operLog.strFailReason = iFileGroupLen <= 0
                    ? "回放图片未初始化或图片组数量为 0"
                    : "回放文件夹不存在或者图像文件不存在";
                AddLog();
                return false;
            }
            if (cup.p_VPPRTP.bVisionInfer && rts.initStatus.iVPPStatus != 0)  //
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "视觉环境检测";
                operLog.strFailReason = "视觉推理模块初始化失败";
                AddLog();
                return false;
            } 
            if(iType==0)
            { 
                if (!vpWin.bMainTaskFinished)
                {
                    operLog.strOperEvent = "采集图像";
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "当前任务没有结束";
                    AddLog();
                    ShowCurrentTaskNotFinishedWarning();
                    return false;
                }
            }
            if (iType == 1)  //手动运行一次
            {
                if (IsLivePreviewRunning() && !StopLivePreviewForTrigger("\u8fd0\u884c\u4e00\u6b21"))
                    return false;
                if (System.Threading.Volatile.Read(ref manualSingleRunActive) != 0 || !vpWin.bMainTaskFinished)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "运行一次";
                    operLog.strFailReason = "当前任务没有结束";
                    AddLog();
                    ShowCurrentTaskNotFinishedWarning();
                    return false;
                }
                int targetJobIndex = (jobIndex >= 0) ? jobIndex : iJobIndex;
                if (!CheckLaserRunInterlock("运行一次", targetJobIndex))
                    return false;
            }
            else if (iType == 10)   //触发运行一次
            {
                if (IsLivePreviewRunning() && !StopLivePreviewForTrigger("\u89e6\u53d1\u8fd0\u884c"))
                    return false;
                if (!vpWin.bMainTaskFinished && (vpWin._iTaskType != 3 || !cup.p_VPPRTP.bAcqFromCam))
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "运行一次";
                    operLog.strFailReason = "当前任务没有结束";
                    AddLog();
                    ShowCurrentTaskNotFinishedWarning();
                    return false;
                }
                int targetJobIndex = (jobIndex >= 0) ? jobIndex : iJobIndex;
                if (!CheckLaserRunInterlock("触发运行", targetJobIndex))
                    return false;

            }
            else if(iType==3)  //连续运行
            {
                if (!CheckLaserRunInterlockForEnabledJobs("连续运行"))
                    return false;

                if (vpWin._iTaskType != 3)
                {
                    if (!vpWin.bMainTaskFinished)
                    {
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = "连续运行";
                        operLog.strFailReason = "当前任务没有结束";
                        AddLog();
                        ShowCurrentTaskNotFinishedWarning();
                        return false;
                    }
                } 
            }
             
            return true;
        }

      
        private void runonetime()
        { 

            //具备运行条件，则执行一次运行
            {

                bool bKeep = (vpWin._iTaskType == 0);
                vpWin._iTaskType = 2;
                if (!bKeep)
                {
                    if (!cup.p_VPPRTP.bAcqFromCam/* && !toolBtSta_LockImage.Checked*/)  //从文件读取
                    {
                        if (iFileGroupLen <= 0)
                        {
                            vpWin._iTaskType = -1;
                            operLog.strOperEvent = "运行一次";
                            operLog.iEventStatus = -1;
                            operLog.strFailReason = "回放图片未初始化或图片组数量为 0";
                            AddLog();
                            return;
                        }
                        iFileIndex = iNextFileIndex;

                        iFileIndex = iFileIndex % iFileGroupLen;
                        vpWin.iFileIndex = iFileIndex;
                        // iNextFileIndex = iFileIndex;
                        if (!bCrackPlayback)
                            iNextFileIndex = (++iNextFileIndex) % iFileGroupLen;

                    }
                }

                vpWin.bMainTaskFinished = false;

                System.Threading.Interlocked.Exchange(ref manualSingleRunActive, 1);
                System.Threading.Interlocked.Exchange(ref completedJobCount, 0);
                allJobsDone = false;
                //    oneTimeThread = new Thread(RunOneTimeThread);
                oneTimeThread = new Thread(() => RunOneTimeThread(iJobIndex==-1));
                try
                {
                    oneTimeThread.Start();
                }
                catch
                {
                    System.Threading.Interlocked.Exchange(ref manualSingleRunActive, 0);
                    vpWin.bMainTaskFinished = true;
                    throw;
                }


                // operLog.iEventStatus = 0;
                // operLog.strOperEvent = "运行一次";
                if (vpWin._iTaskType == 2)
                    btFunRunOneTime.FlatStyle = FlatStyle.Flat;
                // operLog.iEventStatus = 0;
            }


        }
      
        //触发运行模式
        public bool RunTrigModeInit()
        {
            /*  int iCamInd = cup.p_CD.triggleCam;
              vpWin.cbImage = new MyCamera.cbOutputExdelegate(vpWin.ImageCallBackCont);
              camera2[iCamInd].MV_CC_RegisterImageCallBackEx_NET(vpWin.cbImage, (IntPtr)iCamInd);
              vpWin.SetTrigger(cup.p_CD.bTriggle, cup.p_CD.triggleMode, iCamInd);
              */

            hIKVisionCamera.StopGrabbing(cup.p_CD.camInd[cup.p_CD.triggleCam]);
            bool bSucc = vpWin.InitAcqCB();
            hIKVisionCamera.StartGrabbing(cup.p_CD.camInd[cup.p_CD.triggleCam]);
            return bSucc;

        }
        public void RunCont()
        {
            if (vpWin.RunCont())
            {
                /*if (rts.onLS.bTrigg)
                {
                    RunTrigModeInit();

                }
                else*/
                //st 非触发模式，启用线程循环采集图像并检测
                /*{
                    contThread = new Thread(RunContinuousThread);
                    contThread.Start();
                }*/
            }
            else
            {
                vpWin.bMainTaskFinished = true;
            }
        }

  
       

        public void StartService(string serviceName)
        {
            ServiceController service = new ServiceController(serviceName);
            if (service.Status != ServiceControllerStatus.Running)
            {
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(60));
            }
        }
        private void toolBt_LaserPreview_Click(object sender, EventArgs e)
        {
            StartService("Cognex Security Service");
        }
         
        /// <summary>
        /// 设置 ROI 为全尺寸（相机最大分辨率）
        /// </summary>
        /*private void btSetROIFull_Click(object sender, EventArgs e)
        {
            int icamInd = icamSelectInd;
            if (icamInd < 0)
            {
                MessageBox.Show("请先选择相机");
                return;
            }
            if (camera2 == null || camera2[icamInd] == null)
            {
                MessageBox.Show("相机未初始化");
                return;
            }
         //   hIKVisionCamera.StopGrabbing(icamInd);
            // 获取相机的最大分辨率（Width/Height 的最大值）
            MyCamera.MVCC_INTVALUE stW = new MyCamera.MVCC_INTVALUE();
            MyCamera.MVCC_INTVALUE stH = new MyCamera.MVCC_INTVALUE();
            int retW = camera2[icamInd].MV_CC_GetIntValue_NET("Width", ref stW);
            int retH = camera2[icamInd].MV_CC_GetIntValue_NET("Height", ref stH);
            if (retW != MyCamera.MV_OK || retH != MyCamera.MV_OK)
            {
                MessageBox.Show("无法获取相机的最大分辨率，请检查相机连接");
                return;
            }
            uint maxWidth = stW.nMax;
            uint maxHeight = stH.nMax;

            // 设置 ROI 参数：偏移为0，宽高为最大值
            cup.p_Cam[icamInd].iROI[0] = 0;
            cup.p_Cam[icamInd].iROI[1] = 0;
            cup.p_Cam[icamInd].iROI[2] = maxWidth;
            cup.p_Cam[icamInd].iROI[3] = maxHeight;

            // 将 ROI 设置应用到相机硬件
            bool bSucc = hIKVisionCamera.SetROIParam(cup.p_Cam[icamInd], camera2[icamInd]);
            if (bSucc)
            {
                // 更新 UI 中的数值显示
                numROIOffSetX.Value = 0;
                numROIOffSetY.Value = 0;
                numROIWidth.Value = maxWidth;
                numROIHeight.Value = maxHeight;

                operLog.iEventStatus = 0;
                operLog.strOperEvent = "设置ROI全尺寸";
                operLog.strFailReason = $"宽度={maxWidth}, 高度={maxHeight}";
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "设置ROI全尺寸";
                operLog.strFailReason = "调用相机接口失败";
            }
            AddLog();
         //   hIKVisionCamera.StartGrabbing(icamInd);
        }*/
        private void btSetROIFull_Click(object sender, EventArgs e)
        {
            int icamInd = icamSelectInd;
            if (icamInd < 0) return;
            if (camera2 == null || camera2[icamInd] == null) return;

            // 获取最大分辨率
            MyCamera.MVCC_INTVALUE stW = new MyCamera.MVCC_INTVALUE();
            MyCamera.MVCC_INTVALUE stH = new MyCamera.MVCC_INTVALUE();
            if (camera2[icamInd].MV_CC_GetIntValue_NET("Width", ref stW) != MyCamera.MV_OK ||
                camera2[icamInd].MV_CC_GetIntValue_NET("Height", ref stH) != MyCamera.MV_OK)
                return;

            uint maxWidth = stW.nMax;
            uint maxHeight = stH.nMax;
            uint[] roi = new uint[] { 0, 0, maxWidth, maxHeight };

            bool bSucc = ApplyCameraROI(icamInd, roi);
            if (bSucc)
            {
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "设置ROI全尺寸并重新分配缓冲区";
                operLog.strFailReason = $"宽度={maxWidth}, 高度={maxHeight}, 载荷大小={acqFIFO.GetPayloadSize(icamInd)}";
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "设置ROI全尺寸失败";
                operLog.strFailReason = "可能相机不支持该分辨率或缓冲区重新分配失败";
            }
            AddLog();
        }

        /// <summary>
        /// 修改相机 ROI，并重新获取 PayloadSize，重新分配缓冲区
        /// </summary>
        /// <param name="cameraIndex">相机索引</param>
        /// <param name="roi">新 ROI [OffsetX, OffsetY, Width, Height]</param>
        /// <returns>是否成功</returns>
        private bool ApplyCameraROI(int cameraIndex, uint[] roi)
        {
            if (cameraIndex < 0 || cameraIndex >= camera2.Length) return false;
            if (camera2[cameraIndex] == null) return false;
            if (roi == null || roi.Length != 4) return false;

            // 1. 设置 ROI（调用简化版 SetROIParam）
            cup.p_Cam[cameraIndex].iROI = roi;
            bool bSucc = hIKVisionCamera.SetROIParam(cup.p_Cam[cameraIndex], camera2[cameraIndex]);
            if (!bSucc) return false;

            // 2. 获取新的 PayloadSize
            MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
            int ret = camera2[cameraIndex].MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
            if (ret != MyCamera.MV_OK)
            {
                // 如果获取失败，尝试根据分辨率计算（备选方案）
                uint calculatedSize = roi[2] * roi[3] * (uint)(cup.p_Cam[cameraIndex].iMonoType == 0 ? 1 : 3);
                acqFIFO.ReallocateBuffer(cameraIndex, calculatedSize);
                return false;
            }
            uint newPayloadSize = stParam.nCurValue;

            // 3. 重新分配缓冲区
            if (!acqFIFO.ReallocateBuffer(cameraIndex, newPayloadSize))
                return false;

            // 4. 更新相机状态（如果 AcqFIFO 内部维护了状态）
            // 如果有 _cameraStatus，可在此更新
            // rts.cs.iStatus[cameraIndex] = 3; // 就绪

            // 5. 更新 UI 显示（可选）
            numROIOffSetX.Value = roi[0];
            numROIOffSetY.Value = roi[1];
            numROIWidth.Value = roi[2];
            numROIHeight.Value = roi[3];

            return true;
        }
        /// <summary>
        /// Gamma 使能复选框的状态变化事件
        /// </summary>
        private void chkGamma_Enable_CheckedChanged(object sender, EventArgs e)
        {
            // 新增 Gamma 使能/模式功能停用，不调用相机SDK。
        }

        /// <summary>
        /// Gamma 用户模式复选框的状态变化事件
        /// 勾选代表使用用户自定义 Gamma 值（User 模式），未勾选代表使用 sRGB 标准曲线
        /// </summary>
        private void chkGamma_UserMode_CheckedChanged(object sender, EventArgs e)
        {
            // 新增 Gamma 使能/模式功能停用，不调用相机SDK。
        }

        private void timerIOOut_Tick(object sender, EventArgs e)
        {
           
            lblOut1.ForeColor = Color.Black;
            lblOut2.ForeColor = Color.Black;
            //timerIOOut.Enabled = false;
            timerIOOut.Stop();
        }

        bool b9PointEdit = false;
          CogCalibNPointToNPointEditV2 calibEditV2;  // 9点标定编辑控件
       // CogCalibNPointToNPointTool  NPointV2;
        private void bt9PointParam_Click(object sender, EventArgs e)
        {
            // 1. 检查任务状态
            if (vpWin._iTaskType != 5 && !vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "9点标定高级设置";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                return;
            }
            if (rts.iModelRole < 3)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "模板定位";
                operLog.strFailReason = "用户权限不够";
                AddLog();
                return;
            }
            // 2. 切换编辑模式
            b9PointEdit = !b9PointEdit;

            if (b9PointEdit)
            {
                // ---- 进入编辑模式 ----
                iTmpType = vpWin._iTaskType;

                // 创建 9 点标定编辑控件
                calibEditV2 = new CogCalibNPointToNPointEditV2();
                calibEditV2.Subject = vpWin._NpointCalibTool;   // 假设 vpWin 中公开了 9 点标定工具

                // 添加到容器
                splitContainer2.Panel1.Controls.Add(calibEditV2);
                calibEditV2.Dock = DockStyle.Fill;

                // 隐藏 VP 视图
                vpWin.Visible = false;
                bVPPParamChanged = true;

                // 更新状态
                vpWin._iTaskType = 5;
                vpWin.bEnableActivate = false;
                vpWin.bMainTaskFinished = false;

                ((Button)sender).FlatStyle = FlatStyle.Flat;

                operLog.iEventStatus = 0;
                operLog.strOperEvent = "进入9点标定编辑模式";
                AddLog();
            }
            else
            {
                // ---- 退出编辑模式 ----
                // 将修改写回 VP
                if (vpWin.setNpointCalibTool(calibEditV2.Subject,iJobIndex))  // 假设 vpWin 有 setCalibTool 方法
                {
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = "9点标定参数已更新";
                }
                else
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "9点标定参数写回失败";
                    operLog.strFailReason = vpWin.strErrInfo?[0] ?? "未知错误";
                }

                // 移除编辑控件
                splitContainer2.Panel1.Controls.Remove(calibEditV2);
                calibEditV2.Dispose();
                bVPPParamChanged = true;
                // 恢复状态
                vpWin._iTaskType = iTmpType;
                vpWin.bEnableActivate = true;
                vpWin.bMainTaskFinished = true;
                vpWin.Visible = true;

                ((Button)sender).FlatStyle = FlatStyle.Popup;

                AddLog();
            }
        }
 

        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (rts.onLS.bTrigg)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "参数设置";
                operLog.strFailReason = "自动模式禁止操作";
                AddLog();
                return;
            }
            if (vpWin._iTaskType == 5)
            {
                MessageBox.Show("请先关闭当前工具");
                e.Cancel = true;  // 阻止切换
            }
        }

        private void menuCam_Click(object sender, EventArgs e)
        {

        }         

    

        string strEngineerName = "";
        string strEngineerDir = "";
        /// <summary>
        /// 加载工程：恢复训练图像、激光文件、用户参数
        /// </summary>
        private async void menuLaser_LoadEngine_Click(object sender, EventArgs e)
        {
            // 1. 检查任务状态与权限
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "加载工程";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                return;
            }
            if (rts.iModelRole < 3)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "加载工程";
                operLog.strFailReason = "用户权限不够";
                AddLog();
                return;
            } 

            //ezd文件可能随时修改，加载之前先备份ezd文件
            if (strEngineerDir != "")
            {
                SaveLaserFile();
            } 

            // 2. 选择工程根目录
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "选择工程根目录";
            folderDialog.SelectedPath = cup.p_DL.modelPath; // 默认上次结果保存路径
            if (folderDialog.ShowDialog() != DialogResult.OK) return;
            string rootDir = folderDialog.SelectedPath;
            cup.p_DL.modelPath = rootDir;
            // 3. 检查必需的子目录和文件
            string pmaDir = Path.Combine(rootDir, "PMAlignTools");
            string calibDir= Path.Combine(rootDir, "CalibTools");
            string findlineDir = Path.Combine(rootDir, "FindLineTools");
            string trainDir = Path.Combine(rootDir, "TrainImages");
            string laserDir = Path.Combine(rootDir, "LaserFiles");
            string paramFile = Path.Combine(rootDir, "UserParam.dat");
            string LaserParamDir = Path.Combine(rootDir, "LaserParam");


            if (!Directory.Exists(pmaDir) || !Directory.Exists(trainDir) ||
                !Directory.Exists(laserDir) || !Directory.Exists(findlineDir))
            {
                MessageBox.Show("所选目录不是有效工程（缺少必要子目录或文件）。", "错误",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            bVPPParamChanged = true;
            strEngineerDir = rootDir;
            strEngineerName = Path.GetFileName(rootDir);
            toolStripStatus_Engineer.Text = "机种:" + strEngineerName;
            if(cup.p_VPPRTP.bVisionInfer)
            {
                this.Text = ApplicationName + "   " + cup.p_Paf.strVPPFilePath;
            }
            else
                this.Text = ApplicationName + "   " + cup.p_DL.modelPath;
            try
            {
                // 4. 加载 PMA 工具（两个工位的 .vpp 文件）
                vpWin.LoadAllPMAlignTools(pmaDir);
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "加载模板匹配工具";
                operLog.strFailReason = $"从 {pmaDir} 加载";
                AddLog();

                vpWin.LoadAllCalibTools(calibDir);
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "加载标定工具";
                operLog.strFailReason = $"从 {calibDir} 加载";
                AddLog();

                vpWin.LoadAllFindLineTools(findlineDir);
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "加载边缘定位工具";
                operLog.strFailReason = $"从 {findlineDir} 加载";
                AddLog();

                /* vpWin.ExtractLocateBase(0,cup.p_VPPRTP.iXBType);
                 vpWin.ExtractLocateBase(1, cup.p_VPPRTP.iXBType);
                 operLog.iEventStatus = 0;
                 operLog.strOperEvent = "提取定位基准";
                 AddLog();*/

                // 5. 加载训练图像到 PMA 工具（用于重新训练或显示）
                // 5. 恢复背景模板图到程序 data 目录（仅文件复制，不加载到VP）
                string dataDir = Path.Combine(Application.StartupPath, "data");
                if (!Directory.Exists(dataDir))
                    Directory.CreateDirectory(dataDir);

                foreach (string file in Directory.GetFiles(trainDir, "*.jpg"))
                {
                    string dest = Path.Combine(dataDir, Path.GetFileName(file));
                    File.Copy(file, dest, true);
                }
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "恢复背景模板图";
                operLog.strFailReason = $"从 {trainDir} 复制到 {dataDir}";
                AddLog();

                // 6. 恢复激光加工文件到 ManuMarkPath（覆盖）
                string destLaserDir = cup.p_Paf.ManuMarkPath;
                if (!Directory.Exists(destLaserDir))
                    Directory.CreateDirectory(destLaserDir);
                foreach (string file in Directory.GetFiles(laserDir, "*.*"))
                {
                    string dest = Path.Combine(destLaserDir, Path.GetFileName(file));
                    File.Copy(file, dest, true);
                }
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "恢复激光文件";
                operLog.strFailReason = $"从 {laserDir} 复制到 {destLaserDir}";
                AddLog();

                // 7. 加载用户参数（反序列化并更新当前 cup）
                /* CUserParam loadedCup = new CUserParam();
                 if (fo.ReadUserParam(paramFile, ref loadedCup))
                 {
                     // 保留运行时状态（如相机句柄等）仅更新配置项
                     // 注意：此处简单整体替换，实际可能需要保留某些运行时对象
                     cup = loadedCup;

                 }
                 else
                 {
                     operLog.iEventStatus = -1;
                     operLog.strOperEvent = "加载用户参数";
                     operLog.strFailReason = "反序列化失败，文件可能损坏";
                     AddLog();
                 }*/


                // 恢复激光器参数设置
                if(Directory.Exists(pmaDir))
                    CopyDirectory( LaserParamDir, ".\\JCZ", true);


                // 8. 提示用户重启软件使所有设置生效（或自动重新初始化）
                /*  MessageBox.Show("工程加载完成！建议重启软件使所有参数生效。", "提示",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);*/
                bReLoadBGImg = true; //重新加载工程要更新背景图    
                if (!_laserDispatchersReady || !IsJCZServerRunning())
                {
                    await Task.Run(() => InitJCZ());
                }
                else
                {
                    ScheduleInitializeJczCards(200);
                }

                operLog.iEventStatus = 0;
                operLog.strOperEvent = "加载工程";
                operLog.strFailReason = $"从 {rootDir} 加载成功";
                AddLog();
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "加载工程";
                operLog.strFailReason = ex.Message;
                AddLog();
                MessageBox.Show($"加载失败：{ex.Message}", "错误",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void menuLaser_SaveEngine_Click(object sender, EventArgs e)
        {
            // 1. 检查权限与任务状态
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "保存工程";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                return;
            }
            if (rts.iModelRole < 0)  //是否需要权限等级大于等于0？根据实际情况调整
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "保存工程";
                operLog.strFailReason = "用户权限不够";
                AddLog();
                return;
            }

            // 2. 选择目标文件夹
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "请选择或创建工程保存目录";
            folderDialog.SelectedPath = cup.p_DL.modelPath;
            folderDialog.ShowNewFolderButton = true;
            if (folderDialog.ShowDialog() != DialogResult.OK) return;
            string rootDir = folderDialog.SelectedPath;

            // 3. 创建子目录
            string PMADir = Path.Combine(rootDir, "PMAlignTools");
            string calibDir = Path.Combine(rootDir, "calibTools");
            string trainDir = Path.Combine(rootDir, "TrainImages");
            string laserDir = Path.Combine(rootDir, "LaserFiles");
            string FindLineDir= Path.Combine(rootDir, "FindLineTools");
            string LaserParamDir = Path.Combine(rootDir, "LaserParam");
            if (!Directory.Exists(rootDir))
                Directory.CreateDirectory(rootDir);
            if(!Directory.Exists(PMADir))
                Directory.CreateDirectory(trainDir);
            if (!Directory.Exists(calibDir))
                Directory.CreateDirectory(calibDir);
            if (!Directory.Exists(laserDir))
                Directory.CreateDirectory(laserDir);
            if(!Directory.Exists(PMADir))
                Directory.CreateDirectory(PMADir);
            if(!Directory.Exists(FindLineDir))
                Directory.CreateDirectory(FindLineDir);
            if (!Directory.Exists(LaserParamDir))
                Directory.CreateDirectory(LaserParamDir);

            try
            {
                //保存    PMA 工具
                vpWin.SaveAllPMAlignTools(PMADir);
                //保存    PMA 工具
                vpWin.SaveAllCalibTools(calibDir);
                //保存    精定位 工具
               /* vpWin.SaveAllFindLineTools(FindLineDir);*/

                // 4. 保存训练背景图像（使用 VisionControl 中已有的 SaveTrainImage 方法）
                vpWin.SaveBGTrainImageFiles(trainDir);   // 需要实现

                // 5. 复制激光加工文件
                //    从 cup 中读取激光文件路径（假设保存在 cup.p_Laser.strLaserFile[0/1]）
                //    或者让用户手动指定，这里我们根据配置复制
                string[] laserSourcePaths = new string[2];
                laserSourcePaths[0] = Path.Combine(cup.p_Paf.ManuMarkPath, "route0.ezd");  // 您需要自己定义字段
                laserSourcePaths[1] = Path.Combine(cup.p_Paf.ManuMarkPath, "route1.ezd");
                for (int i = 0; i < 2; i++)
                {
                    if (!string.IsNullOrEmpty(laserSourcePaths[i]) && File.Exists(laserSourcePaths[i]))
                    {
                        string dest = Path.Combine(laserDir,Path.GetFileName(laserSourcePaths[i]));
                        File.Copy(laserSourcePaths[i], dest, true);
                    }
                    else
                    {
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = $"复制激光文件 Job{i}";
                        operLog.strFailReason = "源文件不存在或路径未配置";
                        AddLog();
                    }
                }

                // 6. 保存用户参数（CUserParam）
              //  string paramFile = Path.Combine(rootDir, "UserParam");
                FileOper.SaveUserParam(cup, rootDir);   // 需要扩展 FileOper 支持指定路径

                operLog.iEventStatus = 0;
                operLog.strOperEvent = "保存工程";
                operLog.strFailReason = $"已保存到 {rootDir}";
                AddLog();
                MessageBox.Show("工程保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //7. 保存激光器参数
                string strLaserParamDirDest = Path.Combine(LaserParamDir, "Custom.ini"); ;
                string strLaserParamDirSrc = Path.Combine(".\\JCZ\\", "Custom.ini");
                File.Copy(strLaserParamDirSrc, strLaserParamDirDest, true);

                strLaserParamDirDest = Path.Combine(LaserParamDir, "EZCAD.CFG"); ;
                strLaserParamDirSrc = Path.Combine(".\\JCZ\\", "EZCAD.CFG");
                File.Copy(strLaserParamDirSrc, strLaserParamDirDest, true);
                CopyFolderToParent(".\\JCZ\\plug", LaserParamDir);

                cup.p_DL.modelPath = rootDir;
                strEngineerName = Path.GetFileName(cup.p_DL.modelPath);
                toolStripStatus_Engineer.Text = "机种:" + strEngineerName;
                if (cup.p_VPPRTP.bVisionInfer)
                {
                    this.Text = ApplicationName + "   " + cup.p_Paf.strVPPFilePath;
                }
                else
                    this.Text = ApplicationName + "   " + cup.p_DL.modelPath;

            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "保存工程";
                operLog.strFailReason = ex.Message;
                AddLog();
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // 如果 VPP 参数发生改变，保存 VPP 文件
            // 如果 VPP 参数发生改变，保存 VPP 文件
            if (bVPPParamChanged)
            {
                try
                {
                    int saveResult = vpWin.SaveVPP();
                    if (saveResult == 0)
                    {
                        operLog.iEventStatus = 0;
                        operLog.strOperEvent = "保存VPP文件";
                        operLog.strFailReason = $"已保存到 {cup.p_Paf.strVPPFilePath}";
                        AddLog();
                        bVPPParamChanged = false;   // 重置标志
                    }
                    else
                    {
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = "保存VPP文件";
                        operLog.strFailReason = $"保存失败，错误码：{saveResult}";
                        AddLog();
                    }
                }
                catch (Exception ex)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "保存VPP文件";
                    operLog.strFailReason = ex.Message;
                    AddLog();
                }
            }
        }

        private void SaveLaserFile()
        {
            string laserDir = Path.Combine(strEngineerDir, "LaserFiles");

            string[] laserSourcePaths = new string[2];
            laserSourcePaths[0] = Path.Combine(cup.p_Paf.ManuMarkPath, "route0.ezd");  // 您需要自己定义字段
            laserSourcePaths[1] = Path.Combine(cup.p_Paf.ManuMarkPath, "route1.ezd");
            for (int i = 0; i < 2; i++)
            {
                if (!string.IsNullOrEmpty(laserSourcePaths[i]) && File.Exists(laserSourcePaths[i]))
                {
                    string dest = Path.Combine(laserDir, Path.GetFileName(laserSourcePaths[i]));
                    File.Copy(laserSourcePaths[i], dest, true);
                }
                else
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = $"复制激光文件 Job{i}";
                    operLog.strFailReason = "源文件不存在或路径未配置";
                    AddLog();
                }
            }
        }
        /// <summary>
        /// 将整个源文件夹（如 plug）复制到目标父文件夹（如 LaserParam）下
        /// </summary>
        /// <param name="sourcePath">源文件夹路径（如 @"D:\Data\plug"）</param>
        /// <param name="destParentPath">目标父文件夹路径（如 @"D:\Data\LaserParam"）</param>
        private void CopyFolderToParent(string sourcePath, string destParentPath)
        {
            DirectoryInfo sourceDir = new DirectoryInfo(sourcePath);
            if (!sourceDir.Exists)
            {
                Console.WriteLine($"错误：源文件夹不存在 - {sourcePath}");
                return;
            }

            // ★ 核心：目标路径 = LaserParam 文件夹下的 plug 文件夹 ★
            string destFullPath = Path.Combine(destParentPath, sourceDir.Name);

            // 开始递归复制
            CopyRecursive(sourceDir.FullName, destFullPath);
        }

        private   void CopyRecursive(string source, string destination)
        {
            // 1. 创建目标文件夹（如果 LaserParam\plug 已存在，不会报错）
            Directory.CreateDirectory(destination);

            // 2. 复制当前目录下的所有文件
            foreach (string file in Directory.GetFiles(source))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destination, fileName);
                File.Copy(file, destFile, overwrite: true); // true 表示同名文件直接覆盖
            }

            // 3. 递归处理所有子文件夹（如果 plug 里面还有文件夹，一并复制）
            foreach (string subDir in Directory.GetDirectories(source))
            {
                string dirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destination, dirName);
                CopyRecursive(subDir, destSubDir);
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir, bool overwrite = true)
        {
            // 1. 校验源目录是否存在
            DirectoryInfo source = new DirectoryInfo(sourceDir);
            if (!source.Exists)
                throw new DirectoryNotFoundException($"源目录不存在: {sourceDir}");

            // 2. 创建目标目录（如果已存在则不会报错，等同于 EnsureDirectory）
            Directory.CreateDirectory(destinationDir);

            // 3. 复制当前目录下的所有文件
            foreach (FileInfo file in source.GetFiles())
            {
                string destFile = Path.Combine(destinationDir, file.Name);
                file.CopyTo(destFile, overwrite);
            }

            // 4. 递归复制所有子目录
            foreach (DirectoryInfo subDir in source.GetDirectories())
            {
                string destSubDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, destSubDir, overwrite);
            }
        }

        private void cmbLs_Channel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI || _isUpdatingLightUI) return;
            RefreshSelectedCameraLightUI();
        }

        private void chkLS_Sync_Click(object sender, EventArgs e)
        {
            if (chkLS_Sync.Checked)
            {
                ApplySelectedCameraLightBrightness(null, tbBirghtness.Value);
            }
            else
            {
                RefreshSelectedCameraLightUI();
            }
          //  ApplyLightBrightness(iJobIndex);
        }

        private void menu_View_ErrLog_Click(object sender, EventArgs e)
        {
            List<string> errorList;

            lock (_errorLogLock)
            {
                // 如果内存缓存为空，尝试从文件加载最近20条
                if (_recentErrorLogs.Count == 0)
                {
                    errorList = LoadRecentErrorsFromFile();
                }
                else
                {
                    errorList = new List<string>(_recentErrorLogs);
                }
            }

            if (errorList.Count == 0)
            {
                MessageBox.Show("暂无错误日志记录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 显示错误日志
            ShowErrorLogDialog(errorList);
        }
        private void ShowErrorLogDialog(List<string> errorList)
        {
            // 使用对话框展示列表
            FrmErrorLogViewer frm = new FrmErrorLogViewer(errorList);
            frm.Show();
        }
        private List<string> LoadRecentErrorsFromFile()
        {
            List<string> lines = new List<string>();
            try
            {
                string logDir = cup.p_Paf.strLogPath;
                if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
                    return lines;

                // 获取当前日期及前两天的错误日志文件，防止跨天边界
                string[] files = new string[]
                {
            $"ErrorLog_{DateTime.Now:yyyyMMdd}.txt",
            $"ErrorLog_{DateTime.Now.AddDays(-1):yyyyMMdd}.txt",
            $"ErrorLog_{DateTime.Now.AddDays(-2):yyyyMMdd}.txt"
                };

                foreach (var fileName in files)
                {
                    string fullPath = Path.Combine(logDir, fileName);
                    if (File.Exists(fullPath))
                    {
                        // 读取所有行并逆序取最后N行
                        var allLines = File.ReadAllLines(fullPath);
                        lines.AddRange(allLines.Reverse().Take(20 - lines.Count));
                        if (lines.Count >= 20) break;
                    }
                }

                // 按时间顺序恢复（逆序再反转）
                lines.Reverse();
            }
            catch
            {
                // 忽略读取错误
            }
            return lines;
        }
          
        calipParam clp = new calipParam();
        private void btGetCalipEdgeParam_Click(object sender, EventArgs e)
        {
            vpWin.GetCLPParam(ref clp, iJobIndex, cmbCLPEdgeLeft.SelectedIndex == 0);

            UpdateUIFromCLPParam();
        }

        private void btSetCLPEdgeParam_Click(object sender, EventArgs e)
        {
            UpdateCLPParamFromUI();

            vpWin.SetCLPParam(clp, iJobIndex, cmbCLPEdgeLeft.SelectedIndex == 0);

        }


        private void UpdateUIFromCLPParam()
        {
            cmbCLPPol.SelectedIndex = clp.bPolarity;
            nudCLPEdgeThre.Value = (decimal)clp.dThre;
            nudCLPRad.Value = (decimal)clp.iRadius;
        }
        private void UpdateCLPParamFromUI()
        {
            clp.bPolarity = cmbCLPPol.SelectedIndex;
            clp.dThre = (double)nudCLPEdgeThre.Value;
            clp.iRadius = (int)nudCLPRad.Value;
            clp.iRes1 = (int)nudLenTh.Value;
        }


        private CogCaliperEditV2 clpV2 = new CogCaliperEditV2();
        bool bCalipEdit = false;
        private void btEdgeParam_Click(object sender, EventArgs e)
        {
            {
                if (vpWin._iTaskType != 5 && !vpWin.bMainTaskFinished)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "卡尺测量";
                    operLog.strFailReason = "当前任务未结束";
                    AddLog();
                    return;
                }
                if (rts.iModelRole < 3)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "卡尺测量";
                    operLog.strFailReason = "用户权限不够";
                    AddLog();
                    return;
                }

                if (rts.initStatus.iVPPStatus != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "卡尺测量";
                    operLog.strFailReason = "流程文件初始化失败";
                    return;
                }
                operLog.strOperEvent = "卡尺测量";


                bCalipEdit = !bCalipEdit;
                if (bCalipEdit)
                {
                    iTmpType = vpWin._iTaskType;
                    clpV2 = new CogCaliperEditV2();
                    clpV2.Subject = vpWin.getCLPEdge(iJobIndex, cmbCLPEdgeLeft.SelectedIndex == 0);  //2026年6月14日 需要修改第二个参数
                    splitContainer2.Panel1.Controls.Add(clpV2);
                    clpV2.Dock = DockStyle.Fill;

                    //     pmaV2 = new CogPMAlignEditV2();
                    //  pmaV2.Subject = vpWin._pmaMT;
                    //  splitContainer3.Panel1.Controls.Add(pmaV2);
                    //   pmaV2.Dock = DockStyle.Fill;
                    vpWin.Visible = false;
                    bVPPParamChanged = true;


                    vpWin._iTaskType = 5;
                    operLog.iEventStatus = 0;
                    ((Button)sender).FlatStyle = FlatStyle.Flat;
                    vpWin.bMainTaskFinished = false;
                }
                else
                {
                    vpWin.setCLPEdge(clpV2.Subject, iJobIndex, cmbCLPEdgeLeft.SelectedIndex == 0); //2026年6月14日 需要修改第三个参数
                    splitContainer2.Panel1.Controls.Remove(clpV2);
                    clpV2.Dispose();
                    /*  vpWin.setPMA(pmaV2.Subject);
                      splitContainer3.Panel1.Controls.Remove(pmaV2);
                      pmaV2.Dispose();*/


                    vpWin._iTaskType = iTmpType;
                    ((Button)sender).FlatStyle = FlatStyle.Popup;
                    vpWin.bMainTaskFinished = true;

                    vpWin.Visible = true;

                }

            }
        } 

        private void btMeasEdge_Click(object sender, EventArgs e)
        {
            try
            {
                int index = cmbCLPEdgeInd.SelectedIndex + 1;
                if (index < 1 || index > 2)
                {
                    MessageBox.Show("请选择定位边缘");
                    return;
                }
                vpWin.SetFindLineSearchRegion(iJobIndex, index, 1);
                bVPPParamChanged = true;
            }
            catch { }
        }

        private void btMeasEdgeExtra_Click(object sender, EventArgs e)
        {
            try
            {
                int index = cmbCLPEdgeInd.SelectedIndex + 1;
                if (index < 1 || index > 2)
                {
                    MessageBox.Show("请选择定位边缘");
                    return;
                }
                vpWin.GetFindLineSearchRes(iJobIndex, index, cup.p_VPPRTP.iXBType);
                bVPPParamChanged = true;
            }
            catch { }
        }

        private void menuTask_VisionInfer_Click_1(object sender, EventArgs e)
        {
            if (!cup.p_VPPRTP.bVisionInfer)
                SwitchInferenceModel(true);
        }

        private void menuTask_DeepInfer_Click(object sender, EventArgs e)
        {
            if (!cup.p_VPPRTP.bDeepInfer)
                SwitchInferenceModel(false);
        }
        /// <summary>
        /// 切换推理模式
        /// </summary>
        /// <param name="bToVisionInfer">true: 切换到视觉推理；false: 切换到深度推理</param>
        private void SwitchInferenceModel(bool bToVisionInfer)
        {
            // 1. 检查当前任务是否允许切换（如连续运行、预览等）
            if (vpWin != null && !vpWin.bMainTaskFinished)
            {
                MessageBox.Show("请先停止当前任务（连续运行、预览等）后再切换推理模式。", "提示");
                return;
            }

            // 2. 如果正在实时预览，先关闭
            if (vpWin != null && vpWin._iTaskType == 1)
            {
                OpenOrCloseCam(false);
            }

            // 3. 卸载当前模型
            if (cup.p_VPPRTP.bVisionInfer)
            {
                // 从视觉推理切换出去：卸载 VPP 引擎
                if (vpWin != null)
                {
                    vpWin.UnloadVisionInfer();  // 需要实现此方法，释放 mJM
                }
                rts.initStatus.bVPPInit = false;


            }
            else if (cup.p_VPPRTP.bDeepInfer)
            {
                // 从深度推理切换出去：卸载深度学习模型
                cdl.CloseDL();
                rts.initStatus.bDLModelInit = false;
            }

            // 4. 更新配置
            cup.p_VPPRTP.bVisionInfer = bToVisionInfer;
            cup.p_VPPRTP.bDeepInfer = !bToVisionInfer;
            SaveUserParam();

            // 5. 加载新模型
            if (bToVisionInfer)
            {
                // 加载视觉推理模型

                toolStrip1.Enabled = false;
                splitContainer1.Panel1.Enabled = false;
                LoadVPFile(true);   // 确保 vpWin 存在并加载 VPP
                toolStrip1.Enabled = true;
                splitContainer1.Panel1.Enabled = true;
               
                /* if (vpWin == null || string.IsNullOrEmpty(vpWin.LoadedVppFilename))  // 不直接访问私有字段 mJM，改为检查已加载的 VPP 文件名
                 {

                 }
                 else
                 {
                     vpWin.ReLoadVisionControl(cup.p_Paf.strVPPFilePath);
                 }*/
                if (rts.initStatus.bVPPInit)
                {
                    InitVisionControl();
                    vpWin.SetVPInferModel(true, false);
                }
                else
                {
                    MessageBox.Show("视觉推理模型加载失败，请检查 VPP 文件路径！", "错误");
                }
                // UI 更新
                btFunSet.Visible = true;
                bLoadVPRes = false;
                menuTask_LoadRes.Checked = false;
                vpWin.bLoadVPRes = false;
            }
            else
            {
                // 加载深度推理模型
                if (vpWin == null)
                {
                    vpWin = new VisionControl.VisionControl();
                    splitContainer3.Panel1.Controls.Add(vpWin);
                    bVPWinAttached = true;
                    vpWin.InitBG();
                }
                // 如果深度学习未初始化则初始化
                if (!rts.initStatus.bDLModelInit)
                {
                    toolStrip1.Enabled = false;
                    splitContainer1.Panel1.Enabled = false;
                    InitDL();
                    toolStrip1.Enabled = true;
                    splitContainer1.Panel1.Enabled = true;
                }
                if (rts.initStatus.bDLModelInit)
                {
                    vpWin.SetVPInferModel(false, true);
                    btFunSet.Visible = true;
                    bLoadVPRes = false;
                    menuTask_LoadRes.Checked = false;
                    vpWin.bLoadVPRes = false;
                }
                else
                {
                    MessageBox.Show("深度推理模型加载失败，请检查模型路径！", "错误");
                }

                if (cup.p_VPPRTP.bVisionInfer)
                {
                    this.Text = ApplicationName + "   " + cup.p_Paf.strVPPFilePath;
                }
                else
                    this.Text = ApplicationName + "   " + cup.p_DL.modelPath;

            }

            // 6. 更新界面状态
            InitCheckStatus();
            // 更新菜单勾选（确保与 cup 一致）
            menuTask_VisionInfer.Checked = cup.p_VPPRTP.bVisionInfer;
            menuTask_DeepInfer.Checked = cup.p_VPPRTP.bDeepInfer;
            ViewLayout();
        }

      

        private void btSaveVPP_Click(object sender, EventArgs e)
        {
            if (bVPPParamChanged)
            {
                try
                {
                    int saveResult = vpWin.SaveVPP();
                    if (saveResult == 0)
                    {
                        operLog.iEventStatus = 0;
                        operLog.strOperEvent = "保存VPP文件";
                        operLog.strFailReason = $"已保存到 {cup.p_Paf.strVPPFilePath}";
                        AddLog();
                        bVPPParamChanged = false;   // 重置标志
                    }
                    else
                    {
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = "保存VPP文件";
                        operLog.strFailReason = $"保存失败，错误码：{saveResult}";
                        AddLog();
                    }
                }
                catch (Exception ex)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "保存VPP文件";
                    operLog.strFailReason = ex.Message;
                    AddLog();
                }
            }
        }
         

        private void timerIOIn_Tick(object sender, EventArgs e)
        {
            SetFaceTriggerIndicator(0, false);
            SetFaceTriggerIndicator(1, false);
            timerIOIn.Stop();
        }

       

        //Thread UpdateUIThread; 
        //RunOneTime()->   RunOneTimeThread()  ->vpWin.RunOneTime()
        //->LaserMarkAfterVisionDet()->UpdateUIExe()
        public void RunOneTimeThread()
        {
            int selectedJobIndex = iJobIndex;
            if (selectedJobIndex >= 0 && selectedJobIndex < maxCameraNum)
            {
                try
                {
                    rts.iIsRunning = 1;
                    RunOneTimeForFace(selectedJobIndex, true).GetAwaiter().GetResult();
                    rts.iIsRunning = 0;
                }
                catch (Exception ex)
                {
                    rts.iIsRunning = -1;
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = $"工位{selectedJobIndex + 1}运行一次";
                    operLog.strFailReason = ex.Message;
                    AddLog();
                }
                finally
                {
                    vpWin.bMainTaskFinished = true;
                    btFunRunOneTime.FlatStyle = FlatStyle.Popup;
                }

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => UpdateUIExeForJob(selectedJobIndex)));
                }
                else
                {
                    UpdateUIExeForJob(selectedJobIndex);
                }
                return;
            }

            try
            {
                int legacyJobIndex = iJobIndex;
                long legacyIOCycleToken;
                string legacyIOError;
                if (!TryBeginIOFeedbackRun(legacyJobIndex, true,
                    out legacyIOCycleToken, out legacyIOError))
                {
                    WriteIOFeedbackLog(-1, "单次运行IO批次拒绝", legacyIOError);
                    return;
                }
                rts.iIsRunning = 1;
                vpWin._bRunOneTimeFinished[legacyJobIndex] = 0;
                if (vpWin.RunOneTimeSplite(legacyJobIndex))
                {
                    int count = 0;
                    int maxTimes = cup.p_RTSP.runTimeOut / 10;

                    // 等待视觉检测完成
                    while (vpWin._bRunOneTimeFinished[legacyJobIndex] != 2 && count < maxTimes)
                    {
                        Thread.Sleep(10);
                        count++;
                    }

                    bool legacyRunCompleted = vpWin._bRunOneTimeFinished[legacyJobIndex] == 2;
                    vpWin._bRunOneTimeFinished[legacyJobIndex] = 2;  // 强制结束
                    pmaRes[legacyJobIndex] = vpWin._PMARes[legacyJobIndex];
                    if (legacyRunCompleted)
                        ExeIOOut(legacyJobIndex, legacyIOCycleToken);
                    else
                        AbortIOFeedbackCycle(legacyJobIndex, legacyIOCycleToken, "单次运行超时");

                    // 视觉检测完成后，判断是否需要激光标刻
                    if (rts.onLS.bLock && rts.initStatus.bJCZ == 0 && pmaRes[legacyJobIndex].bLocated)
                    {
                        // 异步执行激光标刻，不阻塞UI
                        Task.Run(async () =>
                        {
                            try
                            {
                                await LaserMarkAfterVisionDetAsync(legacyJobIndex);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"激光标刻异常: {ex.Message}");
                            }
                        });
                    }

                    vpWin.bMainTaskFinished = true;
                    rts.iIsRunning = 0;
                }
                else
                {
                    rts.iIsRunning = -1;
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "执行一次运行";
                    operLog.strFailReason = vpWin.strErrInfo[legacyJobIndex];
                    AddLog();
                    iErrExecuteTimes++;
                }
            }
            catch
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "启动运行";
                operLog.strFailReason = "执行运行失败";
                iErrExecuteTimes++;
            }

            vpWin.bMainTaskFinished = true;
            btFunRunOneTime.FlatStyle = FlatStyle.Popup;

            // 更新UI
            if (this.InvokeRequired)
            {
                int jobIndex = (iJobIndex >= 0 && iJobIndex < maxCameraNum) ? iJobIndex : 0;
                this.Invoke(new Action(() => UpdateUIExeForJob(jobIndex)));
            }
            else
            {
                int jobIndex = (iJobIndex >= 0 && iJobIndex < maxCameraNum) ? iJobIndex : 0;
                UpdateUIExeForJob(jobIndex);
            }
        }


        /// <summary>
        /// 单次运行线程（支持同步模式）
        /// </summary>
        /// <param name="syncMode">true: 同步执行所有启用工位；false: 仅执行当前选中的工位</param>
        private void RunOneTimeThread(bool syncMode)
        {
            // 1. 确定需要执行的工位列表
            List<int> jobIndices = new List<int>();
            if (syncMode)
            {
                // 同步模式：遍历所有启用的工位
                for (int i = 0; i < maxCameraNum; i++)
                {
                    if (cup.p_CD.bListProcess[i])
                        jobIndices.Add(i);
                }
            }
            else
            {
                // 单工位模式：只执行当前选中的工位（必须启用）
                if (iJobIndex >= 0 && iJobIndex < maxCameraNum && cup.p_CD.bListProcess[iJobIndex])
                    jobIndices.Add(iJobIndex);
                else
                {
                    // 若当前工位未启用，则不执行任何操作（或可改为执行第一个启用工位）
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "单次运行";
                    operLog.strFailReason = "当前工位未启用";
                    System.Threading.Interlocked.Exchange(ref manualSingleRunActive, 0);
                    vpWin.bMainTaskFinished = true;
                    AddLog();
                    return;
                }
            }

            if (jobIndices.Count == 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "单次运行";
                operLog.strFailReason = "没有启用的工位";
                System.Threading.Interlocked.Exchange(ref manualSingleRunActive, 0);
                vpWin.bMainTaskFinished = true;
                AddLog();
                return;
            }

            long activeIOCycleToken = 0;
            int activeIOJobIndex = -1;
            bool independentVisionJobs = syncMode && cup.p_VPPRTP.bVisionInfer;
            bool ioFeedbackCancelled = false;
            try
            {
                rts.iIsRunning = 1;
                vpWin.bMainTaskFinished = false;

                // 顺序执行每个工位（若需并行，可改为 Parallel.ForEach 或 Task 并行）
                foreach (int jobIndex in jobIndices)
                {
                    string ioFeedbackError;
                    if (!ioFeedbackCancelled && !TryBeginIOFeedbackRun(jobIndex, !syncMode,
                        out activeIOCycleToken, out ioFeedbackError))
                    {
                        WriteIOFeedbackLog(-1, "运行IO批次拒绝", ioFeedbackError);
                        if (!independentVisionJobs)
                            break;
                        ioFeedbackCancelled = true;
                        activeIOCycleToken = 0;
                    }
                    activeIOJobIndex = jobIndex;

                    // VPP入口先检查/恢复该工位busy，再初始化完成标志。
                    if (!cup.p_VPPRTP.bVisionInfer)
                        vpWin._bRunOneTimeFinished[jobIndex] = 0;

                    // 执行该工位的检测
                    if (!vpWin.RunOneTime(jobIndex))
                    {
                        // 启动失败，强制标记完成并记录错误
                        vpWin._bRunOneTimeFinished[jobIndex] = 2;
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = $"工位{jobIndex + 1}运行一次";
                        operLog.strFailReason = vpWin.strErrInfo?[jobIndex] ?? "启动失败";
                        AddLog();
                        AbortIOFeedbackCycle(jobIndex, activeIOCycleToken, "同步运行启动失败");
                        if (independentVisionJobs)
                            ioFeedbackCancelled = true;
                        if (syncMode && activeIOCycleToken > 0 && !independentVisionJobs)
                            break;
                        continue;
                    }

                    // 等待该工位完成（轮询直到 _bRunOneTimeFinished 变为 2 或超时）
                    int count = 0;
                    int maxTimes = cup.p_RTSP.runTimeOut / 5; // 假设 runTimeOut 单位为毫秒
                    bool jobRunFailed = false;
                    while (vpWin._bRunOneTimeFinished[jobIndex] != 2 && count < maxTimes)
                    {
                        Thread.Sleep(5);
                        count++;
                    }

                    if (vpWin._bRunOneTimeFinished[jobIndex] != 2)
                    {
                        // 超时处理：强制结束并记录
                        vpWin._bRunOneTimeFinished[jobIndex] = 2;
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = $"工位{jobIndex + 1}运行一次";
                        operLog.strFailReason = "视觉检测超时";
                        AddLog();
                        AbortIOFeedbackCycle(jobIndex, activeIOCycleToken, "同步运行超时");
                        if (independentVisionJobs)
                            ioFeedbackCancelled = true;
                        jobRunFailed = true;
                    }
                    else
                    {
                        // 正常完成，获取结果（假设 pmaRes 数组存储每个工位的 PMA 结果）
                        pmaRes[jobIndex] = vpWin._PMARes[jobIndex];
                        if (cup.p_VPPRTP.bVisionInfer && !vpWin.IsVppJobResultValid(jobIndex))
                        {
                            AbortIOFeedbackCycle(jobIndex, activeIOCycleToken, "本轮长度测量结果无效");
                            ioFeedbackCancelled = true;
                            jobRunFailed = true;
                        }
                        else if (!ioFeedbackCancelled)
                            ExeIOOut(jobIndex, activeIOCycleToken);
                    }

                    // 超时没有本轮有效结果，不能记录完成或累加统计。
                    if (jobRunFailed && cup.p_VPPRTP.bVisionInfer)
                        continue;

                    // 更新该工位的 UI（必须在主线程执行）
                    if (this.InvokeRequired)
                        this.Invoke(new Action(() => UpdateUIExeForJob(jobIndex)));
                    else
                        UpdateUIExeForJob(jobIndex);

                    if (jobRunFailed && syncMode && activeIOCycleToken > 0)
                        break;
                }

                rts.iIsRunning = 0;
                vpWin.bMainTaskFinished = true;
            }
            catch (Exception ex)
            {
                AbortIOFeedbackCycle(activeIOJobIndex, activeIOCycleToken, "单次运行异常");
                rts.iIsRunning = -1;
                vpWin.bMainTaskFinished = true;
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "单次运行异常";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
            finally
            {
                // 恢复按钮样式（在 UI 线程执行）
                System.Threading.Interlocked.Exchange(ref manualSingleRunActive, 0);
                if (btFunRunOneTime.InvokeRequired)
                    btFunRunOneTime.Invoke(new Action(() => btFunRunOneTime.FlatStyle = FlatStyle.Popup));
                else
                    btFunRunOneTime.FlatStyle = FlatStyle.Popup;
            }
        }
        private void UpdateUIExe()
        {
            int jobIndex = (iJobIndex >= 0 && iJobIndex < maxCameraNum) ? iJobIndex : 0;
            UpdateUIExeForJob(jobIndex);
        }

        private void UpdateUIExeForJob(int jobIndex)
        {
            if (jobIndex < 0 || jobIndex >= maxCameraNum)
                return;
            if (cup.p_VPPRTP.bVisionInfer && !vpWin.IsVppJobResultValid(jobIndex))
                return;

            // ★★★ 如果当前是静默运行，则跳过累加，重置标志并返回 ★★★
            if (_skipStatUpdateForCurrentRun)
            {
                _skipStatUpdateForCurrentRun = false;  // 一次性标志，用完即复位
                                                       // 此时不累加 rts.detS，不做 IO 反馈，仅显示图像（已在别处更新）
                return;
            }

            UpdateStatisticsForJob(jobIndex);  //更新统计数据
            if (cup.p_RTSP.bUpdateRecord)
            {                
               AnaEventForJob(jobIndex);
                //AddEventLog(iEventStatus);  //已取消颜色显示
            }
           /* if (cup.p_RTSP.bUpdateIO)  //启用IO输出
            {
                //只输出OK或者只输出NG
                if (pmaRes[jobIndex].bLocated)
                    ExeIOOut(jobIndex);
            }*/
        }
  
        public void RunContinuousThread()
        {
            int selectedJobIndex = iJobIndex;
            if (selectedJobIndex < 0 || selectedJobIndex >= maxCameraNum)
                return;

            while (vpWin._bContRun)
            {
                if (!cup.p_VPPRTP.bAcqFromCam && iFileGroupLen <= 0)
                {
                    vpWin._bContRun = false;
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "连续运行";
                    operLog.strFailReason = "回放图片未初始化或图片组数量为 0";
                    AddLog();
                    break;
                }

                long continuousIOCycleToken;
                string continuousIOError;
                if (!TryBeginIOFeedbackRun(selectedJobIndex, true,
                    out continuousIOCycleToken, out continuousIOError))
                {
                    WriteIOFeedbackLog(-1, "连续运行IO批次拒绝", continuousIOError);
                    break;
                }

                if (vpWin.RunOneTime(selectedJobIndex))
                {
                    int count = 0;
                    int maxTimes = cup.p_RTSP.runTimeOut / 5;
                    //2025年10月8日 此处条件不完全正确，待修改
                    while (/*vpWin.mCurrentRunState != 0*/vpWin._bRunOneTimeFinished[selectedJobIndex] != 2 && count < maxTimes)
                    {
                        Thread.Sleep(5);
                        count++;
                    }
                  /*  if(vpWin.mCurrentRunState!=0)
                    {

                    }*/
                    bool continuousRunCompleted = vpWin._bRunOneTimeFinished[selectedJobIndex] == 2;
                    vpWin._bRunOneTimeFinished[selectedJobIndex] = 2;  //强制结束
                    pmaRes[selectedJobIndex] = vpWin._PMARes[selectedJobIndex];
                    if (continuousRunCompleted)
                        ExeIOOut(selectedJobIndex, continuousIOCycleToken);
                    else
                        AbortIOFeedbackCycle(selectedJobIndex, continuousIOCycleToken, "连续运行超时");
                    if(!pmaRes[selectedJobIndex].bOk)
                    {
                      //  vpWin._bContRun = false;
                    }
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => UpdateUIExeForJob(selectedJobIndex)));
                        //    Invoke(new EventHandler(ClearHistoryQueues), new object[] { sender, e });
                    }
                    else
                    {
                        UpdateUIExeForJob(selectedJobIndex);
                    }
                    /*if(!cup.p_VPPRTP.bAcqFromCam && !cup.p_VPPRTP.bAcqFromIDB)
                     {
                         iFileIndex++;
                         iNextFileIndex++;
                     }*/

                    if (!cup.p_VPPRTP.bAcqFromIDB && !cup.p_VPPRTP.bAcqFromCam && !toolBtSta_LockImage.Checked)
                    {
                        iFileIndex = iNextFileIndex;
                        iFileIndex = iFileIndex % iFileGroupLen;
                        vpWin.iFileIndex = iFileIndex;
                        iNextFileIndex = iFileIndex;
                        iNextFileIndex = (++iNextFileIndex) % iFileGroupLen;
                    }
                    Thread.Sleep(2);
                }
                else
                {
                    AbortIOFeedbackCycle(selectedJobIndex, continuousIOCycleToken, "连续运行启动失败");
                }
            }
            vpWin._iTaskType = -1;
            //  operLog.strOperEvent = "关闭连续运行";
            //  ((Button)sender).FlatStyle = FlatStyle.Flat;
            if (btFunRunCont.InvokeRequired)
            {
                btFunRunCont.Invoke(new Action(() =>
                { 
                    btFunRunCont.Image = global::VisionApplication.Properties.Resources.LoopRun2;
                    btFunRunCont.FlatStyle = FlatStyle.Popup;
                    btFunRunCont.Enabled = true;

                }));
            }
            else
            { 
                btFunRunCont.Image = global::VisionApplication.Properties.Resources.LoopRun2;
                btFunRunCont.FlatStyle = FlatStyle.Popup;
                btFunRunCont.Enabled = true; 
            }
            

            vpWin.bMainTaskFinished = true;
            operLog.iEventStatus = 0;
           
        }
        #endregion


     
 

    }

}
