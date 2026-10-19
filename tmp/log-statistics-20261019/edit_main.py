exec((__import__('pathlib').Path(__file__).parent / 'edit_sources.py').read_text(encoding='utf-8').split("p = 'CommonCls/CDL.cs'")[0])

p = 'VisionApplication/FrmMainUI.cs'
s = read(p)
s = once(s, 'using System.Collections.Generic;', 'using System.Collections.Generic;\nusing System.Collections.Concurrent;')
s = once(s, '        private OperLog operLog = new OperLog();                // 操作日志记录对象', '''        // Each producer assembles its own record; A/B callbacks cannot overwrite each other's fields.
        private readonly ThreadLocal<OperLog[]> operationLogs = new ThreadLocal<OperLog[]>(() => new OperLog[1]);
        private ref OperLog operLog => ref operationLogs.Value[0];
        private string logUserName = "";
        private readonly ConcurrentQueue<OperLog> pendingUiLogs = new ConcurrentQueue<OperLog>();
        private int pendingUiLogCount, logUiScheduled;
        private bool closingAfterLogDrain;''')
s = once(s, '        private DetStat detStat = new DetStat();                // 检测统计', '''        private readonly object detectionStatisticsSync = new object();
        private DetStat[] detectionStatisticsBase = new DetStat[2];
        private bool statisticsDirty;
        private DateTime lastStatisticsSave = DateTime.MinValue;''')
s = method(s, '        private static void WriteStartupLog(string message)', '        private void LoadVPFile', '''        private static void WriteStartupLog(string message)
        {
            RuntimeLog.Model(message);
        }

''')
s = once(s, '                bool bStatus = FileOper.SaveUserParam(cup);', '                RuntimeLog.Configure(cup.p_Paf.strLogPath);\n                bool bStatus = FileOper.SaveUserParam(cup);')
s = once(s, '                vpWin.JobResultReady += OnJobResultReady;', '''                vpWin.JobResultReady += OnJobResultReady;
                vpWin.DetectionStatisticsChanged -= OnDetectionStatisticsChanged;
                vpWin.DetectionStatisticsChanged += OnDetectionStatisticsChanged;''')
s = method(s, '        private void WriteCameraDiagnostic(string operation, int status, string detail)', '        /// <summary>\n        /// 主动连接', '''        private void WriteCameraDiagnostic(string operation, int status, string detail)
        {
            RuntimeLog.Camera(operation, status, detail);
        }

''')
s = once(s, '            string strPath = cup.p_Paf.strLogPath + "\\\\RTS_0000.txt";\n            fo.ReadRunTimeStatus(strPath, ref rts);', '''            ReplaceDrive();
            RuntimeLog.Configure(cup.p_Paf.strLogPath);
            string strPath = Path.Combine(cup.p_Paf.strLogPath, "Runtime", "RTS_0000.txt");
            // Read-only compatibility; no historical file is moved, overwritten or deleted.
            if (!fo.ReadRunTimeStatus(strPath, ref rts))
                fo.ReadRunTimeStatus(Path.Combine(cup.p_Paf.strLogPath, "RTS_0000.txt"), ref rts);
            if (rts.statisticsVersion != 2 || rts.detByJob == null || rts.detByJob.Length != maxJobNum)
            {
                if (!rts.legacyDetS.HasValue) rts.legacyDetS = rts.detS;
                rts.detByJob = new DetStat[maxJobNum];
                rts.detS = new DetStat();
                rts.statisticsVersion = 2;
            }
            detectionStatisticsBase = (DetStat[])rts.detByJob.Clone();''')
s = once(s, '            detStat = rts.detS;', '            statisticsDirty = true;')
s = method(s, '        private string _lastLogHash', '        #endregion  自定义操作区', '''        private void AddLog()
        {
            OperLog record = operLog;
            record.dtOperTime = DateTime.Now;
            record.strUserName = Volatile.Read(ref logUserName);
            RuntimeLog.Operation(record);
            if (RuntimeLog.StatusText(record.iEventStatus) != "执行正常")
            {
                string line = $"{record.dtOperTime:yyyy-MM-dd HH:mm:ss} | {record.strOperEvent} | {RuntimeLog.StatusText(record.iEventStatus)} | {record.strFailReason}";
                lock (_errorLogLock)
                {
                    _recentErrorLogs.Add(line);
                    if (_recentErrorLogs.Count > 20) _recentErrorLogs.RemoveAt(0);
                }
            }
            // Bounded UI queue; neither disk latency nor a busy UI can hold the producer.
            if (Interlocked.Increment(ref pendingUiLogCount) > 128)
            {
                Interlocked.Decrement(ref pendingUiLogCount);
                return;
            }
            pendingUiLogs.Enqueue(record);
            ScheduleLogUi();
        }

        private void ScheduleLogUi()
        {
            if (IsDisposed || !IsHandleCreated || Interlocked.CompareExchange(ref logUiScheduled, 1, 0) != 0) return;
            try { BeginInvoke(new Action(DrainLogUi)); }
            catch (InvalidOperationException) { Interlocked.Exchange(ref logUiScheduled, 0); }
        }

        private void DrainLogUi()
        {
            try
            {
                OperLog record;
                int count = 0;
                while (count++ < 20 && pendingUiLogs.TryDequeue(out record))
                {
                    Interlocked.Decrement(ref pendingUiLogCount);
                    try
                    {
                        while (dgvLog.RowCount >= 20) dgvLog.Rows.RemoveAt(dgvLog.RowCount - 1);
                        dgvLog.Rows.Insert(0, 1);
                        var row = dgvLog.Rows[0];
                        row.Cells[0].Value = record.dtOperTime.ToString("yyyy-MM-dd HH:mm:ss");
                        row.Cells[1].Value = record.strOperEvent;
                        row.Cells[2].Value = RuntimeLog.StatusText(record.iEventStatus);
                        row.Cells[3].Value = record.strFailReason;
                        row.Cells[4].Value = record.strUserName;
                        row.DefaultCellStyle.ForeColor = record.iEventStatus == -2 ||
                            RuntimeLog.StatusText(record.iEventStatus) != "执行正常" ? Color.Red : Color.Black;
                    }
                    catch { /* A disposed/busy log grid cannot affect detection or file logging. */ }
                }
            }
            finally
            {
                Interlocked.Exchange(ref logUiScheduled, 0);
                if (!pendingUiLogs.IsEmpty) ScheduleLogUi();
            }
        }

''')
s = once(s, '                operLog.strUserName = login.strID;', '                Volatile.Write(ref logUserName, login.strID);')
s = once(s, '            operLog.strUserName = "";', '            Volatile.Write(ref logUserName, "");')
# Preview records should also be submitted before scheduling any UI work.
s = method(s, '        private void ShowPreviewStatusOnUi(', '        private void OnAcqFIFOError(', '''        private void ShowPreviewStatusOnUi(int jobIndex, int cameraIndex, int status, string message)
        {
            operLog.iEventStatus = status;
            operLog.strOperEvent = $"实时预览-工位{jobIndex + 1}/相机{cameraIndex + 1}";
            operLog.strFailReason = message;
            AddLog();
        }
''')
s = once(s, '        private void FrmMainUI_FormClosing(object sender, FormClosingEventArgs e)\n        {', '''        private async void FrmMainUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (closingAfterLogDrain) return;''')
s = once(s, '            string strPath = cup.p_Paf.strLogPath + "\\\\RTS_0000.txt";\n            fo.SaveRunTimeStatus(strPath, rts);', '            QueueStatisticsSnapshot(true);')
s = once(s, '''            if (Debugger.IsAttached == false) // 非调试环境
            {
                Environment.Exit(0);
            }''', '''            // Let the UI message loop run while the background writer drains, with a bounded exit wait.
            e.Cancel = true;
            QueueStatisticsSnapshot(true);
            await Task.Run(() => RuntimeLog.Shutdown(1500));
            closingAfterLogDrain = true;
            Close();
            if (!Debugger.IsAttached) Environment.Exit(0);''')
s = once(s, '        private void timer1_Tick(object sender, EventArgs e)\n        {', '''        private void timer1_Tick(object sender, EventArgs e)
        {
            ScheduleLogUi();
            QueueStatisticsSnapshot(false);''')
s = method(s, '        private void UpdateStatisticsForJob(int i)', '        private void ViewChange()', '''        private void OnDetectionStatisticsChanged(int jobIndex)
        {
            lock (detectionStatisticsSync)
            {
                RefreshDetectionStatistics();
                rts.dtLastDet = DateTime.Now;
                statisticsDirty = true;
            }
        }

        private void RefreshDetectionStatistics()
        {
            if (vpWin == null) return;
            DetStat[] current = vpWin.GetDetectionStatistics();
            for (int job = 0; job < maxJobNum; job++)
                rts.detByJob[job] = DetectionStatistics.Add(detectionStatisticsBase[job], current[job]);
            rts.detS = DetectionStatistics.Add(rts.detByJob[0], rts.detByJob[1]);
        }

        private void QueueStatisticsSnapshot(bool force)
        {
            lock (detectionStatisticsSync)
            {
                if (!force && (!statisticsDirty || (DateTime.Now - lastStatisticsSave).TotalSeconds < 30)) return;
                RefreshDetectionStatistics();
                string path = Path.Combine(cup.p_Paf.strLogPath, "Runtime", "RTS_0000.txt");
                if (fo.SaveRunTimeStatus(path, rts))
                {
                    statisticsDirty = false;
                    lastStatisticsSave = DateTime.Now;
                }
            }
        }

        private void UpdateStatisticsForJob(int i)
        {
            if (vpWin == null || i < 0 || i >= maxJobNum) return;
            strStatus = vpWin.RetStatisticsForJob(i);
            if (strStatus == null) return;
            lock (detectionStatisticsSync)
            {
                RefreshDetectionStatistics();
                toolStripStatus_ExcuteTimes.Text = "执行次数：" + strStatus[0] + "/" + rts.detS.iTotal;
            }
        }

        private void ResetDetectionStatistics(int jobIndex)
        {
            if (vpWin == null || !vpWin.bMainTaskFinished || !vpWin.ResetDetectionStatistics(jobIndex)) return;
            lock (detectionStatisticsSync)
            {
                if (jobIndex == -1) detectionStatisticsBase = new DetStat[maxJobNum];
                else detectionStatisticsBase[jobIndex] = new DetStat();
                RefreshDetectionStatistics();
                statisticsDirty = true;
            }
            QueueStatisticsSnapshot(true);
            UpdateStatisticsForJob(jobIndex < 0 ? 0 : jobIndex);
        }

''')
s = method(s, '        private void button_ResetStatistics_Click(', '        private void menuCam_OpenCloseDev_Click(', '''        private void button_ResetStatistics_Click(object sender, EventArgs e)
        {
            if (iJobIndex < 0 || iJobIndex >= maxJobNum) return;
            ResetDetectionStatistics(iJobIndex);
        }

''')
s = method(s, '        private void button_ResetStatisticsForAllJobs_Click(', '        private void SetShowType(', '''        private void button_ResetStatisticsForAllJobs_Click(object sender, EventArgs e)
        {
            ResetDetectionStatistics(-1);
        }

''')
s = once(s, '        private void menu_View_ErrLog_Click(object sender, EventArgs e)', '        private async void menu_View_ErrLog_Click(object sender, EventArgs e)')
# Avoid holding the in-memory error lock during a disk read.
s = once(s, '                    errorList = LoadRecentErrorsFromFile();', '                    errorList = null;')
s = once(s, '            if (errorList.Count == 0)', '            if (errorList == null) errorList = await Task.Run(() => LoadRecentErrorsFromFile());\n            if (IsDisposed) return;\n            if (errorList.Count == 0)')
s = once(s, '                string logDir = cup.p_Paf.strLogPath;', '                string logDir = Path.Combine(cup.p_Paf.strLogPath, "Error");')
# Track accepted runs in each existing wrapper; only a timeout still needs explicit error settlement.
s = once(s, '                    // 等待该工位完成（轮询直到', '                    long statisticsRunToken = vpWin.GetDetectionRunToken(jobIndex);\n                    // 等待该工位完成（轮询直到')
s = once(s, '                        // 超时处理：强制结束并记录', '                        vpWin.FailDetectionStatistics(jobIndex, statisticsRunToken);\n                        // 超时处理：强制结束并记录')
s = once(s, '''                if (vpWin.RunOneTime(selectedJobIndex))
                {
                    int count''', '''                if (vpWin.RunOneTime(selectedJobIndex))
                {
                    long statisticsRunToken = vpWin.GetDetectionRunToken(selectedJobIndex);
                    int count''')
s = once(s, '                    bool continuousRunCompleted = vpWin._bRunOneTimeFinished[selectedJobIndex] == 2;', '''                    bool continuousRunCompleted = vpWin._bRunOneTimeFinished[selectedJobIndex] == 2;
                    if (!continuousRunCompleted) vpWin.FailDetectionStatistics(selectedJobIndex, statisticsRunToken);''')
s = once(s, '''                if (vpWin.RunOneTimeSplite(legacyJobIndex))
                {
                    int count''', '''                if (vpWin.RunOneTimeSplite(legacyJobIndex))
                {
                    long statisticsRunToken = vpWin.GetDetectionRunToken(legacyJobIndex);
                    int count''')
s = once(s, '                    bool legacyRunCompleted = vpWin._bRunOneTimeFinished[legacyJobIndex] == 2;', '''                    bool legacyRunCompleted = vpWin._bRunOneTimeFinished[legacyJobIndex] == 2;
                    if (!legacyRunCompleted) vpWin.FailDetectionStatistics(legacyJobIndex, statisticsRunToken);''')
save(p, s)

p = 'VisionApplication/FrmMain_Laser.cs'
s = read(p)
# Only edit the live implementation, leaving historical commented copies unchanged.
start = s.index('        private async Task RunOneTimeForFace(int faceIndex, bool singleJobIOCycle = true)')
end = s.index('        /*private async Task RunOneTimeForFace', start)
block = s[start:end]
block = once(block, '            bool runSucceeded = false;', '            bool runSucceeded = false;\n            long statisticsRunToken = 0;')
block = once(block, '                        int maxWait =', '                        statisticsRunToken = vpWin.GetDetectionRunToken(faceIndex);\n                        int maxWait =')
block = once(block, '                if (!runSucceeded)\n                    AbortIOFeedbackCycle', '''                if (!visionOk && statisticsRunToken > 0)
                    vpWin.FailDetectionStatistics(faceIndex, statisticsRunToken);
                if (!runSucceeded)
                    AbortIOFeedbackCycle''')
s = s[:start] + block + s[end:]
save(p, s)

p = 'CommonCls/CUserParam.cs'
s = read(p)
s = once(s, '        public DetStat[] detByJob;', '        public DetStat[] detByJob;\n        public DetStat? legacyDetS;')
s = once(s, '                File.WriteAllText(path, json);', '                return RuntimeLog.SaveSnapshot(path, json);')
# The former return was for a synchronous file write; queued is the only truthful success here.
s = once(s, '                return RuntimeLog.SaveSnapshot(path, json);\n                return true;', '                return RuntimeLog.SaveSnapshot(path, json);')
save(p, s)
