
using System;
using System.Collections.Generic;
using System.Windows.Forms;
class MyCamera {
 public const int MV_OK=0, MV_E_SUPPORT=unchecked((int)0x80000001);
 public enum MvGvspPixelType {PixelType_Gvsp_Mono8=1,PixelType_Gvsp_BGR8_Packed=2,PixelType_Gvsp_RGB8_Packed=3,PixelType_Gvsp_BayerRG8=4,PixelType_Gvsp_BayerGB8=5,PixelType_Gvsp_BayerGR8=6,PixelType_Gvsp_BayerBG8=7}
 public enum MV_XML_AccessMode {AM_NI,AM_NA,AM_WO,AM_RO,AM_RW,AM_Undefined}
 public struct MVCC_FLOATVALUE { public float fCurValue,fMin,fMax; }
 public struct MVCC_INTVALUE { public uint nCurValue; }
 public struct MVCC_ENUMVALUE { public uint nCurValue; }
 public List<string> Reads=new List<string>(),Writes=new List<string>();
 public string FailNode=""; public uint Pixel=4, Auto=2; public MV_XML_AccessMode GammaAccess=MV_XML_AccessMode.AM_RW;
 public int GammaQueryRet=0; public int GammaWriteRet=0; public uint FormatSuccess=4;
 public int ReadFloat(string node,float value, ref MVCC_FLOATVALUE result) {Reads.Add(node); if(node==FailNode)return -9;result.fCurValue=value;result.fMin=0.1f;result.fMax=4;return 0;}
 public int MV_CC_GetExposureTime_NET(ref MVCC_FLOATVALUE result) {return ReadFloat("ExposureTime",500,ref result);}
 public int MV_CC_GetGain_NET(ref MVCC_FLOATVALUE result) {return ReadFloat("Gain",22.5f,ref result);}
 public int MV_CC_GetGamma_NET(ref MVCC_FLOATVALUE result) {return ReadFloat("Gamma",0.55f,ref result);}
 public int MV_CC_GetEnumValue_NET(string node,ref MVCC_ENUMVALUE result) {Reads.Add(node);result.nCurValue=Auto;return node==FailNode?-9:0;}
 public int MV_CC_GetPixelFormat_NET(ref MVCC_ENUMVALUE result) {Reads.Add("PixelFormat");result.nCurValue=Pixel;return FailNode=="PixelFormat"?-9:0;}
 public int MV_CC_GetIntValue_NET(string node,ref MVCC_INTVALUE result) {Reads.Add(node); result.nCurValue=node=="OffsetX"?20u:30u;return node==FailNode?-9:0;}
 public int MV_CC_GetWidth_NET(ref MVCC_INTVALUE result) {Reads.Add("Width");result.nCurValue=3072;return FailNode=="Width"?-9:0;}
 public int MV_CC_GetHeight_NET(ref MVCC_INTVALUE result) {Reads.Add("Height");result.nCurValue=2048;return FailNode=="Height"?-9:0;}
 public int MV_XML_GetNodeAccessMode_NET(string node,ref MV_XML_AccessMode mode) {Reads.Add(node+".Access");mode=GammaAccess;return GammaQueryRet;}
 public int MV_CC_SetPixelFormat_NET(uint value) {Writes.Add("PixelFormat="+value);return value==FormatSuccess?0:-6;}
 public int MV_CC_SetGamma_NET(float value) {Writes.Add("Gamma="+value);return GammaWriteRet;}
}
struct CameraParam { public float iExposeTime,dGain,dGamma; public uint iWidth,iHeight,iBrightness,iSharp; public int iMonoType; public uint[] iROI; }
class CameraCheck {
 MyCamera[] _cameraArray=new MyCamera[2];
 public List<string> Failures=new List<string>(); public event Action<string> CameraDiagnostic;
 public CameraCheck() {CameraDiagnostic+=s=>Failures.Add(s);}
 bool CheckSdkResult(MyCamera camera,string node,string target,int ret) {if(ret!=0)Failures.Add(node);return ret==0;}
 static float Clamp(float v,float min,float max) {return Math.Max(min,Math.Min(max,v));}
public bool GetCamParam(ref CameraParam param, MyCamera cam,
            out bool autoExposure, out bool gammaReadable)
        {
            autoExposure = false;
            gammaReadable = false;
            CameraParam local = param;
            var floatVal = new MyCamera.MVCC_FLOATVALUE();
            var intVal = new MyCamera.MVCC_INTVALUE();
            var enumVal = new MyCamera.MVCC_ENUMVALUE();

            if (!CheckSdkResult(cam, "ExposureTime.Read", "read", cam.MV_CC_GetExposureTime_NET(ref floatVal))) return false;
            local.iExposeTime = floatVal.fCurValue;
            if (!CheckSdkResult(cam, "ExposureAuto.Read", "read", cam.MV_CC_GetEnumValue_NET("ExposureAuto", ref enumVal))) return false;
            autoExposure = enumVal.nCurValue != 0;
            if (!CheckSdkResult(cam, "Gain.Read", "read", cam.MV_CC_GetGain_NET(ref floatVal))) return false;
            local.dGain = floatVal.fCurValue;
            if (!CheckSdkResult(cam, "Width.Read", "read", cam.MV_CC_GetWidth_NET(ref intVal))) return false;
            local.iWidth = intVal.nCurValue;
            if (!CheckSdkResult(cam, "Height.Read", "read", cam.MV_CC_GetHeight_NET(ref intVal))) return false;
            local.iHeight = intVal.nCurValue;
            if (!CheckSdkResult(cam, "OffsetX.Read", "read", cam.MV_CC_GetIntValue_NET("OffsetX", ref intVal))) return false;
            uint offsetX = intVal.nCurValue;
            if (!CheckSdkResult(cam, "OffsetY.Read", "read", cam.MV_CC_GetIntValue_NET("OffsetY", ref intVal))) return false;
            local.iROI = new uint[] { offsetX, intVal.nCurValue, local.iWidth, local.iHeight };
            if (GetPixelFormat(ref local.iMonoType, cam) != MyCamera.MV_OK) return false;

            // Gamma 可选；不可读时不伪造数值，也不把已连接设备改判为断开。
            var access = MyCamera.MV_XML_AccessMode.AM_Undefined;
            int accessRet = cam.MV_XML_GetNodeAccessMode_NET("Gamma", ref access);
            if (accessRet != MyCamera.MV_OK ||
                (access == MyCamera.MV_XML_AccessMode.AM_RO || access == MyCamera.MV_XML_AccessMode.AM_RW))
            {
                gammaReadable = CheckSdkResult(cam, "Gamma.Read", "read", cam.MV_CC_GetGamma_NET(ref floatVal));
                if (gammaReadable) local.dGamma = floatVal.fCurValue;
            }
            // iBrightness 为项目光源亮度；锐度停用。两者均不由相机读取覆盖。
            param = local;
            return true;
        }
public int GetPixelFormat(ref int format, MyCamera cam)
        {
            var enumVal = new MyCamera.MVCC_ENUMVALUE();
            int ret = cam.MV_CC_GetPixelFormat_NET(ref enumVal);
            if (!CheckSdkResult(cam, "PixelFormat.Read", "read", ret)) return ret;
            switch ((MyCamera.MvGvspPixelType)enumVal.nCurValue)
            {
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8:
                    format = 0;
                    return MyCamera.MV_OK;
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG8:
                    format = 1;
                    return MyCamera.MV_OK;
                default:
                    CameraDiagnostic?.Invoke($"cameraIndex={Array.IndexOf(_cameraArray, cam)}, node=PixelFormat.Read, 不支持当前格式=0x{enumVal.nCurValue:X8}，保留设备格式未写入");
                    return MyCamera.MV_E_SUPPORT;
            }
        }
public bool SetPixelFormat(ref int type, MyCamera cam)
        {
            // 黑白模式：直接设置 Mono8
            if (type == 0)
            {
                int ret = cam.MV_CC_SetPixelFormat_NET((uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8);
                CheckSdkResult(cam, "PixelFormat", "Mono8", ret);
                if (ret == MyCamera.MV_OK)
                {
                    // 确保外部类型为 0
                    type = 0;
                    return true;
                }
                return false;
            }

            // 彩色模式：依次尝试常见的彩色格式
            uint[] colorFormats = {
        (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed,
        (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed,
        (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG8,
        (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB8,
        (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR8,
        (uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG8
    };

            foreach (uint fmt in colorFormats)
            {
                int ret = cam.MV_CC_SetPixelFormat_NET(fmt);
                // 候选失败属于格式适配过程；任一候选成功即成功，不产生SDK失败事件。
                if (ret == MyCamera.MV_OK)
                {
                    // 彩色格式设置成功，保持 type = 1
                    type = 1;
                    return true;
                }
            }

            // 所有彩色格式都失败，回退到黑白模式
            int fallbackRet = cam.MV_CC_SetPixelFormat_NET((uint)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8);
            if (CheckSdkResult(cam, "PixelFormat", "Mono8 (原有回退)", fallbackRet))
            {
                type = 0;
                return true;
            }

            // 彻底失败
            type = 0;
            return false;
        }
public bool SetGamma(ref float value, MyCamera cam)
        {
            var range = new MyCamera.MVCC_FLOATVALUE();
            if (!CheckSdkResult(cam, "Gamma.GetRange", value.ToString(), cam.MV_CC_GetGamma_NET(ref range))) return false;
            float adjusted = Clamp(value, range.fMin, range.fMax);
            if (!CheckSdkResult(cam, "Gamma", adjusted.ToString(), cam.MV_CC_SetGamma_NET(adjusted))) return false;
            value = adjusted;
            return true;
        }
}
class UIHelpers {
public static void SetCameraNumericValue(NumericUpDown control, decimal value)
        {
            if (value < control.Minimum) control.Minimum = value;
            if (value > control.Maximum) control.Maximum = value;
            control.Value = value;
        }
public static void SetCameraTrackValue(TrackBar control, int value)
        {
            if (value < control.Minimum) control.Minimum = value;
            if (value > control.Maximum) control.Maximum = value;
            control.Value = value;
        }
}

class ReconnectionCheck {
 const int maxJobNum=2;
 readonly object _cameraConnectionSync=new object();
 public bool _cameraConnectionInProgress; public volatile bool _cameraManuallyDisconnected;
 public int Enumerations,Connections,Closes; public bool CloseSucceeded=true;
 class CD {public bool[] bListProcess=new[]{true,true};}
 class CUP {public CD p_CD=new CD();}
 class Init {public bool bCamPrepared,bCamInit;}
 class RTS {public Init initStatus=new Init();}
 class VP {public bool bMainTaskFinished=true;}
 class Oper {public int iEventStatus;public string strOperEvent,strFailReason;}
 class Acq {public object[] _HikCam;}
 class Device {public object[] GetCameraArray(){return new object[2];}}
 CUP cup=new CUP(); RTS rts=new RTS(); VP vpWin=new VP();Oper operLog=new Oper();Acq acqFIFO=new Acq();Device hIKVisionCamera=new Device();object[] camera2;
 void LogCameraConnection(string op,bool success,string detail){}
 void AddLog(){} void ShowCurrentTaskNotFinishedWarning(){} void SaveUserParam(){}
 bool RefreshCameraDevices(string operation,out List<string> items){Enumerations++;items=new List<string>{"A","B"};return true;}
 bool TryMapJobsByCameraSerial(List<string> items,out bool changed){changed=false;return true;}
 bool ConnectJobCamera(int jobIndex,List<string> items,string operation,bool mappingValid){Connections++;return true;}
 bool CloseCam(){Closes++;return CloseSucceeded;}
public bool TryConnectCameras(string operation)
        {
            lock (_cameraConnectionSync)
            {
                if (_cameraConnectionInProgress)
                {
                    LogCameraConnection(operation, false, "已有连接操作正在进行，忽略本次重复进入");
                    return false;
                }
                if (operation == "自动重连" && _cameraManuallyDisconnected) return false;
                if (operation == "手动连接") _cameraManuallyDisconnected = false;
                _cameraConnectionInProgress = true;
            }

            try
            {
                List<string> latestItems;
                if (!RefreshCameraDevices(operation, out latestItems))
                    return false;

                bool mappingChanged;
                bool mappingValid = TryMapJobsByCameraSerial(latestItems, out mappingChanged);
                if (mappingChanged)
                    SaveUserParam();

                camera2 = hIKVisionCamera.GetCameraArray();
                acqFIFO._HikCam = camera2;

                int successCount = 0;
                int enabledCount = 0;
                for (int jobIndex = 0; jobIndex < maxJobNum; jobIndex++)
                {
                    if (!cup.p_CD.bListProcess[jobIndex])
                        continue;
                    enabledCount++;
                    if (ConnectJobCamera(jobIndex, latestItems, operation, mappingValid))
                        successCount++;
                }

                bool allSucceeded = mappingValid && enabledCount > 0 && successCount == enabledCount;
                rts.initStatus.bCamPrepared = allSucceeded;
                rts.initStatus.bCamInit = allSucceeded;
                LogCameraConnection(operation, allSucceeded,
                    $"连接结果: 成功={successCount}, 启用工位={enabledCount}, 枚举设备={latestItems.Count}, 映射有效={mappingValid}");
                return allSucceeded;
            }
            finally
            {
                lock (_cameraConnectionSync)
                {
                    _cameraConnectionInProgress = false;
                }
            }
        }
public void menuCam_Close_Click(object sender, EventArgs e)
        {
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "断开相机";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                //   MessageBox.Show("当前任务没有结束，不能执行该操作");
                return;
            }
            lock (_cameraConnectionSync)
            {
                if (_cameraConnectionInProgress)
                {
                    LogCameraConnection("断开相机", false, "相机连接操作尚未结束");
                    return;
                }
                _cameraManuallyDisconnected = true;
                _cameraConnectionInProgress = true;
            }
            try
            {
                if (!CloseCam()) return;
                rts.initStatus.bCamInit = false;
                rts.initStatus.bCamPrepared = false;
                LogCameraConnection("断开相机", true, "设备连接已释放，等待手动连接");
            }
            finally
            {
                lock (_cameraConnectionSync) _cameraConnectionInProgress = false;
            }
        }
}

class Program {
 static int checks;
 static void Check(bool condition,string name) {if(!condition)throw new Exception(name);Console.WriteLine("PASS "+name);checks++;}
 static CameraParam Initial() {return new CameraParam {iExposeTime=9999,dGamma=1,iBrightness=49,iSharp=77,iWidth=12,iROI=new uint[]{1,2,12,13}};}
 [STAThread] static void Main() {
  var device=new MyCamera();var sut=new CameraCheck();var p=Initial();var originalROI=p.iROI;bool auto,gamma;
  Check(sut.GetCamParam(ref p,device,out auto,out gamma),"read succeeds");
  Check(device.Writes.Count==0,"read performs zero SDK writes");
  Check(p.iExposeTime==500 && p.dGain==22.5f && p.dGamma==0.55f,"actual values returned by ref");
  Check(auto && gamma && p.iMonoType==1,"auto mode preserved and BayerRG8 is color");
  Check(p.iROI[0]==20 && p.iROI[1]==30 && p.iROI[2]==3072 && p.iROI[3]==2048 && originalROI[0]==1,"actual ROI with separate array");
  Check(p.iBrightness==49 && p.iSharp==77 && !device.Reads.Contains("Sharpness") && !device.Reads.Contains("Brightness"),"light and sharpness untouched");
  foreach(string node in new[]{"ExposureTime","ExposureAuto","Gain","Width","Height","OffsetX","OffsetY","PixelFormat"}) {
   device=new MyCamera{FailNode=node};p=Initial();originalROI=p.iROI;sut=new CameraCheck();
   Check(!sut.GetCamParam(ref p,device,out auto,out gamma) && p.iExposeTime==9999 && Object.ReferenceEquals(p.iROI,originalROI) && device.Writes.Count==0,"failed "+node+" read leaves caller untouched");
  }
  device=new MyCamera{GammaAccess=MyCamera.MV_XML_AccessMode.AM_NA};sut=new CameraCheck();p=Initial();
  Check(sut.GetCamParam(ref p,device,out auto,out gamma) && !gamma && p.dGamma==1 && !device.Reads.Contains("Gamma") && sut.Failures.Count==0,"unavailable gamma does not block connection or fake value");
  device=new MyCamera{GammaQueryRet=-1,FailNode="Gamma"};sut=new CameraCheck();p=Initial();
  Check(sut.GetCamParam(ref p,device,out auto,out gamma) && !gamma && sut.Failures.Count==1,"real gamma read failure reported independently");
  device=new MyCamera{Pixel=99};sut=new CameraCheck();p=Initial();
  Check(!sut.GetCamParam(ref p,device,out auto,out gamma) && device.Writes.Count==0 && p.iExposeTime==9999,"unsupported pixel format retained on device");
  foreach(uint value in new uint[]{1,2,3,4,5,6,7}) {
   device=new MyCamera{Pixel=value};sut=new CameraCheck();int format=-1;
   Check(sut.GetPixelFormat(ref format,device)==0 && format==(value==1?0:1),"pixel classification "+value);
  }
  foreach(uint value in new uint[]{2,3,4,5,6,7,1}) {
   device=new MyCamera{FormatSuccess=value};sut=new CameraCheck();int format=1;
   Check(sut.SetPixelFormat(ref format,device) && sut.Failures.Count==0 && format==(value==1?0:1),"candidate/fallback succeeds without failure event "+value);
  }
  device=new MyCamera{FormatSuccess=99};sut=new CameraCheck();int f=1;
  Check(!sut.SetPixelFormat(ref f,device) && sut.Failures.Count==1 && device.Writes.Count==7,"all candidates and fallback fail: one actual failure");
  device=new MyCamera();sut=new CameraCheck();float g=0.55f;
  Check(sut.SetGamma(ref g,device) && device.Writes.Count==1 && device.Writes[0].StartsWith("Gamma="),"manual gamma writes only Gamma");
  device=new MyCamera{FailNode="Gamma"};sut=new CameraCheck();g=0.55f;
  Check(!sut.SetGamma(ref g,device) && g==0.55f && device.Writes.Count==0,"gamma range read failure never writes zero");
  device=new MyCamera{GammaWriteRet=-1};sut=new CameraCheck();g=0.55f;
  Check(!sut.SetGamma(ref g,device) && sut.Failures.Count==1,"manual gamma write failure retained");
  var numeric=new NumericUpDown {Minimum=1000,Maximum=100000};var track=new TrackBar {Minimum=1000,Maximum=100000};
  UIHelpers.SetCameraNumericValue(numeric,500);UIHelpers.SetCameraTrackValue(track,500);
  Check(numeric.Value==500 && track.Value==500,"UI preserves exposure below old design minimum");
  UIHelpers.SetCameraNumericValue(numeric,200000);UIHelpers.SetCameraTrackValue(track,200000);
  Check(numeric.Value==200000 && track.Value==200000,"UI preserves exposure above old design maximum");
  var connection=new ReconnectionCheck();connection.menuCam_Close_Click(null,EventArgs.Empty);
  Check(connection.Closes==1 && connection._cameraManuallyDisconnected && !connection._cameraConnectionInProgress,"manual disconnect releases connection and pauses reconnect");
  Check(!connection.TryConnectCameras("自动重连") && connection.Enumerations==0,"paused automatic reconnect performs no device calls");
  Check(connection.TryConnectCameras("手动连接") && !connection._cameraManuallyDisconnected && connection.Connections==2,"manual connection resumes and connects both jobs");
  connection._cameraConnectionInProgress=true;connection.menuCam_Close_Click(null,EventArgs.Empty);
  Check(connection.Closes==1,"disconnect cannot release during active reconnect");
  connection=new ReconnectionCheck{CloseSucceeded=false};connection.menuCam_Close_Click(null,EventArgs.Empty);
  Check(connection._cameraManuallyDisconnected && !connection._cameraConnectionInProgress && !connection.TryConnectCameras("自动重连"),"failed disconnect retains manual pause and unlocks lifecycle");
  Console.WriteLine(checks+" executable behaviour checks passed");
 }
}
