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
      //  private int _rotateFlipType = 4;
        private int[] _rotateFlipTypes = new int[maxCameraNum];
        /// <summary>海康SDK返回值</summary>
        private int nRet = 0;

        /// <summary>每台物理相机独立的单帧取图锁。</summary>
        private readonly object[] _cameraFrameLocks = new object[maxCameraNum];

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
        /// <summary>
        /// 当发生错误时触发，参数为错误描述字符串。
        /// </summary>
        public event EventHandler<string> ErrorOccurred;
        private void OnErrorOccurred(string errorMessage)
        {
            ErrorOccurred?.Invoke(this, errorMessage);
        }


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
     //   public HObject[] hColorImg = new HObject[maxCameraNum];

        /// <summary>多通道的灰度图（全光图）</summary>
        public CogImage8Grey[] ci8gAcq = new CogImage8Grey[maxCameraNum]; 
        /// <summary>多通道的光度图</summary>
    //    public CogImage8Grey[] ci8gAcqFX = new CogImage8Grey[maxCameraNum];
         /// <summary>彩色图像（Cognex 24位平面彩色）</summary>
        public CogImage24PlanarColor[] ci24ColorAcq = new CogImage24PlanarColor[maxCameraNum];
        
        /// <summary>原始图像字节数据（用于深度学习等）</summary>
        public byte[][] bImgSrcData = new byte[maxCameraNum][];
           
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

        
        /// <summary>Cognex格式图像保存工具</summary>
      //  CogImageFile mImageFile = new CogImageFile();
        private CogIPOneImageFlipRotate[] cogImFlipRotate;
        private CogIPOneImageTool[] imgTool;
        private CogImageFile[] mImageFile;
        
        /// <summary>文件保存线程</summary>
        private Thread saveFileThread;

        
        /// <summary>时间戳字符串（用于文件名）</summary>
        public string[] strTime = new string[maxCameraNum];


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
                _cameraFrameLocks[i] = new object();
                _rotateFlipTypes[i] = 0;   // 默认无旋转镜像，可根据需要调整
            }
        }
        #endregion

        #region 属性
        /// <summary>旋转镜像类型赋值与读取</summary>
        /*public int _rtType
        {
            get { return _rotateFlipType; }
            set { _rotateFlipType = value; }
        }*/
        public int[] _rtType => _rotateFlipTypes;
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
       
        /// <summary>检查指定索引相机是否已连接</summary>
        /// <param name="i">相机索引</param>
        public bool IsCamConn(int iCamIndex)
        {
            if (_cameras != null && _cameras[iCamIndex] != null)
                return _cameras[iCamIndex].MV_CC_IsDeviceConnected_NET();
            else
                return false;
        }
 
        #endregion

        #region 图像采集（实时） 
        /// <summary>采集单帧灰度图像转Cog格式,VP 算法使用
        public CogImage8Grey GrabOneFrameCog(int jobIndex,int iCamIndex)
        {
            if (iCamIndex < 0 || iCamIndex >= maxCameraNum)
            {
                OnErrorOccurred($"GrabOneFrameCog (Job{jobIndex}, Cam{iCamIndex}): 相机索引无效");
                return null;
            }

            lock (_cameraFrameLocks[iCamIndex])
            {
                MyCamera.MV_FRAME_OUT frame = new MyCamera.MV_FRAME_OUT();
                bool frameAcquired = false;
                try
                {
                    CameraStatus currentStatus = GetCurrentCameraStatus();
                    if (currentStatus.iStatus[iCamIndex] < 2 ||
                        _cameras == null || _cameras[iCamIndex] == null)
                    {
                        OnErrorOccurred($"GrabOneFrameCog (Job{jobIndex}, Cam{iCamIndex}): 相机未就绪");
                        return null;
                    }

                    strTime[jobIndex] = DateTime.Now.ToString("yyyyMMddHHmmssFF");
                    int frameResult = _cameras[iCamIndex].MV_CC_GetImageBuffer_NET(ref frame, 1000);
                    if (frameResult != MyCamera.MV_OK)
                    {
                        OnErrorOccurred($"GrabOneFrameCog (Job{jobIndex}, Cam{iCamIndex}): GetImageBuffer失败，SDK=0x{unchecked((uint)frameResult):X8}");
                        return null;
                    }

                    frameAcquired = true;
                    MyCamera.MV_FRAME_OUT_INFO_EX frameInfo = frame.stFrameInfo;
                    if (frame.pBufAddr == IntPtr.Zero || frameInfo.nFrameLen == 0)
                    {
                        OnErrorOccurred($"GrabOneFrameCog (Job{jobIndex}, Cam{iCamIndex}): SDK返回空帧");
                        return null;
                    }
                    if (frameInfo.enPixelType != MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8)
                    {
                        OnErrorOccurred($"GrabOneFrameCog (Job{jobIndex}, Cam{iCamIndex}): 不支持的PixelFormat={frameInfo.enPixelType}，灰度入口仅支持Mono8");
                        return null;
                    }

                    int frameLength = checked((int)frameInfo.nFrameLen);
                    byte[] frameData = new byte[frameLength];
                    Marshal.Copy(frame.pBufAddr, frameData, 0, frameLength);
                    bImgSrcData[iCamIndex] = frameData;

                    GCHandle pinnedFrame = GCHandle.Alloc(frameData, GCHandleType.Pinned);
                    try
                    {
                        CogImage8Root localRoot = new CogImage8Root();
                        localRoot.Initialize((int)frameInfo.nWidth, (int)frameInfo.nHeight,
                            pinnedFrame.AddrOfPinnedObject(), (int)frameInfo.nWidth, null);
                        CogImage8Grey localImage = new CogImage8Grey();
                        localImage.SetRoot(localRoot);
                        CogImage8Grey ownedImage = (CogImage8Grey)localImage.Copy();
                        ci8gAcq[iCamIndex] = (CogImage8Grey)CogFlipRotateIm(ownedImage, jobIndex);
                        return ci8gAcq[iCamIndex];
                    }
                    finally
                    {
                        pinnedFrame.Free();
                    }
                }
                catch(Exception ex)
                {
                    OnErrorOccurred($"GrabOneFrameCog (Job{jobIndex}, Cam{iCamIndex}): {ex.Message}");
                    return null;
                }
                finally
                {
                    if (frameAcquired)
                    {
                        int freeResult = _cameras[iCamIndex].MV_CC_FreeImageBuffer_NET(ref frame);
                        if (freeResult != MyCamera.MV_OK)
                            OnErrorOccurred($"GrabOneFrameCog (Job{jobIndex}, Cam{iCamIndex}): FreeImageBuffer失败，SDK=0x{unchecked((uint)freeResult):X8}");
                    }
                }
            }
        }
        /// <summary>采集单帧彩色图像转Cog格式,VP 算法使用     
        public CogImage24PlanarColor GrabOneFrameCogColor(int jobIndex,int iCamIndex)
        {
            if (iCamIndex < 0 || iCamIndex >= maxCameraNum)
            {
                OnErrorOccurred($"CogImage24PlanarColor (Job{jobIndex}, Cam{iCamIndex}): 相机索引无效");
                return null;
            }

            lock (_cameraFrameLocks[iCamIndex])
            {
                MyCamera.MV_FRAME_OUT frame = new MyCamera.MV_FRAME_OUT();
                bool frameAcquired = false;
                try
                {
                    CameraStatus currentStatus = GetCurrentCameraStatus();
                    if (currentStatus.iStatus[iCamIndex] < 2 ||
                        _cameras == null || _cameras[iCamIndex] == null)
                    {
                        OnErrorOccurred($"CogImage24PlanarColor (Job{jobIndex}, Cam{iCamIndex}): 相机未就绪");
                        return null;
                    }

                    strTime[jobIndex] = DateTime.Now.ToString("yyyyMMddHHmmssFF");
                    int frameResult = _cameras[iCamIndex].MV_CC_GetImageBuffer_NET(ref frame, 1000);
                    if (frameResult != MyCamera.MV_OK)
                    {
                        OnErrorOccurred($"CogImage24PlanarColor (Job{jobIndex}, Cam{iCamIndex}): GetImageBuffer失败，SDK=0x{unchecked((uint)frameResult):X8}");
                        return null;
                    }

                    frameAcquired = true;
                    MyCamera.MV_FRAME_OUT_INFO_EX frameInfo = frame.stFrameInfo;
                    if (frame.pBufAddr == IntPtr.Zero || frameInfo.nFrameLen == 0)
                    {
                        OnErrorOccurred($"CogImage24PlanarColor (Job{jobIndex}, Cam{iCamIndex}): SDK返回空帧");
                        return null;
                    }
                    if (frameInfo.enPixelType != MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed &&
                        frameInfo.enPixelType != MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed &&
                        !IsBayerPixelFormat(frameInfo.enPixelType))
                    {
                        OnErrorOccurred($"CogImage24PlanarColor (Job{jobIndex}, Cam{iCamIndex}): 不支持的PixelFormat={frameInfo.enPixelType}，彩色入口仅支持BGR8/RGB8/Bayer8");
                        return null;
                    }

                    int frameLength = checked((int)frameInfo.nFrameLen);
                    byte[] frameData = new byte[frameLength];
                    Marshal.Copy(frame.pBufAddr, frameData, 0, frameLength);

                    int width = checked((int)frameInfo.nWidth);
                    int height = checked((int)frameInfo.nHeight);
                    int bgrLength = checked(width * height * 3);
                    byte[] bgrData;
                    if (IsBayerPixelFormat(frameInfo.enPixelType))
                    {
                        bgrData = BayerToBGR(frameData, width, height, frameInfo.enPixelType);
                    }
                    else
                    {
                        if (frameData.Length < bgrLength)
                            throw new ArgumentException($"{frameInfo.enPixelType}帧长度小于BGR图像尺寸要求");

                        bgrData = new byte[bgrLength];
                        Buffer.BlockCopy(frameData, 0, bgrData, 0, bgrLength);
                        if (frameInfo.enPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed)
                            ConvertRgbToBgrInPlace(bgrData, width, height);
                    }

                    bImgSrcData[iCamIndex] = bgrData;
                    using (Bitmap bitmap = CreateBgrBitmap(bgrData, width, height))
                    {
                        CogImage24PlanarColor ownedImage = new CogImage24PlanarColor(bitmap);
                        ci24ColorAcq[iCamIndex] = (CogImage24PlanarColor)CogFlipRotateIm(ownedImage, jobIndex);
                    }
                    return ci24ColorAcq[iCamIndex];
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"CogImage24PlanarColor (Job{jobIndex}, Cam{iCamIndex}): {ex.Message}");
                    return null;
                }
                finally
                {
                    if (frameAcquired)
                    {
                        int freeResult = _cameras[iCamIndex].MV_CC_FreeImageBuffer_NET(ref frame);
                        if (freeResult != MyCamera.MV_OK)
                            OnErrorOccurred($"CogImage24PlanarColor (Job{jobIndex}, Cam{iCamIndex}): FreeImageBuffer失败，SDK=0x{unchecked((uint)freeResult):X8}");
                    }
                }
            }
        }
        //采集一帧Cog格式图像
        public bool GrabOneFrameCogFormat(bool bRecord, bool bListProcess,int wsInd, int iCamIndex)
        {
            //根据工位获取相机编号
            //   int iCamIndex = cup.p_CD.camInd[wsInd];
             
            if (_cameraStatus.iStatus[iCamIndex] < 2) 
            {
                OnErrorOccurred($"GrabOneFrameCogFormat (Job{wsInd}, Cam{iCamIndex}): 相机未就绪");
                return false;
            }
            if (!bListProcess) {
                OnErrorOccurred($"GrabOneFrameCogFormat (Job{wsInd}, Cam{iCamIndex}): 工位未启用");
                return false;}


            try
            {
                ICogImage acquiredImage;
                if (cup.p_Cam[iCamIndex].iMonoType == 0)
                {
                    ci8gAcq[iCamIndex] = null;
                    bImgSrcData[iCamIndex] = null;
                    acquiredImage = GrabOneFrameCog(wsInd, iCamIndex); //实时运行时只采集当前工位对应相机
                }
                else
                {
                    ci24ColorAcq[iCamIndex] = null;
                    bImgSrcData[iCamIndex] = null;
                    acquiredImage = GrabOneFrameCogColor(wsInd, iCamIndex); //实时运行时只采集当前工位对应相机
                }

                if (acquiredImage == null)
                {
                    strErrInfo = $"Job{wsInd} 对应 Cam{iCamIndex} 本轮取图失败";
                    OnErrorOccurred($"GrabOneFrameCogFormat (Job{wsInd}, Cam{iCamIndex}): 本轮取图返回空图像");
                    return false;
                }

             //   OnErrorOccurred($"是否存储图像:{bRecord}");
                if (bRecord)                        //判断保存运行时采集图像条件
                {
                  //  OnErrorOccurred($"存储一帧图像");
                    SaveCogImage(wsInd,iCamIndex);            //保存图像按照工位编号保存                  
                }
                return true;
            }
            catch (Exception ex)
            {
                strErrInfo = ex.Message;
                OnErrorOccurred($"GrabOneFrameCogFormat (Job{wsInd}, Cam{iCamIndex}): {strErrInfo}");
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
                        OnErrorOccurred($"GrabOneFrameByte (  Cam{iCamIndex}): 错误代码：{nRet}");
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
                        OnErrorOccurred($"GrabOneFrameByte (  Cam{iCamIndex}): 错误代码：{nRet}");
                        return false;
                    }
                }
            }
            catch(Exception ex)
            {
                OnErrorOccurred($"GrabOneFrameByte (  Cam{iCamIndex}): 错误信息：{ex.Message}");
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
        public CogImage8Grey[] GrabOneFrameCog(int jobIndex)
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
                cogImage8Grey = (CogImage8Grey)CogFlipRotateIm(cogImage8Grey, jobIndex);
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
                        strTime[wsInd] = (Path.GetFileName(strFiles[wsInd][ind])).Substring(iStart, iEnd - iStart);
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
            catch (Exception ex)
            {
                OnErrorOccurred($"GrabOneFrameCogFromFile ( 错误信息：{ex.Message}");
                return ;
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
                    ci = CogFlipRotateIm(ci,wsInd);
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
                    OnErrorOccurred($"GrabOneFrameCogFromFile 通道未启用");
                    return -1;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"GrabOneFrameCogFromFile (JobInd{wsInd}): 错误信息：{ex.Message}");
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

                        bImgSrcData[wsInd] = BitmapToBytes(ci24ColorAcq[wsInd].ToBitmap());

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

                          bImgSrcData[wsInd] = BitmapToBytes(ci8gAcq[wsInd].ToBitmap()); 

                        return 0;
                    }
                    /*HOperatorSet.ReadImage(out objTmp, strFiles[wsInd][iFileInd]);
                   ci8gAcq[wsInd] = HObj2CogImage(objTmp);
                   Thread.Sleep(tWait);*/
                }
                else
                {
                    OnErrorOccurred($"GrabOneFrameCogFromFileWithSrcData 通道未启用");
                    return -1;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"GrabOneFrameCogFromFileWithSrcData (JobInd{wsInd}): 错误信息：{ex.Message}");
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

                    ci = CogFlipRotateIm(ci,wsInd);
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
                    OnErrorOccurred($"GrabOneFrameCogFromFile 通道未启用");
                    return -1;
                }
            }
            catch(Exception ex)
            {
                OnErrorOccurred($"GrabOneFrameCogFromFile (JobInd{wsInd}): 错误信息：{ex.Message}");
                return -1;
            }
            
        }

        private byte[] BitmapToBytes(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            int width = bitmap.Width;
            int height = bitmap.Height;
            int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            byte[] pixels = new byte[width * height * bytesPerPixel];

            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                bitmap.PixelFormat
            );

            try
            {
                IntPtr ptr = bmpData.Scan0;
                int stride = bmpData.Stride;

                // 如果每行无填充，一次性复制全部
                if (stride == width * bytesPerPixel)
                {
                    Marshal.Copy(ptr, pixels, 0, pixels.Length);
                }
                else
                {
                    // 有填充时逐行复制，跳过填充字节
                    for (int y = 0; y < height; y++)
                    {
                        IntPtr rowPtr = IntPtr.Add(ptr, y * stride);
                        Marshal.Copy(rowPtr, pixels, y * width * bytesPerPixel, width * bytesPerPixel);
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }

            return pixels;
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
        public ICogImage GrabOneFrameCogFromFilePMA(string strFile,int jobIndex)
        {
            try
            {
                ICogImage ci = ReadCogImage(strFile);  //文件存储是根据工位编号存储 
                 
                return CogFlipRotateIm(ci,jobIndex);

            }
            catch
            {
                return null;
            }
           
        }

        /// <summary>
        /// 保存指定工位的当前采集图像到错误路径（用于定位失败自动保存）
        /// </summary>
        /// <param name="strErrPathS">错误图像保存路径数组（按工位索引）</param>
        /// <param name="strTime">时间戳字符串</param>
        /// <param name="jobIndex">工位索引（0=A面，1=B面）</param>
        public void SaveCurrentImageForErr(string[] strErrPathS, string strTime, int jobIndex)
        {
            if (jobIndex < 0 || jobIndex >= maxCameraNum)
                return;

           /* if (cup == null || cup.p_CD == null || cup.p_LI == null)
                return;*/

            int iCamIndex = cup.p_CD.camInd[jobIndex];  // 物理相机索引

            // 确保错误目录存在
            string dir = strErrPathS[jobIndex];
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string strImageFile = Path.Combine(dir, cup.p_LI.strPrefix[jobIndex] + strTime + "_10.jpg");

            // 根据图像类型保存
            if (cup.p_Cam[jobIndex].iMonoType == 0 && ci8gAcq[iCamIndex] != null)
            {
                mImageFile[jobIndex].Open(strImageFile, CogImageFileModeConstants.Write);
                mImageFile[jobIndex].Append(CogFlipRotateIm(ci8gAcq[iCamIndex],jobIndex));
                mImageFile[jobIndex].Close();
            }
            else if (ci24ColorAcq[iCamIndex] != null)
            {
                mImageFile[jobIndex].Open(strImageFile, CogImageFileModeConstants.Write);
                mImageFile[jobIndex].Append(CogFlipRotateIm(ci24ColorAcq[iCamIndex],jobIndex));
                mImageFile[jobIndex].Close();
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
        public ICogImage CogFlipRotateIm(ICogImage inputIm,int jobIndex)
        {
            switch (_rotateFlipTypes[jobIndex])
            {
                case 0: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.None; break;
                case 1: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate90Deg; break;
                case 2: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate180Deg; break;
                case 3: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate270Deg; break;
                case 4: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Flip; break;
                case 5: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate90Deg; break;
                case 6: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate180Deg; break;
                case 7: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate270Deg; break;
                case 8: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate180Deg; break;
                case 9: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate270Deg; break;
                case 10: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Flip; break;
                case 11: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate90Deg; break;
            }
            imgTool[jobIndex].InputImage = inputIm;
            imgTool[jobIndex].Run();
            return imgTool[jobIndex].OutputImage;
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

        #region 更新VP记录使用（初始化、采集、重构）
                
        public bool GrabOneFrameCogWithObj(bool bRecord, bool[] bListProcess, int wsInd,int iCamIndex)
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
        /// <summary>从文件读取 （单工位）</summary>
        public bool GrabOneFrameCogWithObjFromFile(int iFileInd, bool[] bListProcess, int wsInd)
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
                    strTime[wsInd] = (Path.GetFileName(strFiles[wsInd][iFileInd])).Substring(iStart, 14);
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
        #endregion

        #region 文件保存与删除
        private void SaveCogImage(int wsInd, int iCamIndex)
        {
            try
            {
                {
                    int dw = 0;
                    //保存时间应该放在分通道之前，统一时间

                    string strImageFile = strConSavePathS[wsInd] + "\\" + cup.p_LI.strPrefix[wsInd] + strTime[wsInd] + "_0" + dw.ToString() + ".jpg";

                    //   for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (cup.p_CD.bListProcess[wsInd])
                        {
                            mImageFile[wsInd].Open(strImageFile, CogImageFileModeConstants.Write);
                            if (cup.p_Cam[wsInd].iMonoType == 0 && ci8gAcq[iCamIndex] != null)
                                mImageFile[wsInd].Append(CogFlipRotateIm(ci8gAcq[iCamIndex],wsInd));  
                            else if (ci24ColorAcq[iCamIndex] != null)
                                mImageFile[wsInd].Append(CogFlipRotateIm(ci24ColorAcq[iCamIndex], wsInd));
                            mImageFile[wsInd].Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"GrabOneFrameCog (Job{wsInd}, Cam{iCamIndex})，错误信息：{ex.Message}");
            }
        }
        // 创建图像文件工具
        CogImageFileTool imageFileTool = new CogImageFileTool();
        private ICogImage ReadCogImage(string strImageFile)
        {
            try
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
            catch (Exception ex)
            {
                OnErrorOccurred($"GrabOneFrameCog (文件名{strImageFile})，错误信息：{ex.Message}");
                return null;
            }
        }
        
        /// <summary>保存所有光度立体法图像（多方向图+光度图）</summary>
        public void SaveImageForFXSurfaceAll()
        {
            strTime[0] = DateTime.Now.ToString("yyyyMMddHHmmssFF");
            string strImageFile;
            for (int i = 0; i < maxCameraNum; i++)
            {
                for (int j = 0; j <= 4; j++)
                {
                    strImageFile = strConSavePathS[i] + "\\" + cup.p_LI.strPrefix[i] + strTime[0] + "_0" + j.ToString() + ".jpg";
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
 
        /// <summary>导出当前图像到指定目录</summary>
        public bool ExportCurImage(string strExportPath, string[,] strFiles)
        {
            string strImageFile="";
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
            catch(Exception ex)
            {
                OnErrorOccurred($"ExportCurImage (文件名{strImageFile})，错误信息： {ex.Message}");
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
            int iCamIndex = cup.p_CD.camInd[wsInd];
            try
            {
                if (bListProcess[wsInd])
                {
                    GrabOneFrameByte(iCamIndex);
                    Thread.Sleep(tWait);
                    if (bRecord)
                    {
                        saveFileThread = new Thread(SaveImageFormSrcImage);
                        saveFileThread.Start();
                    }
                }
                return true;
            }
            catch(Exception ex)
            {
                OnErrorOccurred($"ExportCurImage (Job{wsInd}, Cam{iCamIndex})，错误信息： {ex.Message}");
                return false;
            }
        }

      

        /// <summary>保存原始字节图像为JPEG</summary>
        public void SaveImageFormSrcImage()
        {
            strTime[0] = DateTime.Now.ToString("yyyyMMddHHmmssFF");
            string strImageFile;
            for (int i = 0; i < maxCameraNum; i++)
            {
                int iCamIndex = cup.p_CD.camInd[i];
                strImageFile = strConSavePathS[i] + "\\" + cup.p_LI.strPrefix[i] + strTime[0] + "_00" + ".jpg";
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

        private bool IsBayerPixelFormat(MyCamera.MvGvspPixelType pixelType)
        {
            switch (pixelType)
            {
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG8:
                    return true;
                default:
                    return false;
            }
        }

        private void ConvertRgbToBgrInPlace(byte[] imageData, int width, int height)
        {
            int pixelDataLength = checked(width * height * 3);
            if (imageData == null || imageData.Length < pixelDataLength)
                throw new ArgumentException("RGB8帧长度小于图像尺寸要求");

            for (int index = 0; index < pixelDataLength; index += 3)
            {
                byte red = imageData[index];
                imageData[index] = imageData[index + 2];
                imageData[index + 2] = red;
            }
        }

        private Bitmap CreateBgrBitmap(byte[] imageData, int width, int height)
        {
            int sourceStride = checked(width * 3);
            int requiredLength = checked(sourceStride * height);
            if (imageData == null || imageData.Length < requiredLength)
                throw new ArgumentException("BGR8帧长度小于图像尺寸要求");

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            try
            {
                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                try
                {
                    for (int row = 0; row < height; row++)
                    {
                        IntPtr destination = IntPtr.Add(bitmapData.Scan0, row * bitmapData.Stride);
                        Marshal.Copy(imageData, row * sourceStride, destination, sourceStride);
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }
                return bitmap;
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }

        /// <summary>将 Bayer 格式字节数组转换为 BGR 字节数组</summary>
        private byte[] BayerToBGR(byte[] bayerData, int width, int height, MyCamera.MvGvspPixelType pixelType)
        {
            int requiredLength = checked(width * height);
            if (bayerData == null || bayerData.Length < requiredLength)
                throw new ArgumentException($"{pixelType}帧长度小于Bayer图像尺寸要求");

            using (Mat bayerMat = new Mat(height, width, MatType.CV_8UC1, bayerData))
            using (Mat bgrMat = new Mat())
            {
                ColorConversionCodes code;
                switch (pixelType)
                {
                    case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG8:
                        code = ColorConversionCodes.BayerBG2BGR;
                        break;
                    case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB8:
                        code = ColorConversionCodes.BayerGR2BGR;
                        break;
                    case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR8:
                        code = ColorConversionCodes.BayerGB2BGR;
                        break;
                    case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG8:
                        code = ColorConversionCodes.BayerRG2BGR;
                        break;
                    default:
                        throw new NotSupportedException($"不支持的Bayer像素格式: {pixelType}");
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
            else if (IsBayerPixelFormat(frameInfo.enPixelType))
            {
                byte[] bayer = new byte[frameLen];
                Marshal.Copy(bufferPtr, bayer, 0, frameLen);
                return BayerToBGR(bayer, width, height, frameInfo.enPixelType);
            }
            else
            {
                throw new NotSupportedException($"不支持的彩色像素格式: {frameInfo.enPixelType}");
            }
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
        private CogImage24PlanarColor CogFlipRotateImColor(CogImage24PlanarColor inputIm,int jobIndex)
        {
            try
            {
                switch (_rotateFlipTypes[jobIndex])
                {
                    case 0: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.None; break;
                    case 1: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate90Deg; break;
                    case 2: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate180Deg; break;
                    case 3: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate270Deg; break;
                    case 4: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Flip; break;
                    case 5: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate90Deg; break;
                    case 6: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate180Deg; break;
                    case 7: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate270Deg; break;
                    case 8: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate180Deg; break;
                    case 9: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate270Deg; break;
                    case 10: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Flip; break;
                    case 11: cogImFlipRotate[jobIndex].OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate90Deg; break;
                }
                imgTool[jobIndex].InputImage = inputIm;
                imgTool[jobIndex].Run();
                return (CogImage24PlanarColor)imgTool[jobIndex].OutputImage;
            }catch(Exception ex)
            {
                OnErrorOccurred($"CogFlipRotateImColor (Job{jobIndex}): {ex.Message}");
                return inputIm;
            }
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
