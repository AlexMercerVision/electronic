
using System;
using System.Collections.Generic;
class MyCamera {
 public const int MV_OK=0;
 public enum MV_CAM_EXPOSURE_AUTO_MODE { MV_EXPOSURE_AUTO_MODE_CONTINUOUS=2 }
 public struct MVCC_INTVALUE { public uint nMin,nMax; }
 public struct MVCC_FLOATVALUE { public float fMin,fMax; }
 public string Fault; public List<string> Calls=new List<string>();
 int Result(string node){return node==Fault?unchecked((int)0x80000106):0;}
 int GetInt(string node,ref MVCC_INTVALUE v){
  Calls.Add(node); int r=Result(node); if(r==0){v.nMin=0;v.nMax=100;} return r;
 }
 int GetFloat(string node,ref MVCC_FLOATVALUE v){
  Calls.Add(node);int r=Result(node);if(r==0){v.fMin=0;v.fMax=10000;}return r;
 }
 int Set(string node,object value){Calls.Add(node+"="+value);return Result(node);}
 public int MV_CC_GetSharpness_NET(ref MVCC_INTVALUE v){return GetInt("Sharpness.GetRange",ref v);}
 public int MV_CC_SetSharpness_NET(uint v){return Set("Sharpness",v);}
 public int MV_CC_GetGamma_NET(ref MVCC_FLOATVALUE v){return GetFloat("Gamma.GetRange",ref v);}
 public int MV_CC_SetGamma_NET(float v){return Set("Gamma",v);}
 public int MV_CC_GetWidth_NET(ref MVCC_INTVALUE v){return GetInt("Width.GetRange",ref v);}
 public int MV_CC_GetHeight_NET(ref MVCC_INTVALUE v){return GetInt("Height.GetRange",ref v);}
 public int MV_CC_SetWidth_NET(uint v){return Set("Width",v);}
 public int MV_CC_SetHeight_NET(uint v){return Set("Height",v);}
 public int MV_CC_GetExposureTime_NET(ref MVCC_FLOATVALUE v){return GetFloat("ExposureTime.GetRange",ref v);}
 public int MV_CC_SetFloatValue_NET(string n,float v){return Set(n,v);}
 public int MV_CC_SetEnumValue_NET(string n,uint v){return Set(n,v);}
}
class Legacy : MyCamera {
public bool SetSharpness(ref uint value, MyCamera cam)
        {
            var range = new MyCamera.MVCC_INTVALUE();
            cam.MV_CC_GetSharpness_NET(ref range);
            value = Clamp(value, range.nMin, range.nMax);
            cam.MV_CC_SetEnumValue_NET("SharpnessAuto", 0);
            int ret = cam.MV_CC_SetSharpness_NET(value);
            return ret == MyCamera.MV_OK;
        }
public bool SetGamma(ref float value, MyCamera cam)
        {
            var range = new MyCamera.MVCC_FLOATVALUE();
            cam.MV_CC_GetGamma_NET(ref range);
            value = Clamp(value, range.fMin, range.fMax);
            cam.MV_CC_SetEnumValue_NET("GammaAuto", 0);
            int ret = cam.MV_CC_SetGamma_NET(value);
            return ret == MyCamera.MV_OK;
        }
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
}
class Restored : MyCamera {
public bool SetSharpness(ref uint value, MyCamera cam)
        {
            var range = new MyCamera.MVCC_INTVALUE();
            // 恢复旧版调用顺序：范围/自动模式错误只记录，最终写入仍决定结果。
            CheckSdkResult(cam, "Sharpness.GetRange", value.ToString(), cam.MV_CC_GetSharpness_NET(ref range));
            uint adjusted = Clamp(value, range.nMin, range.nMax);
            SetEnumValueChecked(cam, "SharpnessAuto", 0);
            if (!CheckSdkResult(cam, "Sharpness", adjusted.ToString(), cam.MV_CC_SetSharpness_NET(adjusted))) return false;
            value = adjusted;
            return true;
        }
public bool SetGamma(ref float value, MyCamera cam)
        {
            var range = new MyCamera.MVCC_FLOATVALUE();
            CheckSdkResult(cam, "Gamma.GetRange", value.ToString(), cam.MV_CC_GetGamma_NET(ref range));
            float adjusted = Clamp(value, range.fMin, range.fMax);
            SetEnumValueChecked(cam, "GammaAuto", 0);
            if (!CheckSdkResult(cam, "Gamma", adjusted.ToString(), cam.MV_CC_SetGamma_NET(adjusted))) return false;
            value = adjusted;
            return true;
        }
public bool SetResolution(ref uint width, ref uint height, MyCamera cam)
        {
            var range = new MyCamera.MVCC_INTVALUE();
            CheckSdkResult(cam, "Width.GetRange", width.ToString(), cam.MV_CC_GetWidth_NET(ref range));
            uint newWidth = Clamp(width, range.nMin, range.nMax);
            bool widthSet = CheckSdkResult(cam, "Width", newWidth.ToString(), cam.MV_CC_SetWidth_NET(newWidth));

            CheckSdkResult(cam, "Height.GetRange", height.ToString(), cam.MV_CC_GetHeight_NET(ref range));
            uint newHeight = Clamp(height, range.nMin, range.nMax);
            bool heightSet = CheckSdkResult(cam, "Height", newHeight.ToString(), cam.MV_CC_SetHeight_NET(newHeight));
            if (!widthSet || !heightSet) return false;

            width = newWidth;
            height = newHeight;
            return true;
        }
public bool SetParam(float gain, float exposure, ref string gainString, ref string exposureString, MyCamera cam)
        {


            // 曝光时间
            // 获取当前Exposure、最大Exposure、最小Exposure
            MyCamera.MVCC_FLOATVALUE exposureValue = new MyCamera.MVCC_FLOATVALUE();
            bool success = CheckSdkResult(cam, "ExposureTime.GetRange", exposure.ToString(), cam.MV_CC_GetExposureTime_NET(ref exposureValue));
            //float exposure = exposureValue.fCurValue;
            float maxExposure = exposureValue.fMax;
            float minExposure = exposureValue.fMin;
            // 判断是否处于最小到最大值之间
            if (exposure <= 0)
            {
                success &= SetEnumValueChecked(cam, "ExposureAuto", (uint)MV_CAM_EXPOSURE_AUTO_MODE.MV_EXPOSURE_AUTO_MODE_CONTINUOUS);
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
                success &= SetEnumValueChecked(cam, "ExposureAuto", 0);
                // 设置ExposureTime
                success &= CheckSdkResult(cam, "ExposureTime", exposure.ToString(), cam.MV_CC_SetFloatValue_NET("ExposureTime", exposure));
                exposureString = exposure.ToString("F0");
            }
            return success;
        }

public List<string> Failures = new List<string>();
bool CheckSdkResult(MyCamera cam,string node,string target,int result) {
 if(result!=0) Failures.Add(node+":"+result); return result==0;
}
bool SetEnumValueChecked(MyCamera cam,string node,uint value) {
 return CheckSdkResult(cam,node,value.ToString(),cam.MV_CC_SetEnumValue_NET(node,value));
}
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
}

class CamParam {public string strCamName="DA8696070";}
class Param { public CamParam[] p_Cam={new CamParam(),new CamParam()}; }
class State {public int[] iStatus={0,0};}
class Runtime {public State cs=new State();}
class Oper {public string strOperEvent,strFailReason;public int iEventStatus;}
class GrabSDK {public int Result,Calls;public int StartGrabbing(int index){Calls++;return Result;}}
class StartHarness {
 Param cup=new Param(); public Runtime rts=new Runtime();
 public GrabSDK hIKVisionCamera=new GrabSDK(); Oper operLog=new Oper();
 public bool ReadResult;uint cameraNum=2; MyCamera[] camera2=new MyCamera[2];
 public List<string> Logs=new List<string>();
 bool GetCameraRuntimeState(int i,out string s){s="readback";return ReadResult;}
 void SyncCameraStatus(int i,int s){rts.cs.iStatus[i]=s;}
 void WriteCameraDiagnostic(string op,int s,string message){Logs.Add(message);}
 void AddLog(){}
 public bool Run(){return StartGrab(0);}
private bool StartGrab(int iCamInd)
        {
            try
            {
                int sdkResult = hIKVisionCamera.StartGrabbing(iCamInd);
                bool success = sdkResult == MyCamera.MV_OK;

                string serial = cup.p_Cam != null && iCamInd >= 0 && iCamInd < cup.p_Cam.Length
                    ? cup.p_Cam[iCamInd].strCamName
                    : string.Empty;
                string runtimeState;
                GetCameraRuntimeState(iCamInd, out runtimeState);
                SyncCameraStatus(iCamInd, success ? 3 : 1);

                operLog.strOperEvent = $"启动相机采集-Cam{iCamInd}";
                operLog.iEventStatus = success ? 0 : -1;
                operLog.strFailReason =
                    $"cameraIndex={iCamInd}, serial={serial}, StartGrabbing SDK=0x{unchecked((uint)sdkResult):X8}";
                WriteCameraDiagnostic(operLog.strOperEvent, operLog.iEventStatus, operLog.strFailReason);
                AddLog();
                return success;
            }
            catch (Exception ex)
            {
                SyncCameraStatus(iCamInd, 1);
                operLog.strOperEvent = $"启动相机采集-Cam{iCamInd}";
                operLog.iEventStatus = -1;
                operLog.strFailReason =
                    $"阶段=StartGrabbing, Cam{iCamInd}, cameraNum={cameraNum}, " +
                    $"cameraArrayLength={(camera2 == null ? 0 : camera2.Length)}, exception={ex}";
                WriteCameraDiagnostic(operLog.strOperEvent, -1, operLog.strFailReason);
                AddLog();
                return false;
            }
        }
}

class Tests {
 static int passed;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);passed++;}
 static void Compare(string fault,int kind) {
  var oldCam=new MyCamera{Fault=fault};var newCam=new MyCamera{Fault=fault};
  var legacy=new Legacy();var restored=new Restored();bool oldResult=true,newResult=true;
  if(kind==0){uint a=0,b=0;oldResult=legacy.SetSharpness(ref a,oldCam);newResult=restored.SetSharpness(ref b,newCam);}
  if(kind==1){float a=3,b=3;oldResult=legacy.SetGamma(ref a,oldCam);newResult=restored.SetGamma(ref b,newCam);}
  if(kind==2){uint a=80,b=70,c=80,d=70;oldResult=legacy.SetResolution(ref a,ref b,oldCam);newResult=restored.SetResolution(ref c,ref d,newCam);}
  if(kind==3){string a=null,b=null,c=null,d=null;legacy.SetParam(3,1000,ref a,ref b,oldCam);restored.SetParam(3,1000,ref c,ref d,newCam);}
  Check(string.Join("|",oldCam.Calls)==string.Join("|",newCam.Calls),"SDK sequence/values: "+fault);
  Check(oldResult==newResult,"legacy write result: "+fault);
  Check(fault==null||restored.Failures.Count>0,"failed SDK call logged: "+fault);
 }
 static void Main(){
  foreach(var f in new string[]{null,"Sharpness.GetRange","SharpnessAuto","Sharpness"})Compare(f,0);
  foreach(var f in new string[]{null,"Gamma.GetRange","GammaAuto","Gamma"})Compare(f,1);
  foreach(var f in new string[]{null,"Width.GetRange","Width","Height.GetRange","Height"})Compare(f,2);
  foreach(var f in new string[]{null,"ExposureTime.GetRange","ExposureAuto","ExposureTime"})Compare(f,3);
  foreach(bool read in new[]{false,true})foreach(int ret in new[]{0,unchecked((int)0x80000003)}){
   var h=new StartHarness{ReadResult=read};h.hIKVisionCamera.Result=ret;
   Check(h.Run()==(ret==0),"Start returns actual SDK result");
   Check(h.hIKVisionCamera.Calls==1,"initial state0 still calls Start once");
   Check(h.rts.cs.iStatus[0]==(ret==0?3:1),"readback does not falsify grabbing state");
   Check(h.Logs.Exists(x=>x.Contains("SDK=0x"+unchecked((uint)ret).ToString("X8"))),"raw Start result logged");
  }
  Console.WriteLine("PASS "+passed+" legacy-regression checks (fake SDK only)");
 }
}
