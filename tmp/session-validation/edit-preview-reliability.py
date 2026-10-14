from pathlib import Path
p = Path('VisionControl/VisionControlExtention.cs')
raw = p.read_bytes()
bom = raw.startswith(b'\xef\xbb\xbf')
s = raw.decode('utf-8-sig').replace('\r\n', '\n')
def replace_section(begin, end, value):
    global s
    a = s.index(begin)
    b = s.index(end, a)
    s = s[:a] + value + s[b:]

s = s.replace('        private Thread[] _previewThreads', '''        private readonly object _previewLifecycleSync = new object();
        private int[] _previewCameraIndices = { -1, -1 };
        private const int PreviewJoinTimeoutMs = 2000;
        private Thread[] _previewThreads''', 1)
# 保留原有窗口和镜像准备逻辑；在任何缓冲区操作之前检查旧线程。
a = s.index('            // 根据配置设置镜像（在开始预览前）', s.index('public bool previewOnlive'))
b = s.index('            _previewUiPending[wsInd] = 0;', a)
setup = s[a:b].replace('            if (_previewRunning[wsInd])\n                return true;\n', '')
replace_section('        public bool previewOnlive(bool bOpen, int wsInd)', '        private void AdjustPictureBoxSize', '''        public bool StopPreviewForCamera(int cameraIndex)
        {
            lock (_previewLifecycleSync)
            {
                bool stopped = true;
                for (int job = 0; job < maxCameraNum; job++)
                    if (_previewCameraIndices[job] == cameraIndex)
                        stopped &= previewOnlive(false, job);
                return stopped;
            }
        }

        public bool previewOnlive(bool bOpen, int wsInd)
        {
            if (wsInd < 0 || wsInd >= maxCameraNum) return false;
            lock (_previewLifecycleSync)
            {
                int iCamIdx = bOpen ? cup.p_CD.camInd[wsInd] : _previewCameraIndices[wsInd];
                Thread previous = _previewThreads[wsInd];
                if (!bOpen)
                {
                    System.Threading.Volatile.Write(ref _previewRunning[wsInd], false);
                    if (previous != null && previous.IsAlive && !previous.Join(PreviewJoinTimeoutMs))
                    {
                        OnPreviewStatus(wsInd, iCamIdx, -1,
                            $"Job{wsInd}, Cam{iCamIdx}: 预览停止超时，ThreadId={previous.ManagedThreadId}，保留线程引用和缓冲区，拒绝重启/重连");
                        return false;
                    }
                    _previewThreads[wsInd] = null;
                    _previewThreadStarted[wsInd] = false;
                    _previewCameraIndices[wsInd] = -1;
                    _previewUiPending[wsInd] = 0;
                    if (iCamIdx >= 0) acqFIFO.ReleasePreview(iCamIdx, wsInd);
                    if (_flipBuffers[wsInd] != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(_flipBuffers[wsInd]);
                        _flipBuffers[wsInd] = IntPtr.Zero;
                        _flipBufferSizes[wsInd] = 0;
                    }
                    if (_previewBuffers[wsInd] != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(_previewBuffers[wsInd]);
                        _previewBuffers[wsInd] = IntPtr.Zero;
                        _previewBufferSizes[wsInd] = 0;
                    }
                    Control displayControl = GetPreviewControl(wsInd);
                    if (displayControl is PictureBox pb)
                    {
                        Image oldImage = pb.Image;
                        pb.Image = null;
                        oldImage?.Dispose();
                    }
                    else if (displayControl is CogRecordDisplay cogDisp) cogDisp.Image = null;
                    return true;
                }

                if (previous != null && previous.IsAlive)
                {
                    if (System.Threading.Volatile.Read(ref _previewRunning[wsInd]) && _previewCameraIndices[wsInd] == iCamIdx)
                        return true;
                    OnPreviewStatus(wsInd, iCamIdx, -1, $"Job{wsInd}, Cam{iCamIdx}: 旧预览线程尚未退出，拒绝创建第二个线程");
                    return false;
                }
                if (iCamIdx < 0 || acqFIFO._HikCam == null || iCamIdx >= acqFIFO._HikCam.Length ||
                    cup.p_Cam == null || iCamIdx >= cup.p_Cam.Length)
                {
                    OnPreviewStatus(wsInd, iCamIdx, -1, $"Job{wsInd}, Cam{iCamIdx}: 相机索引无效");
                    return false;
                }
                MyCamera camera = acqFIFO._HikCam[iCamIdx];
                string serial = cup.p_Cam[iCamIdx].strCamName;
                if (camera == null || !acqFIFO.IsCameraGrabbing(iCamIdx))
                {
                    OnPreviewStatus(wsInd, iCamIdx, -1, $"Job{wsInd}, Cam{iCamIdx}, Serial={serial}: 采集未就绪或设备已断开，未启动预览取帧");
                    return false;
                }
                for (int job = 0; job < maxCameraNum; job++)
                    if (job != wsInd && _previewCameraIndices[job] == iCamIdx &&
                        _previewThreads[job] != null && _previewThreads[job].IsAlive)
                    {
                        OnPreviewStatus(wsInd, iCamIdx, -1, $"Job{wsInd}, Cam{iCamIdx}: 相机已有Job{job}预览线程，拒绝重复取帧");
                        return false;
                    }
''' + setup + '''
                if (!acqFIFO.TryReservePreview(iCamIdx, wsInd))
                {
                    OnPreviewStatus(wsInd, iCamIdx, -1, $"Job{wsInd}, Cam{iCamIdx}: 相机取帧已被其他预览占用");
                    return false;
                }
                try
                {
                    _previewUiPending[wsInd] = 0;
                    _previewLastUiTick[wsInd] = 0;
                    _previewFirstFrameReceived[wsInd] = false;
                    _previewGetBufferFailureLogged[wsInd] = false;
                    _previewDisplayFailureLogged[wsInd] = false;
                    _previewLastGetBufferError[wsInd] = MyCamera.MV_OK;
                    _previewCameraIndices[wsInd] = iCamIdx;
                    _previewFlipTypes[wsInd] = cup.p_Cam[iCamIdx].iFlipType;
                    System.Threading.Volatile.Write(ref _previewRunning[wsInd], true);
                    _previewThreads[wsInd] = new Thread(() => PreviewThreadProc(camera, wsInd, iCamIdx, serial));
                    _previewThreads[wsInd].IsBackground = true;
                    _previewThreads[wsInd].Start();
                    _previewThreadStarted[wsInd] = true;
                }
                catch (Exception ex)
                {
                    System.Threading.Volatile.Write(ref _previewRunning[wsInd], false);
                    acqFIFO.ReleasePreview(iCamIdx, wsInd);
                    OnPreviewStatus(wsInd, iCamIdx, -1, $"Job{wsInd}, Cam{iCamIdx}, Serial={serial}: 预览启动异常: {ex}");
                    return false;
                }
                OnPreviewStatus(wsInd, iCamIdx, 2, $"Job{wsInd}, Cam{iCamIdx}, Serial={serial}: Preview线程已启动，ThreadId={_previewThreads[wsInd].ManagedThreadId}，等待首帧");
                return true;
            }
        }

''')
replace_section('        public void previewOnliveAll(bool bOpen)', '        // 用于软件翻转/旋转', '''        public void previewOnliveAll(bool bOpen)
        {
            for (int job = 0; job < maxCameraNum; job++)
                if (!bOpen || cup.p_CD.bListProcess[job]) previewOnlive(bOpen, job);
        }

''')
# 原有翻转和 SDK 显示逻辑原样保留，放进取得帧后的 try/finally。
a = s.index('                    uint frameLen = stFrameInfo.stFrameInfo.nFrameLen;', s.index('private void PreviewThreadProc'))
b = s.index('                    camera.MV_CC_FreeImageBuffer_NET(ref stFrameInfo);', a)
display = s[a:b].replace('                        if (displayRet == MyCamera.MV_OK)\n                        {', '''                        if (displayRet == MyCamera.MV_OK)
                        {
                            displayedFrames++;
                            lastDisplayTime = DateTime.Now;''')
replace_section('        private void PreviewThreadProc(MyCamera camera, int wsInd)', '        private Bitmap CreatePreviewBitmap', '''        private string ReadPreviewAcquisitionState(MyCamera camera)
        {
            List<string> values = new List<string>();
            foreach (string node in new[] { "AcquisitionMode", "TriggerMode", "TriggerSource" })
            {
                MyCamera.MVCC_ENUMVALUE value = new MyCamera.MVCC_ENUMVALUE();
                int ret = camera.MV_CC_GetEnumValue_NET(node, ref value);
                values.Add(ret == MyCamera.MV_OK ? $"{node}={value.nCurValue}" : $"{node}=读取失败(0x{unchecked((uint)ret):X8})");
            }
            return string.Join(", ", values);
        }

        private void PreviewThreadProc(MyCamera camera, int wsInd, int iCamIdx, string serial)
        {
            MyCamera.MV_FRAME_OUT stFrameInfo = new MyCamera.MV_FRAME_OUT();
            MyCamera.MV_DISPLAY_FRAME_INFO stDisplayInfo = new MyCamera.MV_DISPLAY_FRAME_INFO();
            long successfulFrames = 0, displayedFrames = 0, consecutiveNoData = 0, totalNoData = 0;
            DateTime? firstFrameTime = null, lastFrameTime = null, lastDisplayTime = null;
            DateTime lastReport = DateTime.Now;
            string exitReason = "停止请求";
            string identity = $"Job{wsInd}, Cam{iCamIdx}, Serial={serial}, ThreadId={Thread.CurrentThread.ManagedThreadId}";
            try
            {
                OnPreviewStatus(wsInd, iCamIdx, 2, $"{identity}: 预览采集状态; {ReadPreviewAcquisitionState(camera)}");
                while (System.Threading.Volatile.Read(ref _previewRunning[wsInd]))
                {
                    bool stopForFreeFailure = false;
                    lock (acqFIFO.GetCameraFrameLock(iCamIdx))
                    {
                        if (!System.Threading.Volatile.Read(ref _previewRunning[wsInd])) break;
                        int nRet = camera.MV_CC_GetImageBuffer_NET(ref stFrameInfo, 1000);
                        if (nRet == MyCamera.MV_OK)
                        {
                            // 一旦 Get 成功，即使翻转、显示或日志回调异常，也必须归还该帧。
                            try
                            {
                                successfulFrames++;
                                consecutiveNoData = 0;
                                lastFrameTime = DateTime.Now;
                                if (!firstFrameTime.HasValue)
                                {
                                    firstFrameTime = lastFrameTime;
                                    OnPreviewStatus(wsInd, iCamIdx, 2, $"{identity}: 预览首帧取帧成功时间={firstFrameTime:yyyy-MM-dd HH:mm:ss.fff}");
                                }
''' + display + '''
                            }
                            finally
                            {
                                int freeRet = camera.MV_CC_FreeImageBuffer_NET(ref stFrameInfo);
                                if (freeRet != MyCamera.MV_OK)
                                {
                                    exitReason = $"FreeImageBuffer失败，SDK=0x{unchecked((uint)freeRet):X8}";
                                    stopForFreeFailure = true;
                                    OnPreviewStatus(wsInd, iCamIdx, -1, $"{identity}: {exitReason}");
                                }
                            }
                        }
                        else
                        {
                            if (nRet == MyCamera.MV_E_NODATA) { consecutiveNoData++; totalNoData++; }
                            else consecutiveNoData = 0;
                            if (!camera.MV_CC_IsDeviceConnected_NET())
                            {
                                exitReason = $"设备已断开，GetImageBuffer SDK=0x{unchecked((uint)nRet):X8}";
                                break;
                            }
                            if (!_previewGetBufferFailureLogged[wsInd] || _previewLastGetBufferError[wsInd] != nRet)
                            {
                                _previewGetBufferFailureLogged[wsInd] = true;
                                _previewLastGetBufferError[wsInd] = nRet;
                                OnPreviewStatus(wsInd, iCamIdx, -1,
                                    $"{identity}: GetImageBuffer失败，SDK=0x{unchecked((uint)nRet):X8}, timeoutMs=1000, 线程仍在运行; {ReadPreviewAcquisitionState(camera)}");
                            }
                            Thread.Sleep(5);
                        }
                    }
                    if (stopForFreeFailure) break;
                    if ((DateTime.Now - lastReport).TotalSeconds >= 5)
                    {
                        lastReport = DateTime.Now;
                        OnPreviewStatus(wsInd, iCamIdx, 2,
                            $"{identity}: 预览统计 running=True, firstFrame={firstFrameTime:yyyy-MM-dd HH:mm:ss.fff}, successfulFrames={successfulFrames}, subsequentFrames={Math.Max(0, successfulFrames - 1)}, displayedFrames={displayedFrames}, consecutiveNoData={consecutiveNoData}, totalNoData={totalNoData}, lastFrame={lastFrameTime:yyyy-MM-dd HH:mm:ss.fff}, lastDisplay={lastDisplayTime:yyyy-MM-dd HH:mm:ss.fff}");
                    }
                }
            }
            catch (Exception ex)
            {
                exitReason = $"预览线程异常: {ex}";
            }
            finally
            {
                System.Threading.Volatile.Write(ref _previewRunning[wsInd], false);
                acqFIFO.ReleasePreview(iCamIdx, wsInd);
                OnPreviewStatus(wsInd, iCamIdx, exitReason == "停止请求" ? 2 : -1,
                    $"{identity}: 预览线程退出 reason={exitReason}, firstFrame={firstFrameTime:yyyy-MM-dd HH:mm:ss.fff}, successfulFrames={successfulFrames}, subsequentFrames={Math.Max(0, successfulFrames - 1)}, displayedFrames={displayedFrames}, consecutiveNoData={consecutiveNoData}, totalNoData={totalNoData}, lastFrame={lastFrameTime:yyyy-MM-dd HH:mm:ss.fff}, lastDisplay={lastDisplayTime:yyyy-MM-dd HH:mm:ss.fff}");
            }
        }

''')
p.write_bytes((b'\xef\xbb\xbf' if bom else b'') + s.replace('\n', '\r\n').encode('utf-8'))
