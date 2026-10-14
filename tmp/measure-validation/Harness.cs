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
 }private bool TryBeginVppJobCompletion(int jobIndex)
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
                bRunOneTimeFinished[jobIndex] = 0;
                jobBusy[jobIndex] = true;
                return true;
            }
        }

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
 void UpdateUIExeForJob(int i) { ui.Add(i); }private void RunOneTimeThread(bool syncMode)
        {
            // 1. 确定需要执行的工位列表
            List<int> jobIndices = new List<int>();
            if (syncMode)
            {
                // 同步模式：遍历所有启用的工位
                for (int i = 0; i < maxCameraNum; i++)
                {
                    if (cup.p_CD.bListProcess[i])
                        jobIndices.Add(i);
                }
            }
            else
            {
                // 单工位模式：只执行当前选中的工位（必须启用）
                if (iJobIndex >= 0 && iJobIndex < maxCameraNum && cup.p_CD.bListProcess[iJobIndex])
                    jobIndices.Add(iJobIndex);
                else
                {
                    // 若当前工位未启用，则不执行任何操作（或可改为执行第一个启用工位）
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "单次运行";
                    operLog.strFailReason = "当前工位未启用";
                    AddLog();
                    return;
                }
            }

            if (jobIndices.Count == 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "单次运行";
                operLog.strFailReason = "没有启用的工位";
                AddLog();
                return;
            }

            long activeIOCycleToken = 0;
            int activeIOJobIndex = -1;
            bool independentVisionJobs = syncMode && cup.p_VPPRTP.bVisionInfer;
            bool ioFeedbackCancelled = false;
            try
            {
                rts.iIsRunning = 1;
                vpWin.bMainTaskFinished = false;

                // 顺序执行每个工位（若需并行，可改为 Parallel.ForEach 或 Task 并行）
                foreach (int jobIndex in jobIndices)
                {
                    string ioFeedbackError;
                    if (!ioFeedbackCancelled && !TryBeginIOFeedbackRun(jobIndex, !syncMode,
                        out activeIOCycleToken, out ioFeedbackError))
                    {
                        WriteIOFeedbackLog(-1, "运行IO批次拒绝", ioFeedbackError);
                        if (!independentVisionJobs)
                            break;
                        AbortIOFeedbackCycle(jobIndex, activeIOCycleToken, "同步运行IO批次拒绝");
                        ioFeedbackCancelled = true;
                        activeIOCycleToken = 0;
                    }
                    activeIOJobIndex = jobIndex;

                    // VPP入口先检查/恢复该工位busy，再初始化完成标志。
                    if (!cup.p_VPPRTP.bVisionInfer)
                        vpWin._bRunOneTimeFinished[jobIndex] = 0;

                    // 执行该工位的检测
                    if (!vpWin.RunOneTime(jobIndex))
                    {
                        // 启动失败，强制标记完成并记录错误
                        vpWin._bRunOneTimeFinished[jobIndex] = 2;
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = $"工位{jobIndex + 1}运行一次";
                        operLog.strFailReason = vpWin.strErrInfo?[jobIndex] ?? "启动失败";
                        AddLog();
                        AbortIOFeedbackCycle(jobIndex, activeIOCycleToken, "同步运行启动失败");
                        if (independentVisionJobs)
                            ioFeedbackCancelled = true;
                        if (syncMode && activeIOCycleToken > 0 && !independentVisionJobs)
                            break;
                        continue;
                    }

                    // 等待该工位完成（轮询直到 _bRunOneTimeFinished 变为 2 或超时）
                    int count = 0;
                    int maxTimes = cup.p_RTSP.runTimeOut / 5; // 假设 runTimeOut 单位为毫秒
                    bool jobRunFailed = false;
                    while (vpWin._bRunOneTimeFinished[jobIndex] != 2 && count < maxTimes)
                    {
                        Thread.Sleep(5);
                        count++;
                    }

                    if (vpWin._bRunOneTimeFinished[jobIndex] != 2)
                    {
                        // 超时处理：强制结束并记录
                        vpWin._bRunOneTimeFinished[jobIndex] = 2;
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = $"工位{jobIndex + 1}运行一次";
                        operLog.strFailReason = "视觉检测超时";
                        AddLog();
                        AbortIOFeedbackCycle(jobIndex, activeIOCycleToken, "同步运行超时");
                        if (independentVisionJobs)
                            ioFeedbackCancelled = true;
                        jobRunFailed = true;
                    }
                    else
                    {
                        // 正常完成，获取结果（假设 pmaRes 数组存储每个工位的 PMA 结果）
                        pmaRes[jobIndex] = vpWin._PMARes[jobIndex];
                        if (!ioFeedbackCancelled)
                            ExeIOOut(jobIndex, activeIOCycleToken);
                    }

                    // 超时没有本轮有效结果，不能记录完成或累加统计。
                    if (jobRunFailed && cup.p_VPPRTP.bVisionInfer)
                        continue;

                    // 更新该工位的 UI（必须在主线程执行）
                    if (this.InvokeRequired)
                        this.Invoke(new Action(() => UpdateUIExeForJob(jobIndex)));
                    else
                        UpdateUIExeForJob(jobIndex);

                    if (jobRunFailed && syncMode && activeIOCycleToken > 0)
                        break;
                }

                rts.iIsRunning = 0;
                vpWin.bMainTaskFinished = true;
            }
            catch (Exception ex)
            {
                AbortIOFeedbackCycle(activeIOJobIndex, activeIOCycleToken, "单次运行异常");
                rts.iIsRunning = -1;
                vpWin.bMainTaskFinished = true;
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "单次运行异常";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
            finally
            {
                // 恢复按钮样式（在 UI 线程执行）
                if (btFunRunOneTime.InvokeRequired)
                    btFunRunOneTime.Invoke(new Action(() => btFunRunOneTime.FlatStyle = FlatStyle.Popup));
                else
                    btFunRunOneTime.FlatStyle = FlatStyle.Popup;
            }
        }
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