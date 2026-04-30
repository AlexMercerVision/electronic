// ==================== ImageAcqwithOpenCV.cs ====================
 
using System.Collections.Generic;
using System; 
using System.Runtime.InteropServices;
using System.Text;
using MvCamCtrl.NET;

namespace ImageAcqCls
{
    public class ImageAcqwithOpenCV
    {
        #region 常量与配置
        public const int MaxCameraNum = 5;
        private const int OneFrameTimeoutMs = 10000;
        #endregion

        #region 相机相关字段
        public MyCamera camera = new MyCamera();
        private MyCamera[] _cameras = new MyCamera[MaxCameraNum];
        private CameraStatus _cameraStatus;
        private int _cameraType = 0;          // 0: OneFrameTimeout, 1: GetImageBuffer
        private uint[] nPayloadSizeS = new uint[MaxCameraNum];
        public IntPtr[] m_BufForDriverS = new IntPtr[MaxCameraNum];
        public string[] strPixelFormat = new string[MaxCameraNum];
        public bool[] bIsColorCam = new bool[MaxCameraNum];
        public MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();
        public MyCamera.MV_FRAME_OUT stFrameInfoWithData = new MyCamera.MV_FRAME_OUT();
        private int nRet;

        public byte[][] bImgSrcData = new byte[MaxCameraNum][];
        public int LastWidth { get; private set; }
        public int LastHeight { get; private set; }
        public int LastChannels { get; private set; }

        private CUserParam cup = new CUserParam();
        public CSerialDef csd = new CSerialDef();
        public string strErrInfo = "";
        #endregion

        #region 构造函数
        public ImageAcqwithOpenCV()
        {
            _cameraStatus = new CameraStatus();
            _cameraStatus.iStatus = new int[MaxCameraNum];
            _cameraStatus.camNum = 0;
        }
        #endregion

        #region 属性
        public CameraStatus _cs
        {
            get { return _cameraStatus; }
            set { _cameraStatus = value; }
        }

        public CUserParam _cup
        {
            get { return cup; }
            set { cup = value; }
        }

        public MyCamera[] _HikCam
        {
            get { return _cameras; }
            set { _cameras = value; }
        }

        public CSerialDef _csd
        {
            get { return csd; }
            set { csd = value; }
        }
        #endregion

        #region 枚举设备
        public static List<string> EnumDevices()
        {
            List<string> devices = new List<string>();
            MyCamera.MV_CC_DEVICE_INFO_LIST deviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
            int ret = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref deviceList);
            if (ret != MyCamera.MV_OK) return devices;

            for (int i = 0; i < deviceList.nDeviceNum; i++)
            {
                IntPtr pInfo = deviceList.pDeviceInfo[i];
                MyCamera.MV_CC_DEVICE_INFO info = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(pInfo, typeof(MyCamera.MV_CC_DEVICE_INFO));
                string deviceName = "Unknown";

                if (info.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    var gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)MyCamera.ByteToStruct(
                        info.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                    deviceName = !string.IsNullOrEmpty(gigeInfo.chUserDefinedName)
                        ? gigeInfo.chUserDefinedName
                        : gigeInfo.chSerialNumber;
                }
                else if (info.nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    var usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)MyCamera.ByteToStruct(
                        info.SpecialInfo.stUsb3VInfo, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                    deviceName = !string.IsNullOrEmpty(usbInfo.chUserDefinedName)
                        ? usbInfo.chUserDefinedName
                        : usbInfo.chSerialNumber;
                }
                devices.Add($"{i}: {deviceName}");
            }
            return devices;
        }
        #endregion

        #region 打开/关闭相机
        public bool OpenCamera(int index)
        {
            if (index < 0 || index >= MaxCameraNum) return false;
            if (_cameras[index] != null && _cameras[index].MV_CC_IsDeviceConnected_NET())
                CloseCamera(index);

            MyCamera.MV_CC_DEVICE_INFO_LIST deviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
            int ret = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref deviceList);
            if (ret != MyCamera.MV_OK || index >= deviceList.nDeviceNum)
                return false;

            IntPtr pDeviceInfo = deviceList.pDeviceInfo[index];
            MyCamera.MV_CC_DEVICE_INFO deviceInfo = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(pDeviceInfo, typeof(MyCamera.MV_CC_DEVICE_INFO));

            _cameras[index] = new MyCamera();
            ret = _cameras[index].MV_CC_CreateDevice_NET(ref deviceInfo);
            if (ret != MyCamera.MV_OK) return false;
            ret = _cameras[index].MV_CC_OpenDevice_NET();
            if (ret != MyCamera.MV_OK) return false;

            if (deviceInfo.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                int packetSize = _cameras[index].MV_CC_GetOptimalPacketSize_NET();
                if (packetSize > 0)
                {
                    _cameras[index].MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)packetSize);
                }
            }

            MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
            _cameras[index].MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
            nPayloadSizeS[index] = stParam.nCurValue;
            if (m_BufForDriverS[index] == IntPtr.Zero)
                m_BufForDriverS[index] = Marshal.AllocHGlobal((int)nPayloadSizeS[index]);

            _cameraStatus.iStatus[index] = 1;
            if (index == 0) camera = _cameras[0];
            return true;
        }

        public void CloseCamera(int index)
        {
            if (index < 0 || index >= MaxCameraNum) return;
            if (_cameras[index] != null)
            {
                if (_cameraStatus.iStatus[index] >= 2)
                    _cameras[index].MV_CC_StopGrabbing_NET();
                _cameras[index].MV_CC_CloseDevice_NET();
                _cameras[index].MV_CC_DestroyDevice_NET();
                _cameras[index] = null;
            }
            if (m_BufForDriverS[index] != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(m_BufForDriverS[index]);
                m_BufForDriverS[index] = IntPtr.Zero;
            }
            _cameraStatus.iStatus[index] = 0;
            bImgSrcData[index] = null;
        }

        public bool StartGrabbing(int index)
        {
            if (!IsCamOpen(index)) return false;
            int ret = _cameras[index].MV_CC_StartGrabbing_NET();
            if (ret == MyCamera.MV_OK)
            {
                _cameraStatus.iStatus[index] = 3;
                return true;
            }
            return false;
        }

        public bool StopGrabbing(int index)
        {
            if (!IsCamOpen(index)) return false;
            int ret = _cameras[index].MV_CC_StopGrabbing_NET();
            if (ret == MyCamera.MV_OK)
                _cameraStatus.iStatus[index] = 1;
            return ret == MyCamera.MV_OK;
        }

        public bool IsCamOpen(int index)
        {
            return index >= 0 && index < MaxCameraNum && _cameras[index] != null &&
                   _cameras[index].MV_CC_IsDeviceConnected_NET() && _cameraStatus.iStatus[index] >= 1;
        }
        #endregion

        #region 参数读写
        public float GetExposure(int index)
        {
            if (!IsCamOpen(index)) return -1;
            MyCamera.MVCC_FLOATVALUE fVal = new MyCamera.MVCC_FLOATVALUE();
            _cameras[index].MV_CC_GetFloatValue_NET("ExposureTime", ref fVal);
            return fVal.fCurValue;
        }

        public bool SetExposure(int index, float exposureUs)
        {
            if (!IsCamOpen(index)) return false;
            return _cameras[index].MV_CC_SetFloatValue_NET("ExposureTime", exposureUs) == MyCamera.MV_OK;
        }

        public float GetGain(int index)
        {
            if (!IsCamOpen(index)) return -1;
            MyCamera.MVCC_FLOATVALUE fVal = new MyCamera.MVCC_FLOATVALUE();
            _cameras[index].MV_CC_GetFloatValue_NET("Gain", ref fVal);
            return fVal.fCurValue;
        }

        public bool SetGain(int index, float gain)
        {
            if (!IsCamOpen(index)) return false;
            return _cameras[index].MV_CC_SetFloatValue_NET("Gain", gain) == MyCamera.MV_OK;
        }

        public bool GetAutoExposure(int index)
        {
            if (!IsCamOpen(index)) return false;
            MyCamera.MVCC_ENUMVALUE enumVal = new MyCamera.MVCC_ENUMVALUE();
            _cameras[index].MV_CC_GetEnumValue_NET("ExposureAuto", ref enumVal);
            return enumVal.nCurValue == 2;
        }

        public bool SetAutoExposure(int index, bool enable)
        {
            if (!IsCamOpen(index)) return false;
            uint value = enable ? 2u : 0u;
            return _cameras[index].MV_CC_SetEnumValue_NET("ExposureAuto", value) == MyCamera.MV_OK;
        }

        public int GetPixelFormat(int index)
        {
            if (!IsCamOpen(index)) return -1;
            MyCamera.MVCC_ENUMVALUE enumVal = new MyCamera.MVCC_ENUMVALUE();
            _cameras[index].MV_CC_GetEnumValue_NET("PixelFormat", ref enumVal);
            return (int)enumVal.nCurValue;
        }

        public bool SetPixelFormat(int index, int pixelFormatIndex)
        {
            if (!IsCamOpen(index)) return false;
            return _cameras[index].MV_CC_SetEnumValue_NET("PixelFormat", (uint)pixelFormatIndex) == MyCamera.MV_OK;
        }

        public void GetResolution(int index, out uint width, out uint height)
        {
            width = height = 0;
            if (!IsCamOpen(index)) return;
            MyCamera.MVCC_INTVALUE intVal = new MyCamera.MVCC_INTVALUE();
            _cameras[index].MV_CC_GetIntValue_NET("Width", ref intVal);
            width = intVal.nCurValue;
            _cameras[index].MV_CC_GetIntValue_NET("Height", ref intVal);
            height = intVal.nCurValue;
        }

        public bool SetResolution(int index, uint width, uint height)
        {
            if (!IsCamOpen(index)) return false;
            bool wasGrabbing = (_cameraStatus.iStatus[index] >= 3);
            if (wasGrabbing) StopGrabbing(index);
            bool ret1 = _cameras[index].MV_CC_SetIntValue_NET("Width", width) == MyCamera.MV_OK;
            bool ret2 = _cameras[index].MV_CC_SetIntValue_NET("Height", height) == MyCamera.MV_OK;
            if (wasGrabbing) StartGrabbing(index);
            return ret1 && ret2;
        }

        public void SetBuff(IntPtr buff, uint payloadSize, int iCamIndex)
        {
            m_BufForDriverS[iCamIndex] = buff;
            nPayloadSizeS[iCamIndex] = payloadSize;
        }
        #endregion

        #region 核心采集
        public bool GrabOneFrameToByteArray(int iCamInd)
        {
            if (_cameraStatus.iStatus[iCamInd] < 3)
                return false;
            try
            {
                if (_cameraType == 0)
                {
                    nRet = _cameras[iCamInd].MV_CC_GetOneFrameTimeout_NET(
                        m_BufForDriverS[iCamInd],
                        nPayloadSizeS[iCamInd],
                        ref stFrameInfo,
                        OneFrameTimeoutMs);
                    if (nRet == MyCamera.MV_OK)
                    {
                        LastWidth = (int)stFrameInfo.nWidth;
                        LastHeight = (int)stFrameInfo.nHeight;
                        LastChannels = IsColorPixelFormat(stFrameInfo.enPixelType) ? 3 : 1;
                        int length = (int)nPayloadSizeS[iCamInd];
                        if (bImgSrcData[iCamInd] == null || bImgSrcData[iCamInd].Length != length)
                            bImgSrcData[iCamInd] = new byte[length];
                        Marshal.Copy(m_BufForDriverS[iCamInd], bImgSrcData[iCamInd], 0, length);
                        return true;
                    }
                }
                else
                {
                    nRet = _cameras[iCamInd].MV_CC_GetImageBuffer_NET(ref stFrameInfoWithData, 1000);
                    if (nRet == MyCamera.MV_OK)
                    {
                        LastWidth = (int)stFrameInfoWithData.stFrameInfo.nWidth;
                        LastHeight = (int)stFrameInfoWithData.stFrameInfo.nHeight;
                        LastChannels = IsColorPixelFormat(stFrameInfoWithData.stFrameInfo.enPixelType) ? 3 : 1;
                        int length = (int)stFrameInfoWithData.stFrameInfo.nFrameLen;
                        if (bImgSrcData[iCamInd] == null || bImgSrcData[iCamInd].Length != length)
                            bImgSrcData[iCamInd] = new byte[length];
                        Marshal.Copy(stFrameInfoWithData.pBufAddr, bImgSrcData[iCamInd], 0, length);
                        _cameras[iCamInd].MV_CC_FreeImageBuffer_NET(ref stFrameInfoWithData);
                        return true;
                    }
                }
            }
            catch (Exception ex) { strErrInfo = ex.Message; }
            return false;
        }

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
                default: return false;
            }
        }
        #endregion
    }
}