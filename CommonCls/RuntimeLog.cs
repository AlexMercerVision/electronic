using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace CommonCls
{
    // Producers only submit immutable records. All file access, throttling and retention run here.
    public static class RuntimeLog
    {
        private sealed class Entry
        {
            internal string Root, Category, Name, Text;
            internal DateTime Time;
            internal OperLog? Operation;
            internal bool Replace;
        }

        private sealed class RepeatedError
        {
            internal Entry First;
            internal DateTime LastWritten, LastSeen;
            internal int Count;
        }

        private const int Capacity = 4096;
        private static readonly ConcurrentQueue<Entry> queue = new ConcurrentQueue<Entry>();
        private static readonly AutoResetEvent wake = new AutoResetEvent(false);
        private static readonly Dictionary<string, RepeatedError> errors = new Dictionary<string, RepeatedError>();
        private static readonly Dictionary<string, DateTime> cleaned = new Dictionary<string, DateTime>();
        private static string root;
        private static int queued, stopping;
        private static long dropped;
        private static readonly Thread writer;

        static RuntimeLog()
        {
            writer = new Thread(WriteLoop) { IsBackground = true, Name = "RuntimeLogWriter" };
            writer.Start();
        }

        public static long DroppedRecords => Interlocked.Read(ref dropped);
        public static void Configure(string directory)
        {
            // No directory creation or probing on the caller's thread.
            Volatile.Write(ref root, directory);
        }

        public static string StatusText(int status)
        {
            switch (status)
            {
                case 0: case 2: case 9: case 10: case -2: return "执行正常";
                case -1: return "执行失败";
                case 1: return "执行超时";
                default: return "执行异常";
            }
        }

        private static bool Submit(Entry entry)
        {
            try
            {
                if (Volatile.Read(ref stopping) != 0 || string.IsNullOrWhiteSpace(entry.Root)) return false;
                if (Interlocked.Increment(ref queued) > Capacity)
                {
                    Interlocked.Decrement(ref queued);
                    Interlocked.Increment(ref dropped);
                    return false;
                }
                queue.Enqueue(entry);
                wake.Set();
                return true;
            }
            catch { return false; }
        }

        public static void Operation(OperLog operation)
        {
            Submit(new Entry { Root = Volatile.Read(ref root), Time = operation.dtOperTime,
                Operation = operation });
        }

        public static void Camera(string operation, int status, string detail)
        {
            DateTime now = DateTime.Now;
            Submit(new Entry { Root = Volatile.Read(ref root), Time = now, Category = "Camera",
                Name = "CameraLog_" + now.ToString("yyyyMMdd") + ".txt",
                Text = $"{now:yyyy-MM-dd HH:mm:ss.fff} | {operation} | status={status} | {detail}" });
        }

        public static void Model(string message)
        {
            DateTime now = DateTime.Now;
            Submit(new Entry { Root = Volatile.Read(ref root), Time = now, Category = "Model",
                Name = "dl_init_debug_" + now.ToString("yyyyMMdd") + ".log",
                Text = $"{now:yyyy-MM-dd HH:mm:ss.fff} {message}" });
        }

        public static bool SaveSnapshot(string path, string json)
        {
            // Capture the target along with the serialized snapshot so path changes cannot redirect it.
            return Submit(new Entry { Root = Path.GetDirectoryName(path), Name = Path.GetFileName(path),
                Time = DateTime.Now, Text = json, Replace = true });
        }

        public static bool AppendFile(string path, string text)
        {
            return Submit(new Entry { Root = Path.GetDirectoryName(path), Category = "",
                Name = Path.GetFileName(path), Time = DateTime.Now, Text = text });
        }

        private static void Append(Entry entry, string category, string name, string text)
        {
            string directory = Path.Combine(entry.Root, category);
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, name), text + Environment.NewLine);
        }

        private static void WriteOperation(Entry entry, int repeats = 0, DateTime? lastSeen = null)
        {
            OperLog op = entry.Operation.Value;
            string detail = op.strFailReason;
            if (repeats > 0) detail += $" | 重复{repeats}次，最后发生={lastSeen:yyyy-MM-dd HH:mm:ss.fff}";
            string status = StatusText(op.iEventStatus);
            string line = $"{entry.Time:yyyy-MM-dd HH:mm:ss.fff} | {op.strOperEvent} | {status} | {detail}";
            // A failure to write one category must not prevent trying the other category.
            try { Append(entry, "Oper", "Oper_" + entry.Time.ToString("yyyyMMdd") + ".txt",
                line + $" | status={op.iEventStatus} | user={op.strUserName}"); }
            catch { Interlocked.Increment(ref dropped); }
            if (status != "执行正常")
            {
                try { Append(entry, "Error", "ErrorLog_" + entry.Time.ToString("yyyyMMdd") + ".txt", line); }
                catch { Interlocked.Increment(ref dropped); }
            }
        }

        private static void Process(Entry entry)
        {
            if (entry.Replace)
            {
                Directory.CreateDirectory(entry.Root);
                string path = Path.Combine(entry.Root, entry.Name);
                string temp = path + ".tmp";
                File.WriteAllText(temp, entry.Text);
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
                return;
            }
            if (!entry.Operation.HasValue)
            {
                Append(entry, entry.Category, entry.Name, entry.Text);
                return;
            }
            OperLog op = entry.Operation.Value;
            if (StatusText(op.iEventStatus) == "执行正常") { WriteOperation(entry); return; }
            // Remove only known timestamp prefixes, never device error codes or frame payloads.
            string reason = Regex.Replace(op.strFailReason ?? "", @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\s+", "");
            string key = entry.Root + "|" + entry.Time.ToString("yyyyMMdd") + "|" +
                op.strOperEvent + "|" + op.iEventStatus + "|" + reason;
            RepeatedError previous;
            if (errors.TryGetValue(key, out previous))
            {
                previous.Count++;
                previous.LastSeen = entry.Time;
                return;
            }
            // Bound both the record queue and distinct error keys.
            if (errors.Count >= Capacity) FlushErrors(DateTime.Now, true);
            errors[key] = new RepeatedError { First = entry, LastWritten = DateTime.Now, LastSeen = entry.Time };
            WriteOperation(entry);
        }

        private static void FlushErrors(DateTime now, bool force)
        {
            var remove = new List<string>();
            foreach (var pair in errors)
            {
                RepeatedError error = pair.Value;
                if (!force && (now - error.LastWritten).TotalSeconds < 60) continue;
                if (error.Count > 0)
                {
                    Entry summary = new Entry { Root = error.First.Root, Time = error.LastSeen,
                        Operation = error.First.Operation };
                    WriteOperation(summary, error.Count, error.LastSeen);
                }
                remove.Add(pair.Key);
            }
            foreach (string key in remove) errors.Remove(key);
        }

        private static void Clean(string directory, DateTime now)
        {
            DateTime day;
            if (cleaned.TryGetValue(directory, out day) && day == now.Date) return;
            if (cleaned.Count >= 32) cleaned.Clear();
            cleaned[directory] = now.Date;
            string fullRoot = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(Path.Combine(fullRoot, "Runtime"));
            foreach (string category in new[] { "Oper", "Error", "Camera", "Model" })
            {
                string folder = Path.GetFullPath(Path.Combine(fullRoot, category));
                if (!folder.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)) continue;
                Directory.CreateDirectory(folder);
                foreach (string file in Directory.EnumerateFiles(folder))
                {
                    string name = Path.GetFileName(file);
                    string prefix = category == "Oper" ? "Oper_" : category == "Error" ? "ErrorLog_" :
                        category == "Camera" ? "CameraLog_" : "dl_init_debug_";
                    string suffix = category == "Model" ? ".log" : ".txt";
                    DateTime date;
                    if (name.Length != prefix.Length + 8 + suffix.Length ||
                        !name.StartsWith(prefix, StringComparison.Ordinal) || !name.EndsWith(suffix, StringComparison.Ordinal) ||
                        !DateTime.TryParseExact(name.Substring(prefix.Length, 8), "yyyyMMdd", CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out date) || date >= now.Date.AddDays(-29)) continue;
                    try { File.Delete(file); }
                    catch { Interlocked.Increment(ref dropped); }
                }
            }
        }

        private static void WriteLoop()
        {
            while (true)
            {
                Entry entry;
                int batch = 0;
                while (batch++ < 128 && queue.TryDequeue(out entry))
                {
                    Interlocked.Decrement(ref queued);
                    try { Process(entry); }
                    catch { Interlocked.Increment(ref dropped); }
                }
                try { FlushErrors(DateTime.Now, Volatile.Read(ref stopping) != 0); }
                catch { Interlocked.Increment(ref dropped); }
                string directory = Volatile.Read(ref root);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    try { Clean(directory, DateTime.Now); }
                    catch { Interlocked.Increment(ref dropped); }
                }
                if (Volatile.Read(ref stopping) != 0 && queue.IsEmpty) return;
                if (queue.IsEmpty) wake.WaitOne(1000);
            }
        }

        // Only a background shutdown continuation may wait for the writer.
        public static bool Shutdown(int milliseconds)
        {
            Interlocked.Exchange(ref stopping, 1);
            wake.Set();
            return writer.Join(milliseconds);
        }
    }
}
