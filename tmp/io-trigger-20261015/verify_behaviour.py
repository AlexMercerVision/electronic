from pathlib import Path
import subprocess

task = Path(__file__).resolve().parent
root = task.parents[1]
main = (root / 'VisionApplication/FrmMainUI.cs').read_text(encoding='utf-8-sig')
laser = (root / 'VisionApplication/FrmMain_Laser.cs').read_text(encoding='utf-8-sig')
serial = (root / 'CommonCls/CSerialDef.cs').read_text(encoding='utf-8-sig')


def method(source, signature):
    start = source.index(signature)
    position = source.index('{', start)
    depth = 1
    end = position + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]


source = r'''
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
class SerialDataReceivedEventArgs : EventArgs { }
delegate void SerialDataReceivedEventHandler(object sender, SerialDataReceivedEventArgs args);
class SerialPort {
 public bool IsOpen; public string PortName="COM4";
 public List<byte[]> Writes=new List<byte[]>(); public Queue<byte> Pending=new Queue<byte>();
 public int MaxRead=int.MaxValue; public bool FailWrite;
 private SerialDataReceivedEventHandler received;
 public int HandlerCount {get {return received==null?0:received.GetInvocationList().Length;}}
 public event SerialDataReceivedEventHandler DataReceived {add {received+=value;} remove {received-=value;}}
 public int BytesToRead {get {return Pending.Count;}}
 public int Read(byte[] target,int offset,int count) {int actual=Math.Min(Math.Min(count,MaxRead),Pending.Count);for(int index=0;index<actual;index++)target[offset+index]=Pending.Dequeue();return actual;}
 public void Write(byte[] frame,int offset,int count) {if(FailWrite)throw new InvalidOperationException("write failure");lock(Writes)Writes.Add(frame.Skip(offset).Take(count).ToArray());}
 public void Report(byte[] frame) {foreach(byte value in frame)Pending.Enqueue(value);if(received!=null)received(this,new SerialDataReceivedEventArgs());}
}
class CSerialDef {
 public SerialPort _sPortCy,_sPort; public SerialPort[] _sPortSub;
 private readonly object ioOutputWriteSync=new object();
'''
for signature in ['private void WriteIOFrame(', 'public bool SendIOOutputBitmapPulse(', 'private byte[] BuildWriteEightOutputsFrame(', 'private void CalculateCRC(']:
    source += method(serial, signature) + '\n'
source += r'''
}
class Param {
 public SD p_SD=new SD(); public IO p_IOPP=new IO(); public CD p_CD=new CD(); public RT p_RTSP=new RT();public RTP p_VPPRTP=new RTP();
}
class SD {public bool[] bUsed={false,false,false,true};public int[] serInd={0,1,0,0};}
class IO {public int APlane=1,BPlane=2,EndAPlane,EndBPlane,IOTimeDelay=1;public int[] JobOkOut={1,3},JobNGOut={2,4};}
class CD {public bool[] bListProcess={true,true};}
class RT {public bool bUpdateIO=true;public int runTimeOut=10;}
class RTP {public bool bVisionInfer,bAcqFromCam=true;}
class Init {public bool bCamInit=true,bCamPrepared=true,bLocalFileExist=true,bSerInit;public int iVPPStatus;}
class Online {public bool bTrigg=true,bLock;}
class State {public int iModelRole=9,iIsRunning;public Init initStatus=new Init();public Online onLS=new Online();}
class Log {public int iEventStatus;public string strOperEvent,strFailReason;}
struct Result {public bool bOk,bLocated;}
enum FlatStyle {Popup}
class Control {
 public string Text;public bool Enabled;public FlatStyle FlatStyle;
 public bool InvokeRequired {get{return false;}}public void Invoke(Action action){action();}
}
class VP {
 public bool bMainTaskFinished=true;public int _iTaskType;public int[] _bRunOneTimeFinished=new int[2];
 public Result[] _PMARes=new Result[2];public string[] strErrInfo={"failure","failure"};
 public bool[] Busy=new bool[2],Success={true,true},Validity={true,true},TimeOut=new bool[2];public int[] Runs=new int[2];public bool ThrowClear;
 public void ClearRDRecord(int index){if(ThrowClear)throw new InvalidOperationException("display failure");}
 public bool RunOneTimeSplite(int index){return RunOneTime(index);}
 public bool RunOneTime(int index){Runs[index]++;if(!Success[index])return false;if(TimeOut[index])return true;_bRunOneTimeFinished[index]=2;_PMARes[index].bLocated=true;return true;}
 public bool IsVppJobResultValid(int index){return Validity[index];}
 public void SetJobBusy(int index,bool busy){Busy[index]=busy;}
}
class Harness {
 const int maxCameraNum=2,ioFeedbackJobCount=2;
 Param cup=new Param(); State rts=new State();Log operLog=new Log(); VP vpWin=new VP();CSerialDef csd=new CSerialDef();
 SerialPort sPortCy=new SerialPort {IsOpen=true},sPortMain;SerialPort[] sPortAll=new SerialPort[4],sPortSub=new SerialPort[2];
 object ioFeedbackSync=new object(),_autoTriggerSync=new object(),_lockBuffer=new object();
 bool[] ioFeedbackResult=new bool[2],ioFeedbackReady=new bool[2],ioFeedbackStarted=new bool[2],ioFeedbackParticipating=new bool[2];
 long[] ioFeedbackSingleJobCycles=new long[2];bool[] ioFeedbackSingleJobOutputInProgress=new bool[2];
 bool ioFeedbackCycleActive,ioFeedbackOutputInProgress,allJobsDone;long ioFeedbackCycleId;int completedJobCount,manualSingleRunActive;
 bool[] _autoTriggerVisionReservedByJob=new bool[2],_visionRunningByJob=new bool[2],_ioTriggerActiveByJob=new bool[2];
 Result[] pmaRes=new Result[2];int iJobIndex;string strOrder="";Control btFunRunOneTime=new Control(),toolStripStatus_Ser=new Control(),toolStripStatus_Hand=new Control();
 List<byte> _rxBuffer=new List<byte>();byte lastIOInputState;
 System.Timers.Timer timerIOIn;
 List<int> Triggers=new List<int>();bool DispatchTriggers,OpenSuccess=true;
 bool[] serialConfigErrorLogged=new bool[4];List<string> Logs=new List<string>();
 enum SerialStatus {Disabled,Success,Failed}
 bool InvokeRequired {get{return false;}} void Invoke(Action action){action();}
 void AddLog(){lock(Logs)Logs.Add(operLog.strOperEvent);}
 void WriteIOFeedbackLog(int status,string name,string detail){lock(Logs)Logs.Add(name);}
 void SetFaceTriggerIndicator(int index,bool active){}
 bool IsFaceLaserBusy(int index){return false;}
 bool IsLivePreviewRunning(){return false;}
 bool StopLivePreviewForTrigger(string operation){return true;}
 void ShowCurrentTaskNotFinishedWarning(){}
 Task LaserMarkAfterVisionDetAsync(int index){return Task.FromResult(0);}
 void PreviewImage(int mode){}void StopRun(){}
 void UpdateUIExeForJob(int index){}
 void ExeIOTrig(int type){Triggers.Add(type);if(DispatchTriggers)RealExeIOTrig(type);}
 void ResetIOFeedbackCycle(string reason){lock(ioFeedbackSync){ResetIOFeedbackCycleNoLock();Array.Clear(ioFeedbackSingleJobCycles,0,2);Array.Clear(ioFeedbackSingleJobOutputInProgress,0,2);}}
 List<string> GetSortedPorts(){return new List<string>{"COM4","COM5"};}
 void LogInvalidSerialConfigurationOnce(int slot,int configIndex,int count){}
 bool OpenSerialPort(SerialPort port,string name){if(port==null)return false;port.PortName=name;port.IsOpen=OpenSuccess;return port.IsOpen;}
 void UpdateSerialStatusIndicator(int slot,SerialStatus status){}
 void UpdateSerialUIStatus(){}void InitializeLightBrightness(){}
'''
for signature in [
    'private bool TryBeginIOFeedbackRun(', 'private void SubmitIOFeedbackResult(',
    'private bool TryBuildIOOutputBitmap(', 'private bool ValidateIOOutputMapping(',
    'private bool CanAcceptIOFeedbackRun(', 'private void AbortIOFeedbackCycle(',
    'private void ResetIOFeedbackCycleNoLock(', 'private void ExeIOOut(',
    'private void ProcessSerialBuffer(', 'private void ProcessActiveReport(',
    'private void ProcessSetOutputResponse(', 'private void ProcessQueryResponse(',
    'private bool InitSerialIndex(', 'private void RegisterIOReceive(',
    'private void InitSerial(', 'private bool TryGetPortNameByConfigIndex('
]:
    source += method(main, signature) + '\n'
source += method(main[main.index('private readonly List<byte> _rxBuffer'):], 'private void PortGetCylinder(') + '\n'
source += method(main, 'private void ExeIOTrig(').replace('private void ExeIOTrig(', 'private void RealExeIOTrig(', 1).replace('MessageBox.Show("用户身份未验证，无法操作");', '') + '\n'
source += method(main, 'private void RunOneTimeThread(bool syncMode)') + '\n'
for signature in [
    'private bool IsFaceVisionBusy(', 'private bool AnyFaceVisionActive(',
    'private void UpdateMainVisionTaskFinishedFlag(', 'private bool CanRunFaceAutoTrigger(',
    'private bool TryReserveAutoTriggerRun(', 'private void ReleaseAutoTriggerReservation(',
    'private void EnterFaceVisionRun(', 'private void FinishFaceVisionRun(',
    'private async Task RunOneTimeForFace('
]:
    source += method(laser, signature) + '\n'
source += r'''
 static int checks;
 static void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static byte[] Report10(byte value){return new byte[]{1,0x45,5,1,value,0,0,0,0,0};}
 static byte[] Report25(byte value){var frame=new byte[25];frame[0]=1;frame[1]=0x45;frame[2]=1;frame[4]=1;frame[5]=value;return frame;}
 void Feed(byte[] frame){_rxBuffer.AddRange(frame);ProcessSerialBuffer();}
 long Begin(int job,bool single){long token;string error;if(!TryBeginIOFeedbackRun(job,single,out token,out error))throw new Exception(error);return token;}
 int[] Bitmaps(){lock(sPortCy.Writes)return sPortCy.Writes.Select(frame=>(int)frame[7]).ToArray();}
 static void WaitFor(Func<bool> condition){var until=DateTime.UtcNow.AddSeconds(4);while(!condition()){if(DateTime.UtcNow>until)throw new Exception("async timeout");Thread.Sleep(5);}}
 static void ParserChecks(){
  var sample=new Harness();sample.Feed(Report10(1));Check(sample.Triggers.SequenceEqual(new[]{1}),"10-byte DI reads offset 4");
  sample.Feed(Report10(1));Check(sample.Triggers.Count==1,"held high does not retrigger");
  sample.Feed(Report10(0));sample.Feed(Report10(1));Check(sample.Triggers.Count==2,"low then high rearms");
  sample.Feed(Report10(3));Check(sample.Triggers.SequenceEqual(new[]{1,1,2}),"B rising while A remains high only triggers B");
  sample=new Harness();sample.Feed(Report25(2));Check(sample.Triggers.SequenceEqual(new[]{2}),"25-byte DI reads offset 5");
  sample=new Harness();var frame=Report10(3);sample.Feed(frame.Take(7).ToArray());Check(sample.Triggers.Count==0,"split report waits for complete packet");sample.Feed(frame.Skip(7).ToArray());Check(sample.Triggers.SequenceEqual(new[]{1,2}),"split report preserves both input bits");
  sample=new Harness();sample.Feed(Report10(1).Concat(Report25(0)).Concat(Report25(2)).ToArray());Check(sample.Triggers.SequenceEqual(new[]{1,2})&&sample._rxBuffer.Count==0,"mixed coalesced frame lengths remain local");
  sample=new Harness();sample.rts.onLS.bTrigg=false;sample.Feed(Report10(1));sample.rts.onLS.bTrigg=true;sample.Feed(Report10(1));Check(sample.Triggers.Count==0,"old high input does not trigger on automatic enable");sample.Feed(Report10(0));sample.Feed(Report10(1));Check(sample.Triggers.Count==1,"fresh pulse triggers after automatic enable");
  sample=new Harness();sample.ProcessActiveReport(Report10(1),25,5);Check(sample.Triggers.Count==0,"mismatched packet metadata rejected");
 }
 static void RegistrationChecks(){
  var sample=new Harness();sample.OpenSuccess=false;sample.InitSerial();Check(sample.sPortCy.HandlerCount==0,"initial open failure has no receive registration");
  sample.OpenSuccess=true;Check(sample.InitSerialIndex(3)&&sample.sPortCy.HandlerCount==1&&ReferenceEquals(sample.sPortCy,sample.csd._sPortCy),"IO reconnect registers and shares exact serial object");
  sample.InitSerialIndex(3);Check(sample.sPortCy.HandlerCount==1,"repeated reconnect keeps one subscription");
  sample.sPortCy.Report(Report10(1));Check(sample.Triggers.Count==1,"reconnected serial reaches real parser");
  var previous=sample.sPortCy;sample.sPortAll[3]=new SerialPort{IsOpen=true};sample.RegisterIOReceive();Check(previous.HandlerCount==0&&sample.sPortCy.HandlerCount==1,"replacement detaches old serial callback");
  sample.cup.p_SD.bUsed[3]=false;sample.RegisterIOReceive();Check(sample.sPortCy.HandlerCount==0&&!sample.InitSerialIndex(3),"disabled IO never registers or reconnects");
  sample=new Harness();sample.InitSerial();sample.sPortCy.MaxRead=4;sample.sPortCy.Report(Report10(1));sample.PortGetCylinder(sample.sPortCy,new SerialDataReceivedEventArgs());sample.PortGetCylinder(sample.sPortCy,new SerialDataReceivedEventArgs());Check(sample.Triggers.Count==1&&sample._rxBuffer.Count==0,"partial SerialPort.Read appends only actual bytes");
 }
 static void FeedbackChecks(){
  string error;long rejected;
  var sample=new Harness();long token=sample.Begin(0,true);sample.SubmitIOFeedbackResult(0,true,token);Check(sample.Bitmaps().SequenceEqual(new[]{1,0})&&!sample.ioFeedbackCycleActive,"A alone emits Y1 and clear without B");
  token=sample.Begin(0,true);sample.SubmitIOFeedbackResult(0,false,token);Check(sample.Bitmaps().SequenceEqual(new[]{1,0,2,0}),"A can start next cycle and emit NG Y2");
  sample=new Harness();token=sample.Begin(1,true);sample.SubmitIOFeedbackResult(1,true,token);Check(sample.Bitmaps().SequenceEqual(new[]{4,0}),"B alone emits Y3 and clear without A");
  sample=new Harness();long first=sample.Begin(0,true),second=sample.Begin(1,true);Check(!sample.TryBeginIOFeedbackRun(0,true,out rejected,out error),"same face duplicate cycle rejected");sample.SubmitIOFeedbackResult(0,true,first);sample.SubmitIOFeedbackResult(1,false,second);Check(sample.Bitmaps().SequenceEqual(new[]{1,0,8,0}),"independent simultaneous faces do not block each other");
  sample=new Harness();first=sample.Begin(0,false);sample.SubmitIOFeedbackResult(0,true,first);Check(sample.Bitmaps().Length==0,"dual-face cycle waits for second result");second=sample.Begin(1,false);sample.SubmitIOFeedbackResult(1,false,second);Check(first==second&&sample.Bitmaps().SequenceEqual(new[]{9,0}),"dual-face cycle combines only its two results");
  sample=new Harness();sample.cup.p_CD.bListProcess[1]=false;token=sample.Begin(0,false);sample.SubmitIOFeedbackResult(0,true,token);Check(sample.Bitmaps().SequenceEqual(new[]{1,0}),"sync run with one enabled face only waits for that face");
  sample=new Harness();first=sample.Begin(0,true);second=sample.Begin(1,true);sample.AbortIOFeedbackCycle(0,first,"failure");sample.SubmitIOFeedbackResult(1,true,second);Check(sample.Bitmaps().SequenceEqual(new[]{4,0})&&sample.ioFeedbackSingleJobCycles[0]==0,"A failure does not cancel B independent output");
  sample=new Harness();token=sample.Begin(0,true);sample.AbortIOFeedbackCycle(0,token,"failure");first=sample.Begin(0,true);sample.AbortIOFeedbackCycle(0,token,"late");sample.SubmitIOFeedbackResult(0,false,token);Check(sample.ioFeedbackSingleJobCycles[0]==first&&sample.Bitmaps().Length==0,"stale result and cancel cannot affect next cycle");sample.SubmitIOFeedbackResult(0,true,first);
  sample=new Harness();token=sample.Begin(0,true);sample.sPortCy.FailWrite=true;sample.SubmitIOFeedbackResult(0,true,token);Check(sample.ioFeedbackSingleJobCycles[0]==0&&!sample.ioFeedbackSingleJobOutputInProgress[0],"send failure releases single-face batch");sample.sPortCy.FailWrite=false;token=sample.Begin(0,true);sample.SubmitIOFeedbackResult(0,true,token);Check(sample.Bitmaps().SequenceEqual(new[]{1,0}),"single-face retries after send failure");
  sample=new Harness();sample.cup.p_IOPP.JobOkOut[0]=0;token=sample.Begin(0,true);sample.SubmitIOFeedbackResult(0,true,token);Check(sample.ioFeedbackSingleJobCycles[0]==0&&sample.Bitmaps().Length==0,"invalid output mapping releases cycle without writing");
  sample=new Harness();sample.Begin(0,true);Check(!sample.TryBeginIOFeedbackRun(1,false,out rejected,out error),"combined run cannot consume independent cycle");
  sample=new Harness();sample.Begin(0,false);Check(!sample.TryBeginIOFeedbackRun(1,true,out rejected,out error),"independent run cannot consume combined cycle");
  foreach(bool firstOk in new[]{false,true})foreach(bool secondOk in new[]{false,true}){sample=new Harness();first=sample.Begin(0,false);second=sample.Begin(1,false);sample.SubmitIOFeedbackResult(0,firstOk,first);sample.SubmitIOFeedbackResult(1,secondOk,second);Check(sample.Bitmaps().SequenceEqual(new[]{(firstOk?1:2)|(secondOk?4:8),0}),"all combined OK/NG mappings "+firstOk+"/"+secondOk);}
 }
 static void RunChecks(){
  var sample=new Harness();sample.vpWin._PMARes[0].bOk=true;sample.iJobIndex=0;sample.RunOneTimeThread(false);Check(sample.Bitmaps().SequenceEqual(new[]{1,0})&&sample.vpWin.Runs.SequenceEqual(new[]{1,0}),"actual UI single-face runner emits A output without 24V");
  sample.iJobIndex=1;sample.RunOneTimeThread(false);Check(sample.Bitmaps().SequenceEqual(new[]{1,0,8,0}),"actual UI B runner emits B output independently");
  sample=new Harness();sample.vpWin._PMARes[0].bOk=true;sample.vpWin._PMARes[1].bOk=true;sample.RunOneTimeThread(true);Check(sample.Bitmaps().SequenceEqual(new[]{5,0})&&sample.vpWin.Runs.SequenceEqual(new[]{1,1}),"actual UI sync runner emits combined A+B output without 24V");
  sample.RunOneTimeThread(true);Check(sample.Bitmaps().SequenceEqual(new[]{5,0,5,0}),"actual UI sync runner rearms for next single click");
  sample=new Harness();sample.Begin(0,true);sample._autoTriggerVisionReservedByJob[0]=true;sample._ioTriggerActiveByJob[0]=true;sample.RunOneTimeForFace(0).GetAwaiter().GetResult();Check(!sample._autoTriggerVisionReservedByJob[0]&&!sample._ioTriggerActiveByJob[0]&&sample.vpWin.bMainTaskFinished,"rejected batch releases auto reservation and trigger flags");
  sample=new Harness();sample.vpWin.ThrowClear=true;sample._autoTriggerVisionReservedByJob[0]=true;sample._ioTriggerActiveByJob[0]=true;sample.RunOneTimeForFace(0).GetAwaiter().GetResult();Check(!sample._ioTriggerActiveByJob[0]&&sample.ioFeedbackSingleJobCycles[0]==0,"failure before vision entry releases reservation and IO batch");
  sample=new Harness();sample.vpWin.Success[0]=false;sample._autoTriggerVisionReservedByJob[0]=true;sample._ioTriggerActiveByJob[0]=true;sample.RunOneTimeForFace(0).GetAwaiter().GetResult();Check(!sample._visionRunningByJob[0]&&!sample._ioTriggerActiveByJob[0]&&sample.ioFeedbackSingleJobCycles[0]==0,"vision start failure rearms without output");sample.vpWin.Success[0]=true;sample.RunOneTimeForFace(0).GetAwaiter().GetResult();Check(sample.Bitmaps().SequenceEqual(new[]{2,0}),"face can run successfully after start failure");
  sample=new Harness();sample.cup.p_CD.bListProcess[0]=false;Check(!sample.TryReserveAutoTriggerRun(0,"A")&&!sample._autoTriggerVisionReservedByJob[0],"disabled face rejected before reservation");
  sample=new Harness();sample.vpWin.TimeOut[0]=true;sample.TryReserveAutoTriggerRun(0,"A");sample.RunOneTimeForFace(0).GetAwaiter().GetResult();Check(!sample.AnyFaceVisionActive()&&!sample._ioTriggerActiveByJob[0]&&sample.ioFeedbackSingleJobCycles[0]==0&&sample.Bitmaps().Length==0,"vision timeout releases run and IO state without output");sample.vpWin.TimeOut[0]=false;Check(sample.TryReserveAutoTriggerRun(0,"A"),"next trigger accepted after vision timeout");sample.RunOneTimeForFace(0).GetAwaiter().GetResult();
  sample=new Harness();sample.cup.p_VPPRTP.bVisionInfer=true;sample.vpWin.Validity[0]=false;sample.RunOneTimeForFace(0).GetAwaiter().GetResult();Check(sample.Bitmaps().Length==0&&sample.ioFeedbackSingleJobCycles[0]==0,"invalid measurement result still cancels output");
  sample=new Harness();sample.DispatchTriggers=true;sample.Feed(Report10(1));WaitFor(()=>sample.vpWin.Runs[0]==1&&!sample._ioTriggerActiveByJob[0]);sample.Feed(Report10(1));Check(sample.vpWin.Runs[0]==1,"actual external runner does not repeat held high input");sample.Feed(Report10(0));sample.Feed(Report10(1));WaitFor(()=>sample.vpWin.Runs[0]==2&&!sample._ioTriggerActiveByJob[0]);Check(sample.Bitmaps().SequenceEqual(new[]{2,0,2,0}),"actual external runner completes two separate pulses");
  sample=new Harness();sample.DispatchTriggers=true;sample.Feed(Report10(3));WaitFor(()=>sample.vpWin.Runs[0]==1&&sample.vpWin.Runs[1]==1&&!sample.AnyFaceVisionActive());Check(sample.Bitmaps().Count(value=>value==2)==1&&sample.Bitmaps().Count(value=>value==8)==1,"both external inputs run and output their own faces");
 }
 public static void Main(){ParserChecks();RegistrationChecks();FeedbackChecks();RunChecks();Console.WriteLine("TOTAL "+checks+" checks passed");}
}
'''
output = task / 'BehaviourChecks.cs'
output.write_text(source, encoding='utf-8-sig')
framework = Path('C:/Windows/Microsoft.NET/Framework64/v4.0.30319')
command = ['dotnet', 'C:/Program Files/dotnet/sdk/10.0.300/Roslyn/bincore/csc.dll', '/nologo', '/noconfig', '/nostdlib', '/langversion:latest']
command += ['/r:' + str(framework / name) for name in ['mscorlib.dll', 'System.dll', 'System.Core.dll']]
command += ['/out:' + str(task / 'BehaviourChecks.exe'), str(output)]
subprocess.run(command, check=True)
result = subprocess.run([str(task / 'BehaviourChecks.exe')], check=True, capture_output=True, text=True)
(task / 'behaviour-checks.txt').write_text(result.stdout, encoding='utf-8')
print(result.stdout)
