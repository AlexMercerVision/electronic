using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommonCls;
using Newtonsoft.Json;

public static class Regression
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
    static void Until(Func<bool> condition, string label)
    {
        var watch = Stopwatch.StartNew();
        while (!condition() && watch.ElapsedMilliseconds < 5000) Thread.Sleep(10);
        Check(condition(), label);
    }
    static OperLog Op(string name, int status, string detail)
    {
        return new OperLog { dtOperTime = DateTime.Now, strOperEvent = name,
            iEventStatus = status, strFailReason = detail, strUserName = "test" };
    }
    static string Daily(string root, string folder, string prefix, string suffix = ".txt")
    { return Path.Combine(root, folder, prefix + DateTime.Now.ToString("yyyyMMdd") + suffix); }
    [STAThread]
    public static void Main(string[] args)
    {
        string directory = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(directory);
        string logRoot = Path.Combine(directory, "new-log");
        Directory.CreateDirectory(logRoot);
        string oldRootFile = Path.Combine(logRoot, "ErrorLog_20000101.txt");
        File.WriteAllText(oldRootFile, "old root untouched");
        foreach (string kind in new[] { "Oper", "Error", "Camera", "Model", "Runtime" }) Directory.CreateDirectory(Path.Combine(logRoot, kind));
        string oldError = Path.Combine(logRoot, "Error", "ErrorLog_20000101.txt");
        string oldCamera = Path.Combine(logRoot, "Camera", "CameraLog_20000101.txt");
        string unknown = Path.Combine(logRoot, "Error", "keep.txt");
        string runtime = Path.Combine(logRoot, "Runtime", "RTS_0000.txt");
        string boundaryKeep = Path.Combine(logRoot, "Error", "ErrorLog_" + DateTime.Now.Date.AddDays(-29).ToString("yyyyMMdd") + ".txt");
        string boundaryDelete = Path.Combine(logRoot, "Error", "ErrorLog_" + DateTime.Now.Date.AddDays(-30).ToString("yyyyMMdd") + ".txt");
        foreach (string file in new[] {oldError, oldCamera, unknown, runtime, boundaryKeep, boundaryDelete}) File.WriteAllText(file, "preserved-or-expired");
        RuntimeLog.Configure(logRoot);
        RuntimeLog.Operation(Op("normal", 0, "normal detail"));
        RuntimeLog.Operation(Op("same-error", -1, "Job0 Code=42"));
        RuntimeLog.Operation(Op("same-error", -1, "Job0 Code=42"));
        RuntimeLog.Operation(Op("same-error", -1, "Job0 Code=42"));
        RuntimeLog.Operation(Op("different-error", -1, "Job1 Code=43"));
        RuntimeLog.Camera("camera-start", 0, "Cam0 serial=test");
        RuntimeLog.Model("[Warmup] success; useGPU=True; gpuID=0");
        string operFile = Daily(logRoot, "Oper", "Oper_");
        string errorFile = Daily(logRoot, "Error", "ErrorLog_");
        Until(() => File.Exists(errorFile) && File.ReadAllLines(errorFile).Length == 2, "first/different error immediate; repeated error suppressed");
        Check(File.ReadAllText(operFile).Contains("normal detail"), "Oper restored with details");
        Until(() => File.Exists(Daily(logRoot, "Camera", "CameraLog_")), "camera category");
        Until(() => File.Exists(Daily(logRoot, "Model", "dl_init_debug_", ".log")), "model category");
        Until(() => !File.Exists(oldError) && !File.Exists(oldCamera), "expired daily files removed");
        Check(!File.Exists(boundaryDelete) && File.Exists(boundaryKeep), "30-day boundary");
        Check(File.Exists(oldRootFile) && File.Exists(unknown) && File.Exists(runtime), "legacy root/unknown/runtime not deleted");
        var errorStates = (IDictionary)typeof(RuntimeLog).GetField("errors", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        // The writer owns its dictionary; pause it only after it has become idle.
        Thread writer = (Thread)typeof(RuntimeLog).GetField("writer", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        Until(() => (writer.ThreadState & System.Threading.ThreadState.WaitSleepJoin) != 0, "writer idle");
#pragma warning disable 618
        writer.Suspend();
        try
        {
            foreach (object value in errorStates.Values)
                value.GetType().GetField("LastWritten", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, DateTime.Now.AddSeconds(-61));
        }
        finally { writer.Resume(); }
#pragma warning restore 618
        Until(() => File.ReadAllText(errorFile).Contains("重复2次"), "60-second repeat summary");
        Check(File.ReadAllText(operFile).Contains("重复2次"), "Oper shares repeat suppression");

        var stats = new DetectionStatistics();
        long first = stats.Begin(0, true);
        Check(stats.Finish(0, first, true, true), "valid zero-defect OK");
        Check(!stats.Finish(0, first, true, true), "duplicate result cannot recount");
        long second = stats.Begin(0, true);
        Check(stats.Finish(0, second, true, false), "measurement NG");
        long timeout = stats.Begin(1, true);
        Check(!stats.Reset(-1), "cannot reset active run");
        Check(stats.Finish(1, timeout, false, false), "timeout is error");
        Check(!stats.Finish(1, timeout, true, true), "late success cannot recount timeout");
        long next = stats.Begin(1, true);
        Check(!stats.Finish(1, timeout, false, false), "old token cannot cancel new run");
        Check(stats.Finish(1, next, true, true), "next run completes");
        long silent = stats.Begin(0, false);
        Check(!stats.Finish(0, silent, true, true), "silent operation excluded");
        DetStat[] snap = stats.Snapshot();
        Check(snap[0].iTotal == 2 && snap[0].iOKNum == 1 && snap[0].iNGNum == 1, "A counters independent");
        Check(snap[1].iTotal == 1 && snap[1].iErrNum == 1, "B error excluded from valid total");
        DetStat combined = DetectionStatistics.Add(snap[0], snap[1]);
        Check(combined.iTotal == 3 && combined.iOKNum == 2 && Math.Abs(combined.iOKPer - 66.66667f) < .001, "weighted aggregate");
        Check(stats.Reset(0) && stats.Snapshot()[1].iTotal == 1, "single reset preserves other face");
        Check(stats.Reset(-1), "all reset");
        Parallel.Invoke(() => RunFace(stats, 0), () => RunFace(stats, 1));
        snap = stats.Snapshot();
        Check(snap[0].iTotal == 2000 && snap[1].iTotal == 2000, "parallel faces exact independent totals");
        for (int refresh = 0; refresh < 200; refresh++) stats.Snapshot();
        Check(stats.Snapshot()[0].iTotal == 2000, "UI refresh does not increment");
        var state = new RunTimeStatus { statisticsVersion = 2, detByJob = snap, detS = DetectionStatistics.Add(snap[0], snap[1]) };
        Check(new FileOper().SaveRunTimeStatus(runtime, state), "snapshot queued");
        Until(() => File.ReadAllText(runtime).Contains("statisticsVersion"), "snapshot serialized and atomically replaced");
        var restored = JsonConvert.DeserializeObject<RunTimeStatus>(File.ReadAllText(runtime));
        Check(restored.detByJob[0].iTotal == 2000 && restored.detS.iTotal == 4000, "per-face persistence");
        Check(!File.Exists(runtime + ".tmp"), "no partial snapshot left");

        // A busy UI must not force a detection/log producer to wait for Control.Invoke.
        long uiMilliseconds = UiLogChecks.Run();
        checks += UiLogChecks.Checks;
        // Disk failures must remain confined to the writer.
        string badRoot = Path.Combine(directory, "root-is-a-file");
        File.WriteAllText(badRoot, "blocked");
        RuntimeLog.Configure(badRoot);
        var failureWatch = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++) RuntimeLog.Camera("failure-test", -1, "disk unavailable");
        Check(failureWatch.ElapsedMilliseconds < 1000, "disk failure cannot block caller");
        long failureMilliseconds = failureWatch.ElapsedMilliseconds;
        RuntimeLog.Configure(logRoot);
        Thread.Sleep(100);
        Until(() => (writer.ThreadState & System.Threading.ThreadState.WaitSleepJoin) != 0, "writer returns idle after failures");
        long before = RuntimeLog.DroppedRecords;
        long saturationMilliseconds;
#pragma warning disable 618
        writer.Suspend();
        try
        {
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < 12000; i++) RuntimeLog.Camera("queue-test", 0, "record " + i);
            saturationMilliseconds = watch.ElapsedMilliseconds;
            Check(saturationMilliseconds < 1500, "stalled writer cannot block producers");
            int queued = (int)typeof(RuntimeLog).GetField("queued", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Check(queued <= 4096 && RuntimeLog.DroppedRecords > before, "bounded queue drops without waiting");
        }
        finally { writer.Resume(); }
#pragma warning restore 618
        Check(RuntimeLog.Shutdown(10000), "background queue drains and exits");
        string[] producerLines = File.ReadAllLines(operFile).Where(line => line.Contains(" | Job") && line.Contains("detail-Job")).ToArray();
        Check(producerLines.Length == 320, "all accepted concurrent operation records persisted");
        Check(producerLines.All(line => { string[] cells = line.Split('|'); return cells[3].Trim() == "detail-" + cells[1].Trim(); }), "file records never mix producer fields");
        Console.WriteLine(JsonConvert.SerializeObject(new { Checks = checks, UiProducerMilliseconds = uiMilliseconds,
            FailedDiskProducerMilliseconds = failureMilliseconds, SaturatedProducerMilliseconds = saturationMilliseconds,
            DroppedRecords = RuntimeLog.DroppedRecords }, Formatting.Indented));
    }
    static void RunFace(DetectionStatistics stats, int job)
    {
        for (int i = 0; i < 2000; i++)
        {
            long token = stats.Begin(job, true);
            stats.Finish(job, token, true, i % 2 == 0);
            stats.Finish(job, token, true, i % 2 == 0);
        }
    }
}
