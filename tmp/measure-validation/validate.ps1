$ErrorActionPreference = 'Stop'
$root = 'E:/jieyawenjian/VB_Det'
function Extract-Method($path, $start, $end) {
    $source = [IO.File]::ReadAllText($path)
    $a = $source.IndexOf($start)
    $b = $source.IndexOf($end, $a)
    if ($a -lt 0 -or $b -lt 0) { throw 'Method not found' }
    return $source.Substring($a, $b - $a)
}
$run = Extract-Method "$root/VisionApplication/FrmMainUI.cs" 'private void RunOneTimeThread(bool syncMode)' 'private void UpdateUIExe()'
$begin = Extract-Method "$root/VisionControl/VisionControl.cs" 'private bool TryBeginVppJobCompletion(int jobIndex)' 'private void AbortVppJobCompletion(int jobIndex)'
$prefix = @'
using System;
using System.Collections.Generic;
using System.Threading;
class Param { public CD p_CD = new CD(); public RTP p_VPPRTP = new RTP(); public RT p_RTSP = new RT(); }
class CD { public bool[] bListProcess = {true,true}; }
class RTP { public bool bVisionInfer = true; }
class RT { public int runTimeOut = 10; public bool bUpdateIO = true; }
struct Result { public bool bOk; }
class State { public int iIsRunning; }
class Log { public int iEventStatus; public string strOperEvent, strFailReason; }
enum FlatStyle { Popup }
class Button { public bool InvokeRequired { get { return false; } } public FlatStyle FlatStyle; public void Invoke(Action a) { a(); } }
class VP {
 const int maxJobNum=2;
 public bool bMainTaskFinished;
 public int[] _bRunOneTimeFinished = new int[2];
 int[] bRunOneTimeFinished { get { return _bRunOneTimeFinished; } }
 public bool[] jobBusy = new bool[2], vppJobCompletionPending = new bool[2], vppJobStopped = new bool[2], vppJobResultProcessed = new bool[2];
 object vppJobCompletionLock = new object();
 public string[] strErrInfo = new string[2];
 public Result[] _PMARes = new Result[2];
 public int[] scenario = new int[2]; // 0=success, 1=start failure, 2=timeout
 public List<int> attempted = new List<int>();
 public bool RunOneTime(int i) {
  attempted.Add(i);
  if (!TryBeginVppJobCompletion(i)) { strErrInfo[i]="busy"; return false; }
  if(scenario[i]==1) { jobBusy[i]=false; vppJobCompletionPending[i]=false; strErrInfo[i]="start failure"; return false; }
  if(scenario[i]==2) { vppJobStopped[i]=true; _bRunOneTimeFinished[i]=1; return true; }
  _PMARes[i].bOk=true; _bRunOneTimeFinished[i]=2; jobBusy[i]=false; vppJobCompletionPending[i]=false; return true;
 }
'@
$middle = @'
}
class Harness {
 const int maxCameraNum=2;
 int iJobIndex=0, activeStarts=0, outputs=0, aborts=0;
 bool ioActive=false, rejectIO=false;
 Param cup=new Param(); State rts=new State(); VP vpWin=new VP(); Log operLog=new Log(); Button btFunRunOneTime=new Button();
 Result[] pmaRes=new Result[2]; List<int> ui=new List<int>();
 bool InvokeRequired { get { return false; } }
 void Invoke(Action a) { a(); }
 void AddLog() { }
 void WriteIOFeedbackLog(int status,string name,string error) { }
 bool TryBeginIOFeedbackRun(int i,bool single,out long token,out string error) {
  token=0; error="";
  if(!cup.p_RTSP.bUpdateIO) return true;
  if(rejectIO) { error="reject"; return false; }
  if(!ioActive) { ioActive=true; activeStarts=0; }
  activeStarts++; token=10; return true;
 }
 void AbortIOFeedbackCycle(int i,long token,string reason) { if(token>0&&ioActive) { aborts++; ioActive=false; } }
 void ExeIOOut(int i,long token) { if(cup.p_RTSP.bUpdateIO&&token>0&&ioActive&&activeStarts==2) { outputs++; ioActive=false; } }
 void UpdateUIExeForJob(int i) { ui.Add(i); }
'@
$suffix = @'
 static void Check(bool ok,string name) { if(!ok) throw new Exception(name); }
 static void Test(int a,int b,bool vision,bool io,bool reject,string name) {
  var h=new Harness(); h.vpWin.scenario[0]=a; h.vpWin.scenario[1]=b; h.cup.p_VPPRTP.bVisionInfer=vision; h.cup.p_RTSP.bUpdateIO=io; h.rejectIO=reject;
  h.RunOneTimeThread(true);
  Check(h.vpWin.attempted.Count==2,name+": both jobs attempted");
  Check(h.outputs==((a==0&&b==0&&io&&!reject)?1:0),name+": IO combination");
  Check(h.ui.Count==((a==0?1:0)+(b==0?1:0)),name+": no stale completion");
  Check(h.vpWin.bMainTaskFinished&&h.rts.iIsRunning==0,name+": restored");
  if(a==2) { h.vpWin.scenario[0]=0; h.RunOneTimeThread(true); Check(h.vpWin.attempted.Count==4&&h.ui.Contains(0),name+": retry after stopped"); }
  Console.WriteLine("PASS "+name);
 }
 public static void Main() {
  Test(0,0,true,true,false,"measurement both succeed");
  Test(1,0,true,true,false,"A start failure, B succeeds");
  Test(2,0,true,true,false,"A timeout, B succeeds, retry recovers");
  Test(0,1,true,true,false,"B start failure, no incomplete IO");
  Test(0,2,true,true,false,"B timeout, no stale completion");
  Test(0,0,true,true,true,"IO rejection does not block measurement");
  Test(0,0,true,false,false,"IO disabled");
  Test(0,0,false,true,false,"defect successful path unchanged");
  var h=new Harness(); h.vpWin.jobBusy[0]=true; h.RunOneTimeThread(true); Check(h.vpWin.attempted.Count==2&&h.ui.Count==1&&h.ui[0]==1&&h.outputs==0,"busy A isolation"); Console.WriteLine("PASS busy A isolation");
 }
}
'@
$output = Join-Path $PSScriptRoot 'Harness.cs'
[IO.File]::WriteAllText($output, $prefix + $begin + $middle + $run + $suffix)
$exe = Join-Path $PSScriptRoot 'Harness.exe'
dotnet 'C:/Program Files/dotnet/sdk/10.0.300/Roslyn/bincore/csc.dll' /nologo /noconfig /nostdlib /r:C:/Windows/Microsoft.NET/Framework64/v4.0.30319/mscorlib.dll /r:C:/Windows/Microsoft.NET/Framework64/v4.0.30319/System.dll /r:C:/Windows/Microsoft.NET/Framework64/v4.0.30319/System.Core.dll "/out:$exe" $output
if ($LASTEXITCODE -ne 0) { throw 'Harness compilation failed' }
& $exe
if ($LASTEXITCODE -ne 0) { throw 'Behavior validation failed' }
