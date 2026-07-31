 
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

using Cognex.VisionPro.Blob;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.PMAlign;
using MvCamCtrl.NET;  // 海康相机

using CommonCls;
using PipeChanel;

namespace VisionApplication
{


    public partial class FrmMainUI : Form  
    {
        #region       
        // 1. 常量
        private const int maxCameraNum = 2;                     // 最大支持的相机数量（工位数量）

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
        private string[] strPortName;                           // 系统可用串口名称数组
        private List<int> comInd = new List<int>();              // 排序后的串口COM号列表

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

        #endregion

        // 14. 构造函数
        public FrmMainUI()
        {            
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent(); 
            InitUserParam(); 
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
                 default:
                    strErr = "未知原因";
                    break;
            }
            return strErr;
        }
        
        private void LoadVPFile(bool bReLoad)  //2025年10月4日  修改
        {
            string strVPPFile = "";
           //判断算法文件是否存在
            if (File.Exists(cup.p_Paf.strVPPFilePath))
            {
                strVPPFile = cup.p_Paf.strVPPFilePath;
                rts.initStatus.bVPPFileExist = true;
            }
            else //在VPP文件中查找
            {
                Path.GetFileName(cup.p_Paf.strVPPFilePath);
                if (File.Exists(@".\VPP\"+ Path.GetFileName(cup.p_Paf.strVPPFilePath)))
                {
                    strVPPFile = @".\VPP\" + Path.GetFileName(cup.p_Paf.strVPPFilePath);
                    rts.initStatus.bVPPFileExist = true;
                }
                else if(File.Exists(@".\" + Path.GetFileName(cup.p_Paf.strVPPFilePath)))
                {
                    strVPPFile = @".\VPP\" + Path.GetFileName(cup.p_Paf.strVPPFilePath);
                    rts.initStatus.bVPPFileExist = true;
                }
                else
                    rts.initStatus.bVPPFileExist = false;
            }

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
                        operLog.strOperEvent = "加载视觉算法文件";
                        operLog.iEventStatus = -1;
                        operLog.strFailReason =  VPPinitErr(rts.initStatus.iVPPStatus);
                        AddLog();
                    }
                    strEngineerName = Path.GetFileName(cup.p_DL.modelPath);
                    toolStripStatus_Engineer.Text = "机种:" + strEngineerName;
                    this.Text = "镭射除胶   " +cup.p_DL.modelPath;

                }
                catch
                {
                    rts.initStatus.bVPPInit = false;
                    operLog.strOperEvent = "加载视觉算法文件";
                    operLog.iEventStatus = -1;
                    operLog.strFailReason =  VPPinitErr(rts.initStatus.iVPPStatus);
                    AddLog();
                }

            }
            else    //**2023年7月30日  //文件不存在
            {

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
                vpWin._cup = cup;
                vpWin.SetGUIShow(0);
                vpWin._cameraNum = cameraNum;
                vpWin.SetAcqFIFO(acqFIFO);
                vpWin.JobResultReady -= OnJobResultReady;
                vpWin.JobResultReady += OnJobResultReady;
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
            }
            catch
            {
                operLog.iEventStatus = -1;
            }
           
        }
        // 事件处理方法（直接打日志）
        private void OnVisionErrorFromVP(int jobIndex, string errorMessage)
        {
            // AddLog 内部已处理跨线程 Invoke，可直接调用
            operLog.iEventStatus = -1;
            operLog.strOperEvent = $"视觉工位{jobIndex + 1}异常";
            operLog.strFailReason = errorMessage;
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
        private void InitDL()
        {
            int iRet = -1;
            operLog.strOperEvent = "加载深度学习模型";
            if (Directory.Exists(cup.p_DL.modelPath))
            {
                rts.initStatus.bDLModelPathExist = true;
                try
                {
                   iRet = cdl.InitDL(cup.p_DL);
                    if (iRet == -1)
                    {
                        rts.initStatus.bDLModelInit = false;
                        cup.p_VPPRTP.bDeepInfer = false;
                        menuTask_Predict.Checked = false;
                        operLog.strFailReason = "深度推理模型初始化异常";
                    }
                    else
                    {
                        rts.initStatus.bDLModelInit = true;
                    }
                }
                catch
                {
                    cup.p_VPPRTP.bDeepInfer = false;
                    menuTask_Predict.Checked = false;
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "深度推理模型初始化异常";

                }

            }
            else
            {
                cup.p_VPPRTP.bDeepInfer = false;
                rts.initStatus.bDLModelPathExist = false;
                menuTask_Predict.Checked = false;
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
        }
        //工程拷贝后可能存在盘符不一致情况，修改路径盘符
        private void ReplaceDrive()
        {
            string tmppath;
            string curDriver = Environment.CurrentDirectory.Substring(0, 1);
            if (!File.Exists(cup.p_Paf.strVPPFilePath))
            {
                tmppath = curDriver + cup.p_Paf.strVPPFilePath.Substring(1);
                if (File.Exists(tmppath))
                {
                    cup.p_Paf.strVPPFilePath = tmppath;
                }
                tmppath = curDriver + cup.p_Paf.strConSavePath.Substring(1);
                if (File.Exists(tmppath))
                {
                    cup.p_Paf.strConSavePath = tmppath;
                }
                try { tmppath = curDriver + cup.p_Paf.strErrPath.Substring(1); }
                catch { };
                
                if (File.Exists(tmppath))
                {
                    cup.p_Paf.strErrPath = tmppath;
                }
                try { tmppath = curDriver + cup.p_Paf.strExportPath.Substring(1); }
                catch { }  ;
             
                if (File.Exists(tmppath))
                {
                    cup.p_Paf.strExportPath = tmppath;
                }
                tmppath = curDriver + cup.p_Paf.strLogPath.Substring(1);
                if (File.Exists(tmppath))
                {
                    cup.p_Paf.strLogPath = tmppath;
                }
                tmppath = curDriver + cup.p_Paf.strPlaybackPath.Substring(1);
                if (File.Exists(tmppath))
                {
                    cup.p_Paf.strPlaybackPath = tmppath;
                }
                tmppath = curDriver + cup.p_Paf.strResSavePath.Substring(1);
                if (File.Exists(tmppath))
                {
                    cup.p_Paf.strResSavePath = tmppath;
                }
                tmppath = curDriver + cup.p_Paf.strSigSavePath.Substring(1);
                if (File.Exists(tmppath))
                {
                    cup.p_Paf.strSigSavePath = tmppath;
                }
                SaveUserParam();
            }
        }
        //初始化用户参数
        private void InitUserParam()
        {
            FileOper.MigrateOldBinaryIfNeeded();
            ReadUserParam();

            //初始化运行时状态，开机可用
            {
                cup.p_VPPRTP.bAcqFromCam = true;
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
            rts.initStatus.bUserParamFileInit = bUserParamInit;
            detStat = rts.detS;
            

        }
    

        // 验证相机是否都处于就位状态
        private void CheckCamPrepared()
        {
            rts.initStatus.bCamPrepared = true;
            for (int i = 0; i < maxCameraNum; i++)
            {
                if (cup.p_CD.bListProcess[i] && rts.cs.iStatus[cup.p_CD.camInd[i]] < 3)
                {
                    rts.initStatus.bCamPrepared = false;
                }
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
            bool bRet = true;
            operLog.strOperEvent = "回放数据检查";
            if (!Directory.Exists(cup.p_Paf.strPlaybackPath))
            {
                rts.initStatus.bLocalFileExist = false;
                bRet = false;
                operLog.iEventStatus = -1;
                operLog.strFailReason = "回放数据目录不存在";
            }
            else
            {
                try
                {
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (cup.p_CD.bListProcess[i])
                        {
                            string strPathDir = cup.p_Paf.strPlaybackPath + cup.p_LI.strCamPrefix[i];
                            if (cup.p_Paf.strPlaybackPath.Substring(cup.p_Paf.strPlaybackPath.Length - 1, 1) != "\\")
                            {
                                strPathDir = cup.p_Paf.strPlaybackPath + "\\" + cup.p_LI.strCamPrefix[i];
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
                                    if (i == 0)
                                    {
                                        len = strFiles[i].Length / iPerGroupLen;
                                    }
                                    acqFIFO.cmAcqNum = 1;

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
                                /* if( strFiles[i].Length%6 !=0 || strFiles[i].Length / 6!=len)
                                  {
                                      rts.initStatus.bLocalFileRegular = false;
                                      bRet = false;
                                      operLog.strFailReason = "子目录图像文件数量异常";
                                     // operLog.strOperEvent = "回放数据图像文件检查";
                                  }*/
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
                rts.initStatus.bLocalFileExist = true;
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
                            if (strLivePath.Substring(cup.p_Paf.strPlaybackPath.Length - 1, 1) != "\\")
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
                    // ... 其他 case 保持不变 ...
            }

            // ✅ 更新相机列表信息（只显示启用的相机）
            cbDeviceList.Items.Clear();
            for (int i = 0; i < maxCameraNum; i++)
            {
                // 只有当任务启用且相机索引有效时才添加到列表
                if (cup.p_CD.bListProcess[i] && cup.p_CD.camInd[i] < cameraNum && items.Count > cup.p_CD.camInd[i])
                {
                    cbDeviceList.Items.Add(items[cup.p_CD.camInd[i]]);
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
                if (iCamInd < cameraNum)
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
        private bool InitCamInd(int ind)
        {
            bool bSucc = false;
            int iCamInd = cup.p_CD.camInd[ind];          // 物理相机索引


            // 在 InitCamInd 开头增加
            if (camera2[iCamInd] == null)
                return false;

            // 先尝试获取一个简单参数（如设备型号），若失败则视为离线
            MyCamera.MVCC_STRINGVALUE stStr = new MyCamera.MVCC_STRINGVALUE();
            int ret = camera2[iCamInd].MV_CC_GetStringValue_NET("DeviceModelName", ref stStr);
            if (ret != MyCamera.MV_OK)
            {
                // 相机可能已断开，清理资源后返回
                camera2[iCamInd] = null;
                SyncCameraStatus(iCamInd, 0);
                return false;
            }

            // 1. 检查相机名称是否发生变化，若变化则记录日志
            if (cup.p_Cam[iCamInd].strCamName != items[iCamInd])
            {
                cup.p_Cam[iCamInd].strCamName = items[iCamInd];
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "相机" + (ind + 1).ToString() + "发生更换";
                operLog.strFailReason = cup.p_Cam[iCamInd].strCamName + "->" + items[iCamInd];
                AddLog();
            }

            // 2. 重新初始化相机对象（如果之前已存在则先销毁）
            camera2 = hIKVisionCamera.CameraInit(iCamInd, ref rts.cs.iStatus);
            if (camera2[iCamInd] == null)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "相机初始化失败";
                operLog.strFailReason = "异常错误";
                AddLog();

                // 更新状态
                SyncCameraStatus(iCamInd, 0);
                return false;
            }

            // 3. 获取图像数据载荷大小（PayloadSize）
            MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
            uint nPayloadSize = 0;
            if (camera2[iCamInd].MV_CC_GetIntValue_NET("PayloadSize", ref stParam) != 0)
            {
                // 部分相机无法直接获取 PayloadSize，采用抓一帧的方式
                MyCamera.MV_FRAME_OUT stFrameInfo1 = new MyCamera.MV_FRAME_OUT();
                int nRet = camera2[iCamInd].MV_CC_GetImageBuffer_NET(ref stFrameInfo1, 2000);
                if (nRet == MyCamera.MV_OK)
                {
                    nPayloadSize = stFrameInfo1.stFrameInfo.nFrameLen;
                }
                else
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "检测位" + (ind + 1).ToString() + "获取相机数据量失败";
                    operLog.strFailReason = "异常错误";
                    AddLog();

                    SyncCameraStatus(iCamInd, 0);
                    return false;
                }
            }
            else
            {
                nPayloadSize = stParam.nCurValue;
            }

            // 临时更新相机状态为“已获取载荷”
            SyncCameraStatus(iCamInd, 1);

            // 4. 设置相机参数（曝光、增益、ROI 等）
            if (!SetCamParam(iCamInd))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "设置相机参数";
                operLog.strFailReason = "异常错误";
                AddLog();

                SyncCameraStatus(iCamInd, 0);
                return false;
            }
            SyncCameraStatus(iCamInd, 2);

            // 5. 分配/调整图像缓冲区（由 AcqFIFO 统一管理）
            if (!acqFIFO.ReallocateBuffer(iCamInd, nPayloadSize))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "分配数据空间";
                operLog.strFailReason = "AcqFIFO 分配内存失败";
                AddLog();

                SyncCameraStatus(iCamInd, 0);
                return false;
            }

            // 分配成功，更新相机状态为就绪
            SyncCameraStatus(iCamInd, 3);
            bSucc = true;

            return bSucc;
        }
       
        private void InitCam()
        {
            int iInitType = 0; // 0:正常初始化 -1:没有可用相机
            bool bNeedCleanup = false;
            bool bStatusChanged = false;

            if (cameraNum < 1)
            {
                iInitType = -1;
                goto outofInit;
            }

            try
            {
                // 1. 枚举相机名称
                items = hIKVisionCamera.EnumDevices();

                // 2. 检查每个工位配置的相机编号是否有效，无效则禁用
                bool bAnyProcessEnabled = false;
                for (int i = 0; i < maxCameraNum; i++)
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
                    if (camera2[iCamInd] == null)
                    {
                     //   cup.p_CD.bListProcess[ind] = false;
                        bStatusChanged = true;
                        continue;
                    }

                    // 5. 一次性设置所有相机参数（包括 ROI、分辨率、曝光、增益、锐度、伽马等）
                    if (!SetCamParam(iCamInd))
                    {
                    //    cup.p_CD.bListProcess[ind] = false;
                        bStatusChanged = true;
                        continue;
                    }
                    
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
                // 可添加日志记录异常信息
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
        private void StopDevice()
        {
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
                    hIKVisionCamera.MultiDestroyCamera();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"关闭相机失败：{ex.Message}，请重启程序！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // 关闭 Basler 相机（如有，可在此处添加对应代码）
            // ...

            // 释放 AcqFIFO 管理的所有图像缓冲区（非托管内存）
            if (acqFIFO != null)
            {
                acqFIFO.FreeAllBuffers();
            }
        }
        //停止采集
        private void StopGrab()
        {
            if (chooseHIK)
            {
                try
                {
                    hikCanGrab = false;   // 标志位设为false
                                          //   vpWin.OpenOrCloseCam(false);  //停止预览线程，并未关闭相机的采集
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        vpWin.previewOnlive(false, i);
                    }

                  //  vpWin.previewOnlive(false, iJobIndex);
                    //    hIKVisionCamera.StopGrabbing();
                }
                catch
                {
                    MessageBox.Show("停止采集失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

        }

        private void CloseCamStatus()
        {
            for (int i = 0; i < rts.cs.iStatus.Length; i++)
                rts.cs.iStatus[i] = 0;         
        }
        private void CloseCam()
        {
            StopGrab();
            StopDevice();
            CloseCamStatus();
        }
 
        private bool SetCamParam(int iCamIndex)
        {
            // 1. 参数有效性检查
            if (iCamIndex < 0 || iCamIndex >= maxCameraNum)
                return false;
            if (!chooseHIK || camera2 == null || camera2[iCamIndex] == null)
                return false;

            try
            {
                // 2. 设置曝光和增益
                float exposure = cup.p_Cam[iCamIndex].iExposeTime;
                float gain = (float)cup.p_Cam[iCamIndex].dGain;
                string gainshow = null;
                string exposureshow = null;
                hIKVisionCamera.SetParam(gain, exposure, ref gainshow, ref exposureshow, camera2[iCamIndex]);

                // 3. 设置像素格式
                if (!hIKVisionCamera.SetPixelFormat(ref cup.p_Cam[iCamIndex].iMonoType, camera2[iCamIndex]))
                    return false;

                // 4. 设置锐度
                if (!hIKVisionCamera.SetSharpness(ref cup.p_Cam[iCamIndex].iSharp, camera2[iCamIndex]))
                    return false;

                // 5. 设置伽马（先确保伽马使能，如果界面有开关则根据状态设置，这里仅设置值）
                float gamma = cup.p_Cam[iCamIndex].dGamma;
                if (!hIKVisionCamera.SetGamma(ref gamma, camera2[iCamIndex]))
                    return false;
                cup.p_Cam[iCamIndex].dGamma = gamma; // 同步可能被修正的值

                // 6. 设置分辨率（作为基准）
                uint iWidth = cup.p_Cam[iCamIndex].iWidth;
                uint iHeight = cup.p_Cam[iCamIndex].iHeight;
                if (!hIKVisionCamera.SetResolution(ref iWidth, ref iHeight, camera2[iCamIndex]))
                    return false;

                // 7. 处理 ROI（确保数组有效）
                uint[] roi = cup.p_Cam[iCamIndex].iROI;
                if (roi == null || roi.Length < 4)
                {
                    roi = new uint[] { 0, 0, iWidth, iHeight };
                    cup.p_Cam[iCamIndex].iROI = roi;
                }
                // 先设置宽高，再设置偏移（顺序重要）
                int ret = camera2[iCamIndex].MV_CC_SetIntValue_NET("Width", roi[2]);
                if (ret != MyCamera.MV_OK) return false;
                ret = camera2[iCamIndex].MV_CC_SetIntValue_NET("Height", roi[3]);
                if (ret != MyCamera.MV_OK) return false;
                ret = camera2[iCamIndex].MV_CC_SetIntValue_NET("OffsetX", roi[0]);
                if (ret != MyCamera.MV_OK) return false;
                ret = camera2[iCamIndex].MV_CC_SetIntValue_NET("OffsetY", roi[1]);
                if (ret != MyCamera.MV_OK) return false;

                // 更新分辨率参数为 ROI 的实际尺寸
                cup.p_Cam[iCamIndex].iWidth = roi[2];
                cup.p_Cam[iCamIndex].iHeight = roi[3];

                // 8. 如果当前工位是触发相机，设置触发模式
                if (cup.p_CD.triggleCam == iCamIndex)
                {
                    SetTrigger(iCamIndex);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool SetTrigger(int iCamIndex)
        {
            if (camera2[iCamIndex] == null)
                return false;
            try
            {
                if (cup.p_CD.bTriggle)
                {
                    camera2[iCamIndex].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);
                    if (cup.p_CD.triggleMode == 4)
                        camera2[iCamIndex].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                    else
                    {
                        camera2[iCamIndex].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                    }
                }
                else
                {
                    camera2[iCamIndex].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void StartGrab(int iCamInd)
        {
            if (hIKVisionCamera.StartGrabbing(iCamInd))
            {
                rts.cs.iStatus[iCamInd] = 3;    //相机开启采集
                /*
                                operLog.strOperEvent = items[iCamInd] + "-" + (iCamInd + 1) + "启动采集";
                                operLog.iEventStatus = 0;*/
            }
            else
            {
                // strError = items[iCamInd] + "-" + (iCamInd + 1) + "启动采集失败";
                /*  operLog.strOperEvent = items[iCamInd] + "-" + (iCamInd + 1) + "启动采集";
                  operLog.iEventStatus = -1;*/
            }
            //   AddLog();
        }
        private void StartGrab()
        {
            //   string strError;
            for (int iCamInd = 0; iCamInd < cameraNum; iCamInd++)
            {
                StartGrab(iCamInd);
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
        private bool OpenOrCloseCam(bool bOpen)
        {
            if (bOpen)
            {
                // 用线程更新显示
                if (chooseHIK)
                {
                    operLog.strOperEvent = "启动图像预览";
                    try
                    {
                        // 用线程更新显示

                        // vpWin.OpenOrCloseCam(true);
                        for (int i = 0; i < maxCameraNum; i++)
                        {
                            if (!cup.p_CD.bListProcess[i]) continue;
                            vpWin.previewOnlive(true, i);
                        }
                        hikCanGrab = true;
                        operLog.iEventStatus = 0;
                        menuCam_Connect.Text = "关闭图像预览";

                    }
                    catch
                    {
                        hikCanGrab = false;
                        //     hikGrabThread.Join();
                        // MessageBox.Show("开始采集失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        menuCam_Connect.Text = "启动图像预览";
                        operLog.iEventStatus = -1;
                        operLog.strFailReason = "启动预览线程失败";
                        AddLog();
                        return false;
                    }

                    AddLog();

                }
            }
            else
            {
                operLog.strOperEvent = "关闭图像预览";
                try
                {
                    StopGrab();
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

        private void AnaEventForJob(int jobIndex)
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

                       /* operLog.iEventStatus = 0;
                        operLog.strOperEvent = "自动保存失败图像";
                        operLog.strFailReason = $"{faceName} 图像已保存";
                        AddLog();*/
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
        }
       
        //2025年10月7日  执行一次运算输出IO信号
        private async void ExeIOOut(int jobIndex) 
        {
            
            // 发送 IO 信号（假设此方法不会阻塞）
            if (csd.SendResultToIO(sPortCy, jobIndex == 0, cup.p_IOPP))
            {
                // 1. 变红
                if (jobIndex == 0)
                    lblOut1.ForeColor = Color.Red;
                else if (jobIndex == 1)
                    lblOut2.ForeColor = Color.Red;

                // 2. 异步等待 1 秒，不阻塞 UI
                await Task.Delay(1000);

                // 3. 变绿
                if (jobIndex == 0)
                    lblOut1.ForeColor = Color.Green;
                else if (jobIndex == 1)
                    lblOut2.ForeColor = Color.Green;
            }
        }
        private void ExeIOOut()
        {
            switch (rts.onLS.iFeedbackType)                  //启用了结果自动反馈
            {
                case 1:
                   
                    if(csd.SendResultToIO(sPortCy, pmaRes[currentCardSN].bOk, cup.p_IOPP))
                    {
                        if (pmaRes[currentCardSN].bOk)
                            lblOut1.ForeColor = Color.Red;
                        else
                            lblOut2.ForeColor = Color.Red;
                        if (!timerIOOut.Enabled)
                            timerIOOut.Start();
                    }
                    break;
                case 2:
                    //固定反馈OK
                    
                    if(csd.SendResultToIO(sPortCy, true, cup.p_IOPP))
                    {
                        lblOut1.ForeColor = Color.Red;
                        if (!timerIOOut.Enabled)
                            timerIOOut.Start();
                    }
                     
                    break;
                case 3:
                    
                    if(csd.SendResultToIO(sPortCy, false, cup.p_IOPP)) //固定反馈NG 
                    {
                        lblOut2.ForeColor = Color.Red;
                        if (!timerIOOut.Enabled)
                            timerIOOut.Start();
                    }
                    break; 
                case 0:         //不反馈，等待手动
                    break; 
            }
            if (!timerIOOut.Enabled)
                timerIOOut.Start();
        }
        private void menuTask_Clear_Click(object sender, EventArgs e)
        {
            strOrder = "清除";
            csd.ClearOutput(sPortCy);
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
                        dgvLog.Rows[ind].Cells[2].Value = "执行失败";
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
            if (strMAC == strEnableMAC || strMAC == "207BD2D2CA3E" || strMAC == "00E04C782532" || strMAC == "00FFCF3A6322" || strMAC == "8C32233F2B85" || strMAC == "00E269A99B04")  //E4:5E:37:62:A1:3D   00FFCF3A6322  
            {
                rts.initStatus.bModelRoleCert = true;

                DateVerify(iModelRole);
                if (rts.iModelRole == 0)
                {
                    if (strMAC == "00E04C7825321" /*|| strMAC == "207BD2D2CA3E" || strMAC == "00FFCF3A6322" || strMAC == "8C32233F2B85"*/)   //调试专用功能
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
                fw.SetProgress(20, "正在加载机器视觉环境");
                LoadVPFile(false);            //VP控件初始化，加载VPP文件和VisionControl控件

                iProcess = 30;
                fw.SetProgress(30, "正在初始化视图");
               


                iProcess = 40;
                fw.SetProgress(40, "正在加载硬件驱动");

               
                EnsureJCZServerRunning();          // 确保 JCZServer 进程正常
                jczPipThread = new Thread(InitJCZ);
                jczPipThread.IsBackground = true;
                jczPipThread.Start();


                iProcess = 50;
                fw.SetProgress(50, "正在搜索并初始化相机");
                SearchCam();   // 搜搜相机数量                //
                InitCam();      //

                iProcess = 60;
                fw.SetProgress(60, "正在搜索并初始化串口");
                InitSerial();                                           //串口初始化
                InitTimer(); //IO口状态刷新                                                  //    fw.SetProgress(90, "正在初始化算法");

                if (cup.p_VPPRTP.bDeepInfer || rts.initStatus.bVPPInit)
                {
                    InitVisionControl();
                    //2025年8月26日
                    alImParam[0] = new AlignImParam();
                    alImParam[0] = new AlignImParam();
                    vpWin.GetAlignRegion(ref alImParam[0], 0);
                    alImParam[1] = new AlignImParam();
                    vpWin.GetAlignRegion(ref alImParam[1], 1);
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

            CloseCam();
            SearchCam();     //
            InitCam();
            StartGrab();
            InitVisionControl();
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

            CloseCam();
            SearchCam();             //
            InitCam();
            StartGrab();
             InitVisionControl();
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
            CloseCam();
            rts.initStatus.bCamInit = false;
            operLog.strOperEvent = "断开相机";
            operLog.iEventStatus = 0;
            AddLog();
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
            InitSubRecordsList();
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
                menuTask_Predict.Checked = true;
            else
                menuTask_Predict.Checked = false;
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
         
        private void menuTask_OK_Click(object sender, EventArgs e)
        {
            operLog.strOperEvent = "OK反馈";
            strOrder = "Ok";
            //  csd.SendResult(sPortCy, true);
            if (csd.SendResultToIO(sPortCy, true, cup.p_IOPP))
            {
                lblOut1.ForeColor = Color.Red;
                if(!timerIOOut.Enabled)
                    timerIOOut.Start();
                operLog.iEventStatus = 0;
            }
            else
            {
                operLog.iEventStatus = -1;
            }

        }

        private void menuTask_FeedBack_Click(object sender, EventArgs e)
        {

        }

        //检测反馈NG
        private void menuTask_NG_Click(object sender, EventArgs e)
        {
            operLog.strOperEvent = "NG反馈";
            strOrder = "NG凹口";
            if (csd.SendResultToIO(sPortCy, false, cup.p_IOPP))
            {
                lblOut2.ForeColor = Color.Red;
                if (!timerIOOut.Enabled)
                    timerIOOut.Start();
                operLog.iEventStatus = 0;
            }
            else
            {
                operLog.iEventStatus = -1;
            }
        }
        private void menuTask_NG2_Click(object sender, EventArgs e)
        {
            operLog.strOperEvent = "NG反馈";
            strOrder = "NG平口";
            if (csd.SendResultToIO(sPortCy, false, cup.p_IOPP))
            {
                operLog.iEventStatus = 0;
            }
            else
            {
                operLog.iEventStatus = -1;
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
        /// 光源控制（根据当前选中的工位控制对应的光源）
        /// </summary>
        /// <summary>
        /// 光源控制（根据当前选中的工位控制对应的光源）
        /// </summary>
        private void LightControlTurnOn()
        {
            // 检查相机是否已选择
            if (icamSelectInd < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "光源控制";
                operLog.strFailReason = "未选择相机";
                AddLog();
               // return;
            }

            bool open = !toolBtSta_Triggle.Checked;
            int brightness = open ? tbBirghtness.Value : 0;
            ApplyLightBrightness(iJobIndex, null, brightness);
            toolBtSta_Triggle.Checked = open;
            toolBtSta_Triggle.Image = open ? global::VisionApplication.Properties.Resources.LightOn
                                            : global::VisionApplication.Properties.Resources.LightOff;
            
            /*bool bSucc;
            int targetBrightness;

            // 根据当前按钮状态决定开关
            if (!toolBtSta_Triggle.Checked)
            {
                // 打开光源：使用配置的亮度值
                targetBrightness = GetLightBrightnessForCurrentJob();
                bSucc = csd.SetLightBrightnessByJob(iJobIndex, targetBrightness);

                if (bSucc)
                {
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = $"光源控制-打开(工位{iJobIndex + 1})";
                    operLog.strFailReason = $"亮度:{targetBrightness}";
                    AddLog();
                }
            }
            else
            {
                // 关闭光源：亮度设为0
                bSucc = csd.SetLightBrightnessByJob(iJobIndex, 0);

                if (bSucc)
                {
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = $"光源控制-关闭(工位{iJobIndex + 1})";
                    AddLog();
                }
            }

            if (bSucc)
            {
                // 切换按钮状态
                toolBtSta_Triggle.Checked = !toolBtSta_Triggle.Checked;

                if (toolBtSta_Triggle.Checked)
                {
                    toolBtSta_Triggle.Image = global::VisionApplication.Properties.Resources.LightOn;
                    toolBtSta_Triggle.ToolTipText = "光源开启";
                }
                else
                {
                    toolBtSta_Triggle.Image = global::VisionApplication.Properties.Resources.LightOff;
                    toolBtSta_Triggle.ToolTipText = "光源关闭";
                }
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "光源控制";
                operLog.strFailReason = "串口通信失败";
                AddLog();
            }*/
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
                        if (!acqFIFO.IsCamConn(iCamIndex))
                            rts.cs.iStatus[i] = 0;
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
            for (int i = 0; i < maxCameraNum; i++)
            {
                int iCamInd = cup.p_CD.camInd[i];
                // 在 TipInfo 中，发现相机状态异常时
                if (cup.p_VPPRTP.bAcqFromCam && cup.p_CD.bListProcess[i] && rts.cs.iStatus[iCamInd] < 2)
                {
                    // 如果尚未在重连且距离上次重连尝试超过间隔
                    if (!_isReconnecting && (DateTime.Now - _lastReconnectAttempt) > _reconnectInterval)
                    {
                        _lastReconnectAttempt = DateTime.Now;
                        // 启动后台线程尝试重连
                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            _isReconnecting = true;
                            try
                            {
                                int index = (int)state;
                                // 重新枚举相机（可选）
                                // 调用 InitCamInd 或更轻量的重连方法
                                bool success = InitCamInd(index);
                                if (success)
                                {
                                    // 重连成功，恢复采集
                                    StartGrab(cup.p_CD.camInd[index]);
                                    // 更新状态栏颜色等
                                }
                            }
                            catch (Exception ex)
                            {
                                // 记录日志，但不影响主线程
                                operLog.iEventStatus = -1;
                                operLog.strOperEvent = "相机后台重连";
                                operLog.strFailReason = ex.Message;
                                AddLog();
                            }
                            finally
                            {
                                _isReconnecting = false;
                            }
                        }, i);
                    }
                }

                if (rts.cs.iStatus[iCamInd] >= 2)
                {
                    iCamSuccNum++;
                    switch (i)
                    {
                        case 0:
                            toolStripStatus_Cam1.ForeColor = Color.Green;
                            break;
                        case 1:
                            toolStripStatus_Cam2.ForeColor = Color.Green;
                            break;
                        case 2:
                            toolStripStatus_Cam3.ForeColor = Color.Green;
                            break;
                        case 3:
                            toolStripStatus_Cam4.ForeColor = Color.Green;
                            break;
                        case 4:
                            toolStripStatus_Cam5.ForeColor = Color.Green;
                            break;
                    }

                }
                else if (!cup.p_CD.bListProcess[i])
                {
                    switch (i)
                    {
                        case 0:
                            toolStripStatus_Cam1.ForeColor = Color.Gray;
                            break;
                        case 1:
                            toolStripStatus_Cam2.ForeColor = Color.Gray;
                            break;
                        case 2:
                            toolStripStatus_Cam3.ForeColor = Color.Gray;
                            break;
                        case 3:
                            toolStripStatus_Cam4.ForeColor = Color.Gray;
                            break;
                        case 4:
                            toolStripStatus_Cam5.ForeColor = Color.Gray;
                            break;
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
                    if (strPortName == null || strPortName.Length < i + 1)
                    {
                        strPortName = SerialPort.GetPortNames();
                    }
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
                if (strPortName == null || strPortName.Length < 4)
                {
                    strPortName = SerialPort.GetPortNames();
                }
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
                rts.iModelRole = 9;
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
        //是否启用深度推理
        private void menuTask_Predict_Click(object sender, EventArgs e)
        {
            if (!menuTask_Predict.Checked && !rts.initStatus.bDLModelInit)  //视图开启深度学习，但目前深度学习还未初始化
            {
                if (MessageBox.Show("需要进行深度推理初始化吗", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    toolStrip1.Enabled = false;
                    splitContainer1.Panel1.Enabled = false;
                    InitDL();
                    toolStrip1.Enabled = true;
                    splitContainer1.Panel1.Enabled = true;
                }
            }
            menuTask_Predict.Checked = !menuTask_Predict.Checked;
            cup.p_VPPRTP.bDeepInfer = menuTask_Predict.Checked;
            if (cup.p_VPPRTP.bDeepInfer)  //如果启用了深度推理，则不加载历史结果
            {
                bLoadVPRes = false;
                menuTask_LoadRes.Checked = bLoadVPRes;
                vpWin.bLoadVPRes = bLoadVPRes;
            }

            if (cup.p_VPPRTP.bVisionInfer)
            {
                vpWin.SetVPInferModel(1 + (cup.p_VPPRTP.bDeepInfer ? 1 : 0) * 10);
            }
            else
            {
                vpWin.SetVPInferModel(0 + (bLoadVPRes ? 1 : 0) * 10);
            }

            //     vpWin.ClearDeepInfer(cup.p_VPPRTP.bDeepInfer);
            InitCheckStatus();
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
                menuTask_Predict.Checked = false;
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
            if (strPortName == null || strPortName.Length <= i)
                return false;
            bool bSucc = false;
            try
            {
                if (openSer(sPortAll[i], strPortName[serialInd[i]]))
                {
                    bSucc = true;
                }
                else
                {
                    bSucc = false;
                }
            }
            catch
            {
                bSucc = false;

            }

            //    AddLog();
            return bSucc;
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
        private string GetPortNameByConfigIndex(int configIndex)
        {
            List<string> sortedPorts = GetSortedPorts();

            if (configIndex >= 0 && configIndex < sortedPorts.Count)
            {
                return sortedPorts[configIndex];
            }

            // 如果配置索引超出范围，尝试返回可用的第一个串口
            if (sortedPorts.Count > 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "串口配置";
                operLog.strFailReason = $"配置索引{configIndex}超出可用串口范围(共{sortedPorts.Count}个)，使用默认串口{sortedPorts[0]}";
                AddLog();
                return sortedPorts[0];
            }

            return "";
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
                string portName = GetPortNameByConfigIndex(configIndex);

                if (string.IsNullOrEmpty(portName))
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = $"串口{i + 1}初始化";
                    operLog.strFailReason = $"配置索引{configIndex}超出可用范围";
                    AddLog();
                    UpdateSerialStatusIndicator(i, SerialStatus.Failed);
                    continue;
                }

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
            if (sPortAll[3] != null) sPortCy = sPortAll[3];

            csd._sPort = sPortMain;
            csd._sPortSub = sPortSub;
            csd._sPortCy = sPortCy;

            // 注册数据接收事件
            if (sPortCy != null && sPortCy.IsOpen)
            {
                sPortCy.DataReceived += new SerialDataReceivedEventHandler(PortGetCylinder);
            }

            rts.initStatus.bSerInit = (openedCount > 0);

            // 更新UI显示
            UpdateSerialUIStatus();
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
        // 修改后的串口数据接收事件
        private void PortGetCylinder(object sender, SerialDataReceivedEventArgs e)
        {           
            // 获取接收缓冲区长度
            Thread.Sleep(60);
            // 1. 读取所有可用数据
            int bytesToRead = sPortCy.BytesToRead;
            if (bytesToRead == 0) return;
            byte[] temp = new byte[bytesToRead];
            sPortCy.Read(temp, 0, bytesToRead);

            // 2. 追加到缓存，并加锁防止多线程冲突
            lock (_lockBuffer)
            {
                _rxBuffer.AddRange(temp);
                ProcessSerialBuffer();   // 尝试解析缓存中的完整包
            }
        }

        /// <summary>
        /// 从缓存中循环解析完整的数据包，直到缓存不足一个包为止。
        /// </summary>
        /// 
        private int packLen = 25;
        private void ProcessSerialBuffer() 
        {
            while (_rxBuffer.Count >= packLen)  // 至少能容纳一个完整主动上报包（最短10字节）
            {
              //  toolStripStatus_ExcuteTimes.Text = "接收缓存长度：" + _rxBuffer.Count.ToString();
                // ---- 1. 尝试解析主动上报帧（固定10字节） ----
                if (_rxBuffer[0] == 0x01 && _rxBuffer[1] == 0x45 &&
                    _rxBuffer[2] == 0x01 && _rxBuffer[3] == 0x00 && _rxBuffer[4] == 0x01)
                {
                    if (_rxBuffer.Count < packLen)
                        break;   // 数据不够，等待下次接收
                   

                    byte[] packet = _rxBuffer.GetRange(0, packLen).ToArray();
                    _rxBuffer.RemoveRange(0, packLen);

                   /* operLog.iEventStatus = 0;
                    operLog.strOperEvent = BitConverter.ToString(packet);
                    operLog.strFailReason = "-";
                    AddLog();*/
                    // 处理该主动上报包（内部会判断触发）
                    ProcessActiveReport(packet);
                    continue;
                }

                // ---- 2. 尝试解析设置输出反馈（长度8或16） ----
                if (_rxBuffer.Count >= 8 && _rxBuffer[0] == 0x01 && _rxBuffer[1] == 0x0F)
                {
                    // 实际长度可能为8或16，这里根据协议判断（示例中简单取8）
                    int len = 8;
                    if (_rxBuffer.Count >= len)
                    {
                        byte[] packet = _rxBuffer.GetRange(0, len).ToArray();
                        _rxBuffer.RemoveRange(0, len);
                  //      ProcessSetOutputResponse(packet);
                        continue;
                    }
                    break;
                }

                // ---- 3. 尝试解析查询反馈（长度6） ----
                if (_rxBuffer.Count >= 6 && _rxBuffer[0] == 0x01 && _rxBuffer[1] == 0x02 && _rxBuffer[2] == 0x01)
                {
                    byte[] packet = _rxBuffer.GetRange(0, 6).ToArray();
                    _rxBuffer.RemoveRange(0, 6);
                    ProcessQueryResponse(packet);
                    continue;
                }

                // ---- 4. 无法识别包头，跳过1字节防止死循环 ----
                _rxBuffer.RemoveAt(0);
            }
        }

        // 处理主动上报包（原主动上报逻辑，但改为收集多个触发类型）
        private void ProcessActiveReport(byte[] packet)
        {
            if (!rts.onLS.bTrigg)
                return;

            operLog.iEventStatus = 0;
            operLog.strOperEvent = "收到有效触发信号";
            operLog.strFailReason = "-";
            AddLog();

            // 收集所有触发的类型
            List<int> triggerTypes = new List<int>();
            byte diByte = packet[5];   // DI状态字节

            // A面触发
            if (cup.p_IOPP.APlane > 0)
            {
                byte mask = (byte)(1 << (cup.p_IOPP.APlane - 1));
                if ((diByte & mask) == mask)
                {
                    SetFaceTriggerIndicator(0, true);
                    triggerTypes.Add(1);
                }
            }

            // B面触发
            if (cup.p_IOPP.BPlane > 0)
            {
                byte mask = (byte)(1 << (cup.p_IOPP.BPlane - 1));
                if ((diByte & mask) == mask)
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

                if (!timerIOIn.Enabled)
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
            operLog.strOperEvent = "采集图像";
            vpWin.bMainTaskFinished = false;
            if (cup.p_VPPRTP.bAcqFromCam)  //实时采集
            {
                rts.dtLastAcq = DateTime.Now;
                for (int wsInd = 0; wsInd < maxCameraNum; wsInd++)
                {
                    if(cup.p_CD.bListProcess[wsInd])
                        bRet = vpWin.PreViewOneImage(wsInd);
                    if (!bRet)
                    {
                        operLog.strFailReason = "采集异常错误";
                    }
                }
            }
            else if (!bSingleFrameModel)  //回放模式
            {
                iFileIndex = iNextFileIndex;
                iFileIndex = iFileIndex % iFileGroupLen;
                vpWin.iFileIndex = iFileIndex;
                if (!bCrackPlayback)
                    iNextFileIndex = (++iNextFileIndex) % iFileGroupLen;

                for (int wsInd = 0; wsInd < maxCameraNum; wsInd++)
                {
                    if (cup.p_CD.bListProcess[wsInd])
                        bRet = vpWin.PreViewOneImageFromFile(iFileIndex, false, wsInd);
                    if (!bRet)
                    {
                        operLog.strFailReason = "回放异常错误";
                    }
                } 
            }
            else
            {
                iFileIndex = iNextFileIndex;
                iFileIndex = iFileIndex % (iFileGroupLen * 6);

                for (int wsInd = 0; wsInd < maxCameraNum; wsInd++)
                {
                    if (cup.p_CD.bListProcess[wsInd])
                        bRet = vpWin.PreViewOneImageFromFile(iFileIndex, false, wsInd);
                    if (!bRet)
                    {
                        operLog.strFailReason = "回放异常错误";
                    }
                }  
                iNextFileIndex = (iFileIndex + 1) % (iFileGroupLen * 6);
                
            }

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
            if (!cup.p_VPPRTP.bAcqFromCam && !rts.initStatus.bLocalFileExist)
            {
                operLog.strOperEvent = "数据检测";
                operLog.iEventStatus = -1;
                operLog.strFailReason = "回放文件夹不存在或者图像文件不存在";
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
                if (!vpWin.bMainTaskFinished)   //如果当前有任务在执行，即不能重复运行
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
                        iFileIndex = iNextFileIndex;

                        iFileIndex = iFileIndex % iFileGroupLen;
                        vpWin.iFileIndex = iFileIndex;
                        // iNextFileIndex = iFileIndex;
                        if (!bCrackPlayback)
                            iNextFileIndex = (++iNextFileIndex) % iFileGroupLen;

                    }
                }

                vpWin.bMainTaskFinished = false;

                oneTimeThread = new Thread(RunOneTimeThread);
                oneTimeThread.Start();


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

       
        //更新VP记录，在VP模块发生改变时需要手动更新一次VP图片记录
        private void InitSubRecordsList()
        {
            UpdateThread = new Thread(RunAndUpdateThread);
            UpdateThread.Start();
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
            int icamInd = cbDeviceList.SelectedIndex;
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
            int icamInd = cbDeviceList.SelectedIndex;
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
            if (icamSelectInd < 0 || camera2 == null || camera2[icamSelectInd] == null)
                return;

            bool bEnable = chkGamma_Enable.Checked;   // true=开启 Gamma，false=关闭

            // 部分相机需要在停止采集状态下修改 GammaEnable
            bool wasGrabbing = false;
          //  if (hIKVisionCamera.IsGrabbing(icamSelectInd))  // 假设有该方法，若没有则直接调用 StopGrabbing
            {
                wasGrabbing = true;
                hIKVisionCamera.StopGrabbing(icamSelectInd);
            }

            bool bSuccess = hIKVisionCamera.SetGammaEnable(bEnable, camera2[icamSelectInd]);

            if (wasGrabbing)
                hIKVisionCamera.StartGrabbing(icamSelectInd);

            if (bSuccess)
            {
                // 更新 UI 状态：当 Gamma 禁用时，用户模式复选框和 Gamma 值调节控件应不可用
                chkGamma_UserMode.Enabled = bEnable;
                tbGamma.Enabled = (bEnable && chkGamma_UserMode.Checked);
                nudGamma.Enabled = (bEnable && chkGamma_UserMode.Checked);

                operLog.iEventStatus = 0;
                operLog.strOperEvent = $"Gamma {(bEnable ? "开启" : "关闭")}";
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "设置 Gamma 使能";
                operLog.strFailReason = "调用相机接口失败";
            }
            AddLog();
        }

        /// <summary>
        /// Gamma 用户模式复选框的状态变化事件
        /// 勾选代表使用用户自定义 Gamma 值（User 模式），未勾选代表使用 sRGB 标准曲线
        /// </summary>
        private void chkGamma_UserMode_CheckedChanged(object sender, EventArgs e)
        {
            if (icamSelectInd < 0 || camera2 == null || camera2[icamSelectInd] == null)
                return;

            // 只有 Gamma 使能开启时，才允许切换模式
            if (!chkGamma_Enable.Checked)
            {
                // 若 Gamma 未启用，提示用户先启用 Gamma
                chkGamma_UserMode.Checked = false;
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "切换 Gamma 模式";
                operLog.strFailReason = "请先启用 Gamma 校正";
                AddLog();
                return;
            }

            bool isUserMode = chkGamma_UserMode.Checked;
            int mode = isUserMode ? 0 : 1;   // 0=User, 1=sRGB

            // 停止采集（部分相机要求）
            bool wasGrabbing = false;
        //    if (hIKVisionCamera.IsGrabbing(icamSelectInd))
            {
                wasGrabbing = true;
                hIKVisionCamera.StopGrabbing(icamSelectInd);
            }

            bool bSuccess = hIKVisionCamera.SetGammaSelector(mode, camera2[icamSelectInd]);

            if (wasGrabbing)
                hIKVisionCamera.StartGrabbing(icamSelectInd);

            if (bSuccess)
            {
                // 更新 Gamma 值控件的可用性：User 模式下可调，sRGB 模式下不可调
                tbGamma.Enabled = isUserMode;
                nudGamma.Enabled = isUserMode;

                // 如果切换到 User 模式，需要将界面上当前的 Gamma 值重新写入相机
                if (isUserMode)
                {
                    // 确保 Gamma 使能仍然为开启（代码前面已经检查）
                    float gammaVal = (float)cup.p_Cam[icamSelectInd].dGamma;
                    // 再次写入一次（保证相机值与界面一致）
                    hIKVisionCamera.SetGamma(ref gammaVal, camera2[icamSelectInd]);
                    // 更新显示控件
                    tbGamma.Value = (int)(gammaVal * 10);
                    nudGamma.Value = (decimal)gammaVal;
                }

                operLog.iEventStatus = 0;
                operLog.strOperEvent = $"Gamma 模式切换至 {(isUserMode ? "User (可调)" : "sRGB")}";
            }
            else
            {
                // 若设置失败，恢复复选框状态
                chkGamma_UserMode.Checked = !isUserMode;
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "切换 Gamma 模式";
                operLog.strFailReason = "调用相机接口失败";
            }
            AddLog();
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
            this.Text = "镭射除胶   " + cup.p_DL.modelPath;
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

               /* vpWin.LoadAllFindLineTools(findlineDir);
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "加载边缘定位工具";
                operLog.strFailReason = $"从 {findlineDir} 加载";
                AddLog();*/

                vpWin.ExtractLocateBase(0,cup.p_DL.gpuID);
                vpWin.ExtractLocateBase(1, cup.p_DL.gpuID);
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "提取定位基准";
                AddLog();

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
                this.Text = "镭射除胶   " + cup.p_DL.modelPath;
                //   FrmMainUI.

            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "保存工程";
                operLog.strFailReason = ex.Message;
                AddLog();
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (iJobIndex < 0 || iJobIndex > 1) return;
            _isUpdatingUI = true;
            try
            {
                int channel = cmbLs_Channel.SelectedIndex + 1; // 1~4
                int brightness = 0;

                // 根据工位选择正确的光源配置
                if (iJobIndex == 0) // A面 -> 主光源
                {
                    if (cup.p_LS.indent != null && cup.p_LS.indent.Length > channel)
                        brightness = cup.p_LS.indent[channel];
                }
                else if (iJobIndex == 1) // B面 -> 侧光源1
                {
                    if (cup.p_LS2 != null && cup.p_LS2.Length > 0 &&
                        cup.p_LS2[0].indent != null && cup.p_LS2[0].indent.Length > channel)
                        brightness = cup.p_LS2[0].indent[channel];
                }

                tbBirghtness.Value = brightness;
                nudContrast.Value = brightness;
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }

        private void chkLS_Sync_Click(object sender, EventArgs e)
        {
            if (chkLS_Sync.Checked)
            {
                ApplyLightBrightness(iJobIndex, null, tbBirghtness.Value);
            }
          //  ApplyLightBrightness(iJobIndex);
        }

        private void timerIOIn_Tick(object sender, EventArgs e)
        {
            SetFaceTriggerIndicator(0, false);
            SetFaceTriggerIndicator(1, false);
            timerIOIn.Stop();
        }

        //运行一次，产生新的图像列表
        public void RunAndUpdateThread()
        {
            try
            {

                //    if (!vpWin.HaveRun())
                {
                    vpWin.bChkSubrecord = false;
                    if (vpWin.RunOneTime())
                    {
                        while (vpWin._bRunOneTimeFinished[iJobIndex] != 2)
                        {
                            Thread.Sleep(10);
                        }
                    }
                }
                Thread.Sleep(10);
                vpWin.UpdateSubRecordsList();
                vpWin.bChkSubrecord = true;
                cup = vpWin._cup;
                SaveUserParam();
                btFunRunOneTime.FlatStyle = FlatStyle.Popup;
            }
            catch
            {

            }



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
                    RunOneTimeForFace(selectedJobIndex).GetAwaiter().GetResult();
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

                    vpWin._bRunOneTimeFinished[legacyJobIndex] = 2;  // 强制结束
                    pmaRes[legacyJobIndex] = vpWin._PMARes[legacyJobIndex];

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

        private void UpdateUIExe()
        {
            int jobIndex = (iJobIndex >= 0 && iJobIndex < maxCameraNum) ? iJobIndex : 0;
            UpdateUIExeForJob(jobIndex);
        }

        private void UpdateUIExeForJob(int jobIndex)
        {
            if (jobIndex < 0 || jobIndex >= maxCameraNum)
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
               // AnaEventForJob(iJobIndex);
                //AddEventLog(iEventStatus);  //已取消颜色显示
            }
           /* if(cup.p_RTSP.bUpdateIO)  //启用IO输出
            {
                //只输出OK或者只输出NG
                if(pmaRes[jobIndex].bLocated )
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
                    vpWin._bRunOneTimeFinished[selectedJobIndex] = 2;  //强制结束
                    pmaRes[selectedJobIndex] = vpWin._PMARes[selectedJobIndex];
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
