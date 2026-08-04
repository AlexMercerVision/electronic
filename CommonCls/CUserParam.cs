using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string strCamName;
        public uint iBrightness;
        public uint iSharp;
        public float dGamma;
        public int iRotateType;
        public int iFlipType;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CameraDistr
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public int[] camInd;
        public bool bTriggle;
        public int triggleCam;
        public int triggleMode;
        public int TriggerActivation;
        public int trigglePosInd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public bool[] bListProcess;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PathAndFile
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string strVPPFilePath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string strSigSavePath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string strConSavePath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string strLogPath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string strResSavePath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string strPlaybackPath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string strErrPath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string MarkPicPath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string AutoMarkPath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string ManuMarkPath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string LaserTemplateFileA;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string LaserTemplateFileB;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string strExportPath;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FXParam
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
    public struct LSParam
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
    public struct VPPRunTimeParam
    {
        public bool bUseFX;
        public bool bAcqFromCam;
        public bool bAcqFromIDB;
        public bool bRecord;
        public bool bSaveRes;
        public bool bRemarkResult;
        public bool bRemarkArea;
        public int iLayoutType;
        public bool bUseWeakCrack;
        public int iXBType;
        public int iFeedbackType;
        public bool bVisionInfer;
        public bool bDeepInfer;
        public bool bFXRestruct;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SubRecordsName
    {
        public int count;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 400)]
        public bool[] bList;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 400)]
        public string[] strName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public int[] subDefaultGUIIndex;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public string[] strDefaultGUIName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public string[] strGreyGUIName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public string[] strFXGUIName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public string[] strPMAGUIName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public string[] strSelectName;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VPPDiagDMParam
    {
        public double greyAbnormCount;
        public double greyAbnormArea;
        public double FXAbnormArea1;
        public double FXAbnormArea2;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VPPDiagCMParam
    {
        public double edgeAbnormCount;
        public double edgeLeftAbnormCount;
        public double edgeRightAbnormCount;
        public double lwArea;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VPPDiagParam
    {
        public VPPDiagDMParam dmParam;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public VPPDiagCMParam[] cmParam;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SerDistrParam
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public int[] serInd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public bool[] bUsed;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] stationCardMap;
        public int BaudRate;
        public int DataBits;
        public int parity;
        public int stopBits;
        public int WriteTimeout;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BlowParam
    {
        public bool bUsed;
        public int blowType;
        public int perBlowTime;
        public int intervType;
        public int intervTimes;
        public int reguType;
        public int perExeTimes;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DLParam
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string modelPath;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string modelType;
        public bool bUseGPU;
        public int gpuID;
        public float fThreshold;
        public float fThresholdCrackGrey;
        public float fThresholdPoszFX;
        public float fThresholdPoszGrey;
        public float fThresholdCrack;
        public float fThresholdPosz;
        public float fIoUTh;
        public int iPoszNum;
        public float fThresholdPoszArea;
        public float fThresholdPoszSide;
        public float fThresholdCrackLen;
    }

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

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LineInfo
    {
        public string strLineName;
        public int LineNum;
        public string strLineDesc;
        public string strLineScence;
        public string strLinePrefix;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public string[] strCamPrefix;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public string[] strPrefix;
        public int bPreType;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IOPortParam
    {
        public int APlane;
        public int BPlane;
        public int EndAPlane;
        public int EndBPlane;
        public int OkOut;
        public int NGOut;
        public int defIn1;
        public int defIn2;
        public int defIn3;
        public int defIn4;
        public int IOTimeDelay;
        public int defOut2;
        public int defOut3;
        public int defOut4;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RunTimeStatusParam
    {
        public bool bAutoConnCam;
        public bool bAutoConnSerial;
        public bool bAutoConnJCZ;
        public bool bAutoConnMotionCard;
        public bool bAutoRunVPP;
        public bool bUpdateImage;
        public bool bUpdateRecord;
        public bool bUpdateLog;
        public bool bUpdateIO;
        public bool bSaveRes;
        public bool bAutoConnDef1;
        public bool bAutoConnDef2;
        public bool bAutoConnDef3;
        public bool bAutoConnDef4;
        public bool bUpdateDef2;
        public bool bUpdateDef3;
        public bool bUpdateDef4;
        public int runTimeOut;
    }
    public struct calipFindLineParam
    {
        public bool bUseTuneLocation;  //左边缘还是右边缘
        public int bPolarity;  //边缘极性
        public double dThre;       //边缘对比度阈值
        public int iRadius;     //边缘滤波半径
        public int iRemovePoint;       //预留参数1
        public int iCaliperNum;       //预留参数2
        public double dRes1;    //预留参数3
        public double dRes2;    //预留参数4
    }

    public struct calipParam
    {
        //  public bool bLeft;  //左边缘还是右边缘
        public int bPolarity;  //边缘极性
        public double dThre;       //边缘对比度阈值
        public int iRadius;     //边缘滤波半径
        public int iRes1;       //预留参数1
        public int iRes2;       //预留参数2
        public double dRes1;    //预留参数3
        public double dRes2;    //预留参数4
    }

    // 主参数类
    [Serializable]
    public class CUserParam
    {
        const int maxCameraNum = 5;
        public const uint DefaultCameraWidth = 5472;
        public const uint DefaultCameraHeight = 3648;
        public const uint LegacyCameraWidth = 3072;
        public const uint LegacyCameraHeight = 2048;
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

        // 以下所有 InitXxx 方法保持不变（与原代码完全相同）
        public void InitCameraDistr()
        {
            p_CD.camInd = new int[maxCameraNum];
            p_CD.bListProcess = new bool[maxCameraNum];
            for (int i = 0; i < maxCameraNum; i++)
            {
                p_CD.camInd[i] = i;
                p_CD.bListProcess[i] = true;
            }
            p_CD.triggleCam = 1;
            p_CD.triggleMode = 1;
            p_CD.TriggerActivation = 0;
            p_CD.trigglePosInd = 1;
        }

        public void InitCameraParam()
        {
            for (int i = 0; i < maxCameraNum; i++)
            {
                p_Cam[i].iMonoType = 0;
                p_Cam[i].iWidth = DefaultCameraWidth;
                p_Cam[i].iHeight = DefaultCameraHeight;
                p_Cam[i].dGain = 0;
                p_Cam[i].iExposeTime = 25000;
                p_Cam[i].iROI = new uint[4];
                p_Cam[i].iROI[0] = 0;
                p_Cam[i].iROI[1] = 0;
                p_Cam[i].iROI[2] = DefaultCameraWidth;
                p_Cam[i].iROI[3] = DefaultCameraHeight;
            }
        }

        public void InitPathAndFile()
        {
            p_Paf.strVPPFilePath = @".\VPP\MDT.vpp";
            p_Paf.strSigSavePath = "SigSave\\";
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
            p_subFX = new FXParam[maxCameraNum - 1];
            for (int i = 0; i < maxCameraNum - 1; i++)
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
            p_VPPRTP.bDeepInfer = false;
            p_VPPRTP.bVisionInfer = true;
            p_VPPRTP.bFXRestruct = true;
        }

        public void InitSubRecordsName()
        {
            p_SRN.count = 0;
            p_SRN.bList = new bool[400];
            p_SRN.strName = new string[400];
            for (int i = 0; i < 400; i++)
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
            p_DP.cmParam = new VPPDiagCMParam[maxCameraNum - 1];
            for (int i = 0; i < maxCameraNum - 1; i++)
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
            p_SD.stationCardMap = new int[2];
            for (int i = 0; i < 4; i++)
            {
                p_SD.serInd[i] = i;
                p_SD.bUsed[i] = true;
            }
            p_SD.stationCardMap[0] = 0;
            p_SD.stationCardMap[1] = 1;
            p_SD.BaudRate = 19200;
            p_SD.DataBits = 8;
            p_SD.parity = (int)Parity.None;
            p_SD.stopBits = (int)StopBits.One;
            p_SD.WriteTimeout = 100;
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
            p_DL.fThresholdCrack = 1.0f;
            p_DL.fThresholdPosz = 1.4f;
            p_DL.fIoUTh = 0.35f;
            p_DL.iPoszNum = 2;
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

            p_CP.fSureNotCrackParam[0] = 10;
            p_CP.fSureNotCrackParam[1] = 20;
            p_CP.fSureNotCrackParam[2] = 0.35f;
            p_CP.fSureNotCrackParam[3] = 0.35f;
            p_CP.fSureNotCrackParam[4] = 150;
            p_CP.fSureNotCrackParam[5] = 150;

            p_CP.fSureCrackParam[1] = 1.7f;

            p_CP.fBlobParam[0] = 6;
            p_CP.fBlobParam[1] = 6;
            p_CP.fBlobParam[2] = 0.35f;
            p_CP.fBlobParam[3] = 0.35f;
            p_CP.fBlobParam[4] = 1.5f;
            p_CP.fBlobParam[5] = 1.5f;

            //光度图参数
            p_CP.fCrackParam[0] = 50;
            p_CP.fCrackParam[1] = 100;
            p_CP.fCrackParam[2] = 0.6f;
            p_CP.fCrackParam[3] = 120;
            p_CP.fCrackParam[4] = 3;
            p_CP.fCrackParam[5] = 25;
            p_CP.fCrackParam[6] = 0.3f;

            //灰度图参数
            p_CP.fCrackParam[10] = 50;
            p_CP.fCrackParam[11] = 100;
            p_CP.fCrackParam[12] = 0.6f;
            p_CP.fCrackParam[13] = 120;
            p_CP.fCrackParam[14] = 3;
            p_CP.fCrackParam[15] = 25;
            p_CP.fCrackParam[16] = 0.3f;

            p_CP.fCrackParam[20] = 100;
            p_CP.fCrackParam[21] = 0.24f;
            p_CP.fCrackParam[22] = 4;
            p_CP.fCrackParam[23] = 20;
            p_CP.fCrackParam[24] = 7;
            p_CP.fCrackParam[25] = 6;
            p_CP.fCrackParam[26] = 100;
            p_CP.fCrackParam[27] = 0.1f;
            p_CP.fCrackParam[28] = 1.3f;
            p_CP.fCrackParam[29] = 2.2f;
            p_CP.fCrackParam[30] = 2;
            p_CP.fCrackParam[31] = p_CP.fCrackParam[22];
            p_CP.fCrackParam[32] = 20;
            p_CP.fCrackParam[33] = 30;
            p_CP.fCrackParam[34] = 0.3f;
            p_CP.fCrackParam[35] = 3.5f;
            p_CP.fCrackParam[36] = 0.3f;
            p_CP.fCrackParam[37] = 40;
            p_CP.fCrackParam[38] = 5;
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
            p_LI.strPrefix = new string[maxCameraNum];
            p_LI.strPrefix[0] = "1#_Main_";
            p_LI.strPrefix[1] = "1#_Sub1_";
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
            p_RTSP.runTimeOut = 1000;
        }
    }

    // ----------------- 以下为非用户参数结构体（运行状态等，不保存至用户文件） -----------------
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CameraStatus
    {
        public int camNum;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public int[] iStatus;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InitStatus
    {
        public bool bUserParamFileInit;
        public bool bModelRoleCert;
        public bool bCamInit;
        public bool bCamPrepared;
        public string strCamInit;
        public bool bSerInit;
        public bool bLocalFileExist;
        public bool bIDBFileExist;
        public bool bDLModelPathExist;
        public bool bVPPFileExist;
        public bool bDLModelInit;
        public bool bVPPInit;
        public int iVPPStatus;
        public bool bLocalFileRegular;
        public int bJCZ;
    }

    public struct ResultInfo
    {
        public int iInd;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string strTime;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string strResCode;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public string strMark;
    }

    public struct OperLog
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string strUserName;
        public DateTime dtOperTime;
        public string strOperEvent;
        public string strFailReason;
        public int iEventStatus;
    }

    public struct DetectionResult
    {
        public int[] iPlaneIndex;
        public string[] strResult;
        public string[] strRemark;
        public int xbType;
    }

    public struct BlocResult
    {
        public int[] iPlaneIndex;
        public string[] strResult;
        public string[] strRemark;
        public int xbType;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DetStat
    {
        public int iTotal;
        public int iOKNum;
        public float iOKPer;
        public int iNGNum;
        public int iWarningNum;
        public int iErrNum;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AcqStat
    {
        public int iTotal;
        public int iLossNum;
        public int iErrNum;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct OnLineStatus
    {
        public bool bRecord;
        public bool bAcqFromCam;
        public bool bSaveRes;
        public bool bTrigg;
        public bool bLock;
        public bool bVisionInfer;
        public bool bDeepInfer;
        public bool bFXRestruct;
        public int iFeedbackType;
    }

    public struct AlignImParam
    {
        public int width;
        public int height;
        public double dCenX;
        public double dCenY;
        public double dWidth;
        public double dHeight;
    }

    [Serializable]
    public class RunTimeStatus
    {
        public DateTime dtLastAcq;
        public DateTime dtLastDet;
        public CameraStatus cs;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public int[] iSerStatus;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public int[] iEventStatus;
        public int iModelRole;
        public int iTaskType;
        public DetStat detS;
        public AcqStat acqS;
        public OnLineStatus onLS;
        public int driveS;
        public InitStatus initStatus;
        public int iIsRunning;

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
    }

    // ----------------- 文件操作类（已改为 JSON 序列化） -----------------
    // ... 前面所有的 struct 定义保持不变 ...

    public class FileOper
    {
        #region JSON序列化/反序列化辅助方法
        private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Converters = new List<JsonConverter> { new StringEnumConverter() }
        };

        private static string SerializeToJson(CUserParam obj)
        {          
            return JsonConvert.SerializeObject(obj, jsonSettings);
        }

        private static CUserParam DeserializeFromJson(string json)
        {
            return JsonConvert.DeserializeObject<CUserParam>(json, jsonSettings);
        }
        #endregion

        #region 旧版二进制文件迁移（一次性）
        public static void MigrateOldBinaryIfNeeded()
        {
            string oldPath = "UserData\\serUserParam.dat";
            string newPath = "UserData\\serUserParam.json";
            if (File.Exists(oldPath) && !File.Exists(newPath))
            {
                try
                {
                    using (FileStream fs = new FileStream(oldPath, FileMode.Open))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        CUserParam oldCup = bf.Deserialize(fs) as CUserParam;
                        if (oldCup != null)
                        {
                            // 执行兼容性检查，补全缺失字段
                            CheckParamCompatibility(oldCup);
                            // 保存为 JSON 格式
                            SaveUserParam(oldCup);
                        }
                        else
                        {
                            // 反序列化得到 null，创建全新参数并保存
                            SaveUserParam(new CUserParam());
                        }
                    }
                }
                catch (Exception)
                {
                    // 迁移失败（如旧文件格式不兼容），创建全新参数保存为 JSON
                    SaveUserParam(new CUserParam());
                }
            }
        }
        #endregion

        #region 读取和保存用户参数
        public bool ReadUserParam(ref CUserParam cup)
        {
            string jsonPath = "UserData\\serUserParam.json";
            MigrateOldBinaryIfNeeded();

            try
            {
                if (File.Exists(jsonPath))
                {
                    string json = File.ReadAllText(jsonPath);
                    cup = DeserializeFromJson(json);
                    CheckParamCompatibility(cup);
                    return true;
                }
                else
                {
                    cup = new CUserParam();
                    SaveUserParam(cup);
                    return false;
                }
            }
            catch (Exception)
            {
                cup = new CUserParam();
                SaveUserParam(cup);
                return false;
            }
        }

        public bool ReadUserParam(string strUserParam, ref CUserParam cup)
        {
            try
            {
                if (File.Exists(strUserParam))
                {
                    string json = File.ReadAllText(strUserParam);
                    cup = DeserializeFromJson(json);
                    CheckParamCompatibility(cup);
                    return true;
                }
                else
                {
                    SaveUserParam(cup);
                    return false;
                }
            }
            catch
            {
                cup = new CUserParam();
                SaveUserParam(cup);
                return false;
            }
        }

        public static bool SaveUserParam(CUserParam cup)
        {
            try
            {
                string dir = "UserData";
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                string jsonPath = Path.Combine(dir, "serUserParam.json");
                string json = SerializeToJson(cup);
                File.WriteAllText(jsonPath, json);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool SaveUserParam(CUserParam cup,string dir)
        {
            try
            {
               // string dir = "UserData";
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                string jsonPath = Path.Combine(dir, "serUserParam.json");
                string json = SerializeToJson(cup);
                File.WriteAllText(jsonPath, json);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region 兼容性检查（原 CheckParam 方法，改为静态）
        private static void CheckParamCompatibility(CUserParam cup)
        {
            CUserParam cupTmp = new CUserParam();
            //2026年6月28日 去掉强制升级
       //     UpgradeLegacyCameraResolution(cup);

            // 相机数组长度兼容
            if (cupTmp.p_Cam.Length > cup.p_Cam.Length)
            {
                for (int i = 0; i < cup.p_Cam.Length; i++)
                    cupTmp.p_Cam[i] = cup.p_Cam[i];
                cup.p_Cam = cupTmp.p_Cam;
            }

            // 子FX参数数组长度兼容
            if (cupTmp.p_subFX.Length > cup.p_subFX.Length)
            {
                for (int i = 0; i < cup.p_subFX.Length; i++)
                    cupTmp.p_subFX[i] = cup.p_subFX[i];
                cup.p_subFX = cupTmp.p_subFX;
            }

            // p_subFX 内部数组长度兼容
           /* for (int i = 1; i < 4; i++)
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
            }*/

            // CameraDistr.camInd 长度兼容
            if (cupTmp.p_CD.camInd.Length > cup.p_CD.camInd.Length)
            {
                for (int i = 0; i < cup.p_CD.camInd.Length; i++)
                    cupTmp.p_CD.camInd[i] = cup.p_CD.camInd[i];
                cup.p_CD.camInd = cupTmp.p_CD.camInd;
            }

            // CameraDistr.bListProcess 长度兼容
            if (cup.p_CD.bListProcess == null || cupTmp.p_CD.bListProcess.Length > cup.p_CD.bListProcess.Length)
            {
                int copyLength = cup.p_CD.bListProcess == null ? 0 : cup.p_CD.bListProcess.Length;
                for (int i = 0; i < copyLength; i++)
                    cupTmp.p_CD.bListProcess[i] = cup.p_CD.bListProcess[i];
                cup.p_CD.bListProcess = cupTmp.p_CD.bListProcess;
            }

            // SubRecordsName 字段数组长度兼容
            if (cup.p_SRN.strDefaultGUIName == null || cupTmp.p_SRN.strDefaultGUIName.Length > cup.p_SRN.strDefaultGUIName.Length)
            {
                int copyLength = cup.p_SRN.strDefaultGUIName == null ? 0 : cup.p_SRN.strDefaultGUIName.Length;
                for (int i = 0; i < copyLength; i++)
                {
                    cupTmp.p_SRN.strDefaultGUIName[i] = cup.p_SRN.strDefaultGUIName[i];
                    cupTmp.p_SRN.strGreyGUIName[i] = cup.p_SRN.strGreyGUIName[i];
                    cupTmp.p_SRN.strFXGUIName[i] = cup.p_SRN.strFXGUIName[i];
                    cupTmp.p_SRN.strPMAGUIName[i] = cup.p_SRN.strPMAGUIName[i];
                }
                cup.p_SRN = cupTmp.p_SRN;
            }

            // 诊断参数 cmParam 数组长度兼容
            if (cupTmp.p_DP.cmParam.Length > cup.p_DP.cmParam.Length)
            {
                for (int i = 0; i < cup.p_DP.cmParam.Length; i++)
                    cupTmp.p_DP.cmParam[i] = cup.p_DP.cmParam[i];
                cup.p_DP = cupTmp.p_DP;
            }

            // p_LS2 数组长度兼容
            if (cupTmp.p_LS2.Length > cup.p_LS2.Length)
            {
                for (int i = 0; i < cup.p_LS2.Length; i++)
                    cupTmp.p_LS2[i] = cup.p_LS2[i];
                cup.p_LS2 = cupTmp.p_LS2;
            }

            // 串口参数兼容
            if (cup.p_SD.serInd == null || cup.p_SD.BaudRate == 0)
                cup.p_SD = cupTmp.p_SD;
            if (cup.p_SD.stationCardMap == null || cup.p_SD.stationCardMap.Length < 2)
                cup.p_SD.stationCardMap = cupTmp.p_SD.stationCardMap;
            for (int i = 0; i < cup.p_SD.stationCardMap.Length && i < 2; i++)
            {
                if (cup.p_SD.stationCardMap[i] < 0 || cup.p_SD.stationCardMap[i] > 1)
                    cup.p_SD.stationCardMap[i] = i;
            }
            if (cup.p_SD.stationCardMap.Length >= 2 && cup.p_SD.stationCardMap[0] == cup.p_SD.stationCardMap[1])
            {
                cup.p_SD.stationCardMap[0] = 0;
                cup.p_SD.stationCardMap[1] = 1;
            }

            // 深度学习参数兼容
            if (cup.p_DL.modelPath == null || cup.p_DL.modelPath == "")
                cup.InitDLParam();
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

            // 匣钵类型兼容
            if (cup.p_VPPRTP.iXBType == 0)
                cup.p_VPPRTP.iXBType = cupTmp.p_VPPRTP.iXBType;

            // 裂纹参数数组兼容
            if (cup.p_CP.fBlobParam == null)
                cup.p_CP.fBlobParam = (float[])cupTmp.p_CP.fBlobParam.Clone();
            if (cup.p_CP.fSureNotCrackParam == null)
                cup.p_CP.fSureNotCrackParam = (float[])cupTmp.p_CP.fSureNotCrackParam.Clone();
            if (cup.p_CP.fPoszParam == null)
                cup.p_CP.fPoszParam = (float[])cupTmp.p_CP.fPoszParam.Clone();
            if (cup.p_CP.fQPParam == null)
                cup.p_CP.fQPParam = (float[])cupTmp.p_CP.fQPParam.Clone();
            if (cup.p_CP.fSureCrackParam == null)
                cup.p_CP.fSureCrackParam = (float[])cupTmp.p_CP.fSureCrackParam.Clone();
            if (cup.p_CP.fCrackParam == null)
                cup.p_CP.fCrackParam = (float[])cupTmp.p_CP.fCrackParam.Clone();

            // 产线信息兼容
            if (string.IsNullOrEmpty(cup.p_LI.strLineName))
                cup.InitLineParam();
            if (string.IsNullOrEmpty(cup.p_LI.strLinePrefix))
                cup.p_LI.strLinePrefix = cup.p_LI.LineNum.ToString() + "#";
            if (cup.p_LI.strCamPrefix == null || cup.p_LI.strCamPrefix.Length == 0)
            {
                cup.p_LI.strCamPrefix = (string[])cupTmp.p_LI.strCamPrefix.Clone();
                cup.p_LI.strPrefix = (string[])cupTmp.p_LI.strPrefix.Clone();
            }

            // 吹气参数兼容
            if (cup.p_BP.intervTimes == 0)
                cup.InitBlowParam();

            // 标定模板兼容
            if (cup.calTP.iRow == 0)
                cup.InitCalibTemplateParam();

            // 标注路径兼容
            if (string.IsNullOrEmpty(cup.p_Paf.ManuMarkPath))
                cup.p_Paf.ManuMarkPath = @"..\..\EZDModel";
            if (string.IsNullOrEmpty(cup.p_Paf.LaserTemplateFileA))
                cup.p_Paf.LaserTemplateFileA = "route0.ezd";
            if (string.IsNullOrEmpty(cup.p_Paf.LaserTemplateFileB))
                cup.p_Paf.LaserTemplateFileB = "route1.ezd";

            // 运行时超时兼容
            if (cup.p_RTSP.runTimeOut == 0)
                cup.p_RTSP.runTimeOut = 1000;

            // IO延时兼容
            if (cup.p_IOPP.IOTimeDelay == 0)
                cup.p_IOPP.IOTimeDelay = 100;

            // 保存兼容后的参数（覆盖旧文件）
            SaveUserParam(cup);
        }
        #endregion

        #region 其他文件操作方法（保存实时状态、操作日志等）
        private static void UpgradeLegacyCameraResolution(CUserParam cup)
        {
            if (cup.p_Cam == null)
                return;

            for (int i = 0; i < cup.p_Cam.Length; i++)
            {
                if (cup.p_Cam[i].iROI == null || cup.p_Cam[i].iROI.Length < 4)
                    cup.p_Cam[i].iROI = new uint[4];

                bool isLegacyResolution =
                    cup.p_Cam[i].iWidth == CUserParam.LegacyCameraWidth &&
                    cup.p_Cam[i].iHeight == CUserParam.LegacyCameraHeight;

                bool isLegacyFullRoi =
                    cup.p_Cam[i].iROI[0] == 0 &&
                    cup.p_Cam[i].iROI[1] == 0 &&
                    cup.p_Cam[i].iROI[2] == CUserParam.LegacyCameraWidth &&
                    cup.p_Cam[i].iROI[3] == CUserParam.LegacyCameraHeight;

                if (isLegacyResolution)
                {
                    cup.p_Cam[i].iWidth = CUserParam.DefaultCameraWidth;
                    cup.p_Cam[i].iHeight = CUserParam.DefaultCameraHeight;
                }

                if (isLegacyFullRoi)
                {
                    cup.p_Cam[i].iROI[2] = CUserParam.DefaultCameraWidth;
                    cup.p_Cam[i].iROI[3] = CUserParam.DefaultCameraHeight;
                }
            }
        }

        public bool SaveRunTimeStatus(string path, RunTimeStatus rts)
        {
            try
            {
                string json = JsonConvert.SerializeObject(rts, jsonSettings);
                File.WriteAllText(path, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ReadRunTimeStatus(string strRunTimeStatus, ref RunTimeStatus rts)
        {
            try
            {
                if (File.Exists(strRunTimeStatus))
                {
                    string json = File.ReadAllText(strRunTimeStatus);
                    rts = JsonConvert.DeserializeObject<RunTimeStatus>(json, jsonSettings);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool WriteOperLog(string strPath, OperLog operLog)
        {
            try
            {
                FileStream fs;
                if (!File.Exists(strPath))
                    fs = new FileStream(strPath, FileMode.Create);
                else
                    fs = new FileStream(strPath, FileMode.Append);
                using (fs)
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine("{0} {1} {2} {3}", operLog.dtOperTime, operLog.strOperEvent, operLog.iEventStatus, operLog.strUserName);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool WriteMark(string strPath, ResultInfo strResInfo)
        {
            try
            {
                if (strResInfo.strMark == "0")
                {
                    FileStream fs;
                    if (!File.Exists(strPath))
                        fs = new FileStream(strPath, FileMode.Create);
                    else
                        fs = new FileStream(strPath, FileMode.Append);
                    using (fs)
                    {
                        using (StreamWriter sw = new StreamWriter(fs))
                        {
                            sw.WriteLine("{0} {1} {2} {3}", strResInfo.iInd, strResInfo.strTime, strResInfo.strResCode, strResInfo.strMark);
                        }
                    }
                }
                else
                {
                    string[] lines = File.ReadAllLines(strPath);
                    List<string> list = new List<string>(lines);
                    string strEndLine = list[lines.Length - 1];
                    strEndLine = strEndLine.Substring(0, strEndLine.Length - 1) + strResInfo.strMark;
                    list.RemoveAt(list.Count - 1);
                    list.Add(strEndLine);
                    File.WriteAllLines(strPath, list.ToArray());
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<string> ReadRemarkImageFile(string strPath)
        {
            List<string> lstImg = new List<string>();
            try
            {
                if (!File.Exists(strPath))
                    return lstImg;
                using (FileStream fs = new FileStream(strPath, FileMode.Open))
                using (StreamReader sr = new StreamReader(fs))
                {
                    string imgfile;
                    while ((imgfile = sr.ReadLine()) != null)
                        lstImg.Add(imgfile);
                }
                return lstImg;
            }
            catch
            {
                return new List<string>();
            }
        }
        #endregion
    }
    // 以下为其他兼容性结构（原代码末尾保留）
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct calibTemplateParam
    {
        public int iRow;
        public int iCol;
        public double dRadius;
        public double dDisX;
        public double dDisY;
        public double dCentX;
        public double dCentY;
    }

    public struct PMARes
    {
        public bool bLocated;
        public bool bOk;
        public double dTX;
        public double dTY;
        public double dTR;
        public double dPMAScore;
    }

    public struct CalibRes
    {
        public bool bCalib;
        public double dTX;
        public double dTY;
        public double dTR;
        public double dXYRatio;
        public double dSkew;
        public double dScale;
        public double dErr;
    }

    public struct calibBlobParam
    {
        public bool bPolarity‌;
        public int iSegType;
        public int iThre;
        public int iAreaMin;
        public int iAreaMax;
    }

    public struct PMAlignParam
    {
        public int TrainAlg;
        public bool TrainPolarity;
        public bool TrainAutoEdgeThre;
        public double TrainEdgeThre;
        public bool TrainAutoGrun;
        public double TrainCoarse;
        public double TrainFine;
        public int RunAlg;
        public bool RunArt;
        public bool RunAutoEdgeThre;
        public double RunEdgeThre;
        public double RunConstractThre;
        public int RunSearchNum;
        public double RunScoreThre;
        public bool RunEnableAngle;
        public double RunAngleD;
        public double RunAngleU;
    }
}
