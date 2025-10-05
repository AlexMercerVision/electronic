using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvCamCtrl.NET;
using System.Runtime.InteropServices;
//using System.Windows.Forms;
//using System.Drawing;
using System.Threading;

namespace CommonCls
{
    using ImageCallBack = MyCamera.cbOutputdelegate;
    using ExceptionCallBack = MyCamera.cbExceptiondelegate;
    public class HIKVisionCamera : MyCamera
    {
        MyCamera.MV_FRAME_OUT_INFO_EX m_stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();

        public const int CO_FAIL = -1;
        public const int CO_OK = 0;
        int cameraNum = 0;
        const int maxCameraNum = 5;
        //放出一个Camera
        MyCamera camera = null;
        MyCamera[] cameraArr = new MyCamera[maxCameraNum];

        // 列表
        MyCamera.MV_CC_DEVICE_INFO_LIST deviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();

        public HIKVisionCamera()
        {
            camera = new MyCamera();
        }
        //相机个数
        public uint CameraNum()
        {
            //   MyCamera.MV_CC_EnumDevicesEx_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref deviceList, "Hikvision");
            MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref deviceList);
            return deviceList.nDeviceNum;
        }

        // 枚举相机
        public List<string> EnumDevices()
        {
            uint num = CameraNum();
            List<string> allCameraInfos = new List<string>();
            for (int i = 0; i < num; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(deviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)MyCamera.ByteToStruct(device.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                    if (gigeInfo.chUserDefinedName != "")
                    {
                        allCameraInfos.Add("GEV: " + gigeInfo.chUserDefinedName);
                    }
                    else
                    {
                        allCameraInfos.Add("GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName);
                    }
                }
                else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)MyCamera.ByteToStruct(device.SpecialInfo.stUsb3VInfo, typeof(MyCamera.MV_USB3_DEVICE_INFO));

                    if (usbInfo.chUserDefinedName != "")
                    {
                        allCameraInfos.Add("U3V: " + usbInfo.chUserDefinedName);
                    }
                    else
                    {
                        allCameraInfos.Add("U3V: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " " + usbInfo.chSerialNumber);
                    }
                }

            }
            return allCameraInfos;


        }

        // 相机初始化
        public MyCamera CameraInit(int index)
        {

            MyCamera.MV_CC_DEVICE_INFO device =
                (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(deviceList.pDeviceInfo[index],
                                                                typeof(MyCamera.MV_CC_DEVICE_INFO));
            if (null == camera)
            {
                camera = new MyCamera();
                if (null == camera)
                {
                    return null;
                }
            }
            // 创建设备
            int nRet=camera.MV_CC_CreateDevice_NET(ref device);
            if (MyCamera.MV_OK != nRet)
            {
                return null;
            }
            // 打开设备
            nRet=camera.MV_CC_OpenDevice_NET();
            // 探测网络最佳包大小(只对GigE相机有效)
            if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                int nPacketSize = camera.MV_CC_GetOptimalPacketSize_NET();
                if (nPacketSize > 0)
                {
                    camera.MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                }
            }
            // 设置连续采集模式
           //st  2023年1月1日 MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_MUTLI
            camera.MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
            camera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);

            return camera;
        }
        public void SetIndexCamera(MyCamera myCamera,int index)
        {
            cameraArr[index] = myCamera;
        }

        public MyCamera[] CameraInit(int i, ref int[] iStatus)
        {
                MyCamera.MV_CC_DEVICE_INFO device =
                    (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(deviceList.pDeviceInfo[i],
                                                                    typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (null == cameraArr[i])
                {
                    cameraArr[i] = new MyCamera();
                }
                // 创建设备
                int nRet = cameraArr[i].MV_CC_CreateDevice_NET(ref device);
                if (MyCamera.MV_OK != nRet)
                {
                    iStatus[i] = 0;
                    goto outFun;
                }
                // 打开设备
                nRet = cameraArr[i].MV_CC_OpenDevice_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    iStatus[i] = 0;
                    goto outFun;
                }

                // 探测网络最佳包大小(只对GigE相机有效)
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    int nPacketSize = cameraArr[i].MV_CC_GetOptimalPacketSize_NET();
                    if (nPacketSize > 0)
                    {
                        cameraArr[i].MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                    }
                }
                // 设置连续采集模式
                iStatus[i] = 1;
                //20220415
                //st 2023年1月1日 MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_MUTLI

                cameraArr[i].MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
                // cameraArr[i].MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_SINGLE);
                cameraArr[i].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
  outFun:          
            return cameraArr;
        }


        public MyCamera[] MultiCameraInit(int Num,ref int[] iStatus)
        {
            cameraNum = Num;
            for (int i = 0; i < cameraNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device =
                    (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(deviceList.pDeviceInfo[i],
                                                                    typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (null == cameraArr[i])
                {
                    cameraArr[i] = new MyCamera();
                }
                // 创建设备
                int nRet = cameraArr[i].MV_CC_CreateDevice_NET(ref device);
                if (MyCamera.MV_OK != nRet)
                {
                    iStatus[i] = 0;
                      continue;
                }
                // 打开设备
                nRet = cameraArr[i].MV_CC_OpenDevice_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    iStatus[i] = 0;
                    continue;
                }

                // 探测网络最佳包大小(只对GigE相机有效)
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    int nPacketSize = cameraArr[i].MV_CC_GetOptimalPacketSize_NET();
                    if (nPacketSize > 0)
                    {
                        cameraArr[i].MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                    }
                }
                // 设置连续采集模式
                iStatus[i] = 1;
                //20220415
                //st 2023年1月1日 MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_MUTLI

                cameraArr[i].MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
               // cameraArr[i].MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_SINGLE);
                cameraArr[i].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
            }
            return cameraArr;
        }

        public MyCamera[] MultiCameraInitTrigger(int Num, ref int[] iStatus)
        {
            cameraNum = Num;
            for (int i = 0; i < cameraNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device =
                    (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(deviceList.pDeviceInfo[i],
                                                                    typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (null == cameraArr[i])
                {
                    cameraArr[i] = new MyCamera();
                }
                // 创建设备
                int nRet = cameraArr[i].MV_CC_CreateDevice_NET(ref device);
                if (MyCamera.MV_OK != nRet)
                {
                    iStatus[i] = 0;
                    continue;
                }
                // 打开设备
                nRet = cameraArr[i].MV_CC_OpenDevice_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    iStatus[i] = 0;
                    continue;
                }

                // 探测网络最佳包大小(只对GigE相机有效)
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    int nPacketSize = cameraArr[i].MV_CC_GetOptimalPacketSize_NET();
                    if (nPacketSize > 0)
                    {
                        cameraArr[i].MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                    }
                }
                // 设置连续采集模式
                iStatus[i] = 1;
                //20220415
                cameraArr[i].MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
                // cameraArr[i].MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_SINGLE);
                cameraArr[i].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
            }
            return cameraArr;
        }

        public MyCamera[] MultiCameraInit(int Num)
        {
            cameraNum = Num;
            for (int i = 0; i < cameraNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device =
                    (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(deviceList.pDeviceInfo[i],
                                                                    typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (null == cameraArr[i])
                {
                    cameraArr[i] = new MyCamera();
                }
                // 创建设备
               int nRet=cameraArr[i].MV_CC_CreateDevice_NET(ref device);
                if (MyCamera.MV_OK != nRet)
                {
                    return null;
                }
                // 打开设备
                nRet = cameraArr[i].MV_CC_OpenDevice_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    return null;
                }

                // 探测网络最佳包大小(只对GigE相机有效)
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    int nPacketSize = cameraArr[i].MV_CC_GetOptimalPacketSize_NET();
                    if (nPacketSize > 0)
                    {
                        cameraArr[i].MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                    }
                }
                // 设置连续采集模式

                //20220415
                cameraArr[i].MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
               // cameraArr[i].MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_SINGLE);
                cameraArr[i].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
            }
            return cameraArr;
        }

       
        // 关闭相机对象
        public void DestroyCamera()
        {
            if(camera != null)
            {
                // 停止采集
                camera.MV_CC_StopGrabbing_NET();
                // 关闭设备
                camera.MV_CC_CloseDevice_NET();
                camera.MV_CC_DestroyDevice_NET();
                camera = null;
            }  
        }

        public bool IsCamConn(int i)
        {
            return cameraArr[i].MV_CC_IsDeviceConnected_NET(); 
        }
        public void MultiDestroyCamera()
        {
            for (int i = 0; i < cameraNum; i++)
            {
                if (cameraArr[i] != null)
                {
                    // 停止采集
                    cameraArr[i].MV_CC_StopGrabbing_NET();
                    // 关闭设备
                    cameraArr[i].MV_CC_CloseDevice_NET();
                    cameraArr[i].MV_CC_DestroyDevice_NET();
                    cameraArr[i] = null;
                }
            }
        }


        #region //设置单项参数

        public bool SetPixelFormat(ref int iType, MyCamera cam)
        {
            uint enValue=0;
          
            switch (iType)
            {
                case 0:
                    enValue = (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8;
                    break;
                case 1:
                    enValue = (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed;
                    break;
            }
            int  nRet = cam.MV_CC_SetPixelFormat_NET(enValue);
            return nRet == MV_OK;

        }
       
        public bool SetResolution(ref uint iW,ref uint iH, MyCamera cam)
        {
            int bSuccW = 0, bSuccH = 0;
             MVCC_INTVALUE nValue = new MVCC_INTVALUE();
            cam.MV_CC_GetWidth_NET(ref nValue);
            uint maxV = nValue.nMax;
            uint minV = nValue.nMin;
            // 判断是否处于最小到最大值之间
            if (iW < minV)
            {
                iW = minV;
            }
            else if (iW > maxV)
            {
                iW = maxV;
            }
            bSuccW = cam.MV_CC_SetWidth_NET(iW);

            cam.MV_CC_GetHeight_NET(ref nValue);
              maxV = nValue.nMax;
              minV = nValue.nMin;
            // 判断是否处于最小到最大值之间
            if (iH < minV)
            {
                iH = minV;
            }
            else if (iH > maxV)
            {
                iH = maxV;
            }
            bSuccH=cam.MV_CC_SetHeight_NET(iH);

            if (bSuccW + bSuccH != 0)
                return false;
            else
                return true;
        }
       
        public bool SetBrightness(ref uint value, MyCamera cam)
        {
            MyCamera.MVCC_INTVALUE gainValue = new MVCC_INTVALUE();
            cam.MV_CC_GetBrightness_NET(ref gainValue);
            uint maxV = gainValue.nMax;
            uint minV = gainValue.nMin;
            // 判断是否处于最小到最大值之间
            if (value < minV)
            {
                value = minV;
            }
            else if (value > maxV)
            {
                value = maxV;
            }
            // 关闭自动设置
            cam.MV_CC_SetEnumValue_NET("BrightnessAuto", 0);
            // 设置Sharpness  
           int nRet= cam.MV_CC_SetBrightness_NET((uint)value);
            return nRet == MV_OK;
        }
        public bool SetSharpness(ref uint value, MyCamera cam)
        {
             MVCC_INTVALUE gainValue = new MVCC_INTVALUE();
            cam.MV_CC_GetSharpness_NET(ref gainValue); 
            uint maxV = gainValue.nMax;
            uint minV = gainValue.nMin;
            // 判断是否处于最小到最大值之间
            if (value < minV)
            {
                value = minV;
            }
            else if (value > maxV)
            {
                value = maxV;
            }
            // 关闭自动设置
            cam.MV_CC_SetEnumValue_NET("SharpnessAuto", 0);
            // 设置Sharpness 
            int nRet = cam.MV_CC_SetSharpness_NET(value);
            return nRet == MV_OK;
        }
        public bool SetGamma(ref float value, MyCamera cam)
        {
            MyCamera.MVCC_FLOATVALUE gainValue = new MyCamera.MVCC_FLOATVALUE();
            cam.MV_CC_GetGamma_NET(ref gainValue);
            //float gain = gainValue.fCurValue;
            float maxGain = gainValue.fMax;
            float minGain = gainValue.fMin;
            // 判断是否处于最小到最大值之间
            if (value < minGain)
            {
                value = minGain;
            }
            else if (value > maxGain)
            {
                value = maxGain;
            }
            // 关闭自动设置
            cam.MV_CC_SetEnumValue_NET("GammaAuto", 0);
            // 设置Gamma 
            int nRet = cam.MV_CC_SetGamma_NET(value);
            return nRet == MV_OK;
        }

        public bool SetGain(ref float value, MyCamera cam)
        {
            // 增益
            // 获取当前Gain、最大Gain、最小Gain
            MyCamera.MVCC_FLOATVALUE gainValue = new MyCamera.MVCC_FLOATVALUE();
            cam.MV_CC_GetGain_NET(ref gainValue);
            //float gain = gainValue.fCurValue;
            float maxGain = gainValue.fMax;
            float minGain = gainValue.fMin;
            // 判断是否处于最小到最大值之间
            if (value < minGain)
            { 
                value = minGain;
            }
            else if (value > maxGain)
            { 
                value = maxGain;
            }
            // 关闭自动设置
            cam.MV_CC_SetEnumValue_NET("GainAuto", 0);
            // 设置Gain
           int nRet= cam.MV_CC_SetGain_NET(value);
            return nRet == MV_OK;
          //  cam.MV_CC_SetFloatValue_NET("Gain", (float)value); 
        }
        public bool SetExposure(ref float value, MyCamera cam)
        {
            // 获取当前Exposure、最大Exposure、最小Exposure
            MyCamera.MVCC_FLOATVALUE exposureValue = new MyCamera.MVCC_FLOATVALUE();
            cam.MV_CC_GetExposureTime_NET(ref exposureValue);
            //float exposure = exposureValue.fCurValue;
            float maxExposure = exposureValue.fMax;
            float minExposure = exposureValue.fMin;
            int nRet = 0;
            // 判断是否处于最小到最大值之间
            if (value <= 0)
            {
                cam.MV_CC_SetEnumValue_NET("ExposureAuto", (uint)MV_CAM_EXPOSURE_AUTO_MODE.MV_EXPOSURE_AUTO_MODE_CONTINUOUS);
                cam.MV_CC_GetExposureTime_NET(ref exposureValue);
                value = exposureValue.fCurValue;
            }
            else
            {
                if (value < minExposure && value > 0)
                {
                    value = minExposure;
                }
                else if (value > maxExposure)
                { 
                    value = maxExposure;
                }

                // 关闭自动设置
                cam.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                // 设置ExposureTime
                nRet=  cam.MV_CC_SetExposureTime_NET(value);
              
            }
            return nRet == MV_OK;
        }

        //设置相机参数
        public bool SetCamParam(CameraParam cp, MyCamera cam)
        {
            bool bSucc = true;
            bSucc &=SetExposure(ref cp.iExposeTime, cam);
            bSucc &= SetGain(ref cp.dGain, cam);
            bSucc &= SetSharpness(ref cp.iSharp, cam);
            bSucc &= SetGamma(ref cp.dGamma, cam);
            bSucc &= SetPixelFormat( ref cp.iMonoType, cam);
            bSucc &= SetBrightness(ref cp.iBrightness, cam);
            bSucc &= SetResolution(ref cp.iWidth, ref cp.iHeight, cam);
            return bSucc;
        }
        #endregion

        public bool GetCamParam(CameraParam cp, MyCamera cam)
        { 
            int nRet = 0;
            MVCC_INTVALUE nValue=new MVCC_INTVALUE();
            MVCC_FLOATVALUE fValue = new MVCC_FLOATVALUE();
            nRet += cam.MV_CC_GetExposureTime_NET(ref fValue);
            cp.iExposeTime = fValue.fCurValue;

            nRet += cam.MV_CC_GetGain_NET(ref fValue);
            cp.dGain = fValue.fCurValue;

            nRet += cam.MV_CC_GetGamma_NET(ref fValue);
            cp.dGamma = fValue.fCurValue;

            nRet += cam.MV_CC_GetBrightness_NET(ref nValue);
            cp.iBrightness = nValue.nCurValue;

            nRet += cam.MV_CC_GetSharpness_NET(ref nValue);
            cp.iSharp = nValue.nCurValue;

            nRet += cam.MV_CC_GetWidth_NET(ref nValue);
            cp.iWidth = nValue.nCurValue;

            nRet += cam.MV_CC_GetHeight_NET(ref nValue);
            cp.iHeight = nValue.nCurValue;

            nRet += GetPixelFormat(ref cp.iMonoType, cam);
            return nRet==MV_OK;

        }
        //获取单个参数
        public void GetResolution(ref uint iW, ref uint iH, MyCamera cam)
        {
            MVCC_INTVALUE mvccW = new MVCC_INTVALUE();
            MVCC_INTVALUE mvccH = new MVCC_INTVALUE();

            cam.MV_CC_GetWidth_NET(ref mvccW);
            cam.MV_CC_GetHeight_NET(ref mvccH);

            iW = mvccW.nCurValue;
            iH = mvccH.nCurValue;
        }
        public int  GetPixelFormat(ref int iPixelFormat, MyCamera cam)
        {
            int nRet = 0;
            MVCC_ENUMVALUE stEnumValue = new MyCamera.MVCC_ENUMVALUE();
            nRet= cam.MV_CC_GetPixelFormat_NET(ref stEnumValue);
            switch (stEnumValue.nCurValue)
            {
                case (uint)MvGvspPixelType.PixelType_Gvsp_Mono8:
                    iPixelFormat = 0;
                    break;
                case (uint)MvGvspPixelType.PixelType_Gvsp_RGB8_Packed:
                    iPixelFormat = 1;
                    break;
                default:
                    cam.MV_CC_SetPixelFormat_NET((uint)MvGvspPixelType.PixelType_Gvsp_Mono8);
                    iPixelFormat = 0;
                    break;
            }
            return nRet;
        }

        public bool GetROIParam(CameraParam cp, MyCamera cam)
        {
            int  nRet = 0;
            try
            { 
                MyCamera.MVCC_INTVALUE ROIValue = new MyCamera.MVCC_INTVALUE();
                nRet += cam.MV_CC_GetIntValue_NET("OffsetX", ref ROIValue);
                cp.iROI[0]= ROIValue.nCurValue;
                nRet += cam.MV_CC_GetIntValue_NET("OffsetY", ref ROIValue);
                cp.iROI[1] = ROIValue.nCurValue;
                nRet += cam.MV_CC_GetIntValue_NET("Width", ref ROIValue);
                cp.iROI[2] = ROIValue.nCurValue;
                nRet += cam.MV_CC_GetIntValue_NET("Height", ref ROIValue);
                cp.iROI[3] = ROIValue.nCurValue;
                return nRet == MV_OK;
            }
            catch
            {
                return false;
            }
        }

        public bool SetROIParam(CameraParam cp, MyCamera cam)
        {
            int nRet = 0;
            try
            {
                nRet += cam.MV_CC_SetIntValue_NET("OffsetX", cp.iROI[0]);
                Thread.Sleep(10);
                nRet += cam.MV_CC_SetIntValue_NET("Width", cp.iROI[2]);
                Thread.Sleep(10);
                nRet += cam.MV_CC_SetIntValue_NET("OffsetY", cp.iROI[1]);
                Thread.Sleep(10);
                nRet += cam.MV_CC_SetIntValue_NET("Height", cp.iROI[3]);
                return nRet == MV_OK;
            }
            catch
            {
                return false;
            }

            
        }
        // 获取参数
        public void GetParam(ref string gain, ref string exposure, MyCamera cam)
        {
            

            MyCamera.MVCC_FLOATVALUE stParam = new MyCamera.MVCC_FLOATVALUE();
            // 获取曝光值
            int nRet = cam.MV_CC_GetFloatValue_NET("Gain", ref stParam);
           // int nRet = cam.MV_CC_GetFloatValue("Gain", ref stParam);
         
            if (MyCamera.MV_OK == nRet)
            {
                gain = stParam.fCurValue.ToString("F0");
            }
            // 获取增益
            nRet = cam.MV_CC_GetFloatValue_NET("ExposureTime", ref stParam);
            if (MyCamera.MV_OK == nRet)
            {
                exposure = stParam.fCurValue.ToString("F0");
            }
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

        // 开始采集
        public bool StartGrabbing(int i)
        {
            try
            {
                m_stFrameInfo.nFrameLen = 0; // 取流之前先清除帧长度
                m_stFrameInfo.enPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;
                if (cameraArr[i] == null)
                    cameraArr[i] = camera;
                cameraArr[i].MV_CC_StartGrabbing_NET(); // 开始Grab
                return true;
            }
            catch
            {
                return false;
            }
        }
        public void StartGrabbing()
        {
            m_stFrameInfo.nFrameLen = 0; // 取流之前先清除帧长度
            m_stFrameInfo.enPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;
            camera.MV_CC_StartGrabbing_NET(); // 开始Grab
        }

        // 停止采集
        public void StopGrabbing(int i)
        {
            if (cameraArr[i] != null)
            {
                cameraArr[i].MV_CC_StopGrabbing_NET();
            }
        }
        // 停止采集
        public void StopGrabbing()
        {
            if (camera != null)
            {
                camera.MV_CC_StopGrabbing_NET();
            }
        }
        // 类型转换
        public IntPtr HikConvert()
        {
            IntPtr pTemp = IntPtr.Zero;
            //MyCamera.MvGvspPixelType enDstPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;
            //if (m_stFrameInfo.enPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8 || m_stFrameInfo.enPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed)
            //{
            //    IntPtr m_BufForDriver;
            //    pTemp = m_BufForDriver;
            //    enDstPixelType = m_stFrameInfo.enPixelType;
            //}
            return pTemp;
        }


        //旋转镜像

        public bool HikRotateIm(MyCamera cam,MV_CC_ROTATE_IMAGE_PARAM pstRotateParam)
        {
            cam.MV_CC_RotateImage_NET(ref  pstRotateParam);

            return true;
        }
        public bool HikFlipIm(MyCamera cam, MV_CC_FLIP_IMAGE_PARAM pstFlipParam)
        {
            cam.MV_CC_FlipImage_NET(ref pstFlipParam);

            return true;
        }
    }
}
