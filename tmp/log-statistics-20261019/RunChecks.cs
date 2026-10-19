using System;
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
        private readonly DetectionStatistics detectionStatistics = new DetectionStatistics();
        public event Action<int> DetectionStatisticsChanged;
        public DetStat[] GetDetectionStatistics() => detectionStatistics.Snapshot();
        public long GetDetectionRunToken(int jobIndex) => detectionStatistics.Token(jobIndex);
        public bool ResetDetectionStatistics(int jobIndex)
        {
            for (int job = 0; job < maxJobNum; job++)
                if (IsJobBusy(job)) return false;
            return detectionStatistics.Reset(jobIndex);
        }
        public void FailDetectionStatistics(int jobIndex, long token)
        {
            CompleteDetectionStatistics(jobIndex, token, false, false);
        }
        private void CompleteDetectionStatistics(int jobIndex, long token, bool valid, bool ok)
        {
            if (!detectionStatistics.Finish(jobIndex, token, valid, ok)) return;
            // Statistics and logging listeners may never turn a completed detection into a failure.
            try { DetectionStatisticsChanged?.Invoke(jobIndex); }
            catch { }
        }
        public bool RunOneTimeSplite(int jobIndex)
        {            
            if (jobIndex < 0 || jobIndex >= maxCameraNum)
            {
                OnVisionError(jobIndex, $"运行失败: 任务序号超出范围");
                return false;
            }

            if (IsJobBusy(jobIndex))
            {
                strErrInfo[jobIndex] = "当前工位视觉任务未结束";
                OnVisionError(jobIndex, strErrInfo[jobIndex]); // ✅ 添加
                return false;
            }

            if (mCurrentRunState != RunState.RunningContinuous)
                mCurrentRunState = RunState.RunningOnce;

            int iCount = 0;
            int maxTimes = cup.p_RTSP.runTimeOut / 5;
            while (bActivateRun && iCount < maxTimes)
            {
                Thread.Sleep(5);
                iCount++;
            }

            inactiveTime = 0;       //只要运行了，就将不活跃次数置零
            if (!cup.p_VPPRTP.bVisionInfer)
            {
                bRunOneTimeFinished[jobIndex] = 0;
                SetJobBusy(jobIndex, true);
            }

            //2026年6月20日
       

            //2026年4月12日 设置线程运行  
            if (!RunOneTime(jobIndex))
            {
                SetJobBusy(jobIndex, false);
                bRunOneTimeFinished[jobIndex] = 2;
                OnVisionError(jobIndex, strErrInfo[jobIndex]); // ✅ 添加此处
                return false;
            }
            

            return true;
        }
        public bool RunOneTime(int jobIndex)
        {
            bool vppCompletionStarted = false;
            bool runStarted = false;
            long statisticsToken = 0;
            try
            {  
                if (jobIndex >= 0)
                {
                    if (cup.p_VPPRTP.bVisionInfer)
                    {
                        if (!TryBeginVppJobCompletion(jobIndex))
                        {
                            strErrInfo[jobIndex] = "当前工位视觉任务未结束";
                            return false;
                        }
                        vppCompletionStarted = true;
                    }
                    
                    if (cup.p_VPPRTP.bVisionInfer || cup.p_VPPRTP.bDeepInfer)
                        statisticsToken = detectionStatistics.Begin(jobIndex, !SkipImageUpdateForCurrentRun);

                    //2026年4月12日 修改了采集图像函数
                    if (cup.p_VPPRTP.bAcqFromCam && acqFIFO.GrabOneFrameCogFormat(bRecord, cup.p_CD.bListProcess[jobIndex], jobIndex, cup.p_CD.camInd[jobIndex]) ||
                        !cup.p_VPPRTP.bAcqFromCam && acqFIFO.GrabOneFrameCogFromFileWithSrcData(iFileIndex, cup.p_CD.bListProcess[jobIndex], jobIndex, cup.p_CD.camInd[jobIndex]) >=0)
                    {
                        ci8gAcq = acqFIFO._ci8gAcq;
                      //  ci8gFX = acqFIFO.ci8gAcqFX;

                        if (cup.p_VPPRTP.bDeepInfer)   //从GPU实时推理
                        {
                            if (cup.p_VPPRTP.bAcqFromCam)
                                DLInfer(jobIndex);
                            else
                                DLInferOffLine(jobIndex);
                            pmaRes[jobIndex].bLocated = true;
                            CompleteDetectionStatistics(jobIndex, statisticsToken, true, pmaRes[jobIndex].bOk);
                            SetJobBusy(jobIndex, false);
                            // ★★★ 关键：深度推理已完成，立即设置完成标志 ★★★
                            _bRunOneTimeFinished[jobIndex] = 2;
                        }
                        else if(cup.p_VPPRTP.bVisionInfer)
                        {
                            {
                                SetVPPImage(jobIndex);
                                Thread.Sleep(10);  //st** 20220804 
                                mJM.Job(jobIndex).Run();
                            }
                        }
                    }
                    else
                    {
                        strErrInfo[jobIndex] = "图像采集异常:"+ acqFIFO.strErrInfo;
                        // 采集失败也应当设置完成标志，避免死等
                        if (vppCompletionStarted)
                            AbortVppJobCompletion(jobIndex);
                        else
                            _bRunOneTimeFinished[jobIndex] = 2;
                        return false;
                    }
                }
                runStarted = true;
            }
            catch (Cognex.VisionPro.Exceptions.CogNotStoppedException ex)
            {
                // do nothing in this case - we're already running
                strErrInfo[jobIndex] = ResourceUtility.GetString("RtUnexpectedErrorQB") + ex.Message;
                if (vppCompletionStarted)
                    AbortVppJobCompletion(jobIndex);
                return false;
            }
            catch (Cognex.VisionPro.Exceptions.CogException ex)
            {
                if (mCurrentRunState != RunState.RunningContinuous)
                    mCurrentRunState = RunState.Stopped;
                strErrInfo[jobIndex] = ResourceUtility.GetString("RtUnexpectedErrorQB") + ex.Message;
                if (vppCompletionStarted)
                    AbortVppJobCompletion(jobIndex);
                return false;
            }
            catch (Exception ex)
            {
                strErrInfo[jobIndex] = ResourceUtility.GetString("RtUnexpectedErrorQB") + ex.Message;
                if (vppCompletionStarted)
                    AbortVppJobCompletion(jobIndex);
                return false;
            }
            finally
            {
                if (!runStarted && statisticsToken > 0)
                {
                    CompleteDetectionStatistics(jobIndex, statisticsToken, false, false);
                    if (!vppCompletionStarted) SetJobBusy(jobIndex, false);
                }
            }
            return true; 
        }
        private bool TryBeginVppJobCompletion(int jobIndex)
        {
            if (jobIndex < 0 || jobIndex >= maxJobNum)
                return false;

            lock (vppJobCompletionLock)
            {
                if (jobBusy[jobIndex])
                {
                    // 兼容上层现有超时退出：只有已经收到该工位Stopped时才允许清理并重新开始。
                    if (vppJobCompletionPending[jobIndex] &&
                        vppJobStopped[jobIndex] &&
                        bRunOneTimeFinished[jobIndex] == 2)
                    {
                        vppJobCompletionPending[jobIndex] = false;
                        jobBusy[jobIndex] = false;
                    }
                    else
                    {
                        return false;
                    }
                }

                vppJobResultProcessed[jobIndex] = false;
                vppJobStopped[jobIndex] = false;
                vppJobCompletionPending[jobIndex] = true;
                vppJobResultValid[jobIndex] = false;
                pmaRes[jobIndex].bOk = false;
                pmaRes[jobIndex].dPMAScore = 0;
                bRunOneTimeFinished[jobIndex] = 0;
                jobBusy[jobIndex] = true;
                return true;
            }
        }
        private void AbortVppJobCompletion(int jobIndex)
        {
            if (jobIndex < 0 || jobIndex >= maxJobNum)
                return;

            lock (vppJobCompletionLock)
            {
                vppJobResultProcessed[jobIndex] = false;
                vppJobStopped[jobIndex] = false;
                vppJobCompletionPending[jobIndex] = false;
                bRunOneTimeFinished[jobIndex] = 2;
                vppJobResultValid[jobIndex] = false;
                pmaRes[jobIndex].bOk = false;
                pmaRes[jobIndex].dPMAScore = 0;
                jobBusy[jobIndex] = false;
            }
        }
        private void TryCompleteVppJob(int jobIndex)
        {
            long statisticsToken = GetDetectionRunToken(jobIndex);
            lock (vppJobCompletionLock)
            {
                if (!vppJobCompletionPending[jobIndex] ||
                    !vppJobResultProcessed[jobIndex] ||
                    !vppJobStopped[jobIndex])
                {
                    return;
                }

                // 在锁内领取完成权，保证两个异步事件无论谁后到都只完成一次。
                vppJobCompletionPending[jobIndex] = false;
            }

            try
            {
                RunOneTimeFinished(jobIndex);
                if (!SkipImageUpdateForCurrentRun)
                {
                    if (InvokeRequired)
                        Invoke(new Action(() => {
                            ClearRawImageDisplayGraphicsByJobIndex(jobIndex);
                            DirectShowImage(jobIndex);
                        }));
                    else
                    {
                        ClearRawImageDisplayGraphicsByJobIndex(jobIndex);
                        DirectShowImage(jobIndex);
                    }
                }
                CompleteDetectionStatistics(jobIndex, statisticsToken,
                    IsVppJobResultValid(jobIndex), pmaRes[jobIndex].bOk);
                JobResultReady?.Invoke(jobIndex);
            }
            catch (Exception ex)
            {
                vppJobResultValid[jobIndex] = false;
                pmaRes[jobIndex].bOk = false;
                pmaRes[jobIndex].dPMAScore = 0;
                CompleteDetectionStatistics(jobIndex, statisticsToken, false, false);
                strErrInfo[jobIndex] = $"jobIndex={jobIndex} 本轮结果显示/通知失败: {ex}";
                OnVisionError(jobIndex, strErrInfo[jobIndex]);
            }
            finally
            {
                lock (vppJobCompletionLock)
                {
                    jobBusy[jobIndex] = false;
                    // 图像和本轮结果已经更新，最后释放等待线程。
                    bRunOneTimeFinished[jobIndex] = 2;
                }
            }

        }

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
