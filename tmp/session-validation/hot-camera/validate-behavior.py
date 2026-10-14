from pathlib import Path
root = Path('E:/jieyawenjian/VB_Det')
def extract(path, begin, end):
    text = (root/path).read_text(encoding='utf-8-sig')
    a = text.index(begin)
    return text[a:text.index(end, a)]
preview = extract('VisionControl/VisionControlExtention.cs', '        public bool StopPreviewForCamera', '        private void AdjustPictureBoxSize')
thread = extract('VisionControl/VisionControlExtention.cs', '        private string ReadPreviewAcquisitionState', '        private Bitmap CreatePreviewBitmap')
fields = extract('VisionControl/VisionControlExtention.cs', '        private readonly object _previewLifecycleSync', '        private void InitPreviewLocks')
restore = extract('VisionApplication/FrmMainUI.cs', '        private bool SetCamParam(int iCamIndex)', '        private bool StartGrab(int iCamInd)')
sharpness = extract('CommonCls/HIKDeviceSource.cs', '        public bool SetSharpness(ref uint value, MyCamera cam)', '        /// <summary>\n        /// 设置伽马值')
prefix = r'''
using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
class Image { public void Dispose() {} }
class Control { public IntPtr Handle = (IntPtr)1; public bool IsHandleCreated = true; public void CreateControl() {} }
class PictureBox:Control { public Image Image; }
class CogRecordDisplay:Control { public Image Image; }
struct CameraParam { public int iMonoType; public float dGain,iExposeTime,dGamma; public uint iSharp,iWidth,iHeight; public uint[] iROI; public string strCamName; public int iFlipType; }
class CD { public int[] camInd={0,1}; public int triggleCam=1,triggleMode=1; public bool bTriggle; }
class Param { public CameraParam[] p_Cam = {
 new CameraParam {strCamName="DA8696070",iROI=new uint[]{0,0,3072,2048},iWidth=3072,iHeight=2048,iMonoType=1,iSharp=7,dGamma=0.55f},
 new CameraParam {strCamName="DA8696075",iROI=new uint[]{0,0,3072,2048},iWidth=3072,iHeight=2048,iMonoType=1,iSharp=7,dGamma=3}}; public CD p_CD=new CD(); }
class MyCamera {
 public const int MV_OK=0,MV_E_NODATA=unchecked((int)0x80000007);
 public enum MV_CAM_TRIGGER_MODE {MV_TRIGGER_MODE_ON=1,MV_TRIGGER_MODE_OFF=0}
 public enum MV_CAM_TRIGGER_SOURCE {MV_TRIGGER_SOURCE_SOFTWARE=7,MV_TRIGGER_SOURCE_LINE0=0}
 public enum MV_IMG_FLIP_TYPE {MV_FLIP_HORIZONTAL,MV_FLIP_VERTICAL}
 public struct MVCC_ENUMVALUE {public uint nCurValue;}
 public struct MVCC_INTVALUE {public uint nMin,nMax;}
 public struct MV_FRAME_OUT_INFO_EX {public uint nFrameLen,nWidth,nHeight;public int enPixelType;}
 public struct MV_FRAME_OUT {public MV_FRAME_OUT_INFO_EX stFrameInfo; public IntPtr pBufAddr;}
 public struct MV_DISPLAY_FRAME_INFO {public IntPtr hWnd,pData;public uint nDataLen,nWidth,nHeight;public int enPixelType;}
 public struct MV_CC_FLIP_IMAGE_PARAM {public int enPixelType;public uint nWidth,nHeight,nSrcDataLen,nDstBufLen,nDstBufSize;public IntPtr pSrcData,pDstBuf;public MV_IMG_FLIP_TYPE enFlipType;public uint[] nRes;}
 public Func<int> Next; public Action OnGet; public int GetCalls,FreeCalls,FreeRet,RangeRet,SetCalls; public bool DisplayThrows,FreeThrows; public bool Connected=true;
 public int MV_CC_GetImageBuffer_NET(ref MV_FRAME_OUT f,int timeout) {GetCalls++;OnGet?.Invoke(); f.stFrameInfo.nFrameLen=1;f.stFrameInfo.nWidth=1;f.stFrameInfo.nHeight=1;return Next==null?MV_E_NODATA:Next();}
 public int MV_CC_FreeImageBuffer_NET(ref MV_FRAME_OUT f) {FreeCalls++;if(FreeThrows)throw new InvalidOperationException("free-test");return FreeRet;}
 public int MV_CC_DisplayOneFrame_NET(ref MV_DISPLAY_FRAME_INFO f) {if(DisplayThrows)throw new InvalidOperationException("display-test");return 0;}
 public int MV_CC_FlipImage_NET(ref MV_CC_FLIP_IMAGE_PARAM f) {return 0;}
 public bool MV_CC_IsDeviceConnected_NET() {return Connected;}
 public int MV_CC_GetEnumValue_NET(string node,ref MVCC_ENUMVALUE v) {v.nCurValue=node=="AcquisitionMode"?2u:0u;return 0;}
 public int MV_CC_GetSharpness_NET(ref MVCC_INTVALUE v) {v.nMin=0;v.nMax=10;return RangeRet;}
 public int MV_CC_SetSharpness_NET(uint value) {SetCalls++;return 0;}
}
class FIFO {
 public MyCamera[] _HikCam={new MyCamera(),new MyCamera()}; public object[] Locks={new object(),new object()}; public int[] Owners=new int[2];
 public bool IsCameraGrabbing(int i){return true;}
 public object GetCameraFrameLock(int i){return Locks[i];}
 public uint GetPayloadSize(int i){return 1;}
 public bool TryReservePreview(int i,int job){lock(Locks[i]){if(Owners[i]!=0)return false;Owners[i]=job+1;return true;}}
 public void ReleasePreview(int i,int job){lock(Locks[i]){if(Owners[i]==job+1)Owners[i]=0;}}
}
class PreviewHarness {
 const int maxCameraNum=2;
 public Param cup=new Param(); public FIFO acqFIFO=new FIFO();
 Control cogRDMainUser=new Control(),cogRDSub1=new Control();
 IntPtr[] _flipBuffers=new IntPtr[2]; uint[] _flipBufferSizes=new uint[2]; int[] _previewFlipTypes=new int[2];
 public List<string> Logs=new List<string>();
 Control GetPreviewControl(int i){return i==0?cogRDMainUser:cogRDSub1;}
 void AdjustPictureBoxSize(Control c,int w,int h,int i){}
 void OnPreviewStatus(int job,int cam,int status,string message){lock(Logs)Logs.Add(message);}
'''
middle = r'''
 public void Run(MyCamera camera){_previewRunning[0]=true;_previewHwnds[0]=(IntPtr)1;acqFIFO.TryReservePreview(0,0);PreviewThreadProc(camera,0,0,"DA8696070");}
 public void StopFlag(){Volatile.Write(ref _previewRunning[0],false);}
 public bool Has(string text){lock(Logs)return Logs.Exists(x=>x.Contains(text));}
 public void TimeoutTest(){
   var gate=new ManualResetEvent(false);var entered=new ManualResetEvent(false);
   var old=new Thread(()=>{entered.Set();gate.WaitOne();});_previewThreads[0]=old;_previewRunning[0]=true;_previewCameraIndices[0]=0;
   _flipBuffers[0]=Marshal.AllocHGlobal(4);var buffer=_flipBuffers[0];acqFIFO.TryReservePreview(0,0);old.Start();entered.WaitOne();
   Tests.Check(!previewOnlive(false,0),"join timeout returns failure");
   Tests.Check(ReferenceEquals(_previewThreads[0],old)&&_flipBuffers[0]==buffer&&acqFIFO.Owners[0]==1,"timeout preserves thread, buffer and ownership");
   Tests.Check(!previewOnlive(true,0)&&ReferenceEquals(_previewThreads[0],old),"timeout rejects second thread");
   Tests.Check(!StopPreviewForCamera(0),"reconnect gate rejects live old thread");
   gate.Set();old.Join();Tests.Check(previewOnlive(false,0)&&_previewThreads[0]==null&&_flipBuffers[0]==IntPtr.Zero,"cleanup only after old thread exit");
 }
 public void DuplicateTest(){
   cup.p_CD.camInd[1]=0;var gate=new ManualResetEvent(false);var entered=new ManualResetEvent(false);
   acqFIFO._HikCam[0].Next=()=>{entered.Set();gate.WaitOne();return MyCamera.MV_E_NODATA;};
   Tests.Check(previewOnlive(true,0),"first preview starts");entered.WaitOne();var first=_previewThreads[0];
   Tests.Check(previewOnlive(true,0)&&ReferenceEquals(first,_previewThreads[0]),"same job start is idempotent");
   Tests.Check(!previewOnlive(true,1)&&_previewThreads[1]==null,"same camera second job rejected");
   gate.Set();Tests.Check(previewOnlive(false,0),"running preview stops");
 }
}
class HikStub {
 public string Fail; public bool SetParam(float g,float e,ref string gs,ref string es,MyCamera c){return Fail!="ExposureTime";}
 public bool SetPixelFormat(ref int t,MyCamera c){t=0;return Fail!="PixelFormat";}
 public bool SetSharpness(ref uint v,MyCamera c){v=0;return Fail!="Sharpness";}
 public bool SetGamma(ref float v,MyCamera c){v=1;return Fail!="Gamma";}
 public bool SetResolution(ref uint w,ref uint h,MyCamera c){return Fail!="Resolution";}
 public bool SetIntValueChecked(MyCamera c,string n,uint v){return Fail!=n;}
 public bool SetEnumValueChecked(MyCamera c,string n,uint v){return Fail!=n;}
 public string GetConnectedCameraSerial(int i){return "DA8696070";}
}
class RestoreHarness {
 const int maxCameraNum=2;public Param cup=new Param(); bool chooseHIK=true; MyCamera[] camera2={new MyCamera(),new MyCamera()};public HikStub hIKVisionCamera=new HikStub();
 void LogCameraConnection(string op,bool ok,string text){} void WriteCameraDiagnostic(string op,int status,string text){}
 bool GetCameraRuntimeState(int i,out string state){state="test";return hIKVisionCamera.Fail!="ReadBack";}
'''
suffix = r'''
 public bool Restore(){return SetCamParam(0);}
}
class RangeHarness {
 public int Diagnostics;public bool CheckSdkResult(MyCamera c,string n,string target,int ret){if(ret!=0)Diagnostics++;return ret==0;}
 public bool SetEnumValueChecked(MyCamera c,string n,uint v){c.SetCalls++;return true;}
 static uint Clamp(uint v,uint min,uint max){return Math.Max(min,Math.Min(max,v));}
'''
tests = r'''
}
class Tests {
 public static int Count;public static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL "+name);Count++;Console.WriteLine("PASS "+name);}
 public static void Main(){
  new PreviewHarness().TimeoutTest();new PreviewHarness().DuplicateTest();
  var h=new PreviewHarness();var c=new MyCamera{DisplayThrows=true,Next=()=>0};h.Run(c);
  Check(c.FreeCalls==1&&h.Has("display-test")&&h.Has("InvalidOperationException")&&h.Has("预览线程退出"),"display exception frees frame and records full exit exception");
  h=new PreviewHarness();c=new MyCamera{FreeRet=unchecked((int)0x80000003),Next=()=>0};h.Run(c);
  Check(c.GetCalls==1&&c.FreeCalls==1&&h.Has("FreeImageBuffer失败"),"free SDK failure exits without retry");
  h=new PreviewHarness();c=new MyCamera{FreeThrows=true,Next=()=>0};h.Run(c);
  Check(c.FreeCalls==1&&h.Has("free-test")&&h.Has("InvalidOperationException"),"free exception retained in exit log");
  h=new PreviewHarness();c=new MyCamera();c.Next=()=>c.GetCalls==1?0:MyCamera.MV_E_NODATA;c.OnGet=()=>{if(c.GetCalls==3)h.StopFlag();};h.Run(c);
  Check(c.FreeCalls==1&&h.Has("successfulFrames=1")&&h.Has("subsequentFrames=0")&&h.Has("consecutiveNoData=2")&&h.Has("线程仍在运行"),"first-frame freeze distinguishes no data from thread exit");
  h=new PreviewHarness();c=new MyCamera{Next=()=>unchecked((int)0x80000003)};h.Run(c);
  Check(c.GetCalls==1&&h.Has("未重试"),"non-no-data SDK error exits once");
  h=new PreviewHarness();c=new MyCamera{Connected=false};h.Run(c);Check(h.Has("设备已断开"),"disconnect has distinct exit reason");
  foreach(var node in new[]{"PixelFormat","Sharpness","Gamma","Resolution","Width","Height","OffsetX","OffsetY","ReadBack"}){
    var r=new RestoreHarness();r.hIKVisionCamera.Fail=node;var before=r.cup.p_Cam[0];var roi=before.iROI;
    Check(!r.Restore()&&r.cup.p_Cam[0].iMonoType==before.iMonoType&&r.cup.p_Cam[0].iSharp==before.iSharp&&r.cup.p_Cam[0].dGamma==before.dGamma&&ReferenceEquals(r.cup.p_Cam[0].iROI,roi)&&roi[2]==3072,"failed restore leaves saved parameters intact: "+node);
  }
  var range=new RangeHarness();c=new MyCamera{RangeRet=unchecked((int)0x80000106)};uint value=7;
  Check(!range.SetSharpness(ref value,c)&&value==7&&c.SetCalls==0&&range.Diagnostics==1,"failed range read causes no write or saved-value mutation");
  Console.WriteLine("TOTAL "+Count+" PASS (fake SDK only, no hardware)");
 }
}
'''
out=root/'tmp/session-validation/hot-camera/BehaviorHarness.cs'
out.write_text(prefix+fields+preview+thread+middle+restore+suffix+sharpness+tests,encoding='utf-8-sig')
print(out)
