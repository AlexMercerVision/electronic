using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Pipes;
using System.Management;
using System.Xml;

using HalconDotNet;
//using HOperatorSet_EX;
using System.Runtime.InteropServices;
 
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.IO.Ports;
/*using System.IO.Pipes;

using OpenCvSharp;
using OpenCvSharp.Extensions;*/

using MvCamCtrl.NET;  // 海康相机
//using gRPC_ClientAPP;
using System.Diagnostics;
 
using CameraHandle = System.Int32;
using CommonCls;
//using PipeChanel;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.Blob;

namespace VisionApplication
{


    public partial class FrmMainUI : Form  
    {
        Frm_Welcome fw;
        
        Protect mPasswordFile = new Protect("C:\\Windows\\authentication.txt");

        //用户参数
        CUserParam cup = new CUserParam();
        //文件操作类
        FileOper fo = new FileOper();
        //操作日志
        OperLog operLog = new OperLog();

        //VP检测结果
        DetectionResult dr = new DetectionResult();


        //串口操作相关
        CSerialDef csd = new CSerialDef();
        SerialPort sPortMain = new SerialPort();    //主光源控制器串口
        SerialPort[] sPortSub = new SerialPort[2];  //侧面光源控制器串口
        SerialPort sPortCy = new SerialPort();       //IO口通信串口
        int[] serialInd = { 0, 1, 2, 3 };
        SerialPort[] sPortAll = new SerialPort[4];  //全部串口放在一个变量数组里面管理

        //执行检测相关

        int iFileIndex = 0, iNextFileIndex = 0;
        int iFileGroupLen = 0;
        int iPerGroupLen = 1;   //每组图像文件数量，  一般情况为1， 光度立体图为6

        //  string[] strDirectSub = { "Main", "Sub1", "Sub2", "Sub3", "Sub4" };
        string[][] strFiles = new string[maxCameraNum][];  //回放文件数组

        #region  执行线程
        Thread contThread = null, oneTimeThread = null;     //连续执行与单次执行线程
        Thread UpdateThread = null;                         //更新VP记录清单线程
        Thread fxRestThread = null;
        private delegate void UpdateUI();                   // 声明委托，用于更新界面
        Thread UIStartThread = null;                        //启动界面线程 
        Thread hikGrabThread = null;                        //图像采集线程
        Thread LaserMarkThread = null;
        Thread jczPipThread = null;
        Thread queryEntThread = null;
        Thread ReLoadThread = null;
        #endregion

        #region  图像采集相关
        bool chooseHIK, hikCanGrab;
        ImgAcqFIFO acqFIFO = new ImgAcqFIFO();              //图像采集封装类
        HIKVisionCamera hIKVisionCamera = new HIKVisionCamera();  //海康相机操作类
        //Hik 相机相关参数
        MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();  // 定义输出帧信息结构体

        MyCamera[] camera2 = null;

        MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
        MyCamera.MV_FRAME_OUT stFrameInfo1 = new MyCamera.MV_FRAME_OUT();
        int nRet = 0;
        UInt32 nPayloadSize = 0;
        int iCamType = 0;

        List<string> items;                                 //相机名称



        //采集公共变量

        IntPtr[] m_BufForDriverS = new IntPtr[maxCameraNum];    //采集数据缓存区
        UInt32[] nPayloadSizeS = new UInt32[maxCameraNum];      //采集数据长度
        int mainWindWidth, mainWindHeight;
        UInt32[] m_nBufSizeForDriver = new UInt32[maxCameraNum];
        IntPtr m_BufForDriver;


        HObject hobjImg = new HObject();
        #endregion  图像采集相关

        #region   状态及标志相关
        const int maxCameraNum = 5;
        uint cameraNum = 0;
        int iUsedCamraNum = 2;

        int[] iEventStatus = new int[6];
        string[] strStatus = new string[12];
        //   int iTaskType = -1;      //0：采集单帧图像  1：实时预览图像  2：单帧运行   3：连续运行
        //  CameraStatus cs = new CameraStatus();   //相机状态


        //  bool bTrigg = false;  //启用触发模式
        // int iTaskType = 0;
        int iShowType = 0;  //0:默认显示，1：灰度图显示  2：光度图显示
                            //    private string[] strSubRecordsName;
                            //  public int iModelRole = 0;  //用户身份

        //  int iVPPStatus = 0;  //VPP文件是否正确加载 
        int iCurrentIndex, iNextIndex;
        int iLayoutType = 1;

        bool bVPPParamChanged = false;
        //   int iRunStatus = 0;            //运行状态
        //   bool bFeedback = true;
        //    int iFeedbackType = 0;   //0：不反馈   1：实际结果  2：OK  3：NG 
        // bool bDeepInfer = true; //是否启用深度推理  检测VP算法时可以关闭深度推理 
        // InitStatus initStatus = new InitStatus();
        RunTimeStatus rts = new RunTimeStatus();   //运行状态，用于向控制中心传达当前系统的实时状态

        AlignImParam alImParam = new AlignImParam();
        #endregion   状态及标志相关


        //获取记录清单

        /// <summary 日志记录相关
        ResultInfo strResInfo = new ResultInfo();
        /// </summary>
        /// 



        //更新VP记录，在VP模块发生改变时需要手动更新一次VP图片记录
        private void InitSubRecordsList()
        {
            runandupdate();
        }

        //保存用户参数
        private void SaveUserParam()
        {
            try
            {
                bool bStatus = fo.SaveUserParam(cup);
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


        #region 初始化相关
        //窗口布局初始化  主窗体加载以及窗体大小调整时调用
        private void ViewLayout()
        {
            mainWindWidth = splitContainer3.Panel1.Width;
            mainWindHeight = splitContainer3.Panel1.Height;
            splitContainer4.SplitterDistance = 100;
            splitContainer5.Panel1Collapsed = false;
            //**20220409  主窗口大小调整
            if (vpWin != null)
            {
                vpWin.Width = mainWindWidth;
                vpWin.Height = mainWindHeight;
                vpWin.ViewLayout(iLayoutType); //调整控件分布
            }            
        }

        //日志显示控件初始化
        private void InitLog()
        {
            /* operLog.strUserName = "st";
             operLog.iEventStatus = 0;
             dgvLog.FirstDisplayedScrollingRowIndex = dgvLog.Rows.Count - 1;
             //  dgvEventLog.Rows[0].Visible=false;

             for(int i=0;i<maxCameraNum;i++)
             {
                 if(!cup.p_CD.bListProcess[i])
                 {
                     dgvEventLog.Columns[i + 1].Visible = false;
                 }
             }  
             dr.iPlaneIndex = new int[maxCameraNum];
             dr.strResult = new string[maxCameraNum];
             dr.strRemark = new string[maxCameraNum];*/
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
        //VisionControl初始化
        bool bVPWinAttached = false;  //视图控件是否已经加载进来
     
        private void LoadVPFile()  //2025年10月4日  修改
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
                    this.vpWin = new VisionControl.VisionControl(strVPPFile);
                   
                    rts.initStatus.iVPPStatus = vpWin.mInitError; //vpp文件初始化错误代码
                                                                  //    vpWin.cdl = cdl;
                    if(rts.initStatus.iVPPStatus==0)
                    {
                        rts.initStatus.bVPPInit = true;  //vpp文件完成初始化
                        if (!bVPWinAttached)
                        {
                            bVPWinAttached = true;
                            this.splitContainer3.Panel1.Controls.Add(this.vpWin);
                        }

                        this.vpWin.Location = new System.Drawing.Point(-2, 6);
                        this.vpWin.Name = "XB视图";
                        this.vpWin.Size = new System.Drawing.Size(393, 357);
                        this.vpWin.TabIndex = 2;

                        //2025年8月26日
                        vpWin.GetAlignRegion(ref alImParam);
                    }
                    else
                    {
                        rts.initStatus.bVPPInit = false;
                        operLog.strOperEvent = "加载视觉算法文件";
                        operLog.iEventStatus = -1;
                        operLog.strFailReason =  VPPinitErr(rts.initStatus.iVPPStatus);
                        AddLog();
                    }
                   
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
        }

        //采集类初始化
        private void InitAcqFIFOParam()
        {
            acqFIFO._Buff = m_BufForDriver;
            acqFIFO._PayloadSize = nPayloadSize;
            acqFIFO.SetBuff(m_BufForDriverS, nPayloadSizeS);
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
                //   acqFIFO.cdl = cdl;

                vpWin._cup = cup;
                vpWin.SetGUIShow(0);
                vpWin._cameraNum = cameraNum;
                vpWin.SetAcqFIFO(acqFIFO);

                vpWin.InitBG();
                operLog.iEventStatus = 0;
            }
            catch
            {
                operLog.iEventStatus = -1;
            }

            operLog.strOperEvent = "VP模型初始化";
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
                    //   iRet = cdl.InitDL(cup.p_DL);
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
                tmppath = curDriver + cup.p_Paf.strErrPath.Substring(1);
                if (File.Exists(tmppath))
                {
                    cup.p_Paf.strErrPath = tmppath;
                }
                tmppath = curDriver + cup.p_Paf.strExportPath.Substring(1);
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
            ReadUserParam();

            ReplaceDrive();  //目录不存在时的处理
            //      cup.p_VPPRTP.iXBType = 0;
            iUsedCamraNum = 0;

            for (int i = 0; i < maxCameraNum; i++)
            {
                if (cup.p_CD.bListProcess[i])
                    iUsedCamraNum++;
            }
            iLayoutType = cup.p_VPPRTP.iLayoutType;
            if (iLayoutType != 3 && iLayoutType != 5 && iLayoutType != 1)
                iLayoutType = 3;
            rts.cs.iStatus = new int[maxCameraNum];
            serialInd = cup.p_SD.serInd;

            switch (cup.p_VPPRTP.iXBType)
            {
                case 0:
                    menuXBType_1.Checked = true;
                    break;
                case 1:
                    menuXBType_2.Checked = true;
                    break;
                case 9:
                    menuXBType_9.Checked = true;
                    break;
            }
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
     //   OnLineStatus onLS = new OnLineStatus();
        DetStat detStat = new DetStat();

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
                operLog.strOperEvent = "创建文件夹";
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
            if (operLog.iEventStatus == -1)
                AddLog();

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
            vpWin._cameraNum = cameraNum;
        }

        private void InitCamLog()
        {
            /*if (rts.initStatus.bCamInit)
            {
                if (cameraNum > 0)
                {
                    operLog.strOperEvent = "查找到" + cameraNum + "个HikVision相机";
                    operLog.iEventStatus = 0;
                }
                else
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "查找相机";
                    operLog.strFailReason= "未找到可用相机";
                }
            }
            else
            {
                operLog.strOperEvent = "查找相机";
                operLog.iEventStatus = -1;

            }
            AddLog();*/
            /*if (cameraNum < 1)
            {
                toolStripStatus_CamConnectStatus.Text = "相机未连接";
                toolStripStatus_CamConnectStatus.ForeColor = Color.Red;
                // return;
            }*/

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
                {
                    continue;
                }
                if (m_BufForDriverS[iCamInd] == IntPtr.Zero && iCamInd < cameraNum)
                {
                    operLog.strOperEvent = "相机初始化";
                    operLog.strFailReason = "获取图像帧缓冲失败";
                    operLog.iEventStatus = -1;
                    AddLog();
                }
                else if (m_BufForDriverS[iCamInd] != IntPtr.Zero)
                {
                    AddLog();
                }

            }
        }

        //初始化单个相机， 轮询检测过程使用
        private bool InitCamInd(int ind)
        {
            bool bSucc = false;
            int iCamInd = cup.p_CD.camInd[ind];
            // if (cup.p_CD.bListProcess[i] && iCamInd < cameraNum)
            {
                if (cup.p_Cam[iCamInd].strCamName != items[iCamInd])
                {
                    cup.p_Cam[iCamInd].strCamName = items[iCamInd];
                    // rts.cs.iStatus[iCamInd] = -1;  //相机序列号有变化，需要提示
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = "相机" + (ind + 1).ToString() + "发生更换";
                    operLog.strFailReason = cup.p_Cam[iCamInd].strCamName + "->" + items[iCamInd];
                    AddLog();

                }
            }

            camera2 = hIKVisionCamera.CameraInit(iCamInd, ref rts.cs.iStatus);

            //获取图像大小
            if (camera2[iCamInd].MV_CC_GetIntValue_NET("PayloadSize", ref stParam) != 0)
            {
                iCamType = 1;
                nRet = camera2[iCamInd].MV_CC_GetImageBuffer_NET(ref stFrameInfo1, 10000);
                nPayloadSizeS[iCamInd] = stFrameInfo1.stFrameInfo.nFrameLen;
            }
            else
            {
                nRet = MyCamera.MV_OK;
                nPayloadSizeS[iCamInd] = stParam.nCurValue;
            }

            if (MyCamera.MV_OK != nRet)
            {  //获取图像大小信息失败  
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "检测位" + (ind + 1).ToString() + "获取相机数据量";
                operLog.strFailReason = "异常错误";
                AddLog();
                return false;
            }
            else
            {
                rts.cs.iStatus[iCamInd] = 1;

            }

            if (!SetCamParam(iCamInd))  //设置参数
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "设置相机参数";
                operLog.strFailReason = "异常错误";
                AddLog();
                return false;
            }
            else
            {
                rts.cs.iStatus[iCamInd] = 2;
            }

            //分配数据空间
            if (nPayloadSizeS[iCamInd] > m_nBufSizeForDriver[iCamInd])
            {
                if (m_BufForDriverS[iCamInd] != IntPtr.Zero)
                {
                    Marshal.Release(m_BufForDriverS[iCamInd]);
                }
                m_nBufSizeForDriver[iCamInd] = nPayloadSizeS[iCamInd];
                m_BufForDriverS[iCamInd] = Marshal.AllocHGlobal((Int32)m_nBufSizeForDriver[iCamInd]);
            }

            if (m_BufForDriverS[iCamInd] == IntPtr.Zero)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "分配数据空间";
                operLog.strFailReason = "异常错误";
                AddLog();
            }
            else
            {
                rts.cs.iStatus[iCamInd] = 3;
            }
            return bSucc;

        }

        private void ProcessInitCamStatus(int iInitType)
        {
            switch (iInitType)
            {
                case 0:     // 0：正常初始化
                    rts.initStatus.bCamPrepared = true;
                    rts.initStatus.bCamInit = true;
                    rts.initStatus.strCamInit = "没有可用相机";
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = "初始化相机";

                    break;
                case -1:
                    rts.initStatus.bCamPrepared = false;
                    rts.initStatus.bCamInit = false;
                    rts.initStatus.strCamInit = "没有可用相机";

                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "初始化相机";
                    operLog.strFailReason = "没有可用相机";

                    break;
                case -2:         //选取相机编号超出了可用相机数量
                    rts.initStatus.bCamPrepared = false;
                    rts.initStatus.bCamInit = false;
                    rts.initStatus.strCamInit = "选取相机编号超出范围";
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "初始化相机";
                    operLog.strFailReason = "选取相机编号超出范围";

                    break;
                case -3:
                    rts.initStatus.bCamPrepared = true;
                    rts.initStatus.bCamInit = false;
                    rts.initStatus.strCamInit = "获取相机数据量大小失败";
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "初始化相机";
                    operLog.strFailReason = "获取相机数据量大小失败";
                    break;
                case -4:
                    rts.initStatus.bCamPrepared = true;
                    rts.initStatus.bCamInit = false;
                    rts.initStatus.strCamInit = "初始化相机参数失败";
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "初始化相机";
                    operLog.strFailReason = "初始化相机参数失败";
                    break;
                case -5:
                    rts.initStatus.bCamPrepared = true;
                    rts.initStatus.bCamInit = false;
                    rts.initStatus.strCamInit = "分配数据空间失败";
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "初始化相机";
                    operLog.strFailReason = "分配数据空间失败";
                    break;
                default:
                    rts.initStatus.bCamPrepared = false;
                    rts.initStatus.bCamInit = false;
                    rts.initStatus.strCamInit = "异常错误";
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "初始化相机";
                    operLog.strFailReason = "异常错误";

                    break;
            }
            AddLog();

            //更新相机列表信息
            cbDeviceList.Items.Clear();
            for (int i = 0; i < maxCameraNum; i++)
            {
                if (cup.p_CD.bListProcess[i] && cup.p_CD.camInd[i] < cameraNum)
                {
                    cbDeviceList.Items.Add(items[i]);  //添加相机列表
                    if (cup.p_Cam[cup.p_CD.camInd[i]].strCamName != items[i])  //更新相机名称
                    {
                        cup.p_Cam[cup.p_CD.camInd[i]].strCamName = items[i];

                        operLog.iEventStatus = 0;
                        operLog.strOperEvent = "相机" + (i + 1).ToString() + "发生更换";
                        operLog.strFailReason = cup.p_Cam[cup.p_CD.camInd[i]].strCamName + "->" + items[i];
                        AddLog();
                    }
                }
            }

            //显示相机状态信息
            for (int ind = 0; ind < maxCameraNum; ind++)
            {
                //如果不启用，则跳过，否则对相应相机进行初始化
                if (!cup.p_CD.bListProcess[ind])
                {
                    ((ToolStripStatusLabel)statusStrip1.Items[ind + 1]).ForeColor = Color.Gray;
                    continue;
                }

                int iCamInd = cup.p_CD.camInd[ind];   //第ind检测位对应的相机序号

                if (rts.cs.iStatus[iCamInd] == 2)
                {
                    operLog.strOperEvent = items[iCamInd] + "-" + (iCamInd + 1) + "分辨率：" + cup.p_Cam[iCamInd].iWidth.ToString() + "×" + cup.p_Cam[iCamInd].iHeight.ToString();
                    operLog.iEventStatus = 0;
                }

                if (rts.cs.iStatus[iCamInd] == 3)
                    ((ToolStripStatusLabel)statusStrip1.Items[iCamInd + 1]).ForeColor = Color.Green;

                else if (rts.cs.iStatus[iCamInd] == 2)
                    ((ToolStripStatusLabel)statusStrip1.Items[iCamInd + 1]).ForeColor = Color.Orange;

                else
                    ((ToolStripStatusLabel)statusStrip1.Items[iCamInd + 1]).ForeColor = Color.Red;

            }

        }

        //初始化摄像头
        private void InitCam()
        {
            int iInitType = 0; //0：正常初始化   -1：没有可用相机

            if (cameraNum < 1)
            {
                iInitType = -1;
                goto outofInit;
            }

            try
            {
                bool bNormal = true;
                // 枚举相机,获取相机名称
                items = hIKVisionCamera.EnumDevices();
                int iNeedCamNum = 0;
                for (int i = 0; i < maxCameraNum; i++)
                {
                    if (cup.p_CD.bListProcess[i])  //相机编号选择有误
                    {
                        iNeedCamNum++;              //启动的检测位数量
                        if (cup.p_CD.camInd[i] >= cameraNum)
                            bNormal = false;

                    }
                }
                //相机数量少于检测位数或者 检测面选择的相机编号大于相机数量
                if (!bNormal || iNeedCamNum > cameraNum)
                {
                    iInitType = -2;
                    goto outofInit;
                }

                //创建相机连接
                camera2 = hIKVisionCamera.MultiCameraInit((int)cameraNum, ref rts.cs.iStatus);  //多相机初始化20220409 20220503

                string strError = "";
                int iCamInd = 0;

                //按照检测位逐个初始化相机

                bNormal = true;
                //先检测相机数据量是否正常
                for (int ind = 0; ind < maxCameraNum; ind++)
                {
                    //如果不启用，则跳过，否则对相应相机进行初始化
                    if (!cup.p_CD.bListProcess[ind])
                    {
                        continue;
                    }
                    iCamInd = cup.p_CD.camInd[ind];   //第ind检测位对应的相机序号

                    //获取图像大小
                    if (camera2[iCamInd].MV_CC_GetIntValue_NET("PayloadSize", ref stParam) != 0)
                    {
                        iCamType = 1;
                        nRet = camera2[iCamInd].MV_CC_GetImageBuffer_NET(ref stFrameInfo1, 10000);
                        nPayloadSizeS[iCamInd] = stFrameInfo1.stFrameInfo.nFrameLen;
                    }
                    else
                    {
                        nRet = MyCamera.MV_OK;
                        nPayloadSizeS[iCamInd] = stParam.nCurValue;
                    }

                    if (MyCamera.MV_OK != nRet)
                    {  //获取图像大小信息失败 
                        bNormal = false;
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = "检测位" + (ind + 1).ToString() + "获取相机数据量";
                        operLog.strFailReason = "异常错误";
                        AddLog();
                    }
                    else
                    {
                        rts.cs.iStatus[iCamInd] = 1;

                    }
                }
                if (!bNormal)
                {
                    iInitType = -3;
                    goto outofInit;
                }


                bNormal = true;
                //对相机进行初始化设置
                for (int ind = 0; ind < maxCameraNum; ind++)
                {
                    //如果不启用，则跳过，否则对相应相机进行初始化
                    if (!cup.p_CD.bListProcess[ind])
                    {
                        continue;
                    }
                    iCamInd = cup.p_CD.camInd[ind];   //第ind检测位对应的相机序号

                    if (!SetCamParam(iCamInd))  //设置参数
                    {
                        bNormal = false;
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = "设置相机参数";
                        operLog.strFailReason = "异常错误";
                        AddLog();
                    }
                    else
                    {
                        rts.cs.iStatus[iCamInd] = 2;
                    }
                }
                if (!bNormal)
                {
                    iInitType = -4;
                    goto outofInit;
                }


                bNormal = true;

                //分配数据存储空间
                for (int ind = 0; ind < maxCameraNum; ind++)
                {
                    //如果不启用，则跳过，否则对相应相机进行初始化
                    if (!cup.p_CD.bListProcess[ind])
                    {
                        continue;
                    }
                    iCamInd = cup.p_CD.camInd[ind];   //第ind检测位对应的相机序号

                    if (nPayloadSizeS[iCamInd] > m_nBufSizeForDriver[iCamInd])
                    {
                        if (m_BufForDriverS[iCamInd] != IntPtr.Zero)
                        {
                            Marshal.Release(m_BufForDriverS[iCamInd]);
                        }
                        m_nBufSizeForDriver[iCamInd] = nPayloadSizeS[iCamInd];
                        m_BufForDriverS[iCamInd] = Marshal.AllocHGlobal((Int32)m_nBufSizeForDriver[iCamInd]);
                    }

                    if (m_BufForDriverS[iCamInd] == IntPtr.Zero)
                    {
                        bNormal = false;
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = "分配数据空间";
                        operLog.strFailReason = "异常错误";
                        AddLog();
                    }
                    else
                    {
                        rts.cs.iStatus[iCamInd] = 3;
                    }
                }
                if (!bNormal)
                {
                    iInitType = -5;
                    goto outofInit;
                }
            }
            catch
            {
                iInitType = -6;
            }

        outofInit:
            ProcessInitCamStatus(iInitType);


        }
        //关闭设备
        private void StopDevice()
        {
            // 关闭海康相机
            if (chooseHIK)
            {
                try
                {
                    // 取流标志位清零
                    if (hikCanGrab == true)
                    {
                        hikCanGrab = false;
                        hikGrabThread.Join();
                    }
                    if (m_BufForDriver != IntPtr.Zero)
                    {
                        Marshal.Release(m_BufForDriver);
                    }

                    hIKVisionCamera.MultiDestroyCamera();
                }
                catch
                {
                    MessageBox.Show("关闭相机失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            // 关闭basler相机

        }
        //停止采集
        private void StopGrab()
        {
            if (chooseHIK)
            {
                try
                {
                    hikCanGrab = false;   // 标志位设为false
                    vpWin.OpenOrCloseCam(false);  //停止预览线程，并未关闭相机的采集
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

            /*  toolStripStatus_CamConnectStatus.Text = "相机未连接";
              toolStripStatus_CamConnectStatus.ForeColor = Color.Red;
              toolStripStatus_Cam1.ForeColor = Color.Red;
              toolStripStatus_Cam2.ForeColor = Color.Red;
              toolStripStatus_Cam3.ForeColor = Color.Red;
              toolStripStatus_Cam4.ForeColor = Color.Red;
              toolStripStatus_Cam5.ForeColor = Color.Red;*/
        }
        private void CloseCam()
        {
            StopGrab();
            StopDevice();
            CloseCamStatus();
        }

        private bool SetCamParam(int iCamIndex)
        {
            string gainshow = null;
            string exposureshow = null;
            uint iWidth, iHeight;

            iWidth = (uint)cup.p_Cam[iCamIndex].iWidth;
            iHeight = (uint)cup.p_Cam[iCamIndex].iHeight;

            try
            {
                if (chooseHIK)
                {
                    float exposure = cup.p_Cam[iCamIndex].iExposeTime;
                    float gain = (float)cup.p_Cam[iCamIndex].dGain;
                    hIKVisionCamera.SetParam(gain, exposure, ref gainshow, ref exposureshow, camera2[iCamIndex]);

                    hIKVisionCamera.SetPixelFormat(ref cup.p_Cam[iCamIndex].iMonoType, camera2[iCamIndex]);

                    hIKVisionCamera.SetResolution(ref iWidth, ref iHeight, camera2[iCamIndex]);


                    int ret = camera2[iCamIndex].MV_CC_SetIntValue_NET("OffsetX", (uint)cup.p_Cam[iCamIndex].iROI[0]);
                    ret = camera2[iCamIndex].MV_CC_SetIntValue_NET("OffsetY", (uint)cup.p_Cam[iCamIndex].iROI[1]);
                    ret = camera2[iCamIndex].MV_CC_SetIntValue_NET("Width", (uint)cup.p_Cam[iCamIndex].iROI[2]);
                    ret = camera2[iCamIndex].MV_CC_SetIntValue_NET("Height", (uint)cup.p_Cam[iCamIndex].iROI[3]);


                    if (cup.p_CD.triggleCam == iCamIndex)
                    {
                        //  SetTrigger(iCamIndex);    //** 默认不启用触发
                    }
                }
            }
            catch
            {
                return false;
                // MessageBox.Show("修改参数失败！");
            }
            return true;
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

        #endregion Hik摄像头相关 

        #region 自义定功能函数去
        //采集到的图像数据转为halcon 的Object格式
        public void PtrToObj(IntPtr ptr, int iFormat)
        {
            if (iFormat == 1)  //彩色
            {
                try
                {
                    //  HOperatorSet.GenImage3(out hobjImg, "byte", stFrameInfo.nWidth, stFrameInfo.nHeight, ptr, new IntPtr(ptr.ToInt64() + stFrameInfo.nWidth * stFrameInfo.nHeight), new IntPtr(ptr.ToInt64() + stFrameInfo.nWidth * stFrameInfo.nHeight * 2));

                    HOperatorSet.GenImageInterleaved(out hobjImg, ptr, "bgr", (HTuple)stFrameInfo.nWidth, stFrameInfo.nHeight, 0, "byte", 0, 0, 0, 0, -1, 0);
                }
                catch
                {
                    //  MessageBox.Show(ex.ToString());

                    return;
                }
            }
            else if (iFormat == 0)      //灰色
            {

                try
                {
                    HOperatorSet.GenImage1Extern(out hobjImg, "byte", stFrameInfo.nWidth, stFrameInfo.nHeight, ptr, IntPtr.Zero);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("转换图像格式失败：" + ex);

                    return;
                }
            }
        }
        //黑白图像，直接转为HObject格式
        private HObject GrabOneFrameHObj(int iCamInd)
        {
            HImage image = new HImage();
            if (iCamType == 0)
            {
                nRet = camera2[iCamInd].MV_CC_GetOneFrameTimeout_NET(m_BufForDriver, nPayloadSize, ref stFrameInfo, 10000); // m_BufForDriver为图像数据接收指针


                if (nRet == MyCamera.MV_OK)
                {
                    image.GenImage1("byte", (int)stFrameInfo.nWidth, (int)stFrameInfo.nHeight, m_BufForDriver);
                    //  Marshal.Copy(m_BufForDriver, byteImage, 0, (int)nPayloadSize);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                nRet = camera2[iCamInd].MV_CC_GetImageBuffer_NET(ref stFrameInfo1, 1000);
                if (nRet == MyCamera.MV_OK)
                {

                    image.GenImage1("byte", (int)stFrameInfo1.stFrameInfo.nWidth, (int)stFrameInfo1.stFrameInfo.nHeight, stFrameInfo1.pBufAddr);
                    //  Marshal.Copy(stFrameInfo1.pBufAddr, byteImage, 0, (int)nPayloadSize);
                }
                else
                {
                    return null;
                }

            }
            return image;
            // PtrToObj(matImage.Ptr(), cup.p_HikPara.iPixelType);

        }
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

                        vpWin.OpenOrCloseCam(true);
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

        //显示日志
        string strLogPath = "";
        PMARes pmaRes;
        private void AnaEvent()
        {
            pmaRes = vpWin._PMARes;
            if (!pmaRes.bOk)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "完成一次运行";
                operLog.strFailReason = "检测结果：NG";
                AddLog();
            }
            else
            {
                operLog.iEventStatus = 2;
                operLog.strOperEvent = "完成一次运行";
                operLog.strFailReason = "检测结果：OK";
                /*operLog.strFailReason = "分数:" + pmaRes.dPMAScore.ToString("0.00")
                    + "，X:" + pmaRes.dTX.ToString("0.00") + "，Y:" + pmaRes.dTY.ToString("0.00")
                    + "，R:" + pmaRes.dTR.ToString("0.00");*/
                AddLog();
            }



            /*int iResCode = vpWin._iResCode;
            int[] c = new int[maxCameraNum];
            iEventStatus[maxCameraNum] = 0;
            dr.xbType = vpWin._xbType;
            if (dr.xbType == 1)
                textBox_JobN_xbType.Text = "凹口匣钵";
            else if(dr.xbType == 2)
                textBox_JobN_xbType.Text = "平口匣钵";
            else
                textBox_JobN_xbType.Text = "";
            for (int i=0;i<maxCameraNum;i++)
            {
                c[i] = iResCode % 10;
                iResCode= iResCode /10;
                iEventStatus[i] = c[i];

                dr.iPlaneIndex[i] = c[i];
                if (c[i] > iEventStatus[maxCameraNum])
                    iEventStatus[maxCameraNum] = c[i];
            }
 pmaRes = vpWin._PMARes;
            rts.iEventStatus = iEventStatus;
            //st 2022年11月2日 结果反馈
            switch(rts.onLS.iFeedbackType)                  //启用了结果自动反馈
            {
                case 1:
                    if (vpWin._iResCode == 0)                //按实际结果反馈
                        csd.SendResult(sPortCy, true,0);
                    else
                        csd.SendResult(sPortCy, false, dr.xbType);
                    break;
                case 2:                                           //固定反馈OK
           
                    csd.SendResult(sPortCy, true,0);
                    break;
                case 3:                                            //固定反馈NG
                    csd.SendResult(sPortCy, false,1);
                    break;
                case 6:                                            //固定反馈NG
                    csd.SendResult(sPortCy, false, 2);
                    break;
                case 0:         //不反馈，等待手动
                    break;
                    //   csd.SendResult(sPortCy, true);  //st 2023年1月3日 
                    *//**//*
            }
            strResInfo.iInd = iFileIndex;
            strResInfo.strTime = acqFIFO.strTime;
            strResInfo.strResCode = vpWin._iResCode.ToString();
            strResInfo.strMark = "0";
            if(strLogPath=="")
            { 
                strLogPath = cup.p_Paf.strLogPath + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss") +"_"+ Path.GetFileName(cup.p_Paf.strPlaybackPath) + ".txt";
            }
           if(! fo.WriteMark(strLogPath, strResInfo))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "保存结果";
                AddLog();
            }*/


        }

        private void menuTask_Clear_Click(object sender, EventArgs e)
        {
            strOrder = "清除";
            csd.ClearOutput(sPortCy);
        }

        private void AddEventLog(int[] iEventStatus)
        {
            /* if (dgvEventLog.RowCount > 20)
                 dgvEventLog.Rows.RemoveAt(19);

             int ind = 0;// dgvLog.Rows.Add();
             dgvEventLog.Rows.Insert(0, 1);

             dgvEventLog.Rows[ind].Cells[0].Value = DateTime.Now.ToString("HH:mm:ss");

             for (int i = 0; i < 6; i++)
             {
                 switch (iEventStatus[i])
                 {
                     case 9:
                         //     dgvLog.Rows[ind].Cells[i + 1].VisionApplication.Properties.Resources.EventRed1
                         dgvEventLog.Rows[ind].Cells[i + 1].Value = global::VisionApplication.Properties.Resources.EventUn;
                         //VisionApplication.Properties.Resources.EventUn;
                         break;
                     case 0:
                          dgvEventLog.Rows[ind].Cells[i + 1].Value = global::VisionApplication.Properties.Resources.EventGreen;
                         break;
                     case 1:
                          dgvEventLog.Rows[ind].Cells[i + 1].Value = global::VisionApplication.Properties.Resources.EventYellow;
                         break;
                     case 2:
                          dgvEventLog.Rows[ind].Cells[i + 1].Value = global::VisionApplication.Properties.Resources.EventYellow;
                         break; 
                     case 3:
                          dgvEventLog.Rows[ind].Cells[i + 1].Value = global::VisionApplication.Properties.Resources.EventRed;
                         break;
                     case 4:
                          dgvEventLog.Rows[ind].Cells[i + 1].Value = global::VisionApplication.Properties.Resources.EventRed;
                         break;
                     case 5:
                         dgvEventLog.Rows[ind].Cells[i + 1].Value = global::VisionApplication.Properties.Resources.EventRed;
                         break;
                     case 6:
                          dgvEventLog.Rows[ind].Cells[i + 1].Value = global::VisionApplication.Properties.Resources.EventRed;
                         break;
                     case 7:
                          dgvEventLog.Rows[ind].Cells[i + 1].Value = global::VisionApplication.Properties.Resources.EventRed;
                         break;
                     case 8:
                          dgvEventLog.Rows[ind].Cells[i + 1].Value = global::VisionApplication.Properties.Resources.EventRed;
                         break;
                 }

             }



             if (!cup.p_VPPRTP.bAcqFromCam && cup.p_VPPRTP.bAcqFromIDB)
             {
                 iCurrentIndex = vpWin.GetCurImageIndex();
                 iNextIndex = vpWin.GetNextImageIndex();
                 toolStripStatus_No.Text = iCurrentIndex.ToString() + "|" + iNextIndex.ToString() + "||" + iFileGroupLen;
             }
             else if (!cup.p_VPPRTP.bAcqFromCam && !cup.p_VPPRTP.bAcqFromIDB)
             {
                 toolStripStatus_No.Text = iFileIndex.ToString() + "|" + iNextFileIndex.ToString() + "||" + iFileGroupLen + "  Time:"+acqFIFO.strTime;
             }*/
        }

        //  DateTime dtOperLog = new DateTime();
        private void AddLog()
        {
            if (dgvLog.RowCount > 20)
                dgvLog.Rows.RemoveAt(19);

            int ind = 0;// dgvLog.Rows.Add();
            dgvLog.Rows.Insert(0, 1);
            //  dtOperLog= DateTime.Now;
            operLog.dtOperTime = DateTime.Now;
            dgvLog.Rows[ind].Cells[0].Value = operLog.dtOperTime.ToString("yyyy-MM-dd HH:mm:ss");
            dgvLog.Rows[ind].Cells[1].Value = operLog.strOperEvent;
            switch (operLog.iEventStatus)
            {
                case 0:
                case 2:
                case 9:
                case 10:
                    dgvLog.Rows[ind].Cells[2].Value = "操作正常";
                    dgvLog.Rows[ind].DefaultCellStyle.ForeColor = Color.Black;
                    break;
                case -1:
                    dgvLog.Rows[ind].Cells[2].Value = "操作失败";
                    dgvLog.Rows[ind].DefaultCellStyle.ForeColor = Color.Red;
                    break;
                case 1:
                    dgvLog.Rows[ind].Cells[2].Value = "操作超时";
                    dgvLog.Rows[ind].DefaultCellStyle.ForeColor = Color.Red;
                    break;
                default:
                    dgvLog.Rows[ind].Cells[2].Value = "操作异常";
                    dgvLog.Rows[ind].DefaultCellStyle.ForeColor = Color.Red;
                    break;
            }

            dgvLog.Rows[ind].Cells[4].Value = operLog.strUserName;
            if (operLog.iEventStatus == -1 || operLog.iEventStatus == 1
                || operLog.iEventStatus == 2 || operLog.iEventStatus == 9)
            {
                dgvLog.Rows[ind].Cells[3].Value = operLog.strFailReason;
            }

            fo.WriteOperLog(cup.p_Paf.strLogPath + "\\Oper_" + DateTime.Now.ToString("yyyyMMdd") + ".txt", operLog);
            //   dgvLog.Rows.Insert(0, dgvr);
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


            if (rts.iModelRole == 10)
            {
                /*  txt_NextImageIndex.Visible = true;
                  label5.Visible = true;*/
                btFunSet.Visible = true;
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


        int iProcess = 0;
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


                Thread.Sleep(200);
            }

        }
        //授权验证
        private void RoleCheck()
        {
            if (!File.Exists("C:\\Windows\\authentication.txt"))
            {
                File.Copy("authentication.txt", "C:\\Windows\\authentication.txt", true);
                mPasswordFile = new Protect("C:\\Windows\\authentication.txt");
            }
            string strMAC = mPasswordFile.GetMacAddress();
            string strEnableMAC = mPasswordFile.GetCID();

            int iModelRole = -1;
            if (strMAC == strEnableMAC || strMAC == "207BD2D2CA3E" || strMAC == "00E04C782532" || strMAC == "00FFCF3A6322" || strMAC == "8C32233F2B85")  //E4:5E:37:62:A1:3D   00FFCF3A6322  
            {
                rts.initStatus.bModelRoleCert = true;

                DateVerify(iModelRole);
                if (rts.iModelRole == 0)
                {
                    if (strMAC == "00E04C782532" || strMAC == "207BD2D2CA3E" || strMAC == "00FFCF3A6322" || strMAC == "8C32233F2B85")   //调试专用功能
                    {
                        iModelRole = 10;

                    }
                    else if (DateTime.Now.ToString("yyyyMMdd") == "20240617")  //临时使用
                    {
                        iModelRole = 10;
                    }
                    else
                    {
                        iModelRole = 9;

                    }
                    rts.iModelRole = iModelRole;
                }
                
            }
            operLog.iEventStatus = iModelRole;
            operLog.strOperEvent = "身份验证";
            operLog.strFailReason = iModelRole.ToString();
            AddLog();
        }
        public FrmMainUI()
        {
            //  this.Visible = false;

            try
            {
                /* FileInfo fiN = new FileInfo("CommonCls.dll");
                 FileInfo fiO = new FileInfo("C:\\Program Files\\Cognex\\VisionPro\\ReferencedAssemblies\\CommonCls.dll");
                 if (fiN.LastWriteTime != fiO.LastWriteTime)
                     File.Copy("CommonCls.dll", "C:\\Program Files\\Cognex\\VisionPro\\ReferencedAssemblies\\CommonCls.dll", true);
       */
            }
            catch
            {
                //   File.Copy("CommonCls.dll", "C:\\Program Files\\Cognex\\VisionPro\\ReferencedAssemblies\\CommonCls.dll", true);
            }

            CheckForIllegalCrossThreadCalls = false;

            InitializeComponent();



            //   this.Visible = false;
            InitUserParam();


        }

        bool bLoadFinish = false;
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
                LoadVPFile();            //VP控件初始化，加载VPP文件和VisionControl控件


                iProcess = 30;
                fw.SetProgress(30, "正在加载硬件驱动");

               /* jczPipThread = new Thread(InitJCZ);
                jczPipThread.Start();*/


                iProcess = 40;
                fw.SetProgress(40, "正在搜索并初始化相机");
                SearchCam();   // 搜搜相机数量                //
                InitCam();      //

                iProcess = 50;
                fw.SetProgress(80, "正在搜索并初始化串口");
                InitSerial();                                           //串口初始化
                                                                        //    fw.SetProgress(90, "正在初始化算法");
                iProcess = 60;
                fw.SetProgress(80, "正在初始化视图");
                if (rts.initStatus.bVPPInit)
                {
                    InitVisionControl();
                    //   bSucc = vpWin.SetVPPRuntimeParam();             //visionpro控件初始化
                }

                if (!bSucc)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "设置流程参数";
                    AddLog();
                } 

                // Read9PointCalibPoint();

                iProcess = 70;
                fw.SetProgress(70, "正在进行历史数据加载");
                InitRecord();       //初始化路径
                InitLocalFile();    //加载回放数据                            //  bSucc = vpWin.SetDiagParam(cup);
                 
                InitCheckStatus(); 
                iProcess = 90;
                fw.SetProgress(90, "正在启动相机采集");
                StartGrab(); 
                
                vpWin.iModelRole = rts.iModelRole; 

                fw.SetProgress(25, "UI初始化");
                InitParamUI();  //必要的参数初始化 

                //  runonetime();  启动默认运行一次  2024年7月22日，处理运行初始化界面 
                //  cdl.det_infer_img();

                this.Visible = true;
                this.Show();
                iProcess = 100;
                Thread.Sleep(20);
                InitAddLog();
            }
            else
            {
                rts.initStatus.bModelRoleCert = false;

            }
            fw.Close();
            this.WindowState = FormWindowState.Maximized;
            bLoadFinish = true;
            timer1.Enabled = true;

        }

     
        private void FrmMainUI_FormClosing(object sender, FormClosingEventArgs e)
        {

            SaveUserParam();

            string strPath = cup.p_Paf.strLogPath + "\\RTS_0000.txt";
            fo.SaveRunTimeStatus(strPath, rts);

      //      CloseLaser();

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
                Thread.Sleep(100);
            }

            CloseCam();
            disconnect();
            if (vpWin != null)
                vpWin.Close(bVPPParamChanged);

            //    cdl.CloseDL();
        }

        private void splitContainer3_Panel1_SizeChanged(object sender, EventArgs e)
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

                //     MessageBox.Show("当前任务没有结束，不能执行该操作");
                return;
            }

            CloseCam();
            SearchCam();     //
            InitCam();
            StartGrab();
            InitVisionControl();
        }

        //  int iRecordIndex = -1;
        //   bool bFoundTargetImage = false;
        //    bool bTaskFinish = true;
        bool bPMAEdit = false;
        int iTmpType = 0;
        CogPMAlignEditV2 pmaV2 = new CogPMAlignEditV2();
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

            bool bChecked = false;
            try
            {
                switch (((Button)sender).Name)
                {
                    case "btFunAcqOneImage":    //采集单帧图像
                        {
                            if (!bRunCondiction(0))
                                return;
                           
                            vpWin._iTaskType = 0;
                            rts.iTaskType = vpWin._iTaskType;
                            iAcquisitionsTimes++;
                            bool bRet = false;

                            vpWin.bMainTaskFinished = false;
                            if (cup.p_VPPRTP.bAcqFromCam)  //实时采集
                            {
                                rts.dtLastAcq = DateTime.Now;
                                bRet = vpWin.PreViewOneImage(cup.p_VPPRTP.iXBType);
                                if (!bRet)
                                {
                                    operLog.strOperEvent = "采集图像";
                                    operLog.strFailReason = "采集单帧图像";
                                    operLog.strFailReason = "采集异常错误";
                                }

                            }
                            else if (!bSingleFrameModel)  //回放模式
                            {

                                iFileIndex = iNextFileIndex;

                                iFileIndex = iFileIndex % iFileGroupLen;
                                vpWin.iFileIndex = iFileIndex;
                                if (!bCrackPlayback)
                                    iNextFileIndex = (++iNextFileIndex) % iFileGroupLen;

                                bRet = vpWin.PreViewOneImageFromFile(iFileIndex, false, cup.p_VPPRTP.iXBType);
                                operLog.strOperEvent = "采集图像";
                                operLog.strFailReason = "回放异常错误";

                            }
                            else
                            {
                                iFileIndex = iNextFileIndex;
                                iFileIndex = iFileIndex % (iFileGroupLen * 6);
                                bRet = vpWin.PreViewOneImageFromFile(iFileIndex, true, cup.p_VPPRTP.iXBType);
                                iNextFileIndex = (iFileIndex + 1) % (iFileGroupLen * 6);
                                //    iFileIndex++;
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
                                toolStripStatus_No.Text = iFileIndex.ToString() + "|" + iNextFileIndex.ToString() + "||" + iFileGroupLen + "  Time:" + acqFIFO.strTime;
                            }
                            if (!bRet)
                                iErrAcquisitionsTimes++;
                            vpWin.bMainTaskFinished = true;
                            UpdateStatisticsForJob(0);
                            vpWin.FitAllWin();
                            AddLog();
                            break;
                        }
                    case "btFun_Preview":   // 实时预览  2024年7月11日 待验证
                        {

                            if (!rts.initStatus.bCamInit)
                            {
                                operLog.iEventStatus = -1;
                                operLog.strOperEvent = "启动图像预览";
                                operLog.strFailReason = "相机未初始化";
                                AddLog();
                                return;
                            }
                            //允许预览
                            /*if (vpWin._iTaskType != 1 && !vpWin.bMainTaskFinished)
                            {
                                // MessageBox.Show("当前任务没有结束，不能执行该操作");
                                operLog.strOperEvent = "启动图像预览";
                                operLog.iEventStatus = -1;
                                operLog.strFailReason = "当前任务没有结束";
                                AddLog();
                                return;
                            }*/  
                            if (menuCam_Connect.Text == "启动图像预览")
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

                            //     AddLog();
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
                                /*rts.onLS.bTrigg = !rts.onLS.bTrigg;
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

                                }*/

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
                                    contThread.Start();
                                    operLog.iEventStatus = 0;
                                }
                                else
                                {
                                    vpWin._bContRun = false;
                                    operLog.strOperEvent = "关闭连续运行";
                                    operLog.iEventStatus = 0;
                                    ((Button)sender).Enabled = false;
                                  
                                }


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
                            return;
                        }

                        if (rts.iModelRole < 10)
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
                            if (rts.iModelRole == 10)
                            {
                                int iTmpType = vpWin._iTaskType;
                                vpWin._iTaskType = 5;
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
                                ((Button)sender).FlatStyle = FlatStyle.Popup;

                                vpWin.bMainTaskFinished = true;
                            }


                        }
                        break;
                }
                rts.iTaskType = vpWin._iTaskType;

            }
            catch
            {
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

            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "连接串口";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();

                //    MessageBox.Show("当前任务没有结束，不能执行该操作");
                return;
            }

            bool bConn = false;
            for (int i = 0; i < 4; i++)
            {
                if (sPortAll[i].IsOpen)
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
                    vpWin._bRecord = cup.p_VPPRTP.bRecord;
                }

                acqFIFO._strSigSavePathS = strSigSavePathS;
                acqFIFO._strConSavePathS = strConSavePathS;



                toolBt_Record.Checked = cup.p_VPPRTP.bRecord;
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
                //   MessageBox.Show("当前任务没有结束，不能执行该操作");
                return;
            }
            frm_Serial fs = new frm_Serial(csd);
            fs.ShowDialog();

        }

        private void menuCam_Close_Click(object sender, EventArgs e)
        {
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "关闭设备";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();

                //   MessageBox.Show("当前任务没有结束，不能执行该操作");
                return;
            }
            CloseCam();
            rts.initStatus.bCamInit = false;
            operLog.strOperEvent = "关闭设备";
            operLog.iEventStatus = 0;
            AddLog();
        }

        private void toolBt_RuntimeSet_Click(object sender, EventArgs e)
        {
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

                //VPP文件改变，则重新加载VPP
                if (frs.bVPPChange)
                {

                    // rts.initStatus.bVPPInit = vpWin.ReLoadVPP(frs.strVPPFileName);
                    LoadVPFile();
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
                    vpWin.PreViewOneImageFromFile(iFileIndex, false, cup.p_VPPRTP.iXBType);
                else if (iShowType == 2)
                    vpWin.PreViewOneImageFromFileFX(iFileIndex, false);
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
            Login login = new Login(mPasswordFile);
            if (login.ShowDialog() == DialogResult.OK)
            {
                rts.iModelRole = login.iModelRole;
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

            /*toolBtSta_LockImage.Checked = !toolBtSta_LockImage.Checked;
            if(toolBtSta_LockImage.Checked)
            {
                toolBtSta_LockImage.Image=global::VisionApplication.Properties.Resources.Lock;
            }
            else
            {
                toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.UnLock;
            }
            rts.onLS.bLock = toolBtSta_LockImage.Checked;
            if(rts.initStatus.bVPPInit)
                vpWin.SetLock(toolBtSta_LockImage.Checked);*/
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
                case 3:
                    menu_View_Layout3.Checked = true;
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
                toolStripStatus_No.Text = iFileIndex.ToString() + "|" + iNextFileIndex.ToString() + "||" + iFileGroupLen + "  Time:" + acqFIFO.strTime;
            }
        }

        private void menuManage_UserLogout_Click(object sender, EventArgs e)
        {
            if (rts.iModelRole < 0)
                return;
            rts.iModelRole = 0;
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
            menu_View_Layout3.Checked = false;
            menu_View_Layout5.Checked = false;
            cup.p_VPPRTP.iLayoutType = iLayoutType;
            SaveUserParam();
        }


        private void menu_View_Layout3_Click(object sender, EventArgs e)
        {
            vpWin.ViewLayout(3);
            iLayoutType = 3;
            menu_View_Layout1.Checked = false;
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
            csd.MachineStartOrStop(sPortCy, false);
        }
        //检测反馈OK
        string strOrder = "";
        private void menuTask_OK_Click(object sender, EventArgs e)
        {
            operLog.strOperEvent = "OK反馈";
            strOrder = "Ok";
            //  csd.SendResult(sPortCy, true);
            if (csd.SendResult(sPortCy, true, 0))
            {
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
            if (csd.SendResult(sPortCy, false, 1))
            {
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
            if (csd.SendResult(sPortCy, false, 2))
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
        private void LightControlTurnOn()
        {
            if (icamSelectInd < 0)
                return;
            bool bSucc;
            if (!toolBtSta_Triggle.Checked)
            {
                bSucc = csd.AdjBright((int)cup.p_Cam[icamSelectInd].iBrightness);
            }
            else
            {
                bSucc = csd.AdjBright(0);
            }
            if (bSucc)
            {
                toolBtSta_Triggle.Checked = !toolBtSta_Triggle.Checked;
                if (toolBtSta_Triggle.Checked)
                {
                    toolBtSta_Triggle.Image = global::VisionApplication.Properties.Resources.LightOn;
                    toolBtSta_Triggle.ToolTipText = "光源关闭";
                }
                else
                {
                    toolBtSta_Triggle.Image = global::VisionApplication.Properties.Resources.LightOff;
                    toolBtSta_Triggle.ToolTipText = "光源打开";
                }
            }

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
                    strResInfo.strTime = acqFIFO.strTime;
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
                    strResInfo.strTime = acqFIFO.strTime;
                    strResInfo.strResCode = vpWin._iResCode.ToString();
                    strResInfo.strMark = "2";
                    fo.WriteMark(strLogPath, strResInfo);
                }
                catch
                {
                    operLog.iEventStatus = -1;
                }
                //st2023年3月30日   acqFIFO.SaveImageForErr(strErrPathS, acqFIFO.strTime);
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



        bool bSingleFrameModel = false;  //单帧模式
        private void menuTask_SingleFrame_Click(object sender, EventArgs e)
        {
            if (!bSingleFrameModel)
            {
                if (cup.p_CD.bListProcess[1] || cup.p_CD.bListProcess[2] || cup.p_CD.bListProcess[3] || cup.p_CD.bListProcess[4])
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
                toolBtSta_MarkAfterLocated.ToolTipText = "不标刻";
            }
            else
            {
                toolBtSta_MarkAfterLocated.Image = global::VisionApplication.Properties.Resources.WithOutMark;
                toolBtSta_MarkAfterLocated.ToolTipText = "标刻";
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

        private void menuTool_RemarkManager_Click(object sender, EventArgs e)
        {

        }

        private void menuTool_ImageGroupPreview_Click(object sender, EventArgs e)
        {

        }



        bool bfirstTime = true;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (iProcess < 100)
                return;

            if (bfirstTime)
            {
                //第三步  查询文件对象
            /*    lczParam.cmdType = 8;
                lczParam.strFileName = Path.Combine(cup.p_Paf.ManuMarkPath, "route.ezd.");  //"route.ezd"
                lczParam.ind3 = 0;
                bCmdEnd = false;
                if (pipeClient != null)
                    pipeClient.SendParam(lczParam);*/
                /*queryEntThread = new Thread(QueryEntInfo);
                queryEntThread.Start();*/

               /* JCZServerLayout();
                timer1.Interval = 10000;
                bfirstTime = false;*/
            }
            if (cup.p_RTSP.bAutoConnCam)
            {
                for (int i = 0; i < maxCameraNum; i++)
                {
                    //判断相机是否在线
                    if (!acqFIFO.IsCamConn(i))
                        rts.cs.iStatus[i] = 0;
                }
            }

            try
            {
                TipInfo();
                if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0)
                {
                    dirJudge(true);
                }

                if (cup.p_RTSP.bAutoRunVPP)
                {
                    if (rts.initStatus.bVPPInit && vpWin._iTaskType != 3)
                        vpWin.ActivateRun();
                }
            }
            catch
            { };
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
            /*DriveInfo di = new DriveInfo(path.Substring(0, 1)); //"path.Substring(0,1)
            if (di.IsReady)
            { 
                long totalSize = di.TotalSize/1024/1024/1024;
                long FreeSize = di.TotalFreeSpace / 1024 / 1024 / 1024;
                toolTip1.SetToolTip(btSpace, "剩余磁盘空间：" + FreeSize +"G");
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
                if (cup.p_VPPRTP.bAcqFromCam && cup.p_CD.bListProcess[i] && rts.cs.iStatus[iCamInd] < 2)
                {
                    SearchCam();
                    items = hIKVisionCamera.EnumDevices();
                    if (cameraNum > iCamInd)
                    {
                        if (!InitCamInd(iCamInd))
                        {
                            iCamFailNum++;
                            switch (i)
                            {
                                case 0:
                                    toolStripStatus_Cam1.ForeColor = Color.Orange;
                                    break;
                                case 1:
                                    toolStripStatus_Cam2.ForeColor = Color.Orange;
                                    break;
                                case 2:
                                    toolStripStatus_Cam3.ForeColor = Color.Orange;
                                    break;
                                case 3:
                                    toolStripStatus_Cam4.ForeColor = Color.Orange;
                                    break;
                                case 4:
                                    toolStripStatus_Cam5.ForeColor = Color.Orange;
                                    break;
                            }
                        }
                        else
                        {
                            StartGrab(iCamInd);
                            InitVisionControl();
                        }

                    }
                    else
                    {
                        iCamFailNum++;
                        switch (i)
                        {
                            case 0:
                                toolStripStatus_Cam1.ForeColor = Color.Red;
                                break;
                            case 1:
                                toolStripStatus_Cam2.ForeColor = Color.Red;
                                break;
                            case 2:
                                toolStripStatus_Cam3.ForeColor = Color.Red;
                                break;
                            case 3:
                                toolStripStatus_Cam4.ForeColor = Color.Red;
                                break;
                            case 4:
                                toolStripStatus_Cam5.ForeColor = Color.Red;
                                break;
                        }
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

            string strPath = cup.p_Paf.strLogPath + "\\RTS_0000.txt";
            fo.SaveRunTimeStatus(strPath, rts);
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

        private void menuXBTypeS_Click(object sender, EventArgs e)
        {
            try
            {
                //  double dSig;
                switch (((ToolStripMenuItem)sender).Name)
                {
                    case "menuXBType_1":
                        menuXBType_1.Checked = true;
                        menuXBType_2.Checked = false;
                        menuXBType_9.Checked = false;
                        cup.p_VPPRTP.iXBType = 0;
                        //      dSig = 2;
                        //     vpWin.SetxbType(cup.p_VPPRTP.iXBType);
                        break;
                    case "menuXBType_2":
                        menuXBType_1.Checked = false;
                        menuXBType_2.Checked = true;
                        menuXBType_9.Checked = false;
                        cup.p_VPPRTP.iXBType = 1;
                        //       dSig = 3;
                        //        vpWin.SetxbType(cup.p_VPPRTP.iXBType);
                        break;
                    case "menuXBType_9":
                        menuXBType_1.Checked = false;
                        menuXBType_2.Checked = false;
                        menuXBType_9.Checked = true;
                        cup.p_VPPRTP.iXBType = 9;
                        break;
                }
                vpWin.SetxbType(cup.p_VPPRTP.iXBType);
                /*  for (int i=0;i<4;i++)
                  {
                      cup.p_subFX[i].sigma = dSig;
                  }*/

            }
            catch
            {

            }

        }

        bool bCrackPlayback = false;

        string strPlaybackFileList = "";
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
            frm_Serial fs = new frm_Serial();
            fs.ShowDialog();
        }

        #endregion  交互操作事件区


        #region   
        //改变状态
        int iAcquisitionsTimes = 0;
        int iErrAcquisitionsTimes = 0;
        int iErrExecuteTimes = 0;
        private void UpdateStatisticsForJob(int i)
        {
            // don't update statistics when showing live video

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

            toolStripStatus_inferTime.Text = "推理时间：" + vpWin.cost_milliseconds;

            rts.detS.iTotal  = detStat.iTotal+ Int32.Parse(strStatus[0]);
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
            if (menu_View_SystemLog.Checked && menu_View_EventStatus.Checked)
            {

                splitContainer3.Panel2Collapsed = false;
                splitContainer4.Panel1Collapsed = false;
                splitContainer4.Panel2Collapsed = false;


            }
            else if (menu_View_SystemLog.Checked)
            {
                //  splitContainer4.Visible = true;
                splitContainer3.Panel2Collapsed = false;
                splitContainer4.Panel1Collapsed = true;
                splitContainer4.Panel2Collapsed = false;
            }
            else if (menu_View_EventStatus.Checked)
            {
                // splitContainer4.Visible = true;
                splitContainer3.Panel2Collapsed = false;
                splitContainer4.Panel1Collapsed = false;
                splitContainer4.Panel2Collapsed = true;
            }
            else
            {
                splitContainer3.Panel2Collapsed = true;

            }
        }

        //20220410串口操作
        //初始化串口

        private bool InitSerialIndex(int i)
        {
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

        string[] strPortName;
        //    List<string> strPortNameTmp = new List<string>();
        List<int> comInd = new List<int>();
        private void PortProc(ref string[] strPortName)
        {
            if (strPortName == null || strPortName.Length == 0)
            {
                return;
            }
            for (int i = 0; i < strPortName.Length; i++)
            {
                //strPortNameTmp.Add(strPortName[i]);
                comInd.Add(int.Parse(strPortName[i].Substring(3, strPortName[i].Length - 3)));

            }
            // strPortNameTmp.Sort();
            comInd.Sort();
            bool bCont = false;
            for (int i = 0; i < comInd.Count - 1; i++)
            {
                if (comInd[i + 1] - comInd[i] != 1)
                {
                    if (!bCont)
                    {
                        comInd.RemoveAt(i);
                        //  strPortNameTmp.RemoveAt(i);
                    }
                    else
                    {
                        //  strPortNameTmp.Remove("COM" + comInd[i+1].ToString());
                        comInd.RemoveAt(i + 1);

                    }
                }
                else
                {
                    bCont = true;
                }
            }

            strPortName = new string[comInd.Count];
            for (int i = 0; i < strPortName.Length; i++)
            {
                strPortName[i] = "COM" + comInd[i].ToString();
            }
            //  Array.Copy(strPortNameTmp, strPortName, strPortNameTmp.Count);
            // strPortName=strPortNameTmp
        }

        private void InitSerial()
        {
            serialInd = cup.p_SD.serInd;
            strPortName = SerialPort.GetPortNames();
            PortProc(ref strPortName);                      //规整串口，挑选按循序排列的4个串口
            //st  2022年10月31日
            for (int i = 0; i < 4; i++)
            {
                sPortAll[i] = new SerialPort();
            }
            //  int iConnNum = 0;
            operLog.strOperEvent = "串口初始化";
            if (strPortName == null || strPortName.Length == 0)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "搜索串口失败";
                AddLog();
            }
            else
            {
                int ind = 0;
                for (int i = 0; i < strPortName.Length; i++)
                {
                    if (i >= 4)
                        break;
                    if (cup.p_SD.bUsed[i])
                    {
                        bool bSucc = InitSerialIndex(i);
                        if (bSucc)
                        {
                            // cup.p_SD.
                            operLog.strOperEvent = "打开串口" + (ind + 1) + "成功";
                            operLog.iEventStatus = 0;
                        }
                        else
                        {
                            operLog.strFailReason = "打开串口" + (i + 1) + "失败";
                            //  operLog.strOperEvent = "打开串口" + (i + 1) + "失败";
                            operLog.iEventStatus = -1;
                        }
                        AddLog();
                    }

                    ind++;
                }
            }
            rts.initStatus.bSerInit = false;



            //st 2022年10月31日  是否需要保留
            sPortSub[0] = new SerialPort();
            sPortSub[1] = new SerialPort();

            sPortMain = sPortAll[0];//serialInd[0]
            sPortSub[0] = sPortAll[1];  //
            sPortSub[1] = sPortAll[2]; //serialInd[2]
            sPortCy = sPortAll[3];// serialInd[3]
                                  //  csd._sPortAll = sPortAll;
            csd._sPort = sPortMain;
            csd._sPortSub = sPortSub;
            csd._sPortCy = sPortCy;
            TipInfo();
            sPortCy.DataReceived += new SerialDataReceivedEventHandler(PortGetCylinder);  //启用串口数据回调

        }

        //接受数据
        private void PortGetCylinder(object sender, SerialDataReceivedEventArgs e)
        {
            // 获取接收缓冲区长度
            Thread.Sleep(20);
            int Readlen = sPortCy.BytesToRead;
            // 新建一个 byte[] 数组 用来存放接收的数据
            byte[] inf = new byte[Readlen];
            // 接收数据到by里面
            sPortCy.Read(inf, 0, Readlen);

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

            else if (Readlen == 10 && inf[0] == 0x01 && inf[1] == 0x45
                && inf[2] == 0x05 && inf[3] == 0x01)
            {
                if ((inf[4] & 0x01) == 0x01)  //气缸抬升完成
                {
                    if (rts.onLS.bTrigg && rts.iIsRunning == 0)  //启用触发，并且收到触发信号
                    {
                        toolStripStatus_Hand.Text = DateTime.Now.ToString() + ":请求检测";
                        if (rts.onLS.iFeedbackType == 5)          //不作检测，直接反馈OK
                        {
                            Thread.Sleep(800);
                            csd.SendResult(sPortCy, true, 0);
                        }
                        else                                      //执行检测
                        {
                            Thread.Sleep(5);  //留一个延时去抖动，再进行采集
                            if (bRunCondiction(10))
                                runonetime();
                        }
                    }
                }
                else if ((inf[4] & 0x02) == 0x02)  //气缸下降完成
                {
                    toolStripStatus_Hand.Text = DateTime.Now.ToString() + ":匣钵下降完成";
                    //   AddEventLog();
                }
            }
            else
            {

            }

        }

        //st 2022年10月31日
        private bool openSer(SerialPort sPortS, string com)
        {
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

            try {
                for (int i = 0; i < 4; i++)
                {
                    if (sPortAll[i] != null && sPortAll[i].IsOpen)
                    {
                        sPortAll[i].Close();

                        operLog.strOperEvent = "关闭串口" + (i + 1) + "成功";
                        operLog.iEventStatus = 0;
                        AddLog();
                        ((ToolStripStatusLabel)statusStrip1.Items[i + 8]).ForeColor = Color.Red;
                        //    (this.Controls.Find("toolStripStatus_" + (i + 1).ToString(), true)[0]).ForeColor = Color.Red;
                    }
                    else
                    {
                        //  (this.Controls.Find("toolStripStatus_" + (i + 1).ToString(), true)[0]).ForeColor = Color.Gray;
                    }
                }

                toolStripStatus_Ser.Text = "串口未连接";
                toolStripStatus_Ser.ForeColor = Color.Red;
                return true;
            }
            catch
            {
                operLog.strOperEvent = "关闭串口失败";
                operLog.iEventStatus = -1;
                AddLog();
                return false;
            }


        }


        //实现接口方法

        #endregion

        // 
        #region    Runtime implementation

        private bool bRunCondiction(int iType) 
        {
            //需要的工作相机没有就绪
            if (cup.p_VPPRTP.bAcqFromCam && !rts.initStatus.bCamPrepared)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "连续运行";
                operLog.strFailReason = "部分相机没有准备就绪";
                AddLog();
                return false;
            }
            //回放模式，文件没有就绪
            if (!cup.p_VPPRTP.bAcqFromCam && !rts.initStatus.bLocalFileExist)
            {
                operLog.strOperEvent = "运行一次";
                operLog.iEventStatus = -1;
                operLog.strFailReason = "回放文件夹不存在或者图像文件不存在";
                AddLog();
                return false;
            }
            if (cup.p_VPPRTP.bVisionInfer && rts.initStatus.iVPPStatus != 0)  //
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "运行一次";
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
                    return false;
                }
            }
            if (iType == 1)  //手动运行一次
            {
                if (!vpWin.bMainTaskFinished)   //如果当前有任务在执行，即不能重复运行
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "运行一次";
                    operLog.strFailReason = "当前任务没有结束";
                    AddLog();
                    return false;
                }
                /*if (rts.initStatus.bJCZ != 0 && rts.onLS.bLock)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "运行一次";
                    operLog.strFailReason = "板卡未初始化";
                    AddLog();
                    return false;
                }
                //要进行激光标刻，但激光器还在工作
                if (iLaserWorkingType != 0 && rts.onLS.bLock)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "运行一次";
                    operLog.strFailReason = "其它标刻任务未结束";
                    AddLog();
                    return false;
                }*/
            }
            else if (iType == 10)   //触发运行一次
            {               
                if (!vpWin.bMainTaskFinished && (vpWin._iTaskType != 3 || !cup.p_VPPRTP.bAcqFromCam))
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "运行一次";
                    operLog.strFailReason = "当前任务没有结束";
                    AddLog();
                    return false;
                }
                //要进行激光标刻，但激光器还在工作
             /*   if (iLaserWorkingType != 0 && rts.onLS.bLock)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "激光标刻";
                    operLog.strFailReason = "其它任务未结束";
                    AddLog();
                    return false;
                }*/

            }
            else if(iType==3)  //连续运行
            {
             /*   if (rts.initStatus.bJCZ != 0 && rts.onLS.bLock)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "运行一次";
                    operLog.strFailReason = "板卡未初始化";
                    AddLog();
                    return false;
                }*/

                if (vpWin._iTaskType != 3)
                {
                    if (!vpWin.bMainTaskFinished)
                    {
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = "连续运行";
                        operLog.strFailReason = "当前任务没有结束";
                        AddLog();
                        return false;
                    }
                    //要进行激光标刻，但激光器还在工作
                /*    if (iLaserWorkingType != 0 && rts.onLS.bLock)
                    {
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = "运行一次";
                        operLog.strFailReason = "其它标刻任务未结束";
                        AddLog();
                        return false;
                    }*/
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
                    if (!cup.p_VPPRTP.bAcqFromCam && !toolBtSta_LockImage.Checked)  //从文件读取
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

        private void runandupdate()
        {
            UpdateThread = new Thread(RunAndUpdateThread);
            UpdateThread.Start();
        }

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
                    //   InitDL();
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
        bool bLoadVPRes = false;
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
        private void btFunParam_Click(object sender, EventArgs e)
        {
          /*  if (!LaserCondiction(4))
                return;

            iLaserWorkingType = 4;
            lczParam.cmdType = 40;  //参数设置 
            pipeClient.SendParam(lczParam);*/
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

        private void menuManage_SuperOrNormal_Click(object sender, EventArgs e)
        {
            if (rts.iModelRole == 10)
            {
                rts.iModelRole = 9;
                vpWin.iModelRole = 9;
                menuManage_SuperOrNormal.Text = "切换至超级用户";
            }
            else
            {
                rts.iModelRole = 10;
                vpWin.iModelRole = 10;
                menuManage_SuperOrNormal.Text = "切换至普通用户";
            }
            vpWin.SiftRDMain();
            if (rts.iModelRole == 10)
            {
                /* txt_NextImageIndex.Visible = true;
                 label5.Visible = true;*/
                btFunSet.Visible = true;
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
            Process.Start("explorer.exe", Path.GetDirectoryName(cup.p_Paf.strVPPFilePath));
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



        public void RunAndUpdateThread()
        {
            try
            {

                //    if (!vpWin.HaveRun())
                {
                    vpWin.bChkSubrecord = false;
                    if (vpWin.RunOneTime())
                    {
                        while (vpWin._bRunOneTimeFinished != 2)
                        {
                            Thread.Sleep(10);
                        }
                    }
                }
                Thread.Sleep(40);
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

        Thread UpdateUIThread;


        //RunOneTime()->   RunOneTimeThread()  ->vpWin.RunOneTime()
        //->LaserMarkAfterVisionDet()->UpdateUIExe()
        public void RunOneTimeThread()
        {
            try
            {
                rts.iIsRunning = 1;
                /*  rts.dtLastDet = DateTime.Now;
                  operLog.iEventStatus = 0;
                  operLog.strOperEvent = "启动一次运行";
                  operLog.strFailReason = "";
                  AddLog();*/
                if (vpWin.RunOneTime())
                {
                    while (vpWin._bRunOneTimeFinished != 2)
                    {
                        Thread.Sleep(10);
                    }
                  
                    pmaRes = vpWin._PMARes;
                  /*  if (rts.initStatus.bJCZ == 0 && rts.onLS.bLock)
                        LaserMarkAfterVisionDet(); */ //2025年6月12日，检测完毕后启动激光标刻
                    /* operLog.iEventStatus = 0;
                     operLog.strOperEvent = "执行一次运行";
                     operLog.strFailReason = "";*/
                    vpWin.bMainTaskFinished = true;
                    rts.iIsRunning = 0;
                }
                else
                {
                    rts.iIsRunning = -1;
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "执行一次运行";
                    operLog.strFailReason = "未知原因";
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
            // 

            //st 2024年7月13日
            UpdateUIThread = new Thread(UpdateUIExe);
            UpdateUIThread.Start();

        }



        public delegate void delegateUIUpdate();
        private void UpdateUIExe()
        {
            UpdateStatisticsForJob(0);  //更新统计数据
            if (cup.p_RTSP.bUpdateRecord)
            {                
                AnaEvent();
                AddEventLog(iEventStatus);
            }
        }
        public void RunContinuousThread()
        {

            while (vpWin._bContRun)
            {
                if (vpWin.RunOneTime())
                {
                    while (vpWin._bRunOneTimeFinished != 2)
                    {
                        Thread.Sleep(5);
                    }
                    if (this.InvokeRequired)
                    {
                        delegateUIUpdate updateUItmp = new delegateUIUpdate(UpdateUIExe); //将委托实例化
                        this.Invoke(updateUItmp, null);
                        //    Invoke(new EventHandler(ClearHistoryQueues), new object[] { sender, e });

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
            }
            vpWin._iTaskType = -1;
            //  operLog.strOperEvent = "关闭连续运行";
            //  ((Button)sender).FlatStyle = FlatStyle.Flat;
            btFunRunCont.Image = global::VisionApplication.Properties.Resources.LoopRun2;
            vpWin.bMainTaskFinished = true;
            operLog.iEventStatus = 0;
            btFunRunCont.FlatStyle = FlatStyle.Popup;
            btFunRunCont.Enabled = true;
        }
        #endregion


        /// <summary>
        /// ///////////////////////////////////////////////
        /// 激光打标相关
        /// </summary>
 

        #region  相机参数

        //修改曝光参数
        private void nudExposure_ValueChanged(object sender, EventArgs e)
        {
            if (icamSelectInd < 0)
                return;
            cup.p_Cam[icamSelectInd].iExposeTime = (float)nudExposure.Value;
            bool bSucc = hIKVisionCamera.SetExposure(ref cup.p_Cam[icamSelectInd].iExposeTime, camera2[icamSelectInd]);
            if (bSucc)
            {
                nudExposure.Value = (decimal)cup.p_Cam[icamSelectInd].iExposeTime;
                tbExposure.Value = (int)cup.p_Cam[icamSelectInd].iExposeTime;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机曝光参数";
                AddLog();
            }
        }
        //修改曝光参数
        private void tbExposure_ValueChanged(object sender, EventArgs e)
        {
            if (icamSelectInd < 0)
                return;
            cup.p_Cam[icamSelectInd].iExposeTime = tbExposure.Value;
            bool bSucc = hIKVisionCamera.SetExposure(ref cup.p_Cam[icamSelectInd].iExposeTime, camera2[icamSelectInd]);
            if (bSucc)
            {
                nudExposure.Value = (decimal)cup.p_Cam[icamSelectInd].iExposeTime;
                tbExposure.Value = (int)cup.p_Cam[icamSelectInd].iExposeTime;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机曝光参数";
                AddLog();
            }
        }
        //修改曝光参数
        private void chkAutoExposure_Click(object sender, EventArgs e)
        {
            if (icamSelectInd < 0)
                return;
            if (chkAutoExposure.Checked)
            {
                tbExposure.Enabled = false;
                nudExposure.Enabled = false;

                cup.p_Cam[icamSelectInd].iExposeTime = 0;
                bool bSucc = hIKVisionCamera.SetExposure(ref cup.p_Cam[icamSelectInd].iExposeTime, camera2[icamSelectInd]);
                if (bSucc)
                {
                    nudExposure.Value = (decimal)cup.p_Cam[icamSelectInd].iExposeTime;
                    tbExposure.Value = (int)cup.p_Cam[icamSelectInd].iExposeTime;
                }
                else
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "";
                    operLog.strOperEvent = "修改相机曝光参数";
                    AddLog();
                }
            }
            else
            {
                tbExposure.Enabled = true;
                nudExposure.Enabled = true;
            }
        }
        //修改增益参数
        private void tbGain_ValueChanged(object sender, EventArgs e)
        {
            if (icamSelectInd < 0)
                return;
            cup.p_Cam[icamSelectInd].dGain = (float)tbGain.Value / 10;

            bool bSucc = hIKVisionCamera.SetGain(ref cup.p_Cam[icamSelectInd].dGain, camera2[icamSelectInd]);
            if (bSucc)
            {
                nudGain.Value = (decimal)cup.p_Cam[icamSelectInd].dGain;
                tbGain.Value = (int)(cup.p_Cam[icamSelectInd].dGain * 10);
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机增益参数";
                AddLog();
            }
        }
        //修改增益参数
        private void nudGain_ValueChanged(object sender, EventArgs e)
        {
            if (icamSelectInd < 0)
                return;
            cup.p_Cam[icamSelectInd].dGain = (float)nudGain.Value;
            bool bSucc = hIKVisionCamera.SetGain(ref cup.p_Cam[icamSelectInd].dGain, camera2[icamSelectInd]);
            if (bSucc)
            {
                nudGain.Value = (decimal)cup.p_Cam[icamSelectInd].dGain;
                tbGain.Value = (int)(cup.p_Cam[icamSelectInd].dGain * 10);
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机增益参数";
                AddLog();
            }
        }
        //修改锐化参数
        private void tbSharp_ValueChanged(object sender, EventArgs e)
        {
            if (icamSelectInd < 0)
                return;
            cup.p_Cam[icamSelectInd].iSharp = (uint)tbSharp.Value;
            bool bSucc = hIKVisionCamera.SetSharpness(ref cup.p_Cam[icamSelectInd].iSharp, camera2[icamSelectInd]);

            if (bSucc)
            {
                tbSharp.Value = (int)cup.p_Cam[icamSelectInd].iSharp;
                nudSharp.Value = (decimal)cup.p_Cam[icamSelectInd].iSharp;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机锐化参数";
                AddLog();
            }
        }
        //修改锐化参数
        private void nudSharp_ValueChanged(object sender, EventArgs e)
        {
            if (icamSelectInd < 0)
                return;
            cup.p_Cam[icamSelectInd].iSharp = (uint)nudSharp.Value;
            bool bSucc = hIKVisionCamera.SetSharpness(ref cup.p_Cam[icamSelectInd].iSharp, camera2[icamSelectInd]);
            if (bSucc)
            {
                tbSharp.Value = (int)cup.p_Cam[icamSelectInd].iSharp;
                nudSharp.Value = (decimal)cup.p_Cam[icamSelectInd].iSharp;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机锐化参数";
                AddLog();
            }
        }
        //修改Gamma参数
        private void tbGamma_ValueChanged(object sender, EventArgs e)
        {
            if (icamSelectInd < 0)
                return;
            cup.p_Cam[icamSelectInd].dGamma = (float)tbGamma.Value / 10;
            bool bSucc = hIKVisionCamera.SetGamma(ref cup.p_Cam[icamSelectInd].dGamma, camera2[icamSelectInd]);

            if (bSucc)
            {
                tbGamma.Value = (int)(cup.p_Cam[icamSelectInd].dGamma * 10);
                nudGamma.Value = (decimal)cup.p_Cam[icamSelectInd].dGamma;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机Gamma参数";
                AddLog();
            }
        }
        //修改Gamma参数
        private void nudGamma_ValueChanged(object sender, EventArgs e)
        {
            if (icamSelectInd < 0)
                return;
            cup.p_Cam[icamSelectInd].dGamma = (float)nudGamma.Value;
            bool bSucc = hIKVisionCamera.SetGamma(ref cup.p_Cam[icamSelectInd].dGamma, camera2[icamSelectInd]);
            if (bSucc)
            {
                tbGamma.Value = (int)(cup.p_Cam[icamSelectInd].dGamma * 10);
                nudGamma.Value = (decimal)cup.p_Cam[icamSelectInd].dGamma;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机Gamma参数";
                AddLog();
            }

        }
        //修改亮度参数
        private void tbContrast_ValueChanged(object sender, EventArgs e)
        {
            if (icamSelectInd < 0)
                return;
            cup.p_Cam[icamSelectInd].iBrightness = (uint)tbBirghtness.Value;
            //  bool bSucc = hIKVisionCamera.SetBrightness(ref cup.p_Cam[icamSelectInd].iBrightness, camera2[icamSelectInd]);
            bool bSucc = csd.AdjBright((int)cup.p_Cam[icamSelectInd].iBrightness);

            if (bSucc)
            {
                tbBirghtness.Value = (int)cup.p_Cam[icamSelectInd].iBrightness;
                nudContrast.Value = (decimal)cup.p_Cam[icamSelectInd].iBrightness;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改光源亮度参数";
                AddLog();
            }


        }
        //修改亮度参数
        private void nudContrast_ValueChanged(object sender, EventArgs e)
        {
            // tbBirghtness.Value = (int)nudContrast.Value;
            if (icamSelectInd < 0)
                return;
            cup.p_Cam[icamSelectInd].iBrightness = (uint)nudContrast.Value;
            hIKVisionCamera.StopGrabbing(icamSelectInd);
            //  bool bSucc = hIKVisionCamera.SetBrightness(ref cup.p_Cam[icamSelectInd].iBrightness, camera2[icamSelectInd]);
            bool bSucc = csd.AdjBright((int)cup.p_Cam[icamSelectInd].iBrightness);

            hIKVisionCamera.StartGrabbing(icamSelectInd);
            if (bSucc)
            {
                tbBirghtness.Value = (int)cup.p_Cam[icamSelectInd].iBrightness;
                nudContrast.Value = (decimal)cup.p_Cam[icamSelectInd].iBrightness;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机亮度参数";
                AddLog();
            }

        }
        //修改图像格式
        private void cmbPixelFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (icamSelectInd >= 0)
            {
                hIKVisionCamera.StopGrabbing(icamSelectInd);

                cup.p_Cam[icamSelectInd].iMonoType = cmbPixelFormat.SelectedIndex;
                if (cmbPixelFormat.SelectedIndex < 0)
                    cmbPixelFormat.SelectedIndex = 0;
                bool bSucc = hIKVisionCamera.SetPixelFormat(ref cup.p_Cam[icamSelectInd].iMonoType, camera2[icamSelectInd]);

                if (bSucc)
                {
                    operLog.iEventStatus = 0;
                    operLog.strFailReason = "";
                }
                else
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "";
                }
                operLog.strOperEvent = "修改相机图像格式";
                hIKVisionCamera.StartGrabbing(icamSelectInd);
                AddLog();
            }
        }
        //修改图像分辨率
        private void cmbResulation_SelectedIndexChanged(object sender, EventArgs e)
        {
            //  int icamInd = cbDeviceList.SelectedIndex;
            if (icamSelectInd >= 0)
            {
                hIKVisionCamera.StopGrabbing(icamSelectInd);
                string strRes = cmbResulation.Text;
                int ind = strRes.IndexOf('*');
                cup.p_Cam[icamSelectInd].iWidth = uint.Parse(strRes.Substring(0, ind));
                cup.p_Cam[icamSelectInd].iHeight = uint.Parse(strRes.Substring(ind + 1, strRes.Length - ind - 1));

                bool bSucc = hIKVisionCamera.SetResolution(ref cup.p_Cam[icamSelectInd].iWidth, ref cup.p_Cam[icamSelectInd].iHeight, camera2[icamSelectInd]);

                if (bSucc)
                {
                    operLog.iEventStatus = 0;
                    operLog.strFailReason = "";
                }
                else
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "";
                }
                operLog.strOperEvent = "修改相机分辨率";
                hIKVisionCamera.StartGrabbing(icamSelectInd);
                AddLog();
            }
        }

        private void btGetROI_Click(object sender, EventArgs e)
        {
            int icamInd = cbDeviceList.SelectedIndex;
            if (icamInd >= 0)
            {
                hIKVisionCamera.GetROIParam(cup.p_Cam[icamInd], camera2[icamInd]);

                numROIOffSetX.Value = cup.p_Cam[icamInd].iROI[0];
                numROIOffSetY.Value = cup.p_Cam[icamInd].iROI[1];
                numROIWidth.Value = cup.p_Cam[icamInd].iROI[2];
                numROIHeight.Value = cup.p_Cam[icamInd].iROI[3];
            }
        }

        private void btSetROI_Click(object sender, EventArgs e)
        {
            int icamInd = cbDeviceList.SelectedIndex;
            if (icamInd >= 0)
            {
                cup.p_Cam[icamInd].iROI[0] = (uint)numROIOffSetX.Value;
                cup.p_Cam[icamInd].iROI[1] = (uint)numROIOffSetY.Value;
                cup.p_Cam[icamInd].iROI[2] = (uint)numROIWidth.Value;
                cup.p_Cam[icamInd].iROI[3] = (uint)numROIHeight.Value;
                hIKVisionCamera.SetROIParam(cup.p_Cam[icamInd], camera2[icamInd]);
            }
        }

        //
        private void rdRot_Click(object sender, EventArgs e)
        {
            switch (((RadioButton)sender).Name)
            {
                case "rbNoneRotate":
                    cup.p_Cam[icamSelectInd].iRotateType = 0;
                    break;
                case "rbRot90":
                    cup.p_Cam[icamSelectInd].iRotateType = 1;
                    break;
                case "rbRot180":
                    cup.p_Cam[icamSelectInd].iRotateType = 2;
                    break;
                case "rbRot270":
                    cup.p_Cam[icamSelectInd].iRotateType = 3;
                    break;
            }
            acqFIFO._rtType = cup.p_Cam[icamSelectInd].iFlipType * 4 + cup.p_Cam[icamSelectInd].iRotateType;
        }

        private void rdMirror_Click(object sender, EventArgs e)
        {
            switch (((RadioButton)sender).Name)
            {
                case "rdNoneFlip":
                    cup.p_Cam[icamSelectInd].iFlipType = 0;
                    break;
                case "rdFlipHor":
                    cup.p_Cam[icamSelectInd].iFlipType = 1;
                    break;
                case "rdFlipVer":
                    cup.p_Cam[icamSelectInd].iFlipType = 2;
                    break;
            }
            acqFIFO._rtType = cup.p_Cam[icamSelectInd].iFlipType * 4 + cup.p_Cam[icamSelectInd].iRotateType;
        }
        private void cbDeviceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            icamSelectInd = cbDeviceList.SelectedIndex;
        }

        private void UpdateCamParamFromUI()
        {
            if (icamSelectInd >= 0)
            {
                cup.p_Cam[icamSelectInd].iBrightness = (uint)nudContrast.Value;
                cup.p_Cam[icamSelectInd].iExposeTime = (float)nudExposure.Value;
                cup.p_Cam[icamSelectInd].iSharp = (uint)nudSharp.Value;
                cup.p_Cam[icamSelectInd].dGain = (float)nudGain.Value;
                cup.p_Cam[icamSelectInd].dGamma = (float)nudGamma.Value;

                string strRes = cmbResulation.Text;
                int ind = strRes.IndexOf('*');
                cup.p_Cam[icamSelectInd].iWidth = uint.Parse(strRes.Substring(0, ind));
                cup.p_Cam[icamSelectInd].iHeight = uint.Parse(strRes.Substring(ind + 1, strRes.Length - ind - 1));

                cup.p_Cam[icamSelectInd].iMonoType = cmbPixelFormat.SelectedIndex;
                if (cmbPixelFormat.SelectedIndex < 0)
                    cmbPixelFormat.SelectedIndex = 0;
            }
        }
        private void UpdateCamParamToUI()
        {
            if (cbDeviceList.Items.Count > 0)
            {
                cbDeviceList.SelectedIndex = 0;
                int icamInd = cbDeviceList.SelectedIndex;
                try
                {
                    if (icamInd >= 0)
                    {
                        nudContrast.Value = cup.p_Cam[icamInd].iBrightness;
                      //  LightControlTurnOn();

                        nudSharp.Value = cup.p_Cam[icamInd].iSharp;
                        nudExposure.Value = (decimal)cup.p_Cam[icamInd].iExposeTime;
                        nudGain.Value = (decimal)cup.p_Cam[icamInd].dGain;
                        nudGamma.Value = (decimal)cup.p_Cam[icamInd].dGamma;
                        if (cup.p_Cam[icamInd].iMonoType >= 0 && cup.p_Cam[icamInd].iMonoType < cmbPixelFormat.Items.Count)
                            cmbPixelFormat.SelectedIndex = cup.p_Cam[icamInd].iMonoType;
                        string strRes = cup.p_Cam[icamInd].iWidth.ToString() + "* " + cup.p_Cam[icamInd].iHeight.ToString();
                        cmbResulation.Text = strRes;

                        switch (cup.p_Cam[icamInd].iRotateType)
                        {
                            case 0:
                                rbNoneRotate.Checked = true;
                                break;
                            case 1:
                                rbRot90.Checked = true;
                                break;
                            case 2:
                                rbRot180.Checked = true;
                                break;
                            case 3:
                                rbRot270.Checked = true;
                                break;
                        }

                        switch (cup.p_Cam[icamInd].iFlipType)
                        {
                            case 0:
                                rdNoneFlip.Checked = true;
                                break;
                            case 1:
                                rdFlipHor.Checked = true;
                                break;
                            case 2:
                                rdFlipVer.Checked = true;
                                break;
                        }
                        acqFIFO._rtType = cup.p_Cam[icamInd].iFlipType * 4 + cup.p_Cam[icamInd].iRotateType;
                    }
                }
                catch
                {

                }
            }
        }
        int icamSelectInd = -1;
        private void bnSetParam_Click(object sender, EventArgs e)
        {
            // SetParam();

            UpdateCamParamFromUI();
            icamSelectInd = cbDeviceList.SelectedIndex;
            if (icamSelectInd >= 0)
            {
                hIKVisionCamera.StopGrabbing(icamSelectInd);
                bool bSucc = hIKVisionCamera.SetCamParam(cup.p_Cam[icamSelectInd], camera2[icamSelectInd]);
                operLog.strOperEvent = "修改相机参数";
                if (bSucc)
                {
                    operLog.iEventStatus = 0;
                    operLog.strFailReason = "";
                }
                else
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "";

                }
                hIKVisionCamera.StartGrabbing(icamSelectInd);
                AddLog();
            }

        }
        private void bnGetParam_Click(object sender, EventArgs e)
        {
            int icamInd = cbDeviceList.SelectedIndex;
            if (icamInd >= 0)
            {
                hIKVisionCamera.GetCamParam(cup.p_Cam[icamInd], camera2[icamInd]);
                UpdateCamParamToUI();
            }

        }

        private void UpdateCamParam2UI()
        {
            LightControlTurnOn();
            UpdateCamParamToUI();
        }
        #endregion
        #region  模板匹配参数设置
        PMAlignParam pmaParam = new PMAlignParam();
        //从UI界面更新参数到VP中PMA
        private void UpdatePMAParamFromUI()
        {
            //训练参数
            pmaParam.TrainAlg = cmbPMATrainAlg.SelectedIndex;
            pmaParam.TrainPolarity = chkIgnPolarity.Checked;
            pmaParam.TrainEdgeThre = (double)nudPMATrainEdgeThre.Value;
            pmaParam.TrainAutoEdgeThre = chkPMAAutoEdgeThre.Checked;
            pmaParam.TrainCoarse = (double)nudPMACoarse.Value;
            pmaParam.TrainFine = (double)nudPMAFine.Value;
            pmaParam.TrainAutoGrun = chkPMAAutoGran.Checked;

            //运行参数
            pmaParam.RunAlg = cmbPMARunAlg.SelectedIndex;
            pmaParam.RunArt = chkPMAArt.Checked;
            pmaParam.RunEdgeThre = (double)nudPMARunEdgeThre.Value;
            pmaParam.RunAutoEdgeThre = chkPMARunAutoEdgeThre.Checked;
            pmaParam.RunConstractThre = (double)nudPMAConstractThre.Value;
            pmaParam.RunSearchNum = (int)nudPMASearchNum.Value;
            pmaParam.RunScoreThre = (double)nudPMAScoreThre.Value;
            pmaParam.RunEnableAngle = chkPMARunWithAngle.Checked;
            pmaParam.RunAngleD = (double)nudPMARunAngleD.Value;
            pmaParam.RunAngleU = (double)nudPMARunAngleU.Value;
        }
        //从VP中PMA更新参数到UI界面
        private void UpdateUIFromPMAParam()
        {

            try
            {   //训练参数
                if (pmaParam.TrainAlg >= 0 && pmaParam.TrainAlg <= cmbPMATrainAlg.Items.Count)
                {
                    cmbPMATrainAlg.SelectedIndex = pmaParam.TrainAlg;
                }
                else
                {
                    cmbPMATrainAlg.SelectedIndex = 2;
                }

                chkIgnPolarity.Checked = pmaParam.TrainPolarity;
                if (pmaParam.TrainEdgeThre > (double)nudPMATrainEdgeThre.Maximum)
                    nudPMATrainEdgeThre.Value = nudPMATrainEdgeThre.Maximum;
                else if (pmaParam.TrainEdgeThre < (double)nudPMATrainEdgeThre.Minimum)
                    nudPMATrainEdgeThre.Value = nudPMATrainEdgeThre.Minimum;
                else
                    nudPMATrainEdgeThre.Value = (decimal)pmaParam.TrainEdgeThre;
                chkPMAAutoEdgeThre.Checked = pmaParam.TrainAutoEdgeThre;
                nudPMACoarse.Value = (decimal)pmaParam.TrainCoarse;
                nudPMAFine.Value = (decimal)pmaParam.TrainFine;

                //运行参数
                if (pmaParam.RunAlg >= 0 && pmaParam.RunAlg <= cmbPMARunAlg.Items.Count)
                {
                    cmbPMARunAlg.SelectedIndex = pmaParam.RunAlg;
                }
                else
                {
                    cmbPMARunAlg.SelectedIndex = 2;
                }

                chkPMAArt.Checked = pmaParam.RunArt;

                if (pmaParam.RunEdgeThre > (double)nudPMARunEdgeThre.Maximum)
                    nudPMARunEdgeThre.Value = nudPMARunEdgeThre.Maximum;
                else if (pmaParam.RunEdgeThre < (double)nudPMARunEdgeThre.Minimum)
                    nudPMARunEdgeThre.Value = nudPMARunEdgeThre.Minimum;
                else
                    nudPMARunEdgeThre.Value = (decimal)pmaParam.RunEdgeThre;
                chkPMARunAutoEdgeThre.Checked = pmaParam.RunAutoEdgeThre;

                if (pmaParam.RunConstractThre > (double)nudPMAConstractThre.Maximum)
                    nudPMAConstractThre.Value = nudPMAConstractThre.Maximum;
                else if (pmaParam.RunConstractThre < (double)nudPMAConstractThre.Minimum)
                    nudPMAConstractThre.Value = nudPMAConstractThre.Minimum;
                else
                    nudPMAConstractThre.Value = (decimal)pmaParam.RunConstractThre;

                if (pmaParam.RunSearchNum > (double)nudPMASearchNum.Maximum)
                    nudPMASearchNum.Value = nudPMASearchNum.Maximum;
                else if (pmaParam.RunSearchNum < (double)nudPMASearchNum.Minimum)
                    nudPMASearchNum.Value = nudPMASearchNum.Minimum;
                else
                    nudPMASearchNum.Value = (decimal)pmaParam.RunSearchNum;

                if (pmaParam.RunScoreThre > (double)nudPMAScoreThre.Maximum)
                    nudPMAScoreThre.Value = nudPMAScoreThre.Maximum;
                else if (pmaParam.RunScoreThre < (double)nudPMAScoreThre.Minimum)
                    nudPMAScoreThre.Value = nudPMAScoreThre.Minimum;
                else
                    nudPMAScoreThre.Value = (decimal)pmaParam.RunScoreThre;


                chkPMARunWithAngle.Checked = pmaParam.RunEnableAngle;
                if (pmaParam.RunAngleD > (double)nudPMARunAngleD.Maximum)
                    nudPMARunAngleD.Value = nudPMARunAngleD.Maximum;
                else if (pmaParam.RunAngleD < (double)nudPMARunAngleD.Minimum)
                    nudPMARunAngleD.Value = nudPMARunAngleD.Minimum;
                else
                    nudPMARunAngleD.Value = (decimal)pmaParam.RunAngleD;

                if (pmaParam.RunAngleU > (double)nudPMARunAngleU.Maximum)
                    nudPMARunAngleU.Value = nudPMARunAngleU.Maximum;
                else if (pmaParam.RunAngleU < (double)nudPMARunAngleU.Minimum)
                    nudPMARunAngleU.Value = nudPMARunAngleU.Minimum;
                else
                    nudPMARunAngleU.Value = (decimal)pmaParam.RunAngleU;
            }
            catch
            {

            }

        }

        //获取PMA参数
        private void btGetPMAParam_Click(object sender, EventArgs e)
        {
            vpWin.GetPMAParam(ref pmaParam);
            UpdateUIFromPMAParam();
        }
        //设置PMA参数
        private void btSetPMAParam_Click(object sender, EventArgs e)
        {
            UpdatePMAParamFromUI();
            vpWin.SetPMAParam(pmaParam);
        }
        //PMA工具打开 
        private void btPMAlignParam_Click(object sender, EventArgs e)
        {
            {
                if (vpWin._iTaskType != 5 && !vpWin.bMainTaskFinished)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "模板定位";
                    operLog.strFailReason = "当前任务未结束";
                    AddLog();
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

                if (rts.initStatus.iVPPStatus != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "模板定位";
                    operLog.strFailReason = "流程文件初始化失败";
                    return;
                }
                operLog.strOperEvent = "模板定位";


                bPMAEdit = !bPMAEdit;
                if (bPMAEdit)
                {
                    iTmpType = vpWin._iTaskType;
                    pmaV2 = new CogPMAlignEditV2();
                    pmaV2.Subject = vpWin._pmaMT;
                    splitContainer3.Panel1.Controls.Add(pmaV2);
                    pmaV2.Dock = DockStyle.Fill;
                    vpWin.Visible = false;
                    bVPPParamChanged = true;


                    vpWin._iTaskType = 5;
                    operLog.iEventStatus = 0;
                    ((Button)sender).FlatStyle = FlatStyle.Flat;
                    vpWin.bMainTaskFinished = false;
                }
                else
                {
                    vpWin.setPMA(pmaV2.Subject);
                    splitContainer3.Panel1.Controls.Remove(pmaV2);
                    pmaV2.Dispose();


                    vpWin._iTaskType = iTmpType;
                    ((Button)sender).FlatStyle = FlatStyle.Popup;
                    vpWin.bMainTaskFinished = true;

                    vpWin.Visible = true;

                }

            }
        }
        //训练阶段自动边缘阈值
        private void chkPMAAutoEdgeThre_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPMAAutoEdgeThre.Checked)
            {
                nudPMATrainEdgeThre.Enabled = false;
            }
            else
            {
                nudPMATrainEdgeThre.Enabled = true;
            }
        }
        //训练阶段自动特征粒度
        private void chkPMAAutoGran_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPMAAutoGran.Checked)
            {
                nudPMAFine.Enabled = false;
                nudPMACoarse.Enabled = false;
            }
            else
            {
                nudPMAFine.Enabled = true;
                nudPMACoarse.Enabled = true;
            }

        }
        //运行阶段自动边缘阈值
        private void chkPMARunAutoEdgeThre_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPMARunAutoEdgeThre.Checked)
            {
                nudPMARunEdgeThre.Enabled = false;
            }
            else
            {
                nudPMARunEdgeThre.Enabled = false;
            }
        }
        //使能角度旋转匹配
        private void chkPMARunWithAngle_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPMARunWithAngle.Checked)
            {
                nudPMARunAngleD.Enabled = false;
                nudPMARunAngleU.Enabled = false;
            }
            else
            {
                nudPMARunAngleD.Enabled = true;
                nudPMARunAngleU.Enabled = true;
            }
        }
        #endregion

        //激光视觉标定
        //
        #region  显示9点标定数据
        double[] dCalibPoint = new double[36];
        double[] dCalibPointIm = new double[18];
        double[] dCalibPointLaser = new double[18];
       // calibTemplateParam calTP = new calibTemplateParam();
        calibBlobParam cbp = new calibBlobParam();
        CalibRes cr = new CalibRes();
        int featurePointNum = 9;
        #endregion

        #region  //9点标定模板分割 
        private void UpdateLaserVisionCalibParam2UIFromVP()
        {
            vpWin.GetCalibPoint(ref dCalibPoint);
            Update9PointCalibPoint2UIFromVar();
            vpWin.GetCalibBlobParam(ref cbp);
            Update9PointBlobParam2UIFromVar();
            vpWin.Get9PointCalibRes(ref cr);
            UpdateCalibResult2UIFromVar();
            //补充模板设计数据
        }
        private bool UpdateLaserVisionCalibParam2VPFromUI()
        {
            bool bNormal = true;
            bNormal &= Update9PointCalibPoint2VarFromUI();
            vpWin.SetCalibPoint(dCalibPoint);
            UpdateCalibBlobParam2VarFromUI();
            vpWin.SetCalibBlobParam(cbp);
            return bNormal;
        }

        //从坐标变量更新数据到UI界面
        private void Update9PointCalibPoint2UIFromVar()
        {
            dgvMarkCood.Rows.Clear();

            //坐标数据更新到UI界面
            for (int i = 0; i < dCalibPoint.Length / 4; i++)
            {
                dgvMarkCood.Rows.Add();
                dgvMarkCood.Rows[i].HeaderCell.Value = (i + 1).ToString();
                dgvMarkCood.Rows[i].Cells[0].Value = dCalibPoint[i * 4].ToString("0.000");
                dgvMarkCood.Rows[i].Cells[1].Value = dCalibPoint[i * 4 + 1].ToString("0.000");
                dgvMarkCood.Rows[i].Cells[2].Value = dCalibPoint[i * 4 + 2];
                dgvMarkCood.Rows[i].Cells[3].Value = dCalibPoint[i * 4 + 3];
            }
            //   dgvMarkCood.AllowUserToAddRows = false;
        }
        private bool Update9PointCalibPoint2VarFromUI()
        {
            int nP = dgvMarkCood.Rows.Count;
            if (dCalibPoint == null || dCalibPoint.Length != nP * 4)
            {
                dCalibPoint = new double[nP * 4];
            }
            bool bNormal = true;

            for (int i = 0; i < nP; i++)
            {
                bNormal &= double.TryParse(dgvMarkCood.Rows[i].Cells[0].Value.ToString(), out dCalibPoint[i * 4]);
                bNormal &= double.TryParse(dgvMarkCood.Rows[i].Cells[1].Value.ToString(), out dCalibPoint[i * 4 + 1]);
                bNormal &= double.TryParse(dgvMarkCood.Rows[i].Cells[2].Value.ToString(), out dCalibPoint[i * 4 + 2]);
                bNormal &= double.TryParse(dgvMarkCood.Rows[i].Cells[3].Value.ToString(), out dCalibPoint[i * 4 + 3]);
            }

            return bNormal;
        }

        //从Blob参数变量更新数据到界面UI中
        private void Update9PointBlobParam2UIFromVar()
        {
            cmbPolarity.SelectedIndex = cbp.bPolarity ? 0 : 1;
            cmbThreModel.SelectedIndex = cbp.iSegType;
            numUDThre.Value = cbp.iThre;
            numUDAreaMin.Value = cbp.iAreaMin;
            numUDAreaMax.Value = cbp.iAreaMax;
        }
        private void UpdateCalibBlobParam2VarFromUI()
        {
            cbp.bPolarity = cmbPolarity.SelectedIndex == 0;
            cbp.iSegType = cmbThreModel.SelectedIndex;
            cbp.iThre = (int)numUDThre.Value;
            cbp.iAreaMin = (int)numUDAreaMin.Value;
            cbp.iAreaMax = (int)numUDAreaMax.Value;
        }

        private void UpdateCalibResult2UIFromVar()
        {
            tbCalibCentX.Text = cr.dTX.ToString("0.00");
            tbCalibCentY.Text = cr.dTY.ToString("0.00");
            tbCalibScale.Text = cr.dScale.ToString("0.00");
            tbCalibXYRatio.Text = cr.dXYRatio.ToString("0.00");
            tbCalibRotate.Text = cr.dTR.ToString("0.00");
            tbCalibSkew.Text = cr.dSkew.ToString("0.00");
            tbCalibErr.Text=cr.dErr.ToString("0.00");
        }


        #region blob 参数修改
        //blob极性修改  黑底白点   白底黑点
        private void cmbPolarity_SelectedIndexChanged(object sender, EventArgs e)
        {
            //   UpdateCalibBlobParam2VarFromUI();
        }
        //blob阈值模式修改   固定硬阈值  动态硬阈值
        private void cmbThreModel_SelectedIndexChanged(object sender, EventArgs e)
        {
            //    UpdateCalibBlobParam2VarFromUI();
        }

        //blob阈值修改
        private void trackBarThre_ValueChanged(object sender, EventArgs e)
        {
            numUDThre.Value = trackBarThre.Value;
            //  UpdateCalibBlobParam2VarFromUI();
            // cbp.iThre = (int)numUDThre.Value; 
        }
        //blob阈值修改
        private void numUDThre_ValueChanged(object sender, EventArgs e)
        {
            trackBarThre.Value = (int)numUDThre.Value;
            //   UpdateCalibBlobParam2VarFromUI();
        }

        //blob 面积筛选 最小面积
        private void numUDAreaMin_ValueChanged(object sender, EventArgs e)
        {
            //  UpdateCalibBlobParam2VarFromUI();
        }

        //blob 面积筛选 最大面积
        private void numUDAreaMax_ValueChanged(object sender, EventArgs e)
        {
            //   UpdateCalibBlobParam2VarFromUI();
        }
        #endregion blob 参数修改

        // 从界面获取模板参数到相应变量中
        private void UpdateCalibTemplate2VarFromUI()
        {
            //模板基本参数
            {
                cup.calTP.iRow = (int)numUDRow.Value;
                cup.calTP.iCol = (int)numUDCol.Value;
                cup.calTP.dRadius = (double)numUDRad.Value;
                cup.calTP.dDisX = (double)numUDDisX.Value;
                cup.calTP.dDisY = (double)numUDDisY.Value;
                cup.calTP.dCentX = (double)numUDCenX.Value;
                cup.calTP.dCentY = (double)numUDCenY.Value;
            }
            if (icamSelectInd < 0)
                return;
            //模板标刻坐标
            {
                int count = cup.calTP.iRow * cup.calTP.iCol;
                featurePointNum = count;
                if (dCalibPointLaser.Length != count * 2)
                {
                    dCalibPointLaser = new double[count * 2];
                }
                int ind = 0;
                for (double i = -(cup.calTP.iRow - 1) / 2; i <= (cup.calTP.iRow - 1) / 2; i++)
                {
                    for (double j = -(cup.calTP.iCol - 1) / 2; j <= (cup.calTP.iCol - 1) / 2; j++)
                    {
                       
                        dCalibPointLaser[ind * 2] = j * cup.calTP.dDisX;
                        dCalibPointLaser[ind * 2 + 1] = i * cup.calTP.dDisY;
                        if (cup.p_Cam[icamSelectInd].iFlipType==1)
                            dCalibPointLaser[ind * 2] = -j * cup.calTP.dDisX;
                        if (cup.p_Cam[icamSelectInd].iFlipType == 2)
                            dCalibPointLaser[ind * 2 + 1] = -i * cup.calTP.dDisY;
                        ind++;
                    }
                }
            }
        }
        //9点标定模板界面参数初始化
        private void UpdateLaserVisionCalibTemplate2UIFromVar()
        {
            //模板基本参数
            {
                numUDRow.Value = cup.calTP.iRow;
                numUDCol.Value = cup.calTP.iCol;
                numUDRad.Value = (decimal)cup.calTP.dRadius;
                numUDDisX.Value = (decimal)cup.calTP.dDisX;
                numUDDisY.Value = (decimal)cup.calTP.dDisY;
                numUDCenX.Value = (decimal)cup.calTP.dCentX;
                numUDCenY.Value = (decimal)cup.calTP.dCentY;
            }
        }


        #region  模板参数修改
        //模板行数修改
        private void numUDRow_ValueChanged(object sender, EventArgs e)
        {
               UpdateCalibTemplate2VarFromUI();
        }
        //模板列数修改
        private void numUDCol_ValueChanged(object sender, EventArgs e)
        {
               UpdateCalibTemplate2VarFromUI();
        }
        //模板半径修改
        private void numUDRad_ValueChanged(object sender, EventArgs e)
        {
               UpdateCalibTemplate2VarFromUI();
        }
        //模板中心X 修改
        private void numUDCenX_ValueChanged(object sender, EventArgs e)
        {
               UpdateCalibTemplate2VarFromUI();
        }

        //模板中心Y 修改
        private void numUDCenY_ValueChanged(object sender, EventArgs e)
        {
               UpdateCalibTemplate2VarFromUI();
        }
        //模板X向间距修改
        private void numUDDisX_ValueChanged(object sender, EventArgs e)
        {
              UpdateCalibTemplate2VarFromUI();
        }
        //模板Y向间距修改
        private void numUDDisY_ValueChanged(object sender, EventArgs e)
        {
               UpdateCalibTemplate2VarFromUI();
        }
        #endregion

        //更新表格
        private void UpdateCalibPointTable()
        {
            dgvMarkCood.Rows.Clear();

            for (int i = 0; i < featurePointNum; i++)
            {
                dgvMarkCood.Rows.Add();
                dgvMarkCood.Rows[i].HeaderCell.Value = (i + 1).ToString();
                dgvMarkCood.Rows[i].Cells[0].Value = dCalibPointIm[i * 2].ToString("0.000");
                dgvMarkCood.Rows[i].Cells[1].Value = dCalibPointIm[i * 2 + 1].ToString("0.000");
                dgvMarkCood.Rows[i].Cells[2].Value = dCalibPointLaser[i * 2];
                dgvMarkCood.Rows[i].Cells[3].Value = dCalibPointLaser[i * 2 + 1];
            }
        }


        //blob工具高级设置
        #region
        CogBlobEditV2 blobV2;
        bool bBlobEdit = false;
        private void btCalibBlobParam_Click(object sender, EventArgs e)
        {
            bBlobEdit = !bBlobEdit;
            if (bBlobEdit)
            {
                iTmpType = vpWin._iTaskType;
                blobV2 = new CogBlobEditV2();
                blobV2.Subject = vpWin._blobCalib;
                splitContainer3.Panel1.Controls.Add(blobV2);
                blobV2.Dock = DockStyle.Fill;
                vpWin.Visible = false;
                bVPPParamChanged = true;


                vpWin._iTaskType = 5;   //?
                operLog.iEventStatus = 0;
                ((Button)sender).FlatStyle = FlatStyle.Flat;
                vpWin.bMainTaskFinished = false;
            }
            else
            {
                vpWin.setBlob(blobV2.Subject);
                splitContainer3.Panel1.Controls.Remove(blobV2);
                blobV2.Dispose();


                vpWin._iTaskType = iTmpType;
                ((Button)sender).FlatStyle = FlatStyle.Popup;
                vpWin.bMainTaskFinished = true;

                vpWin.Visible = true;

            }
        }
        #endregion

        private void MarkCalibTemplate()
        {
            
        }
       

        private void btMarkCalib_Click(object sender, EventArgs e)
        { 
        }
        private void btUpdateCalibDate_Click(object sender, EventArgs e)
        {
            //采集模板图像
            CaptureCalibImage();  
            //blob分析并获取远心坐标
            vpWin.GetCalibBlobCoord(cup.calTP.iRow, cup.calTP.iCol, ref dCalibPointIm); //更新图像坐标
           // UpdateCalibTemplate2VarFromUI();                    //更新激光坐标
            UpdateCalibPointTable();                       //更新表格数据
        }
        private void btCalib_Click(object sender, EventArgs e)
        { 
            bool bNormal = UpdateLaserVisionCalibParam2VPFromUI();
            if (bNormal)
            {
               cr= vpWin._9PointCalib(dCalibPoint);
                UpdateCalibResult2UIFromVar();
            }
            else  //addlog
            {

            }
        }

      
        private void btMarkCross_Click(object sender, EventArgs e)
        {
            //   加载十字线文件
          /*  bCmdEnd = false;
            lczParam.strFileName = Path.Combine(cup.p_Paf.ManuMarkPath, "Cross.ezd");
            lczParam.cmdType = 4;
            lczParam.ind3 = 2;
            LaserMarkThread = new Thread(laserMarkThread);
            LaserMarkThread.Start();*/
            //   Thread.Sleep(500);


            /* while (!bCmdEnd)
             {
                 Thread.Sleep(5);
             }
             Thread.Sleep(50);   //等待稳定*/

            //    CaptureCalibImage();            //采集图像并执行blob检测
        }

        private void btMarkSquare_Click(object sender, EventArgs e)
        {
            //   加载方框文件
         /*   bCmdEnd = false;
            lczParam.strFileName = Path.Combine(cup.p_Paf.ManuMarkPath, "Square.ezd");
            lczParam.cmdType = 4;
            lczParam.ind3 = 3;
            LaserMarkThread = new Thread(laserMarkThread);
            LaserMarkThread.Start();*/
        }

        private void CaptureCalibImage()
        {
            operLog.strOperEvent = "采集9点标定图像";
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                return;
            }
            if (cup.p_VPPRTP.bAcqFromCam && !rts.initStatus.bCamInit)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "相机未初始化";
                AddLog();
                return;
            } 
            if (cup.p_VPPRTP.bAcqFromCam &&  cbDeviceList.SelectedIndex < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "没有选择相机";
                AddLog();
                return;
            } 


            vpWin._iTaskType = 0;
            rts.iTaskType = vpWin._iTaskType;
            //  iAcquisitionsTimes++;
            bool bRet = false;

            vpWin.bMainTaskFinished = false;
            if (cup.p_VPPRTP.bAcqFromCam)  //实时采集
            {
                bRet = vpWin.PreViewCalibImage(cbDeviceList.SelectedIndex);
                if (!bRet)
                {

                }
                vpWin.bMainTaskFinished = true;
            }
            else
            {
                bRet = vpWin.PreViewOneCalibImageFromFile(cup.p_VPPRTP.iXBType);

                operLog.strFailReason = "回放异常错误";
            }
        }

        //初始化激光视觉标定参数
        private void InitParamUI()
        {
            //去掉两个页面，后续直接从布局中剔除
            /* tabControl1.TabPages.Remove(tabControl1.TabPages[4]);
             tabControl1.TabPages.Remove(tabControl1.TabPages[3]);*/
            tabControl1.TabPages.Remove(tabControl1.TabPages[2]);
            if (cbDeviceList.Items.Count>0)
                cbDeviceList.SelectedIndex = 0;
           
            cmbPMATrainAlg.SelectedIndex = 2;
            cmbPMARunAlg.SelectedIndex = 2;

            //9点标定模板界面参数初始化
            if (rts.initStatus.bVPPInit)
            {
                //    UpdateLaserVisionCalibTemplate2UIFromVar(); 
                //加载标定参数
                //     UpdateLaserVisionCalibParam2UIFromVP();

                //加载PMA模板匹配参数
                vpWin.GetPMAParam(ref pmaParam);
                UpdateUIFromPMAParam();
            }
            //加载相机参数
            UpdateCamParam2UI();  

         

            ViewLayout();
        }

        private void laserMarkThread()
        {
           
        }

        #endregion
        
        #region  对象操作
         
        private LoadingForm loadingForm;
       
        int iEntIndex = -1;
    
        //获取图像
     
        #endregion
 
    }
}
