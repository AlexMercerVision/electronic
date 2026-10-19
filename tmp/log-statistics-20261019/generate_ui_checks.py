from pathlib import Path
root = Path(__file__).resolve().parents[2]
source = (root/'VisionApplication/FrmMainUI.cs').read_text(encoding='utf-8-sig')
methods = source[source.index('        private void AddLog()'):source.index('        #endregion  自定义操作区')]
fields = source[source.index('        private readonly ThreadLocal<OperLog[]>'):source.index('        private DetectionResult dr')]
fields = fields.replace('        private bool closingAfterLogDrain, logDrainInProgress;', '')
text = '''using System;
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
''' + fields + methods + '''
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
'''
(Path(__file__).parent/'UiLogChecks.cs').write_text(text, encoding='utf-8-sig')
