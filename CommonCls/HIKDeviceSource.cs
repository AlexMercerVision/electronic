using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using MvCamCtrl.NET;

namespace CommonCls
{
    /// <summary>
    /// 海康威视相机操作类，封装相机枚举、初始化、参数设置、图像采集等操作。
    /// 继承自 SDK 的 MyCamera 类。
    /// </summary>
    public class HIKVisionCamera : MyCamera
    {
        // 常量定义
        private const int MaxCameraNum = 5;               // 最大支持相机数量
        private const int ParamSetDelayMs = 10;           // 设置参数时的延迟（毫秒）

        // 字段
        private MyCamera _singleCamera;                   // 单相机模式使用的相机对象
        private readonly MyCamera[] _cameraArray = new MyCamera[MaxCameraNum]; // 多相机模式数组
        private int _cameraCount;                         // 实际初始化的相机数量
        private MyCamera.MV_CC_DEVICE_INFO_LIST _deviceList; // 设备列表

        // 帧信息（用于取流，当前未完全实现转换逻辑）
        private  MyCamera.MV_FRAME_OUT_INFO_EX _frameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();

        /// <summary>
        /// 构造函数，初始化单相机对象。
        /// </summary>
        public HIKVisionCamera()
        {
            _singleCamera = new MyCamera();
        }

        /// <summary>
        /// 枚举设备，获取相机数量。
        /// </summary>
        /// <returns>相机数量</returns>
        public uint CameraNum()
        {
            // 枚举 GigE 和 USB 设备
            MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref _deviceList);
            return _deviceList.nDeviceNum;
        }

        /// <summary>
        /// 枚举所有相机并返回可读信息列表。
        /// </summary>
        /// <returns>相机信息字符串列表，格式如 "GEV: 用户定义名称" 或 "U3V: 制造商 型号 序列号"</returns>
        public List<string> EnumDevices()
        {
            uint num = CameraNum();
            var cameraInfos = new List<string>();

            for (int i = 0; i < num; i++)
            {
                // 从设备列表指针获取设备信息结构
                var device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(
                    _deviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));

                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    var gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)MyCamera.ByteToStruct(
                        device.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                    string info = !string.IsNullOrEmpty(gigeInfo.chUserDefinedName)
                        ? $"GEV: {gigeInfo.chUserDefinedName}"
                        : $"GEV: {gigeInfo.chManufacturerName} {gigeInfo.chModelName}";
                    cameraInfos.Add(info);
                }
                else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    var usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)MyCamera.ByteToStruct(
                        device.SpecialInfo.stUsb3VInfo, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                    string info = !string.IsNullOrEmpty(usbInfo.chUserDefinedName)
                        ? $"U3V: {usbInfo.chUserDefinedName}"
                        : $"U3V: {usbInfo.chManufacturerName} {usbInfo.chModelName} {usbInfo.chSerialNumber}";
                    cameraInfos.Add(info);
                }
            }
            return cameraInfos;
        }

        /// <summary>
        /// 初始化单个相机（使用内部单相机对象）。
        /// </summary>
        /// <param name="index">设备索引</param>
        /// <returns>初始化成功返回相机对象，否则返回 null</returns>
        public MyCamera CameraInit(int index)
        {
            if (index < 0 || index >= _deviceList.nDeviceNum)
                return null;

            var device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(
                _deviceList.pDeviceInfo[index], typeof(MyCamera.MV_CC_DEVICE_INFO));

            // 如果单相机对象为空则创建（C# 7.3 兼容写法）
            if (_singleCamera == null)
                _singleCamera = new MyCamera();

            if (!InitializeCamera(_singleCamera, device))
                return null;

            return _singleCamera;
        }

        /// <summary>
        /// 初始化多个相机，并将状态写入 iStatus 数组。
        /// </summary>
        /// <param name="count">要初始化的相机数量</param>
        /// <param name="iStatus">输出每个相机的初始化状态（1成功，0失败）</param>
        /// <returns>相机对象数组</returns>
        public MyCamera[] CameraInit(int count, ref int[] iStatus)
        {
            return MultiCameraInit(count, ref iStatus);
        }

        /// <summary>
        /// 初始化多个相机（连续采集模式，触发关闭）。
        /// </summary>
        /// <param name="count">相机数量</param>
        /// <param name="iStatus">状态数组</param>
        /// <returns>相机对象数组</returns>
        public MyCamera[] MultiCameraInit(int count, ref int[] iStatus)
        {
            return InitializeMultipleCameras(count, false, ref iStatus);
        }

        /// <summary>
        /// 初始化多个相机（触发模式，此方法与 MultiCameraInit 当前实现相同，保留用于兼容）。
        /// </summary>
        public MyCamera[] MultiCameraInitTrigger(int count, ref int[] iStatus)
        {
            // 目前触发模式和连续模式设置相同，可根据需要修改
            return InitializeMultipleCameras(count, false, ref iStatus);
        }

        /// <summary>
        /// 初始化多个相机，不返回状态（失败时返回 null）。
        /// </summary>
        public MyCamera[] MultiCameraInit(int count)
        {
            var dummyStatus = new int[count];
            var result = InitializeMultipleCameras(count, false, ref dummyStatus);
            // 如果有任何一个失败，返回 null（保持原始行为）
            for (int i = 0; i < count; i++)
            {
                if (dummyStatus[i] == 0)
                    return null;
            }
            return result;
        }

        /// <summary>
        /// 核心多相机初始化逻辑。
        /// </summary>
        /// <param name="count">要初始化的相机数量</param>
        /// <param name="triggerMode">是否设置为触发模式（预留）</param>
        /// <param name="status">状态输出数组</param>
        /// <returns>相机对象数组</returns>
        private MyCamera[] InitializeMultipleCameras(int count, bool triggerMode, ref int[] status)
        {
            if (count > MaxCameraNum)
                throw new ArgumentOutOfRangeException(nameof(count), $"相机数量不能超过 {MaxCameraNum}");

            _cameraCount = count;
            for (int i = 0; i < count; i++)
            {
                status[i] = 0; // 默认失败
                var device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(
                    _deviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));

                if (_cameraArray[i] == null)
                    _cameraArray[i] = new MyCamera();

                if (InitializeCamera(_cameraArray[i], device))
                {
                    status[i] = 1;
                }
            }
            return _cameraArray;
        }

        /// <summary>
        /// 初始化单个相机的通用方法：创建设备、打开设备、设置网络包大小、设置采集模式和触发模式。
        /// </summary>
        /// <param name="camera">相机对象</param>
        /// <param name="device">设备信息</param>
        /// <returns>是否成功</returns>
        private bool InitializeCamera(MyCamera camera, MyCamera.MV_CC_DEVICE_INFO device)
        {
            // 创建设备
            int ret = camera.MV_CC_CreateDevice_NET(ref device);
            if (ret != MyCamera.MV_OK)
                return false;

            // 打开设备
            ret = camera.MV_CC_OpenDevice_NET();
            if (ret != MyCamera.MV_OK)
            {
                camera.MV_CC_DestroyDevice_NET();
                return false;
            }

            // 对 GigE 相机设置最佳包大小
            if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                int packetSize = camera.MV_CC_GetOptimalPacketSize_NET();
                if (packetSize > 0)
                {
                    camera.MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)packetSize);
                }
            }

            // 设置采集模式为连续采集，触发模式关闭
            camera.MV_CC_SetEnumValue_NET("AcquisitionMode",
                (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
            camera.MV_CC_SetEnumValue_NET("TriggerMode",
                (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);

            return true;
        }

        /// <summary>
        /// 销毁单相机对象，释放资源。
        /// </summary>
        public void DestroyCamera()
        {
            if (_singleCamera != null)
            {
                _singleCamera.MV_CC_StopGrabbing_NET();
                _singleCamera.MV_CC_CloseDevice_NET();
                _singleCamera.MV_CC_DestroyDevice_NET();
                _singleCamera = null;
            }
        }

        /// <summary>
        /// 检查指定索引的相机是否仍处于连接状态。
        /// </summary>
        public bool IsCamConn(int index)
        {
            if (index < 0 || index >= _cameraCount || _cameraArray[index] == null)
                return false;
            return _cameraArray[index].MV_CC_IsDeviceConnected_NET();
        }

        /// <summary>
        /// 销毁所有已初始化的多相机对象。
        /// </summary>
        public void MultiDestroyCamera()
        {
            for (int i = 0; i < _cameraCount; i++)
            {
                if (_cameraArray[i] != null)
                {
                    _cameraArray[i].MV_CC_StopGrabbing_NET();
                    _cameraArray[i].MV_CC_CloseDevice_NET();
                    _cameraArray[i].MV_CC_DestroyDevice_NET();
                    _cameraArray[i] = null;
                }
            }
            _cameraCount = 0;
        }

        #region 参数设置与获取

        /// <summary>
        /// 设置像素格式。
        /// </summary>
        /// <param name="type">0: Mono8, 1: RGB8_Packed</param>
        /// <param name="cam">相机对象</param>
        /// <returns>成功返回 true</returns>
        public bool SetPixelFormat(ref int type, MyCamera cam)
        {
            uint pixelType;
            switch (type)
            {
                case 0:
                    pixelType = (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8;
                    break;
                case 1:
                    pixelType = (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed;
                    break;
                default:
                    pixelType = (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8;
                    break;
            }
            int ret = cam.MV_CC_SetPixelFormat_NET(pixelType);
            return ret == MyCamera.MV_OK;
        }

        /// <summary>
        /// 设置分辨率（宽度和高度）。
        /// </summary>
        public bool SetResolution(ref uint width, ref uint height, MyCamera cam)
        {
            var range = new MyCamera.MVCC_INTVALUE();
            cam.MV_CC_GetWidth_NET(ref range);
            width = Clamp(width, range.nMin, range.nMax);
            int retW = cam.MV_CC_SetWidth_NET(width);

            cam.MV_CC_GetHeight_NET(ref range);
            height = Clamp(height, range.nMin, range.nMax);
            int retH = cam.MV_CC_SetHeight_NET(height);

            return retW == MyCamera.MV_OK && retH == MyCamera.MV_OK;
        }

        /// <summary>
        /// 设置亮度（关闭自动亮度）。
        /// </summary>
        public bool SetBrightness(ref uint value, MyCamera cam)
        {
            var range = new MyCamera.MVCC_INTVALUE();
            cam.MV_CC_GetBrightness_NET(ref range);
            value = Clamp(value, range.nMin, range.nMax);
            cam.MV_CC_SetEnumValue_NET("BrightnessAuto", 0);
            int ret = cam.MV_CC_SetBrightness_NET(value);
            return ret == MyCamera.MV_OK;
        }

        /// <summary>
        /// 设置锐度（关闭自动锐度）。
        /// </summary>
        public bool SetSharpness(ref uint value, MyCamera cam)
        {
            var range = new MyCamera.MVCC_INTVALUE();
            cam.MV_CC_GetSharpness_NET(ref range);
            value = Clamp(value, range.nMin, range.nMax);
            cam.MV_CC_SetEnumValue_NET("SharpnessAuto", 0);
            int ret = cam.MV_CC_SetSharpness_NET(value);
            return ret == MyCamera.MV_OK;
        }

        /// <summary>
        /// 设置伽马值（关闭自动伽马）。
        /// </summary>
        public bool SetGamma(ref float value, MyCamera cam)
        {
            var range = new MyCamera.MVCC_FLOATVALUE();
            cam.MV_CC_GetGamma_NET(ref range);
            value = Clamp(value, range.fMin, range.fMax);
            cam.MV_CC_SetEnumValue_NET("GammaAuto", 0);
            int ret = cam.MV_CC_SetGamma_NET(value);
            return ret == MyCamera.MV_OK;
        }

        /// <summary>
        /// 设置增益（关闭自动增益）。
        /// </summary>
        public bool SetGain(ref float value, MyCamera cam)
        {
            var range = new MyCamera.MVCC_FLOATVALUE();
            cam.MV_CC_GetGain_NET(ref range);
            value = Clamp(value, range.fMin, range.fMax);
            cam.MV_CC_SetEnumValue_NET("GainAuto", 0);
            int ret = cam.MV_CC_SetGain_NET(value);
            return ret == MyCamera.MV_OK;
        }

        /// <summary>
        /// 设置曝光时间。若 value <= 0 则开启自动曝光，否则设置手动曝光值。
        /// </summary>
        public bool SetExposure(ref float value, MyCamera cam)
        {
            var range = new MyCamera.MVCC_FLOATVALUE();
            cam.MV_CC_GetExposureTime_NET(ref range);
            int ret = MyCamera.MV_OK;

            if (value <= 0)
            {
                cam.MV_CC_SetEnumValue_NET("ExposureAuto",
                    (uint)MyCamera.MV_CAM_EXPOSURE_AUTO_MODE.MV_EXPOSURE_AUTO_MODE_CONTINUOUS);
                cam.MV_CC_GetExposureTime_NET(ref range);
                value = range.fCurValue;
            }
            else
            {
                value = Clamp(value, range.fMin, range.fMax);
                cam.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                ret = cam.MV_CC_SetExposureTime_NET(value);
            }
            return ret == MyCamera.MV_OK;
        }

        /// <summary>
        /// 批量设置相机参数。
        /// </summary>
        public bool SetCamParam(CameraParam param, MyCamera cam)
        {
            bool success = true;
            success &= SetExposure(ref param.iExposeTime, cam);
            success &= SetGain(ref param.dGain, cam);
            success &= SetSharpness(ref param.iSharp, cam);
            success &= SetGamma(ref param.dGamma, cam);
            success &= SetPixelFormat(ref param.iMonoType, cam);
            success &= SetBrightness(ref param.iBrightness, cam);
            success &= SetResolution(ref param.iWidth, ref param.iHeight, cam);
            return success;
        }
        // 设置参数
        public void SetParam(float gain, float exposure, ref string gainString, ref string exposureString, MyCamera cam)
        {


            // 曝光时间
            // 获取当前Exposure、最大Exposure、最小Exposure
            MyCamera.MVCC_FLOATVALUE exposureValue = new MyCamera.MVCC_FLOATVALUE();
            cam.MV_CC_GetExposureTime_NET(ref exposureValue);
            //float exposure = exposureValue.fCurValue;
            float maxExposure = exposureValue.fMax;
            float minExposure = exposureValue.fMin;
            // 判断是否处于最小到最大值之间
            if (exposure <= 0)
            {
                cam.MV_CC_SetEnumValue_NET("ExposureAuto", (uint)MV_CAM_EXPOSURE_AUTO_MODE.MV_EXPOSURE_AUTO_MODE_CONTINUOUS);
            }
            else
            {
                if (exposure < minExposure && exposure > 0)
                {
                    //**  MessageBox.Show("小于最小值，已修改为最小曝光时间值！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    exposure = minExposure;
                }
                else if (exposure > maxExposure)
                {
                    //** MessageBox.Show("大于最大值，已修改为最大曝光时间值！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    exposure = maxExposure;
                }

                // 关闭自动设置
                cam.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                // 设置ExposureTime
                cam.MV_CC_SetFloatValue_NET("ExposureTime", exposure);
                exposureString = exposure.ToString("F0");
            }
        }
        /// <summary>
        /// 批量获取相机参数。
        /// </summary>
        public bool GetCamParam(CameraParam param, MyCamera cam)
        {
            int ret = MyCamera.MV_OK;
            var intVal = new MyCamera.MVCC_INTVALUE();
            var floatVal = new MyCamera.MVCC_FLOATVALUE();

            ret += cam.MV_CC_GetExposureTime_NET(ref floatVal);
            param.iExposeTime = floatVal.fCurValue;

            ret += cam.MV_CC_GetGain_NET(ref floatVal);
            param.dGain = floatVal.fCurValue;

            ret += cam.MV_CC_GetGamma_NET(ref floatVal);
            param.dGamma = floatVal.fCurValue;

            ret += cam.MV_CC_GetBrightness_NET(ref intVal);
            param.iBrightness = intVal.nCurValue;

            ret += cam.MV_CC_GetSharpness_NET(ref intVal);
            param.iSharp = intVal.nCurValue;

            ret += cam.MV_CC_GetWidth_NET(ref intVal);
            param.iWidth = intVal.nCurValue;

            ret += cam.MV_CC_GetHeight_NET(ref intVal);
            param.iHeight = intVal.nCurValue;

            ret += GetPixelFormat(ref param.iMonoType, cam);
            return ret == MyCamera.MV_OK;
        }

        /// <summary>
        /// 获取当前分辨率。
        /// </summary>
        public void GetResolution(ref uint width, ref uint height, MyCamera cam)
        {
            var intVal = new MyCamera.MVCC_INTVALUE();
            cam.MV_CC_GetWidth_NET(ref intVal);
            width = intVal.nCurValue;
            cam.MV_CC_GetHeight_NET(ref intVal);
            height = intVal.nCurValue;
        }

        /// <summary>
        /// 获取像素格式，输出 0:Mono8, 1:RGB8_Packed。
        /// </summary>
        public int GetPixelFormat(ref int format, MyCamera cam)
        {
            var enumVal = new MyCamera.MVCC_ENUMVALUE();
            int ret = cam.MV_CC_GetPixelFormat_NET(ref enumVal);
            uint curVal = enumVal.nCurValue;
            if (curVal == (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8)
                format = 0;
            else if (curVal == (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed)
                format = 1;
            else
                format = 0;
            return ret;
        }

        /// <summary>
        /// 获取 ROI 区域参数（OffsetX, OffsetY, Width, Height）。
        /// </summary>
        public bool GetROIParam(CameraParam param, MyCamera cam)
        {
            try
            {
                var intVal = new MyCamera.MVCC_INTVALUE();
                int ret = MyCamera.MV_OK;
                ret += cam.MV_CC_GetIntValue_NET("OffsetX", ref intVal);
                param.iROI[0] =intVal.nCurValue;
                ret += cam.MV_CC_GetIntValue_NET("OffsetY", ref intVal);
                param.iROI[1] = intVal.nCurValue;
                ret += cam.MV_CC_GetIntValue_NET("Width", ref intVal);
                param.iROI[2] = intVal.nCurValue;
                ret += cam.MV_CC_GetIntValue_NET("Height", ref intVal);
                param.iROI[3] = intVal.nCurValue;
                return ret == MyCamera.MV_OK;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 设置 ROI 区域。
        /// </summary>
        public bool SetROIParam(CameraParam param, MyCamera cam)
        {
            try
            {
                int ret = MyCamera.MV_OK;
                ret += cam.MV_CC_SetIntValue_NET("OffsetX", (uint)param.iROI[0]);
                Thread.Sleep(ParamSetDelayMs);
                ret += cam.MV_CC_SetIntValue_NET("Width", (uint)param.iROI[2]);
                Thread.Sleep(ParamSetDelayMs);
                ret += cam.MV_CC_SetIntValue_NET("OffsetY", (uint)param.iROI[1]);
                Thread.Sleep(ParamSetDelayMs);
                ret += cam.MV_CC_SetIntValue_NET("Height", (uint)param.iROI[3]);
                return ret == MyCamera.MV_OK;
            }
            catch
            {
                return false;
            }
        }

        // 辅助数值限定方法（C# 7.3 兼容）
        private static uint Clamp(uint value, uint min, uint max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        #endregion

        #region 采集控制

        /// <summary>
        /// 开始指定索引相机的采集。
        /// </summary>
        public bool StartGrabbing(int index)
        {
            try
            {
                MyCamera cam = null;
                if (index >= 0 && index < _cameraCount)
                    cam = _cameraArray[index];
                else
                    cam = _singleCamera;

                if (cam == null) return false;
                _frameInfo.nFrameLen = 0;
                _frameInfo.enPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;
                cam.MV_CC_StartGrabbing_NET();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 开始单相机采集。
        /// </summary>
        public void StartGrabbing()
        {
            _frameInfo.nFrameLen = 0;
            _frameInfo.enPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;
            if (_singleCamera != null)
                _singleCamera.MV_CC_StartGrabbing_NET();
        }

        /// <summary>
        /// 停止指定索引相机的采集。
        /// </summary>
        public void StopGrabbing(int index)
        {
            MyCamera cam = null;
            if (index >= 0 && index < _cameraCount)
                cam = _cameraArray[index];
            cam?.MV_CC_StopGrabbing_NET();
        }

        /// <summary>
        /// 停止单相机采集。
        /// </summary>
        public void StopGrabbing()
        {
            _singleCamera?.MV_CC_StopGrabbing_NET();
        }

        #endregion

        #region 图像处理（旋转/镜像）

        /// <summary>
        /// 旋转图像。
        /// </summary>
        public bool HikRotateIm(MyCamera cam, MyCamera.MV_CC_ROTATE_IMAGE_PARAM rotateParam)
        {
            cam.MV_CC_RotateImage_NET(ref rotateParam);
            return true;
        }

        /// <summary>
        /// 镜像翻转图像。
        /// </summary>
        public bool HikFlipIm(MyCamera cam, MyCamera.MV_CC_FLIP_IMAGE_PARAM flipParam)
        {
            cam.MV_CC_FlipImage_NET(ref flipParam);
            return true;
        }

        #endregion

        // 保留原始未使用的 SetIndexCamera 方法
        public void SetIndexCamera(MyCamera camera, int index)
        {
            if (index >= 0 && index < MaxCameraNum)
                _cameraArray[index] = camera;
        }
    }

    
}