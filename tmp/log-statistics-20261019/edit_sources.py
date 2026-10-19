from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
def read(name):
    return (ROOT / name).read_text(encoding='utf-8-sig')
def save(name, text):
    (ROOT / name).write_text(text, encoding='utf-8-sig', newline='\r\n')
def once(text, old, new):
    assert text.count(old) == 1, (old[:120], text.count(old))
    return text.replace(old, new, 1)
def method(text, start, end, replacement):
    a, b = text.index(start), text.index(end, text.index(start))
    return text[:a] + replacement + text[b:]

p = 'CommonCls/CDL.cs'
s = read(p)
s = method(s, '        private static void WriteInitDLLog(string message)', '        private static string FormatFileList', '''        private static void WriteInitDLLog(string message)
        {
            RuntimeLog.Model(message);
        }

''')
for kind, arg in [('Gray', 'grayData'), ('Color', 'bgrData')]:
    signature = f'        public bool det_infer_img_Byte{kind}(byte[] {arg}, int width, int height, float fThreshold, float fThresholdPS, ref DLResult dlR)'
    expanded = signature + '''
        {
            bool inferenceSucceeded;
            return det_infer_img_Byte''' + kind + f'''({arg}, width, height, fThreshold, fThresholdPS, ref dlR, out inferenceSucceeded);
        }}

''' + signature[:-1] + ', out bool inferenceSucceeded)'
    s = once(s, signature, expanded)
    start = s.index(signature[:-1] + ', out bool inferenceSucceeded)')
    end = s.index('        //', start + len(signature)) if kind == 'Gray' else s.index('        //销毁深度学习模型', start)
    # Gray's next method is preceded by a field, not a comment within its body.
    if kind == 'Gray': end = s.index('        int[] bInfering', start)
    block = s[start:end]
    block = once(block, '            float[] resultlist = new float[600];', '            inferenceSucceeded = false;\n            float[] resultlist = new float[600];')
    block = once(block, '            IntPtr results = FloatToIntptr(resultlist);', '            GCHandle resultHandle = GCHandle.Alloc(resultlist, GCHandleType.Pinned);\n            IntPtr results = resultHandle.AddrOfPinnedObject();')
    block = once(block, '            try\n            {', f'''            try
            {{
                if (!has_model_init || {arg} == null || width <= 0 || height <= 0 ||
                    {arg}.Length < checked(width * height * {1 if kind == 'Gray' else 3}))
                    throw new InvalidOperationException("推理模型或本轮图像无效");''')
    call = 'Det_ModelPredict(bgrData, width, height, 3, results, boxesInfo, ref labellist[0]);'
    block = once(block, call, '''int returnCode = Det_ModelPredict(bgrData, width, height, 3, results, boxesInfo, ref labellist[0]);
                    if (returnCode != 0) throw new InvalidOperationException("Det_ModelPredict returnCode=" + returnCode);
                    if (boxesInfo[0] < 0 || boxesInfo[0] > resultlist.Length / 6)
                        throw new InvalidOperationException("推理框数量无效: " + boxesInfo[0]);''')
    block = once(block, '            }\n            catch (Exception e)', '                inferenceSucceeded = true;\n            }\n            catch (Exception e)')
    import re
    block = re.sub(r'                Console.WriteLine\([^\n]+\);', '                RuntimeLog.Model("[Infer] failed: " + e);', block)
    block = once(block, '            return bSure;', '            finally\n            {\n                if (resultHandle.IsAllocated) resultHandle.Free();\n            }\n            return bSure;')
    s = s[:start] + block + s[end:]
save(p, s)

p = 'VisionControl/VisionControlExtention.cs'
s = read(p)
s = once(s, '        int iDeepInferTime = 0;', '''        private readonly DetectionStatistics detectionStatistics = new DetectionStatistics();
        public event Action<int> DetectionStatisticsChanged;
        public DetStat[] GetDetectionStatistics() => detectionStatistics.Snapshot();
        public long GetDetectionRunToken(int jobIndex) => detectionStatistics.Token(jobIndex);
        public bool ResetDetectionStatistics(int jobIndex) => detectionStatistics.Reset(jobIndex);
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
        }''')
s = once(s, '            iDeepInferTime++; ', '')
for name in ['DLInfer', 'DLInferOffLine']:
    start = s.index('        private void ' + name + '(int jobIndex)')
    end = s.index('\n        }', start) + len('\n        }')
    block = s[start:end]
    block = once(block, '            int cameraIndex = cup.p_CD.camInd[jobIndex];', '            int cameraIndex = cup.p_CD.camInd[jobIndex];\n            bool inferenceSucceeded = false;')
    block = block.replace('bSureCheck[0] =', 'bSureCheck[jobIndex] =')
    block = block.replace('ref DL_result[jobIndex]);', 'ref DL_result[jobIndex], out inferenceSucceeded);')
    block = once(block, '            if (DL_result[jobIndex].AL_Rect.Count > 0)', '''            if (!inferenceSucceeded)
                throw new InvalidOperationException($"Job{jobIndex}本轮深度推理失败，详见Model日志");

            if (DL_result[jobIndex].AL_Rect.Count > 0)''')
    s = s[:start] + block + s[end:]
s = once(s, '            bool vppCompletionStarted = false;', '            bool vppCompletionStarted = false;\n            bool runStarted = false;\n            long statisticsToken = 0;')
s = once(s, '                    //2026年4月12日 修改了采集图像函数', '''                    if (cup.p_VPPRTP.bVisionInfer || cup.p_VPPRTP.bDeepInfer)
                        statisticsToken = detectionStatistics.Begin(jobIndex, !SkipImageUpdateForCurrentRun);

                    //2026年4月12日 修改了采集图像函数''')
s = once(s, '                            pmaRes[jobIndex].bLocated = true;\n                            // ★★★', '''                            pmaRes[jobIndex].bLocated = true;
                            CompleteDetectionStatistics(jobIndex, statisticsToken, true, pmaRes[jobIndex].bOk);
                            SetJobBusy(jobIndex, false);
                            // ★★★''')
# Cover failures in the common entry, irrespective of the calling manual/IO/continuous wrapper.
start = s.index('        public bool RunOneTime(int jobIndex)')
end = s.index('        public bool RunOneTimeSpliteAll()', start)
block = s[start:end]
block = once(block, '                }\n                 \n            }', '                }\n                runStarted = true;\n            }')
block = once(block, '            return true; ', '''            finally
            {
                if (!runStarted && statisticsToken > 0)
                {
                    CompleteDetectionStatistics(jobIndex, statisticsToken, false, false);
                    if (!vppCompletionStarted) SetJobBusy(jobIndex, false);
                }
            }
            return true; ''')
s = s[:start] + block + s[end:]
# Replace only the business count fields; SDK acquisition/throughput fields remain available.
a = s.index('            if (cup.p_VPPRTP.bDeepInfer)', s.index('        public string[] RetStatisticsForJob'))
b = s.index('                strStatus[6] = j.TotalAcquisitions', a)
s = s[:a] + '''            DetStat current = detectionStatistics.Snapshot()[i];
            strStatus[0] = current.iTotal.ToString();
            strStatus[1] = current.iOKNum.ToString();
            strStatus[2] = current.iOKPer.ToString("0.0");
            strStatus[3] = current.iNGNum.ToString();
            strStatus[4] = current.iWarningNum.ToString();
            strStatus[5] = current.iErrNum.ToString();
            for (int field = 6; field < strStatus.Length; field++) strStatus[field] = "0";
            if (cup.p_VPPRTP.bVisionInfer)
            {
                j = mJM.Job(i);
''' + s[b:]
s = once(s, '(DateTime.Now - lastReport).TotalSeconds >= 5', '(DateTime.Now - lastReport).TotalSeconds >= 10')
save(p, s)

p = 'VisionControl/VisionControl.cs'
s = read(p)
s = once(s, '''        private void TryCompleteVppJob(int jobIndex)
        {
            lock''', '''        private void TryCompleteVppJob(int jobIndex)
        {
            long statisticsToken = GetDetectionRunToken(jobIndex);
            lock''')
s = once(s, '                JobResultReady?.Invoke(jobIndex);', '''                CompleteDetectionStatistics(jobIndex, statisticsToken,
                    IsVppJobResultValid(jobIndex), pmaRes[jobIndex].bOk);
                JobResultReady?.Invoke(jobIndex);''')
s = once(s, '                strErrInfo[jobIndex] = $"jobIndex={jobIndex} 本轮结果显示/通知失败: {ex}";', '''                CompleteDetectionStatistics(jobIndex, statisticsToken, false, false);
                strErrInfo[jobIndex] = $"jobIndex={jobIndex} 本轮结果显示/通知失败: {ex}";''')
save(p, s)
