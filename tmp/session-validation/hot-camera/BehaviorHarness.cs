
using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
class Image { public void Dispose() {} }
class Control { public IntPtr Handle = (IntPtr)1; public bool IsHandleCreated = true; public void CreateControl() {} }
class PictureBox:Control { public Image Image; }
class CogRecordDisplay:Control { public Image Image; }
struct CameraParam { public int iMonoType; public float dGain,iExposeTime,dGamma; public uint iSharp,iWidth,iHeight; public uint[] iROI; public string strCamName; public int iFlipType; }
class CD { public int[] camInd={0,1}; public int triggleCam=1,triggleMode=1; public bool bTriggle; }
class Param { public CameraParam[] p_Cam = {
 new CameraParam {strCamName="DA8696070",iROI=new uint[]{0,0,3072,2048},iWidth=3072,iHeight=2048,iMonoType=1,iSharp=7,dGamma=0.55f},
 new CameraParam {strCamName="DA8696075",iROI=new uint[]{0,0,3072,2048},iWidth=3072,iHeight=2048,iMonoType=1,iSharp=7,dGamma=3}}; public CD p_CD=new CD(); }
class MyCamera {
 public const int MV_OK=0,MV_E_NODATA=unchecked((int)0x80000007);
 public enum MV_CAM_TRIGGER_MODE {MV_TRIGGER_MODE_ON=1,MV_TRIGGER_MODE_OFF=0}
 public enum MV_CAM_TRIGGER_SOURCE {MV_TRIGGER_SOURCE_SOFTWARE=7,MV_TRIGGER_SOURCE_LINE0=0}
 public enum MV_IMG_FLIP_TYPE {MV_FLIP_HORIZONTAL,MV_FLIP_VERTICAL}
 public struct MVCC_ENUMVALUE {public uint nCurValue;}
 public struct MVCC_INTVALUE {public uint nMin,nMax;}
 public struct MV_FRAME_OUT_INFO_EX {public uint nFrameLen,nWidth,nHeight;public int enPixelType;}
 public struct MV_FRAME_OUT {public MV_FRAME_OUT_INFO_EX stFrameInfo; public IntPtr pBufAddr;}
 public struct MV_DISPLAY_FRAME_INFO {public IntPtr hWnd,pData;public uint nDataLen,nWidth,nHeight;public int enPixelType;}
 public struct MV_CC_FLIP_IMAGE_PARAM {public int enPixelType;public uint nWidth,nHeight,nSrcDataLen,nDstBufLen,nDstBufSize;public IntPtr pSrcData,pDstBuf;public MV_IMG_FLIP_TYPE enFlipType;public uint[] nRes;}
 public Func<int> Next; public Action OnGet; public int GetCalls,FreeCalls,FreeRet,RangeRet,SetCalls; public bool DisplayThrows,FreeThrows; public bool Connected=true;
 public int MV_CC_GetImageBuffer_NET(ref MV_FRAME_OUT f,int timeout) {GetCalls++;OnGet?.Invoke(); f.stFrameInfo.nFrameLen=1;f.stFrameInfo.nWidth=1;f.stFrameInfo.nHeight=1;return Next==null?MV_E_NODATA:Next();}
 public int MV_CC_FreeImageBuffer_NET(ref MV_FRAME_OUT f) {FreeCalls++;if(FreeThrows)throw new InvalidOperationException("free-test");return FreeRet;}
 public int MV_CC_DisplayOneFrame_NET(ref MV_DISPLAY_FRAME_INFO f) {if(DisplayThrows)throw new InvalidOperationException("display-test");return 0;}
 public int MV_CC_FlipImage_NET(ref MV_CC_FLIP_IMAGE_PARAM f) {return 0;}
 public bool MV_CC_IsDeviceConnected_NET() {return Connected;}
 public int MV_CC_GetEnumValue_NET(string node,ref MVCC_ENUMVALUE v) {v.nCurValue=node=="AcquisitionMode"?2u:0u;return 0;}
 public int MV_CC_GetSharpness_NET(ref MVCC_INTVALUE v) {v.nMin=0;v.nMax=10;return RangeRet;}
 public int MV_CC_SetSharpness_NET(uint value) {SetCalls++;return 0;}
}
class FIFO {
 public MyCamera[] _HikCam={new MyCamera(),new MyCamera()}; public object[] Locks={new object(),new object()}; public int[] Owners=new int[2];
 public bool IsCameraGrabbing(int i){return true;}
 public object GetCameraFrameLock(int i){return Locks[i];}
 public uint GetPayloadSize(int i){return 1;}
 public bool TryReservePreview(int i,int job){lock(Locks[i]){if(Owners[i]!=0)return false;Owners[i]=job+1;return true;}}
 public void ReleasePreview(int i,int job){lock(Locks[i]){if(Owners[i]==job+1)Owners[i]=0;}}
}
class PreviewHarness {
 const int maxCameraNum=2;
 public Param cup=new Param(); public FIFO acqFIFO=new FIFO();
 Control cogRDMainUser=new Control(),cogRDSub1=new Control();
 IntPtr[] _flipBuffers=new IntPtr[2]; uint[] _flipBufferSizes=new uint[2]; int[] _previewFlipTypes=new int[2];
 public List<string> Logs=new List<string>();
 Control GetPreviewControl(int i){return i==0?cogRDMainUser:cogRDSub1;}
 void AdjustPictureBoxSize(Control c,int w,int h,int i){}
 void OnPreviewStatus(int job,int cam,int status,string message){lock(Logs)Logs.Add(message);}
        private readonly object _previewLifecycleSync = new object();
        private int[] _previewCameraIndices = { -1, -1 };
        private const int PreviewJoinTimeoutMs = 2000;
        private Thread[] _previewThreads = new Thread[maxCameraNum];
        private bool[] _previewRunning = new bool[maxCameraNum];
        private IntPtr[] _previewBuffers = new IntPtr[maxCameraNum];
        private uint[] _previewBufferSizes = new uint[maxCameraNum];
        private MyCamera.MV_FRAME_OUT_INFO_EX[] _lastFrameInfos = new MyCamera.MV_FRAME_OUT_INFO_EX[maxCameraNum];
        private object[] _previewLocks = new object[maxCameraNum];
        private IntPtr[] _previewHwnds = new IntPtr[maxCameraNum];
        private const int PreviewUiIntervalMs = 100;
        private int[] _previewUiPending = new int[maxCameraNum];
        private int[] _previewLastUiTick = new int[maxCameraNum];
        private bool[] _previewThreadStarted = new bool[maxCameraNum];
        private bool[] _previewFirstFrameReceived = new bool[maxCameraNum];
        private bool[] _previewGetBufferFailureLogged = new bool[maxCameraNum];
        private bool[] _previewDisplayFailureLogged = new bool[maxCameraNum];
        private int[] _previewLastGetBufferError = new int[maxCameraNum];
        public bool StopPreviewForCamera(int cameraIndex)
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
            // 根据配置设置镜像（在开始预览前）
            int flipType = cup.p_Cam[iCamIdx].iFlipType;
            if (flipType != 0)
            {
                uint payloadSize = acqFIFO.GetPayloadSize(iCamIdx);
                if (_flipBuffers[wsInd] == IntPtr.Zero || _flipBufferSizes[wsInd] < payloadSize)
                {
                    if (_flipBuffers[wsInd] != IntPtr.Zero)
                        Marshal.FreeHGlobal(_flipBuffers[wsInd]);
                    _flipBuffers[wsInd] = Marshal.AllocHGlobal((int)payloadSize);
                    _flipBufferSizes[wsInd] = payloadSize;
                }
            }

            switch (wsInd)
            {
                case 0:
                    AdjustPictureBoxSize(cogRDMainUser, (int)cup.p_Cam[iCamIdx].iWidth, (int)cup.p_Cam[iCamIdx].iHeight, wsInd);
                    break;
                case 1:
                    AdjustPictureBoxSize(cogRDSub1, (int)cup.p_Cam[iCamIdx].iWidth, (int)cup.p_Cam[iCamIdx].iHeight, wsInd);
                    break;
            }

            Control targetControl = GetPreviewControl(wsInd);
            if (targetControl == null || !targetControl.IsHandleCreated)
                targetControl?.CreateControl();
            _previewHwnds[wsInd] = targetControl?.Handle ?? IntPtr.Zero;
            if (_previewHwnds[wsInd] == IntPtr.Zero)
            {
                OnPreviewStatus(wsInd, iCamIdx, -1, $"Job{wsInd}, Cam{iCamIdx}, Serial={serial}: 显示窗口句柄无效");
                return false;
            }


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

        private string ReadPreviewAcquisitionState(MyCamera camera)
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
                    uint frameLen = stFrameInfo.stFrameInfo.nFrameLen;
                    IntPtr displayPtr = stFrameInfo.pBufAddr;
                    uint displayLen = frameLen;
                    int flipType = _previewFlipTypes[wsInd];

                    if ((flipType == 1 || flipType == 2) && frameLen > 0)
                    {
                        if (_flipBuffers[wsInd] == IntPtr.Zero || _flipBufferSizes[wsInd] < frameLen)
                        {
                            if (_flipBuffers[wsInd] != IntPtr.Zero)
                                Marshal.FreeHGlobal(_flipBuffers[wsInd]);
                            _flipBuffers[wsInd] = Marshal.AllocHGlobal((int)frameLen);
                            _flipBufferSizes[wsInd] = frameLen;
                        }

                        MyCamera.MV_CC_FLIP_IMAGE_PARAM flipParam = new MyCamera.MV_CC_FLIP_IMAGE_PARAM();
                        flipParam.enPixelType = stFrameInfo.stFrameInfo.enPixelType;
                        flipParam.nWidth = stFrameInfo.stFrameInfo.nWidth;
                        flipParam.nHeight = stFrameInfo.stFrameInfo.nHeight;
                        flipParam.pSrcData = stFrameInfo.pBufAddr;
                        flipParam.nSrcDataLen = frameLen;
                        flipParam.pDstBuf = _flipBuffers[wsInd];
                        flipParam.nDstBufLen = frameLen;
                        flipParam.nDstBufSize = frameLen;
                        flipParam.enFlipType = flipType == 1
                            ? MyCamera.MV_IMG_FLIP_TYPE.MV_FLIP_HORIZONTAL
                            : MyCamera.MV_IMG_FLIP_TYPE.MV_FLIP_VERTICAL;
                        flipParam.nRes = new uint[8];

                        if (camera.MV_CC_FlipImage_NET(ref flipParam) == MyCamera.MV_OK)
                        {
                            displayPtr = _flipBuffers[wsInd];
                            if (flipParam.nDstBufLen > 0)
                                displayLen = flipParam.nDstBufLen;
                        }
                    }

                    if (_previewHwnds[wsInd] != IntPtr.Zero)
                    {
                        // 显示
                        stDisplayInfo.hWnd = _previewHwnds[wsInd];
                        stDisplayInfo.pData = displayPtr;
                        stDisplayInfo.nDataLen = displayLen;
                        stDisplayInfo.nWidth = stFrameInfo.stFrameInfo.nWidth;
                        stDisplayInfo.nHeight = stFrameInfo.stFrameInfo.nHeight;
                        stDisplayInfo.enPixelType = stFrameInfo.stFrameInfo.enPixelType;
                        int displayRet = camera.MV_CC_DisplayOneFrame_NET(ref stDisplayInfo);
                        if (displayRet == MyCamera.MV_OK)
                        {
                            displayedFrames++;
                            lastDisplayTime = DateTime.Now;
                            if (!_previewFirstFrameReceived[wsInd])
                            {
                                _previewFirstFrameReceived[wsInd] = true;
                                OnPreviewStatus(wsInd, iCamIdx, 0,
                                    $"Job{wsInd}, Cam{iCamIdx}, Serial={serial}: 已收到并显示首帧，PixelFormat={stFrameInfo.stFrameInfo.enPixelType}");
                            }
                        }
                        else if (!_previewDisplayFailureLogged[wsInd])
                        {
                            _previewDisplayFailureLogged[wsInd] = true;
                            OnPreviewStatus(wsInd, iCamIdx, -1,
                                $"Job{wsInd}, Cam{iCamIdx}, Serial={serial}: DisplayOneFrame失败，SDK=0x{unchecked((uint)displayRet):X8}");
                        }
                    }


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
                            if (nRet != MyCamera.MV_E_NODATA)
                            {
                                exitReason = $"GetImageBuffer错误，SDK=0x{unchecked((uint)nRet):X8}，停止预览，未重试";
                                break;
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


 public void Run(MyCamera camera){_previewRunning[0]=true;_previewHwnds[0]=(IntPtr)1;acqFIFO.TryReservePreview(0,0);PreviewThreadProc(camera,0,0,"DA8696070");}
 public void StopFlag(){Volatile.Write(ref _previewRunning[0],false);}
 public bool Has(string text){lock(Logs)return Logs.Exists(x=>x.Contains(text));}
 public void TimeoutTest(){
   var gate=new ManualResetEvent(false);var entered=new ManualResetEvent(false);
   var old=new Thread(()=>{entered.Set();gate.WaitOne();});_previewThreads[0]=old;_previewRunning[0]=true;_previewCameraIndices[0]=0;
   _flipBuffers[0]=Marshal.AllocHGlobal(4);var buffer=_flipBuffers[0];acqFIFO.TryReservePreview(0,0);old.Start();entered.WaitOne();
   Tests.Check(!previewOnlive(false,0),"join timeout returns failure");
   Tests.Check(ReferenceEquals(_previewThreads[0],old)&&_flipBuffers[0]==buffer&&acqFIFO.Owners[0]==1,"timeout preserves thread, buffer and ownership");
   Tests.Check(!previewOnlive(true,0)&&ReferenceEquals(_previewThreads[0],old),"timeout rejects second thread");
   Tests.Check(!StopPreviewForCamera(0),"reconnect gate rejects live old thread");
   gate.Set();old.Join();Tests.Check(previewOnlive(false,0)&&_previewThreads[0]==null&&_flipBuffers[0]==IntPtr.Zero,"cleanup only after old thread exit");
 }
 public void DuplicateTest(){
   cup.p_CD.camInd[1]=0;var gate=new ManualResetEvent(false);var entered=new ManualResetEvent(false);
   acqFIFO._HikCam[0].Next=()=>{entered.Set();gate.WaitOne();return MyCamera.MV_E_NODATA;};
   Tests.Check(previewOnlive(true,0),"first preview starts");entered.WaitOne();var first=_previewThreads[0];
   Tests.Check(previewOnlive(true,0)&&ReferenceEquals(first,_previewThreads[0]),"same job start is idempotent");
   Tests.Check(!previewOnlive(true,1)&&_previewThreads[1]==null,"same camera second job rejected");
   gate.Set();Tests.Check(previewOnlive(false,0),"running preview stops");
 }
}
class HikStub {
 public string Fail; public bool SetParam(float g,float e,ref string gs,ref string es,MyCamera c){return Fail!="ExposureTime";}
 public bool SetPixelFormat(ref int t,MyCamera c){t=0;return Fail!="PixelFormat";}
 public bool SetSharpness(ref uint v,MyCamera c){v=0;return Fail!="Sharpness";}
 public bool SetGamma(ref float v,MyCamera c){v=1;return Fail!="Gamma";}
 public bool SetResolution(ref uint w,ref uint h,MyCamera c){return Fail!="Resolution";}
 public bool SetIntValueChecked(MyCamera c,string n,uint v){return Fail!=n;}
 public bool SetEnumValueChecked(MyCamera c,string n,uint v){return Fail!=n;}
 public string GetConnectedCameraSerial(int i){return "DA8696070";}
}
class RestoreHarness {
 const int maxCameraNum=2;public Param cup=new Param(); bool chooseHIK=true; MyCamera[] camera2={new MyCamera(),new MyCamera()};public HikStub hIKVisionCamera=new HikStub();
 void LogCameraConnection(string op,bool ok,string text){} void WriteCameraDiagnostic(string op,int status,string text){}
 bool GetCameraRuntimeState(int i,out string state){state="test";return hIKVisionCamera.Fail!="ReadBack";}
        private bool SetCamParam(int iCamIndex)
        {
            string serial = cup.p_Cam != null && iCamIndex >= 0 && iCamIndex < cup.p_Cam.Length
                ? cup.p_Cam[iCamIndex].strCamName : string.Empty;
            string stage = "Validate";
            try
            {
                if (iCamIndex < 0 || iCamIndex >= maxCameraNum || cup.p_Cam == null ||
                    iCamIndex >= cup.p_Cam.Length || !chooseHIK || camera2 == null ||
                    iCamIndex >= camera2.Length || camera2[iCamIndex] == null)
                {
                    LogCameraConnection("参数恢复", false, $"cameraIndex={iCamIndex}, serial={serial}, node={stage}, 相机对象或索引无效，未调用SDK");
                    return false;
                }

                // CameraParam 是结构体；ROI 数组也必须复制，全部恢复成功才提交。
                CameraParam local = cup.p_Cam[iCamIndex];
                local.iROI = local.iROI == null ? null : (uint[])local.iROI.Clone();
                MyCamera camera = camera2[iCamIndex];
                string gainshow = null, exposureshow = null;
                stage = "ExposureTime";
                if (!hIKVisionCamera.SetParam(local.dGain, local.iExposeTime, ref gainshow, ref exposureshow, camera)) return false;
                stage = "PixelFormat";
                if (!hIKVisionCamera.SetPixelFormat(ref local.iMonoType, camera)) return false;
                stage = "Sharpness";
                if (!hIKVisionCamera.SetSharpness(ref local.iSharp, camera)) return false;
                stage = "Gamma";
                if (!hIKVisionCamera.SetGamma(ref local.dGamma, camera)) return false;
                stage = "Resolution";
                uint width = local.iWidth, height = local.iHeight;
                if (!hIKVisionCamera.SetResolution(ref width, ref height, camera)) return false;
                uint[] roi = local.iROI;
                if (roi == null || roi.Length < 4)
                    roi = new uint[] { 0, 0, width, height };
                stage = "ROI.Width";
                if (!hIKVisionCamera.SetIntValueChecked(camera, "Width", roi[2])) return false;
                stage = "ROI.Height";
                if (!hIKVisionCamera.SetIntValueChecked(camera, "Height", roi[3])) return false;
                stage = "ROI.OffsetX";
                if (!hIKVisionCamera.SetIntValueChecked(camera, "OffsetX", roi[0])) return false;
                stage = "ROI.OffsetY";
                if (!hIKVisionCamera.SetIntValueChecked(camera, "OffsetY", roi[1])) return false;
                stage = "Trigger";
                if (cup.p_CD.triggleCam == iCamIndex && !SetTrigger(iCamIndex)) return false;
                stage = "AcquisitionState.ReadBack";
                string runtimeState;
                if (!GetCameraRuntimeState(iCamIndex, out runtimeState)) return false;
                WriteCameraDiagnostic("参数恢复读回", 0, $"cameraIndex={iCamIndex}, serial={serial}; {runtimeState}");
                local.iROI = roi;
                local.iWidth = roi[2];
                local.iHeight = roi[3];
                cup.p_Cam[iCamIndex] = local;
                return true;
            }
            catch (Exception ex)
            {
                LogCameraConnection("参数恢复", false,
                    $"cameraIndex={iCamIndex}, serial={serial}, node={stage}, exception={ex}");
                return false;
            }
        }

        public bool SetTrigger(int iCamIndex)
        {
            try
            {
                MyCamera camera = camera2[iCamIndex];
                if (camera == null) return false;
                if (!hIKVisionCamera.SetEnumValueChecked(camera, "TriggerMode", cup.p_CD.bTriggle
                    ? (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON
                    : (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF)) return false;
                if (cup.p_CD.bTriggle)
                    return hIKVisionCamera.SetEnumValueChecked(camera, "TriggerSource", cup.p_CD.triggleMode == 4
                        ? (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE
                        : (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                return true;
            }
            catch (Exception ex)
            {
                LogCameraConnection("触发设置", false, $"cameraIndex={iCamIndex}, serial={hIKVisionCamera.GetConnectedCameraSerial(iCamIndex)}, exception={ex}");
                return false;
            }
        }


 public bool Restore(){return SetCamParam(0);}
}
class RangeHarness {
 public int Diagnostics;public bool CheckSdkResult(MyCamera c,string n,string target,int ret){if(ret!=0)Diagnostics++;return ret==0;}
 public bool SetEnumValueChecked(MyCamera c,string n,uint v){c.SetCalls++;return true;}
 static uint Clamp(uint v,uint min,uint max){return Math.Max(min,Math.Min(max,v));}
        public bool SetSharpness(ref uint value, MyCamera cam)
        {
            var range = new MyCamera.MVCC_INTVALUE();
            if (!CheckSdkResult(cam, "Sharpness.GetRange", value.ToString(), cam.MV_CC_GetSharpness_NET(ref range))) return false;
            uint adjusted = Clamp(value, range.nMin, range.nMax);
            if (!SetEnumValueChecked(cam, "SharpnessAuto", 0)) return false;
            if (!CheckSdkResult(cam, "Sharpness", adjusted.ToString(), cam.MV_CC_SetSharpness_NET(adjusted))) return false;
            value = adjusted;
            return true;
        }


}
class Tests {
 public static int Count;public static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL "+name);Count++;Console.WriteLine("PASS "+name);}
 public static void Main(){
  new PreviewHarness().TimeoutTest();new PreviewHarness().DuplicateTest();
  var h=new PreviewHarness();var c=new MyCamera{DisplayThrows=true,Next=()=>0};h.Run(c);
  Check(c.FreeCalls==1&&h.Has("display-test")&&h.Has("InvalidOperationException")&&h.Has("预览线程退出"),"display exception frees frame and records full exit exception");
  h=new PreviewHarness();c=new MyCamera{FreeRet=unchecked((int)0x80000003),Next=()=>0};h.Run(c);
  Check(c.GetCalls==1&&c.FreeCalls==1&&h.Has("FreeImageBuffer失败"),"free SDK failure exits without retry");
  h=new PreviewHarness();c=new MyCamera{FreeThrows=true,Next=()=>0};h.Run(c);
  Check(c.FreeCalls==1&&h.Has("free-test")&&h.Has("InvalidOperationException"),"free exception retained in exit log");
  h=new PreviewHarness();c=new MyCamera();c.Next=()=>c.GetCalls==1?0:MyCamera.MV_E_NODATA;c.OnGet=()=>{if(c.GetCalls==3)h.StopFlag();};h.Run(c);
  Check(c.FreeCalls==1&&h.Has("successfulFrames=1")&&h.Has("subsequentFrames=0")&&h.Has("consecutiveNoData=2")&&h.Has("线程仍在运行"),"first-frame freeze distinguishes no data from thread exit");
  h=new PreviewHarness();c=new MyCamera{Next=()=>unchecked((int)0x80000003)};h.Run(c);
  Check(c.GetCalls==1&&h.Has("未重试"),"non-no-data SDK error exits once");
  h=new PreviewHarness();c=new MyCamera{Connected=false};h.Run(c);Check(h.Has("设备已断开"),"disconnect has distinct exit reason");
  foreach(var node in new[]{"PixelFormat","Sharpness","Gamma","Resolution","Width","Height","OffsetX","OffsetY","ReadBack"}){
    var r=new RestoreHarness();r.hIKVisionCamera.Fail=node;var before=r.cup.p_Cam[0];var roi=before.iROI;
    Check(!r.Restore()&&r.cup.p_Cam[0].iMonoType==before.iMonoType&&r.cup.p_Cam[0].iSharp==before.iSharp&&r.cup.p_Cam[0].dGamma==before.dGamma&&ReferenceEquals(r.cup.p_Cam[0].iROI,roi)&&roi[2]==3072,"failed restore leaves saved parameters intact: "+node);
  }
  var range=new RangeHarness();c=new MyCamera{RangeRet=unchecked((int)0x80000106)};uint value=7;
  Check(!range.SetSharpness(ref value,c)&&value==7&&c.SetCalls==0&&range.Diagnostics==1,"failed range read causes no write or saved-value mutation");
  Console.WriteLine("TOTAL "+Count+" PASS (fake SDK only, no hardware)");
 }
}
