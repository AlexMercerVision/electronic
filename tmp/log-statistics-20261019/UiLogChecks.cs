using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommonCls;
public class UiLogChecks : Form
{
    public static int Checks;
    private readonly List<string> _recentErrorLogs = new List<string>();
    private readonly object _errorLogLock = new object();
    private DataGridView dgvLog = new DataGridView { AllowUserToAddRows = false, ColumnCount = 5 };
        private readonly ThreadLocal<OperLog[]> operationLogs = new ThreadLocal<OperLog[]>(() => new OperLog[1]);
        private ref OperLog operLog => ref operationLogs.Value[0];
        private string logUserName = "";
        private readonly ConcurrentQueue<OperLog> pendingUiLogs = new ConcurrentQueue<OperLog>();
        private int pendingUiLogCount, logUiScheduled;

        private void AddLog()
        {
            OperLog record = operLog;
            operLog.strFailReason = null; // Do not carry the previous operation's detail into the next record.
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


    public static long Run()
    {
        using (var form = new UiLogChecks())
        {
            var handle = form.Handle;
            var watch = Stopwatch.StartNew();
            var producers = Task.Run(() => Parallel.For(0, 8, job =>
            {
                for (int i = 0; i < 40; i++)
                {
                    form.operLog.strOperEvent = "Job" + job;
                    form.operLog.strFailReason = "detail-Job" + job;
                    form.operLog.iEventStatus = 0;
                    form.AddLog();
                }
            }));
            // Deliberately do not pump the UI while producers submit records.
            if (!producers.Wait(1500)) throw new Exception("producer synchronously waited for UI");
            long elapsed = watch.ElapsedMilliseconds;
            Checks++;
            if (form.pendingUiLogCount > 128) throw new Exception("UI queue unbounded");
            Checks++;
            for (int i = 0; i < 30 && !form.pendingUiLogs.IsEmpty; i++) Application.DoEvents();
            if (form.dgvLog.RowCount > 20 || form.dgvLog.RowCount == 0) throw new Exception("UI history bound");
            Checks++;
            foreach (DataGridViewRow row in form.dgvLog.Rows)
                if (row.Cells[3].Value.ToString() != "detail-" + row.Cells[1].Value.ToString())
                    throw new Exception("parallel record fields mixed");
            Checks++;
            form.operLog.strOperEvent = "old-operation";
            form.operLog.strFailReason = "old-error";
            form.operLog.iEventStatus = -1;
            form.AddLog();
            form.operLog.strOperEvent = "next-operation";
            form.operLog.iEventStatus = 0;
            form.AddLog();
            Application.DoEvents();
            if (form.dgvLog.Rows[0].Cells[3].Value != null) throw new Exception("previous error leaked into next operation");
            Checks++;
            if (form.dgvLog.Rows[1].Cells[3].Value.ToString() != "old-error") throw new Exception("snapshot detail was changed after submission");
            Checks++;
            return elapsed;
        }
    }
}
