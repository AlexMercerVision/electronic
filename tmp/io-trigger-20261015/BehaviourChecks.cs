
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
class SerialDataReceivedEventArgs : EventArgs { }
delegate void SerialDataReceivedEventHandler(object sender, SerialDataReceivedEventArgs args);
class SerialPort {
 public bool IsOpen; public string PortName="COM4";
 public List<byte[]> Writes=new List<byte[]>(); public Queue<byte> Pending=new Queue<byte>();
 public int MaxRead=int.MaxValue; public bool FailWrite;
 private SerialDataReceivedEventHandler received;
 public int HandlerCount {get {return received==null?0:received.GetInvocationList().Length;}}
 public event SerialDataReceivedEventHandler DataReceived {add {received+=value;} remove {received-=value;}}
 public int BytesToRead {get {return Pending.Count;}}
 public int Read(byte[] target,int offset,int count) {int actual=Math.Min(Math.Min(count,MaxRead),Pending.Count);for(int index=0;index<actual;index++)target[offset+index]=Pending.Dequeue();return actual;}
 public void Write(byte[] frame,int offset,int count) {if(FailWrite)throw new InvalidOperationException("write failure");lock(Writes)Writes.Add(frame.Skip(offset).Take(count).ToArray());}
 public void Report(byte[] frame) {foreach(byte value in frame)Pending.Enqueue(value);if(received!=null)received(this,new SerialDataReceivedEventArgs());}
}
class CSerialDef {
 public SerialPort _sPortCy,_sPort; public SerialPort[] _sPortSub;
 private readonly object ioOutputWriteSync=new object();
private void WriteIOFrame(SerialPort sPort, byte[] frame, int count)
        {
            lock (ioOutputWriteSync)
                sPort.Write(frame, 0, count);
        }
public bool SendIOOutputBitmapPulse(SerialPort sPort, byte outputBitmap, int pulseMilliseconds,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (sPort == null)
            {
                errorMessage = "IO串口对象为空";
                return false;
            }
            if (!sPort.IsOpen)
            {
                errorMessage = $"IO串口{sPort.PortName}未打开";
                return false;
            }
            if (outputBitmap == 0)
            {
                errorMessage = "IO输出位图为空";
                return false;
            }
            if (pulseMilliseconds <= 0 || pulseMilliseconds > 60000)
            {
                errorMessage = $"IO脉冲时间非法：{pulseMilliseconds}ms";
                return false;
            }

            lock (ioOutputWriteSync)
            {
                bool outputEnabled = false;
                try
                {
                    byte[] outputFrame = BuildWriteEightOutputsFrame(outputBitmap);
                    WriteIOFrame(sPort, outputFrame, outputFrame.Length);
                    outputEnabled = true;

                    Thread.Sleep(pulseMilliseconds);

                    byte[] clearFrame = BuildWriteEightOutputsFrame(0x00);
                    WriteIOFrame(sPort, clearFrame, clearFrame.Length);
                    outputEnabled = false;
                    return true;
                }
                catch (Exception ex)
                {
                    errorMessage = $"IO组合输出失败：{ex.Message}";
                    if (outputEnabled && sPort.IsOpen)
                    {
                        try
                        {
                            byte[] clearFrame = BuildWriteEightOutputsFrame(0x00);
                            WriteIOFrame(sPort, clearFrame, clearFrame.Length);
                        }
                        catch (Exception clearEx)
                        {
                            errorMessage += $"；全部关闭失败：{clearEx.Message}";
                        }
                    }
                    return false;
                }
            }
        }
private byte[] BuildWriteEightOutputsFrame(byte outputBitmap)
        {
            byte[] frame =
            {
                0x01,       // 从站地址
                0x0F,       // 写多个线圈
                0x00, 0x00, // 起始地址：Y1
                0x00, 0x08, // 线圈数量：Y1～Y8
                0x01,       // 数据字节数
                outputBitmap,
                0x00, 0x00  // CRC占位
            };
            CalculateCRC(ref frame);
            return frame;
        }
private void CalculateCRC(ref byte[] data)
        {
            ushort crc = 0xFFFF;
            const ushort polynomial = 0xA001; // MODBUS标准多项式

            for(int j=0;j<8;j++)
            {
                crc ^= data[j];
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc = (ushort)((crc >> 1) ^ polynomial);
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            data[8] =(Byte)(crc & 0xFF);
            data[9] = (Byte)(crc >> 8);
         
        }

}
class Param {
 public SD p_SD=new SD(); public IO p_IOPP=new IO(); public CD p_CD=new CD(); public RT p_RTSP=new RT();public RTP p_VPPRTP=new RTP();
}
class SD {public bool[] bUsed={false,false,false,true};public int[] serInd={0,1,0,0};}
class IO {public int APlane=1,BPlane=2,EndAPlane,EndBPlane,IOTimeDelay=1;public int[] JobOkOut={1,3},JobNGOut={2,4};}
class CD {public bool[] bListProcess={true,true};}
class RT {public bool bUpdateIO=true;public int runTimeOut=10;}
class RTP {public bool bVisionInfer,bAcqFromCam=true;}
class Init {public bool bCamInit=true,bCamPrepared=true,bLocalFileExist=true,bSerInit;public int iVPPStatus;}
class Online {public bool bTrigg=true,bLock;}
class State {public int iModelRole=9,iIsRunning;public Init initStatus=new Init();public Online onLS=new Online();}
class Log {public int iEventStatus;public string strOperEvent,strFailReason;}
struct Result {public bool bOk,bLocated;}
enum FlatStyle {Popup}
class Control {
 public string Text;public bool Enabled;public FlatStyle FlatStyle;
 public bool InvokeRequired {get{return false;}}public void Invoke(Action action){action();}
}
class VP {
 public bool bMainTaskFinished=true;public int _iTaskType;public int[] _bRunOneTimeFinished=new int[2];
 public Result[] _PMARes=new Result[2];public string[] strErrInfo={"failure","failure"};
 public bool[] Busy=new bool[2],Success={true,true},Validity={true,true},TimeOut=new bool[2];public int[] Runs=new int[2];public bool ThrowClear;
 public void ClearRDRecord(int index){if(ThrowClear)throw new InvalidOperationException("display failure");}
 public bool RunOneTimeSplite(int index){return RunOneTime(index);}
 public bool RunOneTime(int index){Runs[index]++;if(!Success[index])return false;if(TimeOut[index])return true;_bRunOneTimeFinished[index]=2;_PMARes[index].bLocated=true;return true;}
 public bool IsVppJobResultValid(int index){return Validity[index];}
 public void SetJobBusy(int index,bool busy){Busy[index]=busy;}
}
class Harness {
 const int maxCameraNum=2,ioFeedbackJobCount=2;
 Param cup=new Param(); State rts=new State();Log operLog=new Log(); VP vpWin=new VP();CSerialDef csd=new CSerialDef();
 SerialPort sPortCy=new SerialPort {IsOpen=true},sPortMain;SerialPort[] sPortAll=new SerialPort[4],sPortSub=new SerialPort[2];
 object ioFeedbackSync=new object(),_autoTriggerSync=new object(),_lockBuffer=new object();
 bool[] ioFeedbackResult=new bool[2],ioFeedbackReady=new bool[2],ioFeedbackStarted=new bool[2],ioFeedbackParticipating=new bool[2];
 long[] ioFeedbackSingleJobCycles=new long[2];bool[] ioFeedbackSingleJobOutputInProgress=new bool[2];
 bool ioFeedbackCycleActive,ioFeedbackOutputInProgress,allJobsDone;long ioFeedbackCycleId;int completedJobCount,manualSingleRunActive;
 bool[] _autoTriggerVisionReservedByJob=new bool[2],_visionRunningByJob=new bool[2],_ioTriggerActiveByJob=new bool[2];
 Result[] pmaRes=new Result[2];int iJobIndex;string strOrder="";Control btFunRunOneTime=new Control(),toolStripStatus_Ser=new Control(),toolStripStatus_Hand=new Control();
 List<byte> _rxBuffer=new List<byte>();byte lastIOInputState;
 System.Timers.Timer timerIOIn;
 List<int> Triggers=new List<int>();bool DispatchTriggers,OpenSuccess=true;
 bool[] serialConfigErrorLogged=new bool[4];List<string> Logs=new List<string>();
 enum SerialStatus {Disabled,Success,Failed}
 bool InvokeRequired {get{return false;}} void Invoke(Action action){action();}
 void AddLog(){lock(Logs)Logs.Add(operLog.strOperEvent);}
 void WriteIOFeedbackLog(int status,string name,string detail){lock(Logs)Logs.Add(name);}
 void SetFaceTriggerIndicator(int index,bool active){}
 bool IsFaceLaserBusy(int index){return false;}
 bool IsLivePreviewRunning(){return false;}
 bool StopLivePreviewForTrigger(string operation){return true;}
 void ShowCurrentTaskNotFinishedWarning(){}
 Task LaserMarkAfterVisionDetAsync(int index){return Task.FromResult(0);}
 void PreviewImage(int mode){}void StopRun(){}
 void UpdateUIExeForJob(int index){}
 void ExeIOTrig(int type){Triggers.Add(type);if(DispatchTriggers)RealExeIOTrig(type);}
 void ResetIOFeedbackCycle(string reason){lock(ioFeedbackSync){ResetIOFeedbackCycleNoLock();Array.Clear(ioFeedbackSingleJobCycles,0,2);Array.Clear(ioFeedbackSingleJobOutputInProgress,0,2);}}
 List<string> GetSortedPorts(){return new List<string>{"COM4","COM5"};}
 void LogInvalidSerialConfigurationOnce(int slot,int configIndex,int count){}
 bool OpenSerialPort(SerialPort port,string name){if(port==null)return false;port.PortName=name;port.IsOpen=OpenSuccess;return port.IsOpen;}
 void UpdateSerialStatusIndicator(int slot,SerialStatus status){}
 void UpdateSerialUIStatus(){}void InitializeLightBrightness(){}
private bool TryBeginIOFeedbackRun(int jobIndex, bool singleJobCycle,
            out long cycleId, out string errorMessage)
        {
            cycleId = 0;
            errorMessage = string.Empty;
            if (!cup.p_RTSP.bUpdateIO)
                return true;
            if (jobIndex < 0 || jobIndex >= ioFeedbackJobCount)
            {
                errorMessage = $"Job{jobIndex}不属于当前COM3 IO反馈范围";
                return false;
            }

            lock (ioFeedbackSync)
            {
                if (singleJobCycle)
                {
                    if (ioFeedbackCycleActive || ioFeedbackSingleJobCycles[jobIndex] != 0)
                    {
                        errorMessage = $"Job{jobIndex}的IO反馈尚未结束";
                        return false;
                    }
                    cycleId = ++ioFeedbackCycleId;
                    ioFeedbackSingleJobCycles[jobIndex] = cycleId;
                    return true;
                }
                if (ioFeedbackSingleJobCycles.Any(activeCycle => activeCycle != 0))
                {
                    errorMessage = "单工位IO反馈尚未结束";
                    return false;
                }
                if (!ioFeedbackCycleActive)
                {
                    bool anyParticipant = false;
                    for (int i = 0; i < ioFeedbackJobCount; i++)
                    {
                        ioFeedbackParticipating[i] = singleJobCycle
                            ? i == jobIndex
                            : cup.p_CD.bListProcess != null &&
                              i < cup.p_CD.bListProcess.Length &&
                              cup.p_CD.bListProcess[i];
                        anyParticipant |= ioFeedbackParticipating[i];
                        ioFeedbackStarted[i] = false;
                        ioFeedbackReady[i] = false;
                        ioFeedbackResult[i] = false;
                    }

                    if (!anyParticipant || !ioFeedbackParticipating[jobIndex])
                    {
                        ResetIOFeedbackCycleNoLock();
                        errorMessage = $"Job{jobIndex}不属于当前IO反馈批次";
                        return false;
                    }

                    ioFeedbackCycleId++;
                    ioFeedbackCycleActive = true;
                    System.Threading.Interlocked.Exchange(ref completedJobCount, 0);
                    allJobsDone = false;
                }

                if (ioFeedbackOutputInProgress)
                {
                    errorMessage = $"IO反馈批次{ioFeedbackCycleId}正在输出";
                    return false;
                }
                if (!ioFeedbackParticipating[jobIndex])
                {
                    errorMessage = $"Job{jobIndex}未参与IO反馈批次{ioFeedbackCycleId}";
                    return false;
                }
                if (ioFeedbackStarted[jobIndex])
                {
                    errorMessage = $"Job{jobIndex}已开始IO反馈批次{ioFeedbackCycleId}，拒绝跨轮重复运行";
                    return false;
                }

                ioFeedbackStarted[jobIndex] = true;
                cycleId = ioFeedbackCycleId; // 仅成功加入的请求取得取消/提交权限。
                return true;
            }
        }
private void SubmitIOFeedbackResult(int jobIndex, bool isOk, long cycleId)
        {
            if (!cup.p_RTSP.bUpdateIO || jobIndex < 0 || jobIndex >= ioFeedbackJobCount)
                return;

            byte outputBitmap = 0;
            string prepareError = string.Empty;
            bool shouldSend = false;
            bool singleJobCycle = false;

            lock (ioFeedbackSync)
            {
                if (cycleId > 0 && ioFeedbackSingleJobCycles[jobIndex] == cycleId)
                {
                    singleJobCycle = true;
                    if (ioFeedbackSingleJobOutputInProgress[jobIndex])
                    {
                        prepareError = $"Job{jobIndex}的IO反馈批次{cycleId}正在输出";
                    }
                    else
                    {
                        bool[] results = new bool[ioFeedbackJobCount];
                        bool[] participants = new bool[ioFeedbackJobCount];
                        results[jobIndex] = isOk;
                        participants[jobIndex] = true;
                        if (TryBuildIOOutputBitmap(results, participants, out outputBitmap, out prepareError))
                        {
                            ioFeedbackSingleJobOutputInProgress[jobIndex] = true;
                            shouldSend = true;
                        }
                        else
                        {
                            ioFeedbackSingleJobCycles[jobIndex] = 0;
                        }
                    }
                }
                else if (!ioFeedbackCycleActive || cycleId <= 0 || cycleId != ioFeedbackCycleId)
                {
                    prepareError = $"Job{jobIndex}提交了已失效的IO反馈批次{cycleId}，当前批次为" +
                        (ioFeedbackCycleActive ? ioFeedbackCycleId.ToString() : "无");
                }
                else if (!ioFeedbackParticipating[jobIndex] || !ioFeedbackStarted[jobIndex])
                {
                    prepareError = $"Job{jobIndex}未开始IO反馈批次{cycleId}";
                }
                else if (ioFeedbackReady[jobIndex])
                {
                    prepareError = $"Job{jobIndex}已提交IO反馈批次{cycleId}，拒绝重复结果";
                }
                else
                {
                    ioFeedbackResult[jobIndex] = isOk;
                    ioFeedbackReady[jobIndex] = true;

                    bool allReady = true;
                    for (int i = 0; i < ioFeedbackJobCount; i++)
                    {
                        if (ioFeedbackParticipating[i] && !ioFeedbackReady[i])
                        {
                            allReady = false;
                            break;
                        }
                    }

                    if (allReady && !ioFeedbackOutputInProgress)
                    {
                        if (TryBuildIOOutputBitmap(ioFeedbackResult, ioFeedbackParticipating,
                            out outputBitmap, out prepareError))
                        {
                            ioFeedbackOutputInProgress = true;
                            shouldSend = true;
                        }
                        else
                        {
                            ResetIOFeedbackCycleNoLock();
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(prepareError))
            {
                WriteIOFeedbackLog(-1, "IO组合结果", prepareError);
                return;
            }
            if (!shouldSend)
            {
                WriteIOFeedbackLog(0, "IO结果就绪",
                    $"批次{cycleId} Job{jobIndex}={(isOk ? "OK" : "NG")}，等待其他参与工位");
                return;
            }

            string sendError = string.Empty;
            bool sendSucceeded = false;
            try
            {
                sendSucceeded = csd.SendIOOutputBitmapPulse(
                    sPortCy, outputBitmap, cup.p_IOPP.IOTimeDelay, out sendError);
            }
            catch (Exception ex)
            {
                sendError = ex.ToString();
            }
            finally
            {
                lock (ioFeedbackSync)
                {
                    if (singleJobCycle && ioFeedbackSingleJobCycles[jobIndex] == cycleId)
                    {
                        ioFeedbackSingleJobCycles[jobIndex] = 0;
                        ioFeedbackSingleJobOutputInProgress[jobIndex] = false;
                    }
                    else if (!singleJobCycle && ioFeedbackCycleActive && ioFeedbackCycleId == cycleId)
                        ResetIOFeedbackCycleNoLock();
                }
            }

            if (sendSucceeded)
            {
                WriteIOFeedbackLog(0, "IO组合输出",
                    $"批次{cycleId} 位图0x{outputBitmap:X2}发送完成，保持{cup.p_IOPP.IOTimeDelay}ms后Y1～Y8已关闭");
            }
            else
            {
                WriteIOFeedbackLog(-1, "IO组合输出失败",
                    $"批次{cycleId} 位图0x{outputBitmap:X2}：{sendError}");
            }
        }
private bool TryBuildIOOutputBitmap(bool[] results, bool[] participants,
            out byte outputBitmap, out string errorMessage)
        {
            outputBitmap = 0;
            errorMessage = string.Empty;

            if (!ValidateIOOutputMapping(out errorMessage))
                return false;

            for (int jobIndex = 0; jobIndex < ioFeedbackJobCount; jobIndex++)
            {
                if (!participants[jobIndex])
                    continue;

                int outputNo = results[jobIndex]
                    ? cup.p_IOPP.JobOkOut[jobIndex]
                    : cup.p_IOPP.JobNGOut[jobIndex];
                outputBitmap |= (byte)(1 << (outputNo - 1));
            }
            return outputBitmap != 0;
        }
private bool ValidateIOOutputMapping(out string errorMessage)
        {
            errorMessage = string.Empty;
            if (cup.p_IOPP.JobOkOut == null || cup.p_IOPP.JobOkOut.Length < ioFeedbackJobCount ||
                cup.p_IOPP.JobNGOut == null || cup.p_IOPP.JobNGOut.Length < ioFeedbackJobCount)
            {
                errorMessage = "IO输出映射缺失";
                return false;
            }

            HashSet<int> usedOutputs = new HashSet<int>();
            for (int jobIndex = 0; jobIndex < ioFeedbackJobCount; jobIndex++)
            {
                int okOutput = cup.p_IOPP.JobOkOut[jobIndex];
                int ngOutput = cup.p_IOPP.JobNGOut[jobIndex];
                if (okOutput < 1 || okOutput > 8 || ngOutput < 1 || ngOutput > 8)
                {
                    errorMessage = $"Job{jobIndex}输出必须在Y1～Y8范围内";
                    return false;
                }
                if (okOutput == ngOutput)
                {
                    errorMessage = $"Job{jobIndex}的OK与NG不能使用同一输出Y{okOutput}";
                    return false;
                }
                if (!usedOutputs.Add(okOutput) || !usedOutputs.Add(ngOutput))
                {
                    errorMessage = "Job0/Job1的OK、NG输出通道不能重复";
                    return false;
                }
            }
            if (cup.p_IOPP.JobOkOut[0] != 1 || cup.p_IOPP.JobNGOut[0] != 2 ||
                cup.p_IOPP.JobOkOut[1] != 3 || cup.p_IOPP.JobNGOut[1] != 4)
            {
                errorMessage = "本轮IO映射固定为Job0 OK/NG→Y1/Y2、Job1 OK/NG→Y3/Y4";
                return false;
            }
            return true;
        }
private bool CanAcceptIOFeedbackRun(int jobIndex, out string reason)
        {
            reason = string.Empty;
            if (!cup.p_RTSP.bUpdateIO || jobIndex < 0 || jobIndex >= ioFeedbackJobCount)
                return true;

            lock (ioFeedbackSync)
            {
                if (ioFeedbackSingleJobCycles[jobIndex] != 0)
                {
                    reason = $"Job{jobIndex}的IO反馈尚未结束";
                    return false;
                }
                if (!ioFeedbackCycleActive)
                    return true;
                reason = $"IO组合反馈批次{ioFeedbackCycleId}尚未结束";
                return false;
            }
        }
private void AbortIOFeedbackCycle(int failedJobIndex, long cycleId, string reason)
        {
            long abortedCycleId = 0;
            lock (ioFeedbackSync)
            {
                if (cycleId <= 0 || failedJobIndex < 0 || failedJobIndex >= ioFeedbackJobCount)
                    return;
                if (ioFeedbackSingleJobCycles[failedJobIndex] == cycleId)
                {
                    if (ioFeedbackSingleJobOutputInProgress[failedJobIndex])
                        return;
                    abortedCycleId = cycleId;
                    ioFeedbackSingleJobCycles[failedJobIndex] = 0;
                }
                else
                {
                    if (!ioFeedbackCycleActive || ioFeedbackCycleId != cycleId || ioFeedbackOutputInProgress)
                        return;
                    abortedCycleId = ioFeedbackCycleId;
                    ResetIOFeedbackCycleNoLock();
                }
            }
            WriteIOFeedbackLog(-1, "IO反馈批次取消",
                $"批次{abortedCycleId}因Job{failedJobIndex}失败而清除：{reason}");
        }
private void ResetIOFeedbackCycleNoLock()
        {
            for (int i = 0; i < ioFeedbackJobCount; i++)
            {
                ioFeedbackResult[i] = false;
                ioFeedbackReady[i] = false;
                ioFeedbackStarted[i] = false;
                ioFeedbackParticipating[i] = false;
            }
            ioFeedbackCycleActive = false;
            ioFeedbackOutputInProgress = false;
        }
private void ExeIOOut(int jobIndex, long cycleId)
        {
            if (jobIndex < 0 || jobIndex >= ioFeedbackJobCount)
                return;

            if (cup.p_VPPRTP.bVisionInfer && !vpWin.IsVppJobResultValid(jobIndex))
            {
                AbortIOFeedbackCycle(jobIndex, cycleId, "本轮长度测量结果无效");
                return;
            }
            SubmitIOFeedbackResult(jobIndex, pmaRes[jobIndex].bOk, cycleId);
        }
private void ProcessSerialBuffer()
        {
            while (_rxBuffer.Count >= 6)  // 最小包长为6（查询反馈）
            {
                // ---- 每次循环重新判断当前包类型 ----
                int packLen = 0;
                int diOffset = -1;
                bool bIOInput = false;

                // 1. 判断 IO 主动上报
                if (_rxBuffer.Count >= 4 &&
                    _rxBuffer[0] == 0x01 && _rxBuffer[1] == 0x45 &&
                    _rxBuffer[2] == 0x05 && _rxBuffer[3] == 0x01)
                {
                    packLen = 10;
                    diOffset = 4;
                    bIOInput = true;
                }
                else if (_rxBuffer.Count >= 5 &&
                         _rxBuffer[0] == 0x01 && _rxBuffer[1] == 0x45 &&
                         _rxBuffer[2] == 0x01 && _rxBuffer[3] == 0x00 && _rxBuffer[4] == 0x01)
                {
                    packLen = 25;
                    diOffset = 5;
                    bIOInput = true;
                }
                else if (_rxBuffer.Count >= 8 && _rxBuffer[0] == 0x01 && _rxBuffer[1] == 0x0F)
                {
                    // 设置输出反馈（长度可能8或16，这里示例取8，实际可根据协议动态）
                    packLen = 8;
                }
                else if (_rxBuffer.Count >= 6 && _rxBuffer[0] == 0x01 && _rxBuffer[1] == 0x02 && _rxBuffer[2] == 0x01)
                {
                    packLen = 6;
                }
                else
                {
                    // 无法识别：丢弃一个字节，尝试同步
                    byte unknownByte = _rxBuffer[0];
                    string hexStr = BitConverter.ToString(_rxBuffer.Take(Math.Min(10, _rxBuffer.Count)).ToArray());
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "串口未知数据";
                    operLog.strFailReason = $"跳过字节 0x{unknownByte:X2}，缓存前10字节: {hexStr}";
                    AddLog();
                    _rxBuffer.RemoveAt(0);
                    continue;  // 继续下一轮循环
                }

                // 检查完整包是否到达
                if (_rxBuffer.Count < packLen)
                    break;  // 数据不足，等待下一次接收

                // 取出完整包
                byte[] packet = _rxBuffer.GetRange(0, packLen).ToArray();
                _rxBuffer.RemoveRange(0, packLen);

                // 根据类型分发处理
                if (bIOInput)
                {
                    ProcessActiveReport(packet, packLen, diOffset);
                }
                else if (packLen == 8)  // 设置输出反馈
                {
                    ProcessSetOutputResponse(packet);
                }
                else if (packLen == 6)  // 查询反馈
                {
                    ProcessQueryResponse(packet);
                }
                // 循环继续，自动处理下一个包
            }
        }
private void ProcessActiveReport(byte[] packet, int packetLength, int diOffset)
        {
            if (packet == null || packet.Length != packetLength || diOffset < 0 || diOffset >= packetLength)
                return;
            byte diByte = packet[diOffset];
            byte risingInputs = (byte)(diByte & ~lastIOInputState);
            lastIOInputState = diByte;
            if (!rts.onLS.bTrigg)
                return;

            operLog.iEventStatus = 0;
            operLog.strOperEvent = "收到有效触发信号";
            operLog.strFailReason = "-";
            AddLog();

            // 收集所有触发的类型
            List<int> triggerTypes = new List<int>();

            // A面触发
            if (cup.p_IOPP.APlane > 0)
            {
                byte mask = (byte)(1 << (cup.p_IOPP.APlane - 1));
                if ((risingInputs & mask) == mask)
                {
                    SetFaceTriggerIndicator(0, true);
                    triggerTypes.Add(1);
                }
            }

            // B面触发
            if (cup.p_IOPP.BPlane > 0)
            {
                byte mask = (byte)(1 << (cup.p_IOPP.BPlane - 1));
                if ((risingInputs & mask) == mask)
                {
                    SetFaceTriggerIndicator(1, true);
                    triggerTypes.Add(2);
                }
            }

            // EndA触发
            if (cup.p_IOPP.EndAPlane > 0)
            {
                byte mask = (byte)(1 << (cup.p_IOPP.EndAPlane - 1));
                if ((diByte & mask) == mask)
                    triggerTypes.Add(3);
            }

            // EndB触发
            if (cup.p_IOPP.EndBPlane > 0)
            {
                byte mask = (byte)(1 << (cup.p_IOPP.EndBPlane - 1));
                if ((diByte & mask) == mask)
                    triggerTypes.Add(4);
            }

            // 执行所有触发任务（按顺序，每个任务异步执行）
            if (triggerTypes.Count > 0)
            {
                Thread.Sleep(5);   // 保留短延时保证稳定性
                foreach (int type in triggerTypes)
                {
                    ExeIOTrig(type);   // ExeIOTrig 内部已经用 Task.Run 异步执行，所以可以循环调用
                    Thread.Sleep(20);
                }

                if (timerIOIn != null && !timerIOIn.Enabled)
                    timerIOIn.Start();
            }
        }
private void ProcessSetOutputResponse(byte[] packet)
        {
            toolStripStatus_Hand.Text = DateTime.Now.ToString() + ":发送命令：" + strOrder;
        }
private void ProcessQueryResponse(byte[] packet)
        {
            if ((packet[3] & 0x01) == 0x01)  // 气缸抬升完成
            {
                // 可添加业务逻辑
            }
            else if ((packet[3] & 0x02) == 0x02)  // 气缸下降完成
            {
                // 可添加业务逻辑
            }
        }
private bool InitSerialIndex(int i)
        {
            if (i < 0 || i >= sPortAll.Length || cup.p_SD.serInd == null || i >= cup.p_SD.serInd.Length)
                return false;
            if (i == 3 && !cup.p_SD.bUsed[3])
                return false;

            List<string> availablePorts = GetSortedPorts();
            int configIndex = cup.p_SD.serInd[i];
            string portName;
            if (!TryGetPortNameByConfigIndex(configIndex, availablePorts, out portName))
            {
                LogInvalidSerialConfigurationOnce(i, configIndex, availablePorts.Count);
                return false;
            }

            serialConfigErrorLogged[i] = false;
            if (i == 3 && sPortAll[i] == null)
                sPortAll[i] = new SerialPort();
            bool opened = OpenSerialPort(sPortAll[i], portName);
            if (i == 3)
                RegisterIOReceive();
            return opened;
        }
private void RegisterIOReceive()
        {
            SerialPort ioPort = sPortAll[3];
            if (sPortCy != null)
                sPortCy.DataReceived -= PortGetCylinder;
            sPortCy = ioPort;
            csd._sPortCy = ioPort;
            if (ioPort == null)
                return;
            ioPort.DataReceived -= PortGetCylinder;
            if (cup.p_SD.bUsed[3] && ioPort.IsOpen)
                ioPort.DataReceived += PortGetCylinder;
        }
private void InitSerial()
        {
            // 初始化串口数组
            for (int i = 0; i < 4; i++)
            {
                sPortAll[i] = new SerialPort();
            }
            RegisterIOReceive();

            // 获取可用串口列表
            List<string> availablePorts = GetSortedPorts();

            operLog.strOperEvent = "串口初始化";

            if (availablePorts.Count == 0)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "未搜索到可用串口";
                AddLog();
                return;
            }

            // 更新状态栏显示可用串口信息
            toolStripStatus_Ser.Text = $"可用串口: {string.Join(", ", availablePorts)}";

            int openedCount = 0;

            for (int i = 0; i < 4; i++)
            {
                if (!cup.p_SD.bUsed[i])
                {
                    // 未使用的串口，状态显示为灰色
                    UpdateSerialStatusIndicator(i, SerialStatus.Disabled);
                    continue;
                }

                // 获取配置的串口索引
                int configIndex = cup.p_SD.serInd[i];

                // 获取对应的串口名称
                string portName;

                if (!TryGetPortNameByConfigIndex(configIndex, availablePorts, out portName))
                {
                    LogInvalidSerialConfigurationOnce(i, configIndex, availablePorts.Count);
                    UpdateSerialStatusIndicator(i, SerialStatus.Failed);
                    continue;
                }

                serialConfigErrorLogged[i] = false;

                // 打开串口
                if (OpenSerialPort(sPortAll[i], portName))
                {
                    openedCount++;
                    UpdateSerialStatusIndicator(i, SerialStatus.Success);

                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = $"串口{i + 1}({portName})打开成功";
                    AddLog();
                }
                else
                {
                    UpdateSerialStatusIndicator(i, SerialStatus.Failed);
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = $"串口{i + 1}({portName})打开失败";
                    operLog.strFailReason = "请检查设备连接";
                    AddLog();
                }
            }

            // 设置特殊串口引用
            if (sPortAll[0] != null) sPortMain = sPortAll[0];
            if (sPortAll[1] != null) sPortSub[0] = sPortAll[1];
            if (sPortAll[2] != null) sPortSub[1] = sPortAll[2];

            csd._sPort = sPortMain;
            csd._sPortSub = sPortSub;

            // 注册数据接收事件
            RegisterIOReceive();

            rts.initStatus.bSerInit = (openedCount > 0);

            // 更新UI显示
            UpdateSerialUIStatus();
            InitializeLightBrightness();
        }
private bool TryGetPortNameByConfigIndex(int configIndex, IList<string> sortedPorts, out string portName)
        {
            portName = "";
            if (sortedPorts == null)
                return false;

            if (configIndex >= 0 && configIndex < sortedPorts.Count)
            {
                portName = sortedPorts[configIndex];
                return !string.IsNullOrWhiteSpace(portName);
            }

            return false;
        }
private void PortGetCylinder(object sender, SerialDataReceivedEventArgs e)
        {           
            SerialPort ioPort = sender as SerialPort;
            if (ioPort == null || !ReferenceEquals(ioPort, sPortCy) || !cup.p_SD.bUsed[3] || !ioPort.IsOpen)
                return;
            // 获取接收缓冲区长度
            Thread.Sleep(60);
            // 1. 读取所有可用数据
            int bytesToRead = ioPort.BytesToRead;
            if (bytesToRead == 0) return;
            byte[] temp = new byte[bytesToRead];
            int bytesRead = ioPort.Read(temp, 0, bytesToRead);

            // 2. 追加到缓存，并加锁防止多线程冲突
            lock (_lockBuffer)
            {
                _rxBuffer.AddRange(temp.Take(bytesRead));
                ProcessSerialBuffer();   // 尝试解析缓存中的完整包
            }
        }
private void RealExeIOTrig(int TrigType)
        {
            //未登录，禁止操作
            if (rts.iModelRole < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "执行功能操作";
                operLog.strFailReason = "用户身份未验证";
                AddLog();
                
                return;
            }
            else
            {
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "触发检测";
                operLog.strFailReason = "";
                AddLog();
            }

            switch (TrigType)
            {
                case 1:  //A 面触发
                   // if (cup.p_IOPP.EndBPlane == 0)  //没有检测结束信号，则认为每次触发仅检测一帧图像
                    {
                        if (TryReserveAutoTriggerRun(0, "A面触发运行"))
                        {
                            Task.Run(() => RunOneTimeForFace(0));
                        }
                           
                    }
                    
                    break;

                case 2:  //B面触发
                    {
                        if (TryReserveAutoTriggerRun(1, "B面触发运行"))
                        {
                            Task.Run(() => RunOneTimeForFace(1));
                        }

                    }
                    break;
                case 3:  //停止采集
                    PreviewImage(2);  //停止实时预览
                    break;

                case 4:  //停止检测
                    StopRun();
                    break;
            }

        }
private void RunOneTimeThread(bool syncMode)
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
                    System.Threading.Interlocked.Exchange(ref manualSingleRunActive, 0);
                    vpWin.bMainTaskFinished = true;
                    AddLog();
                    return;
                }
            }

            if (jobIndices.Count == 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "单次运行";
                operLog.strFailReason = "没有启用的工位";
                System.Threading.Interlocked.Exchange(ref manualSingleRunActive, 0);
                vpWin.bMainTaskFinished = true;
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
                        if (cup.p_VPPRTP.bVisionInfer && !vpWin.IsVppJobResultValid(jobIndex))
                        {
                            AbortIOFeedbackCycle(jobIndex, activeIOCycleToken, "本轮长度测量结果无效");
                            ioFeedbackCancelled = true;
                            jobRunFailed = true;
                        }
                        else if (!ioFeedbackCancelled)
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
                System.Threading.Interlocked.Exchange(ref manualSingleRunActive, 0);
                if (btFunRunOneTime.InvokeRequired)
                    btFunRunOneTime.Invoke(new Action(() => btFunRunOneTime.FlatStyle = FlatStyle.Popup));
                else
                    btFunRunOneTime.FlatStyle = FlatStyle.Popup;
            }
        }
private bool IsFaceVisionBusy(int faceIndex)
        {
            return faceIndex >= 0 &&
                   faceIndex < maxCameraNum &&
                   (_autoTriggerVisionReservedByJob[faceIndex] || _visionRunningByJob[faceIndex]);
        }
private bool AnyFaceVisionActive()
        {
            for (int i = 0; i < maxCameraNum; i++)
            {
                if (_autoTriggerVisionReservedByJob[i] || _visionRunningByJob[i])
                    return true;
            }

            return false;
        }
private void UpdateMainVisionTaskFinishedFlag()
        {
            if (vpWin == null)
                return;

            vpWin.bMainTaskFinished = System.Threading.Volatile.Read(ref manualSingleRunActive) == 0 && !AnyFaceVisionActive();
        }
private bool CanRunFaceAutoTrigger(int faceIndex, string operEvent)
        {
            if (faceIndex < 0 || faceIndex >= maxCameraNum)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "工位索引无效";
                AddLog();
                return false;
            }

            if (cup.p_CD.bListProcess == null || faceIndex >= cup.p_CD.bListProcess.Length ||
                !cup.p_CD.bListProcess[faceIndex])
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "当前工位未启用";
                AddLog();
                return false;
            }

            if (cup.p_VPPRTP.bAcqFromCam && (!rts.initStatus.bCamInit || !rts.initStatus.bCamPrepared))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "部分相机没有准备就绪";
                AddLog();
                return false;
            }

            if (!cup.p_VPPRTP.bAcqFromCam && !rts.initStatus.bLocalFileExist)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "回放文件夹不存在或者图像文件不存在";
                AddLog();
                return false;
            }

            if (cup.p_VPPRTP.bVisionInfer && rts.initStatus.iVPPStatus != 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "视觉推理模块初始化失败";
                AddLog();
                return false;
            }

            if (vpWin == null)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "视觉窗口未初始化";
                AddLog();
                return false;
            }

            bool nonAutoVisionTaskBusy = !vpWin.bMainTaskFinished &&
                                         !AnyFaceVisionActive() &&
                                         (vpWin._iTaskType != 3 || !cup.p_VPPRTP.bAcqFromCam);
            if (nonAutoVisionTaskBusy && IsLivePreviewRunning())
            {
                if (!StopLivePreviewForTrigger(operEvent))
                    return false;

                nonAutoVisionTaskBusy = !vpWin.bMainTaskFinished &&
                                        !AnyFaceVisionActive() &&
                                        (vpWin._iTaskType != 3 || !cup.p_VPPRTP.bAcqFromCam);
            }
            if (nonAutoVisionTaskBusy)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                return false;
            }

            return true;
        }
private bool TryReserveAutoTriggerRun(int faceIndex, string operEvent)
        {
            lock (_autoTriggerSync)
            {
                if (!CanRunFaceAutoTrigger(faceIndex, operEvent))
                    return false;

                string ioFeedbackReason;
                if (!CanAcceptIOFeedbackRun(faceIndex, out ioFeedbackReason))
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = operEvent;
                    operLog.strFailReason = ioFeedbackReason;
                    AddLog();
                    return false;
                }

                if (IsFaceVisionBusy(faceIndex))
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = operEvent;
                    operLog.strFailReason = "当前工位视觉任务未结束";
                    AddLog();
                    return false;
                }

                if (IsFaceLaserBusy(faceIndex))
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = operEvent;
                    operLog.strFailReason = "当前工位激光任务未结束";
                    AddLog();
                    return false;
                }

                if (_ioTriggerActiveByJob[faceIndex])
                    return false;

                _autoTriggerVisionReservedByJob[faceIndex] = true;
                _ioTriggerActiveByJob[faceIndex] = true;
                UpdateMainVisionTaskFinishedFlag();
                SetFaceTriggerIndicator(faceIndex, true);
                return true;
            }
        }
private void ReleaseAutoTriggerReservation(int faceIndex)
        {
            lock (_autoTriggerSync)
            {
                if (faceIndex >= 0 && faceIndex < maxCameraNum)
                {
                    _autoTriggerVisionReservedByJob[faceIndex] = false;
                    if (!_visionRunningByJob[faceIndex])
                        _ioTriggerActiveByJob[faceIndex] = false;
                }
                UpdateMainVisionTaskFinishedFlag();
            }
        }
private void EnterFaceVisionRun(int faceIndex)
        {
            lock (_autoTriggerSync)
            {
                if (faceIndex < 0 || faceIndex >= maxCameraNum)
                    return;

                _autoTriggerVisionReservedByJob[faceIndex] = false;
                _visionRunningByJob[faceIndex] = true;
                UpdateMainVisionTaskFinishedFlag();
            }
        }
private void FinishFaceVisionRun(int faceIndex)
        {
            lock (_autoTriggerSync)
            {
                if (faceIndex >= 0 && faceIndex < maxCameraNum)
                {
                    _autoTriggerVisionReservedByJob[faceIndex] = false;
                    _visionRunningByJob[faceIndex] = false;
                    _ioTriggerActiveByJob[faceIndex] = false;
                }
                vpWin.SetJobBusy(faceIndex, false);
                UpdateMainVisionTaskFinishedFlag();
            }
        }
private async Task RunOneTimeForFace(int faceIndex, bool singleJobIOCycle = true)
        {
            string ioFeedbackReason;
            long ioFeedbackCycleToken = 0;

            bool reservationReleased = false;
            bool visionBusyEntered = false;
            bool visionOk = false;
            bool runSucceeded = false;
            DateTime startTime = DateTime.Now;   // 记录开始时间

            try
            {
                if (!TryBeginIOFeedbackRun(faceIndex, singleJobIOCycle,
                    out ioFeedbackCycleToken, out ioFeedbackReason))
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = $"工位{faceIndex + 1}运行";
                    operLog.strFailReason = ioFeedbackReason;
                    AddLog();
                    return;
                }
                vpWin.ClearRDRecord(faceIndex);

                // ★★★ 关键修改：不再修改 vpWin._iJobIndex，所有操作直接使用 faceIndex ★★★
                try
                {
                    vpWin._iTaskType = 2;
                    EnterFaceVisionRun(faceIndex);
                    visionBusyEntered = true;
                    ReleaseAutoTriggerReservation(faceIndex);
                    reservationReleased = true;

                    // 执行视觉检测（同步等待）
                    if (vpWin.RunOneTimeSplite(faceIndex))
                    {
                        int maxWait = cup.p_RTSP.runTimeOut / 10;
                        int count = 0;
                        while (vpWin._bRunOneTimeFinished[faceIndex] != 2 && count < maxWait)
                        {
                            await Task.Delay(10);
                            count++;
                        }

                        if (vpWin._bRunOneTimeFinished[faceIndex] == 2)
                        {
                            pmaRes[faceIndex] = vpWin._PMARes[faceIndex];
                            visionOk = !cup.p_VPPRTP.bVisionInfer || vpWin.IsVppJobResultValid(faceIndex);
                        }
                        else
                        {
                            vpWin._bRunOneTimeFinished[faceIndex] = 2;
                            pmaRes[faceIndex].bLocated = false;
                            visionOk = false;
                            operLog.iEventStatus = -1;
                            operLog.strOperEvent = $"工位{faceIndex + 1}视觉检测";
                            operLog.strFailReason = "视觉检测超时，未收到完成结果";
                            AddLog();
                            
                        }
                    }
                    else
                    {
                        vpWin._bRunOneTimeFinished[faceIndex] = 2;
                        pmaRes[faceIndex].bLocated = false;
                        visionOk = false;
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = $"工位{faceIndex + 1}视觉检测";
                        operLog.strFailReason = vpWin.strErrInfo[faceIndex];
                        AddLog();
                    }
                }
                catch (Exception ex)
                {
                    vpWin._bRunOneTimeFinished[faceIndex] = 2;
                    pmaRes[faceIndex].bLocated = false;
                    visionOk = false;
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = $"工位{faceIndex + 1}视觉检测异常";
                    operLog.strFailReason = ex.Message;
                    AddLog();
                }
                finally
                {
                    vpWin._bRunOneTimeFinished[faceIndex] = 2;
                }

                // 视觉检测成功后，若满足条件则激光标刻
                if (visionOk && rts.onLS.bLock && pmaRes[faceIndex].bLocated)
                {
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = $"工位{faceIndex + 1}标刻中";
                    operLog.strFailReason = "";
                    AddLog();
                    await LaserMarkAfterVisionDetAsync(faceIndex);
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = $"工位{faceIndex + 1}结束";
                    operLog.strFailReason = vpWin.strErrInfo[faceIndex];
                    AddLog();
                }

                // 视觉链成功完成后提交本工位真实OK/NG结果。
                if (cup.p_RTSP.bUpdateIO && visionOk)
                {
                    ExeIOOut(faceIndex, ioFeedbackCycleToken);
                }

                runSucceeded = visionOk;
            }
            catch (Exception ex)
            {
                vpWin._bRunOneTimeFinished[faceIndex] = 2;
                operLog.iEventStatus = -1;
                operLog.strOperEvent = $"工位{faceIndex + 1}运行失败";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
            finally
            {
                if (!runSucceeded)
                    AbortIOFeedbackCycle(faceIndex, ioFeedbackCycleToken, "视觉检测未成功完成");

                // 释放资源与标志
                if (!reservationReleased)
                    ReleaseAutoTriggerReservation(faceIndex);

                if (visionBusyEntered)
                    FinishFaceVisionRun(faceIndex);

                // ★★★ 移除 RestoreVisionExecutionContext(previousVisionJobIndex) ★★★
                // 不再恢复 vpWin._iJobIndex，因为我们从未修改它

                SetFaceTriggerIndicator(faceIndex, false);

                // 记录本次运行耗时
                TimeSpan elapsed = DateTime.Now - startTime;
                string faceName = (faceIndex == 0) ? "A面" : "B面";
                operLog.iEventStatus = runSucceeded ? 2 : -1;
                operLog.strOperEvent = runSucceeded
                    ? $"{faceName} 单次运行完成"
                    : $"{faceName} 单次运行失败";
                operLog.strFailReason = runSucceeded
                    ? $"总耗时 {elapsed.TotalMilliseconds:F0} ms"
                    : $"视觉链未成功完成，详见前序错误；总耗时 {elapsed.TotalMilliseconds:F0} ms";
                AddLog();
            }
        }

 static int checks;
 static void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static byte[] Report10(byte value){return new byte[]{1,0x45,5,1,value,0,0,0,0,0};}
 static byte[] Report25(byte value){var frame=new byte[25];frame[0]=1;frame[1]=0x45;frame[2]=1;frame[4]=1;frame[5]=value;return frame;}
 void Feed(byte[] frame){_rxBuffer.AddRange(frame);ProcessSerialBuffer();}
 long Begin(int job,bool single){long token;string error;if(!TryBeginIOFeedbackRun(job,single,out token,out error))throw new Exception(error);return token;}
 int[] Bitmaps(){lock(sPortCy.Writes)return sPortCy.Writes.Select(frame=>(int)frame[7]).ToArray();}
 static void WaitFor(Func<bool> condition){var until=DateTime.UtcNow.AddSeconds(4);while(!condition()){if(DateTime.UtcNow>until)throw new Exception("async timeout");Thread.Sleep(5);}}
 static void ParserChecks(){
  var sample=new Harness();sample.Feed(Report10(1));Check(sample.Triggers.SequenceEqual(new[]{1}),"10-byte DI reads offset 4");
  sample.Feed(Report10(1));Check(sample.Triggers.Count==1,"held high does not retrigger");
  sample.Feed(Report10(0));sample.Feed(Report10(1));Check(sample.Triggers.Count==2,"low then high rearms");
  sample.Feed(Report10(3));Check(sample.Triggers.SequenceEqual(new[]{1,1,2}),"B rising while A remains high only triggers B");
  sample=new Harness();sample.Feed(Report25(2));Check(sample.Triggers.SequenceEqual(new[]{2}),"25-byte DI reads offset 5");
  sample=new Harness();var frame=Report10(3);sample.Feed(frame.Take(7).ToArray());Check(sample.Triggers.Count==0,"split report waits for complete packet");sample.Feed(frame.Skip(7).ToArray());Check(sample.Triggers.SequenceEqual(new[]{1,2}),"split report preserves both input bits");
  sample=new Harness();sample.Feed(Report10(1).Concat(Report25(0)).Concat(Report25(2)).ToArray());Check(sample.Triggers.SequenceEqual(new[]{1,2})&&sample._rxBuffer.Count==0,"mixed coalesced frame lengths remain local");
  sample=new Harness();sample.rts.onLS.bTrigg=false;sample.Feed(Report10(1));sample.rts.onLS.bTrigg=true;sample.Feed(Report10(1));Check(sample.Triggers.Count==0,"old high input does not trigger on automatic enable");sample.Feed(Report10(0));sample.Feed(Report10(1));Check(sample.Triggers.Count==1,"fresh pulse triggers after automatic enable");
  sample=new Harness();sample.ProcessActiveReport(Report10(1),25,5);Check(sample.Triggers.Count==0,"mismatched packet metadata rejected");
 }
 static void RegistrationChecks(){
  var sample=new Harness();sample.OpenSuccess=false;sample.InitSerial();Check(sample.sPortCy.HandlerCount==0,"initial open failure has no receive registration");
  sample.OpenSuccess=true;Check(sample.InitSerialIndex(3)&&sample.sPortCy.HandlerCount==1&&ReferenceEquals(sample.sPortCy,sample.csd._sPortCy),"IO reconnect registers and shares exact serial object");
  sample.InitSerialIndex(3);Check(sample.sPortCy.HandlerCount==1,"repeated reconnect keeps one subscription");
  sample.sPortCy.Report(Report10(1));Check(sample.Triggers.Count==1,"reconnected serial reaches real parser");
  var previous=sample.sPortCy;sample.sPortAll[3]=new SerialPort{IsOpen=true};sample.RegisterIOReceive();Check(previous.HandlerCount==0&&sample.sPortCy.HandlerCount==1,"replacement detaches old serial callback");
  sample.cup.p_SD.bUsed[3]=false;sample.RegisterIOReceive();Check(sample.sPortCy.HandlerCount==0&&!sample.InitSerialIndex(3),"disabled IO never registers or reconnects");
  sample=new Harness();sample.InitSerial();sample.sPortCy.MaxRead=4;sample.sPortCy.Report(Report10(1));sample.PortGetCylinder(sample.sPortCy,new SerialDataReceivedEventArgs());sample.PortGetCylinder(sample.sPortCy,new SerialDataReceivedEventArgs());Check(sample.Triggers.Count==1&&sample._rxBuffer.Count==0,"partial SerialPort.Read appends only actual bytes");
 }
 static void FeedbackChecks(){
  string error;long rejected;
  var sample=new Harness();long token=sample.Begin(0,true);sample.SubmitIOFeedbackResult(0,true,token);Check(sample.Bitmaps().SequenceEqual(new[]{1,0})&&!sample.ioFeedbackCycleActive,"A alone emits Y1 and clear without B");
  token=sample.Begin(0,true);sample.SubmitIOFeedbackResult(0,false,token);Check(sample.Bitmaps().SequenceEqual(new[]{1,0,2,0}),"A can start next cycle and emit NG Y2");
  sample=new Harness();token=sample.Begin(1,true);sample.SubmitIOFeedbackResult(1,true,token);Check(sample.Bitmaps().SequenceEqual(new[]{4,0}),"B alone emits Y3 and clear without A");
  sample=new Harness();long first=sample.Begin(0,true),second=sample.Begin(1,true);Check(!sample.TryBeginIOFeedbackRun(0,true,out rejected,out error),"same face duplicate cycle rejected");sample.SubmitIOFeedbackResult(0,true,first);sample.SubmitIOFeedbackResult(1,false,second);Check(sample.Bitmaps().SequenceEqual(new[]{1,0,8,0}),"independent simultaneous faces do not block each other");
  sample=new Harness();first=sample.Begin(0,false);sample.SubmitIOFeedbackResult(0,true,first);Check(sample.Bitmaps().Length==0,"dual-face cycle waits for second result");second=sample.Begin(1,false);sample.SubmitIOFeedbackResult(1,false,second);Check(first==second&&sample.Bitmaps().SequenceEqual(new[]{9,0}),"dual-face cycle combines only its two results");
  sample=new Harness();sample.cup.p_CD.bListProcess[1]=false;token=sample.Begin(0,false);sample.SubmitIOFeedbackResult(0,true,token);Check(sample.Bitmaps().SequenceEqual(new[]{1,0}),"sync run with one enabled face only waits for that face");
  sample=new Harness();first=sample.Begin(0,true);second=sample.Begin(1,true);sample.AbortIOFeedbackCycle(0,first,"failure");sample.SubmitIOFeedbackResult(1,true,second);Check(sample.Bitmaps().SequenceEqual(new[]{4,0})&&sample.ioFeedbackSingleJobCycles[0]==0,"A failure does not cancel B independent output");
  sample=new Harness();token=sample.Begin(0,true);sample.AbortIOFeedbackCycle(0,token,"failure");first=sample.Begin(0,true);sample.AbortIOFeedbackCycle(0,token,"late");sample.SubmitIOFeedbackResult(0,false,token);Check(sample.ioFeedbackSingleJobCycles[0]==first&&sample.Bitmaps().Length==0,"stale result and cancel cannot affect next cycle");sample.SubmitIOFeedbackResult(0,true,first);
  sample=new Harness();token=sample.Begin(0,true);sample.sPortCy.FailWrite=true;sample.SubmitIOFeedbackResult(0,true,token);Check(sample.ioFeedbackSingleJobCycles[0]==0&&!sample.ioFeedbackSingleJobOutputInProgress[0],"send failure releases single-face batch");sample.sPortCy.FailWrite=false;token=sample.Begin(0,true);sample.SubmitIOFeedbackResult(0,true,token);Check(sample.Bitmaps().SequenceEqual(new[]{1,0}),"single-face retries after send failure");
  sample=new Harness();sample.cup.p_IOPP.JobOkOut[0]=0;token=sample.Begin(0,true);sample.SubmitIOFeedbackResult(0,true,token);Check(sample.ioFeedbackSingleJobCycles[0]==0&&sample.Bitmaps().Length==0,"invalid output mapping releases cycle without writing");
  sample=new Harness();sample.Begin(0,true);Check(!sample.TryBeginIOFeedbackRun(1,false,out rejected,out error),"combined run cannot consume independent cycle");
  sample=new Harness();sample.Begin(0,false);Check(!sample.TryBeginIOFeedbackRun(1,true,out rejected,out error),"independent run cannot consume combined cycle");
  foreach(bool firstOk in new[]{false,true})foreach(bool secondOk in new[]{false,true}){sample=new Harness();first=sample.Begin(0,false);second=sample.Begin(1,false);sample.SubmitIOFeedbackResult(0,firstOk,first);sample.SubmitIOFeedbackResult(1,secondOk,second);Check(sample.Bitmaps().SequenceEqual(new[]{(firstOk?1:2)|(secondOk?4:8),0}),"all combined OK/NG mappings "+firstOk+"/"+secondOk);}
 }
 static void RunChecks(){
  var sample=new Harness();sample.vpWin._PMARes[0].bOk=true;sample.iJobIndex=0;sample.RunOneTimeThread(false);Check(sample.Bitmaps().SequenceEqual(new[]{1,0})&&sample.vpWin.Runs.SequenceEqual(new[]{1,0}),"actual UI single-face runner emits A output without 24V");
  sample.iJobIndex=1;sample.RunOneTimeThread(false);Check(sample.Bitmaps().SequenceEqual(new[]{1,0,8,0}),"actual UI B runner emits B output independently");
  sample=new Harness();sample.vpWin._PMARes[0].bOk=true;sample.vpWin._PMARes[1].bOk=true;sample.RunOneTimeThread(true);Check(sample.Bitmaps().SequenceEqual(new[]{5,0})&&sample.vpWin.Runs.SequenceEqual(new[]{1,1}),"actual UI sync runner emits combined A+B output without 24V");
  sample.RunOneTimeThread(true);Check(sample.Bitmaps().SequenceEqual(new[]{5,0,5,0}),"actual UI sync runner rearms for next single click");
  sample=new Harness();sample.Begin(0,true);sample._autoTriggerVisionReservedByJob[0]=true;sample._ioTriggerActiveByJob[0]=true;sample.RunOneTimeForFace(0).GetAwaiter().GetResult();Check(!sample._autoTriggerVisionReservedByJob[0]&&!sample._ioTriggerActiveByJob[0]&&sample.vpWin.bMainTaskFinished,"rejected batch releases auto reservation and trigger flags");
  sample=new Harness();sample.vpWin.ThrowClear=true;sample._autoTriggerVisionReservedByJob[0]=true;sample._ioTriggerActiveByJob[0]=true;sample.RunOneTimeForFace(0).GetAwaiter().GetResult();Check(!sample._ioTriggerActiveByJob[0]&&sample.ioFeedbackSingleJobCycles[0]==0,"failure before vision entry releases reservation and IO batch");
  sample=new Harness();sample.vpWin.Success[0]=false;sample._autoTriggerVisionReservedByJob[0]=true;sample._ioTriggerActiveByJob[0]=true;sample.RunOneTimeForFace(0).GetAwaiter().GetResult();Check(!sample._visionRunningByJob[0]&&!sample._ioTriggerActiveByJob[0]&&sample.ioFeedbackSingleJobCycles[0]==0,"vision start failure rearms without output");sample.vpWin.Success[0]=true;sample.RunOneTimeForFace(0).GetAwaiter().GetResult();Check(sample.Bitmaps().SequenceEqual(new[]{2,0}),"face can run successfully after start failure");
  sample=new Harness();sample.cup.p_CD.bListProcess[0]=false;Check(!sample.TryReserveAutoTriggerRun(0,"A")&&!sample._autoTriggerVisionReservedByJob[0],"disabled face rejected before reservation");
  sample=new Harness();sample.vpWin.TimeOut[0]=true;sample.TryReserveAutoTriggerRun(0,"A");sample.RunOneTimeForFace(0).GetAwaiter().GetResult();Check(!sample.AnyFaceVisionActive()&&!sample._ioTriggerActiveByJob[0]&&sample.ioFeedbackSingleJobCycles[0]==0&&sample.Bitmaps().Length==0,"vision timeout releases run and IO state without output");sample.vpWin.TimeOut[0]=false;Check(sample.TryReserveAutoTriggerRun(0,"A"),"next trigger accepted after vision timeout");sample.RunOneTimeForFace(0).GetAwaiter().GetResult();
  sample=new Harness();sample.cup.p_VPPRTP.bVisionInfer=true;sample.vpWin.Validity[0]=false;sample.RunOneTimeForFace(0).GetAwaiter().GetResult();Check(sample.Bitmaps().Length==0&&sample.ioFeedbackSingleJobCycles[0]==0,"invalid measurement result still cancels output");
  sample=new Harness();sample.DispatchTriggers=true;sample.Feed(Report10(1));WaitFor(()=>sample.vpWin.Runs[0]==1&&!sample._ioTriggerActiveByJob[0]);sample.Feed(Report10(1));Check(sample.vpWin.Runs[0]==1,"actual external runner does not repeat held high input");sample.Feed(Report10(0));sample.Feed(Report10(1));WaitFor(()=>sample.vpWin.Runs[0]==2&&!sample._ioTriggerActiveByJob[0]);Check(sample.Bitmaps().SequenceEqual(new[]{2,0,2,0}),"actual external runner completes two separate pulses");
  sample=new Harness();sample.DispatchTriggers=true;sample.Feed(Report10(3));WaitFor(()=>sample.vpWin.Runs[0]==1&&sample.vpWin.Runs[1]==1&&!sample.AnyFaceVisionActive());Check(sample.Bitmaps().Count(value=>value==2)==1&&sample.Bitmaps().Count(value=>value==8)==1,"both external inputs run and output their own faces");
 }
 public static void Main(){ParserChecks();RegistrationChecks();FeedbackChecks();RunChecks();Console.WriteLine("TOTAL "+checks+" checks passed");}
}
