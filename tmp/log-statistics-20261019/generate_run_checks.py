from pathlib import Path
root = Path(__file__).resolve().parents[2]
ext = (root/'VisionControl/VisionControlExtention.cs').read_text(encoding='utf-8-sig')
core = (root/'VisionControl/VisionControl.cs').read_text(encoding='utf-8-sig')
def extract(source, signature):
    start = source.index(signature)
    brace = source.index('{', start)
    depth = 1
    pos = brace + 1
    while depth:
        depth += (source[pos] == '{') - (source[pos] == '}')
        pos += 1
    return source[start:pos] + '\n'
members = ext[ext.index('        private readonly DetectionStatistics detectionStatistics'):ext.index('      //  public string strErrReson')]
methods = ''.join(extract(ext, sig) for sig in ['        public bool RunOneTimeSplite(int jobIndex)', '        public bool RunOneTime(int jobIndex)'])
methods += extract(core, '        private bool TryBeginVppJobCompletion(int jobIndex)')
methods += extract(core, '        private void AbortVppJobCompletion(int jobIndex)')
methods += extract(core, '        private void TryCompleteVppJob(int jobIndex)')
text = '''using System;
using System.Threading;
using CommonCls;
namespace Cognex.VisionPro.Exceptions {
 public class CogException : Exception { }
 public class CogNotStoppedException : CogException { }
}
public class RunChecks {
 const int maxCameraNum=2, maxJobNum=2;
 class Mode { public bool bVisionInfer, bDeepInfer=true, bAcqFromCam=true; }
 class Timeout { public int runTimeOut=50; }
 class Camera { public bool[] bListProcess={true,true}; public int[] camInd={1,0}; }
 class Parameters { public Mode p_VPPRTP=new Mode(); public Timeout p_RTSP=new Timeout(); public Camera p_CD=new Camera(); }
 class Acquisition {
  public bool Accept=true; public string strErrInfo="fake acquisition failure"; public object _ci8gAcq;
  public bool GrabOneFrameCogFormat(bool record,bool enabled,int job,int camera) { return Accept; }
  public int GrabOneFrameCogFromFileWithSrcData(int index,bool enabled,int job,int camera) { return Accept ? 0 : -1; }
 }
 class FakeJob { public void Run() { } }
 class Manager { public FakeJob Job(int index) { return new FakeJob(); } }
 struct Result { public bool bOk,bLocated; public double dPMAScore; }
 enum RunState { Stopped,RunningOnce,RunningContinuous }
 static class ResourceUtility { public static string GetString(string key) { return key; } }
 private Parameters cup=new Parameters(); private Acquisition acqFIFO=new Acquisition(); private Manager mJM=new Manager();
 private object ci8gAcq; private int iFileIndex,inactiveTime; private bool bRecord,bActivateRun;
 private bool[] jobBusy=new bool[2]; private Result[] pmaRes=new Result[2];
 private int[] bRunOneTimeFinished=new int[2]; private int[] _bRunOneTimeFinished;
 private string[] strErrInfo=new string[2]; private RunState mCurrentRunState;
 private object vppJobCompletionLock=new object();
 private bool[] vppJobCompletionPending=new bool[2],vppJobStopped=new bool[2],vppJobResultProcessed=new bool[2],vppJobResultValid=new bool[2];
 private bool SkipImageUpdateForCurrentRun,throwInference,measuredOk=true,measurementValid=true;
 private bool InvokeRequired { get { return false; } }
 public event Action<int> JobResultReady;
 public RunChecks() { _bRunOneTimeFinished=bRunOneTimeFinished; }
 private void Invoke(Action action) { action(); }
 private void ClearRawImageDisplayGraphicsByJobIndex(int job) { }
 private void DirectShowImage(int job) { }
 private void OnVisionError(int job,string error) { }
 private bool IsJobBusy(int job) { return jobBusy[job]; }
 private void SetJobBusy(int job,bool busy) { jobBusy[job]=busy; }
 private bool IsVppJobResultValid(int job) { return vppJobResultValid[job]; }
 private void SetVPPImage(int job) { }
 private void DLInfer(int job) { if(throwInference)throw new Exception("inference failed"); pmaRes[job].bOk=measuredOk; }
 private void DLInferOffLine(int job) { DLInfer(job); }
 private void RunOneTimeFinished(int job) { pmaRes[job].bOk=measuredOk; vppJobResultValid[job]=measurementValid; }
''' + members + methods + '''
 static int checks;
 static void Check(bool value,string label) { if(!value) throw new Exception(label); checks++; }
 void CompleteVpp(int job) { vppJobStopped[job]=true; vppJobResultProcessed[job]=true; TryCompleteVppJob(job); }
 public static void Main() {
  var c=new RunChecks(); int notifications=0;
  c.DetectionStatisticsChanged+=job=>notifications++;
  Check(c.RunOneTime(0),"direct manual deep run accepted");
  Check(c.GetDetectionStatistics()[0].iOKNum==1,"direct manual deep counted");
  c.measuredOk=false;
  Check(c.RunOneTimeSplite(1),"split IO deep run accepted");
  Check(c.GetDetectionStatistics()[1].iNGNum==1 && !c.jobBusy[1],"split IO deep NG counted and released");
  c.cup.p_VPPRTP.bAcqFromCam=false;
  Check(c.RunOneTime(0) && c.GetDetectionStatistics()[0].iNGNum==1,"offline entry counted");
  c.throwInference=true;
  Check(!c.RunOneTime(0) && c.GetDetectionStatistics()[0].iErrNum==1,"inference failure is error");
  c.throwInference=false; c.acqFIFO.Accept=false;
  Check(!c.RunOneTime(1) && c.GetDetectionStatistics()[1].iErrNum==1,"acquisition failure is error");
  c.acqFIFO.Accept=true; c.SkipImageUpdateForCurrentRun=true;
  int before=c.GetDetectionStatistics()[0].iTotal;
  Check(c.RunOneTime(0) && c.GetDetectionStatistics()[0].iTotal==before,"silent run excluded at common entry");
  c.SkipImageUpdateForCurrentRun=false;
  c.cup.p_VPPRTP.bDeepInfer=false; c.cup.p_VPPRTP.bVisionInfer=true;
  c.measuredOk=true;
  Check(c.RunOneTime(0),"manual VPP accepted");
  Check(c.GetDetectionStatistics()[0].iTotal==before,"VPP launch does not count before completion");
  c.CompleteVpp(0); c.CompleteVpp(0);
  Check(c.GetDetectionStatistics()[0].iTotal==before+1,"VPP repeated callback counted once");
  c.measuredOk=false;
  Check(c.RunOneTimeSplite(1),"IO VPP accepted"); c.CompleteVpp(1);
  Check(c.GetDetectionStatistics()[1].iNGNum==2,"VPP uses actual NG result");
  c.measurementValid=false;
  Check(c.RunOneTime(0),"invalid VPP starts"); c.CompleteVpp(0);
  Check(c.GetDetectionStatistics()[0].iErrNum==2,"invalid measurement counted as error");
  c.measurementValid=true;
  Check(c.RunOneTime(1),"timeout run starts");
  long token=c.GetDetectionRunToken(1);
  c.FailDetectionStatistics(1,token);
  int completed=c.GetDetectionStatistics()[1].iTotal;
  c.CompleteVpp(1);
  Check(c.GetDetectionStatistics()[1].iTotal==completed && c.GetDetectionStatistics()[1].iErrNum==2,"late VPP result cannot overwrite timeout statistic");
  Check(c.RunOneTime(0),"busy reset test starts");
  Check(!c.ResetDetectionStatistics(-1),"SDK-active reset rejected"); c.CompleteVpp(0);
  Check(c.ResetDetectionStatistics(-1),"reset after SDK completion accepted");
  Check(notifications==10,"exact terminal notifications");
  Console.WriteLine("Actual extracted detection entry/completion methods: "+checks+" checks passed");
 }
}
'''
(Path(__file__).parent/'RunChecks.cs').write_text(text,encoding='utf-8-sig')
