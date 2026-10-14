from pathlib import Path
import subprocess

root = Path('E:/jieyawenjian/VB_Det')
out = Path(__file__).parent

def method(source, signature):
    start = source.index(signature)
    brace = source.index('{', start)
    depth = 1
    end = brace + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]

old = subprocess.check_output([
    'E:/Git/cmd/git.exe', '-c', 'safe.directory=E:/jieyawenjian/VB_Det',
    'show', 'HEAD:CommonCls/HIKDeviceSource.cs'], cwd=root).decode('utf-8-sig')
current = (root/'CommonCls/HIKDeviceSource.cs').read_text(encoding='utf-8-sig')
ui = (root/'VisionApplication/FrmMainUI.cs').read_text(encoding='utf-8-sig')
signatures = [
    'public bool SetSharpness(', 'public bool SetGamma(',
    'public bool SetResolution(', 'public void SetParam('
]
clamps = '\n'.join(method(current, s) for s in [
    'private static uint Clamp(', 'private static float Clamp('])
helpers = '''
public List<string> Failures = new List<string>();
bool CheckSdkResult(MyCamera cam,string node,string target,int result) {
 if(result!=0) Failures.Add(node+":"+result); return result==0;
}
bool SetEnumValueChecked(MyCamera cam,string node,uint value) {
 return CheckSdkResult(cam,node,value.ToString(),cam.MV_CC_SetEnumValue_NET(node,value));
}
'''
old_methods = '\n'.join(method(old, s) for s in signatures)
signatures[-1] = 'public bool SetParam('
new_methods = '\n'.join(method(current, s) for s in signatures)
prefix = r'''
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
'''
prefix += 'class Legacy : MyCamera {\n' + old_methods + '\n' + clamps + '\n}\n'
prefix += 'class Restored : MyCamera {\n' + new_methods + '\n' + helpers + clamps + '\n}\n'
start = method(ui, 'private bool StartGrab(int iCamInd)')
prefix += r'''
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
'''
prefix += start + '\n}\n'
prefix += r'''
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
'''
(out/'LegacyComparison.cs').write_text(prefix, encoding='utf-8-sig')
compiler='E:/VS-IDE/MSBuild/Current/Bin/Roslyn/csc.exe'
subprocess.run([compiler,'/nologo','/langversion:7.3','/out:'+str(out/'LegacyComparison.exe'),str(out/'LegacyComparison.cs')],check=True)
subprocess.run([str(out/'LegacyComparison.exe')],check=True)
