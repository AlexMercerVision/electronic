using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using MvCamCtrl.NET;  // 海康相机
using HalconDotNet;
using Cognex.VisionPro;
using Cognex.VisionPro.ImageProcessing;
//using Cognex.VisionPro.ToolBlock;
//using Cognex.VisionPro.ImageFile;
//using Cognex.VisionPro3D;
using Cognex.VisionPro.Implementation;
using Cognex.VisionPro.ImageFile;
using OpenCvSharp;
using MvCamCtrl.NET;  // 添加海康相机引用

namespace CommonCls
{
    public class ImgAcqFIFO
    {
        //多通道摄像头组
        MyCamera[] camera2 = new MyCamera[maxCameraNum];
        //单个摄像头使用，FrmCam_Debug 调试使用
        public MyCamera camera = new MyCamera();
        //摄像头初始化状态，由外部主程序传入
        CameraStatus cs;
        int iCamType = 0;
        private int irtType = 0;    //旋转镜像类型
        int nRet = 0;
        public MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();  // 定义输出帧信息结构体
        public MyCamera.MV_FRAME_OUT stFrameInfo1 = new MyCamera.MV_FRAME_OUT();
        IntPtr m_BufForDriver;
        UInt32 nPayloadSize = 0;

       public IntPtr[] m_BufForDriverS=new IntPtr[maxCameraNum];
        UInt32[] nPayloadSizeS =new UInt32[maxCameraNum];


       public CSerialDef csd = new CSerialDef();
        CUserParam cup = new CUserParam();
        //

        HObject   hObj1, hObj2, hObj3, hObj4, hObj;
        HTuple Slants, Tilts, ResultType,sigma,dMulti,dAdd;
        HTuple Slants1, Tilts1, Slants2, Tilts2;
        HTuple sub1sigma, sub1dMulti, sub1dAdd;
        HTuple sub2sigma, sub2dMulti, sub2dAdd;

        HObject HeightField, Gradient, Albedo, MeanCurvature;
    //     HObject MeanCurvatureCM1, MeanCurvatureCM2;
      //  Bitmap res;
      //  CogImage8Grey ci8gDM, ci8gCM1, ci8gCM2;
        HObject[] hObjFX = new HObject[6];
     //   HObject[] hObjFXCM1 = new HObject[6];
     //   HObject[] hObjFXCM2 = new HObject[6];
        
        public HObject[,] hObjFXA = new HObject[maxCameraNum, 6];
        public byte[][] bImgSrcData = new byte[maxCameraNum][];
        const int maxCameraNum = 5;
        //五个通道的灰度图
       public CogImage8Grey[] ci8gAcq = new CogImage8Grey[maxCameraNum];
        //五个通道的光度图
        public CogImage8Grey[] ci8gAcqFX = new CogImage8Grey[maxCameraNum];


     //   public byte[][] btSourceImg = new byte[maxCameraNum][];

    //    CogImageFile mImageFile = new CogImageFile();
       public string[] strSigSavePathS = new string[maxCameraNum];
        public string[] strConSavePathS = new string[maxCameraNum];


      public  int cmAcqNum = 6;   //侧面采集图像的张数，老版本是5，新版本是6
       

        //    public string strSigMain, strSigSub1, strSigSub2;
        //    public string strConMain, strConSub1, strConSub2;
        //深度学习相关
      //  public CDL cdl = new CDL();
       public List<CogRectangleAffine>[] AL_det_result = new List<CogRectangleAffine>[maxCameraNum];

        CogIPOneImageTool imgTool = new CogIPOneImageTool();
        CogIPOneImageFlipRotate cogImFlipRotate = new CogIPOneImageFlipRotate();

        public ImgAcqFIFO()
        {

        }
        public int _rtType
        {
            get { return irtType; }
            set { irtType = value; }
        }
        public CameraStatus _cs
        {
            get { return cs; }
            set { cs = value; }
        }
        public CogImage8Grey[] _ci8gAcq
        {
            get { return  ci8gAcq; }
            set {  ci8gAcq = value; }
        }
        public HObject[] _hObj
        {
            get { return hObjFX; }
            set { hObjFX = value; }
        }
        public CUserParam _cup
        {
            get { return cup; }
            set { cup = value; }
        }

        public MyCamera[] _HikCam
        {
            get { return camera2; }
            set { camera2 = value; }
        }
        public IntPtr _Buff
        {
            get { return m_BufForDriver; }
            set { m_BufForDriver = value; }
        }
      
        public UInt32 _PayloadSize
        {
            get { return nPayloadSize; }
            set { nPayloadSize = value; }
        }
        public CSerialDef _csd
        {
            get { return csd; }
            set { csd = value; }
        }

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

        #region  对图像旋转镜像操作，未调试好
        MyCamera.MV_CC_ROTATE_IMAGE_PARAM rotateParam = new MyCamera.MV_CC_ROTATE_IMAGE_PARAM();
        MyCamera.MV_CC_FLIP_IMAGE_PARAM flipParam = new MyCamera.MV_CC_FLIP_IMAGE_PARAM();
       
        IntPtr ptrTmp = IntPtr.Zero;
        private void RotateAndFlipIm(MyCamera.MV_FRAME_OUT_INFO_EX frameParam, ref IntPtr ptrIm, int rotateType, int flipType)
        {
            
            rotateParam.nWidth = frameParam.nWidth;
            rotateParam.nHeight = frameParam.nHeight;
            if (rotateType==1 || rotateType==2 || rotateType==3)
            {
                
                ptrTmp = Marshal.AllocHGlobal((Int32)frameParam.nFrameLen);

                rotateParam.enPixelType = frameParam.enPixelType;// MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8;
             
                rotateParam.pSrcData = ptrIm;
                rotateParam.nSrcDataLen = frameParam.nFrameLen;
                rotateParam.pDstBuf = ptrTmp;
                rotateParam.nDstBufLen = frameParam.nFrameLen;
                rotateParam.nDstBufSize = frameParam.nFrameLen;
                rotateParam.enRotationAngle = (MyCamera.MV_IMG_ROTATION_ANGLE)rotateType;
                rotateParam.nRes = new uint[8];
                camera.MV_CC_RotateImage_NET(ref rotateParam);

                // 拷贝托管数组到非托管内存
              //  Marshal.Copy(ptrTmp, 0, ptrIm, frameParam.nFrameLen);
            } 
            if(flipType==1 || flipType==2)
            {
                flipParam.enPixelType = frameParam.enPixelType;// MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8;
                flipParam.nWidth = rotateParam.nWidth;
                flipParam.nHeight = rotateParam.nHeight;
                flipParam.pSrcData = ptrIm;
                flipParam.nSrcDataLen = frameParam.nFrameLen;
                flipParam.pDstBuf = ptrTmp;
                flipParam.nDstBufLen = frameParam.nFrameLen;
                flipParam.nDstBufSize = frameParam.nFrameLen;
                flipParam.enFlipType = (MyCamera.MV_IMG_FLIP_TYPE)flipType;
                flipParam.nRes = new uint[8];
                camera.MV_CC_FlipImage_NET(ref flipParam);
            }
           ptrIm = ptrTmp;
        }
        public void CogFlipRotateImInit()
        {
            ICogIPOneImageOperatorParams ip = (ICogIPOneImageOperatorParams)cogImFlipRotate;
            imgTool.Operators.Add(ip);
        }
        private CogImage8Grey CogFlipRotateIm(CogImage8Grey inputIm)
        {
            switch(irtType)
            {
                case 0:
                    cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.None;
                    break;
                case 1:
                    cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate90Deg;
                    break;
                case 2:
                    cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate180Deg;
                    break;
                case 3:
                    cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate270Deg;
                    break;
                case 4:
                    cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Flip;
                    break;
                case 5:
                    cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate90Deg;
                    break;
                case 6:
                    cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate180Deg;
                    break;
                case 7:
                    cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate270Deg;
                    break;
                case 8:
                    cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate180Deg;
                    break;
                case 9:
                    cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate270Deg;
                    break;
                case 10:
                    cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Flip;
                    break;
                case 11:
                    cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate90Deg;
                    break;
            }
     //       cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate180Deg;
            imgTool.InputImage = inputIm;
            imgTool.Run();
            return (CogImage8Grey)imgTool.OutputImage;
        }
        public CogImage8Grey GrabOneFrameCog(IntPtr m_BufForDriverS, uint nPayloadSizeS,int rotateType, int flipType)
        {
            /* if (camera != null)
                 camera2[iCamInd] = camera;*/

            CogImage8Grey cogImage8Grey = new CogImage8Grey();
            if (iCamType == 0)
            {
                nRet = camera.MV_CC_GetOneFrameTimeout_NET(m_BufForDriverS, nPayloadSizeS, ref stFrameInfo, 10000); // m_BufForDriver为图像数据接收指针

                if (nRet == MyCamera.MV_OK)
                {
                    RotateAndFlipIm(stFrameInfo, ref m_BufForDriverS, rotateType, flipType);
                    CogImage8Root cogImage8Root = new CogImage8Root();
                    cogImage8Root.Initialize((Int32)stFrameInfo.nWidth, (Int32)stFrameInfo.nHeight, m_BufForDriverS, (Int32)stFrameInfo.nWidth, null);

                    //  CogImage8Grey cogImage8Grey = new CogImage8Grey();
                    cogImage8Grey.SetRoot(cogImage8Root);
                    /* if (btSourceImg[iCamInd] == null || btSourceImg[iCamInd].Length != nPayloadSizeS[iCamInd])
                         btSourceImg[iCamInd] = new byte[nPayloadSizeS[iCamInd]];
                     Marshal.Copy(m_BufForDriverS[iCamInd], btSourceImg[iCamInd], 0, (int)nPayloadSizeS[iCamInd]);*/
                 
                }
                else
                {
                    return null;
                }
            }
            else
            {
                nRet = camera.MV_CC_GetImageBuffer_NET(ref stFrameInfo1, 1000);
                if (nRet == MyCamera.MV_OK)
                {

                    //  image.GenImage1("byte", (int)stFrameInfo1.stFrameInfo.nWidth, (int)stFrameInfo1.stFrameInfo.nHeight, stFrameInfo1.pBufAddr);
                    //  Marshal.Copy(stFrameInfo1.pBufAddr, byteImage, 0, (int)nPayloadSize);
                }
                else
                {
                    return null;
                }

            }
            return cogImage8Grey.Copy();   //st**202206务必添加copy
            // PtrToObj(matImage.Ptr(), cup.p_HikPara.iPixelType);

        }

        #endregion

        #region
        public void CloseAllExposureAuto(int CamNum)
        {
            for (int iCamInd = 0; iCamInd < CamNum; iCamInd++)
            {
                camera2[iCamInd].MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                Thread.Sleep(2);

            }
        }
        //全部关闭自动曝光
        public void CloseAllExposureAuto()
        {
            if (camera2 != null)
            {
                for (int iCamInd = 0; iCamInd < camera2.Length; iCamInd++)
                {
                    if (camera2[iCamInd] != null)
                        camera2[iCamInd].MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                    Thread.Sleep(2);

                }
            }
        }
        //全部打开自动曝光
        public void OpenAllExposureAuto(int CamNum)
        {
            for (int iCamInd = 0; iCamInd < CamNum; iCamInd++)
            {
                camera2[iCamInd].MV_CC_SetEnumValue_NET("ExposureAuto", 2);
                Thread.Sleep(2);
            }
        }
        public void OpenAllExposureAuto()
        {
            if (camera2 != null)
                for (int iCamInd = 0; iCamInd < camera2.Length; iCamInd++)
                {
                    if (camera2[iCamInd] != null)
                        camera2[iCamInd].MV_CC_SetEnumValue_NET("ExposureAuto", 2);
                    Thread.Sleep(2);
                }
        }

        public bool IsCamConn(int i)
        {
            if (camera2 != null && camera2[i] != null)
                return camera2[i].MV_CC_IsDeviceConnected_NET();
            else
                return false;
        }

        public void SetBuff(IntPtr _Buff, UInt32 _PayloadSize, int iCamIndex)
        {
            m_BufForDriverS[iCamIndex] = _Buff;
            nPayloadSizeS[iCamIndex] = _PayloadSize;
        }
        public void SetBuff(IntPtr[] _Buff, UInt32[] _PayloadSize)
        {
            m_BufForDriverS = _Buff;
            nPayloadSizeS = _PayloadSize;
        }

        //连续运行过程采集图像
        public HObject GrabOneFrameHObj(int iCamInd)
        {
            HImage image = new HImage();
            try
            {
                if (iCamType == 0)
                {
                    if (camera2 == null || camera2[iCamInd] == null)
                        return null;
                    nRet = camera2[iCamInd].MV_CC_GetOneFrameTimeout_NET(m_BufForDriverS[iCamInd], nPayloadSizeS[iCamInd], ref stFrameInfo, 1000); // m_BufForDriver为图像数据接收指针

                    if (nRet == MyCamera.MV_OK)
                    {
                        image.GenImage1("byte", (int)stFrameInfo.nWidth, (int)stFrameInfo.nHeight, m_BufForDriverS[iCamInd]);
                        //st 2023年1月5日
                     //   Marshal.Copy(m_BufForDriverS[iCamInd], btSourceImg[iCamInd], 0, (int)nPayloadSize);
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
            }
            catch
            {

            }
            return image;
            // PtrToObj(matImage.Ptr(), cup.p_HikPara.iPixelType);

        }

        
        //从摄像头采集单帧图像
        CogImage8Grey cogImage8Grey = new CogImage8Grey();
        CogImage8Root cogImage8Root = new CogImage8Root();
        public CogImage8Grey GrabOneFrameCog(int iCamInd)
        {
            if (cs.iStatus[iCamInd] < 2)
                return null;
               
            if (iCamType == 0)
            {
                nRet = camera2[iCamInd].MV_CC_GetOneFrameTimeout_NET(m_BufForDriverS[iCamInd], nPayloadSizeS[iCamInd], ref stFrameInfo, 10000); // m_BufForDriver为图像数据接收指针

           //     RotateAndFlipIm(stFrameInfo,ref m_BufForDriverS[iCamInd], 1, 1);
                if (nRet == MyCamera.MV_OK)
                {
                   
                    cogImage8Root.Initialize((Int32)stFrameInfo.nWidth, (Int32)stFrameInfo.nHeight, m_BufForDriverS[iCamInd], (Int32)stFrameInfo.nWidth, null);

                    //  CogImage8Grey cogImage8Grey = new CogImage8Grey();
                    cogImage8Grey.SetRoot(cogImage8Root);
                    /* if (btSourceImg[iCamInd] == null || btSourceImg[iCamInd].Length != nPayloadSizeS[iCamInd])
                         btSourceImg[iCamInd] = new byte[nPayloadSizeS[iCamInd]];
                     Marshal.Copy(m_BufForDriverS[iCamInd], btSourceImg[iCamInd], 0, (int)nPayloadSizeS[iCamInd]);*/
                    cogImage8Grey = CogFlipRotateIm(cogImage8Grey);
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

                  //  image.GenImage1("byte", (int)stFrameInfo1.stFrameInfo.nWidth, (int)stFrameInfo1.stFrameInfo.nHeight, stFrameInfo1.pBufAddr);
                    //  Marshal.Copy(stFrameInfo1.pBufAddr, byteImage, 0, (int)nPayloadSize);
                }
                else
                {
                    return null;
                }

            }
            return cogImage8Grey.Copy();   //st**202206务必添加copy
            // PtrToObj(matImage.Ptr(), cup.p_HikPara.iPixelType);

        }

        public CogImage8Grey[] GrabOneFrameCog()
        {
            for (int i = 0; i < maxCameraNum; i++)
            {
                if (cs.iStatus[i] < 2)
                    return null;
                CogImage8Grey cogImage8Grey = new CogImage8Grey();
                if (iCamType == 0)
                {
                    nRet = camera2[i].MV_CC_GetOneFrameTimeout_NET(m_BufForDriverS[i], nPayloadSizeS[i], ref stFrameInfo, 10000); // m_BufForDriver为图像数据接收指针

                    if (nRet == MyCamera.MV_OK)
                    {
                        CogImage8Root cogImage8Root = new CogImage8Root();
                        cogImage8Root.Initialize((Int32)stFrameInfo.nWidth, (Int32)stFrameInfo.nHeight, m_BufForDriverS[i], (Int32)stFrameInfo.nWidth, null);
                        cogImage8Grey.SetRoot(cogImage8Root);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    nRet = camera2[i].MV_CC_GetImageBuffer_NET(ref stFrameInfo1, 1000);
                    if (nRet == MyCamera.MV_OK)
                    {
                    }
                    else
                    {
                        continue;
                     //   return null;
                    }

                }
                ci8gAcq[i] = cogImage8Grey.Copy();   //st**202206务必添加copy
            }
            return ci8gAcq;
        }
       
        //从本地采集图像
        public CogImage8Grey[] GrabOneFrameCogFromFile(int iFileInd, bool[] bListProcess)
        {
            int tWait = 5;
            int ind = iFileInd * 6;
            HObject objTmp;
            try
            {
             //   csd.ExeProcessAll(0);

                for (int i = 0; i < maxCameraNum; i++)
                {
                    if (i == 0)
                    {
                        ind = iFileInd * 6;
                        int iStart = (Path.GetFileName(strFiles[i][ind])).IndexOf(cup.p_LI.strPrefix[i])+5;
                        int iEnd= (Path.GetFileName(strFiles[i][ind])).IndexOf("_00");
                         if(iStart==0)
                        {
                            iStart = cup.p_LI.strPrefix[i].Length;
                        }
                         
                        strTime = (Path.GetFileName(strFiles[i][ind])).Substring(iStart, iEnd-iStart);
                    }
                    else
                        ind = iFileInd * cmAcqNum;
                    if (bListProcess[i])
                    {

                        
                        //   string strPathDir = strDataPath + strSub[i];
                        //   if(strFiles[i]==null)
                        //       strFiles[i] = Directory.GetFiles(strPathDir, "*.jpg");
                        HOperatorSet.ReadImage(out objTmp, strFiles[i][ind]);

                        ci8gAcq[i] = HObj2CogImage(objTmp);
                        Thread.Sleep(tWait);
                    }
                }
             //   CloseAllExposureAuto(); //关闭自动曝光 
            }
            catch
            {
                return null;
            }
            return ci8gAcq;
        }

         //从本地采集单帧图像
        public CogImage8Grey[] GrabOneFrameCogFromFileSingle(int iFileInd, bool[] bListProcess,int wsInd/*工位号*/)   //逐帧读取
        {
            int tWait = 5;
           // int ind =0;
            HObject objTmp;
            try
            {
                //   csd.ExeProcessAll(0);

                if (bListProcess[wsInd])
                {
                    HOperatorSet.ReadImage(out objTmp, strFiles[wsInd][iFileInd]);
                    ci8gAcq[wsInd] = HObj2CogImage(objTmp);
                    Thread.Sleep(tWait);
                }
                //   CloseAllExposureAuto(); //关闭自动曝光 
            }
            catch
            {
                return null;
            }
            return ci8gAcq;
        }

        public CogImage8Grey[] GrabOneFrameCogFromFileCalib(  int wsInd/*工位号*/)   //逐帧读取
        {
            int tWait = 5;
            // int ind =0;
            HObject objTmp;
            try
            {
               
                {
                    HOperatorSet.ReadImage(out objTmp,cup.p_Paf.strExportPath);
                    ci8gAcq[wsInd] = HObj2CogImage(objTmp);
                    Thread.Sleep(tWait);
                }
                
            }
            catch
            {
                return null;
            }
            return ci8gAcq;
        }
        #endregion

        #region  格式转换相关
        //st 2022年12月22日 HObject格式图像转为byte
        private byte[] HObj2Bytes(HObject image)
        {

     //       HOperatorSet.Compose3(image, image, image, out HObject cImage);

     //       HOperatorSet.WriteImage(cImage, "jpg", 0, "cObjImg");
            HOperatorSet.GetImagePointer1(image, out HTuple pointer, out HTuple type, out HTuple width, out HTuple height);
           // HOperatorSet.Object
            byte[] by = new byte[width * height];
      //      byte[] byTmp = new byte[width * height];
            Marshal.Copy(pointer[0], by, 0, width * height);
            /*  for (int i=0;i< width * height;i++)
              {
                  by[i * 3] = byTmp[i];
                  by[i * 3+1] = byTmp[i];
                  by[i * 3 + 2] = byTmp[i];
              }*/
            //  Marshal.Copy(pointer[0], by, 0, width * height);

            //  byte bTmp = by[width * height-1];

            //  Marshal.Copy(pointer, by, width * height, width * height);
            //  Marshal.Copy(pointer, by, width * height*2, width * height);

            // Marshal.Copy(pointer,)
       //   HTuple  kernel = new HTuple ((double[,]){ {0, 1, 0},{1, 0, 1}, {0, 1, 0} });

            return by;
        }

        private CogImage8Grey HObj2CogImage(HObject image)
        {
            CogImage8Grey cogImage8Grey = new CogImage8Grey();
            HTuple hpoint, type, width, height;
            HOperatorSet.GetImagePointer1(image, out hpoint, out type, out width, out height);

            CogImage8Root cogImage8Root = new CogImage8Root();
            cogImage8Root.Initialize((Int32)width, (Int32)height, (IntPtr)hpoint, (Int32)width, null);

            //  CogImage8Grey cogImage8Grey = new CogImage8Grey();
            cogImage8Grey.SetRoot(cogImage8Root);
            return cogImage8Grey.Copy();
        }
        #endregion


        //构造三通道图像
        /*  public Mat ConstructRGB(int ind)
          {
              HTuple hpoint, type, Width=3072, Height=2048;
              Mat[] matrgb = new Mat[3];

              matrgb[0] = new Mat(Height, Width, MatType.CV_8UC1, btSourceImg[ind]);
              HOperatorSet.GetImagePointer1(hObjFXA[ind, 0], out hpoint, out type, out Width, out Height);
              matrgb[1] = new Mat(Height, Width, MatType.CV_8UC1, (IntPtr)hpoint);
              HOperatorSet.GetImagePointer1(hObjFXA[ind, 2], out hpoint, out type, out Width, out Height);
              matrgb[2] = new Mat(Height, Width, MatType.CV_8UC1, (IntPtr)hpoint);


              Mat matRGBImg = new Mat();
              //  Cv2.CvtColor(matImg, matRGBImg, ColorConversionCodes.GRAY2BGR);
              Cv2.Merge(matrgb, matRGBImg);
              return matRGBImg;
          }*/

        /* public byte[] ConstructRGBByte(int ind)
         {
             HTuple hpoint, type, Width = 3072, Height = 2048;
             Mat[] matrgb = new Mat[3];

             matrgb[0] = new Mat(Height, Width, MatType.CV_8UC1, btSourceImg[ind]);
             HOperatorSet.GetImagePointer1(hObjFXA[ind, 0], out hpoint, out type, out Width, out Height);
             matrgb[1] = new Mat(Height, Width, MatType.CV_8UC1, (IntPtr)hpoint);
             HOperatorSet.GetImagePointer1(hObjFXA[ind, 2], out hpoint, out type, out Width, out Height);
             matrgb[2] = new Mat(Height, Width, MatType.CV_8UC1, (IntPtr)hpoint);


             Mat matRGBImg = new Mat();
             //  Cv2.CvtColor(matImg, matRGBImg, ColorConversionCodes.GRAY2BGR);
             Cv2.Merge(matrgb, matRGBImg);

             byte[] bRGBData = new byte[Width * Height * 3];
             Marshal.Copy(matRGBImg.Data, bRGBData, 0, bRGBData.Length);

             return bRGBData;
         }*/

        //  string[] SaveImagePrefix = { "Main_", "Sub1_", "Sub2_", "Sub3_", "Sub4_" };
        //保存FXSurfaceAll函数产生的图片

        #region  图像文件操作相关
        public string strTime;
        public string[][] strFiles = new string[maxCameraNum][];
        public void SaveImageForCam(int ind,int dw)
        {

            strTime = DateTime.Now.ToString("yyyyMMddHHmmssFF");   //st 2023年1月25日  增加日期，以免名称冲突
            string strImageFile;

            strImageFile = strConSavePathS[ind] + "\\" + cup.p_LI.strPrefix[ind] + strTime + "_0" + dw.ToString() + ".jpg";
            if (cup.p_CD.bListProcess[ind] && hObjFXA[ind, 0] != null)
            {
                HOperatorSet.WriteImage(hObjFXA[ind, 0], "jpg", 0, strImageFile);
            } 
        }

        public void SaveImageForFXSurfaceAll()
        {
            
            strTime = DateTime.Now.ToString("yyyyMMddHHmmssFF");   //st 2023年1月25日  增加日期，以免名称冲突
            string strImageFile;
         

            for (int i = 0; i < maxCameraNum; i++)
            {
                for (int j = 0; j <= 4; j++)
                {
                    strImageFile = strConSavePathS[i] + "\\" + cup.p_LI.strPrefix[i] + strTime + "_0"+j.ToString()+".jpg";
                    if (cup.p_CD.bListProcess[i] && hObjFXA[i, j] != null)
                    {
                        HOperatorSet.WriteImage(hObjFXA[i, j], "jpg", 0, strImageFile);
                    }
                }
                strImageFile = strConSavePathS[i] + "\\" + cup.p_LI.strPrefix[i] + strTime + "_FX.jpg";
                if (cup.p_CD.bListProcess[i] && hObjFXA[i, 5] != null)
                {
                    HOperatorSet.WriteImage(hObjFXA[i, 5], "jpg", 0, strImageFile);
                }
            }
          //  return true;
        }
       
        //sr  2024年1月25日 删除当前数据  
        public bool DelOneGroupImageFile(bool bLive)
        {
            if(bLive)
            {
                DelOneGroupImageFile();     //删除实时数据
            }
            else
            {
                
            }
            return true;
        }
        public bool DelOneGroupImageFile()
        {
            string strImageFile;
            try
            {
                /*for (int i = 0; i < maxCameraNum; i++)
                {
                    //st 2023年1月5日  增加文件名前缀
                    strImageFile = strConSavePathS[i] + "\\" + SaveImagePrefix[i] + strTime + "_00.jpg";
                    if (File.Exists(strImageFile))
                    {
                        File.Delete(strImageFile);
                    }
                    else
                    {
                        strImageFile = strConSavePathS[i] + "\\"   + strTime + "_00.jpg";
                        if (File.Exists(strImageFile))
                        {
                            File.Delete(strImageFile);
                        }
                    }
                }*/
                for (int i = 0; i < maxCameraNum; i++)
                {
                    for (int j = 0; j <= 4; j++)
                    {
                        strImageFile = strConSavePathS[i] + "\\" + cup.p_LI.strPrefix[i] + strTime + "_0" + j.ToString() + ".jpg";
                        if (File.Exists(strImageFile))
                        {
                            File.Delete(strImageFile);
                        }
                        else
                        {
                            strImageFile = strConSavePathS[i] + "\\" + strTime + "_0" + j.ToString() + ".jpg";
                            if (File.Exists(strImageFile))
                            {
                                File.Delete(strImageFile);
                            }
                        }
                    }
                    strImageFile = strConSavePathS[i] + "\\" + cup.p_LI.strPrefix[i] + strTime + "_FX.jpg";
                    if (File.Exists(strImageFile))
                    {
                        File.Delete(strImageFile);
                    }
                    else
                    {
                        strImageFile = strConSavePathS[i] + "\\" + strTime + "_FX.jpg";
                        if (File.Exists(strImageFile))
                        {
                            File.Delete(strImageFile);
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
        public void SaveImageForErr(string[] strErrPathS,string strTime)
        {

          //  strTime = DateTime.Now.ToString("HHmmss");
            string strImageFile;
            /*for (int i = 0; i < maxCameraNum; i++)
            {
                if (cup.p_CD.bListProcess[i] && ci8gAcq[i] != null)
                {
                    //st 2023年1月5日  增加文件名前缀
                    strImageFile = strErrPathS[i] + "\\" + SaveImagePrefix[i] + strTime + "_00.jpg";
                    mImageFile.Open(strImageFile, CogImageFileModeConstants.Write);
                    mImageFile.Append(ci8gAcq[i]);
                    mImageFile.Close();
                }
            }*/

            for (int i = 0; i < maxCameraNum; i++)
            {
                for (int j = 0; j <= 4; j++)
                {
                    strImageFile = strErrPathS[i] + "\\" + cup.p_LI.strPrefix[i] + strTime + "_0" + j.ToString() + ".jpg";
                    if (cup.p_CD.bListProcess[i] && hObjFXA[i, j] != null)
                    {
                        HOperatorSet.WriteImage(hObjFXA[i, j], "jpg", 0, strImageFile);
                    }
                }
                strImageFile = strErrPathS[i] + "\\" + cup.p_LI.strPrefix[i] + strTime + "_FX.jpg";
                if (cup.p_CD.bListProcess[i] && hObjFXA[i, 5] != null)
                {
                    HOperatorSet.WriteImage(hObjFXA[i, 5], "jpg", 0, strImageFile);
                }
            }
            //  return true;
        }
        public bool ExportCurImage(string  strExportPath, string[,] strFiles)
        {

            //  strTime = DateTime.Now.ToString("HHmmss");
            string strImageFile;
            for (int i = 0; i < maxCameraNum; i++)
            {
                if(!Directory.Exists(strExportPath+"\\"+ cup.p_LI.strCamPrefix[i]))
                {
                    Directory.CreateDirectory(strExportPath + "\\" + cup.p_LI.strCamPrefix[i]);
                }
            }
            try
            {
                for (int i = 0; i < maxCameraNum; i++)
                {
                    for (int j = 0; j <= 5; j++)
                    {
                        strImageFile = strExportPath + "\\" + cup.p_LI.strCamPrefix[i] + "\\" + Path.GetFileName(strFiles[i, j]);

                        File.Copy(strFiles[i, j], strImageFile);
                    }

                }
            }catch
            {
                return false;
            }
            return true;
            //  return true;
        }
        #endregion

        #region  光度立体法  图像采集与重构

        public void InitFX()
        {
            /*if(cup.p_Paf.strConSavePath.Substring(cup.p_Paf.strConSavePath.Length-1,1)!="\\")
                strConSavePathS[0] = cup.p_Paf.strConSavePath + "\\" + "Main\\";
            else
                strConSavePathS[0] = cup.p_Paf.strConSavePath +  "Main\\";

            for(int i=1;i<maxCameraNum;i++)
            {
               if (cup.p_Paf.strConSavePath.Substring(cup.p_Paf.strConSavePath.Length - 1, 1) != "\\")
                    strConSavePathS[0] = cup.p_Paf.strConSavePath + "\\" + "Sub"+i.ToString()+"\\";
                else
                    strConSavePathS[0] = cup.p_Paf.strConSavePath + "Sub" + i.ToString() + "\\";
            }*/

            Slants = new HTuple();
            Tilts = new HTuple();
            Slants1 = new HTuple();
            Tilts1 = new HTuple();
            Slants2 = new HTuple();
            Tilts2 = new HTuple();

            ResultType = new HTuple();
            hObj1 = new HObject(); hObj2 = new HObject(); hObj3 = new HObject();
            hObj4 = new HObject(); hObj = new HObject();



            HeightField = new HObject();
            Gradient = new HObject();
            Albedo = new HObject();
            MeanCurvature = new HObject();

            Slants = cup.p_FX.Slants;
            Tilts = cup.p_FX.Tilts;
            ResultType = cup.p_FX.ResultType;
            sigma = cup.p_FX.sigma;
            dMulti = cup.p_FX.dMult;
            dAdd = cup.p_FX.dAdd;

            //侧面光度立体参数 
            try
            {
                Slants1 = cup.p_subFX[0].Slants;
                Slants2 = cup.p_subFX[1].Slants;
                Tilts1 = cup.p_subFX[0].Tilts;
                Tilts2 = cup.p_subFX[1].Tilts;

                sub1sigma = cup.p_subFX[0].sigma;
                sub1dMulti = cup.p_subFX[0].dMult;
                sub1dAdd = cup.p_subFX[0].dAdd;

                sub2sigma = cup.p_subFX[1].sigma;
                sub2dMulti = cup.p_subFX[1].dMult;
                sub2dAdd = cup.p_subFX[1].dAdd;
            }
            catch
            {
                Slants1[0] = 45; Slants1[1] = 45; Slants1[2] = 45;
                Slants2[0] = 45; Slants2[1] = 45; Slants2[2] = 45;
                Tilts1[0] = 0; Tilts1[1] = 90; Tilts1[2] = 180;
                Tilts2[0] = 0; Tilts2[1] = 90; Tilts2[2] = 180;

                sub1sigma = 2;
                sub1dMulti = 1460;
                sub1dAdd = 140;

                sub2sigma = 2;
                sub2dMulti = 1460;
                sub2dAdd = 140;
            }

        }

        public bool FXSurfaceAll(bool bRecord,bool[] bListProcess)
        {
       
            int tWait = 5; 
            try
            {
              //  if (cup.p_LS.bExe[0])  //执行时序0
                {
                    csd.ExeProcessAll(0);

                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (bListProcess[i])
                        {
                            //st 2023年2月28日
                            hObjFXA[i, 0] = GrabOneFrameHObj(cup.p_CD.camInd[i]);
                            ci8gAcq[i] = HObj2CogImage(hObjFXA[i, 0]);
                            Thread.Sleep(tWait);
                        }
                    }
                    CloseAllExposureAuto(); //关闭自动曝光 
                }

                if (cup.p_LS.bExe[1])  //执行时序1
                {
                    csd.ExeProcessAll(1);
                    //  if (cs.iStatus[cup.p_CD.camInd[0]] > 1)
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (bListProcess[i])
                        {
                            hObjFXA[i, 1] = GrabOneFrameHObj(cup.p_CD.camInd[i]);
                            Thread.Sleep(tWait);
                        }
                    }
                }

                if (cup.p_LS.bExe[2])  //执行时序2
                {
                    csd.ExeProcessAll(2);

                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (bListProcess[i])
                        {
                            hObjFXA[i, 2] = GrabOneFrameHObj(cup.p_CD.camInd[i]);
                            Thread.Sleep(tWait);
                        }
                    }
                }

                if (cup.p_LS.bExe[3])  //执行时序3
                {
                    csd.ExeProcessAll(3);

                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (bListProcess[i])
                        {
                            hObjFXA[i, 3] = GrabOneFrameHObj(cup.p_CD.camInd[i]);
                            Thread.Sleep(tWait);
                        }
                    }
                }

                if (cup.p_LS.bExe[4])  //执行时序1
                {
                    csd.ExeProcessAll(4);
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (bListProcess[i])
                        {
                            hObjFXA[i, 4] = GrabOneFrameHObj(cup.p_CD.camInd[i]);
                            Thread.Sleep(tWait);
                        }
                    }
                }


                csd.ExeProcessAll(5);

                OpenAllExposureAuto(); //打开自动曝光


                if (cup.p_VPPRTP.bFXRestruct)  //如果不需要光度立体图，此处则不执行，兼容一般性应用
                {
                    //st 2022年12月3日通过线程同步执行光度重构操作
                    for (int i = 1; i < maxCameraNum; i++)
                    {
                        if (bListProcess[i])
                        {
                            bFXRun[i] = true;
                            FXConstructThread[i] = new Thread(new ParameterizedThreadStart(FXConstruct));
                            FXConstructThread[i].Start((object)i);
                        }
                    }

                    //底面光度重构
                    if (bListProcess[0])
                    {
                        if (hObjFXA[0, 1] != null && hObjFXA[0, 2] != null && hObjFXA[0, 3] != null && hObjFXA[0, 4] != null)
                        {
                            HOperatorSet.ConcatObj(hObjFXA[0, 1], hObjFXA[0, 2], out hObj);
                            HOperatorSet.ConcatObj(hObj, hObjFXA[0, 3], out hObj);
                            HOperatorSet.ConcatObj(hObj, hObjFXA[0, 4], out hObj);

                            HOperatorSet.PhotometricStereo(hObj, out HeightField, out Gradient, out Albedo, Slants, Tilts, ResultType, "poisson", new HTuple(), new HTuple());
                            HOperatorSet.DerivateVectorField(Gradient, out MeanCurvature, sigma, "mean_curvature");
                            HOperatorSet.ScaleImage(MeanCurvature, out MeanCurvature, dMulti, dAdd);
                            HOperatorSet.ConvertImageType(MeanCurvature, out hObjFXA[0, 5], "byte");  //st  2023年2月28日  修改了光度图所在数组编号

                            ci8gAcqFX[0] = HObj2CogImage(hObjFXA[0, 5]);   //st  2023年2月28日  修改了光度图所在数组编号
                        }
                    }
                    //st 2022年12月3日通过线程同步执行光度重构操作
                    while (bFXRun[1] || bFXRun[2] || bFXRun[3] || bFXRun[4])
                    {
                        Thread.Sleep(10);
                    }
                    //st 2022年12月3日通过线程同步执行光度重构操作

                    //st 2022年12月3日通过线程同步执行存储文件操作
                    
                }
                if (bRecord)
                {
                    saveFileThread = new Thread(SaveImageForFXSurfaceAll);
                    saveFileThread.Start();

                    //   SaveImageForFXSurfaceAll(bListProcess);
                }

                return true;
            }
            catch
            {
                return false;
            }

        }
        public string strErrInfo = "";
        //2025年4月21日
        public bool FXSurfaceAll(bool bRecord, bool[] bListProcess, int wsInd)
        {

            int tWait = 5;
            try
            {
                if (bListProcess[wsInd])
                {
                    hObjFXA[wsInd, 0] = GrabOneFrameHObj(cup.p_CD.camInd[wsInd]);
                  //  if(cup.p_VPPRTP.bVisionInfer)
                    {
                        ci8gAcq[wsInd] = HObj2CogImage(hObjFXA[wsInd, 0]);
                        ci8gAcq[wsInd] = CogFlipRotateIm(ci8gAcq[wsInd]);
                    }                   
                    Thread.Sleep(tWait);

                    if (bRecord)
                    {
                        saveFileThread = new Thread(SaveImageForFXSurfaceAll);
                        saveFileThread.Start();
                    }
                }
                return true;
            }
            catch(Exception ex)
            {
                strErrInfo = ex.Message;
                return false;
            }

        }

       
        //    string strDataPath =@"F:\st\WorkSpace\项目\万华匣钵缺陷检测\data\万华测试3\20220807\";
        //    string[] strSub = { "Main", "Sub1", "Sub2", "Sub3", "Sub4" };

        Thread[] FXConstructThread =new Thread[maxCameraNum]  ;        //光度立体重构多个线程同步执行
        Thread saveFileThread;
        bool[] bFXRun = new bool[maxCameraNum];
        private void FXConstruct(Object ind)
        {
            int i = (int)ind;
            HTuple Slants1 = new HTuple();// cup.p_subFX[i - 1].Slants;
            HTuple Tilts1 = new HTuple();// cup.p_subFX[i - 1].Tilts;
            HObject Gradient; 
            HObject MeanCurvature;

            HObject hObj = null;
            int kInd = 0; ;
            for (int j = 1; j < 5; j++)
            {
                if (cup.p_subFX[i - 1].Tilts[j - 1] == -180)
                {
                    //   Slants.TupleRemove(j-1);
                    //   Tilts.TupleRemove(j-1); 
                }
                else
                {
                    if (kInd == 0)
                        hObj = hObjFXA[i, j].Clone();
                    else
                        HOperatorSet.ConcatObj(hObj, hObjFXA[i, j], out hObj);
                    
                    Tilts1[kInd] = cup.p_subFX[i - 1].Tilts[j - 1];
                    Slants1[kInd] = cup.p_subFX[i - 1].Slants[j - 1];
                    kInd++;
                }

            }
            HOperatorSet.PhotometricStereo(hObj, out _, out Gradient, out _, Slants1, Tilts1, ResultType, "poisson", new HTuple(), new HTuple());
            HOperatorSet.DerivateVectorField(Gradient, out MeanCurvature, cup.p_subFX[i - 1].sigma, "mean_curvature");
            HOperatorSet.ScaleImage(MeanCurvature, out MeanCurvature, cup.p_subFX[i - 1].dMult, cup.p_subFX[i - 1].dAdd);
            HOperatorSet.ConvertImageType(MeanCurvature, out hObjFXA[i, 5], "byte");  //st  2023年2月28日  修改了光度图所在数组编号
            ci8gAcqFX[i] = HObj2CogImage(hObjFXA[i, 5]);  //st  2023年2月28日  修改了光度图所在数组编号
           /* if (!cup.p_DL.bUseGPU)
                AL_det_result[i] = cdl.det_infer_one_img(btSourceImg[i], 3072, 2048, 3);*/

            bFXRun[i] = false;
        }
        //读取文件进行光度立体重构
        public string strFileTime;
        public bool FXSurfaceAllFromFile(int iFileInd, bool[] bListProcess)
        {
           // bool bRecord = false; 
            int tWait = 5; 
         //   HObject objTmp ;
            int ind = 0;
            try
            {
                csd.ExeProcessAll(0);

                //读取全光图
                for (int i = 0; i < maxCameraNum; i++)
                {
                    if (i == 0)
                    { 
                        ind = iFileInd * 6;
                        ind = iFileInd * 6;
                        int iStart = (Path.GetFileName(strFiles[i][ind])).IndexOf(cup.p_LI.strCamPrefix[i]);
                        int iEnd = (Path.GetFileName(strFiles[i][ind])).IndexOf("_00");
                         if(iStart==0)
                        {
                            iStart = cup.p_LI.strCamPrefix[i].Length;
            }

                        strTime = (Path.GetFileName(strFiles[i][ind])).Substring(iStart , iEnd - iStart);
                    }
                    else
                        ind = iFileInd * cmAcqNum;
                    if (bListProcess[i])
                    {
                     //   string strPathDir = strDataPath + strSub[i];
                     //   strFiles[i]= Directory.GetFiles(strPathDir, "*.jpg");
                        HOperatorSet.ReadImage(out hObjFXA[i, 0], strFiles[i][ind]);
                      
                    //    btSourceImg[i] = HObj2Bytes(hObjFXA[i, 0]);  //st 2022年12月22日
                        
                        ci8gAcq[i] = HObj2CogImage(hObjFXA[i, 0]); 
                        Thread.Sleep(tWait);
                    }
                }
                //     CloseAllExposureAuto(); //关闭自动曝光 

                if (cup.p_LS.bExe[1])
                {
                    csd.ExeProcessAll(1);

                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (i == 0)
                            ind = iFileInd * 6 + 1;
                        else
                            ind = iFileInd * cmAcqNum + 1;
                        if (bListProcess[i])
                        {
                            HOperatorSet.ReadImage(out hObjFXA[i, 1], strFiles[i][ind]);

                            Thread.Sleep(tWait);
                        }
                    }
                }

                if (cup.p_LS.bExe[2])
                {
                    csd.ExeProcessAll(2);

                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (i == 0)
                            ind = iFileInd * 6 + 2;
                        else
                            ind = iFileInd * cmAcqNum + 2;
                        if (bListProcess[i])
                        {
                            HOperatorSet.ReadImage(out hObjFXA[i, 2], strFiles[i][ind]);
                            // hObjFXA[i, 2] = GrabOneFrameHObj(cup.p_CD.camInd[i]);
                            Thread.Sleep(tWait);
                        }
                    }
                }


                if (cup.p_LS.bExe[3])
                {
                    csd.ExeProcessAll(3);

                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (i == 0)
                            ind = iFileInd * 6 + 3;
                        else
                            ind = iFileInd * cmAcqNum + 3;
                        if (bListProcess[i])
                        {
                            HOperatorSet.ReadImage(out hObjFXA[i, 3], strFiles[i][ind]);
                            //   hObjFXA[i, 3] = GrabOneFrameHObj(cup.p_CD.camInd[i]);
                            Thread.Sleep(tWait);
                        }
                    }
                }

                if (cup.p_LS.bExe[4])
                {
                    csd.ExeProcessAll(4);
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (i == 0)
                            ind = iFileInd * 6 + 4;
                        else
                            ind = iFileInd * cmAcqNum + 4 % (cmAcqNum - 1);

                        if (bListProcess[i])
                        {
                            HOperatorSet.ReadImage(out hObjFXA[i, 4], strFiles[i][ind]);
                            //    hObjFXA[i, 4] = GrabOneFrameHObj(cup.p_CD.camInd[i]);
                            Thread.Sleep(tWait);
                        }
                    }
                }

                csd.ExeProcessAll(5);

                OpenAllExposureAuto(); //打开自动曝光


                if (cup.p_VPPRTP.bFXRestruct)
                {
                    //st 2022年12月3日通过线程同步执行光度重构操作
                    for (int i = 1; i < maxCameraNum; i++)
                    {
                        if (bListProcess[i])
                        {
                            bFXRun[i] = true;
                            FXConstructThread[i] = new Thread(new ParameterizedThreadStart(FXConstruct));
                            FXConstructThread[i].Start((object)i);
                        }
                    }

                    //底面光度重构
                    if (bListProcess[0])
                    {
                        if (hObjFXA[0, 1] != null && hObjFXA[0, 2] != null && hObjFXA[0, 3] != null && hObjFXA[0, 4] != null)
                        {
                            HOperatorSet.ConcatObj(hObjFXA[0, 1], hObjFXA[0, 2], out hObj);
                            HOperatorSet.ConcatObj(hObj, hObjFXA[0, 3], out hObj);
                            HOperatorSet.ConcatObj(hObj, hObjFXA[0, 4], out hObj);

                            HOperatorSet.PhotometricStereo(hObj, out HeightField, out Gradient, out Albedo, Slants, Tilts, ResultType, "poisson", new HTuple(), new HTuple());
                            //     HOperatorSet.DerivateVectorField(Gradient, out MeanCurvature, sigma, "mean_curvature");

                            HOperatorSet.DerivateVectorField(Gradient, out MeanCurvature, cup.p_FX.sigma, "mean_curvature");
                            HOperatorSet.ScaleImage(MeanCurvature, out MeanCurvature, cup.p_FX.dMult, cup.p_FX.dAdd);

                            //     HOperatorSet.ScaleImage(MeanCurvature, out MeanCurvature, dMulti, dAdd);
                            HOperatorSet.ConvertImageType(MeanCurvature, out hObjFXA[0, 5], "byte");  //st  2023年2月28日  修改了光度图所在数组编号

                            ci8gAcqFX[0] = HObj2CogImage(hObjFXA[0, 5]);  //st  2023年2月28日  修改了光度图所在数组编号
                        }
                    }


                    while (bFXRun[1] || bFXRun[2] || bFXRun[3] || bFXRun[4])
                    {
                        Thread.Sleep(10);
                    }
                }
                

               // if (bRecord)
                {
               //     saveFileThread = new Thread(SaveImageForFXSurfaceAll);
               //     saveFileThread.Start();

                    //   SaveImageForFXSurfaceAll(bListProcess);
                }
                return true;
            }
            catch
            {
                return false;
            }

        }
        public bool FXSurfaceAllFromFile(int iFileInd, bool[] bListProcess,int wsInd)
        {
          
            int tWait = 5; 
            try
            {
                if (bListProcess[wsInd])
                {
                    int iStart = (Path.GetFileName(strFiles[wsInd][iFileInd])).IndexOf("20");
                    int iEnd = (Path.GetFileName(strFiles[wsInd][iFileInd])).IndexOf("_00");
                    if (iStart == 0)
                    {
                        iStart = cup.p_LI.strCamPrefix[wsInd].Length;
                    }
                    strTime = (Path.GetFileName(strFiles[wsInd][iFileInd])).Substring(iStart,14); 

                    HOperatorSet.ReadImage(out hObjFXA[wsInd, 0], strFiles[wsInd][iFileInd]); 
                    ci8gAcq[wsInd] = HObj2CogImage(hObjFXA[wsInd, 0]);
                    Thread.Sleep(tWait);
                } 
                 
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }

        }
        public bool FXSurfaceAllFromFile(int iFileInd, bool[] bListProcess,bool bFXRestruct = false)
        {
          //  bool bRecord = false;
            int tWait = 5;
         //   HObject objTmp;
            int ind = 0;
            try
            {
                csd.ExeProcessAll(0);

                //读取全光图
                for (int i = 0; i < maxCameraNum; i++)
                {
                    if (i == 0)
                    {
                        ind = iFileInd * 6;
                        ind = iFileInd * 6;
                        int iStart = (Path.GetFileName(strFiles[i][ind])).IndexOf(cup.p_LI.strCamPrefix[i])+5;
                        int iEnd = (Path.GetFileName(strFiles[i][ind])).IndexOf("_00");
                        if (iStart == 0)
                        {
                            iStart = cup.p_LI.strCamPrefix[i].Length;
                        }

                        strTime = (Path.GetFileName(strFiles[i][ind])).Substring(iStart, iEnd - iStart);
                    }
                    else
                        ind = iFileInd * cmAcqNum;
                    if (bListProcess[i])
                    {
                        //   string strPathDir = strDataPath + strSub[i];
                        //   strFiles[i]= Directory.GetFiles(strPathDir, "*.jpg");
                        HOperatorSet.ReadImage(out hObjFXA[i, 0], strFiles[i][ind]);

                        //    btSourceImg[i] = HObj2Bytes(hObjFXA[i, 0]);  //st 2022年12月22日

                        ci8gAcq[i] = HObj2CogImage(hObjFXA[i, 0]);
                        Thread.Sleep(tWait);
                    }
                }
                //     CloseAllExposureAuto(); //关闭自动曝光 

                if (cup.p_LS.bExe[1])
                {
                    csd.ExeProcessAll(1);

                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (i == 0)
                            ind = iFileInd * 6 + 1;
                        else
                            ind = iFileInd * cmAcqNum + 1;
                        if (bListProcess[i])
                        {
                            HOperatorSet.ReadImage(out hObjFXA[i, 1], strFiles[i][ind]);

                            Thread.Sleep(tWait);
                        }
                    }
                }

                if (cup.p_LS.bExe[2])
                {
                    csd.ExeProcessAll(2);

                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (i == 0)
                            ind = iFileInd * 6 + 2;
                        else
                            ind = iFileInd * cmAcqNum + 2;
                        if (bListProcess[i])
                        {
                            HOperatorSet.ReadImage(out hObjFXA[i, 2], strFiles[i][ind]);
                            // hObjFXA[i, 2] = GrabOneFrameHObj(cup.p_CD.camInd[i]);
                            Thread.Sleep(tWait);
                        }
                    }
                }


                if (cup.p_LS.bExe[3])
                {
                    csd.ExeProcessAll(3);

                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (i == 0)
                            ind = iFileInd * 6 + 3;
                        else
                            ind = iFileInd * cmAcqNum + 3;
                        if (bListProcess[i])
                        {
                            HOperatorSet.ReadImage(out hObjFXA[i, 3], strFiles[i][ind]);
                            //   hObjFXA[i, 3] = GrabOneFrameHObj(cup.p_CD.camInd[i]);
                            Thread.Sleep(tWait);
                        }
                    }
                }

                if (cup.p_LS.bExe[4])
                {
                    csd.ExeProcessAll(4);
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (i == 0)
                            ind = iFileInd * 6 + 4;
                        else
                            ind = iFileInd * cmAcqNum + 4 % (cmAcqNum - 1);

                        if (bListProcess[i])
                        {
                            HOperatorSet.ReadImage(out hObjFXA[i, 4], strFiles[i][ind]);
                            //    hObjFXA[i, 4] = GrabOneFrameHObj(cup.p_CD.camInd[i]);
                            Thread.Sleep(tWait);
                        }
                    }
                }

                csd.ExeProcessAll(5);

                OpenAllExposureAuto(); //打开自动曝光


                if (bFXRestruct)
                {
                    //st 2022年12月3日通过线程同步执行光度重构操作
                    for (int i = 1; i < maxCameraNum; i++)
                    {
                        if (bListProcess[i])
                        {
                            bFXRun[i] = true;
                            FXConstructThread[i] = new Thread(new ParameterizedThreadStart(FXConstruct));
                            FXConstructThread[i].Start((object)i);
                        }
                    }

                    //底面光度重构
                    if (bListProcess[0])
                    {
                        if (hObjFXA[0, 1] != null && hObjFXA[0, 2] != null && hObjFXA[0, 3] != null && hObjFXA[0, 4] != null)
                        {
                            HOperatorSet.ConcatObj(hObjFXA[0, 1], hObjFXA[0, 2], out hObj);
                            HOperatorSet.ConcatObj(hObj, hObjFXA[0, 3], out hObj);
                            HOperatorSet.ConcatObj(hObj, hObjFXA[0, 4], out hObj);

                            HOperatorSet.PhotometricStereo(hObj, out HeightField, out Gradient, out Albedo, Slants, Tilts, ResultType, "poisson", new HTuple(), new HTuple());
                            //     HOperatorSet.DerivateVectorField(Gradient, out MeanCurvature, sigma, "mean_curvature");

                            HOperatorSet.DerivateVectorField(Gradient, out MeanCurvature, cup.p_FX.sigma, "mean_curvature");
                            HOperatorSet.ScaleImage(MeanCurvature, out MeanCurvature, cup.p_FX.dMult, cup.p_FX.dAdd);

                            //     HOperatorSet.ScaleImage(MeanCurvature, out MeanCurvature, dMulti, dAdd);
                            HOperatorSet.ConvertImageType(MeanCurvature, out hObjFXA[0, 5], "byte");  //st  2023年2月28日  修改了光度图所在数组编号

                            ci8gAcqFX[0] = HObj2CogImage(hObjFXA[0, 5]);  //st  2023年2月28日  修改了光度图所在数组编号
                        }
                    }


                    while (bFXRun[1] || bFXRun[2] || bFXRun[3] || bFXRun[4])
                    {
                        Thread.Sleep(10);
                    }
                }
                else
                {
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (i == 0)
                            ind = iFileInd * 6 + 5;
                        else
                            ind = iFileInd * cmAcqNum + 5 ;

                        if (bListProcess[i])
                        {
                            HOperatorSet.ReadImage(out hObjFXA[i, 5], strFiles[i][ind]);
                            ci8gAcqFX[i] = HObj2CogImage(hObjFXA[i, 5]);
                            //    hObjFXA[i, 4] = GrabOneFrameHObj(cup.p_CD.camInd[i]);
                            Thread.Sleep(tWait);
                        }
                    }
                }

                // if (bRecord)
                {
                    //     saveFileThread = new Thread(SaveImageForFXSurfaceAll);
                    //     saveFileThread.Start();

                    //   SaveImageForFXSurfaceAll(bListProcess);
                }
                return true;
            }
            catch
            {
                return false;
            }

        }
        public CogImage8Grey FXSurface(int iCamInd,bool bRecord)
        {
            string strTime = DateTime.Now.ToString("HHmmss");
            string strImageFile;// = strSigMain + "\\" + strTime + ".jpg";
             
            try
            { 
                csd.ExeProcess(0);
                hObjFX[0] = GrabOneFrameHObj(iCamInd);

                if (bRecord)
                {
                    strImageFile = strConSavePathS[0] + "\\" + cup.p_LI.strPrefix[0]+ strTime + "_00";
                    HOperatorSet.WriteImage(hObjFX[0], "jpg", 0, strImageFile);
                } 
                csd.ExeProcess(1);
                hObjFX[1] = GrabOneFrameHObj(iCamInd);

                if (bRecord)
                {
                    strImageFile = strConSavePathS[0] + "\\" + cup.p_LI.strPrefix[0] + strTime + "_01";
                    HOperatorSet.WriteImage(hObjFX[1], "jpg", 0, strImageFile);
                }

                csd.ExeProcess(2);
                hObjFX[2] = GrabOneFrameHObj(iCamInd);
                if (bRecord)
                {
                    strImageFile = strConSavePathS[0] + "\\" + cup.p_LI.strPrefix[0] + strTime + "_02";
                    HOperatorSet.WriteImage(hObjFX[2], "jpg", 0, strImageFile);
                }
                csd.ExeProcess(3);
                hObjFX[3] = GrabOneFrameHObj(iCamInd);
                if (bRecord)
                {
                    strImageFile = strConSavePathS[0] + "\\" + cup.p_LI.strPrefix[0] + strTime + "_03";
                    HOperatorSet.WriteImage(hObjFX[3], "jpg", 0, strImageFile);
                }
                csd.ExeProcess(4);
                hObjFX[4] = GrabOneFrameHObj(iCamInd);
                if (bRecord)
                {
                    strImageFile = strConSavePathS[0] + "\\" + cup.p_LI.strPrefix[0] + strTime + "_04";
                    HOperatorSet.WriteImage(hObjFX[4], "jpg", 0, strImageFile);
                }
                csd.ExeProcess(5);

                if (hObjFX[1] == null || hObjFX[2] == null || hObjFX[3] == null || hObjFX[4] == null)
                    return null;
                HOperatorSet.ConcatObj(hObjFX[1], hObjFX[2], out hObj);
                HOperatorSet.ConcatObj(hObj, hObjFX[3], out hObj);
                HOperatorSet.ConcatObj(hObj, hObjFX[4], out hObj);

                HOperatorSet.PhotometricStereo(hObj, out HeightField, out Gradient, out Albedo, Slants, Tilts, ResultType, "poisson", new HTuple(), new HTuple());
                HOperatorSet.DerivateVectorField(Gradient, out MeanCurvature, sigma, "mean_curvature");
                HOperatorSet.ScaleImage(MeanCurvature, out MeanCurvature, dMulti, dAdd);
                HOperatorSet.ConvertImageType(MeanCurvature, out MeanCurvature, "byte");

               // csd.turnOFFAllNew();

                if (bRecord)
                {
                    strImageFile = strConSavePathS[0] + "\\" + cup.p_LI.strPrefix[0] + strTime + "_FX";
                    HOperatorSet.WriteImage(MeanCurvature, "jpg", 0, strImageFile);
                }
                return HObj2CogImage(MeanCurvature);
                /*  HObject2Bpp8(MeanCurvature, out res);
                  ci8gDM = new CogImage8Grey(res);
                  return ci8gDM;*/
            }
            catch
            {
                return null;
            }
          
        }
        #endregion

        #region  直接采集和保存byte[]型图像数据
        //2026年1月8日 获取byte[]型数组
        public bool AcqImgByte(bool bRecord, bool[] bListProcess, int wsInd)
        {

            int tWait = 5;
            try
            {
                if (bListProcess[wsInd])
                {
                    GrabOneFrameByte(cup.p_CD.camInd[wsInd]);
                    Thread.Sleep(tWait);

                    if (bRecord)
                    {
                        saveFileThread = new Thread(SaveImageFormSrcImage);
                        saveFileThread.Start();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }

        }
        //采集图像，byte[]型数据
        public bool GrabOneFrameByte(int iCamInd)
        {

            try
            {
                if (iCamType == 0)
                {
                    nRet = camera2[iCamInd].MV_CC_GetOneFrameTimeout_NET(m_BufForDriverS[iCamInd], nPayloadSizeS[iCamInd], ref stFrameInfo, 10000); // m_BufForDriver为图像数据接收指针

                    if (nRet == MyCamera.MV_OK)
                    {
                        IntPtrToByteArray(m_BufForDriverS[iCamInd], (int)nPayloadSizeS[iCamInd], bImgSrcData[iCamInd]);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    nRet = camera2[iCamInd].MV_CC_GetImageBuffer_NET(ref stFrameInfo1, 1000);
                    if (nRet == MyCamera.MV_OK)
                    {
                        IntPtrToByteArray(stFrameInfo1.pBufAddr, (int)nPayloadSizeS[iCamInd], bImgSrcData[iCamInd]);
                    }
                    else
                    {
                        return false;
                    }

                }
            }
            catch
            {
                return false;
            }
            return true;
            // PtrToObj(matImage.Ptr(), cup.p_HikPara.iPixelType);

        }

        public bool IntPtrToByteArray(IntPtr ptr, int length, byte[] byteArray)
        {
            if (ptr == IntPtr.Zero || length <= 0)
            {
                return false;
            }
            if (byteArray == null || byteArray.Length != length)
                byteArray = new byte[length];
            Marshal.Copy(ptr, byteArray, 0, length);
            return true;
        }

        public void SaveImageFormSrcImage()
        {

            strTime = DateTime.Now.ToString("yyyyMMddHHmmssFF");   //st 2023年1月25日  增加日期，以免名称冲突
            string strImageFile;


            for (int i = 0; i < maxCameraNum; i++)
            {
                strImageFile = strConSavePathS[i] + "\\" + cup.p_LI.strPrefix[i] + strTime + "_00" + ".jpg";
                if (cup.p_CD.bListProcess[i] && bImgSrcData[i] != null)
                {
                    SaveByteArrayAsJpg_OpenCV(bImgSrcData[i], stFrameInfo.nWidth, stFrameInfo.nHeight, 1, strImageFile);
                    //  HOperatorSet.WriteImage(hObjFXA[i, j], "jpg", 0, strImageFile);
                }

            }
            //  return true;
        }

        // 方法1：已知图像尺寸和通道数
        public bool SaveByteArrayAsJpg_OpenCV(byte[] imageData, int width, int height, int channels, string outputPath, int quality = 95)
        {
            try
            {
                // 确定Mat类型
                MatType matType = GetMatType(channels);

                // 创建Mat对象
                using (Mat mat = new Mat(height, width, matType))
                {
                    // 将byte数组复制到Mat的Data指针
                    Marshal.Copy(imageData, 0, mat.Data, imageData.Length);

                    // 设置JPEG压缩参数（0-100，越高质量越好）
                    int[] qualityParams = { (int)ImwriteFlags.JpegQuality, quality };

                    // 保存为JPG
                    return Cv2.ImWrite(outputPath, mat, qualityParams);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存图像失败: {ex.Message}");
                return false;
            }
        }

        // 根据通道数确定Mat类型
        private MatType GetMatType(int channels)
        {
            switch (channels)
            {
                case 1:
                    return MatType.CV_8UC1;  // 灰度
                case 3:
                    return MatType.CV_8UC3;  // BGR彩色
                case 4:
                    return MatType.CV_8UC4;  // BGRA带透明度
                default:
                    throw new ArgumentException($"不支持的通道数: {channels}");
            }
        }
        #endregion
    }
}
