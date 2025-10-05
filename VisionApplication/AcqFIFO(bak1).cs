using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using MvCamCtrl.NET;  // 海康相机
using HalconDotNet;
using Cognex.VisionPro;
//using Cognex.VisionPro.ToolBlock;
//using Cognex.VisionPro.ImageFile;
//using Cognex.VisionPro3D;
using Cognex.VisionPro.Implementation;
using Cognex.VisionPro.ImageFile;

namespace CommonCls
{
    public class ImgAcqFIFO
    {
        MyCamera[] camera2 = new MyCamera[maxCamNum];
        CameraStatus cs;
        int iCamType = 0;
        int nRet = 0;
        MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();  // 定义输出帧信息结构体
        MyCamera.MV_FRAME_OUT stFrameInfo1 = new MyCamera.MV_FRAME_OUT();
        IntPtr m_BufForDriver;
        UInt32 nPayloadSize = 0;

       public IntPtr[] m_BufForDriverS=new IntPtr[maxCamNum];
        UInt32[] nPayloadSizeS =new UInt32[maxCamNum];


       public CSerialDef csd = new CSerialDef();
        CUserParam cup = new CUserParam();
        //

        HObject   hObj1, hObj2, hObj3, hObj4, hObj;
        HTuple Slants, Tilts, ResultType,sigma,dMulti,dAdd;
        HTuple Slants1, Tilts1, Slants2, Tilts2;
        HTuple sub1sigma, sub1dMulti, sub1dAdd;
        HTuple sub2sigma, sub2dMulti, sub2dAdd;

        HObject HeightField, Gradient, Albedo, MeanCurvature;
         HObject MeanCurvatureCM1, MeanCurvatureCM2;
      //  Bitmap res;
      //  CogImage8Grey ci8gDM, ci8gCM1, ci8gCM2;
        HObject[] hObjFX = new HObject[6];
        HObject[] hObjFXCM1 = new HObject[6];
        HObject[] hObjFXCM2 = new HObject[6];
        HObject[] hObjFXCM3 = new HObject[6];
        HObject[] hObjFXCM4 = new HObject[6];

        const int maxCamNum = 5;
        //三个通道的灰度图
       public CogImage8Grey[] ci8gAcq = new CogImage8Grey[maxCamNum];
        //三个通道的光度图
       public CogImage8Grey[] ci8gAcqFX = new CogImage8Grey[maxCamNum];


        CogImageFile mImageFile = new CogImageFile();

        public string strSigMain, strSigSub1, strSigSub2;
        public string strConMain, strConSub1, strConSub2;

        public ImgAcqFIFO()
        {

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

        public void turnONAll()
        {
            csd.turnONAllNew();

            csd.turnONAllNew2();
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
        public void SetBuff(IntPtr _Buff, UInt32 _PayloadSize,int iCamIndex)
        {
            m_BufForDriverS[iCamIndex] = _Buff;
            nPayloadSizeS[iCamIndex] = _PayloadSize;
        }
        public void SetBuff(IntPtr[] _Buff, UInt32[] _PayloadSize)
        {
            m_BufForDriverS = _Buff;
            nPayloadSizeS = _PayloadSize;
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
        public HObject GrabOneFrameHObj(int iCamInd)
        {
            HImage image = new HImage();
            try
            {
                if (iCamType == 0)
                {
                    nRet = camera2[iCamInd].MV_CC_GetOneFrameTimeout_NET(m_BufForDriverS[iCamInd], nPayloadSizeS[iCamInd], ref stFrameInfo, 10000); // m_BufForDriver为图像数据接收指针


                    if (nRet == MyCamera.MV_OK)
                    {
                        image.GenImage1("byte", (int)stFrameInfo.nWidth, (int)stFrameInfo.nHeight, m_BufForDriverS[iCamInd]);

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

        public CogImage8Grey GrabOneFrameCog(int iCamInd)
        {
            if (cs.iStatus[iCamInd] < 2)
                return null;
                CogImage8Grey cogImage8Grey = new CogImage8Grey(); 
            if (iCamType == 0)
            {
                nRet = camera2[iCamInd].MV_CC_GetOneFrameTimeout_NET(m_BufForDriverS[iCamInd], nPayloadSizeS[iCamInd], ref stFrameInfo, 10000); // m_BufForDriver为图像数据接收指针
             
                if (nRet == MyCamera.MV_OK)
                {

                    CogImage8Root cogImage8Root = new CogImage8Root();
                    cogImage8Root.Initialize((Int32)stFrameInfo.nWidth, (Int32)stFrameInfo.nHeight, m_BufForDriverS[iCamInd], (Int32)stFrameInfo.nWidth, null);

                  //  CogImage8Grey cogImage8Grey = new CogImage8Grey();
                    cogImage8Grey.SetRoot(cogImage8Root);
 

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
            return cogImage8Grey.Copy();   //st务必添加copy
            // PtrToObj(matImage.Ptr(), cup.p_HikPara.iPixelType);

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
            return cogImage8Grey;
        }
        public void InitFX()
        {
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
                sub1dMulti =1460;
                sub1dAdd = 140;

                sub2sigma = 2;
                sub2dMulti = 1460;
                sub2dAdd = 140;
            } 

        }

        [DllImport("kernel32.dll")]
        public static extern void CopyMemory(int Destination, int add, int Length);
       
 

        public void SaveImage_ci8gAcq( )
        {
            string strTime = DateTime.Now.ToString("HHmmss");
            string strImageFile;
            if (ci8gAcq[0] != null)
            {
                strImageFile = strSigMain + "\\" + strTime + "_00.jpg";
                mImageFile.Open(strImageFile, CogImageFileModeConstants.Write);
                mImageFile.Append(ci8gAcq[0]);
                mImageFile.Close();
            }

            if (ci8gAcq[1] != null)
            {
                strImageFile = strSigSub1 + "\\" + strTime + "_00.jpg";
                mImageFile.Open(strImageFile, CogImageFileModeConstants.Write);
                mImageFile.Append(ci8gAcq[1]);
                mImageFile.Close();
            }

            if (ci8gAcq[2] != null)
            {
                strImageFile = strSigSub2 + "\\" + strTime + "_00.jpg";
                mImageFile.Open(strImageFile, CogImageFileModeConstants.Write);
                mImageFile.Append(ci8gAcq[2]);
                mImageFile.Close();
            }
        }


         
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
            for (int iCamInd = 0; iCamInd < 3; iCamInd++)
            {
                camera2[iCamInd].MV_CC_SetEnumValue_NET("ExposureAuto",0);
                Thread.Sleep(2);

            }
        }
        //全部打开自动曝光
        public void OpenAllExposureAuto(int CamNum)
        {
            for(int iCamInd = 0; iCamInd < CamNum; iCamInd++)
            {
                camera2[iCamInd].MV_CC_SetEnumValue_NET("ExposureAuto", 2);
                Thread.Sleep(2);
            }
        }
        public void OpenAllExposureAuto()
        {
            for (int iCamInd = 0; iCamInd < camera2.Length; iCamInd++)
            {
                if(camera2[iCamInd]!=null)
                    camera2[iCamInd].MV_CC_SetEnumValue_NET("ExposureAuto", 2);
                Thread.Sleep(2);
            }
        }
        public bool FXSurfaceAll(int CamNum, bool bRecord)
        {
            string strTime = DateTime.Now.ToString("HHmmss");
            string strImageFile = strSigMain;//= strSigMain + "\\" + strTime + ".jpg";
            int tWait = 5;

            try
            {
                csd.ExeProcessAll(0);

                for(int i=0;i<CamNum;i++)
                {
                    // if(cs.iStatus[cup.p_CD.camInd[0]]>1) 
                    ci8gAcq[i] = GrabOneFrameCog(cup.p_CD.camInd[i]);
                    Thread.Sleep(tWait);
                } 

                CloseAllExposureAuto(CamNum); //关闭自动曝光

                // cup.p_Cam[0].
                if (bRecord)
                {
                    for (int i = 0; i < CamNum; i++)
                    {
                        if (ci8gAcq[i] != null)
                        {
                            if(i==0)
                                strImageFile = strSigMain + "\\" + strTime + "_00.jpg";
                            else if (i == 1)
                                strImageFile = strSigSub1 + "\\" + strTime + "_00.jpg";
                            else if (i == 2)
                                strImageFile = strSigSub1 + "\\" + strTime + "_00.jpg";
                            mImageFile.Open(strImageFile, CogImageFileModeConstants.Write);
                            mImageFile.Append(ci8gAcq[i]);
                            mImageFile.Close();
                        }
                    } 
                }


                csd.ExeProcessAll(1); 
                //  if (cs.iStatus[cup.p_CD.camInd[0]] > 1)
                hObjFX[1] = GrabOneFrameHObj(cup.p_CD.camInd[0]);
                Thread.Sleep(tWait);
                if (CamNum > 1)
                {
                    hObjFXCM1[3] = GrabOneFrameHObj(cup.p_CD.camInd[1]);
                    Thread.Sleep(tWait);
                }
                if (CamNum > 2)
                {
                    hObjFXCM2[2] = GrabOneFrameHObj(cup.p_CD.camInd[2]);
                    Thread.Sleep(tWait);
                }

                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_01";
                    if (hObjFX[1] != null)
                        HOperatorSet.WriteImage(hObjFX[1], "jpg", 0, strImageFile);
                    if (CamNum > 1)
                    {
                        strImageFile = strSigSub1 + "\\" + strTime + "_03";
                        if (hObjFXCM1[3] != null)
                            HOperatorSet.WriteImage(hObjFXCM1[3], "jpg", 0, strImageFile);
                    }
                    if (CamNum >2)
                    {
                        strImageFile = strSigSub2 + "\\" + strTime + "_02";
                        if (hObjFXCM2[2] != null)
                            HOperatorSet.WriteImage(hObjFXCM2[2], "jpg", 0, strImageFile);
                    }
                }

                csd.ExeProcessAll(2);
                hObjFX[2] = GrabOneFrameHObj(cup.p_CD.camInd[0]);
                Thread.Sleep(tWait);
               
                if (CamNum > 1)
                {
                    hObjFXCM1[1] = GrabOneFrameHObj(cup.p_CD.camInd[1]);
                    // hObjFXCM2[1] = GrabOneFrameHObj(cup.p_CD.camInd[2]);
                    Thread.Sleep(tWait);
                }
                //    SaveImage_ci8gAcq();  //st  测试


                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_02";
                    if (hObjFX[2] != null)
                        HOperatorSet.WriteImage(hObjFX[2], "jpg", 0, strImageFile);
                    if (CamNum > 1)
                    {
                        strImageFile = strSigSub1 + "\\" + strTime + "_01";
                        if (hObjFXCM1[1] != null)
                            HOperatorSet.WriteImage(hObjFXCM1[1], "jpg", 0, strImageFile);
                    }
                    // strImageFile = strSigSub2 + "\\" + strTime + "_01";
                    // if (hObjFXCM2[1] != null)
                    //      HOperatorSet.WriteImage(hObjFXCM2[1], "jpg", 0, strImageFile);
                }

                //   SaveImage_ci8gAcq();  //st  测试


                csd.ExeProcessAll(3);
                hObjFX[3] = GrabOneFrameHObj(cup.p_CD.camInd[0]);
                Thread.Sleep(tWait);
                if (CamNum > 1)
                {
                    hObjFXCM1[2] = GrabOneFrameHObj(cup.p_CD.camInd[1]);
                    Thread.Sleep(tWait);
                }
                if (CamNum > 2)
                {
                    hObjFXCM2[3] = GrabOneFrameHObj(cup.p_CD.camInd[2]);
                    Thread.Sleep(tWait);
                }

                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_03";
                    if (hObjFX[3] != null)
                        HOperatorSet.WriteImage(hObjFX[3], "jpg", 0, strImageFile);
                    if (CamNum > 1)
                    {
                        strImageFile = strSigSub1 + "\\" + strTime + "_02";
                        if (hObjFXCM1[2] != null)
                            HOperatorSet.WriteImage(hObjFXCM1[2], "jpg", 0, strImageFile);
                    }
                    if (CamNum > 2)
                    {
                        strImageFile = strSigSub2 + "\\" + strTime + "_03";
                        if (hObjFXCM2[3] != null)
                            HOperatorSet.WriteImage(hObjFXCM2[3], "jpg", 0, strImageFile);
                    }
                }

                //    SaveImage_ci8gAcq();  //st  测试

                csd.ExeProcessAll(4);
                hObjFX[4] = GrabOneFrameHObj(cup.p_CD.camInd[0]);
                Thread.Sleep(tWait);
                // hObjFXCM1[1] = GrabOneFrameHObj(cup.p_CD.camInd[1]);
                if (CamNum > 2)
                {
                    hObjFXCM2[1] = GrabOneFrameHObj(cup.p_CD.camInd[2]);
                    Thread.Sleep(tWait);
                }

                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_04";
                    if (hObjFX[4] != null)
                        HOperatorSet.WriteImage(hObjFX[4], "jpg", 0, strImageFile);
                    //   strImageFile = strSigSub1 + "\\" + strTime + "_01";
                    //   if (hObjFXCM1[1] != null)
                    //       HOperatorSet.WriteImage(hObjFXCM1[1], "jpg", 0, strImageFile);
                    if (CamNum > 2)
                    {
                        strImageFile = strSigSub2 + "\\" + strTime + "_01";
                        if (hObjFXCM2[1] != null)
                            HOperatorSet.WriteImage(hObjFXCM2[1], "jpg", 0, strImageFile);
                    }

                }

                //      SaveImage_ci8gAcq();  //st  测试


                csd.ExeProcessAll(5);

                OpenAllExposureAuto(CamNum); //打开自动曝光
                //底面光度重构
                if (hObjFX[1] != null && hObjFX[2] != null && hObjFX[3] != null && hObjFX[4] != null)
                {
                    HOperatorSet.ConcatObj(hObjFX[1], hObjFX[2], out hObj);
                    HOperatorSet.ConcatObj(hObj, hObjFX[3], out hObj);
                    HOperatorSet.ConcatObj(hObj, hObjFX[4], out hObj);

                    HOperatorSet.PhotometricStereo(hObj, out HeightField, out Gradient, out Albedo, Slants, Tilts, ResultType, "poisson", new HTuple(), new HTuple());
                    HOperatorSet.DerivateVectorField(Gradient, out MeanCurvature, sigma, "mean_curvature");
                    HOperatorSet.ScaleImage(MeanCurvature, out MeanCurvature, dMulti, dAdd);
                    HOperatorSet.ConvertImageType(MeanCurvature, out MeanCurvature, "byte");

                    ci8gAcqFX[0] = HObj2CogImage(MeanCurvature);
                }
                if (CamNum > 1)
                {
                    //侧面1
                    if (hObjFXCM1[1] != null && hObjFXCM1[2] != null && hObjFXCM1[3] != null)
                    {
                        HOperatorSet.ConcatObj(hObjFXCM1[1], hObjFXCM1[2], out hObj);
                        HOperatorSet.ConcatObj(hObj, hObjFXCM1[3], out hObj);
                        //   HOperatorSet.ConcatObj(hObj, hObjFXCM1[4], out hObj);

                        HOperatorSet.PhotometricStereo(hObj, out HeightField, out Gradient, out Albedo, Slants1, Tilts1, ResultType, "poisson", new HTuple(), new HTuple());
                        HOperatorSet.DerivateVectorField(Gradient, out MeanCurvatureCM1, sub1sigma, "mean_curvature");
                        HOperatorSet.ScaleImage(MeanCurvatureCM1, out MeanCurvatureCM1, sub1dMulti, sub1dAdd);
                        HOperatorSet.ConvertImageType(MeanCurvatureCM1, out MeanCurvatureCM1, "byte");
                        ci8gAcqFX[1] = HObj2CogImage(MeanCurvatureCM1);
                    }
                }
                if (CamNum > 2)
                {
                    //侧面2
                    if (hObjFXCM2[1] != null && hObjFXCM2[2] != null && hObjFXCM2[3] != null)
                    {
                        HOperatorSet.ConcatObj(hObjFXCM2[1], hObjFXCM2[2], out hObj);
                        HOperatorSet.ConcatObj(hObj, hObjFXCM2[3], out hObj);
                        //  HOperatorSet.ConcatObj(hObj, hObjFXCM2[4], out hObj);

                        HOperatorSet.PhotometricStereo(hObj, out HeightField, out Gradient, out Albedo, Slants2, Tilts2, ResultType, "poisson", new HTuple(), new HTuple());
                        HOperatorSet.DerivateVectorField(Gradient, out MeanCurvatureCM2, sub2sigma, "mean_curvature");
                        HOperatorSet.ScaleImage(MeanCurvatureCM2, out MeanCurvatureCM2, sub2dMulti, sub2dAdd);
                        HOperatorSet.ConvertImageType(MeanCurvatureCM2, out MeanCurvatureCM2, "byte");
                        ci8gAcqFX[2] = HObj2CogImage(MeanCurvatureCM2);
                    }
                }
                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_FX";
                    if (MeanCurvature != null)
                        HOperatorSet.WriteImage(MeanCurvature, "jpg", 0, strImageFile);
                    if (CamNum > 1)
                    {
                        strImageFile = strSigSub1 + "\\" + strTime + "_FX";
                        if (MeanCurvatureCM1 != null)
                            HOperatorSet.WriteImage(MeanCurvatureCM1, "jpg", 0, strImageFile);
                    }
                    if (CamNum > 2)
                    {
                        strImageFile = strSigSub2 + "\\" + strTime + "_FX";
                        if (MeanCurvatureCM2 != null)
                            HOperatorSet.WriteImage(MeanCurvatureCM2, "jpg", 0, strImageFile);
                    }
                }
                //  return HObj2CogImage(MeanCurvature);


                return true;
            }
            catch
            {
                return false;
            }

        }

        public bool FXSurfaceAll(bool bRecord,bool[] bListProcess)
        {
            string strTime = DateTime.Now.ToString("HHmmss");
            string strImageFile;//= strSigMain + "\\" + strTime + ".jpg";
            int tWait = 5;

            try
            {
                csd.ExeProcessAll(0);
                // if(cs.iStatus[cup.p_CD.camInd[0]]>1)
                ci8gAcq[0] = GrabOneFrameCog(cup.p_CD.camInd[0]);
                Thread.Sleep(tWait);
                //  if (cs.iStatus[cup.p_CD.camInd[1]] > 1)
                ci8gAcq[1] = GrabOneFrameCog(cup.p_CD.camInd[1]);
                Thread.Sleep(tWait);
                //   if (cs.iStatus[cup.p_CD.camInd[2]] > 1)
                ci8gAcq[2] = GrabOneFrameCog(cup.p_CD.camInd[2]);
                Thread.Sleep(tWait);

                CloseAllExposureAuto(); //关闭自动曝光

                // cup.p_Cam[0].
                if (bRecord)
                {
                    if (ci8gAcq[0] != null)
                    {
                        strImageFile = strSigMain + "\\" + strTime + "_00.jpg";
                        mImageFile.Open(strImageFile, CogImageFileModeConstants.Write);
                        mImageFile.Append(ci8gAcq[0]);
                        mImageFile.Close();
                    }

                    if (ci8gAcq[1] != null)
                    {
                        strImageFile = strSigSub1 + "\\" + strTime + "_00.jpg";
                        mImageFile.Open(strImageFile, CogImageFileModeConstants.Write);
                        mImageFile.Append(ci8gAcq[1]);
                        mImageFile.Close();
                    }

                    if (ci8gAcq[2] != null)
                    {
                        strImageFile = strSigSub2 + "\\" + strTime + "_00.jpg";
                        mImageFile.Open(strImageFile, CogImageFileModeConstants.Write);
                        mImageFile.Append(ci8gAcq[2]);
                        mImageFile.Close();
                    }

                }


                csd.ExeProcessAll(1);
                //  if (cs.iStatus[cup.p_CD.camInd[0]] > 1)
                hObjFX[1] = GrabOneFrameHObj(cup.p_CD.camInd[0]);
                Thread.Sleep(tWait);
                hObjFXCM1[3] = GrabOneFrameHObj(cup.p_CD.camInd[1]);
                Thread.Sleep(tWait);
                hObjFXCM2[2] = GrabOneFrameHObj(cup.p_CD.camInd[2]);
                Thread.Sleep(tWait);

                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_01";
                    if (hObjFX[1] != null)
                        HOperatorSet.WriteImage(hObjFX[1], "jpg", 0, strImageFile);
                    strImageFile = strSigSub1 + "\\" + strTime + "_03";
                    if (hObjFXCM1[3] != null)
                        HOperatorSet.WriteImage(hObjFXCM1[3], "jpg", 0, strImageFile);
                    strImageFile = strSigSub2 + "\\" + strTime + "_02";
                    if (hObjFXCM2[2] != null)
                        HOperatorSet.WriteImage(hObjFXCM2[2], "jpg", 0, strImageFile);
                }

                csd.ExeProcessAll(2);
                hObjFX[2] = GrabOneFrameHObj(cup.p_CD.camInd[0]);
                Thread.Sleep(tWait);
                hObjFXCM1[1] = GrabOneFrameHObj(cup.p_CD.camInd[1]);
                // hObjFXCM2[1] = GrabOneFrameHObj(cup.p_CD.camInd[2]);
                Thread.Sleep(tWait);
                //    SaveImage_ci8gAcq();  //st  测试


                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_02";
                    if (hObjFX[2] != null)
                        HOperatorSet.WriteImage(hObjFX[2], "jpg", 0, strImageFile);
                    strImageFile = strSigSub1 + "\\" + strTime + "_01";
                    if (hObjFXCM1[1] != null)
                        HOperatorSet.WriteImage(hObjFXCM1[1], "jpg", 0, strImageFile);
                    // strImageFile = strSigSub2 + "\\" + strTime + "_01";
                    // if (hObjFXCM2[1] != null)
                    //      HOperatorSet.WriteImage(hObjFXCM2[1], "jpg", 0, strImageFile);
                }

                //   SaveImage_ci8gAcq();  //st  测试


                csd.ExeProcessAll(3);
                hObjFX[3] = GrabOneFrameHObj(cup.p_CD.camInd[0]);
                Thread.Sleep(tWait);
                hObjFXCM1[2] = GrabOneFrameHObj(cup.p_CD.camInd[1]);
                Thread.Sleep(tWait);
                hObjFXCM2[3] = GrabOneFrameHObj(cup.p_CD.camInd[2]);
                Thread.Sleep(tWait);

                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_03";
                    if (hObjFX[3] != null)
                        HOperatorSet.WriteImage(hObjFX[3], "jpg", 0, strImageFile);
                    strImageFile = strSigSub1 + "\\" + strTime + "_02";
                    if (hObjFXCM1[2] != null)
                        HOperatorSet.WriteImage(hObjFXCM1[2], "jpg", 0, strImageFile);
                    strImageFile = strSigSub2 + "\\" + strTime + "_03";
                    if (hObjFXCM2[3] != null)
                        HOperatorSet.WriteImage(hObjFXCM2[3], "jpg", 0, strImageFile);
                }

                //    SaveImage_ci8gAcq();  //st  测试

                csd.ExeProcessAll(4);
                hObjFX[4] = GrabOneFrameHObj(cup.p_CD.camInd[0]);
                Thread.Sleep(tWait);
                // hObjFXCM1[1] = GrabOneFrameHObj(cup.p_CD.camInd[1]);
                hObjFXCM2[1] = GrabOneFrameHObj(cup.p_CD.camInd[2]);
                Thread.Sleep(tWait);

                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_04";
                    if (hObjFX[4] != null)
                        HOperatorSet.WriteImage(hObjFX[4], "jpg", 0, strImageFile);
                    //   strImageFile = strSigSub1 + "\\" + strTime + "_01";
                    //   if (hObjFXCM1[1] != null)
                    //       HOperatorSet.WriteImage(hObjFXCM1[1], "jpg", 0, strImageFile);
                    strImageFile = strSigSub2 + "\\" + strTime + "_01";
                    if (hObjFXCM2[1] != null)
                        HOperatorSet.WriteImage(hObjFXCM2[1], "jpg", 0, strImageFile);

                }

                //      SaveImage_ci8gAcq();  //st  测试


                csd.ExeProcessAll(5);

                OpenAllExposureAuto(); //打开自动曝光
                //底面光度重构
                if (hObjFX[1] != null && hObjFX[2] != null && hObjFX[3] != null && hObjFX[4] != null)
                {
                    HOperatorSet.ConcatObj(hObjFX[1], hObjFX[2], out hObj);
                    HOperatorSet.ConcatObj(hObj, hObjFX[3], out hObj);
                    HOperatorSet.ConcatObj(hObj, hObjFX[4], out hObj);

                    HOperatorSet.PhotometricStereo(hObj, out HeightField, out Gradient, out Albedo, Slants, Tilts, ResultType, "poisson", new HTuple(), new HTuple());
                    HOperatorSet.DerivateVectorField(Gradient, out MeanCurvature, sigma, "mean_curvature");
                    HOperatorSet.ScaleImage(MeanCurvature, out MeanCurvature, dMulti, dAdd);
                    HOperatorSet.ConvertImageType(MeanCurvature, out MeanCurvature, "byte");

                    ci8gAcqFX[0] = HObj2CogImage(MeanCurvature);
                }

                //侧面1
                if (hObjFXCM1[1] != null && hObjFXCM1[2] != null && hObjFXCM1[3] != null)
                {
                    HOperatorSet.ConcatObj(hObjFXCM1[1], hObjFXCM1[2], out hObj);
                    HOperatorSet.ConcatObj(hObj, hObjFXCM1[3], out hObj);
                    //   HOperatorSet.ConcatObj(hObj, hObjFXCM1[4], out hObj);

                    HOperatorSet.PhotometricStereo(hObj, out HeightField, out Gradient, out Albedo, Slants1, Tilts1, ResultType, "poisson", new HTuple(), new HTuple());
                    HOperatorSet.DerivateVectorField(Gradient, out MeanCurvatureCM1, sub1sigma, "mean_curvature");
                    HOperatorSet.ScaleImage(MeanCurvatureCM1, out MeanCurvatureCM1, sub1dMulti, sub1dAdd);
                    HOperatorSet.ConvertImageType(MeanCurvatureCM1, out MeanCurvatureCM1, "byte");
                    ci8gAcqFX[1] = HObj2CogImage(MeanCurvatureCM1);
                }
                //侧面2
                if (hObjFXCM2[1] != null && hObjFXCM2[2] != null && hObjFXCM2[3] != null)
                {
                    HOperatorSet.ConcatObj(hObjFXCM2[1], hObjFXCM2[2], out hObj);
                    HOperatorSet.ConcatObj(hObj, hObjFXCM2[3], out hObj);
                    //  HOperatorSet.ConcatObj(hObj, hObjFXCM2[4], out hObj);

                    HOperatorSet.PhotometricStereo(hObj, out HeightField, out Gradient, out Albedo, Slants2, Tilts2, ResultType, "poisson", new HTuple(), new HTuple());
                    HOperatorSet.DerivateVectorField(Gradient, out MeanCurvatureCM2, sub2sigma, "mean_curvature");
                    HOperatorSet.ScaleImage(MeanCurvatureCM2, out MeanCurvatureCM2, sub2dMulti, sub2dAdd);
                    HOperatorSet.ConvertImageType(MeanCurvatureCM2, out MeanCurvatureCM2, "byte");
                    ci8gAcqFX[2] = HObj2CogImage(MeanCurvatureCM2);
                }
                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_FX";
                    if (MeanCurvature != null)
                        HOperatorSet.WriteImage(MeanCurvature, "jpg", 0, strImageFile);

                    strImageFile = strSigSub1 + "\\" + strTime + "_FX";
                    if (MeanCurvatureCM1 != null)
                        HOperatorSet.WriteImage(MeanCurvatureCM1, "jpg", 0, strImageFile);

                    strImageFile = strSigSub2 + "\\" + strTime + "_FX";
                    if (MeanCurvatureCM2 != null)
                        HOperatorSet.WriteImage(MeanCurvatureCM2, "jpg", 0, strImageFile);
                }
                //  return HObj2CogImage(MeanCurvature);


                return true;
            }
            catch
            {
                return false;
            }

        }

        public bool FXSurfaceAll(  bool bRecord)
        {
            string strTime = DateTime.Now.ToString("HHmmss");
            string strImageFile;//= strSigMain + "\\" + strTime + ".jpg";
            int tWait = 5;
            
            try
            {
                csd.ExeProcessAll(0);
                // if(cs.iStatus[cup.p_CD.camInd[0]]>1)
                ci8gAcq[0] = GrabOneFrameCog(cup.p_CD.camInd[0]);
                Thread.Sleep(tWait);
                //  if (cs.iStatus[cup.p_CD.camInd[1]] > 1)
                ci8gAcq[1] = GrabOneFrameCog(cup.p_CD.camInd[1]);
                Thread.Sleep(tWait);
                //   if (cs.iStatus[cup.p_CD.camInd[2]] > 1)
                ci8gAcq[2] = GrabOneFrameCog(cup.p_CD.camInd[2]);
                Thread.Sleep(tWait);

                CloseAllExposureAuto(); //关闭自动曝光

                // cup.p_Cam[0].
                if (bRecord)
                {
                    if (ci8gAcq[0] != null)
                    {
                        strImageFile = strSigMain + "\\" + strTime + "_00.jpg";
                        mImageFile.Open(strImageFile, CogImageFileModeConstants.Write);
                        mImageFile.Append(ci8gAcq[0]);
                        mImageFile.Close();
                    }

                    if (ci8gAcq[1] != null)
                    {
                        strImageFile = strSigSub1 + "\\" + strTime + "_00.jpg";
                        mImageFile.Open(strImageFile, CogImageFileModeConstants.Write);
                        mImageFile.Append(ci8gAcq[1]);
                        mImageFile.Close();
                    }

                    if (ci8gAcq[2] != null)
                    {
                        strImageFile = strSigSub2 + "\\" + strTime + "_00.jpg";
                        mImageFile.Open(strImageFile, CogImageFileModeConstants.Write);
                        mImageFile.Append(ci8gAcq[2]);
                        mImageFile.Close();
                    }

                }


                csd.ExeProcessAll(1);
                //  if (cs.iStatus[cup.p_CD.camInd[0]] > 1)
                hObjFX[1] = GrabOneFrameHObj(cup.p_CD.camInd[0]);
                Thread.Sleep(tWait);
                hObjFXCM1[3] = GrabOneFrameHObj(cup.p_CD.camInd[1]);
                Thread.Sleep(tWait);
                hObjFXCM2[2] = GrabOneFrameHObj(cup.p_CD.camInd[2]);
                Thread.Sleep(tWait);

                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_01";
                    if (hObjFX[1] != null)
                        HOperatorSet.WriteImage(hObjFX[1], "jpg", 0, strImageFile);
                    strImageFile = strSigSub1 + "\\" + strTime + "_03";
                    if (hObjFXCM1[3] != null)
                        HOperatorSet.WriteImage(hObjFXCM1[3], "jpg", 0, strImageFile);
                    strImageFile = strSigSub2 + "\\" + strTime + "_02";
                    if (hObjFXCM2[2] != null)
                        HOperatorSet.WriteImage(hObjFXCM2[2], "jpg", 0, strImageFile);
                }

                csd.ExeProcessAll(2);
                hObjFX[2] = GrabOneFrameHObj(cup.p_CD.camInd[0]);
                Thread.Sleep(tWait);
                hObjFXCM1[1] = GrabOneFrameHObj(cup.p_CD.camInd[1]);
                // hObjFXCM2[1] = GrabOneFrameHObj(cup.p_CD.camInd[2]);
                Thread.Sleep(tWait);
                //    SaveImage_ci8gAcq();  //st  测试


                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_02";
                    if (hObjFX[2] != null)
                        HOperatorSet.WriteImage(hObjFX[2], "jpg", 0, strImageFile);
                    strImageFile = strSigSub1 + "\\" + strTime + "_01";
                    if (hObjFXCM1[1] != null)
                        HOperatorSet.WriteImage(hObjFXCM1[1], "jpg", 0, strImageFile);
                   // strImageFile = strSigSub2 + "\\" + strTime + "_01";
                  // if (hObjFXCM2[1] != null)
                  //      HOperatorSet.WriteImage(hObjFXCM2[1], "jpg", 0, strImageFile);
                }

             //   SaveImage_ci8gAcq();  //st  测试


                csd.ExeProcessAll(3);
                hObjFX[3] = GrabOneFrameHObj(cup.p_CD.camInd[0]);
                Thread.Sleep(tWait);
                hObjFXCM1[2] = GrabOneFrameHObj(cup.p_CD.camInd[1]);
                Thread.Sleep(tWait);
                hObjFXCM2[3] = GrabOneFrameHObj(cup.p_CD.camInd[2]);
                Thread.Sleep(tWait);

                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_03";
                    if (hObjFX[3] != null)
                        HOperatorSet.WriteImage(hObjFX[3], "jpg", 0, strImageFile);
                    strImageFile = strSigSub1 + "\\" + strTime + "_02";
                    if (hObjFXCM1[2] != null)
                        HOperatorSet.WriteImage(hObjFXCM1[2], "jpg", 0, strImageFile);
                     strImageFile = strSigSub2 + "\\" + strTime + "_03";
                    if (hObjFXCM2[3] != null)
                        HOperatorSet.WriteImage(hObjFXCM2[3], "jpg", 0, strImageFile);
                }

            //    SaveImage_ci8gAcq();  //st  测试

                csd.ExeProcessAll(4);
                hObjFX[4] = GrabOneFrameHObj(cup.p_CD.camInd[0]);
                Thread.Sleep(tWait);
                // hObjFXCM1[1] = GrabOneFrameHObj(cup.p_CD.camInd[1]);
                hObjFXCM2[1] = GrabOneFrameHObj(cup.p_CD.camInd[2]);
                Thread.Sleep(tWait);

                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_04";
                    if (hObjFX[4] != null)
                        HOperatorSet.WriteImage(hObjFX[4], "jpg", 0, strImageFile);
                 //   strImageFile = strSigSub1 + "\\" + strTime + "_01";
                 //   if (hObjFXCM1[1] != null)
                 //       HOperatorSet.WriteImage(hObjFXCM1[1], "jpg", 0, strImageFile);
                    strImageFile = strSigSub2 + "\\" + strTime + "_01";
                    if (hObjFXCM2[1] != null)
                        HOperatorSet.WriteImage(hObjFXCM2[1], "jpg", 0, strImageFile);

                }

          //      SaveImage_ci8gAcq();  //st  测试


                csd.ExeProcessAll(5);

                OpenAllExposureAuto(); //打开自动曝光
                //底面光度重构
                if (hObjFX[1] != null && hObjFX[2] != null && hObjFX[3] != null && hObjFX[4] != null)
                {
                    HOperatorSet.ConcatObj(hObjFX[1], hObjFX[2], out hObj);
                    HOperatorSet.ConcatObj(hObj, hObjFX[3], out hObj);
                    HOperatorSet.ConcatObj(hObj, hObjFX[4], out hObj);

                    HOperatorSet.PhotometricStereo(hObj, out HeightField, out Gradient, out Albedo, Slants, Tilts, ResultType, "poisson", new HTuple(), new HTuple());
                    HOperatorSet.DerivateVectorField(Gradient, out MeanCurvature, sigma, "mean_curvature");
                    HOperatorSet.ScaleImage(MeanCurvature, out MeanCurvature, dMulti, dAdd);
                    HOperatorSet.ConvertImageType(MeanCurvature, out MeanCurvature, "byte");

                    ci8gAcqFX[0] = HObj2CogImage(MeanCurvature);
                }

                //侧面1
                if (hObjFXCM1[1] != null && hObjFXCM1[2] != null && hObjFXCM1[3] != null)
                {
                    HOperatorSet.ConcatObj(hObjFXCM1[1], hObjFXCM1[2], out hObj);
                    HOperatorSet.ConcatObj(hObj, hObjFXCM1[3], out hObj);
                    //   HOperatorSet.ConcatObj(hObj, hObjFXCM1[4], out hObj);

                    HOperatorSet.PhotometricStereo(hObj, out HeightField, out Gradient, out Albedo, Slants1, Tilts1, ResultType, "poisson", new HTuple(), new HTuple());
                    HOperatorSet.DerivateVectorField(Gradient, out MeanCurvatureCM1, sub1sigma, "mean_curvature");
                    HOperatorSet.ScaleImage(MeanCurvatureCM1, out MeanCurvatureCM1, sub1dMulti, sub1dAdd);
                    HOperatorSet.ConvertImageType(MeanCurvatureCM1, out MeanCurvatureCM1, "byte");
                    ci8gAcqFX[1] = HObj2CogImage(MeanCurvatureCM1);
                }
                //侧面2
                if (hObjFXCM2[1] != null && hObjFXCM2[2] != null && hObjFXCM2[3] != null)
                {
                    HOperatorSet.ConcatObj(hObjFXCM2[1], hObjFXCM2[2], out hObj);
                    HOperatorSet.ConcatObj(hObj, hObjFXCM2[3], out hObj);
                    //  HOperatorSet.ConcatObj(hObj, hObjFXCM2[4], out hObj);

                    HOperatorSet.PhotometricStereo(hObj, out HeightField, out Gradient, out Albedo, Slants2, Tilts2, ResultType, "poisson", new HTuple(), new HTuple());
                    HOperatorSet.DerivateVectorField(Gradient, out MeanCurvatureCM2, sub2sigma, "mean_curvature");
                    HOperatorSet.ScaleImage(MeanCurvatureCM2, out MeanCurvatureCM2, sub2dMulti, sub2dAdd);
                    HOperatorSet.ConvertImageType(MeanCurvatureCM2, out MeanCurvatureCM2, "byte");
                    ci8gAcqFX[2] = HObj2CogImage(MeanCurvatureCM2);
                }
                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_FX";
                    if(MeanCurvature!=null)
                        HOperatorSet.WriteImage(MeanCurvature, "jpg", 0, strImageFile);

                    strImageFile = strSigSub1 + "\\" + strTime + "_FX";
                    if (MeanCurvatureCM1 != null)
                        HOperatorSet.WriteImage(MeanCurvatureCM1, "jpg", 0, strImageFile);

                    strImageFile = strSigSub2 + "\\" + strTime + "_FX";
                    if (MeanCurvatureCM2 != null)
                        HOperatorSet.WriteImage(MeanCurvatureCM2, "jpg", 0, strImageFile);
                }
                //  return HObj2CogImage(MeanCurvature);


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
            string strImageFile = strSigMain + "\\" + strTime + ".jpg";
             
            try
            {

                csd.ExeProcess(0);
                hObjFX[0] = GrabOneFrameHObj(iCamInd);

                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_00";
                    HOperatorSet.WriteImage(hObjFX[0], "jpg", 0, strImageFile);
                }


                csd.ExeProcess(1);
                hObjFX[1] = GrabOneFrameHObj(iCamInd);

                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_01";
                    HOperatorSet.WriteImage(hObjFX[1], "jpg", 0, strImageFile);
                }

                csd.ExeProcess(2);
                hObjFX[2] = GrabOneFrameHObj(iCamInd);
                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_02";
                    HOperatorSet.WriteImage(hObjFX[2], "jpg", 0, strImageFile);
                }
                csd.ExeProcess(3);
                hObjFX[3] = GrabOneFrameHObj(iCamInd);
                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_03";
                    HOperatorSet.WriteImage(hObjFX[3], "jpg", 0, strImageFile);
                }
                csd.ExeProcess(4);
                hObjFX[4] = GrabOneFrameHObj(iCamInd);
                if (bRecord)
                {
                    strImageFile = strSigMain + "\\" + strTime + "_04";
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
                    strImageFile = strSigMain + "\\" + strTime + "_FX";
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
          //  HOperatorSet.DispObj(MeanCurvature, WindowHandleSub1);
        }
        public CogImage8Grey FXSurfaceFromImageFile(int iFileInd)
        {
            string strPath = @"F:\st\WorkSpace\项目\万华匣钵缺陷检测\data\光度立体法\光度立体数据\";

            try
            {
                csd.turnONAllNew();
                csd.turnONAllNew2();
                // hFull = GrabOneFrame(0);

                csd.turnOFFAllNew();
                csd.turnOFFAllNew2();

                //     csd.turnON(1);
                string strFileName = strPath + "01\\" + (iFileInd + 1).ToString() + "_01.jpg";
                HOperatorSet.ReadImage(out hObj1, strFileName);
                HOperatorSet.Rgb1ToGray(hObj1, out hObj1);

                Thread.Sleep(10);
                //     csd.turnOFF(1);
                Thread.Sleep(20);
                //   csd.turnON(2);
                HOperatorSet.ReadImage(out hObj2, strPath + "02\\" + (iFileInd + 1).ToString() + "_02.jpg");
                HOperatorSet.Rgb1ToGray(hObj2, out hObj2);

                Thread.Sleep(10);
                //   csd.turnOFF(2);
                Thread.Sleep(20);
                //   csd.turnON(3);
                HOperatorSet.ReadImage(out hObj3, strPath + "03\\" + (iFileInd + 1).ToString() + "_03.jpg");
                HOperatorSet.Rgb1ToGray(hObj3, out hObj3);

                Thread.Sleep(10);
                //   csd.turnOFF(3);
                Thread.Sleep(20);
                //   csd.turnON(4);
                HOperatorSet.ReadImage(out hObj4, strPath + "04\\" + (iFileInd + 1).ToString() + "_04.jpg");
                HOperatorSet.Rgb1ToGray(hObj4, out hObj4);

                Thread.Sleep(10);
                //   csd.turnOFF(4);
                Thread.Sleep(20);

                HOperatorSet.ConcatObj(hObj1, hObj2, out hObj);
                HOperatorSet.ConcatObj(hObj, hObj3, out hObj);
                HOperatorSet.ConcatObj(hObj, hObj4, out hObj);

                HOperatorSet.PhotometricStereo(hObj, out HeightField, out Gradient, out Albedo, Slants, Tilts, ResultType, "poisson", new HTuple(), new HTuple());
                HOperatorSet.DerivateVectorField(Gradient, out MeanCurvature, 3.0, "mean_curvature");
                HOperatorSet.ScaleImage(MeanCurvature, out MeanCurvature, 1460, 140);
                HOperatorSet.ConvertImageType(MeanCurvature, out MeanCurvature, "byte");

                //    HOperatorSet.GetImageSize(MeanCurvature, out width, out height);

              //  HOperatorSet.DispObj(MeanCurvature, WindowHandleSub1);
                return HObj2CogImage(MeanCurvature);

            }
            catch
            {
                return null;
            }

        }
    }
}
