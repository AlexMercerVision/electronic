using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;


using Cognex.VisionPro;
using Cognex.VisionPro.QuickBuild;
using Cognex.VisionPro.QuickBuild.Implementation.Internal;
using Cognex.VisionPro.ToolGroup;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.Implementation;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.Blob;
using Cognex.VisionPro.Caliper;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.ImageProcessing;
using Cognex.VisionPro.CalibFix;
using HalconDotNet;
using MvCamCtrl.NET;  // 海康相机
using CommonCls;
using System.Runtime.Serialization.Formatters.Binary;
//using gRPC_ClientAPP;
//using convertService;
//List<CogPolygon> gpgs

namespace VisionControl
{
 

    public partial class VisionControl : UserControl
    {

        //**20220416  开放单次运行接口
        #region  变量定义
        //   CDL_Predict cdl_Predict = new CDL_Predict();
        public int iReject = 0;
        const int maxCameraNum = 5;
        ImgAcqFIFO acqFIFO = new ImgAcqFIFO();
        Thread contThread = null;
        bool bContRun = false;
       // CogImage8Grey ci8g_record, ci8g_preview, cigAcq;
        CogImage8Grey[] ci8gAcq = new CogImage8Grey[maxCameraNum];
        CogImage8Grey[] ci8gFX = new CogImage8Grey[maxCameraNum];
      //  HObject[] hObjFX = new HObject[6];              //光度立体产生的图像


        HTuple WindowHandleSub1 = -1, WindowHandleSub2 = -1;
        bool hikCanGrab = false;
        uint cameraNum = 0;
        private delegate void UpdateUI();  // 声明委托
        Thread hikGrabThread = null;

        //  string strSigSavePath = "SigSave\\";    //单帧保存地址
        //  string strConSavePath = "contSave\\";    //连续帧保存地址
        string strSaveResPath = "";

        //  string strSigMain, strSigSub1, strSigSub2;
        //   string strConMain, strConSub1, strConSub2;
        string[] strSigSavePathS = new string[maxCameraNum];
        string[] strConSavePathS = new string[maxCameraNum];

        CogImageFile mImageFile = new CogImageFile();
        bool bRecord = false;
        int[] iEventStatus = new int[6];
        int bRunOneTimeFinished = 0;
        private CUserParam cup = new CUserParam();

        public string strErrInfo;
        int iResCode, iDM0ResCode, iCM1ResCode, iCM2ResCode, iCM3ResCode, iCM4ResCode, xbType;
        public MyCamera.cbOutputExdelegate cbImage;
        //   private string[] strSubRecordsName;

        int iSubSlectIndex = 1;
        public bool bChkSubrecord = true;  //是否进行清单筛选
        int iTaskType = -1;
        bool[] bTaskFinish = new bool[maxCameraNum];

        public bool bMainTaskFinished = true;

        //深度学习相关
        public bool bDeepInfer = true; //是否启用深度推理  检测VP算法时可以关闭深度推理
        public CDL cdl = new CDL();
        List<CogRectangleAffine>[] AL_det_result = new List<CogRectangleAffine>[maxCameraNum];

        DLResult[] DL_result = new DLResult[maxCameraNum];


        private PMARes pmaRes;
        public PMARes _PMARes
        {
            get { return pmaRes; }
            set { pmaRes = value; }
        }
        //DLResult[] DL_result=new DLResult[maxCameraNum];
        public int _iTaskType
        {
            get { return iTaskType; }
            set { iTaskType = value; }
        }
        public int _xbType
        {
            get { return xbType; }
            set { xbType = value; }
        }

        public int _iResCode
        {
            get { return iResCode; }
        }
        string[] strSubRecordsName = new string[maxCameraNum];


        //识别结果存放
        public string _strSaveResPath
        {
            get { return strSaveResPath; }
            set { strSaveResPath = value; }
        }
        // 
        public string[] _strSigSavePathS
        {
            get { return strSigSavePathS; }
            set { strSigSavePathS = value; }
        }
        // 
        public string[] _strConSavePathS
        {
            get { return strConSavePathS; }
            set { strConSavePathS = value; }
        }


        public int mInitError = 0;
        /*  public string[] _strSubRecordsName
          {
              get { return strSubRecordsName; }
              set { strSubRecordsName = value; }
          }
  */
        public CUserParam _cup
        {
            get { return cup; }
            set { cup = value; }
        }
        public bool _bRecord
        {
            get { return bRecord; }
            set { bRecord = value; }
        }
        public int _bRunOneTimeFinished
        {
            get { return bRunOneTimeFinished; }
            set { bRunOneTimeFinished = value; }
        }
        int iToolIndex = 0;
        public bool bActivateRun = false;
        int inactiveTime = 0;
        public int iFileIndex = 0;
        public uint _cameraNum
        {
            get { return cameraNum; }
            set { cameraNum = value; }
        }

        public int cost_milliseconds;
        TimeSpan infer_start_time;
        TimeSpan infer_end_time;
        TimeSpan start2end_time;


        //深度学习相关
       // public bool bDeepInfer = true; //是否启用深度推理  检测VP算法时可以关闭深度推理
    //    public CDL cdl = new CDL();
        #endregion



        #region  VP执行运算
        //防假死激活运行
        public void ActivateRun()
        {
            if (mJM == null || mJM.JobsRunningState != CogJobsRunningStateConstants.None)
                return;

            if (mCurrentRunState == RunState.RunningOnce)
                return;

            inactiveTime++;
            inactiveTime = inactiveTime % 10;
            int toolNum= (mJM.Job(0).VisionTool as CogToolGroup).Tools.Count;  //获取任务中工具的数量
            if (inactiveTime == 0)   //连续10次不活跃，执行一个工具
            {
                bActivateRun = true;
               (mJM.Job(0).VisionTool as CogToolGroup).Tools[iToolIndex].Run();  // //20250620  根据脚本代码，此处应该是阻塞执行，不用判断是否执行完毕 

                // ((mJM.Job(0).VisionTool as CogToolGroup).Tools[iToolIndex].RunStatus.Result== CogToolResultConstants.Accept) 
                bActivateRun = false;
                iToolIndex += 1;
                iToolIndex = iToolIndex % toolNum;
            }

        }
        bool[] bSureCheck = new bool[6];
        //VP执行一次运算
        private void AddGrapOkNg(CogRecordDisplay cogDisp, bool bOk)
        {
            //创建文本
            CogGraphicLabel ResultLabel = new CogGraphicLabel();//创建标签对象
            ResultLabel.X = 300;
            ResultLabel.Y = 100;
            if(bOk)
            {
                ResultLabel.Text = "OK";
                ResultLabel.Color = CogColorConstants.Green;
                ResultLabel.Font = new Font("arial", 30);
            }
            else
            {
                ResultLabel.Text = "NG";
                ResultLabel.Color = CogColorConstants.Red;
                ResultLabel.Font = new Font("arial", 30);
            }
            cogDisp.InteractiveGraphics.Add(ResultLabel, "", false);
        }
        private void AddGrapOkNg(CogRecordsDisplay cogDisp, bool bOk)
        {
            //创建文本
            CogGraphicLabel ResultLabel = new CogGraphicLabel();//创建标签对象
            ResultLabel.X = 300;
            ResultLabel.Y = 100;
            if (bOk)
            {
                ResultLabel.Text = "OK";
                ResultLabel.Color = CogColorConstants.Green;
                ResultLabel.Font = new Font("arial", 30);
            }
            else
            {
                ResultLabel.Text = "NG";
                ResultLabel.Color = CogColorConstants.Red;
                ResultLabel.Font = new Font("arial", 30);
            }
            cogDisp.Display.InteractiveGraphics.Add(ResultLabel, "", false);
        }
        private void AddGrap(CogRecordDisplay cogDisp, DLResult dlRes)
        {
            string strGroupName = "";
            cogDisp.InteractiveGraphics.Clear();

            
            //创建文本
            CogGraphicLabel ResultLabel = new CogGraphicLabel();//创建标签对象

            //     ResultLabel.Text = "Label Test,添加文本测试";

            for (int i = 0; i < dlRes.AL_Rect.Count; i++)
            {
                if (dlRes.AL_Type[i] == 1)
                {
                    dlRes.AL_Rect[i].Color = CogColorConstants.Red;
                    /*strGroupName = "liewen";
                    ResultLabel.Color = CogColorConstants.Red;
                    ResultLabel.Font = new Font("arial", 10);*/
                }
                else if (dlRes.AL_Type[i] == 2)
                {
                    dlRes.AL_Rect[i].Color = CogColorConstants.Green;
                    /* strGroupName = "posz";
                     ResultLabel.Color = CogColorConstants.Green;
                     ResultLabel.Font = new Font("arial", 10);*/
                }
                else if (dlRes.AL_Type[i] == 3)
                {
                    dlRes.AL_Rect[i].Color = CogColorConstants.Blue;
                    /*strGroupName = "liewenWP";
                    ResultLabel.Color = CogColorConstants.Green;
                    ResultLabel.Font = new Font("arial", 10);*/
                }
                else if (dlRes.AL_Type[i] == 4)
                {
                    dlRes.AL_Rect[i].Color = CogColorConstants.Cyan;
                    /* strGroupName = "poszWP";
                     ResultLabel.Color = CogColorConstants.Green;
                     ResultLabel.Font = new Font("arial", 10);*/
                }
                else if (dlRes.AL_Type[i] == 5)  //容易误判类别，屏蔽处理
                {
                    /* dlRes.AL_Rect[i].Color = CogColorConstants.Green;
                     strGroupName = "flaking";
                     ResultLabel.Color = CogColorConstants.Green;
                     ResultLabel.Font = new Font("arial", 10);*/
                }
                //    dlRes.AL_Rect[i].TipText = dlRes.AL_score[i].ToString();
                ResultLabel.X = dlRes.AL_Rect[i].CornerYX;
                ResultLabel.Y = dlRes.AL_Rect[i].CornerXY;
                ResultLabel.Text = strGroupName + ":" + Math.Round(dlRes.AL_score[i], 2).ToString();
                strGroupName += i;
                cogDisp.InteractiveGraphics.Add(dlRes.AL_Rect[i], strGroupName, false);
               // cogDisp.InteractiveGraphics.Add(ResultLabel, strGroupName, false);
              //  ResultLabel = new CogGraphicLabel();
            }
        }
        private void AddGrap(CogRecordsDisplay cogDisp, DLResult dlRes)
        {
            string strGroupName = "";
            cogDisp.Display.InteractiveGraphics.Clear();
             
            //创建文本
            CogGraphicLabel ResultLabel = new CogGraphicLabel();//创建标签对象

            //     ResultLabel.Text = "Label Test,添加文本测试";

            for (int i = 0; i < dlRes.AL_Rect.Count; i++)
            {
                if (dlRes.AL_Type[i] == 1)
                {
                    dlRes.AL_Rect[i].Color = CogColorConstants.Red;
                    /*strGroupName = "liewen";
                    ResultLabel.Color = CogColorConstants.Red;
                    ResultLabel.Font = new Font("arial", 10);*/
                }
                else if (dlRes.AL_Type[i] == 2)
                {
                    dlRes.AL_Rect[i].Color = CogColorConstants.Green;
                   /* strGroupName = "posz";
                    ResultLabel.Color = CogColorConstants.Green;
                    ResultLabel.Font = new Font("arial", 10);*/
                }
                else if (dlRes.AL_Type[i] == 3)
                {
                    dlRes.AL_Rect[i].Color = CogColorConstants.Blue;
                    /*strGroupName = "liewenWP";
                    ResultLabel.Color = CogColorConstants.Green;
                    ResultLabel.Font = new Font("arial", 10);*/
                }
                else if (dlRes.AL_Type[i] == 4)
                {
                    dlRes.AL_Rect[i].Color = CogColorConstants.Cyan;
                   /* strGroupName = "poszWP";
                    ResultLabel.Color = CogColorConstants.Green;
                    ResultLabel.Font = new Font("arial", 10);*/
                }
                else if (dlRes.AL_Type[i] == 5)  //容易误判类别，屏蔽处理
                {
                   /* dlRes.AL_Rect[i].Color = CogColorConstants.Green;
                    strGroupName = "flaking";
                    ResultLabel.Color = CogColorConstants.Green;
                    ResultLabel.Font = new Font("arial", 10);*/
                }
                //    dlRes.AL_Rect[i].TipText = dlRes.AL_score[i].ToString();
                ResultLabel.X = dlRes.AL_Rect[i].CornerYX;
                ResultLabel.Y = dlRes.AL_Rect[i].CornerXY;
                ResultLabel.Text = strGroupName + ":" + Math.Round(dlRes.AL_score[i], 2).ToString();
                strGroupName += i;
                cogDisp.Display.InteractiveGraphics.Add(dlRes.AL_Rect[i], strGroupName, false);
              //  cogDisp.Display.InteractiveGraphics.Add(ResultLabel, strGroupName, false);
              //  ResultLabel = new CogGraphicLabel();
            }
        }

        private void DLInfer(int iJobIndex)
        {
            DL_result[iJobIndex].AL_Rect.Clear();
            DL_result[iJobIndex].AL_score.Clear();
            DL_result[iJobIndex].AL_Type.Clear();
            DL_result[iJobIndex].Al_bDiff.Clear();

            // bSureCheck[0] = cdl.det_infer_img_En(acqFIFO.hObjFXA[0, 0], ci8gAcq[iJobIndex].Width, ci8gAcq[iJobIndex].Height, cup.p_DL.fThreshold, cup.p_DL.fThreshold, ref DL_result[iJobIndex]);
            if(cup.p_Cam[iJobIndex].iMonoType==0 && acqFIFO.bImgSrcData[iJobIndex]!=null)  //灰度图推理
                bSureCheck[0] = cdl.det_infer_img_ByteGray(acqFIFO.bImgSrcData[iJobIndex], acqFIFO.ci8gAcq[iJobIndex].Width, acqFIFO.ci8gAcq[iJobIndex].Height, cup.p_DL.fThreshold, cup.p_DL.fThreshold, ref DL_result[iJobIndex]);
            else if(acqFIFO.bImgSrcData[iJobIndex] != null) //彩色图推理
                bSureCheck[0] = cdl.det_infer_img_ByteColor(acqFIFO.bImgSrcData[iJobIndex], acqFIFO.ci24ColorAcq[iJobIndex].Width, acqFIFO.ci24ColorAcq[iJobIndex].Height, cup.p_DL.fThreshold, cup.p_DL.fThreshold, ref DL_result[iJobIndex]);

            if (DL_result[iJobIndex].AL_Rect.Count > 0)
            {
                pmaRes.bOk = false;
            }
            else
            {
                pmaRes.bOk = true;
            }
            //连续运行只更新NG图像和信号
            //  if ( iTaskType!=3 || !pmaRes.bOk)
            {
                DirectShowImage(iJobIndex);

            }
                
        }
        //离线文件推理
        private void DLInferOffLine(int iJobIndex)
        {
            DL_result[iJobIndex].AL_Rect.Clear();
            DL_result[iJobIndex].AL_score.Clear();
            DL_result[iJobIndex].AL_Type.Clear();
            DL_result[iJobIndex].Al_bDiff.Clear();

            // bSureCheck[0] = cdl.det_infer_img_En(acqFIFO.hObjFXA[0, 0], ci8gAcq[iJobIndex].Width, ci8gAcq[iJobIndex].Height, cup.p_DL.fThreshold, cup.p_DL.fThreshold, ref DL_result[iJobIndex]);
            if (cup.p_Cam[iJobIndex].iMonoType == 0 && acqFIFO.bImgSrcData[iJobIndex] != null)  //灰度图推理
                bSureCheck[0] = cdl.det_infer_img_ByteColor(acqFIFO.bImgSrcData[iJobIndex], acqFIFO.ci8gAcq[iJobIndex].Width, acqFIFO.ci8gAcq[iJobIndex].Height, cup.p_DL.fThreshold, cup.p_DL.fThreshold, ref DL_result[iJobIndex]);
            else if (acqFIFO.bImgSrcData[iJobIndex] != null) //彩色图推理
                bSureCheck[0] = cdl.det_infer_img_ByteColor(acqFIFO.bImgSrcData[iJobIndex], acqFIFO.ci24ColorAcq[iJobIndex].Width, acqFIFO.ci24ColorAcq[iJobIndex].Height, cup.p_DL.fThreshold, cup.p_DL.fThreshold, ref DL_result[iJobIndex]);

            if (DL_result[iJobIndex].AL_Rect.Count > 0)
            {
                pmaRes.bOk = false;
            }
            else
            {
                pmaRes.bOk = true;
            }
            //连续运行只更新NG图像和信号
            //  if ( iTaskType!=3 || !pmaRes.bOk)
            {
                DirectShowImage(iJobIndex);

            }

        }


        int iDeepInferTime = 0;
      //  public string strErrReson = "";

        //每通道独立执行运行
        public bool RunOneTimeSplite()
        {            
            if (cup.p_VPPRTP.bVisionInfer)
            {
                //2026年4月12日 应判断单个通道的空闲状态
                if (mJM.JobsRunningState != CogJobsRunningStateConstants.None)
                {
                    strErrInfo = "JM状态非空闲";
                    return false;
                }
            }

            if (mCurrentRunState != RunState.RunningContinuous)
                mCurrentRunState = RunState.RunningOnce;

            int iCount = 0;
            int maxTimes = cup.p_RTSP.runTimeOut / 5;
            while (bActivateRun && iCount < maxTimes)
            {
                Thread.Sleep(5);
            }

            inactiveTime = 0;       //只要运行了，就将不活跃次数置零
            bRunOneTimeFinished = 0;

            infer_start_time = new TimeSpan(DateTime.Now.Ticks); //计时开始
            //各通道时间统一
            acqFIFO.strTime = DateTime.Now.ToString("yyyyMMddHHmmssFF");

            //2026年4月12日 设置线程运行  
            for (int i=0;i<maxCameraNum;i++)
            {
                if(cup.p_CD.bListProcess[i])                   
                   RunOneTime(i);
            }
            iDeepInferTime++;

            bRunOneTimeFinished = 2;
            infer_end_time = new TimeSpan(DateTime.Now.Ticks);
            start2end_time = infer_end_time.Subtract(infer_start_time).Duration();
            cost_milliseconds = (int)start2end_time.TotalMilliseconds;


            return true;
        }

        public bool RunOneTime(int iJobIndex)
        {            
            try
            {  
                if (iJobIndex >= 0)
                {
                    //2026年4月12日 修改了采集图像函数
                    if (cup.p_VPPRTP.bAcqFromCam && acqFIFO.GrabOneFrameCogFormat(bRecord, cup.p_CD.bListProcess, iJobIndex) ||
                        !cup.p_VPPRTP.bAcqFromCam && acqFIFO.GrabOneFrameCogFromFileWithSrcData(iFileIndex, cup.p_CD.bListProcess, iJobIndex)>=0)
                    {
                        ci8gAcq = acqFIFO._ci8gAcq;
                        ci8gFX = acqFIFO.ci8gAcqFX;

                        if (cup.p_VPPRTP.bDeepInfer)   //从GPU实时推理
                        {
                            if (cup.p_VPPRTP.bAcqFromCam)
                                DLInfer(iJobIndex);
                            else
                                DLInferOffLine(iJobIndex);
                            pmaRes.bLocated = true;
                        }
                        else if(cup.p_VPPRTP.bVisionInfer)
                        {
                            {
                                SetVPPImage(iJobIndex);
                                Thread.Sleep(10);  //st** 20220804 
                                mJM.Job(iJobIndex).Run();
                            }
                        }
                    }
                    else
                    {
                        strErrInfo = "图像采集异常:"+ acqFIFO.strErrInfo;
                        return false;
                    }
                }
                /*else
                {
                    for(int index=0; index < 3; index++)
                    {                        
                        if (cup.p_VPPRTP.bAcqFromCam && acqFIFO.FXSurfaceAll(bRecord, cup.p_CD.bListProcess, index) ||
                         !cup.p_VPPRTP.bAcqFromCam && acqFIFO.FXSurfaceAllFromFile(iFileIndex, cup.p_CD.bListProcess, index))
                        {
                            {
                                ci8gAcq = acqFIFO._ci8gAcq;
                                ci8gFX = acqFIFO.ci8gAcqFX;

                                SetVPPImage(index);
                                Thread.Sleep(10);  //st** 20220804 
                                mJM.Job(index).Run();
                            }
                        }
                        else
                        {
                            strErrInfo = "图像采集异常";
                            return false;
                        }
                    }
                }*/
            }
            catch (Cognex.VisionPro.Exceptions.CogNotStoppedException)
            {
                // do nothing in this case - we're already running
                strErrInfo = "CogNotStoppedException";
                return false;
            }
            catch (Cognex.VisionPro.Exceptions.CogException ex)
            {
                if (mCurrentRunState != RunState.RunningContinuous)
                    mCurrentRunState = RunState.Stopped;
                strErrInfo = ResourceUtility.GetString("RtUnexpectedErrorQB") + ex.Message;
                return false;
            }          
            return true; 
        }
      
        //VP执行一次运算
        public bool RunOneTime()
        {
            infer_start_time = new TimeSpan(DateTime.Now.Ticks); //计时开始


            if (mJM.JobsRunningState != CogJobsRunningStateConstants.None)
                return false;

            if (mCurrentRunState != RunState.RunningContinuous)
                mCurrentRunState = RunState.RunningOnce;

            while (bActivateRun)
            {
                Thread.Sleep(5);
            }
            try
            {

                //st***20220812
                //   SetNextImageIndex(ImageInd); 
                inactiveTime = 0;       //只要运行了，就将不活跃次数置零

                //采集图像或者读取图像，包括重构光度立体图


                if (cup.p_VPPRTP.bAcqFromCam && acqFIFO.FXSurfaceAll(bRecord, cup.p_CD.bListProcess, cup.p_VPPRTP.iXBType) ||
                    !cup.p_VPPRTP.bAcqFromCam && acqFIFO.FXSurfaceAllFromFile(iFileIndex, cup.p_CD.bListProcess, cup.p_VPPRTP.iXBType))
                {
                    //st 2022年12月22日  深度学习推理
                    //////////////////////////////////////////////////////////////////////////


                    //////////////////////////////////////////////////////////////////////////


                    ci8gAcq = acqFIFO._ci8gAcq;
                    ci8gFX = acqFIFO.ci8gAcqFX;



                    //  if (cup.p_VPPRTP.bVisionInfer)  //2024年7月3日  如果启用视觉推理，则
                    {

                        SetVPPImage();

                        bRunOneTimeFinished = 0;
                        Thread.Sleep(10);  //st** 20220804
                        iJobIndex = 0;
                        mJM.Job(0).Run();


                    }
                    //  else
                    {
                        //  DirectShowImage();
                        //  bRunOneTimeFinished = 2;
                    }

                }
                else
                {
                    return false;
                }


            }
            catch (Cognex.VisionPro.Exceptions.CogNotStoppedException)
            {
                // do nothing in this case - we're already running
                return false;
            }
            catch (Cognex.VisionPro.Exceptions.CogException ex)
            {
                if (mCurrentRunState != RunState.RunningContinuous)
                    mCurrentRunState = RunState.Stopped;
                strErrInfo = ResourceUtility.GetString("RtUnexpectedErrorQB") + ex.Message;
                return false;
            }
            infer_end_time = new TimeSpan(DateTime.Now.Ticks);
            start2end_time = infer_end_time.Subtract(infer_start_time).Duration();
            cost_milliseconds = (int)start2end_time.TotalMilliseconds;

            return true;

        }

        //设置VP的输入图像
        public void SetVPPImage(int iJobIndex)
        { 
            if(cup.p_Cam[iJobIndex].iMonoType==0 && acqFIFO.ci8gAcq[iJobIndex]!=null)
                ((mJM.Job(iJobIndex).VisionTool as CogToolGroup).Tools["CogIPOneImageToolInfo"] as CogIPOneImageTool).InputImage = acqFIFO.ci8gAcq[iJobIndex];
            else
                ((mJM.Job(iJobIndex).VisionTool as CogToolGroup).Tools["CogIPOneImageToolInfo"] as CogIPOneImageTool).InputImage = acqFIFO.ci24ColorAcq[iJobIndex];
        }


        //设置VP的输入图像
        public void SetVPPImage()
        {

            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["CogIPOneImageToolInfo"] as CogIPOneImageTool).InputImage = ci8gAcq[cup.p_VPPRTP.iXBType];

        }
     
       //VP执行连续运算
        public bool RunCont()
        {
            if (bContRun)
            {
                // Stop continuous
                mJM.Stop();
                mCurrentRunState = RunState.Stopped;
                bContRun = false;
                // note that mJM_Stopped will be called when job manager has completed this stop

                return false;
            }
            else
            {
                // update control state here to prevent an error from a quick button doubleclick
                //  mCurrentRunState = RunState.RunningContinuous;
                bContRun = true;
                mCurrentRunState = RunState.RunningContinuous;

                try
                {
                    mJM.RunContinuous();
                }
                catch (Cognex.VisionPro.Exceptions.CogNotStoppedException)
                {
                    // do nothing in this case - we're already running
                }
                catch (Cognex.VisionPro.Exceptions.CogException ex)
                {
                    mCurrentRunState = RunState.Stopped;
                    strErrInfo = ResourceUtility.GetString("RtUnexpectedErrorQB") + ex.Message;
                }
                return true;
            }
        }

        /* public bool RunOneTime()
         {

             if (mJM.JobsRunningState != CogJobsRunningStateConstants.None)
                 return false;

             if (mCurrentRunState != RunState.RunningContinuous)
                 mCurrentRunState = RunState.RunningOnce;

             try
             {

                 //st***20220812
                 //   SetNextImageIndex(ImageInd); 


                 if (cup.p_VPPRTP.bAcqFromCam && acqFIFO.FXSurfaceAll(bRecord, cup.p_CD.bListProcess) ||
                     !cup.p_VPPRTP.bAcqFromCam && !cup.p_VPPRTP.bAcqFromIDB && acqFIFO.FXSurfaceAllFromFile(iFileIndex, cup.p_CD.bListProcess))
                 {


                     //st 2022年12月22日  深度学习推理
                     //////////////////////////////////////////////////////////////////////////
                     TimeSpan infer_start_time = new TimeSpan(DateTime.Now.Ticks);
                     if (cup.p_DL.bUseGPU && bDeepInfer)   //实时推理
                     {
                         for (int i = 0; i < maxCameraNum; i++)
                         {
                             if (cup.p_CD.bListProcess[i] && acqFIFO.hObjFXA[i, 0] != null)
                             {
                                 double dScoreFX = 0;
                                 double dScoreG = 0;
                                 float dTh = 0.8f;

                                 bool bSure = false;
                                 DL_result[i].AL_Rect.Clear();
                                 DL_result[i].AL_score.Clear();
                                 DL_result[i].AL_Type.Clear();
                                 DL_result[i].Al_bDiff.Clear();

                                 //先检测光度图
                                 bSure = cdl.det_infer_img_En(acqFIFO.hObjFXA[i, 5] *//*光度图*//*, 3072, 2048, cup.p_DL.fThreshold, cup.p_DL.fThresholdPoszFX, ref DL_result[i]);

                                 //    HOperatorSet.WriteImage(acqFIFO.hObjFXA[i, 5], "jpg", 0, "TestDL");
                                 if (!bSure)
                                 {

                                     for (int j = 0; j < 5; j++)
                                     {
                                         bSure = cdl.det_infer_img_En(acqFIFO.hObjFXA[i, j], 3072, 2048, dTh, cup.p_DL.fThresholdPoszGrey, ref DL_result[i]);
                                         Thread.Sleep(10);
                                         if (bSure)
                                         {
                                             break;
                                         }

                                     }
                                 }
                                 // 合并多图识别结果
                                 cdl.UnionRemark(ref DL_result[i]);
                                 AnaInferRes(0, i);

                                 for (int k = 0; k < DL_result[i].AL_Rect.Count; k++)
                                 {
                                     if (DL_result[i].Al_bDiff[k] && DL_result[i].AL_score[k] < 1.2)
                                     {
                                         DL_result[i].AL_score.RemoveAt(k);
                                         DL_result[i].AL_Rect.RemoveAt(k);
                                         DL_result[i].AL_Type.RemoveAt(k);
                                         DL_result[i].Al_bDiff.RemoveAt(k);
                                         k--;
                                     }
                                     else if (DL_result[i].AL_Type[k] == 2 && ((DL_result[i].AL_score[k] > 1.2 && DL_result[i].AL_score[k] < 2 && DL_result[i].AL_Rect[k].SideXLength < 100)
                                         || DL_result[i].AL_Rect[k].SideXLength > 1000 || (DL_result[i].AL_score[k] > 1.2 && DL_result[i].AL_score[k] < 2 && DL_result[i].AL_Rect[k].Area < 10000) ||
                                         DL_result[i].AL_Rect[k].SideXLength < 50 || DL_result[i].AL_Rect[k].Area < 5000))
                                     {
                                         DL_result[i].AL_score.RemoveAt(k);
                                         DL_result[i].AL_Rect.RemoveAt(k);
                                         DL_result[i].AL_Type.RemoveAt(k);
                                         DL_result[i].Al_bDiff.RemoveAt(k);
                                         k--;
                                     }
                                 }




                             }

                         }
                         int iPosNum = 0;
                         for (int i = 0; i < maxCameraNum; i++)
                         {
                             for (int k = 0; k < DL_result[i].AL_Rect.Count; k++)
                             {
                                 if (DL_result[i].AL_Type[k] == 2)
                                 {
                                     iPosNum++;
                                     break;
                                 }
                             }
                         }
                         if (iPosNum < 2)
                         {
                             for (int i = 0; i < maxCameraNum; i++)
                             {
                                 for (int k = 0; k < DL_result[i].AL_Rect.Count; k++)
                                 {
                                     if (DL_result[i].AL_Type[k] == 2)
                                     {
                                         DL_result[i].AL_score.RemoveAt(k);
                                         DL_result[i].AL_Rect.RemoveAt(k);
                                         DL_result[i].AL_Type.RemoveAt(k);
                                         DL_result[i].Al_bDiff.RemoveAt(k);
                                         k--;
                                     }
                                 }
                             }
                         }
                         AnaInferRes(1);

                     }       //实时推理
                     else  //从xml文件中读取结果
                     {
                         acqFIFO.ReadDLResult_EN(iFileIndex, cup.p_CD.bListProcess, ref DL_result);
                     }
                     //////////////////////////////////////////////////////////////////////////
                     //对破整体筛选




                    
                     ci8gAcq = acqFIFO._ci8gAcq;
                     ci8gFX = acqFIFO.ci8gAcqFX;
                     //   acqFIFO.SaveImage_ci8gAcq();Fr 
                     if (cup.p_CD.bListProcess[0] && ci8gFX[0] != null)
                     {
                         ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Inputs["InputImage"].Value = ci8gFX[0];
                         ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Inputs["InputImageGrey"].Value = ci8gAcq[0];

                         ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面检测"] as CogToolBlock).Inputs["DLList"].Value = DL_result[0];


                     }
                     for (int i = 1; i < maxCameraNum; i++)
                     {
                         if (cup.p_CD.bListProcess[i] && ci8gAcq[i] != null)
                         {
                             ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面" + i.ToString() + "定位"] as CogToolBlock).Inputs["InputImage"].Value = ci8gAcq[i];
                             ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面" + i.ToString() + "定位"] as CogToolBlock).Inputs["InputImageFX"].Value = ci8gFX[i];

                             ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面" + i.ToString() + "检测"] as CogToolBlock).Inputs["DLList"].Value = DL_result[i];


                         }
                     }

                 }
                 else
                 {
                     return false;
                 }

                 bRunOneTimeFinished = 0;
                 Thread.Sleep(10);  //st** 20220804
                 mJM.Run();
             }
             catch (Cognex.VisionPro.Exceptions.CogNotStoppedException)
             {
                 // do nothing in this case - we're already running
                 return false;
             }
             catch (Cognex.VisionPro.Exceptions.CogException ex)
             {
                 if (mCurrentRunState != RunState.RunningContinuous)
                     mCurrentRunState = RunState.Stopped;
                 strErrInfo = ResourceUtility.GetString("RtUnexpectedErrorQB") + ex.Message;
                 return false;
             }
             return true;

         }*/
        public bool _bContRun
        {
            set { bContRun = value; }
            get { return bContRun; }
        }
        #endregion


        #region  初始化以及VP设置相关
        //变量初始化，空间加载后_Load 事件中调用
        private void initVar()
        {
            for (int i = 0; i < maxCameraNum; i++)
            {
                ci8gAcq[i] = new CogImage8Grey();
            }
            for (int i = 0; i < maxCameraNum; i++)
            {
                ci8gFX[i] = new CogImage8Grey();
            }
           /* for (int i = 0; i < 6; i++)
            {
                hObjFX[i] = new HObject();
            }*/
            acqFIFO._ci8gAcq = ci8gAcq;
           // acqFIFO._hObj = hObjFX;
            for (int i = 0; i < maxCameraNum; i++)
            {
                DL_result[i].AL_Rect = new List<CogRectangleAffine>();
                DL_result[i].AL_score = new List<float>();
                DL_result[i].AL_Type = new List<int>();
                DL_result[i].Al_bDiff = new List<bool>();
                ;
            }

            vpRes.iResCode = new int[maxCameraNum];
            //vpRes.TF = new CogTransform2DLinear[maxCameraNum];
        }
        //VP选项配置
        public int VPConfig()
        {
            FormConfig frmConfigSelect = new FormConfig(mQuickBuildAccess, mUsePasswords);
            frmConfigSelect.ShowDialog(this);
            int iRet = 0;
            switch (frmConfigSelect.Result)
            {
                case FormConfigResult.None:
                    break;

                case FormConfigResult.QuickBuild:
                    // detach from job manager before displaying edit control
                    AttachToJobManager(false);

                    // put up a new dialog containing QB editor
                    FormQB frm = new FormQB(mJM);
                    frm.ShowDialog(this);
                    frm.Dispose();

                    // prompt for save of vpp file
                    string vpp = mLoadedVppFilename;
                    string quotedvpp = "\"" + vpp + "\"";
                    string saveButtonName = ResourceUtility.GetString("RtSaveSettingsButton");
                    string quotedSaveButtonName = "\"" + saveButtonName + "\"";
                    string promptStr = ResourceUtility.FormatString("RtSaveSettingsTextAfterConfig", quotedvpp, quotedSaveButtonName);
                    iRet = 10 + PromptToSaveSettings(promptStr);

                    // re-attach
                    AttachToJobManager(true);
                    break;

                case FormConfigResult.SetPasswords:
                    FormSetPasswords frmPasswords = new FormSetPasswords(mCurrentPasswordFile);
                    frmPasswords.ShowDialog(this);
                    break;

                case FormConfigResult.AppOptions:
                    FormAppOptions frmOptions = new FormAppOptions(this);
                    frmOptions.EnableDisplay = mCurrentOptionsFile.EnableImageDisplay;
                    frmOptions.EnableIOAtStartup = mCurrentOptionsFile.EnableIOAtStartup;
                    frmOptions.EnableIO = mJM.IOEnable;
                    DialogResult result = frmOptions.ShowDialog(this);
                    if (result == DialogResult.OK)
                    {
                        mCurrentOptionsFile.EnableImageDisplay = frmOptions.EnableDisplay;
                        mCurrentOptionsFile.EnableIOAtStartup = frmOptions.EnableIOAtStartup;
                        mCurrentOptionsFile.Write();

                        // handle IO state change
                        if (frmOptions.EnableIO != mJM.IOEnable)
                        {
                            try
                            {
                                mJM.IOEnable = frmOptions.EnableIO;
                            }
                            catch (Cognex.VisionPro.Exceptions.CogException ex)
                            {
                                MessageBox.Show(ResourceUtility.GetString("RtUnexpectedErrorQB") + ex.Message,
                                                mApplicationName);
                            }
                        }
                    }
                    break;
            }
            return iRet;
        }
      
        public bool SetDiagParam(CUserParam cup)
        {
            try
            {
                // 设置掩膜
                string imagePath = @".\Mask\Sub1MaskWithDL.bmp";
                CogImageFileTool cogFile = new CogImageFileTool();
                CogImage8Grey cogImageMask = new CogImage8Grey();
                cogFile.Operator.Open(imagePath, CogImageFileModeConstants.Read);
                cogFile.Run();//图片在Run()方法运行后才会开始导入。 
                cogImageMask = (CogImage8Grey)cogFile.OutputImage;
                (((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1定位"] as CogToolBlock).Tools["区域提取"] as CogToolBlock).Inputs["InputMaskWithDL"].Value =
                        cogImageMask.Copy();

                imagePath = @".\Mask\Sub1MaskWithOutDL.bmp";
                cogFile.Operator.Open(imagePath, CogImageFileModeConstants.Read);
                cogFile.Run();//图片在Run()方法运行后才会开始导入。 
                cogImageMask = (CogImage8Grey)cogFile.OutputImage;
                (((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1定位"] as CogToolBlock).Tools["区域提取"] as CogToolBlock).Inputs["InputMaskWithOutDL"].Value =
                        cogImageMask.Copy();


                imagePath = @".\Mask\Sub2MaskWithDL.bmp";
                cogFile.Operator.Open(imagePath, CogImageFileModeConstants.Read);
                cogFile.Run();//图片在Run()方法运行后才会开始导入。 
                cogImageMask = (CogImage8Grey)cogFile.OutputImage;
                (((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2定位"] as CogToolBlock).Tools["区域提取"] as CogToolBlock).Inputs["InputMaskWithDL"].Value =
                        cogImageMask.Copy();

                imagePath = @".\Mask\Sub2MaskWithOutDL.bmp";
                cogFile.Operator.Open(imagePath, CogImageFileModeConstants.Read);
                cogFile.Run();//图片在Run()方法运行后才会开始导入。 
                cogImageMask = (CogImage8Grey)cogFile.OutputImage;
                (((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2定位"] as CogToolBlock).Tools["区域提取"] as CogToolBlock).Inputs["InputMaskWithOutDL"].Value =
                        cogImageMask.Copy();

                imagePath = @".\Mask\Sub3MaskWithDL.bmp";

                cogFile.Operator.Open(imagePath, CogImageFileModeConstants.Read);
                cogFile.Run();//图片在Run()方法运行后才会开始导入。 
                cogImageMask = (CogImage8Grey)cogFile.OutputImage;
                (((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面3定位"] as CogToolBlock).Tools["区域提取"] as CogToolBlock).Inputs["InputMaskWithDL"].Value =
                        cogImageMask.Copy();

                imagePath = @".\Mask\Sub3MaskWithOutDL.bmp";
                cogFile.Operator.Open(imagePath, CogImageFileModeConstants.Read);
                cogFile.Run();//图片在Run()方法运行后才会开始导入。 
                cogImageMask = (CogImage8Grey)cogFile.OutputImage;
                (((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面3定位"] as CogToolBlock).Tools["区域提取"] as CogToolBlock).Inputs["InputMaskWithOutDL"].Value =
                        cogImageMask.Copy();

                imagePath = @".\Mask\Sub4MaskWithDL.bmp";

                cogFile.Operator.Open(imagePath, CogImageFileModeConstants.Read);
                cogFile.Run();//图片在Run()方法运行后才会开始导入。 
                cogImageMask = (CogImage8Grey)cogFile.OutputImage;
                (((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面4定位"] as CogToolBlock).Tools["区域提取"] as CogToolBlock).Inputs["InputMaskWithDL"].Value =
                        cogImageMask.Copy();

                imagePath = @".\Mask\Sub4MaskWithOutDL.bmp";
                cogFile.Operator.Open(imagePath, CogImageFileModeConstants.Read);
                cogFile.Run();//图片在Run()方法运行后才会开始导入。 
                cogImageMask = (CogImage8Grey)cogFile.OutputImage;
                (((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面4定位"] as CogToolBlock).Tools["区域提取"] as CogToolBlock).Inputs["InputMaskWithOutDL"].Value =
                        cogImageMask.Copy();



                imagePath = @".\Mask\底面MaskLocalWithoutEdge.bmp";
                cogFile.Operator.Open(imagePath, CogImageFileModeConstants.Read);
                cogFile.Run();//图片在Run()方法运行后才会开始导入。 
                cogImageMask = (CogImage8Grey)cogFile.OutputImage;
                (((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Tools["区域提取"] as CogToolBlock).Inputs["InputMaskWithOutEdge"].Value =
                       cogImageMask.Copy();


                imagePath = @".\Mask\底面MaskEdge.bmp";
                cogFile.Operator.Open(imagePath, CogImageFileModeConstants.Read);
                cogFile.Run();//图片在Run()方法运行后才会开始导入。 
                cogImageMask = (CogImage8Grey)cogFile.OutputImage;
                (((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Tools["区域提取"] as CogToolBlock).Inputs["InputMaskWithEdge"].Value =
                       cogImageMask.Copy();

                ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面检测"] as CogToolBlock).Inputs["greyAbnormCount"].Value = cup.p_DP.dmParam.greyAbnormCount;
                ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面检测"] as CogToolBlock).Inputs["greyAbnormArea"].Value = cup.p_DP.dmParam.greyAbnormArea;
                ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面检测"] as CogToolBlock).Inputs["FXAbnormArea1"].Value = cup.p_DP.dmParam.FXAbnormArea1;
                ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面检测"] as CogToolBlock).Inputs["FXAbnormArea1"].Value = cup.p_DP.dmParam.FXAbnormArea1;
                ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面检测"] as CogToolBlock).Inputs["FXAbnormArea2"].Value = cup.p_DP.dmParam.FXAbnormArea2;


                for (int i = 0; i < maxCameraNum - 1; i++)
                {


                    ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面" + (i + 1).ToString() + "检测"] as CogToolBlock).Inputs["edgeAbnormCount"].Value = cup.p_DP.cmParam[i].edgeAbnormCount;
                    ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面" + (i + 1).ToString() + "检测"] as CogToolBlock).Inputs["edgeLeftAbnormCount"].Value = cup.p_DP.cmParam[i].edgeLeftAbnormCount;
                    ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面" + (i + 1).ToString() + "检测"] as CogToolBlock).Inputs["edgeRightAbnormCount"].Value = cup.p_DP.cmParam[i].edgeRightAbnormCount;
                    ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面" + (i + 1).ToString() + "检测"] as CogToolBlock).Inputs["lwArea"].Value = cup.p_DP.cmParam[i].lwArea;
                    (((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面" + (i + 1).ToString() + "检测"] as CogToolBlock).Tools["CogToolBlockCrack"] as CogToolBlock).Inputs["CrackParam"].Value = cup.p_CP;

                }

                SetRemark(cup.p_VPPRTP.bRemarkArea, cup.p_VPPRTP.bRemarkResult);

                /*
                ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2检测"] as CogToolBlock).Inputs["edgeAbnormCount"].Value = cup.p_DP.cmParam[1].edgeAbnormCount;
                ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2检测"] as CogToolBlock).Inputs["edgeLeftAbnormCount"].Value = cup.p_DP.cmParam[1].edgeLeftAbnormCount;
                ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2检测"] as CogToolBlock).Inputs["edgeRightAbnormCount"].Value = cup.p_DP.cmParam[1].edgeRightAbnormCount;
                ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2检测"] as CogToolBlock).Inputs["lwArea"].Value = cup.p_DP.cmParam[1].lwArea;
                */
            }
            catch
            {
                return false;
                // MessageBox.Show("设置流程参数失败");
            }
            return true;
        }
        //侧面是否使用光度立体法
        public void SetUseFX(bool bUseFX)
        {

            (mJM.Job(0).VisionTool as CogToolGroup).SetScriptTerminalData("bInputFX", bUseFX);

        }
        //是否使用相机数据
        public void SetFromCam(bool bAcqFromCam)
        {
            if(cup.p_VPPRTP.bVisionInfer)
            (mJM.Job(0).VisionTool as CogToolGroup).SetScriptTerminalData("bAcqFromCam", bAcqFromCam);

        }
        public int iModelRole = -1;
        public void SetUseWeakCrackDetect(bool bWeakCrackDetect)
        {
            for (int i = 0; i < maxCameraNum - 1; i++)
            {
                ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面" + (i + 1).ToString() + "检测"] as CogToolBlock).Inputs["bWeakDetect"].Value = cup.p_VPPRTP.bUseWeakCrack;
            }
        }
        //设置VPP算法文件参数
        public bool SetVPPRuntimeParam()
        {
            try
            {
                SetLock(false);
                SetUseFX(cup.p_VPPRTP.bUseFX);
                SetFromCam(cup.p_VPPRTP.bAcqFromCam || !cup.p_VPPRTP.bAcqFromIDB);
                SetRemark(cup.p_VPPRTP.bRemarkArea, cup.p_VPPRTP.bRemarkResult);
                SetListProcess(cup);
                SetDiagParam(cup);
                SetUseWeakCrackDetect(cup.p_VPPRTP.bUseWeakCrack);
                SetxbType(cup.p_VPPRTP.iXBType);
            }
            catch
            {
                return false;
            }
            return true;
        }
        //设置VPP中的工具是否启用
        public bool SetListProcess(CUserParam cup)
        {
            try
            {
                (mJM.Job(0).VisionTool as CogToolGroup).SetScriptTerminalData("ListProcess", cup.p_CD.bListProcess);

                CogToolGroup myToolGroup = mJM.Job(0).VisionTool as CogToolGroup;

                for (int i = 0; i < maxCameraNum; i++)
                {
                    if (cup.p_CD.bListProcess[i])
                    {
                        myToolGroup.DisabledTools.Remove(myToolGroup.Tools[i * 2 + 1]);
                        myToolGroup.DisabledTools.Remove(myToolGroup.Tools[i * 2 + 2]);
                    }
                    else
                    {
                        myToolGroup.DisabledTools.Add(myToolGroup.Tools[i * 2 + 1]);
                        myToolGroup.DisabledTools.Add(myToolGroup.Tools[i * 2 + 2]);
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        //主窗体InitVisionControl()中调用
        public void SetAcqFIFO(ImgAcqFIFO _acqFIFO)
        {
            acqFIFO = _acqFIFO;
        }
        //SetVPPRuntimeParam()函数中调用
        public void SetLock(bool bLock)
        {
            /* ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Inputs["bRerun"].Value = bLock;
             
           ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1定位"] as CogToolBlock).Inputs["bRerun"].Value = bLock;

            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2定位"] as CogToolBlock).Inputs["bRerun"].Value = bLock;
            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面3定位"] as CogToolBlock).Inputs["bRerun"].Value = bLock;

            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面4定位"] as CogToolBlock).Inputs["bRerun"].Value = bLock;*/
        }
        //设置VP中是否要保存图像结果
        public void SetSaveRes(bool bSaveRes)
        {
            /* ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面检测"] as CogToolBlock).Inputs["bSaveRes"].Value = bSaveRes;

             ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1检测"] as CogToolBlock).Inputs["bSaveRes"].Value = bSaveRes;

             ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2检测"] as CogToolBlock).Inputs["bSaveRes"].Value = bSaveRes;
             ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面3检测"] as CogToolBlock).Inputs["bSaveRes"].Value = bSaveRes;

             ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面4检测"] as CogToolBlock).Inputs["bSaveRes"].Value = bSaveRes;*/
        }
        //2024年8月27日  根据设定的匣钵类型对多模板匹配搜索集进行设置
        CogPMAlignMultiTool pmaMTGrey;
        public void SetxbType(int xbType)
        {

            try
            {

                if (cup.p_CD.bListProcess[0])
                {
                    //  pmaMTFX = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Tools["CogPMAlignMultiToolFX"] as CogPMAlignMultiTool;
                    pmaMTGrey = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["CogPMAlignMultiTool1"] as CogPMAlignMultiTool);
                    //   pmaMTFX.Operator.RemoveAllItemsFromSearchOrder();
                    pmaMTGrey.Operator.RemoveAllItemsFromSearchOrder();
                    if (xbType == 0)
                    {
                        //     pmaMTFX.Operator.AddItemToSearchOrderByInternalID(0);
                        pmaMTGrey.Operator.AddItemToSearchOrderByInternalID(0);
                        // pmaMTGrey.Operator.remove
                    }
                    else if (xbType == 1)
                    {
                        //   pmaMTFX.Operator.AddItemToSearchOrderByInternalID(1);
                        pmaMTGrey.Operator.AddItemToSearchOrderByInternalID(1);
                    }
                    else
                    {
                        //  pmaMTFX.Operator.AddAllItemsToSearchOrder();
                        pmaMTGrey.Operator.AddAllItemsToSearchOrder();
                    }

                }

            }
            catch
            { }
        }
        //设置推理模式
        public void SetVPInferModel(int iType)
        {
         /*   ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面检测"] as CogToolBlock).Inputs["inferType"].Value = iType;
            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1检测"] as CogToolBlock).Inputs["inferType"].Value = iType;
            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2检测"] as CogToolBlock).Inputs["inferType"].Value = iType;
            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面3检测"] as CogToolBlock).Inputs["inferType"].Value = iType;
            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面4检测"] as CogToolBlock).Inputs["inferType"].Value = iType;*/
        }

        public void SetVPResult()
        {
            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面检测"] as CogToolBlock).Inputs["iResCode"].Value = vpRes.iResCode[0];
            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面检测"] as CogToolBlock).Inputs["gpgs"].Value = vpRes.gpgsMain;
            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面检测"] as CogToolBlock).Inputs["bDefect"].Value = vpRes.bDefect;


            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1检测"] as CogToolBlock).Inputs["iResCode"].Value = vpRes.iResCode[1];
            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1检测"] as CogToolBlock).Inputs["gpgs"].Value = vpRes.gpgsSub1;
            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1检测"] as CogToolBlock).Inputs["bDefect"].Value = vpRes.bDefect;


            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2检测"] as CogToolBlock).Inputs["iResCode"].Value = vpRes.iResCode[2];
            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2检测"] as CogToolBlock).Inputs["gpgs"].Value = vpRes.gpgsSub2;
            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2检测"] as CogToolBlock).Inputs["bDefect"].Value = vpRes.bDefect;


            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面3检测"] as CogToolBlock).Inputs["iResCode"].Value = vpRes.iResCode[3];
            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面3检测"] as CogToolBlock).Inputs["gpgs"].Value = vpRes.gpgsSub3;
            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面3检测"] as CogToolBlock).Inputs["bDefect"].Value = vpRes.bDefect;


            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面4检测"] as CogToolBlock).Inputs["iResCode"].Value = vpRes.iResCode[4];
            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面4检测"] as CogToolBlock).Inputs["gpgs"].Value = vpRes.gpgsSub4;
            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面4检测"] as CogToolBlock).Inputs["bDefect"].Value = vpRes.bDefect;
        }

        #endregion

        #region  VP工具相关设置

        public CogPMAlignMultiTool _pmaMTGrey
        {
            //  set { ((mJM.Job(0).VisionTool as CogToolGroup).Tools["CogPMAlignMultiTool1"] as CogPMAlignMultiTool) = value; }
            get { return ((mJM.Job(0).VisionTool as CogToolGroup).Tools["CogPMAlignMultiTool1"] as CogPMAlignMultiTool); }

        }
        public CogPMAlignTool _pmaMT
        {
            //  set { ((mJM.Job(0).VisionTool as CogToolGroup).Tools["CogPMAlignMultiTool1"] as CogPMAlignMultiTool) = value; }
            get { return ((mJM.Job(0).VisionTool as CogToolGroup).Tools["CogPMAlignTool1"] as CogPMAlignTool); }

        }
        public CogBlobTool _blobCalib
        {
            //  set { ((mJM.Job(0).VisionTool as CogToolGroup).Tools["CogPMAlignMultiTool1"] as CogPMAlignMultiTool) = value; }
            get { return ((mJM.Job(1).VisionTool as CogToolGroup).Tools["CogBlobTool1"] as CogBlobTool); }

        }
        public bool setBlob(CogBlobTool blobT)
        {
            try
            {
                (mJM.Job(1).VisionTool as CogToolGroup).Tools["CogBlobTool1"] = blobT;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool setPMA(CogPMAlignMultiTool pma)
        {
            try
            {
                (mJM.Job(0).VisionTool as CogToolGroup).Tools["CogPMAlignMultiTool1"] = pma;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool setPMA(CogPMAlignTool pma)
        {
            try
            {
                (mJM.Job(0).VisionTool as CogToolGroup).Tools["CogPMAlignTool1"] = pma;
                return true;
            }
            catch
            {
                return false;
            }
        }


        public void SetPMAParam(PMAlignParam pmaParam)
        {
            try
            {
                //设置训练参数
                CogPMAlignTool pma = (mJM.Job(0).VisionTool as CogToolGroup).Tools["CogPMAlignTool1"] as CogPMAlignTool;
                pma.Pattern.GrainLimitAutoSelect = pmaParam.TrainAutoGrun;
                pma.Pattern.GrainLimitCoarse = pmaParam.TrainCoarse;
                pma.Pattern.GrainLimitFine = pmaParam.TrainFine;
                pma.Pattern.EdgeThreshold = pmaParam.TrainEdgeThre;
                pma.Pattern.AutoEdgeThresholdEnabled = pmaParam.TrainAutoEdgeThre;
                pma.Pattern.IgnorePolarity = pmaParam.TrainPolarity;
                switch (pmaParam.TrainAlg)
                {
                    case 0:
                        pma.Pattern.TrainAlgorithm = CogPMAlignTrainAlgorithmConstants.PatMax;
                        break;
                    case 1:
                        pma.Pattern.TrainAlgorithm = CogPMAlignTrainAlgorithmConstants.PatQuick;
                        break;
                    case 2:
                        pma.Pattern.TrainAlgorithm = CogPMAlignTrainAlgorithmConstants.PatMaxAndPatQuick;
                        break;
                }
                //设置运行参数

                switch (pmaParam.RunAlg)
                {
                    case 0:
                        pma.RunParams.RunAlgorithm = CogPMAlignRunAlgorithmConstants.PatMax;
                        break;
                    case 1:
                        pma.RunParams.RunAlgorithm = CogPMAlignRunAlgorithmConstants.PatQuick;
                        break;
                    case 2:
                        pma.RunParams.RunAlgorithm = CogPMAlignRunAlgorithmConstants.BestTrained;
                        break;
                }
                pma.RunParams.AutoEdgeThresholdEnabled = pmaParam.RunAutoEdgeThre;
                pma.RunParams.ScoreUsingClutter = pmaParam.RunArt;
                pma.RunParams.EdgeThreshold = pmaParam.RunEdgeThre;
                pma.RunParams.ContrastThreshold = pmaParam.RunConstractThre;
                pma.RunParams.ApproximateNumberToFind = pmaParam.RunSearchNum;
                pma.RunParams.AcceptThreshold = pmaParam.RunScoreThre;
                if (pmaParam.RunEnableAngle)
                {
                    pma.RunParams.ZoneAngle.Configuration = CogPMAlignZoneConstants.LowHigh;
                }
                else
                {
                    pma.RunParams.ZoneAngle.Configuration = CogPMAlignZoneConstants.Nominal;
                }
                pma.RunParams.ZoneAngle.Low = pmaParam.RunAngleD * Math.PI / 180;
                pma.RunParams.ZoneAngle.High = pmaParam.RunAngleU * Math.PI / 180;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }
        public void GetAlignRegion(ref AlignImParam alImParam)
        {
            try
            {
                CogAffineTransformTool cogTrans = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["CogToolBlock2"] as CogToolBlock).Tools["CogAffineTransformTool1"] as CogAffineTransformTool;
                alImParam.dCenX = cogTrans.Region.CenterX;
                alImParam.dCenY = cogTrans.Region.CenterY;
                alImParam.dWidth = cogTrans.Region.SideXLength;
                alImParam.dHeight = cogTrans.Region.SideYLength;

                cogTrans.Run();
                CogImage8Grey cogIm = cogTrans.OutputImage as CogImage8Grey;
                alImParam.width = cogIm.Width;
                alImParam.height = cogIm.Height;
                string strImageFile = @".\data\" + "sourceBGImg.jpg";
                mImageFile.Open(strImageFile, CogImageFileModeConstants.Write);
                mImageFile.Append(cogIm);
                mImageFile.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }

        }
        public void GetPMAParam(ref PMAlignParam pmaParam)
        {
            //获取训练参数
            try
            {
                CogPMAlignTool pma = (mJM.Job(0).VisionTool as CogToolGroup).Tools["CogPMAlignTool1"] as CogPMAlignTool;
                pmaParam.TrainAutoGrun = pma.Pattern.GrainLimitAutoSelect;
                pmaParam.TrainCoarse = pma.Pattern.GrainLimitCoarse;
                pmaParam.TrainFine = pma.Pattern.GrainLimitFine;
                pmaParam.TrainEdgeThre = pma.Pattern.EdgeThreshold;
                pmaParam.TrainAutoEdgeThre = pma.Pattern.AutoEdgeThresholdEnabled;
                pmaParam.TrainPolarity = pma.Pattern.IgnorePolarity;
                pmaParam.TrainAlg = (int)pma.Pattern.TrainAlgorithm;
                if (pmaParam.TrainAlg > 2 || pmaParam.TrainAlg < 0)
                    pmaParam.TrainAlg = 2;

                //获取运行参数
                pmaParam.RunAlg = (int)pma.RunParams.RunAlgorithm;
                if (pmaParam.RunAlg > 2 || pmaParam.RunAlg < 0)
                    pmaParam.RunAlg = 2;

                pmaParam.RunAutoEdgeThre = pma.RunParams.AutoEdgeThresholdEnabled;
                pmaParam.RunArt = pma.RunParams.ScoreUsingClutter;
                pmaParam.RunEdgeThre = pma.RunParams.EdgeThreshold;
                pmaParam.RunConstractThre = pma.RunParams.ContrastThreshold;
                pmaParam.RunSearchNum = pma.RunParams.ApproximateNumberToFind;
                pmaParam.RunScoreThre = pma.RunParams.AcceptThreshold;

                if (pma.RunParams.ZoneAngle.Configuration == CogPMAlignZoneConstants.LowHigh)
                    pmaParam.RunEnableAngle = true;
                else
                    pmaParam.RunEnableAngle = false;
                pmaParam.RunAngleD = pma.RunParams.ZoneAngle.Low * 180 / Math.PI;
                pmaParam.RunAngleU = pma.RunParams.ZoneAngle.High * 180 / Math.PI;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        #endregion

        #region  相机触发采集，以及由触发采集执行运算
        public bool InitAcqCB()  //注册触发采集回调
        {
            int iCamInd = cup.p_CD.camInd[cup.p_CD.triggleCam];
            cbImage = new MyCamera.cbOutputExdelegate(ImageCallBackCont);
            acqFIFO._HikCam[iCamInd].MV_CC_RegisterImageCallBackEx_NET(cbImage, (IntPtr)iCamInd);
            bool bSucc = SetTrigger(cup.p_CD.bTriggle, cup.p_CD.triggleMode, iCamInd);
            return bSucc;
        }

        //设置触发模式
        public bool SetTrigger(bool bTrigger, int iMode, int iCamInd)
        {
            if (acqFIFO._HikCam == null || acqFIFO._HikCam[iCamInd] == null)
                return false;
            if (bTrigger)
            {
                //  acqFIFO._HikCam[iCamInd].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);


                if (iMode == 4)
                {
                    acqFIFO._HikCam[iCamInd].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                    //  (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF
                }
                else
                {
                    acqFIFO._HikCam[iCamInd].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                }
            }
            // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
            //           1 - Line1;
            //           2 - Line2;
            //           3 - Line3;
            //           4 - Counter;
            //           7 - Software;           

            return true;
        }

        [DllImport("kernel32.dll")]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        public void ImageCallBackCont(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO_EX pFrameInfo, IntPtr pUser)
        {
            int iCamInd = (int)pUser;
            if (iTaskType == -1)        //没有采集任务
                return;
            if (iTaskType == 0 || iTaskType == 2)
            {
                if (bTaskFinish[iCamInd])
                    return;
            }
            bTaskFinish[iCamInd] = true;

            // CogImage8Grey cogImage8Grey = new CogImage8Grey();
            try
            {

                CopyMemory(acqFIFO.m_BufForDriverS[iCamInd], pData, pFrameInfo.nFrameLen);

                CogImage8Root cogImage8Root = new CogImage8Root();
                cogImage8Root.Initialize((Int32)pFrameInfo.nWidth, (Int32)pFrameInfo.nHeight, acqFIFO.m_BufForDriverS[iCamInd], (Int32)pFrameInfo.nWidth, null);

                ci8gAcq[iCamInd].SetRoot(cogImage8Root);


                /*  for (int i = 0; i < cameraNum; i++)
                  {
                      if (i != iCamInd)
                      {
                          if(i==cup.p_CD.camInd[0])   //上顶面
                          {

                          }
                      }
                  }
               //   cogImage8Grey.SetRoot(cogImage8Root);
               cup.p_CD.camInd
               switch(iCamInd)
                  {
                      case 0:
                          break;
                  }
                  //st 可以不作显示
                if(cup.p_CD.triggleCam== 0)
                  {
                      cogRDMain.Display.Image = ci8gAcq[iCamInd];
                  }
                 else if (cup.p_CD.triggleCam ==1)
                  { 
                      cogRDSub1.Image= ci8gAcq[iCamInd];
                  }
                  else if (cup.p_CD.triggleCam ==2)
                  {
                      cogRDSub2.Image = ci8gAcq[iCamInd];
                  }*/

                CallbackProcOneTime();

            }
            catch
            {
                return;
            }
            // ch:抓取的帧数 | en:Aquired Frame Number
            // ++m_nFrames[nIndex];

        }

        public bool CallbackProcOneTime()
        {

            if (mJM.JobsRunningState != CogJobsRunningStateConstants.None)
                return false;

            // update control state here to prevent an error from a quick button doubleclick
            if (mCurrentRunState != RunState.RunningContinuous)
                mCurrentRunState = RunState.RunningOnce;

            try
            {
                //设置输入图像
                if (acqFIFO._HikCam[cup.p_CD.camInd[0]] != null)  //st 还要添加状态判断,顶面采集
                {
                    ci8gAcq[cup.p_CD.camInd[0]] = acqFIFO.FXSurface(cup.p_CD.camInd[0], bRecord);

                    ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Inputs["InputImage"].Value = ci8gAcq[cup.p_CD.camInd[0]];
                }
                else
                {

                }

                if (cup.p_CD.triggleCam == 1)
                {
                    ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1定位"] as CogToolBlock).Inputs["InputImage"].Value = ci8gAcq[cup.p_CD.camInd[1]];

                    if (acqFIFO._HikCam[cup.p_CD.camInd[2]] != null)
                    {
                        ci8gAcq[cup.p_CD.camInd[2]] = acqFIFO.GrabOneFrameCog(cup.p_CD.camInd[2]);
                        ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2定位"] as CogToolBlock).Inputs["InputImage"].Value = ci8gAcq[cup.p_CD.camInd[2]];

                    }
                    else
                    {

                    }
                }
                else if (cup.p_CD.triggleCam == 2)
                {
                    ((mJM.Job(0).VisionTool as CogToolGroup).Tools["测面2定位"] as CogToolBlock).Inputs["InputImage"].Value = ci8gAcq[cup.p_CD.camInd[1]];

                    if (acqFIFO._HikCam[cup.p_CD.camInd[1]] != null)
                    {
                        ci8gAcq[cup.p_CD.camInd[2]] = acqFIFO.GrabOneFrameCog(cup.p_CD.camInd[2]);
                        ((mJM.Job(0).VisionTool as CogToolGroup).Tools["测面2定位"] as CogToolBlock).Inputs["InputImage"].Value = ci8gAcq[cup.p_CD.camInd[2]];

                    }
                    else
                    {

                    }
                }

                bRunOneTimeFinished = 0;
                iJobIndex = 0;
                mJM.Job(0).Run();

            }
            catch (Cognex.VisionPro.Exceptions.CogNotStoppedException)
            {
                // do nothing in this case - we're already running
            }
            catch (Cognex.VisionPro.Exceptions.CogException ex)
            {
                if (mCurrentRunState != RunState.RunningContinuous)
                    mCurrentRunState = RunState.Stopped;
                strErrInfo = ResourceUtility.GetString("RtUnexpectedErrorQB") + ex.Message;
                return false;
            }
            return true;

        }

        public void ImageCallBack(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO_EX pFrameInfo, IntPtr pUser)
        {
            int iCamInd = (int)pUser;
            if (iTaskType == 0 || iTaskType == 2)
            {
                if (bTaskFinish[iCamInd])
                    return;
            }
            bTaskFinish[iCamInd] = true;



            CogImage8Grey cogImage8Grey = new CogImage8Grey();
            try
            {

                CopyMemory(acqFIFO.m_BufForDriverS[iCamInd], pData, pFrameInfo.nFrameLen);

                CogImage8Root cogImage8Root = new CogImage8Root();
                cogImage8Root.Initialize((Int32)pFrameInfo.nWidth, (Int32)pFrameInfo.nHeight, acqFIFO.m_BufForDriverS[iCamInd], (Int32)pFrameInfo.nWidth, null);
                cogImage8Grey.SetRoot(cogImage8Root);
                if (iModelRole == 10)
                    cogRDMain.Display.Image = cogImage8Grey.Copy();
                else
                    cogRDMainUser.Image = cogImage8Grey.Copy();

            }
            catch
            {
                return;
            }
            // ch:抓取的帧数 | en:Aquired Frame Number
            // ++m_nFrames[nIndex];

        }
        #endregion


        #region  运算后处理
        //更新Record清单
        public bool UpdateSubRecordsList()
        {

            ICogRecord result = mHistoryCollection[mSelectedJob].CurrentItem;
            if (result == null)
                return false;
            string[] strSubRecordsName = null;
            //st** 获取记录清单  cup.p_SRN.strName   cup.p_SRN.count
            int count = Utility.PickSubRecordsName(result, _updateDisplayStrings, ref strSubRecordsName);
            bool[] bList = new bool[count];

            string strTmpName;
            //更新整体记录清单  侧面1检测多模板.模式1分析.侧面裂纹.CogToolBlockWeak.CogBlobTool1.InputImage
            //侧面1检测多模板.模式2分析.侧面裂纹.CogToolBlockFX.CogHistogramTool2.InputImage
            //侧面1检测多模板.模式2分析.侧面裂纹.CogToolBlockWeak.CogBlobTool1.InputImage
            //侧面2检测多模板.模式2分析.侧面裂纹.CogToolBlockWeak.CogBlobTool1.InputImage
            //侧面2检测多模板.模式1分析.侧面裂纹.CogToolBlockWeak.CogBlobTool1.InputImage
            //侧面2检测多模板.模式2分析.侧面裂纹.CogToolBlockFX.CogHistogramTool2.InputImage
            //侧面1检测多模板.模式1分析.侧面裂纹.CogToolBlockFX.CogHistogramTool2.InputImage
            //侧面1检测多模板.模式1分析.边缘破损.CogIPOneImageTool1.InputImage
            //侧面2检测多模板.模式1分析.侧面裂纹.CogToolBlockWeak.CogBlobTool1.InputImage
            //侧面1检测多模板.模式2分析.侧面裂纹.CogToolBlockWeak.CogBlobTool1.InputImage"
            //侧面2检测多模板.模式2分析.侧面裂纹.分析区域提取.CogAffineTransformToolGreyROI.InputImage
            //侧面2检测多模板.模式2分析.侧面裂纹.分析区域提取.CogAffineTransformToolFXROI.InputImage
            for (int i = 0; i < count; i++)
            {
                strTmpName = strSubRecordsName[i];
                bool bFound = false;
                for (int j = 0; j < cup.p_SRN.strName.Length; j++)
                {
                    if (strTmpName == cup.p_SRN.strName[j])
                    {
                        bList[i] = cup.p_SRN.bList[j];
                        bFound = true;
                        break;
                    }
                }
                if (!bFound)
                {

                }
            }
            cup.p_SRN.strName = strSubRecordsName;
            cup.p_SRN.bList = bList;

            //更新默认清单 
            string[] strTmpShowSubs = new string[maxCameraNum];
            cameraNum = maxCameraNum;
            for (int i = 0; i < cameraNum; i++)
            {
                strTmpName = cup.p_SRN.strDefaultGUIName[i];
                for (int j = 0; j < count; j++)
                {
                    if (strTmpName == cup.p_SRN.strName[j])
                    {
                        strTmpShowSubs[i] = strTmpName;
                        break;
                    }
                }

            }
            strTmpShowSubs.CopyTo(cup.p_SRN.strDefaultGUIName, 0);

            //更新灰度图清单 
            strTmpShowSubs = new string[maxCameraNum];
            for (int i = 0; i < cameraNum; i++)
            {
                strTmpName = cup.p_SRN.strGreyGUIName[i];
                for (int j = 0; j < count; j++)
                {
                    if (strTmpName == cup.p_SRN.strName[j])
                    {
                        strTmpShowSubs[i] = strTmpName;
                        break;
                    }
                }

            }
            strTmpShowSubs.CopyTo(cup.p_SRN.strGreyGUIName, 0);


            //更新光度图清单 
            strTmpShowSubs = new string[maxCameraNum];
            for (int i = 0; i < cameraNum; i++)
            {
                strTmpName = cup.p_SRN.strFXGUIName[i];
                for (int j = 0; j < count; j++)
                {
                    if (strTmpName == cup.p_SRN.strName[j])
                    {
                        strTmpShowSubs[i] = strTmpName;
                        break;
                    }
                }

            }
            strTmpShowSubs.CopyTo(cup.p_SRN.strFXGUIName, 0);

            //更新光度图清单 
            strTmpShowSubs = new string[maxCameraNum];
            for (int i = 0; i < cameraNum; i++)
            {
                strTmpName = cup.p_SRN.strPMAGUIName[i];
                for (int j = 0; j < count; j++)
                {
                    if (strTmpName == cup.p_SRN.strName[j])
                    {
                        strTmpShowSubs[i] = strTmpName;
                        break;
                    }
                }

            }
            strTmpShowSubs.CopyTo(cup.p_SRN.strPMAGUIName, 0);
            cup.p_SRN.count = count;
            return true;
        }
       //判断VP是否已经运行过，运行过将产生图像记录
        public bool HaveRun()
        {
            ICogRecord result = mHistoryCollection[mSelectedJob].CurrentItem;
            if (result == null)
                return false;
            return true;
        }
        CogJob j;
        public string[] RetStatisticsForJob(int i)
        {
            bool runningLive = mCurrentRunState == RunState.RunningLive;
            if (runningLive)
                return null;
            string[] strStatus = new string[12];
            if (cup.p_VPPRTP.bDeepInfer)
            {
                strStatus[0] = iDeepInferTime.ToString();
                return strStatus;
            }
            else
            {
                j = mJM.Job(i);
                strStatus[0] = (j.TotalJobIterations).ToString();
                //  strStatus[1] = j.TotalVisionToolAccepts.ToString();
                strStatus[1] = (j.TotalJobIterations - iReject).ToString();
                string percentStr = "0";
                if (j.TotalJobIterations > 0)
                {
                    double p = (j.TotalJobIterations - iReject) * 100.0 / (j.TotalJobIterations);
                    percentStr = p.ToString("0.0");
                }

                strStatus[2] = percentStr;
                //  strStatus[3] = j.TotalVisionToolRejects.ToString();
                strStatus[3] = iReject.ToString();
                strStatus[4] = j.TotalVisionToolWarnings.ToString();
                strStatus[5] = j.TotalVisionToolErrors.ToString();

                strStatus[6] = j.TotalAcquisitions.ToString();
                strStatus[7] = j.TotalAcquisitionErrors.ToString();
                strStatus[8] = j.TotalAcquisitionOverruns.ToString();

                if (j.TotalJobThroughputMax != 0)
                {
                    strStatus[9] = j.TotalJobThroughput.ToString("0.000");
                    strStatus[10] = j.TotalJobThroughputMin.ToString("0.000");
                    strStatus[11] = j.TotalJobThroughputMax.ToString("0.000");
                }
                else
                {
                    string waiting = ResourceUtility.GetString("RtStatsWaiting");
                    strStatus[9] = waiting;
                    strStatus[10] = waiting;
                    strStatus[11] = waiting;
                }
            }

            return strStatus;
        }
        #endregion

        #region  光度立体法相关
        public void FXSurfaceFromImageFile(int iFileInd)
        {
            acqFIFO.FXSurfaceAllFromFile(iFileInd, cup.p_CD.bListProcess);
            if (cup.p_CD.bListProcess[0])
            {
                if (iModelRole == 10)
                    cogRDMain.Display.Image = acqFIFO.ci8gAcqFX[0];
                else
                    cogRDMainUser.Image = acqFIFO.ci8gAcqFX[0];
                // cogRDSub1.Image = acqFIFO.ci8gAcq[0];
            }
            if (cup.p_CD.bListProcess[1])
            {
                cogRDSub1.Image = acqFIFO.ci8gAcqFX[1];
            }
            if (cup.p_CD.bListProcess[2])
            {
                cogRDSub2.Image = acqFIFO.ci8gAcqFX[2];
            }
            /*if (cup.p_CD.bListProcess[3])
            {
                cogRDSub3.Image = acqFIFO.ci8gAcqFX[3];
            }
            if (cup.p_CD.bListProcess[4])
            {
                cogRDSub4.Image = acqFIFO.ci8gAcqFX[4];
            }*/

        }
        public void FXSurfaceFromCamNew()
        {
            //  acqFIFO.FXSurfaceAll(camNum, bRecord);
            acqFIFO.FXSurfaceAll(bRecord, cup.p_CD.bListProcess);

            if (cup.p_CD.bListProcess[0])
            {
                if (iModelRole == 10)
                    cogRDMain.Display.Image = acqFIFO.ci8gAcqFX[0];
                else
                    cogRDMainUser.Image = acqFIFO.ci8gAcqFX[0];
                // cogRDSub1.Image = acqFIFO.ci8gAcq[0];
            }
            if (cup.p_CD.bListProcess[1])
            {
                cogRDSub1.Image = acqFIFO.ci8gAcqFX[1];
            }
            if (cup.p_CD.bListProcess[2])
            {
                cogRDSub2.Image = acqFIFO.ci8gAcqFX[2];
            }
            /*if (cup.p_CD.bListProcess[3])
            {
                cogRDSub3.Image = acqFIFO.ci8gAcqFX[3];
            }
            if (cup.p_CD.bListProcess[4])
            {
                cogRDSub4.Image = acqFIFO.ci8gAcqFX[4];
            }*/
        }
        public bool PreViewOneImageFromFileFX(int iFileInd, bool bSingle)
        {
            try
            {
                ci8gAcq = acqFIFO.GrabOneFrameCogFromFileSingle(iFileInd, cup.p_CD.bListProcess, cup.p_VPPRTP.iXBType);
                for (int i = 0; i < maxCameraNum; i++)
                {
                    if (cup.p_CD.bListProcess[i] && ci8gAcq != null && ci8gAcq[i] != null)  //启用的检测面才做采集
                    {
                        switch (i)
                        {
                            case 0:
                                if (iModelRole == 10)
                                {
                                    cogRDMain.Display.Image = ci8gAcq[i];

                                    //  AddGrap(cogRDMain, DL_result[i]);
                                }
                                else
                                {
                                    cogRDMainUser.Image = ci8gAcq[i];

                                    //   AddGrap(cogRDMainUser, DL_result[i]);
                                }

                                break;
                            case 1:
                                cogRDSub1.Image = ci8gAcq[i];
                                //  AddGrap(cogRDSub1, DL_result[i]);
                                break;
                            case 2:
                                cogRDSub2.Image = ci8gAcq[i];
                                //   AddGrap(cogRDSub2, DL_result[i]);
                                break;
                                /*case 3:
                                    cogRDSub3.Image = ci8gAcq[i];
                                 //   AddGrap(cogRDSub3, DL_result[i]);
                                    break;
                                case 4:
                                    cogRDSub4.Image = ci8gAcq[i];*/
                                //    AddGrap(cogRDSub4, DL_result[i]);
                                break;
                        }
                    }

                }

                {
                    ci8gAcq = acqFIFO._ci8gAcq;
                    ci8gFX = acqFIFO.ci8gAcqFX;

                    if (cup.p_CD.bListProcess[0] && ci8gFX[0] != null)
                    {
                        ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Inputs["InputImageFX"].Value = ci8gFX[0];
                        ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Inputs["InputImageGrey"].Value = ci8gAcq[0];

                    }
                    for (int i = 1; i < maxCameraNum; i++)
                    {
                        if (cup.p_CD.bListProcess[i] && ci8gAcq[i] != null)
                        {
                            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面" + i.ToString() + "定位"] as CogToolBlock).Inputs["InputImageGrey"].Value = ci8gAcq[i];
                            ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面" + i.ToString() + "定位"] as CogToolBlock).Inputs["InputImageFX"].Value = ci8gFX[i];
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

        #endregion

        #region  图像采集相关

        public void OpenOrCloseCam(bool bOpen)
        {
            if (bOpen)
            {
                if (hikGrabThread != null && hikGrabThread.IsAlive)
                    return;
                try
                {

                    // 用线程更新显示
                    hikCanGrab = true;
                    hikGrabThread = new Thread(GrabThreadProcess);
                    hikGrabThread.Start();
                }
                catch
                {
                    hikCanGrab = false;
                    hikGrabThread.Join();
                    // MessageBox.Show("开始采集失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    // operLog.iEventStatus = -1;
                    return;
                }
            }
            else
            {
                try
                {
                    if (hikGrabThread != null)
                    {
                        hikCanGrab = false;
                        hikGrabThread.Join();  // 主线程阻塞，等待线程结束
                                               //      hIKVisionCamera.StopGrabbing();  // 停止采集  

                    }
                }
                catch
                {

                }
            }
        }
        //实时预览图像
        public void GrabThreadProcess()
        {
            if (true)
            {

                //  HObject hIm = new HObject();
                int wsInd = cup.p_VPPRTP.iXBType;
             //   cogRDMain.Display.cogRecordDisplay.Record = null;
               
                while (hikCanGrab)
                {
                    try
                    {
                        //   GrabThreadProcessHalcon();
                        // 将海康数据类型转为Mat
                        if (cup.p_CD.bListProcess[wsInd])
                        {
                            //黑白
                            if (cup.p_Cam[wsInd].iMonoType == 0)
                            {
                                acqFIFO.GrabOneFrameCog(cup.p_CD.camInd[wsInd]);
                                Thread.Sleep(2);
                                if (cup.p_CD.bListProcess[wsInd] && acqFIFO.ci8gAcq[wsInd] != null)  //启用的检测面 
                                {
                                    if (iModelRole == 10)
                                    {
                                        cogRDMain.Display.Image = acqFIFO.ci8gAcq[wsInd];
                                    }
                                    else
                                    {
                                        cogRDMainUser.Image = acqFIFO.ci8gAcq[wsInd];
                                    }
                                }
                            }
                            else  //彩色
                            {
                                acqFIFO.GrabOneFrameCogColor(cup.p_CD.camInd[wsInd]);
                                Thread.Sleep(2);
                                if (cup.p_CD.bListProcess[wsInd] && acqFIFO.ci24ColorAcq[wsInd] != null)  //启用的检测面 
                                {
                                    if (iModelRole == 10)
                                    {
                                        cogRDMain.Display.Image = acqFIFO.ci24ColorAcq[wsInd];
                                    }
                                    else
                                    {
                                        cogRDMainUser.Image = acqFIFO.ci24ColorAcq[wsInd];
                                    }
                                }
                            }

                        }
                    }
                    catch
                    {
                        // MessageBox.Show(se.Message);
                        // picMain.Image = bitmap;
                    }
                    //     Thread.Sleep(50);
                }
            }


        }

        public bool PreViewOneImageFromFile(int iFileInd, bool bSingle, int wsInd)
        {
            try
            {
                int iImageType= acqFIFO.GrabOneFrameCogFromFile(iFileInd, cup.p_CD.bListProcess, wsInd);
                ICogImage ci=null;
                if (iImageType==0)
                {
                    ci = acqFIFO.ci8gAcq[wsInd];
                }
                else if (iImageType == 1)
                {
                    ci = acqFIFO.ci24ColorAcq[wsInd];
                }                 

                switch (wsInd)
                {
                    case 0:
                        if (iModelRole == 10)
                        {
                            if (cogRDMain.Visible == false)
                            {
                                cogRDMain.Visible = true;
                                cogRDMainUser.Visible = false;
                            }
                            cogRDMain.Display.Image = ci;
                            if (bLoadVPRes)
                            {

                            }
                        }
                        else
                        {
                            if (cogRDMainUser.Visible == false)
                            {
                                cogRDMainUser.Visible = true;
                                cogRDMain.Visible = false;
                            }
                            cogRDMainUser.Image = ci;
                        }
                        break;
                    case 1:
                        cogRDSub1.Image = ci;
                        break;
                    case 2:
                        cogRDSub2.Image = ci;
                        break;
                   /* case 3:
                        cogRDSub3.Image = ci;
                        break;
                    case 4:
                        cogRDSub4.Image = ci;
                        break;*/
                }
                
                /*   {
                                 ci8gAcq = acqFIFO._ci8gAcq;
                                 ci8gFX = acqFIFO.ci8gAcqFX;

                                 if (cup.p_CD.bListProcess[wsInd] && ci8gFX[wsInd] != null)
                                 {
                                  //   ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as Cog
                                     ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Inputs["InputImageFX"].Value = ci8gFX[0];
                                     ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Inputs["InputImageGrey"].Value = ci8gAcq[0];

                                 }
                                 for (int i = 1; i < maxCameraNum; i++)
                                 {
                                     if (cup.p_CD.bListProcess[i] && ci8gAcq[i] != null)
                                     {
                                         ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面" + i.ToString() + "定位"] as CogToolBlock).Inputs["InputImageGrey"].Value = ci8gAcq[i];
                                         ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面" + i.ToString() + "定位"] as CogToolBlock).Inputs["InputImageFX"].Value = ci8gFX[i];
                                     }
                                 }

                             }*/
                /*     if(bLoadVPRes)
                     {
                         ReadVPResult();
                         ShowVPResult();
                     }*/

            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool PreViewOneCalibImageFromFile(int wsInd)
        {
            try
            {
                ci8gAcq = acqFIFO.GrabOneFrameCogFromFileCalib(wsInd);
                if (cup.p_CD.bListProcess[wsInd] && ci8gAcq != null && ci8gAcq[wsInd] != null)  //启用的检测面才做采集
                {
                    if (iModelRole == 10)
                    {
                        cogRDMain.Display.Image = ci8gAcq[wsInd];
                        if (bLoadVPRes)
                        {

                        }
                    }
                    else
                    {
                        cogRDMainUser.Image = ci8gAcq[wsInd];

                    }
                }

            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool ClearCogDispaly()
        {
            //   cogRDMain.Display
            cogRDMain.Display.InteractiveGraphics.Clear();
            //    cogRDMain.Display.Image = null;
            if (cup.p_CD.bListProcess[1])
            {
                cogRDSub1.InteractiveGraphics.Clear();
            }
            if (cup.p_CD.bListProcess[2])
            { cogRDSub2.InteractiveGraphics.Clear(); }
            return true;
        }

        string[] SaveImagePrefix = { "Main_", "Sub1_", "Sub2_", "Sub3_", "Sub4_" };


        public bool PreViewOneImage(int wsInd)
        {
            string strTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string[] strImageFileS = new string[maxCameraNum];
            try
            {
                //   GrabThreadProcessHalcon();
                // 将海康数据类型转为Mat
                if (cup.p_CD.bListProcess[wsInd])
                {
                    //黑白
                    if (cup.p_Cam[wsInd].iMonoType == 0)
                    {
                        acqFIFO.GrabOneFrameCog(cup.p_CD.camInd[wsInd]);
                        Thread.Sleep(2);
                        if (cup.p_CD.bListProcess[wsInd] && acqFIFO.ci8gAcq[wsInd] != null)  //启用的检测面 
                        {
                            if (iModelRole == 10)
                            {
                                cogRDMain.Display.Image = acqFIFO.ci8gAcq[wsInd];
                            }
                            else
                            {
                                cogRDMainUser.Image = acqFIFO.ci8gAcq[wsInd];
                            }
                        }
                    }
                    else  //彩色
                    {
                        acqFIFO.GrabOneFrameCogColor(cup.p_CD.camInd[wsInd]);
                        Thread.Sleep(2);
                        if (cup.p_CD.bListProcess[wsInd] && acqFIFO.ci24ColorAcq[wsInd] != null)  //启用的检测面 
                        {
                            if (iModelRole == 10)
                            {
                                cogRDMain.Display.Image = acqFIFO.ci24ColorAcq[wsInd];
                            }
                            else
                            {
                                cogRDMainUser.Image = acqFIFO.ci24ColorAcq[wsInd];
                            }
                        }
                    }

                }
                //存储
                if (bRecord)
                {
                    //  cup.p_LI.strPrefix[i]
                    //  for (int i=0;i<maxCameraNum;i++)
                    {
                        strImageFileS[wsInd] = strSigSavePathS[wsInd] + "\\" + cup.p_LI.strPrefix[wsInd] + strTime + ".jpg";
                    }

                    //   for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (cup.p_CD.bListProcess[wsInd] )
                        {
                            mImageFile.Open(strImageFileS[wsInd], CogImageFileModeConstants.Write);
                            if (cup.p_Cam[wsInd].iMonoType == 0 &&  acqFIFO.ci8gAcq[wsInd] != null)
                                mImageFile.Append(ci8gAcq[wsInd]);
                            else if(acqFIFO.ci24ColorAcq[wsInd] != null)
                                mImageFile.Append(acqFIFO.ci24ColorAcq[wsInd]);

                            mImageFile.Close();
                        }
                    }
                }


            }
            catch
            {
                // MessageBox.Show(se.Message);
                // picMain.Image = bitmap;
            }
           
            return true;


        }
        public bool PreViewCalibImage(int wsInd)
        {
            string strTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string[] strImageFileS = new string[maxCameraNum];
            try
            {
                Thread.Sleep(20);
                //采集  
                if (cup.p_CD.bListProcess[wsInd])  //启用的检测面才做采集
                {
                    bTaskFinish[wsInd] = false;
                    ci8gAcq[wsInd] = acqFIFO.GrabOneFrameCog(cup.p_CD.camInd[wsInd]);
                }


                if (cup.p_CD.bListProcess[wsInd] && ci8gAcq[wsInd] != null)  //启用的检测面才做采集
                {
                    if (iModelRole == 10)
                    {
                        cogRDMain.Display.Image = ci8gAcq[wsInd];
                        cogRDMain.Display.Fit(true);
                    }
                    else
                    {
                        cogRDMainUser.Image = ci8gAcq[wsInd];
                        cogRDMainUser.Fit(true);
                    }
                    SetCalibBlobImage(ci8gAcq[wsInd]);

                }
                //存储 
                if (bRecord)
                {
                    //  cup.p_LI.strPrefix[i]
                    //  for (int i=0;i<maxCameraNum;i++)
                    {
                        strImageFileS[cup.p_VPPRTP.iXBType] = cup.p_Paf.strResSavePath + "\\calib_" + strTime + ".jpg";
                    }

                    //   for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (cup.p_CD.bListProcess[cup.p_VPPRTP.iXBType] && ci8gAcq[cup.p_VPPRTP.iXBType] != null)
                        {
                            mImageFile.Open(strImageFileS[cup.p_VPPRTP.iXBType], CogImageFileModeConstants.Write);
                            mImageFile.Append(ci8gAcq[wsInd]);
                            mImageFile.Close();
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool PreViewCrossImage(int wsInd)
        {
            //  string strTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            //  string[] strImageFileS = new string[maxCameraNum];
            try
            {
                Thread.Sleep(20);
                //采集  
                if (cup.p_CD.bListProcess[wsInd])  //启用的检测面才做采集
                {
                    bTaskFinish[wsInd] = false;
                    ci8gAcq[wsInd] = acqFIFO.GrabOneFrameCog(cup.p_CD.camInd[wsInd]);
                }


                if (cup.p_CD.bListProcess[wsInd] && ci8gAcq[wsInd] != null)  //启用的检测面才做采集
                {
                    if (iModelRole == 10)
                    {
                        cogRDMain.Display.Image = ci8gAcq[wsInd];
                        cogRDMain.Display.Fit(true);
                    }
                    else
                    {
                        cogRDMainUser.Image = ci8gAcq[wsInd];
                        cogRDMainUser.Fit(true);
                    }
                    //      SetCalibBlobImage(ci8gAcq[wsInd]);

                }
                //存储 
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool bEqualDisr = false;
        public void ViewLayout(int winNum)
        {
            int mainWindWidth = this.Width - 3;
            int mainWindHeight = this.Height - 3;
            cogRDMain.Left = 1;
            cogRDMain.Top = 1;

            picSub1.Visible = false;
            if (!bEqualDisr)
            {
                switch (winNum)
                {
                    case 1:

                        mainWindWidth = this.Width - 2;
                        mainWindHeight = this.Height - 2;

                        cogRDMain.Width = mainWindWidth;
                        cogRDMain.Height = mainWindHeight;

                        cogRDSub1.Visible = false;
                        cogRDSub2.Visible = false;
                        /*cogRDSub3.Visible = false;
                        cogRDSub4.Visible = false;*/
                        break;
                    case 3:
                        mainWindWidth = this.Width - 2;
                        mainWindHeight = this.Height - 2;
                        cogRDMain.Width = mainWindWidth * 2 / 3;
                        cogRDMain.Height = mainWindHeight;

                        cogRDSub1.Left = cogRDMain.Right + 1;
                        cogRDSub1.Top = cogRDMain.Top;
                        cogRDSub1.Width = mainWindWidth / 3;
                        cogRDSub1.Height = mainWindHeight / 2;


                        cogRDSub2.Left = cogRDMain.Right + 1;
                        cogRDSub2.Top = cogRDMain.Top + mainWindHeight / 2 + 1;
                        cogRDSub2.Width = mainWindWidth / 3;
                        cogRDSub2.Height = mainWindHeight / 2;

                        cogRDSub1.Visible = true;
                        cogRDSub2.Visible = true;
                        /*cogRDSub3.Visible = false;
                        cogRDSub4.Visible = false;*/
                        break;
                        /*case 5:

                            mainWindWidth = this.Width - 3;
                            mainWindHeight = this.Height - 3;


                            cogRDMain.Width = mainWindWidth * 2 / 3;
                            cogRDMain.Height = mainWindHeight * 2 / 3;


                            cogRDSub1.Left = cogRDMain.Right+1;
                            cogRDSub1.Top = cogRDMain.Top;
                            cogRDSub1.Width = mainWindWidth / 3;
                            cogRDSub1.Height = mainWindHeight / 3;


                            cogRDSub2.Left = cogRDMain.Right+1;
                            cogRDSub2.Top = cogRDMain.Top + mainWindHeight / 3+1;
                            cogRDSub2.Width = mainWindWidth / 3;
                            cogRDSub2.Height = mainWindHeight / 3;


                            cogRDSub3.Left = cogRDMain.Left;
                            cogRDSub3.Top = cogRDMain.Bottom+1;
                            cogRDSub3.Width = mainWindWidth / 3;
                            cogRDSub3.Height = mainWindHeight / 3;
                            cogRDSub3.BackColor = Color.Black;

                            cogRDSub4.Left = cogRDSub3.Right+1;
                            cogRDSub4.Top = cogRDMain.Bottom+1;
                            cogRDSub4.Width = mainWindWidth / 3;
                            cogRDSub4.Height = mainWindHeight / 3;
                            cogRDSub4.BackColor = Color.Black;
                            cogRDSub1.Visible = true;
                            cogRDSub2.Visible = true;
                            cogRDSub3.Visible = true;
                            cogRDSub4.Visible = true;


                            picSub1.Visible = true;

                            picSub1.Top = cogRDMain.Bottom+2;
                            picSub1.Width = mainWindWidth / 3;
                            picSub1.Height = mainWindHeight / 3;
                            picSub1.Left = cogRDSub4.Right+1;

                            break;*/

                }

            }
            else
            {
                switch (winNum)
                {
                    case 1:
                        mainWindWidth = this.Width - 2;
                        mainWindHeight = this.Height - 2;

                        cogRDMain.Left = 1;
                        cogRDMain.Top = 1;
                        cogRDMain.Width = mainWindWidth;
                        cogRDMain.Height = mainWindHeight;

                        cogRDSub1.Visible = false;
                        cogRDSub2.Visible = false;
                        /*cogRDSub3.Visible = false;
                        cogRDSub4.Visible = false;*/
                        break;
                    case 3:

                        mainWindWidth = this.Width - 3;
                        mainWindHeight = this.Height - 3;

                        cogRDMain.Left = 1;
                        cogRDMain.Top = 1;

                        cogRDMain.Width = mainWindWidth / 2;
                        cogRDMain.Height = mainWindHeight / 2;

                        cogRDSub1.Left = cogRDMain.Right + 1;
                        cogRDSub1.Top = cogRDMain.Top;
                        cogRDSub1.Width = mainWindWidth / 2;
                        cogRDSub1.Height = mainWindHeight / 2;


                        cogRDSub2.Left = cogRDMain.Left;
                        cogRDSub2.Top = cogRDMain.Bottom + 1;
                        cogRDSub2.Width = mainWindWidth / 2;
                        cogRDSub2.Height = mainWindHeight / 2;

                        cogRDSub1.Visible = true;
                        cogRDSub2.Visible = true;
                        /*cogRDSub3.Visible = false;
                        cogRDSub4.Visible = false;*/


                        picSub1.Visible = true;
                        picSub1.Top = cogRDMain.Bottom + 1;
                        picSub1.Width = mainWindWidth / 2;
                        picSub1.Height = mainWindHeight / 2;
                        picSub1.Left = cogRDSub2.Right + 1;

                        break;
                        /*case 5:
                            mainWindWidth = this.Width - 4;
                            mainWindHeight = this.Height - 3;

                            cogRDMain.Left = 1;
                            cogRDMain.Top = 1;

                            cogRDMain.Width = mainWindWidth  / 3;
                            cogRDMain.Height = mainWindHeight / 2;


                            cogRDSub1.Left = cogRDMain.Right+1;
                            cogRDSub1.Top = cogRDMain.Top;
                            cogRDSub1.Width = mainWindWidth / 3;
                            cogRDSub1.Height = mainWindHeight / 2;


                            cogRDSub2.Left = cogRDSub1.Right+1;
                            cogRDSub2.Top = cogRDMain.Top ;
                            cogRDSub2.Width = mainWindWidth / 3;
                            cogRDSub2.Height = mainWindHeight / 2;


                            cogRDSub3.Left = cogRDMain.Left;
                            cogRDSub3.Top = cogRDMain.Bottom+1;
                            cogRDSub3.Width = mainWindWidth / 3;
                            cogRDSub3.Height = mainWindHeight / 2;

                            cogRDSub3.BackColor = Color.Black;

                            cogRDSub4.Left = cogRDSub3.Right+1;
                            cogRDSub4.Top = cogRDMain.Bottom+1;
                            cogRDSub4.Width = mainWindWidth / 3;
                            cogRDSub4.Height = mainWindHeight / 2;
                            cogRDSub4.BackColor = Color.Black;
                            cogRDSub1.Visible = true;
                            cogRDSub2.Visible = true;
                            cogRDSub3.Visible = true;
                            cogRDSub4.Visible = true;


                            picSub1.Visible = true;
                            picSub1.Top = cogRDMain.Bottom+1;
                            picSub1.Width = mainWindWidth / 3;
                            picSub1.Height = mainWindHeight /2;
                            picSub1.Left = cogRDSub4.Right+1;



                            break;*/

                }

            }

            cogRDMainUser.Left = cogRDMain.Left;
            cogRDMainUser.Top = cogRDMain.Top;
            cogRDMainUser.Width = cogRDMain.Width;
            cogRDMainUser.Height = cogRDMain.Height;

            if (iModelRole == 10)
            {
                cogRDMainUser.Visible = false;
                cogRDMain.Visible = true;
            }
            else
            {
                cogRDMainUser.Visible = true;
                cogRDMain.Visible = false;
            }
        }
        public void ViewLayout()
        {
            int mainWindWidth = this.Width;
            int mainWindHeight = this.Height;

            cogRDMain.Width = mainWindWidth * 2 / 3;
            cogRDMain.Height = mainWindHeight;
            //**   hWindowControl1.Width = mainWindWidth * 2 / 3;
            //**   hWindowControl1.Height = mainWindHeight;
            //**20220409  主窗口大小调整
            //   cogRDMain.Top

            picSub1.Left = cogRDMain.Right;
            picSub1.Top = cogRDMain.Top;
            picSub1.Width = mainWindWidth / 3;
            picSub1.Height = mainWindHeight / 2;


            cogRDSub1.Left = cogRDMain.Right;
            cogRDSub1.Top = cogRDMain.Top;
            cogRDSub1.Width = mainWindWidth / 3;
            cogRDSub1.Height = mainWindHeight / 2;




            picSub2.Left = cogRDMain.Right;
            picSub2.Top = cogRDMain.Top + mainWindHeight / 2;
            picSub2.Width = mainWindWidth / 3;
            picSub2.Height = mainWindHeight / 2;

            cogRDSub2.Left = cogRDMain.Right;
            cogRDSub2.Top = cogRDMain.Top + mainWindHeight / 2;
            cogRDSub2.Width = mainWindWidth / 3;
            cogRDSub2.Height = mainWindHeight / 2;


        }


        CogImageFileTool cogIFT;  //st**20220811
        public int GetCurImageIndex()
        {
            //  NextImageIndex
            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
            return cogIFT.CurrentImageIndex;
        }
        public int GetNextImageIndex()
        {
            //  NextImageIndex
            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
            return cogIFT.NextImageIndex;
        }

        public void SetNextImageIndex(int[] indAll)
        {
            if (j == null)
                return;
            int ind = indAll[j.TotalJobIterations % indAll.Length];
            CogImageFileTool cogIFT;
            //  NextImageIndex
            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Tools["CogImageFileToolGrey"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1定位"] as CogToolBlock).Tools["CogImageFileToolGrey"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2定位"] as CogToolBlock).Tools["CogImageFileToolGrey"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面3定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面3定位"] as CogToolBlock).Tools["CogImageFileToolGrey"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面4定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面4定位"] as CogToolBlock).Tools["CogImageFileToolGrey"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

        }
        public void SetNextImageIndex(int ind)
        {
            CogImageFileTool cogIFT;
            //  NextImageIndex
            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Tools["CogImageFileToolGrey"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1定位"] as CogToolBlock).Tools["CogImageFileToolGrey"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2定位"] as CogToolBlock).Tools["CogImageFileToolGrey"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面3定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面3定位"] as CogToolBlock).Tools["CogImageFileToolGrey"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面4定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

            cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面4定位"] as CogToolBlock).Tools["CogImageFileToolGrey"] as CogImageFileTool;
            cogIFT.NextImageIndex = ind;

        }
        public int SetPreviewImage()
        {
            // CogImageFileTool cogIFT;
            //  NextImageIndex
            //  cogIFT.InputImage.
            /*  cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
              cogIFT.NextImageIndex -= 1;
              if (cogIFT.NextImageIndex < 0)
                  cogIFT.NextImageIndex = cogIFT.Operator.Count-1;
              cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Tools["CogImageFileToolGrey"] as CogImageFileTool;
              cogIFT.NextImageIndex -= 1;
              if (cogIFT.NextImageIndex < 0)
                  cogIFT.NextImageIndex = cogIFT.Operator.Count - 1;
              cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
              cogIFT.NextImageIndex -= 1;
              if (cogIFT.NextImageIndex < 0)
                  cogIFT.NextImageIndex = cogIFT.Operator.Count - 1;
              cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1定位"] as CogToolBlock).Tools["CogImageFileToolGrey"] as CogImageFileTool;
              cogIFT.NextImageIndex -= 1;
              if (cogIFT.NextImageIndex < 0)
                  cogIFT.NextImageIndex = cogIFT.Operator.Count - 1;
              cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
              cogIFT.NextImageIndex -= 1;
              if (cogIFT.NextImageIndex < 0)
                  cogIFT.NextImageIndex = cogIFT.Operator.Count - 1;
              cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2定位"] as CogToolBlock).Tools["CogImageFileToolGrey"] as CogImageFileTool;
              cogIFT.NextImageIndex -= 1;
              if (cogIFT.NextImageIndex < 0)
                  cogIFT.NextImageIndex = cogIFT.Operator.Count - 1;


              cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面3定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
              cogIFT.NextImageIndex -= 1;
              if (cogIFT.NextImageIndex < 0)
                  cogIFT.NextImageIndex = cogIFT.Operator.Count - 1;
              cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面3定位"] as CogToolBlock).Tools["CogImageFileToolGrey"] as CogImageFileTool;
              cogIFT.NextImageIndex -= 1;
              if (cogIFT.NextImageIndex < 0)
                  cogIFT.NextImageIndex = cogIFT.Operator.Count - 1;
              cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面4定位"] as CogToolBlock).Tools["CogImageFileToolFX"] as CogImageFileTool;
              cogIFT.NextImageIndex -= 1;
              if (cogIFT.NextImageIndex < 0)
                  cogIFT.NextImageIndex = cogIFT.Operator.Count - 1;
              cogIFT = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面4定位"] as CogToolBlock).Tools["CogImageFileToolGrey"] as CogImageFileTool;
              cogIFT.NextImageIndex -= 1;
              if (cogIFT.NextImageIndex < 0)
                  cogIFT.NextImageIndex = cogIFT.Operator.Count - 1;*/



            // if(cogIFT.NextImageIndex<0)
            //  cogIFT.NextImageInde+=cogIFT.
            //              ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Inputs["InputImage"].Value = ci8gFX[cup.p_CD.camInd[0]];
            return 0; //cogIFT.NextImageIndex;
        }

      
      
        //单步测试
        public void StepTest(int stepInd, bool bAcq)
        {
            switch (stepInd)
            {
                case 0:

                case 1:

                case 2:

                case 3:

                case 4:

                case 5:
                    acqFIFO.csd.ExeProcessAll(stepInd);
                    break;
            }

            if (!bAcq)
                return;
            PreViewOneImage(cup.p_VPPRTP.iXBType);

        }
      
        #endregion

        #region  保存相关
        Thread SaveImageResultThread;   //保存图像结果线程，UpdateGuiIfNeeded()中调用
        public int SaveVPP()
        {
            if (!cup.p_VPPRTP.bVisionInfer)
                return 0;
            string vpp = mLoadedVppFilename;
            try
            {
                bool wasAttached = mAttached;
                if (wasAttached)
                    AttachToJobManager(false);


                //st**保存之前进行备份
                string strPath = Path.GetDirectoryName(vpp);
                string strName = Path.GetFileName(vpp);

                string strBakName = strPath + "\\" + strName.Substring(0, strName.Length - 4);
                File.Copy(vpp, strBakName, true);

                Utility.FlushAllQueues(mJM);
                CogSerializer.SaveObjectToFile(mJM, vpp);
                return 0;
            }
            catch (IOException ex)
            {
                return 2;
                //    MessageBox.Show("保存流程文件失败");
            }
            catch (AccessViolationException ex)
            {
                return 2;

            }

        }
        //保存图片结果
        public void SaveImageReult()
        {
            string strTime = acqFIFO.strTime.ToString();
            string pathMain = strSaveResPath + strTime + "_Main_" + j.TotalJobIterations.ToString() + ".png";
            if (iModelRole == 10)
                SaveScreenImage(cogRDMain, pathMain);
            else
                SaveScreenImage(cogRDMainUser, pathMain);
            string pathSub1 = strSaveResPath + strTime + "_Sub1_" + j.TotalJobIterations.ToString() + ".png";
            SaveScreenImage(cogRDSub1, pathSub1);
            string pathSub2 = strSaveResPath + strTime + "_Sub2_" + j.TotalJobIterations.ToString() + ".png";
            SaveScreenImage(cogRDSub2, pathSub2);
            /*string pathSub3 = strSaveResPath + strTime + "_Sub3_" + j.TotalJobIterations.ToString() + ".png";
            SaveScreenImage(cogRDSub3, pathSub1);
            string pathSub4 = strSaveResPath + strTime + "_Sub4_" + j.TotalJobIterations.ToString() + ".png";
            SaveScreenImage(cogRDSub4, pathSub2); */
        }
        //保存识别结果
        public void SaveVPResult()
        {
            //     j.TotalJobIterations.ToString();
            try
            {
                // string strTime= DateTime.Now.ToString("HHmmssff");
                string strTime = acqFIFO.strTime.ToString();

                {
                    if (cup.p_CD.bListProcess[0])
                    {
                        vpRes.gpgsMain = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面检测"] as CogToolBlock).Outputs["gpgs"].Value as List<CogPolygon>;
                        vpRes.TF[0] = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["底面定位"] as CogToolBlock).Outputs["Transform2DLinear"].Value as CogTransform2DLinear;

                    }

                    if (cup.p_CD.bListProcess[1])
                    {
                        vpRes.gpgsSub1 = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1检测"] as CogToolBlock).Outputs["gpgs"].Value as List<CogPolygon>;
                        vpRes.TF[1] = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面1定位"] as CogToolBlock).Outputs["Transform2DLinear"].Value as CogTransform2DLinear;

                    }
                    if (cup.p_CD.bListProcess[2])
                    {
                        vpRes.gpgsSub2 = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2检测"] as CogToolBlock).Outputs["gpgs"].Value as List<CogPolygon>;
                        vpRes.TF[2] = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面2定位"] as CogToolBlock).Outputs["Transform2DLinear"].Value as CogTransform2DLinear;
                    }
                    if (cup.p_CD.bListProcess[3])
                    {
                        vpRes.gpgsSub3 = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面3检测"] as CogToolBlock).Outputs["gpgs"].Value as List<CogPolygon>;
                        vpRes.TF[3] = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面3定位"] as CogToolBlock).Outputs["Transform2DLinear"].Value as CogTransform2DLinear;
                    }
                    if (cup.p_CD.bListProcess[4])
                    {
                        vpRes.gpgsSub4 = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面4检测"] as CogToolBlock).Outputs["gpgs"].Value as List<CogPolygon>;
                        vpRes.TF[4] = ((mJM.Job(0).VisionTool as CogToolGroup).Tools["侧面4定位"] as CogToolBlock).Outputs["Transform2DLinear"].Value as CogTransform2DLinear;
                    }
                }
                string strFile = cup.p_Paf.strResSavePath + "\\" + cup.p_LI.strLineName + acqFIFO.strTime.ToString();
                using (FileStream stream = new FileStream(strFile, FileMode.Create))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, vpRes);
                }

            }
            catch
            {

            }

            //  return true;
        }
        //保存主图像窗口图片
        private void SaveScreenImage(CogRecordsDisplay display, string fullpath) //保存界面截图jpeg
        {
            try
            {
                string path = Path.GetDirectoryName(fullpath);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                Bitmap bmp = cogRDMain.Display.CreateContentBitmap(Cognex.VisionPro.Display.CogDisplayContentBitmapConstants.Image) as Bitmap;

                bmp.Save(fullpath, System.Drawing.Imaging.ImageFormat.Png);
            }
            catch (Exception ex)
            {
                //  Log.WriteTxt("保存处理图像出现错误，信息为" + ex.Message);
            }
        }
        //保存辅图像窗口图片
        private void SaveScreenImage(CogRecordDisplay display, string fullpath) //保存界面截图jpeg
        {
            try
            {
                string path = Path.GetDirectoryName(fullpath);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                Bitmap bmp = display.CreateContentBitmap(Cognex.VisionPro.Display.CogDisplayContentBitmapConstants.Image) as Bitmap;

                bmp.Save(fullpath, System.Drawing.Imaging.ImageFormat.Png);
            }
            catch (Exception ex)
            {
                //  Log.WriteTxt("保存处理图像出现错误，信息为" + ex.Message);
            }
        }

        #endregion

        #region  界面显示相关
        Thread cogRDThread;             //界面显示线程，  UpdateGuiIfNeeded()中调用

        public void InitBG()
        {
            cogRDMain.Display.BackColor = Color.Black;
            cogRDMain.Display.VerticalScrollBar = false;
            cogRDMain.Display.HorizontalScrollBar = false;

            cogRDMainUser.BackColor = Color.Black;
            cogRDMainUser.VerticalScrollBar = false;
            cogRDMainUser.HorizontalScrollBar = false;


            if (cup.p_VPPRTP.iLayoutType > 1)
            {
                cogRDSub1.VerticalScrollBar = false;
                cogRDSub1.HorizontalScrollBar = false;
                cogRDSub1.BackColor = Color.Black;
            }
            if (cup.p_VPPRTP.iLayoutType > 1)
            {
                cogRDSub2.VerticalScrollBar = false;
                cogRDSub2.HorizontalScrollBar = false;
                cogRDSub2.BackColor = Color.Black;
            }
            /*if (cup.p_VPPRTP.iLayoutType > 1)
            {
                cogRDSub3.VerticalScrollBar = false;
                cogRDSub3.HorizontalScrollBar = false;
                cogRDSub3.BackColor = Color.Black;
            }
            if (cup.p_VPPRTP.iLayoutType > 1)
            {
                cogRDSub4.VerticalScrollBar = false;
                cogRDSub4.HorizontalScrollBar = false;
                cogRDSub4.BackColor = Color.Black;
            }*/
        }
        public void SiftRDMain()
        {
            if (iModelRole == 10)
            {
                cogRDMain.Visible = true;
                cogRDMainUser.Visible = false;
                cogRDMain.Display.Image = cogRDMainUser.Image;
            }
            else
            {
                cogRDMain.Visible = false;
                cogRDMainUser.Visible = true;
                cogRDMainUser.Image = cogRDMain.Display.Image;
            }
        }
        private void cogRecordsDisplay()
        {
            //待修改，如果定位失败，要更新主界面图像
            if (!pmaRes.bLocated)
            {
                DirectShowImage();

            }
            else
            {
                try
                {
                    if (cup.p_CD.bListProcess[0] && strSubRecordsName[0] != null && iModelRole == 10)
                    {
                        //    cogRDMain.Display.Image = null;
                        cogRDMain.SelectedRecordKey = "LastRun." + strSubRecordsName[0];
                        cogRDMain.Display.BackColor = Color.Black;
                        /*
                        cogRDMain.Display.Fit(true);*/
                    }
                    else if (cup.p_CD.bListProcess[0] && strSubRecordsName[0] != null)
                    {
                        cogRDMainUser.Record = cogRDMain.Subject.SubRecords[strSubRecordsName[0]];
                        cogRDMainUser.BackColor = Color.Black;
                        /* 
                         cogRDMainUser.Fit(true);*/
                    }
                    if (cup.p_CD.bListProcess[1] && strSubRecordsName[1] != null)
                    {
                        cogRDSub1.Record = cogRDMain.Subject.SubRecords[strSubRecordsName[1]];//"侧面1检测多模板.CogFixtureTool1.OutputImage"
                        cogRDSub1.BackColor = Color.Black;
                        /*
                        cogRDSub1.Fit(true);*/
                    }
                    if (cup.p_CD.bListProcess[2] && strSubRecordsName[2] != null)
                    {
                        cogRDSub2.Record = cogRDMain.Subject.SubRecords[strSubRecordsName[2]];//"侧面1检测多模板.CogFixtureTool1.OutputImage"
                                                                                              //  cogRDSub1.Image = ci8gAcq[cup.p_CD.camInd[1]];
                        /*cogRDSub2.BackColor = Color.Black;
                        cogRDSub2.Fit(true);*/
                    }
                    /*if (cup.p_CD.bListProcess[3] && strSubRecordsName[3] != null)
                    {
                        cogRDSub3.Record = cogRDMain.Subject.SubRecords[strSubRecordsName[3]];//"侧面1检测多模板.CogFixtureTool1.OutputImage"
                                                                                              //  cogRDSub1.Image = ci8gAcq[cup.p_CD.camInd[1]];
                        cogRDSub3.BackColor = Color.Black;
                        cogRDSub3.Fit(true);
                    }
                    if (cup.p_CD.bListProcess[4] && strSubRecordsName[4] != null)
                    {
                        cogRDSub4.Record = cogRDMain.Subject.SubRecords[strSubRecordsName[4]];//"侧面1检测多模板.CogFixtureTool1.OutputImage"
                                                                                              //  cogRDSub1.Image = ci8gAcq[cup.p_CD.camInd[1]];
                        cogRDSub4.BackColor = Color.Black;
                        cogRDSub4.Fit(true);
                    }*/
                    /*AnaInferRes(2);
                    picSub1.Invalidate();*/
                }
                catch
                { };
            }
            UpdateGui(null, null);
        }

        private void picSub1_Paint(object sender, PaintEventArgs e)
        {
            if (iModelRole == 10 && bShowDiagInfo)
                DrawDLRes(e.Graphics);
        }

        public bool bShowDiagInfo = false;
        private void picSub1_DoubleClick(object sender, EventArgs e)
        {
            bShowDiagInfo = !bShowDiagInfo;
            picSub1.Invalidate();
        }

        //读取识别结果
        public void ReadVPResult()
        {
            string strTime = acqFIFO.strTime.ToString();

            string strFile = cup.p_Paf.strResSavePath + "\\" + cup.p_LI.strLineName + acqFIFO.strTime.ToString();
            if (File.Exists(strFile))
            {
                try
                {
                    using (FileStream stream = new FileStream(strFile, FileMode.Open))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        vpRes = (VPRes)formatter.Deserialize(stream);
                    }
                }
                catch
                {

                }
            }
            else
            {
                vpRes.bDefect = false;
                for (int i = 0; i < maxCameraNum; i++)
                    vpRes.iResCode[i] = 0;
                vpRes.xbType = 1;
                if (vpRes.gpgsMain != null)
                    vpRes.gpgsMain.Clear();
                if (vpRes.gpgsSub1 != null)
                    vpRes.gpgsSub1.Clear();
                if (vpRes.gpgsSub2 != null)
                    vpRes.gpgsSub2.Clear();
                if (vpRes.gpgsSub3 != null)
                    vpRes.gpgsSub3.Clear();
                if (vpRes.gpgsSub4 != null)
                    vpRes.gpgsSub4.Clear();

            }

            /**/
        }

        public void ShowVPResult()
        {

            //创建文本
            CogGraphicLabel ResultLabel = new CogGraphicLabel();//创建标签对象
            CogTransform2DLinear tf = new CogTransform2DLinear();


            if (vpRes.gpgsMain != null)
            {
                tf = vpRes.TF[0];
                for (int i = 0; i < vpRes.gpgsMain.Count; i++)
                {
                    if (iModelRole == 10)
                        cogRDMain.Display.InteractiveGraphics.Add(vpRes.gpgsMain[i].MapLinear(tf, CogCopyShapeConstants.All), "", false);
                    else
                        cogRDMainUser.InteractiveGraphics.Add(vpRes.gpgsMain[i].MapLinear(tf, CogCopyShapeConstants.All), "", false);
                    //  vpRes.gpgsMain[i].MapLinear(tf,CogCopyShapeConstants)
                }
            }
            if (vpRes.gpgsSub1 != null)
            {
                for (int i = 0; i < vpRes.gpgsSub1.Count; i++)
                    cogRDSub1.InteractiveGraphics.Add(vpRes.gpgsSub1[i].MapLinear(tf, CogCopyShapeConstants.All), "", false);
            }
            if (vpRes.gpgsSub2 != null)
            {
                for (int i = 0; i < vpRes.gpgsSub2.Count; i++)
                    cogRDSub2.InteractiveGraphics.Add(vpRes.gpgsSub2[i].MapLinear(tf, CogCopyShapeConstants.All), "", false);
            }
            /*if (vpRes.gpgsSub3 != null)
            {
                for (int i = 0; i < vpRes.gpgsSub3.Count; i++)
                    cogRDSub3.InteractiveGraphics.Add(vpRes.gpgsSub3[i].MapLinear(tf, CogCopyShapeConstants.All), "", false);
            }
            if (vpRes.gpgsSub4 != null)
            {
                for (int i = 0; i < vpRes.gpgsSub4.Count; i++)
                    cogRDSub4.InteractiveGraphics.Add(vpRes.gpgsSub4[i].MapLinear(tf, CogCopyShapeConstants.All), "", false);
            }*/

            //  strGroupName = "posz";
            ResultLabel.Color = CogColorConstants.Green;
            ResultLabel.Font = new Font("arial", 10);

            //    cogDisp.InteractiveGraphics.Add(ResultLabel, "", false);
        }
        private string[,] strInferRes = new string[maxCameraNum, 6];


        private void AnaInferRes(int iStep)
        {
            for (int i = 0; i < maxCameraNum; i++)
            {
                strInferRes[i, iStep] = "";
                strInferRes[i, iStep + 3] = "";
                /* for (int j = 0; j < DL_result[i].AL_Type.Count; j++)
                 {
                     if (DL_result[i].AL_Type[j] != 3)
                     {
                         strInferRes[i, iStep] += DL_result[i].AL_Type[j].ToString();
                         strInferRes[i, iStep + 3] += DL_result[i].AL_score[j].ToString("0.00");
                     }
                     // strInfo += DL_result[i].AL_Type[j].ToString() + "- " + DL_result[i].AL_score[j].ToString("0.00") + "  ";
                 }*/

            }
        }
        private void DrawDLRes(Graphics g)
        {
            Pen pen = new Pen(Color.White);
            pen.Width = 2;
            Font ft = new Font("宋体", 12, FontStyle.Regular);


            for (int i = 0; i < maxCameraNum; i++)
            {
                string strInfo = SaveImagePrefix[i].Replace('_', ':') + "  ";
                /*for (int j=0;j< DL_result[i].AL_Type.Count;j++)
                {
                    strInfo += DL_result[i].AL_Type[j].ToString() + "- " + DL_result[i].AL_score[j].ToString("0.00") + "  ";
                }*/
                for (int j = 0; j < 3; j++)
                {
                    strInfo += strInferRes[i, j] + "    ;";
                }
                strInfo += strInferRes[i, 3];
                //        if(DL_result[i].AL_Type.Count>0)
                {
                    g.DrawString(strInfo, ft, Brushes.White, 20, 40 + i * 30);
                    g.DrawString("深度推理结果：", ft, Brushes.White, 20, 10);
                }
            }

            //  g.DrawString(.strHeadInfo, ft, brCoor[index], m_RectArea[index].Left + m_nLeftRemain[index] + 25/**/, m_RectArea[index].Top + m_nTopRemain[index] - 30 + sizeTxt.Height * index);
        }

        public void DirectShowImage(int iJobIndex)
        {
            ICogImage cogImg=null;

            if (cup.p_Cam[iJobIndex].iMonoType == 0 && acqFIFO.ci8gAcq[iJobIndex] != null)
                cogImg = acqFIFO.ci8gAcq[iJobIndex];
            else if(acqFIFO.ci24ColorAcq[iJobIndex] != null)
                cogImg = acqFIFO.ci24ColorAcq[iJobIndex];

            {
                 
                if (cup.p_CD.bListProcess[iJobIndex] )  //启用的检测面才做采集
                {
                    switch (iJobIndex)
                    {
                        case 0:
                            if (iModelRole == 10)
                            {
                                cogRDMain.Display.Image = cogImg;
                                AddGrap(cogRDMain, DL_result[iJobIndex]);
                                AddGrapOkNg(cogRDMain, pmaRes.bOk);

                                cogRDMain.Display.Fit(true);
                            }
                            else
                            {
                                cogRDMainUser.Image = cogImg;
                                AddGrap(cogRDMainUser, DL_result[iJobIndex]);
                                AddGrapOkNg(cogRDMainUser, pmaRes.bOk);
                                cogRDMainUser.Fit(true);
                            }
                            break;
                        case 1:
                            cogRDSub1.Image = cogImg;
                            AddGrap(cogRDSub1, DL_result[iJobIndex]);
                            AddGrapOkNg(cogRDSub1, pmaRes.bOk);
                            cogRDSub1.Fit(true);
                            break;
                        case 2:
                            cogRDSub2.Image = cogImg;
                            AddGrap(cogRDSub2, DL_result[iJobIndex]);
                            AddGrapOkNg(cogRDSub2, pmaRes.bOk);
                            cogRDSub2.Fit(true);
                            break;
                            /* case 3:
                                 cogRDSub3.Image = cogImg;
                                 cogRDSub3.Fit(true);
                                 break;
                             case 4:
                                 cogRDSub4.Image = cogImg;
                                 cogRDSub4.Fit(true);
                                 break;*/
                    }
                }

            }

        }
        public void DirectShowImage()
        {
            for (int i = 0; i < maxCameraNum; i++)
            {
                if (cup.p_CD.bListProcess[i] && ci8gAcq[i] != null)  //启用的检测面才做采集
                {
                    switch (i)
                    {
                        case 0:
                            if (iModelRole == 10)
                            {
                                cogRDMain.Display.Image = ci8gAcq[i];
                                cogRDMain.Display.Fit(true);
                            }
                            else
                            {
                                cogRDMainUser.Image = ci8gAcq[i];
                                cogRDMainUser.Fit(true);
                            }
                            break;
                        case 1:
                            cogRDSub1.Image = ci8gAcq[i];
                            cogRDSub1.Fit(true);
                            break;
                        case 2:
                            cogRDSub2.Image = ci8gAcq[i];
                            cogRDSub2.Fit(true);
                            break;
                            /* case 3:
                                 cogRDSub3.Image = ci8gAcq[i];
                                 cogRDSub3.Fit(true);
                                 break;
                             case 4:
                                 cogRDSub4.Image = ci8gAcq[i];
                                 cogRDSub4.Fit(true);
                                 break;*/
                    }
                }

            }
        }

        public void SetGUIShow(int iType)
        {
            // cogRDMain.SelectedRecordKey
            try
            {
                switch (iType)
                {
                    case 0:
                        strSubRecordsName = cup.p_SRN.strDefaultGUIName;

                        break;
                    case 1:
                        strSubRecordsName = cup.p_SRN.strGreyGUIName;

                        break;
                    case 2:
                        strSubRecordsName = cup.p_SRN.strFXGUIName;

                        break;
                    case 3:
                        strSubRecordsName = cup.p_SRN.strPMAGUIName;

                        break;
                }
                if (cogRDMain.Subject == null)
                    return;
                if (strSubRecordsName[0] != null)
                {
                    if (iModelRole == 10)
                        cogRDMain.SelectedRecordKey = "LastRun." + strSubRecordsName[0];
                    else
                        cogRDMainUser.Record = cogRDMain.Subject.SubRecords[strSubRecordsName[0]];

                }
                if (strSubRecordsName[1] != null)
                    cogRDSub1.Record = cogRDMain.Subject.SubRecords[strSubRecordsName[1]];
                if (strSubRecordsName[2] != null)
                    cogRDSub2.Record = cogRDMain.Subject.SubRecords[strSubRecordsName[2]];
                /*if (strSubRecordsName[3] != null)
                    cogRDSub3.Record = cogRDMain.Subject.SubRecords[strSubRecordsName[3]];
                if (strSubRecordsName[4] != null)
                    cogRDSub4.Record = cogRDMain.Subject.SubRecords[strSubRecordsName[4]];*/
            }
            catch
            {

            }
            cogRDMain.Display.BackColor = Color.Black;
            cogRDMainUser.BackColor = Color.Black;
            cogRDSub1.BackColor = Color.Black;
            cogRDSub2.BackColor = Color.Black;
            /*cogRDSub3.BackColor = Color.Black;
            cogRDSub4.BackColor = Color.Black;*/
        }


        private void LoadMenuSubRecord()
        {
            contMenuSubRecord.Items.Clear();
            int count = cup.p_SRN.count;
            for (int i = 0; i < count; i++)
            {
                if (cup.p_SRN.bList[i])
                {
                    contMenuSubRecord.Items.Add(cup.p_SRN.strName[i]);
                }
            }
        }


        private void contMenuSubRecord_Click(object sender, EventArgs e)
        {
            foreach (ToolStripItem items in contMenuSubRecord.Items)
            {
                if (items.Selected == true)
                {
                    switch (iSubSlectIndex)
                    {
                        case 1:
                            cogRDSub1.Record = cogRDMain.Subject.SubRecords[items.ToString()];

                            break;

                        case 2:
                            cogRDSub2.Record = cogRDMain.Subject.SubRecords[items.ToString()];

                            break;
                    }

                }
            }
        }

        private void cogRDSub1_DoubleClick(object sender, EventArgs e)
        {
            iSubSlectIndex = 1;
            ICogImage imgTmp;//=new ICogImage();
            if (iModelRole == 10)
            {
                imgTmp = cogRDMain.Display.Image;
                cogRDMain.Display.Image = cogRDSub1.Image;
            }
            else
            {
                imgTmp = cogRDMainUser.Image;
                cogRDMainUser.Image = cogRDSub1.Image;
            }
            cogRDSub1.Image = imgTmp;

            //    LoadMenuSubRecord();
            //  contMenuSubRecord.Show();
        }
        /*private void cogRDSub3_DoubleClick(object sender, EventArgs e)
        {
            iSubSlectIndex = 3;
            ICogImage imgTmp;//=new ICogImage();
            if (iModelRole == 10)
            {
                imgTmp = cogRDMain.Display.Image;
                cogRDMain.Display.Image = cogRDSub3.Image;
            }
            else
            {
                imgTmp = cogRDMainUser.Image;
                cogRDMainUser.Image = cogRDSub3.Image;
            }
            cogRDSub3.Image = imgTmp;
        }
        private void cogRDSub4_DoubleClick(object sender, EventArgs e)
        {
            iSubSlectIndex = 4;
            ICogImage imgTmp;//=new ICogImage();
            if (iModelRole == 10)
            {
                imgTmp = cogRDMain.Display.Image;
                cogRDMain.Display.Image = cogRDSub4.Image;
            }
            else
            {
                imgTmp = cogRDMainUser.Image;
                cogRDMainUser.Image = cogRDSub4.Image;
            }
            cogRDSub4.Image = imgTmp;
        }*/
        private void cogRDSub2_DoubleClick(object sender, EventArgs e)
        {
            iSubSlectIndex = 2;
            ICogImage imgTmp;//=new ICogImage();
            if (iModelRole == 10)
            {
                imgTmp = cogRDMain.Display.Image;
                cogRDMain.Display.Image = cogRDSub2.Image;
            }
            else
            {
                imgTmp = cogRDMainUser.Image;
                cogRDMainUser.Image = cogRDSub2.Image;
            }
            cogRDSub2.Image = imgTmp;
        }

        public void FitAllWin()
        {
            for (int i = 0; i < maxCameraNum; i++)
            {
                if (cup.p_CD.bListProcess[i] && ci8gAcq[i] != null)  //启用的检测面才做采集
                {
                    switch (i)
                    {
                        case 0:
                            if (iModelRole == 10)
                                cogRDMain.Display.Fit(true);
                            else
                                cogRDMainUser.Fit(true);
                            break;
                        case 1:
                            cogRDSub1.Fit(true);
                            break;
                        case 2:
                            cogRDSub2.Fit(true);
                            break;
                            /*case 3:                           
                                cogRDSub3.Fit(true);
                                break;
                            case 4:                          
                                cogRDSub4.Fit(true);
                                break;*/
                    }
                }

            }
        }
        #endregion


        #region  深度学习推理相关
        //是否对结果进行标注
        public void SetRemark(bool bRemarkArea, bool bRemarkResult)
        {
            (mJM.Job(0).VisionTool as CogToolGroup).SetScriptTerminalData("bRemarkArea", bRemarkArea);
            (mJM.Job(0).VisionTool as CogToolGroup).SetScriptTerminalData("bRemarkResult", bRemarkResult);
        }

        /* public void MainPredict(string model_type, ref byte[] byteArray, bool one_img_flag)
         {

             //byte[] bytes=null;
             if (model_type == "YOLOv3" && one_img_flag)
             {

                 AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                 HttpClientHandler insecureHCH = new HttpClientHandler();
                 insecureHCH.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                 GrpcChannel grpcChannel = GrpcChannel.ForAddress("http://10.29.6.128:8086", new GrpcChannelOptions { HttpHandler = insecureHCH });
                 ConvertService.ConvertServiceClient client = new ConvertService.ConvertServiceClient(grpcChannel);

                 //  Image image = Image.FromFile(filename);
                 MemoryStream ms = new MemoryStream();

                 //   image.Save(ms, image.RawFormat);
                 //   byte[] byteArray = ms.ToArray();
                 var fs = Convert.ToBase64String(byteArray);

                 image req = new image { Inputimage = fs };
                 cls_result reply = client.classification(req);//发送图片，等待返回结果
                 string result = reply.ClsImg;
                 byteArray = Convert.FromBase64String(result);

                 //   MemoryStream memStream = new MemoryStream(bytes);

                 //    mImage = Image.FromStream(memStream);   //预测的结果
                 //Bitmap bp = new Bitmap(mImage);//转换结果格式

             }
             else if (model_type == "PPYOLOv2" && one_img_flag)
             {

                 AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                 HttpClientHandler insecureHCH = new HttpClientHandler();
                 insecureHCH.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                 GrpcChannel grpcChannel = GrpcChannel.ForAddress("http://10.29.6.128:8086", new GrpcChannelOptions { HttpHandler = insecureHCH });
                 ConvertService.ConvertServiceClient client = new ConvertService.ConvertServiceClient(grpcChannel);
                 //  Image image = Image.FromFile(filename);
                 MemoryStream ms = new MemoryStream();

                 //   image.Save(ms, image.RawFormat);
                 //   byte[] byteArray = ms.ToArray();
                 var fs = Convert.ToBase64String(byteArray);

                 image req = new image { Inputimage = fs };
                 det_result reply = client.detection(req);//发送图片，等待返回结果
                 string result = reply.DetImg;
                 byteArray = Convert.FromBase64String(result);

                 // MemoryStream memStream = new MemoryStream(bytes);

                 //  mImage = Image.FromStream(memStream);

             }
             // return bytes;
         }

         public void SetThreshold(float det_threshold)
         {
             // det_threshold = float.Parse(comboBox2.SelectedItem.ToString());
             AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
             HttpClientHandler insecureHCH = new HttpClientHandler();
             insecureHCH.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

             GrpcChannel grpcChannel = GrpcChannel.ForAddress("http://10.29.6.128:8086", new GrpcChannelOptions { HttpHandler = insecureHCH });
             ConvertService.ConvertServiceClient client = new ConvertService.ConvertServiceClient(grpcChannel);

             set inputThreshold = new set { Threshold = det_threshold };
             client.init(inputThreshold);
         }*/
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


        // CDL_Predict cdl_Predict = new CDL_Predict();
        /* public void Predict(CogImage8Grey cogImage, string strMethod)
         {
             //"PPYOLOv2"   YOLOv3
             int nSize = cogImage.Width * cogImage.Height;
             byte[] byteImage = new byte[nSize];

             // 获取CogImage8Grey的像素数据指针
             IntPtr Image8GreyIntPtr = cogImage.Get8GreyPixelMemory(CogImageDataModeConstants.Read, 0, 0, cogImage.Width, cogImage.Height).Scan0;

             // 拷贝Buffer数据到CogImage8Grey的像素数据区
             //   Marshal.Copy(PixelDataBuffer, 0, Image8GreyIntPtr, PixelDataBuffer.Length);

             Marshal.Copy(Image8GreyIntPtr, byteImage, 0, (int)nSize);

             cdl_Predict.SetThreshold(0.5f);
             cdl_Predict.MainPredict(strMethod, ref byteImage, true);

         }*/


        public void PredictInt(int indWin)
        {
            //  cogRDSub1.Image
            /* Predict((CogImage8Grey)cogRDSub1.Image, "PPYOLOv2");
            // cogRDMain.
           // cogRDMain.Subject.SubRecords[strSubRecordsName[1]].
         */
        }

        #endregion
       

        #region  9点标定
        //////////////////////////////////////////
        ///9点标定
        ///
        int iJobIndex = 0;
        public void _9PointMark()
        {
            iJobIndex = 1;
            //  mJM.Job(0).Run();
        }

        //从VP9点标定工具中获取9点标定的数据
        public bool GetCalibPoint(ref double[] dCoor)
        {
            try
            {
                CogCalibNPointToNPointTool calib_NP2NP = (mJM.Job(0).VisionTool as CogToolGroup).Tools["CogCalibNPointToNPointTool1"] as CogCalibNPointToNPointTool;
                int nP = calib_NP2NP.Calibration.NumPoints;
                if (dCoor == null || dCoor.Length != nP * 4)
                {
                    dCoor = new double[nP * 4];
                }
                for (int i = 0; i < nP; i++)
                {
                    dCoor[i * 4] = calib_NP2NP.Calibration.GetUncalibratedPointX(i);
                    dCoor[i * 4 + 1] = calib_NP2NP.Calibration.GetUncalibratedPointY(i);
                    dCoor[i * 4 + 2] = calib_NP2NP.Calibration.GetRawCalibratedPointX(i);
                    dCoor[i * 4 + 3] = calib_NP2NP.Calibration.GetRawCalibratedPointY(i);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
                return false;
            }


            // calib_NP2NP.Calibration.
        }
        //设置VP9点标定工具中的坐标数据
        public void SetCalibPoint(double[] dCoor)
        {
            try
            {
                CogCalibNPointToNPointTool calib_NP2NP = (mJM.Job(0).VisionTool as CogToolGroup).Tools["CogCalibNPointToNPointTool1"] as CogCalibNPointToNPointTool;
                int nP = dCoor.Length / 4;
                calib_NP2NP.Calibration.NumPoints = nP;
                //  calib_NP2NP.Calibration.
                for (int i = 0; i < nP; i++)
                {
                    //  calib_NP2NP.Calibration.AddPointPair(dCoor[i * 4], dCoor[i * 4 + 1], dCoor[i * 4 + 2], dCoor[i * 4 + 3]);
                    calib_NP2NP.Calibration.SetUncalibratedPointX(i, dCoor[i * 4]);
                    calib_NP2NP.Calibration.SetUncalibratedPointY(i, dCoor[i * 4 + 1]);
                    calib_NP2NP.Calibration.SetRawCalibratedPointX(i, dCoor[i * 4 + 2]);
                    calib_NP2NP.Calibration.SetRawCalibratedPointY(i, dCoor[i * 4 + 3]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        //设置blob分析模板参数
        public void SetCalibBlobParam(calibBlobParam cbp)
        {
            try
            {
                CogBlobTool cbt = (mJM.Job(1).VisionTool as CogToolGroup).Tools["CogBlobTool1"] as CogBlobTool;
                cbt.RunParams.SegmentationParams.Polarity = cbp.bPolarity ? CogBlobSegmentationPolarityConstants.LightBlobs : CogBlobSegmentationPolarityConstants.DarkBlobs;
                cbt.RunParams.SegmentationParams.Mode = cbp.iSegType == 0 ? CogBlobSegmentationModeConstants.HardFixedThreshold : CogBlobSegmentationModeConstants.HardDynamicThreshold;
                cbt.RunParams.SegmentationParams.HardFixedThreshold = cbp.iThre;
                cbt.RunParams.RunTimeMeasures[0].Measure = CogBlobMeasureConstants.Area;
                cbt.RunParams.RunTimeMeasures[0].FilterMode = CogBlobFilterModeConstants.IncludeBlobsInRange;
                cbt.RunParams.RunTimeMeasures[0].FilterRangeLow = cbp.iAreaMin;
                cbt.RunParams.RunTimeMeasures[0].FilterRangeHigh = cbp.iAreaMax;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }
        //获取blob分析模板参数
        public void GetCalibBlobParam(ref calibBlobParam cbp)
        {
            try
            {
                CogBlobTool cbt = (mJM.Job(1).VisionTool as CogToolGroup).Tools["CogBlobTool1"] as CogBlobTool;
                cbp.bPolarity = cbt.RunParams.SegmentationParams.Polarity == CogBlobSegmentationPolarityConstants.LightBlobs ? true : false;
                cbp.iSegType = cbt.RunParams.SegmentationParams.Mode == CogBlobSegmentationModeConstants.HardFixedThreshold ? 0 : 1;
                cbp.iThre = cbt.RunParams.SegmentationParams.HardFixedThreshold;
                cbp.iAreaMin = (int)cbt.RunParams.RunTimeMeasures[0].FilterRangeLow;
                cbp.iAreaMax = (int)cbt.RunParams.RunTimeMeasures[0].FilterRangeHigh;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }
        public bool Get9PointCalibRes(ref CalibRes calibRes)
        {
            CogCalibNPointToNPointTool calib_NP2NP = (mJM.Job(0).VisionTool as CogToolGroup).Tools["CogCalibNPointToNPointTool1"] as CogCalibNPointToNPointTool;
            try
            {
                //  CalibRes calibRes = new CalibRes();
                calibRes.bCalib = calib_NP2NP.Calibration.Calibrated;
                calibRes.dErr = calib_NP2NP.Calibration.ComputedRMSError;
                CogTransform2DLinear calibrationTransform = calib_NP2NP.Calibration.GetComputedUncalibratedFromCalibratedTransform() as CogTransform2DLinear;

                calibRes.dTR = calibrationTransform.Rotation;
                calibRes.dTX = calibrationTransform.TranslationX;
                calibRes.dTY = calibrationTransform.TranslationY;
                calibRes.dXYRatio = calibrationTransform.Aspect;
                calibRes.dScale = calibrationTransform.Scaling;
                calibRes.dSkew = calibrationTransform.Skew;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
                return false;
            }
        }
        //使用VP9点标定工具进行9点标定
        public CalibRes _9PointCalib(double[] dCoor)
        {
            CogCalibNPointToNPointTool calib_NP2NP = (mJM.Job(0).VisionTool as CogToolGroup).Tools["CogCalibNPointToNPointTool1"] as CogCalibNPointToNPointTool;
            int nP = dCoor.Length / 4;
            calib_NP2NP.Calibration.NumPoints = nP;
            //  calib_NP2NP.Calibration.
            for (int i = 0; i < nP; i++)
            {
                //  calib_NP2NP.Calibration.AddPointPair(dCoor[i * 4], dCoor[i * 4 + 1], dCoor[i * 4 + 2], dCoor[i * 4 + 3]);
                calib_NP2NP.Calibration.SetUncalibratedPointX(i, dCoor[i * 4]);
                calib_NP2NP.Calibration.SetUncalibratedPointY(i, dCoor[i * 4 + 1]);
                calib_NP2NP.Calibration.SetRawCalibratedPointX(i, dCoor[i * 4 + 2]);
                calib_NP2NP.Calibration.SetRawCalibratedPointY(i, dCoor[i * 4 + 3]);
            }
            try
            {
                calib_NP2NP.Calibration.Calibrate();
                CalibRes calibRes = new CalibRes();
                calibRes.bCalib = calib_NP2NP.Calibration.Calibrated;
                calibRes.dErr = calib_NP2NP.Calibration.ComputedRMSError;
                CogTransform2DLinear calibrationTransform = calib_NP2NP.Calibration.GetComputedUncalibratedFromCalibratedTransform() as CogTransform2DLinear;

                calibRes.dTR = calibrationTransform.Rotation;
                calibRes.dTX = calibrationTransform.TranslationX;
                calibRes.dTY = calibrationTransform.TranslationY;
                calibRes.dXYRatio = calibrationTransform.Aspect;
                calibRes.dScale = calibrationTransform.Scaling;
                calibRes.dSkew = calibrationTransform.Skew;
                return calibRes;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
                return new CalibRes();
            }

        }

        //使用VP9点标定工具进行9点标定，图像坐标与激光坐标分开传入
        public CalibRes _9PointCalib2(double[] dCoorIm, double[] dCoorLaser)
        {
            CogCalibNPointToNPointTool calib_NP2NP = (mJM.Job(0).VisionTool as CogToolGroup).Tools["CogCalibNPointToNPointTool1"] as CogCalibNPointToNPointTool;
            int nP = dCoorIm.Length / 2;
            calib_NP2NP.Calibration.NumPoints = nP;
            for (int i = 0; i < nP; i++)
            {
                calib_NP2NP.Calibration.SetUncalibratedPointX(i, dCoorIm[i * 2]);
                calib_NP2NP.Calibration.SetUncalibratedPointY(i, dCoorIm[i * 2 + 1]);
                calib_NP2NP.Calibration.SetUncalibratedPointX(i, dCoorLaser[i * 2]);
                calib_NP2NP.Calibration.SetUncalibratedPointY(i, dCoorLaser[i * 2 + 1]);
            }

            calib_NP2NP.Calibration.Calibrate();
            CalibRes calibRes = new CalibRes();
            calibRes.bCalib = calib_NP2NP.Calibration.Calibrated;
            calibRes.dErr = calib_NP2NP.Calibration.ComputedRMSError;
            CogTransform2DLinear calibrationTransform = calib_NP2NP.Calibration.GetComputedUncalibratedFromCalibratedTransform() as CogTransform2DLinear;
            calibRes.dTR = calibrationTransform.Rotation;
            calibRes.dTX = calibrationTransform.TranslationX;
            calibRes.dTY = calibrationTransform.TranslationY;
            calibRes.dXYRatio = calibrationTransform.Aspect;
            calibRes.dScale = calibrationTransform.Scaling;
            calibRes.dSkew = calibrationTransform.Skew;
            return calibRes;
        }

        //设置图像并执行blob分析
        public void SetCalibBlobImage(CogImage8Grey calibIm)
        {
            try
            {

                CogBlobTool cbt = (mJM.Job(1).VisionTool as CogToolGroup).Tools["CogBlobTool1"] as CogBlobTool;
                cbt.InputImage = calibIm;
                ((mJM.Job(1).VisionTool as CogToolGroup).Tools["CogFindCircleTool1"] as CogFindCircleTool).InputImage = calibIm;
                mJM.Job(1).Run();
                Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
            //  calibIm.Run();
        }

        //获取9点标定  特征点坐标
        List<double> listCoord = new List<double>();
        public bool GetCalibBlobCoord(int row, int col, ref double[] dCoor)
        {
            try
            {

                CogBlobTool cbt = (mJM.Job(1).VisionTool as CogToolGroup).Tools["CogBlobTool1"] as CogBlobTool;
                int pointNum = row * col;
                int count = cbt.Results.GetBlobs().Count;
                if (count != pointNum)
                    return false;
                else
                {
                    // double[,] dCoord = new double[9, 2];
                    listCoord = (mJM.Job(1).VisionTool as CogToolGroup).GetScriptTerminalData("Coord") as List<double>;
                    if (listCoord == null || listCoord.Count / 2 != pointNum)
                        return false;
                    if (dCoor == null || dCoor.Length != count * 2)
                    {
                        dCoor = new double[count * 2];
                    }
                    for (int i = 0; i < count; i++)
                    {
                        dCoor[i * 2] = listCoord[i * 2];
                        dCoor[i * 2 + 1] = listCoord[i * 2 + 1];
                        // dCoor[i * 2] = cbt.Results.GetBlobs()[i].CenterOfMassX;
                        //  dCoor[i * 2+1] = cbt.Results.GetBlobs()[i].CenterOfMassY;
                    }

                    _2DCoordSort(row, col, dCoor);  //二维数组排序
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
                return false;
            }
        }
        //二维数组排序
        private void _2DCoordSort(int row, int col, double[] dCoor)
        {
            int count = row * col;
            //先按照Y坐标排序
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    if (dCoor[2 * j + 1] < dCoor[2 * i + 1])
                    {
                        double tmpX = dCoor[2 * i];
                        double tmpY = dCoor[2 * i + 1];

                        dCoor[2 * i] = dCoor[2 * j];
                        dCoor[2 * i + 1] = dCoor[2 * j + 1];

                        dCoor[2 * j] = tmpX;
                        dCoor[2 * j + 1] = tmpY;
                    }
                }
            }

            //再每行按照X坐标排序
            for (int r = 0; r < row; r++)
            {
                for (int i = col * r; i < col * (r + 1); i++)
                {
                    for (int j = i + 1; j < col * (r + 1); j++)
                    {
                        if (dCoor[2 * j] < dCoor[2 * i])
                        {
                            double tmpX = dCoor[2 * i];
                            double tmpY = dCoor[2 * i + 1];

                            dCoor[2 * i] = dCoor[2 * j];
                            dCoor[2 * i + 1] = dCoor[2 * j + 1];

                            dCoor[2 * j] = tmpX;
                            dCoor[2 * j + 1] = tmpY;
                        }
                    }
                }
            }

        }
        #endregion

        
    }
 
}
