using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Cognex.VisionPro;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.ImageProcessing;
using HalconDotNet;
using MvCamCtrl.NET;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace CommonCls
{
    /// <summary>
    /// 图像采集 FIFO 类，支持多相机同步采集、光度立体法重构、图像格式转换及文件保存。
    /// 重构后依赖 CameraManager 进行底层相机操作，不直接操作 SDK。
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

        #region 相机相关字段（替换为 CameraManager）

        /// <summary>相机管理器（底层相机操作）</summary>
        private CameraManager _cameraManager;

        /// <summary>相机状态（由主程序传入，保持兼容）</summary>
        private CameraStatus _cameraStatus;

        /// <summary>旋转镜像类型（0-无，1-90°，2-180°，3-270°，4-翻转，5-翻转+90°等）</summary>
        private int _rotateFlipType = 0;

        /// <summary>输出帧信息结构体（保留用于兼容，实际数据从 CameraManager 获取）</summary>
        public MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();

        /// <summary>每个相机的载荷大小（从 CameraManager 获取）</summary>
        private UInt32[] nPayloadSizeS = new UInt32[maxCameraNum];

        /// <summary>相机像素格式字符串（如 "Mono8", "BayerRG8", "RGB8Packed"）</summary>
        public string[] strPixelFormat = new string[maxCameraNum];

        /// <summary>是否为彩色相机（根据像素格式自动判断）</summary>
        public bool[] bIsColorCam = new bool[maxCameraNum];

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

        /// <summary>错误图像保存路径数组</summary>
        public string[] strErrPathS = new string[maxCameraNum];

        /// <summary>用户参数（配置）</summary>
        private CUserParam cup = new CUserParam();

        /// <summary>串口通信辅助类</summary>
        public CSerialDef csd = new CSerialDef();

        /// <summary>深度学习检测结果</summary>
        public List<CogRectangleAffine>[] AL_det_result = new List<CogRectangleAffine>[maxCameraNum];

        /// <summary>Cognex图像处理工具</summary>
        private CogIPOneImageTool imgTool = new CogIPOneImageTool();

        /// <summary>Cognex旋转镜像工具</summary>
        private CogIPOneImageFlipRotate cogImFlipRotate = new CogIPOneImageFlipRotate();

        /// <summary>Cognex格式图像保存工具</summary>
        CogImageFile mImageFile = new CogImageFile();

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
        {
            CogFlipRotateImInit();
        }
        #endregion

        #region 初始化与依赖注入

        /// <summary>
        /// 设置相机管理器（在外部初始化后调用）
        /// </summary>
        public void SetCameraManager(CameraManager cameraManager)
        {
            _cameraManager = cameraManager;
            if (_cameraManager != null)
            {
                // 同步载荷大小缓存
                for (int i = 0; i < maxCameraNum; i++)
                {
                    nPayloadSizeS[i] = _cameraManager.GetPayloadSize(i);
                }
            }
        }

        /// <summary>
        /// 获取获取相机状态的委托（用于实时状态查询）
        /// </summary>
        private Func<CameraStatus> _getCameraStatusFunc = null;

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

        /// <summary>错误图像保存路径数组赋值与读取</summary>
        public string[] _strErrPathS
        {
            get { return strErrPathS; }
            set { strErrPathS = value; }
        }

        #endregion

        #region 相机控制（自动曝光、连接检测）- 委托给 CameraManager

        /// <summary>关闭指定相机的自动曝光</summary>
        public void CloseExposureAuto(int iCamInd)
        {
            _cameraManager?.SetExposureAuto(iCamInd, false);
        }

        /// <summary>关闭所有已连接相机的自动曝光</summary>
        public void CloseAllExposureAuto()
        {
            for (int i = 0; i < maxCameraNum; i++)
            {
                _cameraManager?.SetExposureAuto(i, false);
            }
        }

        /// <summary>打开指定相机的自动曝光</summary>
        public void OpenExposureAuto(int iCamIndex)
        {
            _cameraManager?.SetExposureAuto(iCamIndex, true);
        }

        /// <summary>打开所有已连接相机的自动曝光</summary>
        public void OpenAllExposureAuto()
        {
            for (int i = 0; i < maxCameraNum; i++)
            {
                _cameraManager?.SetExposureAuto(i, true);
            }
        }

        /// <summary>检查指定索引相机是否已连接</summary>
        public bool IsCamConn(int iCamIndex)
        {
            return _cameraManager?.IsCameraConnected(iCamIndex) ?? false;
        }

        #endregion

        #region 图像采集（实时）- 委托给 CameraManager

        /// <summary>采集单帧图像转Halcon格式，用于光度立体法（Halcon HObject格式）</summary>
        public HObject GrabOneFrameHObj(int iCamIndex)
        {
            return _cameraManager?.GrabOneFrameHObject(iCamIndex);
        }

        /// <summary>采集单帧灰度图像转Cog格式，VP算法使用</summary>
        public CogImage8Grey GrabOneFrameCog(int iCamIndex)
        {
            CogImage8Grey img = _cameraManager?.GrabOneFrameMono(iCamIndex);
            if (img != null)
            {
                // 应用旋转镜像
                img = (CogImage8Grey)CogFlipRotateIm(img);
                ci8gAcq[iCamIndex] = img;
            }
            return img;
        }

        /// <summary>采集单帧彩色图像转Cog格式，VP算法使用</summary>
        public CogImage24PlanarColor GrabOneFrameCogColor(int iCamIndex)
        {
            CogImage24PlanarColor img = _cameraManager?.GrabOneFrameColor(iCamIndex);
            if (img != null)
            {
                img = (CogImage24PlanarColor)CogFlipRotateIm(img);
                ci24ColorAcq[iCamIndex] = img;
            }
            return img;
        }

        /// <summary>采集一帧Cog格式图像（根据工位）</summary>
        public bool GrabOneFrameCogFormat(bool bRecord, bool bListProcess, int wsInd, int iCamIndex)
        {
            if (!bListProcess) return false;
            CameraStatus currentStatus = GetCurrentCameraStatus();
            if (currentStatus.iStatus[iCamIndex] < 2) return false;

            try
            {
                if (cup.p_Cam[iCamIndex].iMonoType == 0)
                    GrabOneFrameCog(iCamIndex);
                else
                    GrabOneFrameCogColor(iCamIndex);

                if (bRecord)
                {
                    SaveCogImage(wsInd, iCamIndex);
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
            byte[] data = _cameraManager?.GrabOneFrameBytes(iCamIndex);
            if (data != null)
            {
                bImgSrcData[iCamIndex] = data;
                return true;
            }
            return false;
        }

        /// <summary>批量采集所有相机图像（CogImage8Grey数组）</summary>
        public CogImage8Grey[] GrabOneFrameCog()
        {
            for (int iCamIndex = 0; iCamIndex < maxCameraNum; iCamIndex++)
            {
                CameraStatus currentStatus = GetCurrentCameraStatus();
                if (currentStatus.iStatus[iCamIndex] < 2) continue;

                CogImage8Grey img = _cameraManager?.GrabOneFrameMono(iCamIndex);
                if (img != null)
                {
                    img = (CogImage8Grey)CogFlipRotateIm(img);
                    ci8gAcq[iCamIndex] = img;
                }
            }
            return ci8gAcq;
        }

        #endregion

        #region 图像采集（从文件）- 保留原有实现

        /// <summary>从文件读取一组灰度图像（所有相机）</summary>
        public void GrabOneFrameCogFromFile(int iFileInd, bool[] bListProcess)
        {
            int tWait = 5;
            int ind = iFileInd * 6;
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
                        ci = ReadCogImage(strFiles[wsInd][ind]);
                        iCamIndex = cup.p_CD.camInd[wsInd];
                        if (ci is CogImage24PlanarColor)
                        {
                            ci24ColorAcq[iCamIndex] = (CogImage24PlanarColor)ci;
                            cup.p_Cam[iCamIndex].iMonoType = 1;
                        }
                        else
                        {
                            ci8gAcq[iCamIndex] = (CogImage8Grey)ci;
                            cup.p_Cam[iCamIndex].iMonoType = 0;
                        }
                        Thread.Sleep(tWait);
                    }
                }
            }
            catch
            {
                return;
            }
        }

        /// <summary>从文件读取单帧图像（指定工位）</summary>
        public int GrabOneFrameCogFromFile(int iFileInd, bool bListProcess, int wsInd, int iCamIndex)
        {
            int tWait = 5;
            try
            {
                if (bListProcess)
                {
                    ICogImage ci = ReadCogImage(strFiles[wsInd][iFileInd]);
                    if (ci is CogImage24PlanarColor)
                    {
                        ci24ColorAcq[iCamIndex] = (CogImage24PlanarColor)ci;
                        cup.p_Cam[iCamIndex].iMonoType = 1;
                        return 1;
                    }
                    else
                    {
                        ci8gAcq[wsInd] = (CogImage8Grey)ci;
                        cup.p_Cam[wsInd].iMonoType = 0;
                        return 0;
                    }
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

        /// <summary>从文件读取单帧图像（带源数据）</summary>
        public int GrabOneFrameCogFromFileWithSrcData(int iFileInd, bool bListProcess, int wsInd, int iCamIndex)
        {
            try
            {
                if (bListProcess)
                {
                    ICogImage ci = ReadCogImage(strFiles[wsInd][iFileInd]);
                    if (ci is CogImage24PlanarColor)
                    {
                        ci24ColorAcq[iCamIndex] = (CogImage24PlanarColor)ci;
                        cup.p_Cam[iCamIndex].iMonoType = 1;
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

        /// <summary>从文件读取单帧图像（指定工位和物理相机）</summary>
        public int GrabOneFrameCogFromFile(int iFileInd, bool[] bListProcess, int wsInd, int iCamIndex)
        {
            try
            {
                if (bListProcess[wsInd])
                {
                    ICogImage ci = ReadCogImage(strFiles[wsInd][iFileInd]);
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

        /// <summary>从文件读取单帧灰度图像数组</summary>
        public CogImage8Grey[] GrabOneFrameCogFromFileSingle(int iFileInd, bool bListProcess, int wsInd, int iCamIndex)
        {
            try
            {
                if (bListProcess)
                {
                    HObject objTmp;
                    HOperatorSet.ReadImage(out objTmp, strFiles[wsInd][iFileInd]);
                    ci8gAcq[iCamIndex] = HObj2CogImage(objTmp);
                    Thread.Sleep(5);
                }
            }
            catch
            {
                return null;
            }
            return ci8gAcq;
        }

        /// <summary>从标定文件读取图像</summary>
        public CogImage8Grey[] GrabOneFrameCogFromFileCalib(int wsInd, int iCamIndex)
        {
            try
            {
                ICogImage ci = ReadCogImage(cup.p_Paf.strExportPath);
                ci8gAcq[iCamIndex] = (CogImage8Grey)ci;
                cup.p_Cam[iCamIndex].iMonoType = 0;
            }
            catch
            {
                return null;
            }
            return ci8gAcq;
        }

        #endregion

        #region 图像旋转镜像（Cognex）- 保留原有实现

        public void CogFlipRotateImInit()
        {
            ICogIPOneImageOperatorParams ip = (ICogIPOneImageOperatorParams)cogImFlipRotate;
            imgTool.Operators.Add(ip);
        }

        private ICogImage CogFlipRotateIm(ICogImage inputIm)
        {
            switch (_rotateFlipType)
            {
                case 0: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.None; break;
                case 1: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate90Deg; break;
                case 2: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate180Deg; break;
                case 3: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate270Deg; break;
                case 4: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Flip; break;
                case 5: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate90Deg; break;
                case 6: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate180Deg; break;
                case 7: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate270Deg; break;
                case 8: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate180Deg; break;
                case 9: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate270Deg; break;
                case 10: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Flip; break;
                case 11: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate90Deg; break;
                default: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.None; break;
            }
            imgTool.InputImage = inputIm;
            imgTool.Run();
            return imgTool.OutputImage;
        }

        #endregion

        #region 格式转换（HObject ↔ CogImage ↔ byte[]）- 保留原有实现

        private byte[] HObj2Bytes(HObject image)
        {
            HOperatorSet.GetImagePointer1(image, out HTuple pointer, out HTuple type, out HTuple width, out HTuple height);
            byte[] by = new byte[width * height];
            Marshal.Copy(pointer[0], by, 0, width * height);
            return by;
        }

        private CogImage8Grey HObj2CogImage(HObject image)
        {
            CogImage8Grey cogImage8Grey = new CogImage8Grey();
            HOperatorSet.GetImagePointer1(image, out HTuple hpoint, out HTuple type, out HTuple width, out HTuple height);
            CogImage8Root cogImage8Root = new CogImage8Root();
            cogImage8Root.Initialize((Int32)width, (Int32)height, (IntPtr)hpoint, (Int32)width, null);
            cogImage8Grey.SetRoot(cogImage8Root);
            return cogImage8Grey.Copy();
        }

        public bool IntPtrToByteArray(IntPtr ptr, int length, ref byte[] byteArray)
        {
            if (ptr == IntPtr.Zero || length <= 0)
                return false;
            if (byteArray == null || byteArray.Length != length)
                byteArray = new byte[length];
            Marshal.Copy(ptr, byteArray, 0, length);
            return true;
        }

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

        private byte[] BitmapToBytes(Bitmap bitmap, ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, format);
                return ms.ToArray();
            }
        }

        public Bitmap IntPtrToBitmap(IntPtr ptr, int width, int height)
        {
            int stride = (width * 3 + 3) & ~3;
            return new Bitmap(width, height, stride, PixelFormat.Format24bppRgb, ptr);
        }

        #endregion

        #region 光度立体法（初始化、采集、重构）- 保留原有实现，仅替换相机采集

        public void InitFX()
        {
            Slants = new HTuple();
            Tilts = new HTuple();
            Slants1 = new HTuple();
            Tilts1 = new HTuple();
            Slants2 = new HTuple();
            Tilts2 = new HTuple();

            ResultType = new HTuple();
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
            }
            catch
            {
                Slants1[0] = 45; Slants1[1] = 45; Slants1[2] = 45;
                Slants2[0] = 45; Slants2[1] = 45; Slants2[2] = 45;
                Tilts1[0] = 0; Tilts1[1] = 90; Tilts1[2] = 180;
                Tilts2[0] = 0; Tilts2[1] = 90; Tilts2[2] = 180;
            }
        }

        public bool FXSurfaceAll(bool bRecord, bool[] bListProcess)
        {
            int tWait = 5;
            try
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

        public bool FXSurfaceAll(bool bRecord, bool[] bListProcess, int wsInd, int iCamIndex)
        {
            int tWait = 5;
            try
            {
                if (bListProcess[wsInd])
                {
                    hObjFXA[iCamIndex, 0] = GrabOneFrameHObj(iCamIndex);
                    {
                        ci8gAcq[iCamIndex] = HObj2CogImage(hObjFXA[iCamIndex, 0]);
                        ci8gAcq[iCamIndex] = (CogImage8Grey)CogFlipRotateIm(ci8gAcq[iCamIndex]);
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

        #endregion

        #region 文件保存与删除 - 保留原有实现

        private void SaveCogImage(int wsInd, int iCamIndex)
        {
            string strImageFile = strConSavePathS[wsInd] + "\\" + cup.p_LI.strPrefix[wsInd] + strTime + "_00.jpg";
            if (cup.p_CD.bListProcess[wsInd])
            {
                mImageFile.Open(strImageFile, CogImageFileModeConstants.Write);
                if (cup.p_Cam[wsInd].iMonoType == 0 && ci8gAcq[iCamIndex] != null)
                    mImageFile.Append(ci8gAcq[iCamIndex]);
                else if (ci24ColorAcq[iCamIndex] != null)
                    mImageFile.Append(ci24ColorAcq[iCamIndex]);
                mImageFile.Close();
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

        public void SaveImageForCam(int ind, int dw)
        {
            strTime = DateTime.Now.ToString("yyyyMMddHHmmssFF");
            string strImageFile = strConSavePathS[ind] + "\\" + cup.p_LI.strPrefix[ind] + strTime + "_0" + dw.ToString() + ".jpg";
            if (cup.p_CD.bListProcess[ind] && hObjFXA[ind, 0] != null)
            {
                HOperatorSet.WriteImage(hObjFXA[ind, 0], "jpg", 0, strImageFile);
            }
        }

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

        public bool DelOneGroupImageFile(bool bLive)
        {
            if (bLive)
            {
                DelOneGroupImageFile();
            }
            return true;
        }

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
                    SaveByteArrayAsJpg_OpenCV(bImgSrcData[iCamIndex], (int)stFrameInfo.nWidth, (int)stFrameInfo.nHeight, 1, strImageFile);
                }
            }
        }

        #endregion

        #region 彩色图像相关 - 保留原有实现

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

        private byte[] BayerToBGR(byte[] bayerData, int width, int height, string bayerType)
        {
            using (Mat bayerMat = new Mat(height, width, MatType.CV_8UC1, bayerData))
            using (Mat bgrMat = new Mat())
            {
                ColorConversionCodes code = ColorConversionCodes.BayerBG2BGR;
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

        private CogImage24PlanarColor CogFlipRotateImColor(CogImage24PlanarColor inputIm)
        {
            switch (_rotateFlipType)
            {
                case 0: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.None; break;
                case 1: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate90Deg; break;
                case 2: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate180Deg; break;
                case 3: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate270Deg; break;
                case 4: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Flip; break;
                case 5: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate90Deg; break;
                case 6: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate180Deg; break;
                case 7: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate270Deg; break;
                case 8: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate180Deg; break;
                case 9: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate270Deg; break;
                case 10: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Flip; break;
                case 11: cogImFlipRotate.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.FlipAndRotate90Deg; break;
            }
            imgTool.InputImage = inputIm;
            imgTool.Run();
            return (CogImage24PlanarColor)imgTool.OutputImage;
        }

        #endregion

        #region 释放资源
        public void Dispose()
        {
            // 释放 Halcon 图像
            for (int i = 0; i < maxCameraNum; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    if (hObjFXA[i, j] != null)
                    {
                        hObjFXA[i, j].Dispose();
                        hObjFXA[i, j] = null;
                    }
                }
                if (ci8gAcq[i] != null)
                {
                    ci8gAcq[i].Dispose();  // 公共方法
                    ci8gAcq[i] = null;
                }
                if (ci24ColorAcq[i] != null)
                {
                    ci24ColorAcq[i].Dispose();
                    ci24ColorAcq[i] = null;
                }
                if (ci8gAcqFX[i] != null)
                {
                    ci8gAcqFX[i].Dispose();
                    ci8gAcqFX[i] = null;
                }
                if (hColorImg[i] != null)
                {
                    hColorImg[i].Dispose();
                    hColorImg[i] = null;
                }
            }
            if (hObj != null) hObj.Dispose();
            if (HeightField != null) HeightField.Dispose();
            if (Gradient != null) Gradient.Dispose();
            if (Albedo != null) Albedo.Dispose();
            if (MeanCurvature != null) MeanCurvature.Dispose();

            // Cognex 工具调用 Dispose()（公共方法）
            if (cogImFlipRotate != null) cogImFlipRotate.Dispose();
            if (imgTool != null) imgTool.Dispose();
            if (mImageFile != null) mImageFile.Dispose();
        }
        #endregion
    }
}