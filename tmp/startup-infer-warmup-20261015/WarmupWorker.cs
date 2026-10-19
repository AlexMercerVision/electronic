using CommonCls;
using System;
using System.Diagnostics;
using System.Threading;

public static class WarmupWorker
{
    public static long[] Run(CDL model, int width, int height)
    {
        bool success = false;
        long elapsed = 0;
        Exception error = null;
        Thread thread = new Thread(() =>
        {
            try
            {
                Stopwatch watch = Stopwatch.StartNew();
                success = model.det_infer_img(width, height);
                elapsed = watch.ElapsedMilliseconds;
            }
            catch (Exception ex) { error = ex; }
        });
        thread.Start();
        thread.Join();
        if (error != null) throw new Exception("Worker inference failed", error);
        return new long[] { success ? 1 : 0, elapsed };
    }
}
