using Cognex.VisionPro;
using HalconDotNet;
using MvCamCtrl.NET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace CommonCls
{
    /// <summary>
    /// 相机管理器，封装所有相机操作（枚举、初始化、参数设置、采集控制、状态管理、缓冲区管理）。
    /// 支持多相机（最多 MaxCameraCount 个），每个相机独立管理。
    /// </summary>
    public class CameraManager : IDisposable
    {
        private const int MaxCameraCount = 2;           // 最大支持的物理相机数量
        private const int ParamSetDelayMs = 10;         // 参数设置后的延迟（毫秒）
        private const int OneFrameTimeoutMs = 10000;    // 单帧采集超时（毫秒）

        // 底层 SDK 封装
        private HIKVisionCamera _hikCamera = new HIKVisionCamera();

        // 相机对象数组
        private MyCamera[] _cameras = new MyCamera[MaxCameraCount];

        // 设备信息列表
        private MyCamera.MV_CC_DEVICE_INFO_LIST _deviceList;

        // 实际枚举到的相机数量
        private uint _cameraCount = 0;

        // 每个相机的载荷大小（图像数据字节数）
        private UInt32[] _payloadSizes = new UInt32[MaxCameraCount];

        // 每个相机的驱动缓冲区指针（非托管内存）
        private IntPtr[] _bufferPtrs = new IntPtr[MaxCameraCount];
        private UInt32[] _bufferSizes = new UInt32[MaxCameraCount];

        // 相机参数配置（由外部传入，通常从 cup.p_Cam 读取）
        private CameraParam[] _cameraParams = new CameraParam[MaxCameraCount];

        // 相机状态数组（0=未连接, 1=已获取载荷, 2=参数已设置, 3=就绪）
        private int[] _cameraStatus = new int[MaxCameraCount];

        // 参数缓存，避免重复设置
        private Dictionary<int, CameraParamCache> _paramCache = new Dictionary<int, CameraParamCache>();

        // 当前选中的物理相机索引（用于 UI 操作）
        private int _currentSelectedIndex = -1;

        /// <summary>
        /// 相机状态变更事件（用于更新 UI 指示灯等）
        /// </summary>
        public event Action<int, int> CameraStatusChanged;

        /// <summary>
        /// 当前选中的物理相机索引
        /// </summary>
        public int CurrentSelectedIndex
        {
            get => _currentSelectedIndex;
            set
            {
                if (value >= -1 && value < _cameraCount)
                    _currentSelectedIndex = value;
            }
        }

        /// <summary>
        /// 获取指定索引相机的状态
        /// </summary>
        public int GetCameraStatus(int index) => (index >= 0 && index < MaxCameraCount) ? _cameraStatus[index] : -1;

        /// <summary>
        /// 获取实际相机数量
        /// </summary>
        public uint CameraCount => _cameraCount;

        /// <summary>
        /// 获取相机参数对象引用
        /// </summary>
        public CameraParam[] CameraParams => _cameraParams;

        /// <summary>
        /// 设置相机参数配置（通常在加载用户配置后调用）
        /// </summary>
        public void SetCameraParams(CameraParam[] paramsArray)
        {
            for (int i = 0; i < MaxCameraCount && i < paramsArray.Length; i++)
            {
                _cameraParams[i] = paramsArray[i];
            }
        }

        /// <summary>
        /// 获取指定相机的缓冲区指针（供 AcqFIFO 使用）
        /// </summary>
        public IntPtr GetBufferPtr(int camIndex) => _bufferPtrs[camIndex];

        /// <summary>
        /// 获取指定相机的载荷大小
        /// </summary>
        public UInt32 GetPayloadSize(int camIndex) => _payloadSizes[camIndex];

        /// <summary>
        /// 获取相机对象（用于底层调用）
        /// </summary>
        public MyCamera GetCamera(int index) => _cameras[index];

        #region 枚举与初始化

        /// <summary>
        /// 枚举设备，获取相机数量及名称列表
        /// </summary>
        public List<string> EnumDevices()
        {
            _cameraCount = _hikCamera.CameraNum();
            return _hikCamera.EnumDevices();
        }

        /// <summary>
        /// 初始化所有相机（根据用户配置的启用列表和相机索引）
        /// </summary>
        /// <param name="bListProcess">工位启用标志数组，长度为 maxCameraNum（逻辑工位数）</param>
        /// <param name="camIndices">工位对应的物理相机索引数组</param>
        /// <returns>初始化结果：0=成功，-1=无相机，-2=配置无效，-3=获取载荷失败，-4=参数设置失败，-5=分配内存失败</returns>
        public int InitializeAllCameras(bool[] bListProcess, int[] camIndices)
        {
            // 1. 枚举设备，检查是否有相机
            if (_cameraCount < 1)
                return -1;

            // 2. 获取设备名称列表（用于日志）
            List<string> deviceNames = EnumDevices();

            // 3. 检查配置的有效性，禁用无效工位
            bool anyProcessEnabled = false;
            for (int i = 0; i < bListProcess.Length; i++)
            {
                if (bListProcess[i])
                {
                    int phyIdx = camIndices[i];
                    if (phyIdx >= _cameraCount)
                    {
                        bListProcess[i] = false;   // 相机编号超出范围，禁用该工位
                    }
                    else
                    {
                        anyProcessEnabled = true;
                    }
                }
            }
            if (!anyProcessEnabled)
                return -2;

            // 4. 创建相机连接
            _cameras = _hikCamera.MultiCameraInit((int)_cameraCount, ref _cameraStatus);
            for (int i = 0; i < _cameraCount; i++)
            {
                if (_cameras[i] == null)
                    _cameraStatus[i] = 0;
                else
                    _cameraStatus[i] = 1;   // 已连接，未获取载荷
            }
            NotifyStatusChanges();

            // 5. 为每个启用的工位对应的物理相机设置参数并分配缓冲区
            bool anySuccess = false;
            for (int ws = 0; ws < bListProcess.Length; ws++)
            {
                if (!bListProcess[ws]) continue;
                int phyIdx = camIndices[ws];
                if (phyIdx >= _cameraCount) continue;

                // 5.1 设置相机参数（分辨率、曝光、增益等）
                if (!SetCameraParameters(phyIdx))
                {
                    bListProcess[ws] = false;   // 参数设置失败，禁用该工位
                    continue;
                }
                _cameraStatus[phyIdx] = 2;
                NotifyStatusChanged(phyIdx);

                // 5.2 获取载荷大小（先设置完参数再获取，确保正确）
                if (!GetPayloadSizeFromCamera(phyIdx))
                {
                    bListProcess[ws] = false;
                    continue;
                }
                _cameraStatus[phyIdx] = 3;  // 临时就绪（还需缓冲区）
                NotifyStatusChanged(phyIdx);

                // 5.3 分配缓冲区
                if (!AllocateBuffer(phyIdx))
                {
                    bListProcess[ws] = false;
                    continue;
                }
                _cameraStatus[phyIdx] = 3;
                NotifyStatusChanged(phyIdx);
                anySuccess = true;
            }

            if (!anySuccess)
                return -5;

            return 0;
        }

        /// <summary>
        /// 从相机获取 PayloadSize（优先通过属性获取，失败则尝试抓一帧）
        /// </summary>
        private bool GetPayloadSizeFromCamera(int phyIdx)
        {
            if (_cameras[phyIdx] == null) return false;

            var intVal = new MyCamera.MVCC_INTVALUE();
            if (_cameras[phyIdx].MV_CC_GetIntValue_NET("PayloadSize", ref intVal) == MyCamera.MV_OK)
            {
                _payloadSizes[phyIdx] = intVal.nCurValue;
                return true;
            }

            // 回退：抓一帧获取长度
            IntPtr tmpBuffer = Marshal.AllocHGlobal((int)_payloadSizes[phyIdx]);
            var frameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();
            int ret = _cameras[phyIdx].MV_CC_GetOneFrameTimeout_NET(tmpBuffer, _payloadSizes[phyIdx], ref frameInfo, 1000);
            Marshal.FreeHGlobal(tmpBuffer);
            if (ret == MyCamera.MV_OK)
            {
                _payloadSizes[phyIdx] = frameInfo.nFrameLen;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 分配或重新分配缓冲区
        /// </summary>
        private bool AllocateBuffer(int phyIdx)
        {
            if (_bufferSizes[phyIdx] < _payloadSizes[phyIdx])
            {
                if (_bufferPtrs[phyIdx] != IntPtr.Zero)
                    Marshal.FreeHGlobal(_bufferPtrs[phyIdx]);
                _bufferPtrs[phyIdx] = Marshal.AllocHGlobal((int)_payloadSizes[phyIdx]);
                if (_bufferPtrs[phyIdx] == IntPtr.Zero)
                    return false;
                _bufferSizes[phyIdx] = _payloadSizes[phyIdx];
            }
            return true;
        }

        /// <summary>
        /// 设置单个相机的所有参数（曝光、增益、锐化、Gamma、亮度、分辨率、像素格式、ROI）
        /// </summary>
        private bool SetCameraParameters(int phyIdx)
        {
            if (_cameras[phyIdx] == null) return false;
            var param = _cameraParams[phyIdx];
            bool success = true;

            // 曝光（若值<=0则自动曝光）
            success &= SetExposure(phyIdx, param.iExposeTime);
            // 增益
            success &= SetGain(phyIdx, param.dGain);
            // 锐化
            success &= SetSharpness(phyIdx, param.iSharp);
            // Gamma 使能和 value
            success &= SetGamma(phyIdx, param.dGamma);
            // 亮度
            success &= SetBrightness(phyIdx, param.iBrightness);
            // 分辨率
            success &= SetResolution(phyIdx, param.iWidth, param.iHeight);
            // 像素格式
            success &= SetPixelFormat(phyIdx, param.iMonoType);
            // ROI
            success &= SetROI(phyIdx, param.iROI);

            return success;
        }

        #endregion

        #region 单参数设置（带缓存）

        private bool SetExposure(int phyIdx, float value)
        {
            // 检查缓存
            if (_paramCache.ContainsKey(phyIdx) && Math.Abs(_paramCache[phyIdx].LastExposure - value) < 0.01f)
                return true;

            var range = new MyCamera.MVCC_FLOATVALUE();
            _cameras[phyIdx].MV_CC_GetExposureTime_NET(ref range);
            value = Math.Max(range.fMin, Math.Min(range.fMax, value));
            int ret;
            if (value <= 0)
            {
                ret = _cameras[phyIdx].MV_CC_SetEnumValue_NET("ExposureAuto",
                    (uint)MyCamera.MV_CAM_EXPOSURE_AUTO_MODE.MV_EXPOSURE_AUTO_MODE_CONTINUOUS);
                // 获取实际自动曝光值
                _cameras[phyIdx].MV_CC_GetExposureTime_NET(ref range);
                value = range.fCurValue;
            }
            else
            {
                _cameras[phyIdx].MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                ret = _cameras[phyIdx].MV_CC_SetExposureTime_NET(value);
            }
            if (ret == MyCamera.MV_OK)
            {
                UpdateCache(phyIdx, exposure: value);
                _cameraParams[phyIdx].iExposeTime = value;
                return true;
            }
            return false;
        }

        private bool SetGain(int phyIdx, float value)
        {
            if (_paramCache.ContainsKey(phyIdx) && Math.Abs(_paramCache[phyIdx].LastGain - value) < 0.01f)
                return true;

            var range = new MyCamera.MVCC_FLOATVALUE();
            _cameras[phyIdx].MV_CC_GetGain_NET(ref range);
            value = Math.Max(range.fMin, Math.Min(range.fMax, value));
            _cameras[phyIdx].MV_CC_SetEnumValue_NET("GainAuto", 0);
            int ret = _cameras[phyIdx].MV_CC_SetGain_NET(value);
            if (ret == MyCamera.MV_OK)
            {
                UpdateCache(phyIdx, gain: value);
                _cameraParams[phyIdx].dGain = value;
                return true;
            }
            return false;
        }

        private bool SetSharpness(int phyIdx, uint value)
        {
            if (_paramCache.ContainsKey(phyIdx) && _paramCache[phyIdx].LastSharpness == value)
                return true;

            var range = new MyCamera.MVCC_INTVALUE();
            _cameras[phyIdx].MV_CC_GetSharpness_NET(ref range);
            value = (uint)Math.Max(range.nMin, Math.Min(range.nMax, value));
            _cameras[phyIdx].MV_CC_SetEnumValue_NET("SharpnessAuto", 0);
            int ret = _cameras[phyIdx].MV_CC_SetSharpness_NET(value);
            if (ret == MyCamera.MV_OK)
            {
                UpdateCache(phyIdx, sharpness: value);
                _cameraParams[phyIdx].iSharp = value;
                return true;
            }
            return false;
        }

        private bool SetGamma(int phyIdx, float value)
        {
            if (_paramCache.ContainsKey(phyIdx) && Math.Abs(_paramCache[phyIdx].LastGamma - value) < 0.01f)
                return true;

            var range = new MyCamera.MVCC_FLOATVALUE();
            _cameras[phyIdx].MV_CC_GetGamma_NET(ref range);
            value = Math.Max(range.fMin, Math.Min(range.fMax, value));
            _cameras[phyIdx].MV_CC_SetEnumValue_NET("GammaAuto", 0);
            int ret = _cameras[phyIdx].MV_CC_SetGamma_NET(value);
            if (ret == MyCamera.MV_OK)
            {
                UpdateCache(phyIdx, gamma: value);
                _cameraParams[phyIdx].dGamma = value;
                return true;
            }
            return false;
        }

        private bool SetBrightness(int phyIdx, uint value)
        {
            if (_paramCache.ContainsKey(phyIdx) && _paramCache[phyIdx].LastBrightness == value)
                return true;

            var range = new MyCamera.MVCC_INTVALUE();
            _cameras[phyIdx].MV_CC_GetBrightness_NET(ref range);
            value = (uint)Math.Max(range.nMin, Math.Min(range.nMax, value));
            _cameras[phyIdx].MV_CC_SetEnumValue_NET("BrightnessAuto", 0);
            int ret = _cameras[phyIdx].MV_CC_SetBrightness_NET(value);
            if (ret == MyCamera.MV_OK)
            {
                UpdateCache(phyIdx, brightness: value);
                _cameraParams[phyIdx].iBrightness = value;
                return true;
            }
            return false;
        }

        private bool SetResolution(int phyIdx, uint width, uint height)
        {
            if (_paramCache.ContainsKey(phyIdx) &&
                _paramCache[phyIdx].LastWidth == width && _paramCache[phyIdx].LastHeight == height)
                return true;

            var range = new MyCamera.MVCC_INTVALUE();
            _cameras[phyIdx].MV_CC_GetWidth_NET(ref range);
            width = (uint)Math.Max(range.nMin, Math.Min(range.nMax, width));
            _cameras[phyIdx].MV_CC_GetHeight_NET(ref range);
            height = (uint)Math.Max(range.nMin, Math.Min(range.nMax, height));

            int ret = _cameras[phyIdx].MV_CC_SetWidth_NET(width);
            ret += _cameras[phyIdx].MV_CC_SetHeight_NET(height);
            if (ret == MyCamera.MV_OK * 2)
            {
                UpdateCache(phyIdx, width: width, height: height);
                _cameraParams[phyIdx].iWidth = width;
                _cameraParams[phyIdx].iHeight = height;
                // 分辨率改变后，需要重新分配缓冲区（由外部调用 ReallocateBuffer）
                return true;
            }
            return false;
        }

        /// <summary>
        /// 设置像素格式（0=黑白 Mono8，1=彩色自动适配）
        /// </summary>
        private bool SetPixelFormat(int phyIdx, int monoType)
        {
            if (_paramCache.ContainsKey(phyIdx) && _paramCache[phyIdx].LastPixelFormat == monoType)
                return true;

            int desiredType = monoType; // 0=黑白，1=彩色
            bool ok = _hikCamera.SetPixelFormat(ref desiredType, _cameras[phyIdx]);
            if (ok)
            {
                UpdateCache(phyIdx, pixelFormat: desiredType);
                _cameraParams[phyIdx].iMonoType = desiredType;
                // 像素格式改变后，载荷大小会变，需要重新获取 PayloadSize 并分配缓冲区
                GetPayloadSizeFromCamera(phyIdx);
                AllocateBuffer(phyIdx);
                return true;
            }
            return false;
        }

        private bool SetROI(int phyIdx, uint[] roi)
        {
            if (roi == null || roi.Length < 4) return false;
            if (_paramCache.ContainsKey(phyIdx) &&
                _paramCache[phyIdx].LastROI[0] == roi[0] && _paramCache[phyIdx].LastROI[1] == roi[1] &&
                _paramCache[phyIdx].LastROI[2] == roi[2] && _paramCache[phyIdx].LastROI[3] == roi[3])
                return true;

            int ret = _cameras[phyIdx].MV_CC_SetIntValue_NET("OffsetX", roi[0]);
            Thread.Sleep(ParamSetDelayMs);
            ret += _cameras[phyIdx].MV_CC_SetIntValue_NET("OffsetY", roi[1]);
            Thread.Sleep(ParamSetDelayMs);
            ret += _cameras[phyIdx].MV_CC_SetIntValue_NET("Width", roi[2]);
            Thread.Sleep(ParamSetDelayMs);
            ret += _cameras[phyIdx].MV_CC_SetIntValue_NET("Height", roi[3]);
            if (ret == MyCamera.MV_OK * 4)
            {
                UpdateCache(phyIdx, roi: roi);
                Array.Copy(roi, _cameraParams[phyIdx].iROI, 4);
                return true;
            }
            return false;
        }

        #endregion

        #region 批量参数获取与设置（供UI调用）

        /// <summary>
        /// 从相机硬件读取当前参数到 _cameraParams
        /// </summary>
        public bool RefreshCameraParameters(int phyIdx)
        {
            if (_cameras[phyIdx] == null) return false;
            var param = _cameraParams[phyIdx];
            bool ok = true;
            var floatVal = new MyCamera.MVCC_FLOATVALUE();
            var intVal = new MyCamera.MVCC_INTVALUE();

            ok &= _cameras[phyIdx].MV_CC_GetExposureTime_NET(ref floatVal) == MyCamera.MV_OK;
            param.iExposeTime = floatVal.fCurValue;
            ok &= _cameras[phyIdx].MV_CC_GetGain_NET(ref floatVal) == MyCamera.MV_OK;
            param.dGain = floatVal.fCurValue;
            ok &= _cameras[phyIdx].MV_CC_GetGamma_NET(ref floatVal) == MyCamera.MV_OK;
            param.dGamma = floatVal.fCurValue;
            ok &= _cameras[phyIdx].MV_CC_GetBrightness_NET(ref intVal) == MyCamera.MV_OK;
            param.iBrightness = intVal.nCurValue;
            ok &= _cameras[phyIdx].MV_CC_GetSharpness_NET(ref intVal) == MyCamera.MV_OK;
            param.iSharp = intVal.nCurValue;
            ok &= _cameras[phyIdx].MV_CC_GetWidth_NET(ref intVal) == MyCamera.MV_OK;
            param.iWidth = intVal.nCurValue;
            ok &= _cameras[phyIdx].MV_CC_GetHeight_NET(ref intVal) == MyCamera.MV_OK;
            param.iHeight = intVal.nCurValue;
            ok &= GetPixelFormatFromCamera(phyIdx, out int fmt);
            param.iMonoType = fmt;
            ok &= GetROIFromCamera(phyIdx, param.iROI);

            if (ok)
                UpdateCache(phyIdx, param);
            return ok;
        }

        private bool GetPixelFormatFromCamera(int phyIdx, out int format)
        {
            format = 0;
            var enumVal = new MyCamera.MVCC_ENUMVALUE();
            if (_cameras[phyIdx].MV_CC_GetPixelFormat_NET(ref enumVal) != MyCamera.MV_OK)
                return false;
            uint cur = enumVal.nCurValue;
            if (cur == (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8)
                format = 0;
            else if (cur == (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed ||
                     cur == (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed)
                format = 1;
            else
                format = 0;
            return true;
        }

        private bool GetROIFromCamera(int phyIdx, uint[] roi)
        {
            var intVal = new MyCamera.MVCC_INTVALUE();
            int ret = _cameras[phyIdx].MV_CC_GetIntValue_NET("OffsetX", ref intVal);
            roi[0] = intVal.nCurValue;
            ret += _cameras[phyIdx].MV_CC_GetIntValue_NET("OffsetY", ref intVal);
            roi[1] = intVal.nCurValue;
            ret += _cameras[phyIdx].MV_CC_GetIntValue_NET("Width", ref intVal);
            roi[2] = intVal.nCurValue;
            ret += _cameras[phyIdx].MV_CC_GetIntValue_NET("Height", ref intVal);
            roi[3] = intVal.nCurValue;
            return ret == MyCamera.MV_OK * 4;
        }

        /// <summary>
        /// 将 UI 修改后的参数写入相机（仅写有变化的项）
        /// </summary>
        public bool ApplyCameraParametersFromUI(int phyIdx)
        {
            var param = _cameraParams[phyIdx];
            bool success = true;
            success &= SetExposure(phyIdx, param.iExposeTime);
            success &= SetGain(phyIdx, param.dGain);
            success &= SetSharpness(phyIdx, param.iSharp);
            success &= SetGamma(phyIdx, param.dGamma);
            success &= SetBrightness(phyIdx, param.iBrightness);
            success &= SetResolution(phyIdx, param.iWidth, param.iHeight);
            success &= SetPixelFormat(phyIdx, param.iMonoType);
            success &= SetROI(phyIdx, param.iROI);
            return success;
        }

        #endregion

        #region 采集控制

        /// <summary>
        /// 开始指定相机的采集
        /// </summary>
        public bool StartGrabbing(int phyIdx)
        {
            if (_cameras[phyIdx] == null) return false;
            int ret = _cameras[phyIdx].MV_CC_StartGrabbing_NET();
            if (ret == MyCamera.MV_OK)
            {
                _cameraStatus[phyIdx] = 3;
                NotifyStatusChanged(phyIdx);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 停止指定相机的采集
        /// </summary>
        public void StopGrabbing(int phyIdx)
        {
            _cameras[phyIdx]?.MV_CC_StopGrabbing_NET();
        }

        /// <summary>
        /// 停止所有相机的采集并释放资源
        /// </summary>
        public void Shutdown()
        {
            for (int i = 0; i < MaxCameraCount; i++)
            {
                if (_cameras[i] != null)
                {
                    _cameras[i].MV_CC_StopGrabbing_NET();
                    _cameras[i].MV_CC_CloseDevice_NET();
                    _cameras[i].MV_CC_DestroyDevice_NET();
                    _cameras[i] = null;
                }
                if (_bufferPtrs[i] != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_bufferPtrs[i]);
                    _bufferPtrs[i] = IntPtr.Zero;
                }
                _cameraStatus[i] = 0;
            }
            _cameraCount = 0;
        }

        /// <summary>
        /// 重新分配缓冲区（当分辨率或像素格式改变时调用）
        /// </summary>
        public bool ReallocateBuffer(int phyIdx)
        {
            if (!GetPayloadSizeFromCamera(phyIdx)) return false;
            return AllocateBuffer(phyIdx);
        }

        #endregion

        #region 触发模式设置

        /// <summary>
        /// 设置指定相机的触发模式（on/off）和触发源
        /// </summary>
        public bool SetTriggerMode(int phyIdx, bool enable, int triggerSource = 0)
        {
            if (_cameras[phyIdx] == null) return false;
            int ret;
            if (enable)
            {
                ret = _cameras[phyIdx].MV_CC_SetEnumValue_NET("TriggerMode",
                    (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);
                if (ret != MyCamera.MV_OK) return false;
                if (triggerSource == 4) // 软件触发
                    ret = _cameras[phyIdx].MV_CC_SetEnumValue_NET("TriggerSource",
                        (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                else
                    ret = _cameras[phyIdx].MV_CC_SetEnumValue_NET("TriggerSource",
                        (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
            }
            else
            {
                ret = _cameras[phyIdx].MV_CC_SetEnumValue_NET("TriggerMode",
                    (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
            }
            return ret == MyCamera.MV_OK;
        }

        /// <summary>
        /// 软件触发一次
        /// </summary>
        public bool SoftwareTrigger(int phyIdx)
        {
            if (_cameras[phyIdx] == null) return false;
            return _cameras[phyIdx].MV_CC_SetCommandValue_NET("TriggerSoftware") == MyCamera.MV_OK;
        }

        #endregion

        #region 辅助方法

        private void UpdateCache(int phyIdx, float? exposure = null, float? gain = null, uint? sharpness = null,
            float? gamma = null, uint? brightness = null, uint? width = null, uint? height = null,
            int? pixelFormat = null, uint[] roi = null)
        {
            if (!_paramCache.ContainsKey(phyIdx))
                _paramCache[phyIdx] = new CameraParamCache();
            var cache = _paramCache[phyIdx];
            if (exposure.HasValue) cache.LastExposure = exposure.Value;
            if (gain.HasValue) cache.LastGain = gain.Value;
            if (sharpness.HasValue) cache.LastSharpness = sharpness.Value;
            if (gamma.HasValue) cache.LastGamma = gamma.Value;
            if (brightness.HasValue) cache.LastBrightness = brightness.Value;
            if (width.HasValue) cache.LastWidth = width.Value;
            if (height.HasValue) cache.LastHeight = height.Value;
            if (pixelFormat.HasValue) cache.LastPixelFormat = pixelFormat.Value;
            if (roi != null) Array.Copy(roi, cache.LastROI, 4);
        }

        private void UpdateCache(int phyIdx, CameraParam param)
        {
            UpdateCache(phyIdx, exposure: param.iExposeTime, gain: param.dGain, sharpness: param.iSharp,
                gamma: param.dGamma, brightness: param.iBrightness, width: param.iWidth, height: param.iHeight,
                pixelFormat: param.iMonoType, roi: param.iROI);
        }

        private void NotifyStatusChanged(int phyIdx)
        {
            CameraStatusChanged?.Invoke(phyIdx, _cameraStatus[phyIdx]);
        }

        private void NotifyStatusChanges()
        {
            for (int i = 0; i < MaxCameraCount; i++)
                NotifyStatusChanged(i);
        }

        public void Dispose()
        {
            Shutdown();
        }

        #endregion

        // 内部参数缓存类
        private class CameraParamCache
        {
            public uint LastWidth { get; set; } = 0;
            public uint LastHeight { get; set; } = 0;
            public int LastPixelFormat { get; set; } = -1;
            public float LastExposure { get; set; } = -1;
            public float LastGain { get; set; } = -1;
            public uint LastSharpness { get; set; } = uint.MaxValue;
            public float LastGamma { get; set; } = -1;
            public uint LastBrightness { get; set; } = uint.MaxValue;
            public uint[] LastROI { get; set; } = new uint[4];
        }

        // ========== 在 CameraManager 类中添加以下方法 ==========

        /// <summary>设置相机的自动曝光模式</summary>
        public void SetExposureAuto(int phyIdx, bool enable)
        {
            if (_cameras[phyIdx] == null) return;
            uint mode = enable ?
                (uint)MyCamera.MV_CAM_EXPOSURE_AUTO_MODE.MV_EXPOSURE_AUTO_MODE_CONTINUOUS : 0;
            _cameras[phyIdx].MV_CC_SetEnumValue_NET("ExposureAuto", mode);
        }

        /// <summary>检查相机是否已连接</summary>
        public bool IsCameraConnected(int phyIdx)
        {
            return _cameras[phyIdx] != null && _cameras[phyIdx].MV_CC_IsDeviceConnected_NET();
        }

        /// <summary>采集单帧图像（Halcon HObject 格式）</summary>
        public HObject GrabOneFrameHObject(int phyIdx)
        {
            if (_cameras[phyIdx] == null || _bufferPtrs[phyIdx] == IntPtr.Zero) return null;

            var frameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();
            int ret = _cameras[phyIdx].MV_CC_GetOneFrameTimeout_NET(
                _bufferPtrs[phyIdx], _payloadSizes[phyIdx], ref frameInfo, 1000);
            if (ret != MyCamera.MV_OK) return null;

            HObject image;
            HOperatorSet.GenImage1(out image, "byte", (int)frameInfo.nWidth,
                (int)frameInfo.nHeight, _bufferPtrs[phyIdx]);
            return image;
        }

        /// <summary>采集单帧图像（CogImage8Grey 格式）</summary>
        public CogImage8Grey GrabOneFrameMono(int phyIdx)
        {
            if (_cameras[phyIdx] == null || _bufferPtrs[phyIdx] == IntPtr.Zero) return null;

            var frameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();
            int ret = _cameras[phyIdx].MV_CC_GetOneFrameTimeout_NET(
                _bufferPtrs[phyIdx], _payloadSizes[phyIdx], ref frameInfo, 1000);
            if (ret != MyCamera.MV_OK) return null;

            var root = new CogImage8Root();
            root.Initialize((int)frameInfo.nWidth, (int)frameInfo.nHeight,
                _bufferPtrs[phyIdx], (int)frameInfo.nWidth, null);
            var img = new CogImage8Grey();
            img.SetRoot(root);
            return img;
        }

        /// <summary>采集单帧图像（CogImage24PlanarColor 格式）</summary>
        public CogImage24PlanarColor GrabOneFrameColor(int phyIdx)
        {
            if (_cameras[phyIdx] == null || _bufferPtrs[phyIdx] == IntPtr.Zero) return null;

            var frameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();
            int ret = _cameras[phyIdx].MV_CC_GetOneFrameTimeout_NET(
                _bufferPtrs[phyIdx], _payloadSizes[phyIdx], ref frameInfo, 1000);
            if (ret != MyCamera.MV_OK) return null;

            // 假设相机输出为 BGR24 格式（彩色模式）
            int width = (int)frameInfo.nWidth;
            int height = (int)frameInfo.nHeight;
            int stride = (width * 3 + 3) & ~3;
            using (var bitmap = new Bitmap(width, height, stride, PixelFormat.Format24bppRgb, _bufferPtrs[phyIdx]))
            {
                return new CogImage24PlanarColor(bitmap);
            }
        }

        /// <summary>采集单帧图像（byte[] 格式）</summary>
        public byte[] GrabOneFrameBytes(int phyIdx)
        {
            if (_cameras[phyIdx] == null || _bufferPtrs[phyIdx] == IntPtr.Zero) return null;

            var frameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();
            int ret = _cameras[phyIdx].MV_CC_GetOneFrameTimeout_NET(
                _bufferPtrs[phyIdx], _payloadSizes[phyIdx], ref frameInfo, 1000);
            if (ret != MyCamera.MV_OK) return null;

            byte[] data = new byte[frameInfo.nFrameLen];
            Marshal.Copy(_bufferPtrs[phyIdx], data, 0, (int)frameInfo.nFrameLen);
            return data;
        }
    }
}
