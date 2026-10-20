from pathlib import Path
import re
task = Path(__file__).resolve().parent
root = task.parents[1]
main = (root/'VisionApplication/FrmMainUI.cs').read_text(encoding='utf-8-sig')
ext = (root/'VisionApplication/FrmMainUIExtention.cs').read_text(encoding='utf-8-sig')
def method(src, sig):
    start = src.index(sig)
    p = src.index('{', start)
    depth, end = 1, p+1
    while depth:
        depth += (src[end]=='{') - (src[end]=='}')
        end += 1
    return src[start:end]

s = r'''
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;
using CommonCls;
namespace VisionApplication.Properties {
 static class Resources {public static System.Drawing.Image LightOn=null,LightOff=null;}
}
// Only the transport and SDK are simulated. Methods under test are extracted verbatim.
class SerialPort : IDisposable {
 public static HashSet<string> Denied = new HashSet<string>();
 public string PortName="COM1"; public int BaudRate,DataBits,WriteTimeout;
 public Parity Parity; public StopBits StopBits;
 public bool IsOpen,FailWrite; public int Opens,Closes,Disposals;
 public List<string> Writes=new List<string>();
 public event SerialDataReceivedEventHandler DataReceived;
 public void Open(){Opens++;if(Denied.Contains(PortName))throw new Exception("open failed");IsOpen=true;}
 public void Close(){Closes++;IsOpen=false;}
 public void Dispose(){Disposals++;}
 public void Write(string text){if(!IsOpen||FailWrite)throw new Exception("write failed");Writes.Add(text);}
}
class CSerialDef {public SerialPort _sPort,_sPortCy;public SerialPort[] _sPortSub;}
class MyCamera {public const int MV_OK=0;}
class Init {public bool bCamInit,bCamPrepared,bSerInit;}
class CS {public int[] iStatus=new int[2];}
class RTS {public Init initStatus=new Init();public CS cs=new CS();}
class Harness : Form {
 const int maxJobNum=2, lightControllerSlotCount=3, lightChannelCount=4;
 CUserParam cup=new CUserParam(); RTS rts=new RTS();
 List<string> items=new List<string>{"U3V A serial-A","U3V B serial-B"};uint cameraNum=2;
 List<int> cameraListPhysicalIndices=new List<int>(),cameraListJobIndices=new List<int>();
 ComboBox cbDeviceList=new ComboBox(),cmbLs_Channel=new ComboBox();
 TrackBar tbBirghtness=new TrackBar{Minimum=0,Maximum=255};
 NumericUpDown nudContrast=new NumericUpDown{Minimum=0,Maximum=255};
 CheckBox chkLS_Sync=new CheckBox();ToolStripButton toolBtSta_Triggle=new ToolStripButton();
 ToolStripStatusLabel toolStripStatus_Ser=new ToolStripStatusLabel();
 bool[,] lightOnStates=new bool[3,4];
 SerialPort[] sPortAll=new SerialPort[4],sPortSub=new SerialPort[2];
 SerialPort sPortMain,sPortCy;CSerialDef csd=new CSerialDef();
 OperLog operLog;List<string> logs=new List<string>();
 int icamSelectInd=-1;bool _isUpdatingLightUI,_isUpdatingUI;
 object[] camera2;List<int> starts=new List<int>();int[] sdkResults=new int[2];
 int[] sdkStatuses=new[]{3,3};
 readonly object _cameraConnectionSync=new object();bool _cameraConnectionInProgress,_cameraManuallyDisconnected;
 class Acq {public object[] _HikCam;}Acq acqFIFO=new Acq();
 class Hik {public object[] GetCameraArray(){return new object[2];}}Hik hIKVisionCamera=new Hik();
 int ConnectCalls;bool MappingValid=true;
 bool RefreshCameraDevices(string operation,out List<string> latest){latest=items;return true;}
 bool TryMapJobsByCameraSerial(List<string> latest,out bool changed){changed=false;return MappingValid;}
 bool ConnectJobCamera(int job,List<string> latest,string operation,bool mapping){ConnectCalls++;return true;}
 void SaveUserParam(){}
 void AddLog(){logs.Add(operLog.strFailReason??"");}
 void LogCameraConnection(string op,bool success,string detail){logs.Add(detail);}
 void LogLightControllerResolution(int job){}
 void UpdateCamParamToUI(){RefreshSelectedCameraLightUI();}
 void PortGetCylinder(object sender,SerialDataReceivedEventArgs e){}
 void UpdateSerialStatusIndicator(int slot,SerialStatus status){}
 enum SerialStatus {Disabled,Success,Failed}
 bool StartGrab(int cameraIndex){starts.Add(cameraIndex);rts.cs.iStatus[cameraIndex]=sdkStatuses[cameraIndex];return sdkResults[cameraIndex]==0;}
 Harness(){
  cup.p_CD.bListProcess[0]=cup.p_CD.bListProcess[1]=true;
  cup.p_CD.camInd[0]=1;cup.p_CD.camInd[1]=0;
  cup.p_LS.indent=new[]{0,10,20,30,40};
  cup.p_LS2[0].indent=new[]{0,50,60,70,80};
  cmbLs_Channel.Items.AddRange(new object[]{"CH1","CH2","CH3","CH4"});cmbLs_Channel.SelectedIndex=0;
  cbDeviceList.SelectedIndexChanged+=(a,b)=>{
    int job;if(!TryGetSelectedCameraBinding(out icamSelectInd,out job))icamSelectInd=-1;
    UpdateCamParamToUI();
  };
 }
'''
for sig in ['private void RefreshCameraSelectionItems()', 'private bool TryConnectCameras(',
            'private bool StartGrab()', 'private bool OpenBoundSerialPort(',
            'private bool OpenSerialPort(', 'private void RegisterIOReceive()',
            'public bool disconnect()', 'private void LightControlTurnOn()']:
    s += method(main,sig)+'\n'
for sig in ['private bool TryGetSelectedCameraBinding(', 'private int ResolveLightControllerSlot(',
            'private int GetSharedLightControllerSlot(', 'private SerialPort GetLightPortForJob(',
            'private bool IsLightSerialConfigured(', 'private int[] GetLightValuesStorage(',
            'private bool TryGetLightValuesForController(', 'private bool TryGetLightValues(',
            'private bool SaveLightValues(', 'private void SetLightButtonState(',
            'private void RefreshSelectedCameraLightUI()', 'private void GetLightOutputValues(',
            'private bool SendAllChannelsCommand(', 'private bool ApplyLightBrightness(',
            'private bool ApplySelectedCameraLightBrightness(', 'private void InitializeLightBrightness()']:
    s += method(ext,sig)+'\n'
s += r'''
 static int checks;
 static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static void Bind(CUserParam p,int slot,string name,string id=""){
  p.p_SD.bUsed[slot]=true;p.p_SD.portNames[slot]=name;p.p_SD.deviceIds[slot]=id;
 }
 [STAThread] static void Main(){
  var devices=new List<SerialDeviceInfo>{new SerialDeviceInfo{PortName="COM8",DeviceId="A"},new SerialDeviceInfo{PortName="COM12",DeviceId="IO"}};
  var p=new CUserParam();SerialDeviceInfo d;string error;
  Bind(p,0,"COM3","A");Bind(p,1,"COM8");Bind(p,2,"COM8","A");Bind(p,3,"COM12","IO");
  for(int slot=0;slot<4;slot++)Check(SerialPortBinding.TryResolve(p.p_SD,slot,devices,out d,out error),"resolve shared lights / independent IO slot "+slot);
  p.p_SD.deviceIds[3]="A";
  for(int slot=0;slot<4;slot++)Check(!SerialPortBinding.TryResolve(p.p_SD,slot,devices,out d,out error)&&error.Contains("IO"),"reject IO conflict from slot "+slot);
  p.p_SD.bUsed[3]=false;
  Check(SerialPortBinding.TryResolve(p.p_SD,0,devices,out d,out error)&&d.PortName=="COM8","identity follows changed COM");
  p.p_SD.deviceIds[0]="missing";
  Check(!SerialPortBinding.TryResolve(p.p_SD,0,devices,out d,out error),"missing identity never falls back to name");
  p.p_SD.deviceIds[0]="A";
  var ambiguous=new List<SerialDeviceInfo>(devices){new SerialDeviceInfo{PortName="COM9",DeviceId="A"}};
  Check(!SerialPortBinding.TryResolve(p.p_SD,0,ambiguous,out d,out error),"ambiguous identity rejected");
  p.p_SD.portNames[0]="";p.p_SD.deviceIds[0]="";p.p_SD.serInd[0]=0;
  Check(!SerialPortBinding.TryResolve(p.p_SD,0,devices,out d,out error),"legacy index cannot bind a port");
  using(var h=new Harness()){
   var handle=h.Handle;
   Check(!h.cup.p_RTSP.bAutoConnCam,"manual connection test uses new default");
   Check(h.TryConnectCameras("手动连接")&&h.ConnectCalls==2&&h.cbDeviceList.Items.Count==2,"manual connection populates camera list without InitCam");
   int cam,job;Check(h.TryGetSelectedCameraBinding(out cam,out job)&&cam==1&&job==0,"list maps swapped physical camera to Job0");
   Check(!h.tbBirghtness.Enabled&&!h.toolBtSta_Triggle.Enabled,"no serial disables light controls");
   h.cbDeviceList.SelectedIndex=1;h.RefreshCameraSelectionItems();
   Check(h.cbDeviceList.SelectedIndex==1&&h.icamSelectInd==0,"refresh preserves selected Job1");
   h.cup.p_CD.camInd[1]=1;h.RefreshCameraSelectionItems();
   Check(h.cbDeviceList.SelectedIndex==1&&h.icamSelectInd==1,"enumeration reorder preserves selected job");
   h.cup.p_CD.camInd[1]=0;
   Bind(h.cup,0,"COM8","A");Bind(h.cup,1,"COM8","A");Bind(h.cup,3,"COM12","IO");
   Check(h.OpenBoundSerialPort(1,devices[0],devices),"shared COM can be opened starting from B slot");
   var port=h.sPortAll[0];
   Check(Object.ReferenceEquals(port,h.sPortAll[1])&&port.Opens==1,"both slots share one physical open");
   Check(h.OpenBoundSerialPort(0,devices[0],devices)&&port.Opens==1&&port.Closes==0,"connecting alias never closes or reopens owner");
   h.RefreshCameraSelectionItems();h.RefreshSelectedCameraLightUI();
   Check(h.tbBirghtness.Enabled&&h.nudContrast.Enabled&&h.cmbLs_Channel.Enabled&&h.toolBtSta_Triggle.Enabled,"connected serial and selected camera enable all light controls");
   Check(port.Writes.Count==0,"list and light UI refresh issue zero serial commands");
   string reason;Check(h.ResolveLightControllerSlot(0,out reason)==0&&h.ResolveLightControllerSlot(1,out reason)==0,"two faces share controller channel state");
   h.InitializeLightBrightness();
   Check(port.Writes.Count==1&&port.Writes[0]=="S010T020T030T040TC#","shared controller initialized once using first enabled slot");
   Check(h.cup.p_LS2[0].indent.Skip(1).SequenceEqual(h.cup.p_LS.indent.Skip(1)),"successful initialization synchronizes alias settings");
   h.cmbLs_Channel.SelectedIndex=1;
   h.ApplySelectedCameraLightBrightness(null,99);
   Check(port.Writes.Last()=="S010T099T030T040TC#","B changes CH2 while preserving other physical channels");
   Check(h.cup.p_LS.indent[2]==99&&h.cup.p_LS2[0].indent[2]==99,"shared brightness persists in both slot settings");
   h.cbDeviceList.SelectedIndex=0;h.cmbLs_Channel.SelectedIndex=0;h.LightControlTurnOn();
   Check(port.Writes.Last()=="S000T099T030T040TC#","A turns off CH1 preserving B CH2");
   h.cbDeviceList.SelectedIndex=1;h.LightControlTurnOn();
   Check(port.Writes.Last()=="S010T099T030T040TC#","B sees same CH1 switch state and turns it back on");
   int count=port.Writes.Count;h.RefreshCameraSelectionItems();h.RefreshSelectedCameraLightUI();
   Check(port.Writes.Count==count,"refresh never flips light output");
   port.FailWrite=true;h.LightControlTurnOn();
   Check(h.lightOnStates[0,0],"failed switch write retains successful state");
   Check(!h.ApplySelectedCameraLightBrightness(2,123)&&h.cup.p_LS.indent[2]==99&&h.cup.p_LS2[0].indent[2]==99,"failed brightness write never saves new values");
   port.FailWrite=false;h.chkLS_Sync.Checked=true;h.LightControlTurnOn();
   Check(port.Writes.Last()=="S000T000T000T000TC#","light sync switches all physical channels");
   Check(h.OpenBoundSerialPort(3,devices[1],devices)&&!Object.ReferenceEquals(h.sPortAll[3],port),"IO has distinct physical object");
   var io=h.sPortAll[3];Check(h.disconnect(),"disconnect shared serials succeeds");
   Check(port.Closes==1&&port.Disposals==1&&io.Closes==1&&io.Disposals==1&&h.sPortAll.All(x=>x==null),"disconnect disposes each physical port once and clears aliases");
   Check(!h.toolBtSta_Triggle.Enabled&&!h.tbBirghtness.Enabled,"disconnect disables light UI");
   Check(h.OpenBoundSerialPort(1,devices[0],devices)&&!Object.ReferenceEquals(port,h.sPortAll[1])&&Object.ReferenceEquals(h.sPortAll[0],h.sPortAll[1]),"reconnect creates a fresh shared owner");
   h.RefreshSelectedCameraLightUI();Check(h.toolBtSta_Triggle.Enabled,"reconnect refresh restores light controls");
   Check(h.sPortAll[0].Writes.Count==0,"reconnect transport and UI do not send light commands");
   h.cup.p_CD.bListProcess[0]=h.cup.p_CD.bListProcess[1]=false;h.RefreshCameraSelectionItems();
   Check(h.cbDeviceList.Items.Count==0&&!h.tbBirghtness.Enabled,"no enabled jobs clears list and disables light");
  }
  using(var h=new Harness()){
   Bind(h.cup,0,"COM8","A");Bind(h.cup,1,"COM12","IO");
   h.OpenBoundSerialPort(0,devices[0],devices);h.OpenBoundSerialPort(1,devices[1],devices);
   string reason;Check(!Object.ReferenceEquals(h.sPortAll[0],h.sPortAll[1])&&h.ResolveLightControllerSlot(1,out reason)==1,"distinct light controllers retain independent objects and state");
   h.RefreshCameraSelectionItems();h.InitializeLightBrightness();
   Check(h.sPortAll[0].Writes.Single()=="S010T020T030T040TC#"&&h.sPortAll[1].Writes.Single()=="S050T060T070T080TC#","distinct controllers retain own channel values");
  }
  using(var h=new Harness()){
   Bind(h.cup,1,"COM8","A");Bind(h.cup,2,"COM8","A");
   h.OpenBoundSerialPort(2,devices[0],devices);string reason;
   Check(h.ResolveLightControllerSlot(0,out reason)==1&&h.ResolveLightControllerSlot(1,out reason)==1,"two alias slots count as one fallback controller");
  }
  using(var h=new Harness()){
   Bind(h.cup,1,"COM8","A");
   SerialPort.Denied.Add("COM8");Check(!h.OpenBoundSerialPort(1,devices[0],devices),"open failure returned");
   h.RefreshCameraSelectionItems();Check(!h.tbBirghtness.Enabled&&!h.toolBtSta_Triggle.Enabled,"failed open never enables light");
   SerialPort.Denied.Clear();
  }
  using(var h=new Harness()){
   h.cup.p_CD.bListProcess[0]=h.cup.p_CD.bListProcess[1]=false;
   Check(!h.StartGrab()&&h.starts.Count==0,"enumerated cameras without enabled jobs never start");
   h.cup.p_CD.bListProcess[0]=true;
   Check(!h.StartGrab()&&h.starts.Count==0,"null camera array never reaches SDK");
   h.camera2=new object[0];Check(!h.StartGrab()&&h.starts.Count==0,"empty camera array never reaches SDK");
   h.camera2=new object[2];Check(!h.StartGrab()&&h.starts.Count==0,"missing camera object never reaches SDK");
   h.camera2[1]=new object();h.rts.cs.iStatus[1]=1;
   Check(!h.StartGrab()&&h.starts.Count==0,"unprepared buffer never reaches SDK");
   h.rts.cs.iStatus[1]=2;Check(h.StartGrab()&&h.starts.SequenceEqual(new[]{1}),"only enabled mapped ready camera starts");
   h.starts.Clear();h.cup.p_CD.bListProcess[1]=true;h.cup.p_CD.camInd[1]=-1;
   Check(!h.StartGrab()&&h.starts.SequenceEqual(new[]{1})&&h.rts.cs.iStatus[1]==3,"invalid second job does not stop healthy camera");
   h.starts.Clear();h.cup.p_CD.camInd[1]=0;h.camera2[0]=new object();h.rts.cs.iStatus[0]=2;h.sdkResults[0]=-1;h.sdkStatuses[0]=1;
   Check(!h.StartGrab()&&h.starts.SequenceEqual(new[]{1,0})&&h.rts.cs.iStatus[1]==3,"one start failure preserves other camera");
   h.starts.Clear();h.cup.p_CD.camInd[0]=99;h.cup.p_CD.bListProcess[1]=false;
   Check(!h.StartGrab()&&h.starts.Count==0,"out of range physical index skipped");
   h.cup.p_CD.camInd[0]=0;h.rts.cs.iStatus=null;
   Check(!h.StartGrab()&&h.starts.Count==0,"missing status array skipped");
  }
  Console.WriteLine("TOTAL "+checks+" passed");
 }
}
'''
(task/'ControlChecks.cs').write_text(s,encoding='utf-8-sig')
# Verify the integration call sites that the extracted-method harness cannot execute.
startup=method(main,'private void FrmMainUI_Load(') if 'private void FrmMainUI_Load(' in main else main
assert 'if (cup.p_RTSP.bAutoConnCam) StartGrab();' in startup
assert 'items = hIKVisionCamera.EnumDevices();' in method(main,'private void SearchCam()')
assert 'RefreshCameraSelectionItems();' in method(main,'private void SearchCam()')
assert 'RefreshCameraSelectionItems();' in method(main,'private bool TryConnectCameras(')
assert 'RefreshCameraSelectionItems();' in method(main,'private void menu_RuntimeSet_Click(') if 'private void menu_RuntimeSet_Click(' in main else 'ApplyJobSelection(iJobIndex);\n                RefreshCameraSelectionItems();' in main
print('Generated actual-method harness; integration call site checks passed')
