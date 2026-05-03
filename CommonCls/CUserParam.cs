using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;
//using Cognex.VisionPro.Blob;
using Cognex.VisionPro;
namespace CommonCls
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VPRes
    {
        public bool bDefect;                //是否有缺陷
        public int xbType;                  //匣钵类型
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 5)]
        public int[] iResCode;              //缺陷代码
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 5)]
        public CogTransform2DLinear[] TF;   //定位结果
        public List<CogPolygon> gpgsMain;
        public List<CogPolygon> gpgsSub1;
        public List<CogPolygon> gpgsSub2;
        public List<CogPolygon> gpgsSub3;
        public List<CogPolygon> gpgsSub4;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CameraParam
    {
        public int iMonoType;
        public uint iWidth;
        public uint iHeight;
        public float dGain;
        public float iExposeTime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] iROI;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]  //st 2023年10月2日
        public string strCamName;  //记录清单
        public uint iBrightness;     //亮度
        public uint iSharp;          //锐度
        public float dGamma;          //gamma曲线
        public int iRotateType;
        public int iFlipType;
    }   //摄像头参数

   
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CameraDistr   //相机分配
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public int[] camInd;
        public bool bTriggle;       //是否启用触发
        public int triggleCam;      //触发的相机
        public int triggleMode;     //触发的模式
        public int TriggerActivation;  //触发电平 0：触发上升沿:1：下降沿:2：高电平:3：低电平
        public int trigglePosInd;   //触发相机的安装位置  0：顶面， 1：侧面1   2：侧面2

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public bool[] bListProcess;     //启用哪些相机
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PathAndFile           //路径参数
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]  //st 2023年10月2日
        public string strVPPFilePath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]  //st 2023年10月2日

        public string strSigSavePath;        //单帧存储路径
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]  //st 2023年10月2日

        public string strConSavePath;        //连续存储路径
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]  //st 2023年10月2日
        public string strLogPath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]  //st 2023年10月2日
        public string strResSavePath;     //模型图片路径
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]  //st 2023年10月2日
        public string strPlaybackPath;     //回放数据路径
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]  //st 2023年10月2日
        public string strErrPath;          //标注误判储存路径
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]  //st 2023年10月2日

        public string MarkPicPath;              //预留字段
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]  //st 2023年10月2日
        public string AutoMarkPath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]  //st 2023年10月2日
        public string ManuMarkPath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]  //st 2023年10月2日
        public string strExportPath;        //导出图片路径


        //  public string strPlaybackPath;     //模型图片文件

    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FXParam                       //光度立体法重构参数
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public double[] Slants;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public double[] Tilts;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public string[] ResultType;
        public double sigma;
        public double dMult;
        public double dAdd;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LSParam                      //打光参数                 
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public string[] strCode;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public int[] indent;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public int[] spTime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public bool[] bExe;
     }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VPPRunTimeParam           //实时状态参数
    {
       public  bool bUseFX;         //侧面启用光度立体
       public bool bAcqFromCam;     //数据源于相机实采
       public bool bAcqFromIDB;     //获取IDB数据包数据
       public bool bRecord;         //保存图片记录
       public bool bSaveRes;        //保存识别结果
       public bool bRemarkResult;   //标注识别结果
       public bool bRemarkArea;     //标注识别区域
       public int iLayoutType;      //窗口布局类型
       public bool bUseWeakCrack;   //启用弱裂纹识别
       public int iXBType;          //匣钵类型
        public int iFeedbackType;   //发送结果类型

        public bool bVisionInfer;       //是否进行视觉推理
        public bool bDeepInfer;         //是否进行深度推理
        public bool bFXRestruct;        //是否进行光度图重构
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SubRecordsName
    {
        public int count;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 400)]
        public bool[] bList;  // 记录是否加载
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 400)]
        public string[] strName;  //记录清单
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public int[] subDefaultGUIIndex;  //默认主界面显示记录
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public string[] strDefaultGUIName;  //默认主界面显示记录
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public string[] strGreyGUIName;  //灰度图面显示记录
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public string[] strFXGUIName;  //光度图显示记录
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public string[] strPMAGUIName;  //定位图显示记录 

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public string[] strSelectName;  //记录清单

    }       //图像列表

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VPPDiagDMParam            //底面诊断参数
    {
        public double greyAbnormCount;     // 
        public double greyAbnormArea;           // 
        public double FXAbnormArea1;
        public double FXAbnormArea2;
    }
    
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VPPDiagCMParam            //侧面诊断参数
    {
        public double edgeAbnormCount;     // 
        public double edgeLeftAbnormCount;           // 
        public double edgeRightAbnormCount;
        public double lwArea; 
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VPPDiagParam          //视觉诊断参数
    {
       public VPPDiagDMParam dmParam;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
       public VPPDiagCMParam[] cmParam;

    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SerDistrParam         //串口参数
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public int[] serInd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public bool[] bUsed;

        public int BaudRate;        //串口的波特率 
        public int DataBits;        //数据位 
        public int parity;          //奇偶校验 
        public int stopBits;        //停止位 1 bit
        public int WriteTimeout;    //写超时 

    }


    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BlowParam         //吹气参数
    {
        public bool bUsed;     //是否启用
        public int blowType;//吹气方式：0：等间隔吹气   1：检测吹气
        public int perBlowTime; //每次吹气时长
        public int intervType;// 等间隔时长  0：每天  1：每小时  2：每分钟
        public int intervTimes;////指定间隔内吹气次数
        public int reguType; //0:检测前   1：检测后
        public int perExeTimes;// 多少次检测执行一次吹气

    }


    //深度学习参数
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DLParam           
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]  //st 2023年10月2日
        public string modelPath;            // 模型路径 
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]  //st 2023年10月2日
        public string modelType;            // 模型类型
        public bool bUseGPU;                //  是否使用GPU
        public int gpuID;                   //GPU ID
        public float fThreshold;            //裂纹光度图  阈值 
        public float fThresholdCrackGrey;   //裂纹灰度图 阈值 
        public float fThresholdPoszFX;      // 破损光度图 阈值 
        public float fThresholdPoszGrey;    // 破损灰度图 阈值
        public float fThresholdCrack;       //裂纹综合阈值        
        public float fThresholdPosz;        //破损综合阈值
        public float fIoUTh;                //交并比阈值
        public int iPoszNum;                //破损面数阈值

        public float fThresholdPoszArea; //破损面积
        public float fThresholdPoszSide;    //破损边长
        public float fThresholdCrackLen;    //裂纹长度
    }

    //VP高级诊断参数
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CrackParam
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public float[] fBlobParam;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public float[] fSureCrackParam;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public float[] fSureNotCrackParam;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
        public float[] fCrackParam;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public float[] fPoszParam;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public float[] fQPParam;

    }

    //实时运行状态 2024年7月2日
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LineInfo
    {
        public string strLineName;            //产线名称
        public int LineNum;                   //产线编号
        public string strLineDesc;            //产线描述
        public string strLineScence;          //产线现场图片 
        public string strLinePrefix;          //线体命名前缀
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public string[] strCamPrefix;         //相机命名前缀
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public string[] strPrefix;         //相机命名前缀
        public int bPreType;                //前缀规则
    }

    //实时运行状态 2024年7月2日
    

    // VPP 相关参数
    public struct PMARes
    {
        public bool bLocated;               //是否成功定位
        public bool bOk;
        public double dTX;                  //定位平移量X
        public double dTY;                  //定位平移量Y
        public double dTR;                  //定位旋转量R 
        public double dPMAScore;            //定位分数Score 
    }
    public struct CalibRes
    {
        public bool bCalib;               //是否成功标定
        public double dTX;                  //标定平移量X
        public double dTY;                  //标定平移量Y
        public double dTR;                  //标定旋转量R 
        public double dXYRatio;             //纵横比
        public double dSkew;                //倾斜
        public double dScale;               //缩放比例
        public double dErr;                 //标定误差 

    }
    //9点标定模板图  阈值分割参数
   
    public struct calibBlobParam
    {
        public bool     bPolarity‌;              //极性
        public int      iSegType;               //分割模式， 1：固定硬阈值  2：自适应硬阈值
        public int      iThre;                  //分割阈值
        public int      iAreaMin;               //最小面积 
        public int      iAreaMax;               //最大阈值
    }

    //9点标定模板图  阈值分割参数
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct calibTemplateParam
    {
        public int iRow;                //行数
        public int iCol;                //列数
        public double dRadius;          //半径
        public double dDisX;           //行间距
        public double dDisY;           //列间距
        public double dCentX;          //中心圆中心X坐标
        public double dCentY;          //中心圆中心Y坐标
    }

    //PMAlign参数
    public struct PMAlignParam
    {
        public int TrainAlg;                //训练算法
        public bool TrainPolarity;          //考虑极性
        public bool TrainAutoEdgeThre;          //自动边缘阈值
        public double TrainEdgeThre;           //边缘阈值
        public bool TrainAutoGrun;      //自动粒度
        public double TrainCoarse;   //粗糙粒度
        public double TrainFine;   //粗糙粒度

        public int RunAlg;                //运行算法
        public bool RunArt;          //考虑杂斑
        public bool RunAutoEdgeThre;          //自动边缘阈值
        public double  RunEdgeThre;           //边缘阈值
        public double RunConstractThre;     //对比度阈值
        public int RunSearchNum;        //搜索数量
        public double RunScoreThre;     //分数阈值
        public bool RunEnableAngle;     //允许角度
        public double RunAngleD;        //最小角度
        public double RunAngleU;        //最大角度
    }


    //IO端口选择参数
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IOPortParam
    {
        public int AcqTrig;                 // 采集触发
        public int DetTrig;                 // 检测触发
        public int EndAcqTrig;              // 结束采集
        public int EndDetTrig;              // 结束检测
        public int OkOut;                   // 输出OK
        public int NGOut;                   // 输出NG
        public int defIn1;                  // 
        public int defIn2;                  //
        public int defIn3;                  //
        public int defIn4;                  //
        public int IOTimeDelay;             // 输出脉冲时长
        public int defOut2;                  //
        public int defOut3;                  //
        public int defOut4;                  //

    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct  RunTimeStatusParam           //运行状态参数
    {
        public bool bAutoConnCam;               //自动连接相机
        public bool bAutoConnSerial;            //自动连接串口
        public bool bAutoConnJCZ;               //自动连接激光板卡
        public bool bAutoConnMotionCard;        //自动连接电机运动卡
        public bool bAutoRunVPP;                //自动运行VPP，防止VPP假死
        public bool bUpdateImage;               //运行结束更新图像
        public bool bUpdateRecord;              //运行结束更新日志区记录
        public bool bUpdateLog;                 //运行结束更新保存日志文件
        public bool bUpdateIO;                  //运行结束输出IO信号
       
        public bool bSaveRes;                   //运行结束保存结果  

        public bool bAutoConnDef1;                  
        public bool bAutoConnDef2;                      
        public bool bAutoConnDef3;                      
        public bool bAutoConnDef4;                      

                     
        public bool bUpdateDef2;                        
        public bool bUpdateDef3;                        
        public bool bUpdateDef4;

        public int runTimeOut;              //VP单次运行超时
    }

    [Serializable]
    public class CUserParam
    {
        const int maxCameraNum = 2;
        public CameraParam[] p_Cam = new CameraParam[maxCameraNum];
        public PathAndFile p_Paf = new PathAndFile();
        public FXParam p_FX = new FXParam(); 
        public LSParam p_LS = new LSParam();
        public LSParam[] p_LS2 = new LSParam[2];
        public CameraDistr p_CD = new CameraDistr();
        public VPPRunTimeParam p_VPPRTP = new VPPRunTimeParam();
        public SubRecordsName p_SRN = new SubRecordsName();
        public VPPDiagParam p_DP = new VPPDiagParam();
        public FXParam[] p_subFX = new FXParam[maxCameraNum - 1];
        public SerDistrParam p_SD = new SerDistrParam();
        public DLParam p_DL = new DLParam();

        public CrackParam p_CP = new CrackParam();
        public LineInfo p_LI = new LineInfo();
        public BlowParam p_BP = new BlowParam();
        public calibTemplateParam calTP = new calibTemplateParam();
        public IOPortParam p_IOPP = new IOPortParam();
        public RunTimeStatusParam p_RTSP = new RunTimeStatusParam();
        //  public calibBlobParam calbp = new calibBlobParam();


        public CUserParam()
        {
            InitCameraParam();
            InitPathAndFile();
            InitFXParam();
            InitSubFXParam();
            InitLSParam();
            InitLSParam1();
            InitCameraDistr();
            InitVPPParam();
            InitSubRecordsName();
            InitDiagParam();
            InitSerDistrParam();
            InitDLParam();
            InitCrackParam();
            InitLineParam();
            InitBlowParam();
            InitCalibTemplateParam();
            InitIOPortParam();
            InitRTSParam();
        }

        public void InitCameraDistr()
        {
            p_CD.camInd = new int[maxCameraNum];
            p_CD.bListProcess=new bool[maxCameraNum];
            for (int i=0;i<maxCameraNum;i++)
            {
                p_CD.camInd[i] = i;
                p_CD.bListProcess[i] = true;
            }
           /* p_CD.camInd[0] = 0;
            p_CD.camInd[1] = 1;
            p_CD.camInd[2] = 2;
            p_CD.camInd[1] = 1;
            p_CD.camInd[2] = 2;*/

            p_CD.triggleCam = 1;   //前侧面触发
            p_CD.triggleMode = 1;  // line0触发模式
            p_CD.TriggerActivation = 0;  // 上升沿触发
            p_CD.trigglePosInd = 1;
        }
        public void InitCameraParam()
        {

            for (int i = 0; i < maxCameraNum; i++)
            {

                p_Cam[i].iMonoType = 0;
                p_Cam[i].iWidth = 3072;
                p_Cam[i].iHeight = 2048;
                p_Cam[i].dGain = 0;
                p_Cam[i].iExposeTime = 25000;

                p_Cam[i].iROI = new uint[4];
                p_Cam[i].iROI[0] = 0;
                p_Cam[i].iROI[1] = 0;
                p_Cam[i].iROI[2] = 3072;
                p_Cam[i].iROI[3] = 2048;
            }


        }
        public void InitPathAndFile()
        {
            p_Paf.strVPPFilePath = @".\VPP\MDT.vpp";
            p_Paf.strSigSavePath = "SigSave\\"; ;
            p_Paf.strConSavePath = "contSave\\";
            p_Paf.strResSavePath = "\\data\\万华测试3\\20220807\\";
            p_Paf.strPlaybackPath = @"\data\万华测试3\20220807\";
            p_Paf.strErrPath = @"\result\";
            p_Paf.ManuMarkPath = @"..\..\EZDModel";
        }
        public void InitFXParam()
        {
            p_FX.Slants = new double[4];
            p_FX.Tilts = new double[4];
            p_FX.ResultType = new string[2];

            p_FX.Slants[0] = 45.4; p_FX.Slants[1] = 44.8; p_FX.Slants[2] = 45.2; p_FX.Slants[3] = 46.1;
            p_FX.Tilts[0] = 0; p_FX.Tilts[1] = 90; p_FX.Tilts[2] = 180; p_FX.Tilts[3] = -90;
            p_FX.ResultType[0] = "gradient";
            p_FX.ResultType[1] = "albedo";
            p_FX.sigma = 3;
            p_FX.dMult = 1460;
            p_FX.dAdd = 140; 
        }
        public void InitSubFXParam()
        {
            p_subFX = new FXParam[maxCameraNum-1];
            for (int i=0;i< maxCameraNum - 1; i++)
            { 
                p_subFX[i].Slants = new double[4];
                p_subFX[i].Tilts = new double[4];
                p_subFX[i].ResultType = new string[2];

                p_subFX[i].Slants[0] = 45; p_subFX[i].Slants[1] = 45; p_subFX[i].Slants[2] = 45;
                p_subFX[i].Tilts[0] = 0; p_subFX[i].Tilts[1] = 90; p_subFX[i].Tilts[2] = 180;
                p_subFX[i].ResultType[0] = "gradient";
                p_subFX[i].ResultType[1] = "albedo";
                p_subFX[i].sigma = 2;
                p_subFX[i].dMult = 1460;
                p_subFX[i].dAdd = 140;
            } 
        }

        public void InitLSParam()
        {
            p_LS.strCode = new string[6];
            p_LS.spTime = new int[6];
            p_LS.indent = new int[6];
            p_LS.bExe = new bool[6];

            for (int i = 0; i < 6; i++)
            {
                p_LS.spTime[i] = 100;
                p_LS.indent[i] = 255;
                p_LS.bExe[i] = true;
            }
            p_LS.strCode[0] = "1234";
            p_LS.strCode[1] = "1";
            p_LS.strCode[2] = "2";
            p_LS.strCode[3] = "3";
            p_LS.strCode[4] = "4";
            p_LS.strCode[5] = "";

        }

        public void InitLSParam1()
        {
            for (int j = 0; j < 2; j++)
            {
                p_LS2[j] = new LSParam();
                p_LS2[j].strCode = new string[6];
                p_LS2[j].spTime = new int[6];
                p_LS2[j].indent = new int[6];
                p_LS2[j].bExe = new bool[6];

                for (int i = 0; i < 6; i++)
                {
                    p_LS2[j].spTime[i] = 100;
                    p_LS2[j].indent[i] = 255;
                    p_LS2[j].bExe[i] = true;
                }
                p_LS2[j].strCode[0] = "1234";
                p_LS2[j].strCode[1] = "1";
                p_LS2[j].strCode[2] = "2";
                p_LS2[j].strCode[3] = "3";
                p_LS2[j].strCode[4] = "4";
                p_LS2[j].strCode[5] = "";
            }
        }

        public void InitVPPParam()
        {
            p_VPPRTP.bUseFX = true;
            p_VPPRTP.bAcqFromCam = false;
            p_VPPRTP.bAcqFromIDB = false;
            p_VPPRTP.bRecord = true;
            p_VPPRTP.bSaveRes = true;
            p_VPPRTP.bRemarkResult = true;
            p_VPPRTP.bRemarkArea = true;
            p_VPPRTP.iLayoutType = 3;
            p_VPPRTP.bUseWeakCrack = false;
            p_VPPRTP.iXBType = 0;
            p_VPPRTP.iFeedbackType = 1;
            p_VPPRTP.bDeepInfer = false;        //默认不开启深度推理
            p_VPPRTP.bVisionInfer = true;       //默认开启视觉推理
            p_VPPRTP.bFXRestruct = true;        //默认开启FX重构
        }
        public void InitSubRecordsName()
        {
            p_SRN.count =0;
            p_SRN.bList = new bool[400];
            p_SRN.strName = new string[400];
           
            for (int i=0;i<400;i++)
            {
                p_SRN.bList[i] = true;
            }
            p_SRN.subDefaultGUIIndex = new int[maxCameraNum];
            p_SRN.strDefaultGUIName = new string[maxCameraNum];
            p_SRN.strGreyGUIName = new string[maxCameraNum];
            p_SRN.strFXGUIName = new string[maxCameraNum];
            p_SRN.strPMAGUIName = new string[maxCameraNum];
            p_SRN.strSelectName = new string[40];
        }

        public void InitDiagParam()
        {
            p_DP.dmParam = new VPPDiagDMParam();
            p_DP.dmParam.greyAbnormCount = 40;
            p_DP.dmParam.greyAbnormArea = 8;
            p_DP.dmParam.FXAbnormArea1 = 2000;
            p_DP.dmParam.FXAbnormArea2 = 15000;
            p_DP.cmParam = new VPPDiagCMParam[maxCameraNum-1];

            for(int i=0;i< maxCameraNum-1; i++)
            {
                p_DP.cmParam[i].lwArea = 600;
                p_DP.cmParam[i].edgeAbnormCount = 8;
                p_DP.cmParam[i].edgeLeftAbnormCount = 20;
                p_DP.cmParam[i].edgeRightAbnormCount = 5;
            }
            
        }
   
        public void InitSerDistrParam()
        {
            p_SD.serInd = new int[4];
            p_SD.bUsed = new bool[4];
            for(int i=0;i<4;i++)
            {
                p_SD.serInd[i] = i;
                p_SD.bUsed[i] = true;
            }

            p_SD.BaudRate = 19200;//串口的波特率 
            p_SD.DataBits = 8;//数据位 
            p_SD.parity =(int) Parity.None;////奇偶校验 
            p_SD.stopBits = (int)StopBits.One; //停止位 1 bit
            p_SD.WriteTimeout = 100;//写超时 
        }

        public void InitDLParam()
        {
            p_DL.bUseGPU = true;
            p_DL.fThreshold = 0.5f;
            p_DL.fThresholdCrackGrey = 0.8f;
            p_DL.fThresholdPoszFX = 0.5f;
            p_DL.fThresholdPoszGrey = 0.8f;
            p_DL.gpuID = 0;
            p_DL.modelType = "paddlex";
            p_DL.modelPath = @"H:\st\xb\XBmodel\infer\inference_model";

            p_DL.fThresholdCrack=1.0f;       //裂纹综合阈值        
            p_DL.fThresholdPosz = 1.4f;       //破损综合阈值
            p_DL.fIoUTh = 0.35f;               //交并比阈值
            p_DL.iPoszNum=2;                //破损检测面数

            p_DL.fThresholdPoszArea = 6000;
            p_DL.fThresholdPoszSide = 40;
            p_DL.fThresholdCrackLen = 100;
        }

        public void InitCrackParam()
        {
            p_CP.fBlobParam = new float[20];
            p_CP.fCrackParam = new float[60];
            p_CP.fSureCrackParam = new float[10];
            p_CP.fSureNotCrackParam = new float[10];
            p_CP.fPoszParam = new float[10];
            p_CP.fQPParam = new float[10];


            p_CP.fSureNotCrackParam[0] = 10;        //类型1标准差阈值
            p_CP.fSureNotCrackParam[1] = 20;        //类型2标准差阈值 
            p_CP.fSureNotCrackParam[2] = 0.35f;      //类型1确认非裂纹阈值系数
            p_CP.fSureNotCrackParam[3] = 0.35f;      //类型1确认非裂纹阈值系数
            p_CP.fSureNotCrackParam[4] = 150;      //类型1确认非裂纹阈值系数
            p_CP.fSureNotCrackParam[5] = 150;      //类型1确认非裂纹阈值系数

            p_CP.fSureCrackParam[1] = 1.7f;         //超过1.7不进行验证



            p_CP.fBlobParam[0] = 6;        //类型1blob扫描小边界
            p_CP.fBlobParam[1] = 6;        //类型2blob扫描小边界
            p_CP.fBlobParam[2] = 0.35f;        //类型1blob扫描小边界系数
            p_CP.fBlobParam[3] = 0.35f;        //类型2blob扫描小边界系数
            p_CP.fBlobParam[4] = 1.5f;        //类型1blob扫描大边界系数
            p_CP.fBlobParam[5] = 1.5f;        //类型2blob扫描大边界系数



            /*double dTopMin, double dWHTh, double dAngTh, double dDisTh, double dAspth, double dAvgHTh, double dShapeTh1*/
           //光度图参数
            p_CP.fCrackParam[0] = 50;       //dTopMin
            p_CP.fCrackParam[1] = 100;      //dWHTh
            p_CP.fCrackParam[2] = 0.6f;     //dAngTh
            p_CP.fCrackParam[3] = 120;      //dDisTh
            p_CP.fCrackParam[4] = 3;        //dAspth
            p_CP.fCrackParam[5] = 25;       //dAvgHTh
            p_CP.fCrackParam[6] = 0.3f;      //dShapeTh1

            //灰度图参数  50, 100, 0.6, 120, 3, 25, 0.3
            p_CP.fCrackParam[10] = 50;       //dTopMin
            p_CP.fCrackParam[11] = 100;      //dWHTh
            p_CP.fCrackParam[12] = 0.6f;     //dAngTh
            p_CP.fCrackParam[13] = 120;      //dDisTh
            p_CP.fCrackParam[14] = 3;        //dAspth
            p_CP.fCrackParam[15] = 25;       //dAvgHTh
            p_CP.fCrackParam[16] = 0.3f;      //dShapeTh1



            p_CP.fCrackParam[20] = 100;         //dStepLen = 100; 
            p_CP.fCrackParam[21] = 0.24f;      // dWHFactorTh = 0.24;  
            p_CP.fCrackParam[22] = 4;           //dWHRationStart = 4;
            p_CP.fCrackParam[23] = 20;          //dAvgWStd = 20;
            p_CP.fCrackParam[24] = 7;           //dScoreTh = 7;
            p_CP.fCrackParam[25] = 6;           // dScoreTh2 = 6; 
            p_CP.fCrackParam[26] = 100;         // dImHTh = 100;
            p_CP.fCrackParam[27] = 0.1f;        //dHordAngTh = 0.1;

            p_CP.fCrackParam[28] = 1.3f;         //dVerdAngTh = 1.3;
            p_CP.fCrackParam[29] = 2.2f;      //  dVerLneWHRationTh = 2.2;
            p_CP.fCrackParam[30] = 2;           //dHAvgWTh = 2;
            p_CP.fCrackParam[31] = p_CP.fCrackParam[22];          //dLneWHRationTh = dWHRationStart;
            p_CP.fCrackParam[32] = 20;           //dTopTh = 20; 
            p_CP.fCrackParam[33] = 30;           // dAvgWThCan = 30;
            p_CP.fCrackParam[34] = 0.3f;         // dShapeThCan = 0.3;
            p_CP.fCrackParam[35] = 3.5f;        // dLneWHRationThCan = 3.5;
            p_CP.fCrackParam[36] = 0.3f;           // dWHFactorThCan = 0.3;
            p_CP.fCrackParam[37] = 40;         // dWHThTmpCan = 40;
            p_CP.fCrackParam[38] = 5;        // dScoreThCan = 5;
             
        }

        public void InitLineParam()
        {
            p_LI.strLineName = "1#产线";
            p_LI.strLineDesc = "1#窑炉";
            p_LI.LineNum = 1;
            p_LI.strLineScence = "Default.jpg";
            p_LI.strLinePrefix = "1#";
            p_LI.strCamPrefix = new string[maxCameraNum];
            p_LI.strCamPrefix[0] = "Main";
            p_LI.strCamPrefix[1] = "Sub1";
         //   p_LI.strCamPrefix[2] = "Sub2";
        //    p_LI.strCamPrefix[3] = "Sub3";
       //     p_LI.strCamPrefix[4] = "Sub4";
            p_LI.strPrefix = new string[maxCameraNum];
            p_LI.strPrefix[0] = "1#_Main_";
            p_LI.strPrefix[1] = "1#_Sub1_";
        //    p_LI.strPrefix[2] = "1#_Sub2_";
         //   p_LI.strPrefix[3] = "1#_Sub3_";
        //    p_LI.strPrefix[4] = "1#_Sub4_";

            p_LI.bPreType = 1;
        }
        public void InitBlowParam()
        {
            p_BP.bUsed = true;
            p_BP.blowType = 0;
            p_BP.perBlowTime = 600;
            p_BP.intervType = 1;
            p_BP.intervTimes = 1;
            p_BP.reguType = 1;
            p_BP.perExeTimes = 10;
        }

        public void InitCalibTemplateParam()
        {
            calTP.dCentX = 0;
            calTP.dCentY = 0;
            calTP.dDisX = 20;
            calTP.dDisY = 20;
            calTP.dRadius = 2.5;
            calTP.iCol = 3;
            calTP.iRow = 3;
        }
        public void InitIOPortParam()
        {
            p_IOPP.OkOut = 1;
            p_IOPP.NGOut = 2;
            p_IOPP.IOTimeDelay = 100;
        }

        public void InitRTSParam()
        {
            p_RTSP.bAutoConnCam = true;
            p_RTSP.bAutoConnSerial = true;
            p_RTSP.bAutoConnJCZ = true;
            p_RTSP.bAutoConnMotionCard = true;
            p_RTSP.bAutoRunVPP = true;
            p_RTSP.bUpdateImage = true;
            p_RTSP.bUpdateRecord = true;
            p_RTSP.bUpdateLog = true;
            p_RTSP.bUpdateIO = true;
            p_RTSP.runTimeOut = 1000;  //1000ms运行超时
        }
    }


    /// <summary>
    /// //////////////////////////////////////
    /// 以下参数不保存至用户参数文件
    /// </summary>


    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CameraStatus      //摄像头状态
    {
        public int camNum;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        //五个相机的状态，0：表示为连接  1：表示已连接
        //2:表示相机参数设置正常 3：表示设备开启循环采集  4：表示软件在采集图像  -1：表示未知状态
        public int[] iStatus;
    }


    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InitStatus         //初始化状态参数
    {
        public bool bUserParamFileInit;     //2024年5月17日
        public bool bModelRoleCert;         //2024年5月17日
        public bool bCamInit;               //进行了相机初始化
        public bool bCamPrepared;           //相机符合工作条件
        public string strCamInit;
        public bool bSerInit;
        public bool bLocalFileExist;
        public bool bIDBFileExist;
        public bool bDLModelPathExist;
        public bool bVPPFileExist;
        public bool bDLModelInit;
        public bool bVPPInit;
        public int iVPPStatus;
        public bool bLocalFileRegular;      //回放文件规整对齐
        public int bJCZ;
    }

    public struct ResultInfo   //结果参数
    {
        public int iInd;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]  //st 2023年10月2日
        public string strTime;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]  //st 2023年10月2日
        public string strResCode;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]  //st 2023年10月2日
        public string strMark;
        
    }

    public struct OperLog   //操作日志
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]  //st 2023年10月2日
        public string strUserName;
        public DateTime dtOperTime;
        public string strOperEvent;
        public string strFailReason;
        public int iEventStatus;
    }
    public struct DetectionResult
    {
        public int[] iPlaneIndex;       //各检测面代码
        public string[] strResult;
        public string[] strRemark;      //备注
        public int xbType;                                      //xb类型，2024年7月21日
    }

    public struct BlocResult
    {
       
        public int[] iPlaneIndex;       //各检测面代码
        public string[] strResult;
        public string[] strRemark;      //备注
        public int xbType;                                      //xb类型，2024年7月21日
    }

    //检测结果统计
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DetStat           //检测结果统计
    {
        public int iTotal;          //总共执行检测次数
        public int iOKNum;          //检测结果OK的次数
        public float iOKPer;        //良品率
        public int iNGNum;          //检测结果NG的次数
        public int iWarningNum;     //检测警告的次数
        public int iErrNum;         //检测错误的次数
    } 
    //采集结果统计
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AcqStat           //检测结果统计
    {
        public int iTotal;          //总共执行采集次数
        public int iLossNum;          //采集丢失的次数      
        public int iErrNum;         //采集错误的次数
    }


    //实时运行状态(选项）
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct OnLineStatus
    {
        public bool bRecord;            //是否录制模式（保存采集图片）
        public bool bAcqFromCam;        //是否从相机获取图像
        public bool bSaveRes;           //是否保存结果（含有标注结果图像）
        public bool bTrigg;             //是否采用触发模式
        public bool bLock;              //是否锁定
        public bool bVisionInfer;       //是否进行视觉推理
        public bool bDeepInfer;         //是否进行深度推理
        public bool bFXRestruct;        //是否进行光度图重构
        public int iFeedbackType;       //发送结果类型
       
    }

    public struct AlignImParam
    {
        public int width;            // 像素高度
        public int height;          // 像素宽度
        public double dCenX;           // 激光坐标系下 中心坐标X
        public double dCenY;             // 激光坐标系下 中心坐标Y
        public double dWidth;              // 激光坐标系下 图像宽度
        public double dHeight;       // 激光坐标系下 高度 
    }

    //软件运行实时状态参数
    [Serializable]
    public class RunTimeStatus
    {
        public DateTime dtLastAcq;      //最后一次采集时间
        public DateTime dtLastDet;      //最后一次检测时间
        public CameraStatus cs;         //相机的状态
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public int[] iSerStatus;        //串口状态 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public int[] iEventStatus;        //检测结果
        public int iModelRole;            //用户角色
        public int iTaskType;             //当前正在执行的任务
        public DetStat detS;              //检测结果统计
        public AcqStat acqS;              //采集结果统计  
        public OnLineStatus onLS;         //实时状态
        public int driveS;                  //硬盘空间状态
        public InitStatus initStatus;            //初始化状态
        public int iIsRunning;              //是否在运行
        public RunTimeStatus()
        {
            dtLastAcq = new DateTime();
            dtLastDet = new DateTime();
            cs = new CameraStatus();
            detS = new DetStat();
            acqS = new AcqStat();
            initStatus = new InitStatus();
            onLS = new OnLineStatus();
            iModelRole = -1;
            iTaskType = -1;
            driveS = -1;
            initEventStatus();
            initSerStatus();

        }
        public void initSerStatus()
        {
            iSerStatus = new int[4];
        }
        public void initEventStatus()
        {
            iEventStatus = new int[6];
        }
        public void initControlStatus()
        {

        }
    }


    public class FileOper
    {
        #region  读取和保存参数
        //读取用户参数
        public bool ReadUserParam(ref CUserParam cup)
        {

            FileStream fs;
            try
            {
                //      SaveUserParam(cup);
                //   operLog.strOperEvent = "读取配置参数";
                if (File.Exists("UserData\\serUserParam.dat"))
                {
                    fs = new FileStream("UserData\\serUserParam.dat", FileMode.Open);
                    BinaryFormatter bf = new BinaryFormatter();
                    cup = bf.Deserialize(fs) as CUserParam;
                    fs.Close();
                    // SaveUserParam(cup);
                    CheckParam(cup);  //兼容性考虑
                }
                else
                {
                    SaveUserParam(cup);
                    return false;
                }
            }
            catch(Exception ex)
            {
                // if(f)
                // fs.Close();
                cup = new CUserParam();
                SaveUserParam(cup);

                return false;
            }
            return true;
        }

        private void CheckParam(CUserParam cup)
        {
            CUserParam cupTmp = new CUserParam();
            if (cupTmp.p_Cam.Length > cup.p_Cam.Length)
            {
                for (int i = 0; i < cup.p_Cam.Length; i++)
                {
                    cupTmp.p_Cam[i] = cup.p_Cam[i];
                }
                cup.p_Cam = cupTmp.p_Cam;
            }
            if (cupTmp.p_subFX.Length > cup.p_subFX.Length)
            {
                for (int i = 0; i < cup.p_subFX.Length; i++)
                {
                    cupTmp.p_subFX[i] = cup.p_subFX[i];
                }
                cup.p_subFX = cupTmp.p_subFX;
            }

            for (int i = 1; i < 4; i++)
            {
                if (cupTmp.p_subFX[i].Tilts.Length > cup.p_subFX[i].Tilts.Length)
                {
                    for (int j = 0; j < cup.p_subFX[i].Tilts.Length; j++)
                    {
                        cupTmp.p_subFX[i].Tilts[j] = cup.p_subFX[i].Tilts[j];
                        cupTmp.p_subFX[i].Slants[j] = cup.p_subFX[i].Slants[j];
                    }
                    cup.p_subFX[i].Tilts = cupTmp.p_subFX[i].Tilts;
                    cup.p_subFX[i].Slants = cupTmp.p_subFX[i].Slants;
                }
            }


            if (cupTmp.p_CD.camInd.Length > cup.p_CD.camInd.Length)
            {
                for (int i = 0; i < cup.p_CD.camInd.Length; i++)
                {
                    cupTmp.p_CD.camInd[i] = cup.p_CD.camInd[i];
                }
                cup.p_CD.camInd = cupTmp.p_CD.camInd;
            }

            if (cupTmp.p_SRN.strDefaultGUIName.Length > cup.p_SRN.strDefaultGUIName.Length)
            {
                for (int i = 0; i < cup.p_SRN.strDefaultGUIName.Length; i++)
                {
                    //    cupTmp.p_SRN.subDefaultGUIIndex[i] = cup.p_SRN.subDefaultGUIIndex[i];
                    cupTmp.p_SRN.strDefaultGUIName[i] = cup.p_SRN.strDefaultGUIName[i];
                    cupTmp.p_SRN.strGreyGUIName[i] = cup.p_SRN.strGreyGUIName[i];
                    cupTmp.p_SRN.strFXGUIName[i] = cup.p_SRN.strFXGUIName[i];
                    cupTmp.p_SRN.strPMAGUIName[i] = cup.p_SRN.strPMAGUIName[i];
                }
                cup.p_SRN = cupTmp.p_SRN;
            }

            if (cupTmp.p_DP.cmParam.Length > cup.p_DP.cmParam.Length)
            {
                for (int i = 0; i < cup.p_DP.cmParam.Length; i++)
                {
                    //    cupTmp.p_SRN.subDefaultGUIIndex[i] = cup.p_SRN.subDefaultGUIIndex[i];
                    cupTmp.p_DP.cmParam[i] = cup.p_DP.cmParam[i];

                }
                cup.p_DP = cupTmp.p_DP;
            }

            if (cupTmp.p_LS2.Length > cup.p_LS2.Length)
            {
                for (int i = 0; i < cup.p_LS2.Length; i++)
                {
                    //    cupTmp.p_SRN.subDefaultGUIIndex[i] = cup.p_SRN.subDefaultGUIIndex[i];
                    cupTmp.p_LS2[i] = cup.p_LS2[i];

                }
                cup.p_LS2 = cupTmp.p_LS2;
            }

            if (cup.p_SD.serInd == null || cup.p_SD.BaudRate == 0)
            {
                cup.p_SD = cupTmp.p_SD;
                //  cup.p_SD.bUsed = cupTmp.p_SD.bUsed;
            }

            if (cup.p_DL.modelPath == null || cup.p_DL.modelPath == "")
            {
                cup.InitDLParam();
            }
            if (cup.p_DL.fThreshold == 0)
                cup.p_DL.fThreshold = cupTmp.p_DL.fThreshold;
            if (cup.p_DL.fThresholdCrackGrey == 0)
                cup.p_DL.fThresholdCrackGrey = cupTmp.p_DL.fThresholdCrackGrey;
            if (cup.p_DL.fThresholdPoszFX == 0)
                cup.p_DL.fThresholdPoszFX = cupTmp.p_DL.fThresholdPoszFX;
            if (cup.p_DL.fThresholdPoszGrey == 0)
                cup.p_DL.fThresholdPoszGrey = cupTmp.p_DL.fThresholdPoszGrey;

            if (cup.p_DL.fThresholdCrack == 0)
                cup.p_DL.fThresholdCrack = cupTmp.p_DL.fThresholdCrack;
            if (cup.p_DL.fThresholdPosz == 0)
                cup.p_DL.fThresholdPosz = cupTmp.p_DL.fThresholdPosz;
            if (cup.p_DL.fIoUTh == 0)
                cup.p_DL.fIoUTh = cupTmp.p_DL.fIoUTh;
            if (cup.p_DL.iPoszNum == 0)
                cup.p_DL.iPoszNum = cupTmp.p_DL.iPoszNum;

            if (cup.p_DL.fThresholdCrackLen == 0)
                cup.p_DL.fThresholdCrackLen = cupTmp.p_DL.fThresholdCrackLen;
            if (cup.p_DL.fThresholdPoszArea == 0)
                cup.p_DL.fThresholdPoszArea = cupTmp.p_DL.fThresholdPoszArea;
            if (cup.p_DL.fThresholdPoszSide == 0)
                cup.p_DL.fThresholdPoszSide = cupTmp.p_DL.fThresholdPoszSide;

            if (cup.p_VPPRTP.iXBType == 0)
                cup.p_VPPRTP.iXBType = cupTmp.p_VPPRTP.iXBType;

            //   if(cup.p_CP.fBlobParam==null)
            {
                cup.p_CP.fBlobParam = (float[])cupTmp.p_CP.fBlobParam.Clone();
                cup.p_CP.fSureNotCrackParam = (float[])cupTmp.p_CP.fSureNotCrackParam.Clone();
                cup.p_CP.fPoszParam = (float[])cupTmp.p_CP.fPoszParam.Clone();
                cup.p_CP.fQPParam = (float[])cupTmp.p_CP.fQPParam.Clone();

                if (cup.p_CP.fSureCrackParam == null)
                    cup.p_CP.fSureCrackParam = (float[])cupTmp.p_CP.fSureCrackParam.Clone();

                if (cup.p_CP.fCrackParam == null)
                    cup.p_CP.fCrackParam = (float[])cupTmp.p_CP.fCrackParam.Clone();
            }


            if (cup.p_LI.strLineName == null || cup.p_LI.strLineName == "")
            {
                cup.InitLineParam();
            }
            if (cup.p_LI.strLinePrefix == null || cup.p_LI.strLineName == "")
            {
                cup.p_LI.strLinePrefix = cup.p_LI.LineNum.ToString() + "#";
            }
            if (cup.p_LI.strCamPrefix == null || cup.p_LI.strCamPrefix.Length == 0)
            {
                cup.p_LI.strCamPrefix = (string[])cupTmp.p_LI.strCamPrefix.Clone();
                cup.p_LI.strPrefix = (string[])cupTmp.p_LI.strPrefix.Clone();
            }
            if( cup.p_BP.intervTimes==0)
            {
                cup.InitBlowParam();
            }
            if(cup.calTP.iRow==0)
            {
                cup.InitCalibTemplateParam();
            }
            if (cup.p_Paf.ManuMarkPath == null || cup.p_Paf.ManuMarkPath == "")
                cup.p_Paf.ManuMarkPath = @"..\..\EZDModel";
            SaveUserParam(cup);

            if (cup.p_RTSP.runTimeOut == 0)
                cup.p_RTSP.runTimeOut = 1000;
            if (cup.p_IOPP.IOTimeDelay == 0)
                cup.p_IOPP.IOTimeDelay = 100;

        }
        public bool ReadUserParam(string strUserParam, ref CUserParam cup)
        {

            FileStream fs;
            try
            {
                //      SaveUserParam(cup);
                //   operLog.strOperEvent = "读取配置参数";
                if (File.Exists(strUserParam))
                {
                    fs = new FileStream(strUserParam, FileMode.Open);
                    BinaryFormatter bf = new BinaryFormatter();
                    cup = bf.Deserialize(fs) as CUserParam;
                    fs.Close();
                    // SaveUserParam(cup);
                }
                else
                {
                    SaveUserParam(cup);
                    return false;
                }
            }
            catch
            {
                // if(f)
                // fs.Close();
                cup = new CUserParam();
                SaveUserParam(cup);

                return false;
            }
            return true;
        }

        //保存用户参数
        public bool SaveUserParam(CUserParam cup)
        {
            try
            {

                FileStream fs = new FileStream("UserData\\serUserParam.dat", FileMode.Create);

                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, cup);
                fs.Close();
                //  operLog.iEventStatus = 0;
            }
            catch(Exception ex)
            {
                return false;
            }
            // AddLog();
            return true;
        }
        #endregion

        #region  保存实时状态数据，用于局域网数据共享        
        public bool SaveRunTimeStatus(string path, RunTimeStatus rts)
        {
            try
            {
                FileStream fs = new FileStream(path, FileMode.Create);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, rts);
                fs.Close();
            }
            catch
            {
                return false;
            }
            return true;
        }
        public bool ReadRunTimeStatus(string strRunTimeStatus, ref RunTimeStatus rts)
        {

            FileStream fs;
            try
            {

                if (File.Exists(strRunTimeStatus))
                {
                    fs = new FileStream(strRunTimeStatus, FileMode.Open);
                    BinaryFormatter bf = new BinaryFormatter();
                    rts = bf.Deserialize(fs) as RunTimeStatus;
                    fs.Close();
                    // SaveUserParam(cup);
                }
                else
                {
                    // SaveUserParam(cup);
                    return false;
                }
            }
            catch
            {
                // if(f)
                // fs.Close();
                // cup = new CUserParam();
                // SaveUserParam(cup);

                return false;
            }
            return true;
        }


        #endregion


        public bool WriteOperLog(string strPath, OperLog operLog)
        {
            try
            {

                FileStream fs = new FileStream(strPath, FileMode.Append);
                if (!File.Exists(strPath))
                    fs = new FileStream(strPath, FileMode.Create);

                StreamWriter sw = new StreamWriter(fs);

                sw.WriteLine("{0} {1} {2} {3}", operLog.dtOperTime, operLog.strOperEvent, operLog.iEventStatus, operLog.strUserName);

                sw.Flush();
                sw.Close();
                fs.Close();


            }
            catch 
            {
                return false;
            }
            return true;
        }
        public bool WriteMark(string strPath, ResultInfo strResInfo)
        {
            try
            {
                if (strResInfo.strMark == "0")
                {
                    FileStream fs = new FileStream(strPath, FileMode.Append);
                    if (!File.Exists(strPath))
                        fs = new FileStream(strPath, FileMode.Create);

                    StreamWriter sw = new StreamWriter(fs);

                    sw.WriteLine("{0} {1} {2} {3}", strResInfo.iInd, strResInfo.strTime, strResInfo.strResCode, strResInfo.strMark);

                    sw.Flush();
                    sw.Close();
                    fs.Close();
                }
                else
                {
                    string[] lines = File.ReadAllLines(strPath);
                    //转换
                    List<string> list = new List<string>();
                    list.AddRange(lines);

                    string strEndLine = (string)list[lines.Length - 1];
                    strEndLine = strEndLine.Substring(0, strEndLine.Length - 1) + strResInfo.strMark;
                    list.RemoveAt(list.Count - 1);
                    list.Add(strEndLine);
                    lines = list.ToArray();
                    //保存
                    File.WriteAllLines(strPath, lines);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        

        public List<string> ReadRemarkImageFile(string strPath)
        {
            List<string> lstImg = new List<string>();
            try
            {
                if (!File.Exists(strPath))
                    return lstImg;
                FileStream fs = new FileStream(strPath, FileMode.Open);


                StreamReader sr = new StreamReader(fs);
                string imgfile;
                while ((imgfile = sr.ReadLine()) != null)
                {
                    lstImg.Add(imgfile);
                }
                sr.Close();
                fs.Close();
                return lstImg;
            }
            catch
            {
                return new List<string>(); ;
            }

        }


    }



}
