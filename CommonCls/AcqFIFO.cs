using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Cognex.VisionPro;
using Cognex.VisionPro.ImageProcessing;
using Cognex.VisionPro.ImageFile;
using HalconDotNet;
using MvCamCtrl.NET;
using OpenCvSharp;
using OpenCvSharp.Extensions; // 引入此命名空间以使用 BitmapConverter
using System.Drawing.Imaging;   // PixelFormat 枚举
namespace CommonCls
{
    /// <summary>
    /// 图像采集 FIFO 类，支持多相机同步采集、光度立体法重构、图像格式转换及文件保存。
    /// </summary>
    public class ImgAcqFIFO
    {
        #region 常量与配置字段

        /// <summary>最大相机数量</summary>
        private const int maxCameraNum = 2;

        /// <summary>默认线程等待时间（毫秒）</summary>
        private const int DefaultWaitMs = 5;

        /// <summary>单帧采集超时时间（毫秒）</summary>
        private const int OneFrameTimeoutMs = 10000;

        /// <summary>每路相机采集图像数量（光度立体法6张，普通检测1张）</summary>
        public int cmAcqNum = 6;

        #endregion

        #region 相机相关字段

        /// <summary>单个摄像头（调试用）</summary>
        public MyCamera camera = new MyCamera();

        /// <summary>多通道摄像头组</summary>
        private MyCamera[] _cameras = new MyCamera[maxCameraNum];

        /// <summary>摄像头初始化状态（由外部主程序传入）</summary>
        private CameraStatus _cameraStatus;

        /// <summary>相机类型（0-海康GigE，其他-其他类型）
        /// 用户预先分配的非托管缓冲区（m_BufForDriverS[iCamInd]）
        /// SDK 内部管理的缓冲区（通过 stFrameInfoWithData.pBufAddr 返回）
        /// 
        /// </summary>
        private int _cameraType = 0;  //用户预先分配的非托管缓冲区

        /// <summary>旋转镜像类型（0-无，1-90°，2-180°，3-270°，4-翻转，5-翻转+90°等）</summary>
        private int _rotateFlipType = 0;

        /// <summary>海康SDK返回值</summary>
        private int nRet = 0;

        /// <summary>输出帧信息结构体</summary>
        public MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();

        /// <summary>输出帧信息结构体（含帧数据）</summary>
        public MyCamera.MV_FRAME_OUT stFrameInfoWithData = new MyCamera.MV_FRAME_OUT();

        /// <summary>每个相机的载荷大小（图像数据字节数）</summary>
        private UInt32[] nPayloadSizeS = new UInt32[maxCameraNum];
        private UInt32[] m_nBufSizeForDriver = new UInt32[maxCameraNum];
        /// <summary>每个相机的驱动缓冲区指针</summary>
        public IntPtr[] m_BufForDriverS = new IntPtr[maxCameraNum];

        /// <summary>相机像素格式字符串（如 "Mono8", "BayerRG8", "RGB8Packed"）</summary>
        public string[] strPixelFormat = new string[maxCameraNum];

        /// <summary>是否为彩色相机（根据像素格式自动判断）</summary>
        public bool[] bIsColorCam = new bool[maxCameraNum];

        #endregion


        #region
        // 新增公共方法：根据相机参数分配缓冲区
        public bool AllocateBuffers(CameraParam[] camParams, int[] camIndices, bool[] bListProcess)
        {
            for (int i = 0; i < maxCameraNum; i++)
            {
                if (!bListProcess[i]) continue;
                int idx = camIndices[i];
                uint needed = (uint)(camParams[idx].iWidth * camParams[idx].iHeight * (camParams[idx].iMonoType == 0 ? 1 : 3));
                if (needed > m_nBufSizeForDriver[idx])
                {
                    FreeBuffer(idx);                     // 释放旧内存
                    m_BufForDriverS[idx] = Marshal.AllocHGlobal((int)needed);
                    if (m_BufForDriverS[idx] == IntPtr.Zero)
                        return false;
                    m_nBufSizeForDriver[idx] = needed;
                }
                nPayloadSizeS[idx] = needed;
            }
            return true;
        }

        private void FreeBuffer(int camIndex)
        {
            if (m_BufForDriverS[camIndex] != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(m_BufForDriverS[camIndex]);
                m_BufForDriverS[camIndex] = IntPtr.Zero;
                m_nBufSizeForDriver[camIndex] = 0;
            }
        }

        public void FreeAllBuffers()
        {
            for (int i = 0; i < maxCameraNum; i++)
                FreeBuffer(i);
        }

        // 提供只读访问（供采集函数内部使用）
        public IntPtr GetBufferPtr(int camIndex) => m_BufForDriverS[camIndex];
        public UInt32 GetPayloadSize(int camIndex) => nPayloadSizeS[camIndex];
        // 在相机参数变化时重新分配单路缓冲区
        public bool ReallocateBuffer(int camIndex, uint newPayloadSize)
        {
            if (newPayloadSize > m_nBufSizeForDriver[camIndex])
            {
                FreeBuffer(camIndex);
                m_BufForDriverS[camIndex] = Marshal.AllocHGlobal((int)newPayloadSize);
                if (m_BufForDriverS[camIndex] == IntPtr.Zero)
                    return false;
                m_nBufSizeForDriver[camIndex] = newPayloadSize;
            }
            nPayloadSizeS[camIndex] = newPayloadSize;
            return true;
        }

        #endregion


        #region 图像数据字段
        /// <summary>将图像数据转为Cog格式用（CogImage8Grey格式）</summary>
        private CogImage8Grey cogImage8Grey = new CogImage8Grey();
        private CogImage8Root cogImage8Root = new CogImage8Root();
        
        /// <summary>多相机多方向图像数组 [相机索引, 图像索引] (0-4:多方向图,5:光度图)</summary>
        public HObject[,] hObjFXA = new HObject[maxCameraNum, 6];
         /// <summary>彩色图像（Halcon 三通道 HObject）</summary>
        public HObject[] hColorImg = new HObject[maxCameraNum];

        /// <summary>多通道的灰度图（全光图）</summary>
        public CogImage8Grey[] ci8gAcq = new CogImage8Grey[maxCameraNum]; 
        /// <summary>多通道的光度图</summary>
        public CogImage8Grey[] ci8gAcqFX = new CogImage8Grey[maxCameraNum];
         /// <summary>彩色图像（Cognex 24位平面彩色）</summary>
        public CogImage24PlanarColor[] ci24ColorAcq = new CogImage24PlanarColor[maxCameraNum];
        
        /// <summary>原始图像字节数据（用于深度学习等）</summary>
        public byte[][] bImgSrcData = new byte[maxCameraNum][];
         
       
         
        #endregion

        #region 光度立体法参数
        private HObject hObj;
        private HTuple Slants, Tilts, ResultType, sigma, dMulti, dAdd;
        private HTuple Slants1, Tilts1, Slants2, Tilts2; 
        private HObject HeightField, Gradient, Albedo, MeanCurvature;
        #endregion

        #region 文件路径与辅助对象
        /// <summary>信号图像保存路径数组</summary>
        public string[] strSigSavePathS = new string[maxCameraNum];

        /// <summary>连续采集保存路径数组</summary>
        public string[] strConSavePathS = new string[maxCameraNum];

        /// <summary>用户参数（配置）</summary>
        private CUserParam cup = new CUserParam();

        /// <summary>串口通信辅助类</summary>
        public CSerialDef csd = new CSerialDef();

        /// <summary>深度学习相关（注释）</summary>
        // public CDL cdl = new CDL();

        /// <summary>深度学习检测结果</summary>
        public List<CogRectangleAffine>[] AL_det_result = new List<CogRectangleAffine>[maxCameraNum];

        /// <summary>Cognex图像处理工具</summary>
      //  private CogIPOneImageTool imgTool = new CogIPOneImageTool();

        /// <summary>Cognex旋转镜像工具</summary>
      //  private CogIPOneImageFlipRotate cogImFlipRotate = new CogIPOneImageFlipRotate();
       
        /// <summary>Cognex格式图像保存工具</summary>
      //  CogImageFile mImageFile = new CogImageFile();
        private CogIPOneImageFlipRotate[] cogImFlipRotate;
        private CogIPOneImageTool[] imgTool;
        private CogImageFile[] mImageFile;
        /// <summary>光度立体重构线程数组</summary>
        private Thread[] FXConstructThread = new Thread[maxCameraNum];

        /// <summary>文件保存线程</summary>
        private Thread saveFileThread;

        /// <summary>标记各相机光度重构是否正在运行</summary>
        private bool[] bFXRun = new bool[maxCameraNum];

        /// <summary>时间戳字符串（用于文件名）</summary>
        public string strTime;

        /// <summary>从文件读取的图像路径数组</summary>
        public string[][] strFiles = new string[maxCameraNum][];   //按工位编号

        /// <summary>错误信息</summary>
        public string strErrInfo = "";

        /// <summary>文件时间戳（用于回放）</summary>
        public string strFileTime;
      
        #endregion

        #region 构造函数

        /// <summary>默认构造函数</summary>
        public ImgAcqFIFO()
        {// 初始化数组
            cogImFlipRotate = new CogIPOneImageFlipRotate[maxCameraNum];
            imgTool = new CogIPOneImageTool[maxCameraNum];
            mImageFile = new CogImageFile[maxCameraNum];

            for (int i = 0; i < maxCameraNum; i++)
            {
                cogImFlipRotate[i] = new CogIPOneImageFlipRotate();
                imgTool[i] = new CogIPOneImageTool();
                mImageFile[i] = new CogImageFile();
            }
        }
        #endregion

        #region 属性
        /// <summary>旋转镜像类型赋值与读取</summary>
        public int _rtType
        {
            get { return _rotateFlipType; }
            set { _rotateFlipType = value; }
        }

        /// <summary>相机状态赋值与读取</summary>
        public CameraStatus _cs
        {
            get { return _cameraStatus; }
            set { _cameraStatus = value; }
        }
        // AcqFIFO.cs 中添加
        private Func<CameraStatus> _getCameraStatusFunc = null;

        /// <summary>
        /// 设置获取相机状态的委托（用于实时获取最新状态）
        /// </summary>
        public void SetCameraStatusGetter(Func<CameraStatus> getStatusFunc)
        {
            _getCameraStatusFunc = getStatusFunc;
        }

        /// <summary>
        /// 获取当前相机状态（实时）
        /// </summary>
        private CameraStatus GetCurrentCameraStatus()
        {
            if (_getCameraStatusFunc != null)
                return _getCameraStatusFunc();
            return _cameraStatus;
        }
        /// <summary>灰度图像数组赋值与读取</summary>
        public CogImage8Grey[] _ci8gAcq
        {
            get { return ci8gAcq; }
            set { ci8gAcq = value; }
        }

        /// <summary>光度立体法中间图像（单相机）</summary>
     /*   public HObject[] _hObj
        {
            get { return hObjFX; }
            set { hObjFX = value; }
        }*/

        /// <summary>用户参数赋值与读取</summary>
        public CUserParam _cup
        {
            get { return cup; }
            set { cup = value; }
        }

        /// <summary>海康相机数组赋值与读取</summary>
        public MyCamera[] _HikCam
        {
            get { return _cameras; }
            set { _cameras = value; }
        }

        /// <summary>串口操作类赋值与读取</summary>
        public CSerialDef _csd
        {
            get { return csd; }
            set { csd = value; }
        }

        /// <summary>信号图像保存路径数组赋值与读取</summary>
        public string[] _strSigSavePathS
        {
            get { return strSigSavePathS; }
            set { strSigSavePathS = value; }
        }

        /// <summary>连续采集保存路径数组赋值与读取</summary>
        public string[] _strConSavePathS
        {
            get { return strConSavePathS; }
            set { strConSavePathS = value; }
        }

        #endregion


        #region 相机控制（自动曝光、连接检测、缓冲区设置）

       
        /// <summary>关闭指定数量相机的自动曝光</summary>
        /// <param name="CamNum">相机数量</param>
        public void CloseExposureAuto(int iCamInd)
        { 
            {
                _cameras[iCamInd].MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                Thread.Sleep(2);
            }
        }

        /// <summary>关闭所有已连接相机的自动曝光</summary>
        public void CloseAllExposureAuto()
        {
            if (_cameras != null)
            {
                for (int iCamInd = 0; iCamInd < _cameras.Length; iCamInd++)
                {
                    if (_cameras[iCamInd] != null)
                        _cameras[iCamInd].MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                    Thread.Sleep(2);
                }
            }
        }

        /// <summary>打开指定序号相机的自动曝光</summary>
        /// <param name="CamNum">相机数量</param>
        public void OpenExposureAuto(int iCamIndex)
        {
           // for (int iCamInd = 0; iCamInd < CamNum; iCamInd++)
            {
                _cameras[iCamIndex].MV_CC_SetEnumValue_NET("ExposureAuto", 2);
                Thread.Sleep(2);
            }
        }

        /// <summary>打开所有已连接相机的自动曝光</summary>
        public void OpenAllExposureAuto()
        {
            if (_cameras != null)
                for (int iCamInd = 0; iCamInd < _cameras.Length; iCamInd++)
                {
                    if (_cameras[iCamInd] != null)
                        _cameras[iCamInd].MV_CC_SetEnumValue_NET("ExposureAuto", 2);
                    Thread.Sleep(2);
                }
        }

        /// <summary>检查指定索引相机是否已连接</summary>
        /// <param name="i">相机索引</param>
        public bool IsCamConn(int iCamIndex)
        {
            if (_cameras != null && _cameras[iCamIndex] != null)
                return _cameras[iCamIndex].MV_CC_IsDeviceConnected_NET();
            else
                return false;
        }

        /// <summary>设置单个相机的驱动缓冲区和载荷大小</summary>
      /*  public void   SetBuff(IntPtr _Buff, UInt32 _PayloadSize, int iCamIndex)
        {
            m_BufForDriverS[iCamIndex] = _Buff;
            nPayloadSizeS[iCamIndex] = _PayloadSize;
        }*/

        /// <summary>批量设置所有相机的驱动缓冲区和载荷大小</summary>
      /*  public void SetBuff(IntPtr[] _Buff, UInt32[] _PayloadSize)
        {
            m_BufForDriverS = _Buff;
            nPayloadSizeS = _PayloadSize;
        }*/

        #endregion

        #region 图像采集（实时）
        /// <summary>采集单帧图像转Halcon格式，用于光度立体法（Halcon HObject格式）</summary>
        public HObject GrabOneFrameHObj(int iCamIndex)
        {
            if (_cameraStatus.iStatus[iCamIndex] < 2)
                return null;
           
            HImage image = new HImage();
            try
            {
                if (_cameraType == 0)
                {
                    if (_cameras == null || _cameras[iCamIndex] == null)
                        return null;
                    nRet = _cameras[iCamIndex].MV_CC_GetOneFrameTimeout_NET(m_BufForDriverS[iCamIndex], nPayloadSizeS[iCamIndex], ref stFrameInfo, 1000);
                    if (nRet == MyCamera.MV_OK)
                    {
                        image.GenImage1("byte", (int)stFrameInfo.nWidth, (int)stFrameInfo.nHeight, m_BufForDriverS[iCamIndex]);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    nRet = _cameras[iCamIndex].MV_CC_GetImageBuffer_NET(ref stFrameInfoWithData, 1000);
                    if (nRet == MyCamera.MV_OK)
                    {
                        image.GenImage1("byte", (int)stFrameInfoWithData.stFrameInfo.nWidth, (int)stFrameInfoWithData.stFrameInfo.nHeight, stFrameInfoWithData.pBufAddr);
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
        }
        /// <summary>采集单帧灰度图像转Cog格式,VP 算法使用
        public CogImage8Grey GrabOneFrameCog(int iJobIndex,int iCamIndex)
        {
            CameraStatus currentStatus = GetCurrentCameraStatus();
            if (_cameraStatus.iStatus[iCamIndex] < 2)
                return null;

            if (_cameraType == 0)
            {
                nRet = _cameras[iCamIndex].MV_CC_GetOneFrameTimeout_NET(m_BufForDriverS[iCamIndex], nPayloadSizeS[iCamIndex], ref stFrameInfo, 10000);
                if (nRet == MyCamera.MV_OK)
                {
                     //保留byte[]型数据
                    IntPtrToByteArray(m_BufForDriverS[iCamIndex], (int)nPayloadSizeS[iCamIndex], ref bImgSrcData[iCamIndex]);                  
                    cogImage8Root.Initialize((Int32)stFrameInfo.nWidth, (Int32)stFrameInfo.nHeight, m_BufForDriverS[iCamIndex], (Int32)stFrameInfo.nWidth, null);
                    cogImage8Grey.SetRoot(cogImage8Root);
                    
                }
                else
                {
                    return null;
                }
            }
            else
            {
                nRet = _cameras[iCamIndex].MV_CC_GetImageBuffer_NET(ref stFrameInfoWithData, 1000);
                if (nRet == MyCamera.MV_OK)
                {
                    // 从 frameInfoForOther 中获取图像宽高和像素数据指针
                   
                    IntPtr imagePtr = stFrameInfoWithData.pBufAddr;
                    //保留byte[]型数据
                    int width = (int)stFrameInfoWithData.stFrameInfo.nWidth;
                    int height = (int)stFrameInfoWithData.stFrameInfo.nHeight;
                    /* 
                     IntPtrToByteArray(imagePtr, (int)nPayloadSizeS[iCamIndex], ref bImgSrcData[iCamIndex]);
 */
                    // 初始化 CogImage8Root
                    cogImage8Root.Initialize(width, height, imagePtr, width, null);
                    cogImage8Grey.SetRoot(cogImage8Root);
                    // 应用旋转/镜像（如果需要）
                                      
                }
                else
                {
                    return null;
                } 
            }
            ci8gAcq[iCamIndex] = (CogImage8Grey)CogFlipRotateIm(cogImage8Grey,iJobIndex);
           // ci8gAcq[iCamInd] = cogImage8Grey.Copy();
            return ci8gAcq[iCamIndex];
        }
        /// <summary>采集单帧彩色图像转Cog格式,VP 算法使用     
        public CogImage24PlanarColor GrabOneFrameCogColor(int iJobIndex,int iCamIndex)
        {
            CameraStatus currentStatus = GetCurrentCameraStatus();
            if (currentStatus.iStatus[iCamIndex] < 2)
                return null;

            /*if (_cameraStatus.iStatus[iCamIndex] < 2)
                return null;*/
            if (_cameraType == 0)
            {
                nRet = _cameras[iCamIndex].MV_CC_GetOneFrameTimeout_NET(m_BufForDriverS[iCamIndex], nPayloadSizeS[iCamIndex], ref stFrameInfo, 10000);
                if (nRet == MyCamera.MV_OK)
                {
                    //保留byte[]型数据
                    IntPtrToByteArray(m_BufForDriverS[iCamIndex], (int)nPayloadSizeS[iCamIndex], ref bImgSrcData[iCamIndex]);
                    ci24ColorAcq[iCamIndex] = new CogImage24PlanarColor(IntPtrToBitmap(m_BufForDriverS[iCamIndex], stFrameInfo.nWidth, stFrameInfo.nHeight));
                    //  nRet = _cameras[iCamInd].MV_CC_FreeImageBuffer_NET(ref stFrameInfoWithData);
                   
                }
                else
                {
                    return null;
                }
            }
            else
            {
                nRet = _cameras[iCamIndex].MV_CC_GetImageBuffer_NET(ref stFrameInfoWithData, 1000);
                if (nRet == MyCamera.MV_OK)
                {
                    MyCamera.MvGvspPixelType enType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;
                    if (IsColorPixelFormat(stFrameInfoWithData.stFrameInfo.enPixelType))
                    {
                        enType = MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed;
                       
                    }
                    else if (IsMonoPixelFormat(stFrameInfoWithData.stFrameInfo.enPixelType))
                    {
                        enType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8;
                        
                    }
                    else
                    {
                        Console.WriteLine("Don't need to convert!");
                    }

                    // 从 frameInfoForOther 中获取图像宽高和像素数据指针
                    int width = (int)stFrameInfoWithData.stFrameInfo.nWidth;
                    int height = (int)stFrameInfoWithData.stFrameInfo.nHeight;
                    /*
                    IntPtr imagePtr = stFrameInfoWithData.pBufAddr;
                    //保留byte[]型数据
                    IntPtrToByteArray(imagePtr, (int)nPayloadSizeS[iCamIndex], ref bImgSrcData[iCamIndex]);
                     */
                    ci24ColorAcq[iCamIndex] = new CogImage24PlanarColor(IntPtrToBitmap(stFrameInfoWithData.pBufAddr, width, height));
                    nRet = _cameras[iCamIndex].MV_CC_FreeImageBuffer_NET(ref stFrameInfoWithData);                     
                }
                else
                {
                    return null;
                }
            }
            ci24ColorAcq[iCamIndex] = (CogImage24PlanarColor)CogFlipRotateIm(ci24ColorAcq[iCamIndex],iJobIndex);
            return ci24ColorAcq[iCamIndex];

        }
        //采集一帧Cog格式图像
        public bool GrabOneFrameCogFormat(bool bRecord, bool bListProcess,int wsInd, int iCamIndex)
        {
            //根据工位获取相机编号
         //   int iCamIndex = cup.p_CD.camInd[wsInd];
            if (_cameraStatus.iStatus[iCamIndex] < 2)
                return false;
            if(!bListProcess)
                return false;
            try
            {
                if (cup.p_Cam[iCamIndex].iMonoType == 0)
                    GrabOneFrameCog(wsInd,iCamIndex);         //实时运行时采集一帧黑白Cog格式图像
                else
                    GrabOneFrameCogColor(wsInd, iCamIndex);    //实时运行时采集一帧彩色Cog格式图像

                if (bRecord)                        //判断保存运行时采集图像条件
                {
                    SaveCogImage(wsInd,iCamIndex);            //保存图像按照工位编号保存                  
                }
                return true;
            }
            catch (Exception ex)
            {
                strErrInfo = ex.Message;
                return false;
            }
            
        }
        /// <summary>采集图像到byte数组</summary>
        public bool GrabOneFrameByte(int iCamIndex)
        {
            try
            {
                if (_cameraType == 0)
                {
                    nRet = _cameras[iCamIndex].MV_CC_GetOneFrameTimeout_NET(m_BufForDriverS[iCamIndex], nPayloadSizeS[iCamIndex], ref stFrameInfo, 10000);
                    if (nRet == MyCamera.MV_OK)
                    {
                        IntPtrToByteArray(m_BufForDriverS[iCamIndex], (int)nPayloadSizeS[iCamIndex], ref bImgSrcData[iCamIndex]);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    nRet = _cameras[iCamIndex].MV_CC_GetImageBuffer_NET(ref stFrameInfoWithData, 1000);
                    if (nRet == MyCamera.MV_OK)
                    {
                        IntPtrToByteArray(stFrameInfoWithData.pBufAddr, (int)nPayloadSizeS[iCamIndex], ref bImgSrcData[iCamIndex]);
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
        }
        public Bitmap IntPtrToBitmap(IntPtr ptr, int width, int height)
        {
            // stride 是每一行数据的字节数，对于24位图像，通常是 width * 3
            // 但内存对齐要求 stride 必须是4的倍数，所以需要进行对齐计算
            int stride = (width * 3 + 3) & ~3; // 计算4字节对齐的stride

            // 创建Bitmap，指定PixelFormat为Format24bppRgb
            return new Bitmap(width, height, stride, PixelFormat.Format24bppRgb, ptr);
        }
        /// <summary>批量采集所有相机图像（CogImage8Grey数组）</summary>
        public CogImage8Grey[] GrabOneFrameCog(int iJobIndex)
        {
            for (int iCamIndex = 0; iCamIndex < maxCameraNum; iCamIndex++)
            {
                if (_cameraStatus.iStatus[iCamIndex] < 2)
                   continue;
                CogImage8Grey cogImage8Grey = new CogImage8Grey();
                if (_cameraType == 0)
                {
                    nRet = _cameras[iCamIndex].MV_CC_GetOneFrameTimeout_NET(m_BufForDriverS[iCamIndex], nPayloadSizeS[iCamIndex], ref stFrameInfo, 10000);
                    if (nRet == MyCamera.MV_OK)
                    {
                        CogImage8Root cogImage8Root = new CogImage8Root();
                        cogImage8Root.Initialize((Int32)stFrameInfo.nWidth, (Int32)stFrameInfo.nHeight, m_BufForDriverS[iCamIndex], (Int32)stFrameInfo.nWidth, null);
                        cogImage8Grey.SetRoot(cogImage8Root);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    nRet = _cameras[iCamIndex].MV_CC_GetImageBuffer_NET(ref stFrameInfoWithData, 1000);
                    if (nRet == MyCamera.MV_OK)
                    {
                        // 从 frameInfoForOther 中获取图像宽高和像素数据指针
                        int width = (int)stFrameInfoWithData.stFrameInfo.nWidth;
                        int height = (int)stFrameInfoWithData.stFrameInfo.nHeight;
                        IntPtr imagePtr = stFrameInfoWithData.pBufAddr;

                        // 初始化 CogImage8Root
                        cogImage8Root.Initialize(width, height, imagePtr, width, null);
                        cogImage8Grey.SetRoot(cogImage8Root); 
                    }
                    else
                    {
                        return null;
                    }
                   
                }
                // 应用旋转/镜像（如果需要）               
                cogImage8Grey = (CogImage8Grey)CogFlipRotateIm(cogImage8Grey, iJobIndex);
                ci8gAcq[iCamIndex] = cogImage8Grey.Copy();
            }
            return ci8gAcq;
        }
        #endregion


        #region 图像采集（从文件）
        /// <summary>从文件读取一组灰度图像（所有相机）</summary>
        public void GrabOneFrameCogFromFile(int iFileInd, bool[] bListProcess)
        {
            int tWait = 5;
            int ind = iFileInd * 6;
         //   HObject objTmp;
            try
            {
                ICogImage ci;
                int iCamIndex;
                for (int wsInd = 0; wsInd < maxCameraNum; wsInd++)
                {
                    if (wsInd == 0)
                    {
                        ind = iFileInd * 6;
                        int iStart = (Path.GetFileName(strFiles[wsInd][ind])).IndexOf(cup.p_LI.strPrefix[wsInd]) + 5;
                        int iEnd = (Path.GetFileName(strFiles[wsInd][ind])).IndexOf("_00");
                        if (iStart == 0)
                        {
                            iStart = cup.p_LI.strPrefix[wsInd].Length;
                        }
                        strTime = (Path.GetFileName(strFiles[wsInd][ind])).Substring(iStart, iEnd - iStart);
                    }
                    else
                        ind = iFileInd * cmAcqNum;
                    if (bListProcess[wsInd])
                    {
                        ci = ReadCogImage(strFiles[wsInd][ind]);  //文件按照工位编号读取
                        iCamIndex = cup.p_CD.camInd[wsInd];
                        if (ci is CogImage24PlanarColor)
                        {
                            ci24ColorAcq[iCamIndex] = (CogImage24PlanarColor)ci;
                            cup.p_Cam[iCamIndex].iMonoType = 1;

                            //暂时不需要byte格式数据
                            /*int width = ci24ColorAcq[iCamIndex].Width;
                            int height = ci24ColorAcq[iCamIndex].Height;
                            if (bImgSrcData[iCamIndex] == null || bImgSrcData[iCamIndex].Length != width * height*3)
                                bImgSrcData[iCamIndex] = new byte[width * height * 3];
                            bImgSrcData[wsInd] = BitmapToBytes(ci24ColorAcq[wsInd].ToBitmap(), ImageFormat.Bmp);*/ 
                        }
                        else
                        {
                            ci8gAcq[iCamIndex] = (CogImage8Grey)ci;
                            cup.p_Cam[iCamIndex].iMonoType = 0; 
                            /*int width = ci8gAcq[wsInd].Width;
                           int height = ci8gAcq[wsInd].Height;
                          if (bImgSrcData[wsInd] == null || bImgSrcData[wsInd].Length != width * height * 3)
                               bImgSrcData[wsInd] = new byte[width * height * 3];*/

                            //   bImgSrcData[wsInd] = BitmapToBytes(ci8gAcq[wsInd].ToBitmap(), ImageFormat.Bmp); 
                             
                        }
 
                        Thread.Sleep(tWait);
                    }
                }
            }
            catch
            {
                return  ;
            }
            return  ;
        }
        public int GrabOneFrameCogFromFile(int iFileInd, bool bListProcess, int wsInd, int iCamIndex)
        {
            int tWait = 5;
            HObject objTmp;
          //  int iCamIndex=cup.p_CD.camInd[]
            try
            {
                if (bListProcess)
                {

                    ICogImage ci = ReadCogImage(strFiles[wsInd][iFileInd]);  //文件按照工位编号读取
                    ci = (CogImage8Grey)CogFlipRotateIm(ci,wsInd);
                    if (ci is CogImage24PlanarColor)
                    {
                        ci24ColorAcq[iCamIndex] = (CogImage24PlanarColor)ci;
                        cup.p_Cam[iCamIndex].iMonoType = 1;

                        //暂时不需要byte格式数据
                        /*int width = ci24ColorAcq[iCamIndex].Width;
                        int height = ci24ColorAcq[iCamIndex].Height;
                        if (bImgSrcData[iCamIndex] == null || bImgSrcData[iCamIndex].Length != width * height*3)
                            bImgSrcData[iCamIndex] = new byte[width * height * 3];
                        
                        bImgSrcData[wsInd] = BitmapToBytes(ci24ColorAcq[wsInd].ToBitmap(), ImageFormat.Bmp);*/

                        return 1;
                    }
                    else
                    {
                        ci8gAcq[iCamIndex] = (CogImage8Grey)ci;
                        cup.p_Cam[iCamIndex].iMonoType = 0;


                        /*int width = ci8gAcq[wsInd].Width;
                       int height = ci8gAcq[wsInd].Height;
                      if (bImgSrcData[wsInd] == null || bImgSrcData[wsInd].Length != width * height * 3)
                           bImgSrcData[wsInd] = new byte[width * height * 3];*/

                        //   bImgSrcData[wsInd] = BitmapToBytes(ci8gAcq[wsInd].ToBitmap(), ImageFormat.Bmp); 

                        return 0;
                    }
                    /*HOperatorSet.ReadImage(out objTmp, strFiles[wsInd][iFileInd]);
                   ci8gAcq[wsInd] = HObj2CogImage(objTmp);
                   Thread.Sleep(tWait);*/
                }
                else
                {
                    return -1;
                }
            }
            catch
            {
                return -1;
            }

        }
        public int GrabOneFrameCogFromFileWithSrcData(int iFileInd, bool bListProcess, int wsInd, int iCamIndex)
        {
            int tWait = 5;
            HObject objTmp;
            try
            {
                if (bListProcess)
                {
                    ICogImage ci = ReadCogImage(strFiles[wsInd][iFileInd]);  //文件按照工位编号读取

                    if (ci is CogImage24PlanarColor)
                    {
                        ci24ColorAcq[iCamIndex] = (CogImage24PlanarColor)ci;
                        cup.p_Cam[iCamIndex].iMonoType = 1;

                        //暂时不需要byte格式数据
                        int width = ci24ColorAcq[iCamIndex].Width;
                        int height = ci24ColorAcq[iCamIndex].Height;
                        if (bImgSrcData[iCamIndex] == null || bImgSrcData[iCamIndex].Length != width * height * 3)
                            bImgSrcData[iCamIndex] = new byte[width * height * 3];

                        bImgSrcData[wsInd] = BitmapToBytes(ci24ColorAcq[wsInd].ToBitmap(), ImageFormat.Bmp);

                        return 1;
                    }
                    else
                    {
                        ci8gAcq[wsInd] = (CogImage8Grey)ci;
                        cup.p_Cam[wsInd].iMonoType = 0;


                        int width = ci8gAcq[wsInd].Width;
                        int height = ci8gAcq[wsInd].Height;
                        if (bImgSrcData[wsInd] == null || bImgSrcData[wsInd].Length != width * height * 3)
                            bImgSrcData[wsInd] = new byte[width * height * 3];

                          bImgSrcData[wsInd] = BitmapToBytes(ci8gAcq[wsInd].ToBitmap(), ImageFormat.Bmp); 

                        return 0;
                    }
                    /*HOperatorSet.ReadImage(out objTmp, strFiles[wsInd][iFileInd]);
                   ci8gAcq[wsInd] = HObj2CogImage(objTmp);
                   Thread.Sleep(tWait);*/
                }
                else
                {
                    return -1;
                }
            }
            catch
            {
                return -1;
            }

        }

        /// <summary>从文件读取单帧图像（指定工位）,并判断文件色彩类型</summary>
        public int GrabOneFrameCogFromFile(int iFileInd, bool[] bListProcess, int wsInd, int iCamIndex)
        {
            int tWait = 5;
            HObject objTmp;
            try
            {
                if (bListProcess[wsInd])
                {
                   ICogImage ci= ReadCogImage(strFiles[wsInd][iFileInd]);  //文件存储是根据工位编号存储

                    ci = (CogImage8Grey)CogFlipRotateIm(ci,wsInd);
                    if (ci is CogImage24PlanarColor)
                    {
                        ci24ColorAcq[iCamIndex] = (CogImage24PlanarColor)ci;

                        cup.p_Cam[iCamIndex].iMonoType = 1;
                        return 1;
                    }
                    else
                    {
                        ci8gAcq[iCamIndex] = (CogImage8Grey)ci;
                        cup.p_Cam[iCamIndex].iMonoType = 0;
                        return 0;
                    }
                     /*HOperatorSet.ReadImage(out objTmp, strFiles[wsInd][iFileInd]);
                    ci8gAcq[wsInd] = HObj2CogImage(objTmp);
                    Thread.Sleep(tWait);*/
                }
                else
                {
                    return -1;
                }
            }
            catch
            {
                return -1;
            }
            
        }

        private  byte[] BitmapToBytes(Bitmap bitmap, ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, format);
                return ms.ToArray();
            }
        }
       
        public CogImage8Grey[] GrabOneFrameCogFromFileSingle(int iFileInd, bool bListProcess, int wsInd,int iCamIndex)
        {
            int tWait = 5;
            HObject objTmp;
            try
            {
                if (bListProcess)
                {
                    HOperatorSet.ReadImage(out objTmp, strFiles[wsInd][iFileInd]);
                    ci8gAcq[iCamIndex] = HObj2CogImage(objTmp);
                    Thread.Sleep(tWait);
                }
            }
            catch
            {
                return null;
            }
            return ci8gAcq;
        }

        /// <summary>从标定文件读取图像，索引混乱要修改 2026年5月10日</summary> 
        public CogImage8Grey[] GrabOneFrameCogFromFileCalib(int wsInd,int iCamIndex,string strFile)
        { 
            try
            {

                ICogImage ci = ReadCogImage(strFile);  //文件存储是根据工位编号存储 
                {
                   
                   ci8gAcq[iCamIndex] = (CogImage8Grey)CogFlipRotateIm(ci, wsInd);
                    cup.p_Cam[iCamIndex].iMonoType = 0;
                    
                } 
            }
            catch
            {
                return null;
            }
            return ci8gAcq;
        }
        public ICogImage GrabOneFrameCogFromFilePMA(string strFile,int iJobIndex)
        {
            try
            {
                ICogImage ci = ReadCogImage(strFile);  //文件存储是根据工位编号存储 
                 
                return CogFlipRotateIm(ci,iJobIndex);

            }
            catch
            {
                return null;
            }
           
        }
        #endregion

        #region 图像旋转镜像（Cognex）

        private MyCamera.MV_CC_ROTATE_IMAGE_PARAM rotateParam = new MyCamera.MV_CC_ROTATE_IMAGE_PARAM();
        private MyCamera.MV_CC_FLIP_IMAGE_PARAM flipParam = new MyCamera.MV_CC_FLIP_IMAGE_PARAM();
        private IntPtr ptrTmp = IntPtr.Zero;

        /// <summary>旋转/翻转图像（海康SDK方式，未完全调试）</summary>
        private void RotateAndFlipIm(MyCamera.MV_FRAME_OUT_INFO_EX frameParam, ref IntPtr ptrIm, int rotateType, int flipType)
        {
            rotateParam.nWidth = frameParam.nWidth;
            rotateParam.nHeight = frameParam.nHeight;
            if (rotateType == 1 || rotateType == 2 || rotateType == 3)
            {
                ptrTmp = Marshal.AllocHGlobal((Int32)frameParam.nFrameLen);
                rotateParam.enPixelType = frameParam.enPixelType;
                rotateParam.pSrcData = ptrIm;
                rotateParam.nSrcDataLen = frameParam.nFrameLen;
                rotateParam.pDstBuf = ptrTmp;
                rotateParam.nDstBufLen = frameParam.nFrameLen;
                rotateParam.nDstBufSize = frameParam.nFrameLen;
                rotateParam.enRotationAngle = (MyCamera.MV_IMG_ROTATION_ANGLE)rotateType;
                rotateParam.nRes = new uint[8];
                camera.MV_CC_RotateImage_NET(ref rotateParam);
            }
            if (flipType == 1 || flipType == 2)
            {
                flipParam.enPixelType = frameParam.enPixelType;
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

        /// <summary>初始化Cognex旋转镜像工具</summary>
        public void CogFlipRotateImInit()
        {
            for (int i = 0; i < maxCameraNum; i++)
            {
                ICogIPOneImageOperatorParams ip = (ICogIPOneImageOperatorParams)cogImFlipRotate[i];
                imgTool[i].Operators.Add(ip);
            }
        }

        /// <summary>对CogImage8Grey应用旋转/镜像</summary>
        public ICogImage CogFlipRotateIm(ICogImage inputIm,int iJobIndex)
        {
            switch (_rotateFlipType)
            {
                case 0: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.None; break;
                case 1: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate90Deg; break;
                case 2: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate180Deg; break;
                case 3: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate270Deg; break;
                case 4: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Flip; break;
                case 5: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate90Deg; break;
                case 6: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate180Deg; break;
                case 7: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate270Deg; break;
                case 8: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate180Deg; break;
                case 9: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate270Deg; break;
                case 10: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Flip; break;
                case 11: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate90Deg; break;
            }
            imgTool[iJobIndex].InputImage = inputIm;
            imgTool[iJobIndex].Run();
            return imgTool[iJobIndex].OutputImage;
        }

        /// <summary>采集单帧并应用旋转/镜像</summary>
        public CogImage8Grey GrabOneFrameCog(IntPtr m_BufForDriverS, uint nPayloadSizeS, int rotateType, int flipType)
        {
            CogImage8Grey cogImage8Grey = new CogImage8Grey();
            if (_cameraType == 0)
            {
                nRet = camera.MV_CC_GetOneFrameTimeout_NET(m_BufForDriverS, nPayloadSizeS, ref stFrameInfo, 10000);
                if (nRet == MyCamera.MV_OK)
                {
                    RotateAndFlipIm(stFrameInfo, ref m_BufForDriverS, rotateType, flipType);
                    CogImage8Root cogImage8Root = new CogImage8Root();
                    cogImage8Root.Initialize((Int32)stFrameInfo.nWidth, (Int32)stFrameInfo.nHeight, m_BufForDriverS, (Int32)stFrameInfo.nWidth, null);
                    cogImage8Grey.SetRoot(cogImage8Root);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                nRet = camera.MV_CC_GetImageBuffer_NET(ref stFrameInfoWithData, 1000);
                if (nRet != MyCamera.MV_OK)
                {
                    return null;
                }
            }
            return cogImage8Grey.Copy();
        }

        #endregion

        #region 格式转换（HObject ↔ CogImage ↔ byte[]）

        /// <summary>HObject转byte数组（灰度）</summary>
        private byte[] HObj2Bytes(HObject image)
        {
            HOperatorSet.GetImagePointer1(image, out HTuple pointer, out HTuple type, out HTuple width, out HTuple height);
            byte[] by = new byte[width * height];
            Marshal.Copy(pointer[0], by, 0, width * height);
            return by;
        }

        /// <summary>HObject转CogImage8Grey</summary>
        private CogImage8Grey HObj2CogImage(HObject image)
        {
            CogImage8Grey cogImage8Grey = new CogImage8Grey();
            HOperatorSet.GetImagePointer1(image, out HTuple hpoint, out HTuple type, out HTuple width, out HTuple height);
            CogImage8Root cogImage8Root = new CogImage8Root();
            cogImage8Root.Initialize((Int32)width, (Int32)height, (IntPtr)hpoint, (Int32)width, null);
            cogImage8Grey.SetRoot(cogImage8Root);
            return cogImage8Grey.Copy();
        }

        /// <summary>将IntPtr指向的非托管内存复制到byte数组</summary>
        public bool IntPtrToByteArray(IntPtr ptr, int length, ref byte[] byteArray)
        {
            if (ptr == IntPtr.Zero || length <= 0)
                return false;
            if (byteArray == null || byteArray.Length != length)
                byteArray = new byte[length];
            Marshal.Copy(ptr, byteArray, 0, length);
            return true;
        }

        /// <summary>根据通道数获取OpenCV MatType</summary>
        private MatType GetMatType(int channels)
        {
            switch (channels)
            {
                case 1: return MatType.CV_8UC1;
                case 3: return MatType.CV_8UC3;
                case 4: return MatType.CV_8UC4;
                default: throw new ArgumentException($"不支持的通道数: {channels}");
            }
        }

        /// <summary>将byte数组保存为JPEG（使用OpenCV）</summary>
        public bool SaveByteArrayAsJpg_OpenCV(byte[] imageData, int width, int height, int channels, string outputPath, int quality = 95)
        {
            try
            {
                MatType matType = GetMatType(channels);
                using (Mat mat = new Mat(height, width, matType))
                {
                    Marshal.Copy(imageData, 0, mat.Data, imageData.Length);
                    int[] qualityParams = { (int)ImwriteFlags.JpegQuality, quality };
                    return Cv2.ImWrite(outputPath, mat, qualityParams);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存图像失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 光度立体法（初始化、采集、重构）

        /// <summary>初始化光度立体法参数</summary>
        public void InitFX()
        {
            Slants = new HTuple();
            Tilts = new HTuple();
            Slants1 = new HTuple();
            Tilts1 = new HTuple();
            Slants2 = new HTuple();
            Tilts2 = new HTuple();

            ResultType = new HTuple();
          //  hObj1 = new HObject(); hObj2 = new HObject(); hObj3 = new HObject();hObj4 = new HObject();
            hObj = new HObject();

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

            try
            {
                Slants1 = cup.p_subFX[0].Slants;
                Slants2 = cup.p_subFX[1].Slants;
                Tilts1 = cup.p_subFX[0].Tilts;
                Tilts2 = cup.p_subFX[1].Tilts;
               /* sub1sigma = cup.p_subFX[0].sigma;
                sub1dMulti = cup.p_subFX[0].dMult;
                sub1dAdd = cup.p_subFX[0].dAdd;*/
                /*sub2sigma = cup.p_subFX[1].sigma;
                sub2dMulti = cup.p_subFX[1].dMult;
                sub2dAdd = cup.p_subFX[1].dAdd;*/
            }
            catch
            {
                Slants1[0] = 45; Slants1[1] = 45; Slants1[2] = 45;
                Slants2[0] = 45; Slants2[1] = 45; Slants2[2] = 45;
                Tilts1[0] = 0; Tilts1[1] = 90; Tilts1[2] = 180;
                Tilts2[0] = 0; Tilts2[1] = 90; Tilts2[2] = 180;
              //  sub1sigma = 2; sub1dMulti = 1460; sub1dAdd = 140;
               // sub2sigma = 2; sub2dMulti = 1460; sub2dAdd = 140;
            }
        }

        /// <summary>光度立体法主入口（实时采集）</summary>
        public bool FXSurfaceAll(bool bRecord, bool[] bListProcess)
        {
            int tWait = 5;
            try
            {
                {
                    csd.ExeProcessAll(0);
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (bListProcess[i])
                        {
                            hObjFXA[i, 0] = GrabOneFrameHObj(cup.p_CD.camInd[i]);
                            ci8gAcq[i] = HObj2CogImage(hObjFXA[i, 0]);
                            Thread.Sleep(tWait);
                        }
                    }
                    CloseAllExposureAuto();
                }

                if (cup.p_LS.bExe[1])
                {
                    csd.ExeProcessAll(1);
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (bListProcess[i])
                        {
                            hObjFXA[i, 1] = GrabOneFrameHObj(cup.p_CD.camInd[i]);
                            Thread.Sleep(tWait);
                        }
                    }
                }

                if (cup.p_LS.bExe[2])
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

                if (cup.p_LS.bExe[3])
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

                if (cup.p_LS.bExe[4])
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
                OpenAllExposureAuto();

                if (cup.p_VPPRTP.bFXRestruct)
                {
                    for (int i = 1; i < maxCameraNum; i++)
                    {
                        if (bListProcess[i])
                        {
                            bFXRun[i] = true;
                            FXConstructThread[i] = new Thread(new ParameterizedThreadStart(FXConstruct));
                            FXConstructThread[i].Start((object)i);
                        }
                    }

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
                            HOperatorSet.ConvertImageType(MeanCurvature, out hObjFXA[0, 5], "byte");
                            ci8gAcqFX[0] = HObj2CogImage(hObjFXA[0, 5]);
                        }
                    }
                    while (bFXRun[1] || bFXRun[2] || bFXRun[3] || bFXRun[4])
                    {
                        Thread.Sleep(10);
                    }
                }
                if (bRecord)
                {
                    saveFileThread = new Thread(SaveImageForFXSurfaceAll);
                    saveFileThread.Start();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>光度立体法（单工位）</summary>
        public bool FXSurfaceAll(bool bRecord, bool[] bListProcess, int wsInd,int iCamIndex)
        {
            int tWait = 5;
            try
            {
               // int iCamIndex = cup.p_CD.camInd[wsInd];
                if (bListProcess[wsInd])
                {
                    hObjFXA[iCamIndex, 0] = GrabOneFrameHObj(iCamIndex);
                    {
                        ci8gAcq[iCamIndex] = HObj2CogImage(hObjFXA[iCamIndex, 0]);
                        ci8gAcq[iCamIndex] = (CogImage8Grey)CogFlipRotateIm(ci8gAcq[iCamIndex],wsInd);
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
            catch (Exception ex)
            {
                strErrInfo = ex.Message;
                return false;
            }
        }

        /// <summary>光度重构工作线程（用于侧面相机）</summary>
        private void FXConstruct(Object ind)
        {
            int i = (int)ind;
            HTuple Slants1 = new HTuple();
            HTuple Tilts1 = new HTuple();
            HObject Gradient;
            HObject MeanCurvature;
            HObject hObj = null;
            int kInd = 0;
            for (int j = 1; j < 5; j++)
            {
                if (cup.p_subFX[i - 1].Tilts[j - 1] == -180)
                {
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
            HOperatorSet.ConvertImageType(MeanCurvature, out hObjFXA[i, 5], "byte");
            ci8gAcqFX[i] = HObj2CogImage(hObjFXA[i, 5]);
            bFXRun[i] = false;
        }
        /// <summary>从文件读取并执行光度立体法重构</summary>
        public bool FXSurfaceAllFromFile(int iFileInd, bool[] bListProcess)
        {
            int tWait = 5;
            int ind = 0;
            try
            {
                csd.ExeProcessAll(0);
                for (int i = 0; i < maxCameraNum; i++)
                {
                    if (i == 0)
                    {
                        ind = iFileInd * 6;
                        int iStart = (Path.GetFileName(strFiles[i][ind])).IndexOf(cup.p_LI.strCamPrefix[i]);
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
                        HOperatorSet.ReadImage(out hObjFXA[i, 0], strFiles[i][ind]);
                        ci8gAcq[i] = HObj2CogImage(hObjFXA[i, 0]);
                        Thread.Sleep(tWait);
                    }
                }

                if (cup.p_LS.bExe[1])
                {
                    csd.ExeProcessAll(1);
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (i == 0) ind = iFileInd * 6 + 1;
                        else ind = iFileInd * cmAcqNum + 1;
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
                        if (i == 0) ind = iFileInd * 6 + 2;
                        else ind = iFileInd * cmAcqNum + 2;
                        if (bListProcess[i])
                        {
                            HOperatorSet.ReadImage(out hObjFXA[i, 2], strFiles[i][ind]);
                            Thread.Sleep(tWait);
                        }
                    }
                }

                if (cup.p_LS.bExe[3])
                {
                    csd.ExeProcessAll(3);
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (i == 0) ind = iFileInd * 6 + 3;
                        else ind = iFileInd * cmAcqNum + 3;
                        if (bListProcess[i])
                        {
                            HOperatorSet.ReadImage(out hObjFXA[i, 3], strFiles[i][ind]);
                            Thread.Sleep(tWait);
                        }
                    }
                }

                if (cup.p_LS.bExe[4])
                {
                    csd.ExeProcessAll(4);
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (i == 0) ind = iFileInd * 6 + 4;
                        else ind = iFileInd * cmAcqNum + 4 % (cmAcqNum - 1);
                        if (bListProcess[i])
                        {
                            HOperatorSet.ReadImage(out hObjFXA[i, 4], strFiles[i][ind]);
                            Thread.Sleep(tWait);
                        }
                    }
                }

                csd.ExeProcessAll(5);
                OpenAllExposureAuto();

                if (cup.p_VPPRTP.bFXRestruct)
                {
                    for (int i = 1; i < maxCameraNum; i++)
                    {
                        if (bListProcess[i])
                        {
                            bFXRun[i] = true;
                            FXConstructThread[i] = new Thread(new ParameterizedThreadStart(FXConstruct));
                            FXConstructThread[i].Start((object)i);
                        }
                    }
                    if (bListProcess[0])
                    {
                        if (hObjFXA[0, 1] != null && hObjFXA[0, 2] != null && hObjFXA[0, 3] != null && hObjFXA[0, 4] != null)
                        {
                            HOperatorSet.ConcatObj(hObjFXA[0, 1], hObjFXA[0, 2], out hObj);
                            HOperatorSet.ConcatObj(hObj, hObjFXA[0, 3], out hObj);
                            HOperatorSet.ConcatObj(hObj, hObjFXA[0, 4], out hObj);
                            HOperatorSet.PhotometricStereo(hObj, out HeightField, out Gradient, out Albedo, Slants, Tilts, ResultType, "poisson", new HTuple(), new HTuple());
                            HOperatorSet.DerivateVectorField(Gradient, out MeanCurvature, cup.p_FX.sigma, "mean_curvature");
                            HOperatorSet.ScaleImage(MeanCurvature, out MeanCurvature, cup.p_FX.dMult, cup.p_FX.dAdd);
                            HOperatorSet.ConvertImageType(MeanCurvature, out hObjFXA[0, 5], "byte");
                            ci8gAcqFX[0] = HObj2CogImage(hObjFXA[0, 5]);
                        }
                    }
                    while (bFXRun[1] || bFXRun[2] || bFXRun[3] || bFXRun[4])
                    {
                        Thread.Sleep(10);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>从文件读取并重构（单工位）</summary>
        public bool FXSurfaceAllFromFile(int iFileInd, bool[] bListProcess, int wsInd)
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
                    strTime = (Path.GetFileName(strFiles[wsInd][iFileInd])).Substring(iStart, 14);
                    HOperatorSet.ReadImage(out hObjFXA[wsInd, 0], strFiles[wsInd][iFileInd]);
                    ci8gAcq[wsInd] = HObj2CogImage(hObjFXA[wsInd, 0]);
                    Thread.Sleep(tWait);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        /// <summary>从文件读取并重构（可选是否重构）</summary>
        public bool FXSurfaceAllFromFile(int iFileInd, bool[] bListProcess, bool bFXRestruct = false)
        {
            int tWait = 5;
            int ind = 0;
            try
            {
                csd.ExeProcessAll(0);
                for (int i = 0; i < maxCameraNum; i++)
                {
                    if (i == 0)
                    {
                        ind = iFileInd * 6;
                        int iStart = (Path.GetFileName(strFiles[i][ind])).IndexOf(cup.p_LI.strCamPrefix[i]) + 5;
                        int iEnd = (Path.GetFileName(strFiles[i][ind])).IndexOf("_00");
                        if (iStart == 0) iStart = cup.p_LI.strCamPrefix[i].Length;
                        strTime = (Path.GetFileName(strFiles[i][ind])).Substring(iStart, iEnd - iStart);
                    }
                    else
                        ind = iFileInd * cmAcqNum;
                    if (bListProcess[i])
                    {
                        HOperatorSet.ReadImage(out hObjFXA[i, 0], strFiles[i][ind]);
                        ci8gAcq[i] = HObj2CogImage(hObjFXA[i, 0]);
                        Thread.Sleep(tWait);
                    }
                }

                if (cup.p_LS.bExe[1])
                {
                    csd.ExeProcessAll(1);
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (i == 0) ind = iFileInd * 6 + 1;
                        else ind = iFileInd * cmAcqNum + 1;
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
                        if (i == 0) ind = iFileInd * 6 + 2;
                        else ind = iFileInd * cmAcqNum + 2;
                        if (bListProcess[i])
                        {
                            HOperatorSet.ReadImage(out hObjFXA[i, 2], strFiles[i][ind]);
                            Thread.Sleep(tWait);
                        }
                    }
                }

                if (cup.p_LS.bExe[3])
                {
                    csd.ExeProcessAll(3);
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (i == 0) ind = iFileInd * 6 + 3;
                        else ind = iFileInd * cmAcqNum + 3;
                        if (bListProcess[i])
                        {
                            HOperatorSet.ReadImage(out hObjFXA[i, 3], strFiles[i][ind]);
                            Thread.Sleep(tWait);
                        }
                    }
                }

                if (cup.p_LS.bExe[4])
                {
                    csd.ExeProcessAll(4);
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (i == 0) ind = iFileInd * 6 + 4;
                        else ind = iFileInd * cmAcqNum + 4 % (cmAcqNum - 1);
                        if (bListProcess[i])
                        {
                            HOperatorSet.ReadImage(out hObjFXA[i, 4], strFiles[i][ind]);
                            Thread.Sleep(tWait);
                        }
                    }
                }

                csd.ExeProcessAll(5);
                OpenAllExposureAuto();

                if (bFXRestruct)
                {
                    for (int i = 1; i < maxCameraNum; i++)
                    {
                        if (bListProcess[i])
                        {
                            bFXRun[i] = true;
                            FXConstructThread[i] = new Thread(new ParameterizedThreadStart(FXConstruct));
                            FXConstructThread[i].Start((object)i);
                        }
                    }
                    if (bListProcess[0])
                    {
                        if (hObjFXA[0, 1] != null && hObjFXA[0, 2] != null && hObjFXA[0, 3] != null && hObjFXA[0, 4] != null)
                        {
                            HOperatorSet.ConcatObj(hObjFXA[0, 1], hObjFXA[0, 2], out hObj);
                            HOperatorSet.ConcatObj(hObj, hObjFXA[0, 3], out hObj);
                            HOperatorSet.ConcatObj(hObj, hObjFXA[0, 4], out hObj);
                            HOperatorSet.PhotometricStereo(hObj, out HeightField, out Gradient, out Albedo, Slants, Tilts, ResultType, "poisson", new HTuple(), new HTuple());
                            HOperatorSet.DerivateVectorField(Gradient, out MeanCurvature, cup.p_FX.sigma, "mean_curvature");
                            HOperatorSet.ScaleImage(MeanCurvature, out MeanCurvature, cup.p_FX.dMult, cup.p_FX.dAdd);
                            HOperatorSet.ConvertImageType(MeanCurvature, out hObjFXA[0, 5], "byte");
                            ci8gAcqFX[0] = HObj2CogImage(hObjFXA[0, 5]);
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
                        if (i == 0) ind = iFileInd * 6 + 5;
                        else ind = iFileInd * cmAcqNum + 5;
                        if (bListProcess[i])
                        {
                            HOperatorSet.ReadImage(out hObjFXA[i, 5], strFiles[i][ind]);
                            ci8gAcqFX[i] = HObj2CogImage(hObjFXA[i, 5]);
                            Thread.Sleep(tWait);
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
        /// <summary>单相机光度立体法（旧版）</summary>
        public CogImage8Grey FXSurface(int iCamInd, bool bRecord)
        {
            //2026年4月10日 取消单相机光度立体法
            return null;
           /* string strTime = DateTime.Now.ToString("HHmmss");
            string strImageFile;
            try
            {
                csd.ExeProcess(0);
                hObjFX[0] = GrabOneFrameHObj(iCamInd);
                if (bRecord)
                {
                    strImageFile = strConSavePathS[0] + "\\" + cup.p_LI.strPrefix[0] + strTime + "_00";
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
                if (bRecord)
                {
                    strImageFile = strConSavePathS[0] + "\\" + cup.p_LI.strPrefix[0] + strTime + "_FX";
                    HOperatorSet.WriteImage(MeanCurvature, "jpg", 0, strImageFile);
                }
                return HObj2CogImage(MeanCurvature);
            }
            catch
            {
                return null;
            }*/
        }
        #endregion

        #region 文件保存与删除
        private void SaveCogImage(int wsInd,int iCamIndex)
        {
            //if (bRecord)
            {
                int dw = 0;
                //保存时间应该放在分通道之前，统一时间
                
                string strImageFile = strConSavePathS[wsInd] + "\\" + cup.p_LI.strPrefix[wsInd] + strTime + "_0" + dw.ToString() + ".jpg";

                //   for (int i = 0; i < maxCameraNum; i++)
                {
                    if (cup.p_CD.bListProcess[wsInd])
                    {
                        mImageFile[wsInd].Open(strImageFile, CogImageFileModeConstants.Write);
                        if (cup.p_Cam[wsInd].iMonoType == 0 && ci8gAcq[iCamIndex] != null)
                            mImageFile[wsInd].Append(ci8gAcq[iCamIndex]);
                        else if (ci24ColorAcq[iCamIndex] != null)
                            mImageFile[wsInd].Append(ci24ColorAcq[iCamIndex]); 
                        mImageFile[wsInd].Close();
                    }
                }
            }
        }
        // 创建图像文件工具
        CogImageFileTool imageFileTool = new CogImageFileTool();
        private ICogImage ReadCogImage(string strImageFile)
        { 
            // 以只读模式打开文件
            imageFileTool.Operator.Open(strImageFile, CogImageFileModeConstants.Read);

            // 执行工具，读取图像
            imageFileTool.Run();

            // 获取输出图像（单帧）
            ICogImage image = imageFileTool.OutputImage;

            // 关闭文件，释放资源
            imageFileTool.Operator.Close();
            return image;
        }
        /// <summary>保存单张图像Halcon函数保存（用于连续采集）</summary>
        public void SaveImageForCam(int ind, int dw)
        {
            strTime = DateTime.Now.ToString("yyyyMMddHHmmssFF");
            string strImageFile = strConSavePathS[ind] + "\\" + cup.p_LI.strPrefix[ind] + strTime + "_0" + dw.ToString() + ".jpg";
            if (cup.p_CD.bListProcess[ind] && hObjFXA[ind, 0] != null)
            {
                HOperatorSet.WriteImage(hObjFXA[ind, 0], "jpg", 0, strImageFile);
            }
        }
        /// <summary>保存所有光度立体法图像（多方向图+光度图）</summary>
        public void SaveImageForFXSurfaceAll()
        {
            strTime = DateTime.Now.ToString("yyyyMMddHHmmssFF");
            string strImageFile;
            for (int i = 0; i < maxCameraNum; i++)
            {
                for (int j = 0; j <= 4; j++)
                {
                    strImageFile = strConSavePathS[i] + "\\" + cup.p_LI.strPrefix[i] + strTime + "_0" + j.ToString() + ".jpg";
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
        }

        /// <summary>删除当前时间戳对应的一组图像（根据实时标志）</summary>
        public bool DelOneGroupImageFile(bool bLive)
        {
            if (bLive)
            {
                DelOneGroupImageFile();
            }
            return true;
        }

        /// <summary>删除当前时间戳对应的一组图像</summary>
        public bool DelOneGroupImageFile()
        {
            string strImageFile;
            try
            {
                for (int i = 0; i < maxCameraNum; i++)
                {
                    for (int j = 0; j <= 4; j++)
                    {
                        strImageFile = strConSavePathS[i] + "\\" + cup.p_LI.strPrefix[i] + strTime + "_0" + j.ToString() + ".jpg";
                        if (File.Exists(strImageFile))
                            File.Delete(strImageFile);
                        else
                        {
                            strImageFile = strConSavePathS[i] + "\\" + strTime + "_0" + j.ToString() + ".jpg";
                            if (File.Exists(strImageFile)) File.Delete(strImageFile);
                        }
                    }
                    strImageFile = strConSavePathS[i] + "\\" + cup.p_LI.strPrefix[i] + strTime + "_FX.jpg";
                    if (File.Exists(strImageFile))
                        File.Delete(strImageFile);
                    else
                    {
                        strImageFile = strConSavePathS[i] + "\\" + strTime + "_FX.jpg";
                        if (File.Exists(strImageFile)) File.Delete(strImageFile);
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>保存错误图像</summary>
        public void SaveImageForErr(string[] strErrPathS, string strTime)
        {
            string strImageFile;
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
        }

        /// <summary>导出当前图像到指定目录</summary>
        public bool ExportCurImage(string strExportPath, string[,] strFiles)
        {
            string strImageFile;
            for (int i = 0; i < maxCameraNum; i++)
            {
                if (!Directory.Exists(strExportPath + "\\" + cup.p_LI.strCamPrefix[i]))
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
            }
            catch
            {
                return false;
            }
            return true;
        }

        #endregion

        #region 直接采集和保存byte[]型图像数据
        /// <summary>采集图像并保存为字节数组</summary>
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

      

        /// <summary>保存原始字节图像为JPEG</summary>
        public void SaveImageFormSrcImage()
        {
            strTime = DateTime.Now.ToString("yyyyMMddHHmmssFF");
            string strImageFile;
            for (int i = 0; i < maxCameraNum; i++)
            {
                int iCamIndex = cup.p_CD.camInd[i];
                strImageFile = strConSavePathS[i] + "\\" + cup.p_LI.strPrefix[i] + strTime + "_00" + ".jpg";
                if (cup.p_CD.bListProcess[i] && bImgSrcData[iCamIndex] != null)
                {
                    SaveByteArrayAsJpg_OpenCV(bImgSrcData[iCamIndex], stFrameInfo.nWidth, stFrameInfo.nHeight, 1, strImageFile);
                }
            }
        }

        #endregion

        #region 彩色图像相关
        /// <summary>根据海康像素格式枚举判断是否为彩色</summary>
        private bool IsColorPixelFormat(MyCamera.MvGvspPixelType enPixelType)
        {
            switch (enPixelType)
            {
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_RGBA8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BGRA8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG8:
                    return true;
                default:
                    return false;
            }
        }
        private bool IsMonoPixelFormat(MyCamera.MvGvspPixelType enType)
        {
            switch (enType)
            {
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono12_Packed:
                    return true;
                default:
                    return false;
            }
        }
        /// <summary>将 Bayer 格式字节数组转换为 BGR 字节数组</summary>
        private byte[] BayerToBGR(byte[] bayerData, int width, int height, string bayerType)
        {
            using (Mat bayerMat = new Mat(height, width, MatType.CV_8UC1, bayerData))
            using (Mat bgrMat = new Mat())
            {
                ColorConversionCodes code = ColorConversionCodes.BayerBG2BGR;  //COLOR_BayerBG2BGR
                switch (bayerType)
                {
                    case "BayerRG": code = ColorConversionCodes.BayerRG2BGR; break;
                    case "BayerGB": code = ColorConversionCodes.BayerGB2BGR; break;
                    case "BayerGR": code = ColorConversionCodes.BayerGR2BGR; break;
                    default: code = ColorConversionCodes.BayerBG2BGR; break;
                }
                Cv2.CvtColor(bayerMat, bgrMat, code);
                byte[] result = new byte[bgrMat.Total() * bgrMat.Channels()];
                Marshal.Copy(bgrMat.Data, result, 0, result.Length);
                return result;
            }
        }

        /// <summary>根据帧信息和像素格式提取 BGR 24位数据</summary>
        private byte[] GetBGRDataFromFrame(MyCamera.MV_FRAME_OUT_INFO_EX frameInfo, IntPtr bufferPtr)
        {
            int width = (int)frameInfo.nWidth;
            int height = (int)frameInfo.nHeight;
            int frameLen = (int)frameInfo.nFrameLen;

            if (frameInfo.enPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed)
            {
                byte[] bgr = new byte[frameLen];
                Marshal.Copy(bufferPtr, bgr, 0, frameLen);
                return bgr;
            }
            else if (frameInfo.enPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed)
            {
                byte[] rgb = new byte[frameLen];
                Marshal.Copy(bufferPtr, rgb, 0, frameLen);
                byte[] bgr = new byte[frameLen];
                for (int i = 0; i < frameLen; i += 3)
                {
                    bgr[i] = rgb[i + 2];
                    bgr[i + 1] = rgb[i + 1];
                    bgr[i + 2] = rgb[i];
                }
                return bgr;
            }
            else if (frameInfo.enPixelType >= MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG8 &&
                     frameInfo.enPixelType <= MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG8)
            {
                byte[] bayer = new byte[frameLen];
                Marshal.Copy(bufferPtr, bayer, 0, frameLen);
                string bayerType = frameInfo.enPixelType.ToString().Replace("PixelType_Gvsp_", "");
                return BayerToBGR(bayer, width, height, bayerType);
            }
            else
            {
                throw new NotSupportedException($"不支持的彩色像素格式: {frameInfo.enPixelType}");
            }
        }

        /// <summary>采集单帧彩色图像（CogImage24PlanarColor）</summary>
        public bool GrabOneFrameColorCog(int iCamInd)
        {
            return true;
            /*if (_cameraStatus.iStatus[iCamInd] < 2)
                return null;

            if (_cameraType != 0) // 仅实现 GigE 相机，其他类型可扩展
                return null;

            try
            {
                nRet = _cameras[iCamInd].MV_CC_GetOneFrameTimeout_NET(m_BufForDriverS[iCamInd], nPayloadSizeS[iCamInd], ref stFrameInfo, OneFrameTimeoutMs);
                if (nRet != MyCamera.MV_OK)
                    return null;

                if (!IsColorPixelFormat(stFrameInfo.enPixelType))
                    return null; // 不是彩色格式，返回 null（可考虑转为灰度）

                byte[] bgrData = GetBGRDataFromFrame(stFrameInfo, m_BufForDriverS[iCamInd]);
                int width = (int)stFrameInfo.nWidth;
                int height = (int)stFrameInfo.nHeight;

                CogImage24PlanarColor colorImg = new CogImage24PlanarColor()
                 
                CogImage24PlanarRoot root = new CogImage24PlanarRoot();
                root.Initialize(width, height, bgrData, width, null);
                colorImg.SetRoot(root);

                // 应用旋转镜像（如果有）
                colorImg = CogFlipRotateImColor(colorImg);

                return colorImg;
            }
            catch (Exception ex)
            {
                strErrInfo = ex.Message;
                return null;
            }*/
        }

        private Mat CogImage24PlanarColorToMat(CogImage24PlanarColor cogImage)
        {
            // 1. 参数检查
            if (cogImage == null)
                throw new ArgumentNullException(nameof(cogImage));

            // 2. 将 CogImage24PlanarColor 转换为 System.Drawing.Bitmap
            using (Bitmap bitmap = cogImage.ToBitmap())
            {
                // 检查 Bitmap 是否创建成功
                if (bitmap == null)
                    throw new InvalidOperationException("无法将 CogImage24PlanarColor 转换为 Bitmap。");

                // 3. 使用 OpenCvSharp.Extensions 将 Bitmap 转换为 Mat
                //    注意：此方法默认将 Bitmap 的 RGB 格式转换为 Mat 的 BGR 格式[reference:0]
                Mat mat = BitmapConverter.ToMat(bitmap);

                // 4. 如果希望 Mat 保持 RGB 顺序，可以取消下面一行的注释进行转换
                // Cv2.CvtColor(mat, mat, ColorConversionCodes.BGR2RGB);

                // 返回转换后的 Mat 对象
                return mat;
            }
        }
 

        private CogImage24PlanarColor MatToCogImage24PlanarColor(Mat mat)
        {
            // 1. 参数检查
            if (mat == null || mat.Empty())
                throw new ArgumentNullException(nameof(mat), "输入的 Mat 对象为空或无效。");

            // 2. 确保 Mat 的数据类型为 8UC3 (8-bit unsigned, 3 channels)
            Mat convertedMat = mat;
            if (mat.Type() != MatType.CV_8UC3)
            {
                convertedMat = new Mat();
                mat.ConvertTo(convertedMat, MatType.CV_8UC3);
            }

            // 3. 将 Mat 转换为 System.Drawing.Bitmap
            //    注意：BitmapConverter.ToBitmap 默认将 Mat 的 BGR 格式转换为 Bitmap 的 RGB 格式
            using (Bitmap bitmap = BitmapConverter.ToBitmap(convertedMat))
            {
                // 检查 Bitmap 是否创建成功
                if (bitmap == null)
                    throw new InvalidOperationException("无法将 Mat 转换为 Bitmap。");

                // 4. 使用 CogImage24PlanarColor 的构造函数，将 Bitmap 转换为 CogImage24PlanarColor
                //    此构造函数内部会自动处理 RGB 格式的数据
                CogImage24PlanarColor cogImage = new CogImage24PlanarColor(bitmap);

                // 5. 释放临时 Mat 对象（如果进行了类型转换）
                if (convertedMat != mat)
                    convertedMat.Dispose();

                return cogImage;
            }
        }
        /// <summary>对CogImage8Grey应用旋转/镜像</summary>
        private CogImage24PlanarColor CogFlipRotateImColor(CogImage24PlanarColor inputIm,int iJobIndex)
        {
            switch (_rotateFlipType)
            {
                case 0: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.None; break;
                case 1: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate90Deg; break;
                case 2: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate180Deg; break;
                case 3: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate270Deg; break;
                case 4: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Flip; break;
                case 5: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate90Deg; break;
                case 6: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate180Deg; break;
                case 7: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate270Deg; break;
                case 8: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate180Deg; break;
                case 9: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate270Deg; break;
                case 10: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Flip; break;
                case 11: cogImFlipRotate[iJobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate90Deg; break;
            }
            imgTool[iJobIndex].InputImage = inputIm;
            imgTool[iJobIndex].Run();
            return (CogImage24PlanarColor)imgTool[iJobIndex].OutputImage;
        }
        /// <summary>采集单帧彩色图像（Halcon HObject，三通道交错格式）</summary>
        /* public HObject GrabOneFrameColorHObj(int iCamInd)
         {
             if (_cameraStatus.iStatus[iCamInd] < 2)
                 return null;

             if (_cameraType != 0)
                 return null;

             try
             {
                 nRet = _cameras[iCamInd].MV_CC_GetOneFrameTimeout_NET(m_BufForDriverS[iCamInd], nPayloadSizeS[iCamInd], ref stFrameInfo, OneFrameTimeoutMs);
                 if (nRet != MyCamera.MV_OK)
                     return null;

                 if (!IsColorPixelFormat(stFrameInfo.enPixelType))
                     return null;

                 byte[] bgrData = GetBGRDataFromFrame(stFrameInfo, m_BufForDriverS[iCamInd]);
                 int width = (int)stFrameInfo.nWidth;
                 int height = (int)stFrameInfo.nHeight;

                 HObject image;
                 HOperatorSet.GenImageInterleaved(out image, bgrData, "bgr", width, height, 0, "byte", width, height, 0, 0, -1, 0);
                 return image;
             }
             catch (Exception ex)
             {
                 strErrInfo = ex.Message;
                 return null;
             }
         }*/


        #endregion

    }
}