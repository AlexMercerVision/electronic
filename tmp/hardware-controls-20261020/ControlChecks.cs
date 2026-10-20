
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;
using CommonCls;
namespace VisionApplication.Properties {
 static class Resources {public static System.Drawing.Image LightOn=null,LightOff=null;}
}
// Only the transport and SDK are simulated. Methods under test are extracted verbatim.
class SerialPort : IDisposable {
 public static HashSet<string> Denied = new HashSet<string>();
 public string PortName="COM1"; public int BaudRate,DataBits,WriteTimeout;
 public Parity Parity; public StopBits StopBits;
 public bool IsOpen,FailWrite; public int Opens,Closes,Disposals;
 public List<string> Writes=new List<string>();
 public event SerialDataReceivedEventHandler DataReceived;
 public void Open(){Opens++;if(Denied.Contains(PortName))throw new Exception("open failed");IsOpen=true;}
 public void Close(){Closes++;IsOpen=false;}
 public void Dispose(){Disposals++;}
 public void Write(string text){if(!IsOpen||FailWrite)throw new Exception("write failed");Writes.Add(text);}
}
class CSerialDef {public SerialPort _sPort,_sPortCy;public SerialPort[] _sPortSub;}
class MyCamera {public const int MV_OK=0;}
class Init {public bool bCamInit,bCamPrepared,bSerInit;}
class CS {public int[] iStatus=new int[2];}
class RTS {public Init initStatus=new Init();public CS cs=new CS();}
class Harness : Form {
 const int maxJobNum=2, lightControllerSlotCount=3, lightChannelCount=4;
 CUserParam cup=new CUserParam(); RTS rts=new RTS();
 List<string> items=new List<string>{"U3V A serial-A","U3V B serial-B"};uint cameraNum=2;
 List<int> cameraListPhysicalIndices=new List<int>(),cameraListJobIndices=new List<int>();
 ComboBox cbDeviceList=new ComboBox(),cmbLs_Channel=new ComboBox();
 TrackBar tbBirghtness=new TrackBar{Minimum=0,Maximum=255};
 NumericUpDown nudContrast=new NumericUpDown{Minimum=0,Maximum=255};
 CheckBox chkLS_Sync=new CheckBox();ToolStripButton toolBtSta_Triggle=new ToolStripButton();
 ToolStripStatusLabel toolStripStatus_Ser=new ToolStripStatusLabel();
 bool[,] lightOnStates=new bool[3,4];
 SerialPort[] sPortAll=new SerialPort[4],sPortSub=new SerialPort[2];
 SerialPort sPortMain,sPortCy;CSerialDef csd=new CSerialDef();
 OperLog operLog;List<string> logs=new List<string>();
 int icamSelectInd=-1;bool _isUpdatingLightUI,_isUpdatingUI;
 object[] camera2;List<int> starts=new List<int>();int[] sdkResults=new int[2];
 int[] sdkStatuses=new[]{3,3};
 readonly object _cameraConnectionSync=new object();bool _cameraConnectionInProgress,_cameraManuallyDisconnected;
 class Acq {public object[] _HikCam;}Acq acqFIFO=new Acq();
 class Hik {public object[] GetCameraArray(){return new object[2];}}Hik hIKVisionCamera=new Hik();
 int ConnectCalls;bool MappingValid=true;
 bool RefreshCameraDevices(string operation,out List<string> latest){latest=items;return true;}
 bool TryMapJobsByCameraSerial(List<string> latest,out bool changed){changed=false;return MappingValid;}
 bool ConnectJobCamera(int job,List<string> latest,string operation,bool mapping){ConnectCalls++;return true;}
 void SaveUserParam(){}
 void AddLog(){logs.Add(operLog.strFailReason??"");}
 void LogCameraConnection(string op,bool success,string detail){logs.Add(detail);}
 void LogLightControllerResolution(int job){}
 void UpdateCamParamToUI(){RefreshSelectedCameraLightUI();}
 void PortGetCylinder(object sender,SerialDataReceivedEventArgs e){}
 void UpdateSerialStatusIndicator(int slot,SerialStatus status){}
 enum SerialStatus {Disabled,Success,Failed}
 bool StartGrab(int cameraIndex){starts.Add(cameraIndex);rts.cs.iStatus[cameraIndex]=sdkStatuses[cameraIndex];return sdkResults[cameraIndex]==0;}
 Harness(){
  cup.p_CD.bListProcess[0]=cup.p_CD.bListProcess[1]=true;
  cup.p_CD.camInd[0]=1;cup.p_CD.camInd[1]=0;
  cup.p_LS.indent=new[]{0,10,20,30,40};
  cup.p_LS2[0].indent=new[]{0,50,60,70,80};
  cmbLs_Channel.Items.AddRange(new object[]{"CH1","CH2","CH3","CH4"});cmbLs_Channel.SelectedIndex=0;
  cbDeviceList.SelectedIndexChanged+=(a,b)=>{
    int job;if(!TryGetSelectedCameraBinding(out icamSelectInd,out job))icamSelectInd=-1;
    UpdateCamParamToUI();
  };
 }
private void RefreshCameraSelectionItems()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshCameraSelectionItems));
                return;
            }

            int previousCamera, previousJob;
            TryGetSelectedCameraBinding(out previousCamera, out previousJob);
            cbDeviceList.Items.Clear();
            cameraListPhysicalIndices.Clear();
            cameraListJobIndices.Clear();
            int selectedIndex = -1;
            for (int jobIndex = 0; jobIndex < maxJobNum; jobIndex++)
            {
                int cameraIndex = cup.p_CD.camInd[jobIndex];
                if (!cup.p_CD.bListProcess[jobIndex] || cameraIndex < 0 ||
                    cameraIndex >= cameraNum || items == null || cameraIndex >= items.Count ||
                    cameraIndex >= cup.p_Cam.Length)
                    continue;
                if (jobIndex == previousJob)
                    selectedIndex = cbDeviceList.Items.Count;
                cbDeviceList.Items.Add(items[cameraIndex]);
                cameraListPhysicalIndices.Add(cameraIndex);
                cameraListJobIndices.Add(jobIndex);
            }
            if (cbDeviceList.Items.Count > 0)
                cbDeviceList.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            else
                UpdateCamParamToUI();
        }
private bool TryConnectCameras(string operation)
        {
            lock (_cameraConnectionSync)
            {
                if (_cameraConnectionInProgress)
                {
                    LogCameraConnection(operation, false, "已有连接操作正在进行，忽略本次重复进入");
                    return false;
                }
                if (operation == "自动重连" && _cameraManuallyDisconnected) return false;
                if (operation == "手动连接") _cameraManuallyDisconnected = false;
                _cameraConnectionInProgress = true;
            }

            try
            {
                List<string> latestItems;
                if (!RefreshCameraDevices(operation, out latestItems))
                    return false;

                bool mappingChanged;
                bool mappingValid = TryMapJobsByCameraSerial(latestItems, out mappingChanged);
                if (mappingChanged)
                    SaveUserParam();

                RefreshCameraSelectionItems();
                camera2 = hIKVisionCamera.GetCameraArray();
                acqFIFO._HikCam = camera2;

                int successCount = 0;
                int enabledCount = 0;
                for (int jobIndex = 0; jobIndex < maxJobNum; jobIndex++)
                {
                    if (!cup.p_CD.bListProcess[jobIndex])
                        continue;
                    enabledCount++;
                    if (ConnectJobCamera(jobIndex, latestItems, operation, mappingValid))
                        successCount++;
                }

                bool allSucceeded = mappingValid && enabledCount > 0 && successCount == enabledCount;
                rts.initStatus.bCamPrepared = allSucceeded;
                rts.initStatus.bCamInit = allSucceeded;
                LogCameraConnection(operation, allSucceeded,
                    $"连接结果: 成功={successCount}, 启用工位={enabledCount}, 枚举设备={latestItems.Count}, 映射有效={mappingValid}");
                return allSucceeded;
            }
            finally
            {
                lock (_cameraConnectionSync)
                {
                    _cameraConnectionInProgress = false;
                }
            }
        }
private bool StartGrab()
        {
            bool allSucceeded = true;
            HashSet<int> startedCameras = new HashSet<int>();
            for (int job = 0; job < maxJobNum; job++)
            {
                if (!cup.p_CD.bListProcess[job]) continue;
                int cameraIndex = cup.p_CD.camInd[job];
                if (cameraIndex < 0 || cameraIndex >= cameraNum || camera2 == null ||
                    cameraIndex >= camera2.Length || camera2[cameraIndex] == null ||
                    rts.cs.iStatus == null || cameraIndex >= rts.cs.iStatus.Length ||
                    rts.cs.iStatus[cameraIndex] < 2)
                {
                    allSucceeded = false;
                    LogCameraConnection("启动相机采集", false,
                        $"Job{job}, cameraIndex={cameraIndex}, 相机未初始化或缓冲区未就绪，跳过启动");
                    continue;
                }
                if (startedCameras.Add(cameraIndex) && !StartGrab(cameraIndex))
                    allSucceeded = false;
            }
            bool anyEnabled = false;
            for (int job = 0; job < maxJobNum; job++)
            {
                if (!cup.p_CD.bListProcess[job]) continue;
                anyEnabled = true;
                int cameraIndex = cup.p_CD.camInd[job];
                allSucceeded &= cameraIndex >= 0 && rts.cs.iStatus != null && cameraIndex < rts.cs.iStatus.Length && rts.cs.iStatus[cameraIndex] == 3;
            }
            rts.initStatus.bCamInit = anyEnabled && allSucceeded;
            rts.initStatus.bCamPrepared = rts.initStatus.bCamInit;
            return rts.initStatus.bCamInit;
        }
private bool OpenBoundSerialPort(int slot, SerialDeviceInfo device, IList<SerialDeviceInfo> availablePorts)
        {
            List<int> sharedSlots = new List<int> { slot };
            if (slot < lightControllerSlotCount)
            {
                for (int other = 0; other < lightControllerSlotCount; other++)
                {
                    SerialDeviceInfo assigned;
                    string reason;
                    if (other != slot && SerialPortBinding.TryResolve(cup.p_SD, other, availablePorts,
                        out assigned, out reason) && string.Equals(assigned.PortName, device.PortName,
                        StringComparison.OrdinalIgnoreCase))
                        sharedSlots.Add(other);
                }
            }
            int ownerSlot = sharedSlots.Min();
            SerialPort port = sPortAll[ownerSlot];
            if (port == null) port = new SerialPort();
            foreach (int sharedSlot in sharedSlots) sPortAll[sharedSlot] = port;
            // 同一物理端口只打开一次，重连时保留所有光源槽的共同引用。
            bool opened = port.IsOpen || OpenSerialPort(port, device.PortName);
            csd._sPort = sPortMain = sPortAll[0];
            sPortSub[0] = sPortAll[1];
            sPortSub[1] = sPortAll[2];
            csd._sPortSub = sPortSub;
            if (slot == 3) RegisterIOReceive();
            return opened;
        }
private bool OpenSerialPort(SerialPort serialPort, string portName)
        {
            if (serialPort == null)
                return false;

            try
            {
                // 如果已经打开，先关闭
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }

                serialPort.PortName = portName;
                serialPort.BaudRate = cup.p_SD.BaudRate;
                serialPort.DataBits = cup.p_SD.DataBits;
                serialPort.Parity = (Parity)cup.p_SD.parity;
                serialPort.StopBits = (StopBits)cup.p_SD.stopBits;
                serialPort.WriteTimeout = cup.p_SD.WriteTimeout;
              //  serialPort.ReadTimeout = cup.p_SD.ReadTimeout;

                serialPort.Open();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打开串口{portName}失败: {ex.Message}");
                return false;
            }
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
public bool disconnect()
        {
            bool allClosed = true;
            try {
                // 光源槽可能共用同一对象，每个物理端口只关闭和释放一次。
                foreach (SerialPort port in sPortAll.Where(port => port != null).Distinct())
                {
                    if (port.IsOpen) port.Close();
                    port.Dispose();
                }
                Array.Clear(sPortAll, 0, sPortAll.Length);
                csd._sPort = sPortMain = null;
                Array.Clear(sPortSub, 0, sPortSub.Length);
                RegisterIOReceive();
                rts.initStatus.bSerInit = false;
                for (int i = 0; i < 4; i++)
                    UpdateSerialStatusIndicator(i, cup.p_SD.bUsed[i] ? SerialStatus.Failed : SerialStatus.Disabled);
              
                {
                    operLog.iEventStatus =0;
                    operLog.strOperEvent = "全部串口关闭";
                  //  operLog.strFailReason = "自动模式禁止操作";
                    AddLog();
 
                }
                toolStripStatus_Ser.Text = "串口未连接";
                toolStripStatus_Ser.ForeColor = Color.Red;
                Array.Clear(lightOnStates, 0, lightOnStates.Length);
                RefreshSelectedCameraLightUI();
                 
            }
            catch(Exception ex)
            {
                allClosed = false;
                operLog.strOperEvent = "关闭串口失败";
                operLog.strFailReason = ex.Message;
                operLog.iEventStatus = -1;
                AddLog();

            }
            return allClosed;
        

        }
private void LightControlTurnOn()
        {
            int cameraIndex;
            int lightJobIndex;
            if (!TryGetSelectedCameraBinding(out cameraIndex, out lightJobIndex))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "光源控制";
                operLog.strFailReason = "未选择相机";
                AddLog();
                return;
            }

            string failureReason;
            int controllerSlot = ResolveLightControllerSlot(lightJobIndex, out failureReason);
            if (controllerSlot < 0)
            {
                LogLightControllerResolution(lightJobIndex);
                RefreshSelectedCameraLightUI();
                return;
            }

            SerialPort port = GetLightPortForJob(lightJobIndex);
            if (port == null || !port.IsOpen)
            {
                LogLightControllerResolution(lightJobIndex);
                RefreshSelectedCameraLightUI();
                return;
            }

            int ch1, ch2, ch3, ch4;
            if (!TryGetLightValues(lightJobIndex, out ch1, out ch2, out ch3, out ch4))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "光源控制";
                operLog.strFailReason = "当前相机光源参数无效";
                AddLog();
                return;
            }

            int channel = cmbLs_Channel.SelectedIndex + 1;
            if (channel < 1 || channel > lightChannelCount)
                channel = 1;

            bool sync = chkLS_Sync.Checked;
            bool[] nextStates = new bool[lightChannelCount];
            bool allOn = true;
            for (int channelIndex = 0; channelIndex < lightChannelCount; channelIndex++)
            {
                nextStates[channelIndex] = lightOnStates[controllerSlot, channelIndex];
                allOn &= nextStates[channelIndex];
            }

            bool open = sync ? !allOn : !nextStates[channel - 1];
            if (sync)
            {
                for (int channelIndex = 0; channelIndex < lightChannelCount; channelIndex++)
                    nextStates[channelIndex] = open;
            }
            else
            {
                nextStates[channel - 1] = open;
            }

            int sendCh1 = nextStates[0] ? ch1 : 0;
            int sendCh2 = nextStates[1] ? ch2 : 0;
            int sendCh3 = nextStates[2] ? ch3 : 0;
            int sendCh4 = nextStates[3] ? ch4 : 0;
            string target = sync ? $"SYNC-{(open ? "ON" : "OFF")}" : $"CH{channel}-{(open ? "ON" : "OFF")}";
            if (!SendAllChannelsCommand(lightJobIndex, sendCh1, sendCh2, sendCh3, sendCh4, target))
            {
                RefreshSelectedCameraLightUI();
                return;
            }

            // 串口写入成功后才提交新的通道开关状态。
            for (int channelIndex = 0; channelIndex < lightChannelCount; channelIndex++)
                lightOnStates[controllerSlot, channelIndex] = nextStates[channelIndex];
            SetLightButtonState(lightJobIndex, true);
        }
private bool TryGetSelectedCameraBinding(out int cameraIndex, out int jobIndex)
        {
            cameraIndex = -1;
            jobIndex = -1;

            int listIndex = cbDeviceList.SelectedIndex;
            if (listIndex < 0 || listIndex >= cameraListPhysicalIndices.Count ||
                listIndex >= cameraListJobIndices.Count)
                return false;

            cameraIndex = cameraListPhysicalIndices[listIndex];
            jobIndex = cameraListJobIndices[listIndex];
            return cameraIndex >= 0 && cameraIndex < cup.p_Cam.Length &&
                   jobIndex >= 0 && jobIndex < maxJobNum;
        }
private int ResolveLightControllerSlot(int jobIndex, out string failureReason)
        {
            failureReason = "";
            if (jobIndex < 0 || cup.p_SD.bUsed == null)
            {
                failureReason = $"[Light] no valid light-controller slot for job={jobIndex}";
                return -1;
            }

            // 兼容旧配置：工位原来对应的前三个光源槽已启用时优先沿用。
            if (jobIndex < lightControllerSlotCount && jobIndex < cup.p_SD.bUsed.Length &&
                cup.p_SD.bUsed[jobIndex])
                return GetSharedLightControllerSlot(jobIndex);

            List<int> enabledSlots = new List<int>();
            int slotCount = Math.Min(lightControllerSlotCount, cup.p_SD.bUsed.Length);
            for (int slot = 0; slot < slotCount; slot++)
            {
                if (cup.p_SD.bUsed[slot])
                {
                    int controllerSlot = GetSharedLightControllerSlot(slot);
                    if (!enabledSlots.Contains(controllerSlot)) enabledSlots.Add(controllerSlot);
                }
            }

            if (enabledSlots.Count == 1)
                return GetSharedLightControllerSlot(enabledSlots[0]);

            if (enabledSlots.Count == 0)
            {
                failureReason = $"[Light] no valid light-controller slot for job={jobIndex}";
            }
            else
            {
                string slots = string.Join(",", enabledSlots.Select(slot => (slot + 1).ToString()));
                failureReason = $"[Light] ambiguous light-controller slots: {slots}, job={jobIndex}";
            }
            return -1;
        }
private int GetSharedLightControllerSlot(int slot)
        {
            SerialPort port = sPortAll[slot];
            if (port != null)
                for (int other = 0; other < slot; other++)
                    if (cup.p_SD.bUsed[other] && ReferenceEquals(sPortAll[other], port))
                        return other;
            return slot;
        }
private SerialPort GetLightPortForJob(int jobIndex)
        {
            string failureReason;
            int controllerSlot = ResolveLightControllerSlot(jobIndex, out failureReason);
            if (controllerSlot < 0 || sPortAll == null || controllerSlot >= sPortAll.Length)
                return null;
            return sPortAll[controllerSlot];
        }
private bool IsLightSerialConfigured(int jobIndex)
        {
            string failureReason;
            return ResolveLightControllerSlot(jobIndex, out failureReason) >= 0;
        }
private int[] GetLightValuesStorage(int controllerSlot)
        {
            if (controllerSlot == 0)
                return cup.p_LS.indent;

            int subIndex = controllerSlot - 1;
            if (subIndex >= 0 && cup.p_LS2 != null && subIndex < cup.p_LS2.Length)
                return cup.p_LS2[subIndex].indent;

            return null;
        }
private bool TryGetLightValuesForController(int controllerSlot, out int ch1, out int ch2, out int ch3, out int ch4)
        {
            ch1 = ch2 = ch3 = ch4 = 0;
            int[] indent = GetLightValuesStorage(controllerSlot);
            if (indent == null || indent.Length < 5)
                return false;

            ch1 = indent[1];
            ch2 = indent[2];
            ch3 = indent[3];
            ch4 = indent[4];
            return true;
        }
private bool TryGetLightValues(int jobIndex, out int ch1, out int ch2, out int ch3, out int ch4)
        {
            ch1 = ch2 = ch3 = ch4 = 0;
            string failureReason;
            int controllerSlot = ResolveLightControllerSlot(jobIndex, out failureReason);
            if (controllerSlot < 0)
                return false;
            return TryGetLightValuesForController(controllerSlot, out ch1, out ch2, out ch3, out ch4);
        }
private bool SaveLightValues(int jobIndex, int ch1, int ch2, int ch3, int ch4)
        {
            string failureReason;
            int controllerSlot = ResolveLightControllerSlot(jobIndex, out failureReason);
            int[] indent = GetLightValuesStorage(controllerSlot);
            if (indent == null || indent.Length < 5)
                return false;

            indent[1] = ch1;
            indent[2] = ch2;
            indent[3] = ch3;
            indent[4] = ch4;
            // 共用控制器的槽保存同一份通道值，避免下次禁用主槽后恢复旧亮度。
            for (int slot = 0; slot < lightControllerSlotCount; slot++)
            {
                if (slot == controllerSlot || !cup.p_SD.bUsed[slot] ||
                    GetSharedLightControllerSlot(slot) != controllerSlot)
                    continue;
                int[] sharedValues = GetLightValuesStorage(slot);
                if (sharedValues != null && sharedValues.Length >= 5)
                    Array.Copy(indent, 1, sharedValues, 1, lightChannelCount);
            }
            return true;
        }
private void SetLightButtonState(int jobIndex, bool available)
        {
            string failureReason;
            int controllerSlot = ResolveLightControllerSlot(jobIndex, out failureReason);
            int channel = cmbLs_Channel.SelectedIndex + 1;
            if (channel < 1 || channel > lightChannelCount)
                channel = 1;

            bool isOn = false;
            if (available && controllerSlot >= 0 && controllerSlot < lightControllerSlotCount)
            {
                if (chkLS_Sync.Checked)
                {
                    isOn = true;
                    for (int channelIndex = 0; channelIndex < lightChannelCount; channelIndex++)
                        isOn &= lightOnStates[controllerSlot, channelIndex];
                }
                else
                {
                    isOn = lightOnStates[controllerSlot, channel - 1];
                }
            }

            toolBtSta_Triggle.Enabled = available;
            toolBtSta_Triggle.Checked = isOn;
            toolBtSta_Triggle.Image = isOn
                ? global::VisionApplication.Properties.Resources.LightOn
                : global::VisionApplication.Properties.Resources.LightOff;
            toolBtSta_Triggle.ToolTipText = available
                ? (chkLS_Sync.Checked ? "打开/关闭当前控制器CH1～CH4" : $"打开/关闭当前控制器CH{channel}")
                : "当前所选相机光源串口未连接";
        }
private void RefreshSelectedCameraLightUI()
        {
            int cameraIndex;
            int jobIndex;
            bool hasBinding = TryGetSelectedCameraBinding(out cameraIndex, out jobIndex);
            SerialPort port = hasBinding ? GetLightPortForJob(jobIndex) : null;
            bool available = hasBinding && IsLightSerialConfigured(jobIndex) && port != null && port.IsOpen;

            tbBirghtness.Enabled = available;
            nudContrast.Enabled = available;
            cmbLs_Channel.Enabled = available;
            chkLS_Sync.Enabled = available;
            SetLightButtonState(jobIndex, available);

            if (!hasBinding)
                return;

            int ch1, ch2, ch3, ch4;
            if (!TryGetLightValues(jobIndex, out ch1, out ch2, out ch3, out ch4))
                return;

            int channel = cmbLs_Channel.SelectedIndex + 1;
            int[] values = { ch1, ch2, ch3, ch4 };
            int brightness = channel >= 1 && channel <= 4 ? values[channel - 1] : ch1;

            _isUpdatingLightUI = true;
            try
            {
                tbBirghtness.Value = Math.Max(tbBirghtness.Minimum, Math.Min(tbBirghtness.Maximum, brightness));
                nudContrast.Value = Math.Max(nudContrast.Minimum, Math.Min(nudContrast.Maximum, brightness));
            }
            finally
            {
                _isUpdatingLightUI = false;
            }
        }
private void GetLightOutputValues(int controllerSlot, int ch1, int ch2, int ch3, int ch4,
            out int sendCh1, out int sendCh2, out int sendCh3, out int sendCh4)
        {
            sendCh1 = lightOnStates[controllerSlot, 0] ? ch1 : 0;
            sendCh2 = lightOnStates[controllerSlot, 1] ? ch2 : 0;
            sendCh3 = lightOnStates[controllerSlot, 2] ? ch3 : 0;
            sendCh4 = lightOnStates[controllerSlot, 3] ? ch4 : 0;
        }
private bool SendAllChannelsCommand(int jobIndex, int ch1, int ch2, int ch3, int ch4, string target)
        {
            // 限制亮度范围 0-255
            ch1 = Math.Max(0, Math.Min(255, ch1));
            ch2 = Math.Max(0, Math.Min(255, ch2));
            ch3 = Math.Max(0, Math.Min(255, ch3));
            ch4 = Math.Max(0, Math.Min(255, ch4));

            string failureReason;
            int controllerSlot = ResolveLightControllerSlot(jobIndex, out failureReason);
            if (controllerSlot < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "[Light]";
                operLog.strFailReason = failureReason;
                AddLog();
                return false;
            }

            string command = $"S{ch1:D3}T{ch2:D3}T{ch3:D3}T{ch4:D3}TC#";
            SerialPort port = GetLightPortForJob(jobIndex);

            if (port == null || !port.IsOpen)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "[Light]";
                operLog.strFailReason = $"job={jobIndex} controllerSlot={controllerSlot + 1} port not-open";
                AddLog();
                return false;
            }

            try
            {
                port.Write(command);
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "[Light]";
                operLog.strFailReason = $"job={jobIndex} controllerSlot={controllerSlot + 1} port={port.PortName} " +
                    $"channel={target} values=[{ch1},{ch2},{ch3},{ch4}]";
                AddLog();
                return true;
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "[Light]";
                operLog.strFailReason = $"job={jobIndex} controllerSlot={controllerSlot + 1} port={port.PortName} " +
                    $"channel={target} write-failed: {ex.Message}";
                AddLog();
                return false;
            }
        }
private bool ApplyLightBrightness(int jobIndex, int? channel = null, int? brightness = null)
        {
            string failureReason;
            int controllerSlot = ResolveLightControllerSlot(jobIndex, out failureReason);
            if (controllerSlot < 0)
                return false;

            int ch1, ch2, ch3, ch4;
            if (!TryGetLightValuesForController(controllerSlot, out ch1, out ch2, out ch3, out ch4))
                return false;

            bool sync = chkLS_Sync.Checked;
            int targetBrightness = Math.Max(0, Math.Min(255, brightness ?? tbBirghtness.Value));
            // 如果非同步且未指定通道，则使用当前界面选中的通道
            if (!sync && !channel.HasValue)
            {
                channel = cmbLs_Channel.SelectedIndex + 1;
                if (channel < 1 || channel > lightChannelCount) channel = 1; // 默认通道1
            }

            if (sync)
            {
                ch1 = ch2 = ch3 = ch4 = targetBrightness;
            }
            else if (channel.HasValue && channel >= 1 && channel <= lightChannelCount)
            {
                switch (channel.Value)
                {
                    case 1: ch1 = targetBrightness; break;
                    case 2: ch2 = targetBrightness; break;
                    case 3: ch3 = targetBrightness; break;
                    case 4: ch4 = targetBrightness; break;
                }
            }
            else
            {
                return false;
            }

            int sendCh1, sendCh2, sendCh3, sendCh4;
            GetLightOutputValues(controllerSlot, ch1, ch2, ch3, ch4,
                out sendCh1, out sendCh2, out sendCh3, out sendCh4);
            string target = sync ? "SYNC" : $"CH{channel.Value}";
            if (!SendAllChannelsCommand(jobIndex, sendCh1, sendCh2, sendCh3, sendCh4, target))
                return false;

            // 仅在完整四通道命令发送成功后保存新的亮度设定。
            if (!SaveLightValues(jobIndex, ch1, ch2, ch3, ch4))
                return false;
            SetLightButtonState(jobIndex, true);

            int camIndex = cup.p_CD.camInd[jobIndex];
            if (camIndex >= 0 && camIndex < cup.p_Cam.Length)
                cup.p_Cam[camIndex].iBrightness = (uint)targetBrightness;
            return true;
        }
private bool ApplySelectedCameraLightBrightness(int? channel = null, int? brightness = null)
        {
            int cameraIndex;
            int jobIndex;
            if (!TryGetSelectedCameraBinding(out cameraIndex, out jobIndex))
                return false;

            bool succeeded = ApplyLightBrightness(jobIndex, channel, brightness);
            if (!succeeded)
                RefreshSelectedCameraLightUI();
            return succeeded;
        }
private void InitializeLightBrightness()
        {
            bool[] initializedSlots = new bool[lightControllerSlotCount];
            for (int jobIndex = 0; jobIndex < maxJobNum; jobIndex++)
            {
                string failureReason;
                int controllerSlot = ResolveLightControllerSlot(jobIndex, out failureReason);
                if (controllerSlot < 0 || initializedSlots[controllerSlot])
                    continue;

                int ch1, ch2, ch3, ch4;
                if (!TryGetLightValuesForController(controllerSlot, out ch1, out ch2, out ch3, out ch4))
                    continue;

                initializedSlots[controllerSlot] = true;
                if (!SendAllChannelsCommand(jobIndex, ch1, ch2, ch3, ch4, "INIT"))
                    continue;

                SaveLightValues(jobIndex, ch1, ch2, ch3, ch4);
                lightOnStates[controllerSlot, 0] = ch1 > 0;
                lightOnStates[controllerSlot, 1] = ch2 > 0;
                lightOnStates[controllerSlot, 2] = ch3 > 0;
                lightOnStates[controllerSlot, 3] = ch4 > 0;
            }

            RefreshSelectedCameraLightUI();
        }

 static int checks;
 static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static void Bind(CUserParam p,int slot,string name,string id=""){
  p.p_SD.bUsed[slot]=true;p.p_SD.portNames[slot]=name;p.p_SD.deviceIds[slot]=id;
 }
 [STAThread] static void Main(){
  var devices=new List<SerialDeviceInfo>{new SerialDeviceInfo{PortName="COM8",DeviceId="A"},new SerialDeviceInfo{PortName="COM12",DeviceId="IO"}};
  var p=new CUserParam();SerialDeviceInfo d;string error;
  Bind(p,0,"COM3","A");Bind(p,1,"COM8");Bind(p,2,"COM8","A");Bind(p,3,"COM12","IO");
  for(int slot=0;slot<4;slot++)Check(SerialPortBinding.TryResolve(p.p_SD,slot,devices,out d,out error),"resolve shared lights / independent IO slot "+slot);
  p.p_SD.deviceIds[3]="A";
  for(int slot=0;slot<4;slot++)Check(!SerialPortBinding.TryResolve(p.p_SD,slot,devices,out d,out error)&&error.Contains("IO"),"reject IO conflict from slot "+slot);
  p.p_SD.bUsed[3]=false;
  Check(SerialPortBinding.TryResolve(p.p_SD,0,devices,out d,out error)&&d.PortName=="COM8","identity follows changed COM");
  p.p_SD.deviceIds[0]="missing";
  Check(!SerialPortBinding.TryResolve(p.p_SD,0,devices,out d,out error),"missing identity never falls back to name");
  p.p_SD.deviceIds[0]="A";
  var ambiguous=new List<SerialDeviceInfo>(devices){new SerialDeviceInfo{PortName="COM9",DeviceId="A"}};
  Check(!SerialPortBinding.TryResolve(p.p_SD,0,ambiguous,out d,out error),"ambiguous identity rejected");
  p.p_SD.portNames[0]="";p.p_SD.deviceIds[0]="";p.p_SD.serInd[0]=0;
  Check(!SerialPortBinding.TryResolve(p.p_SD,0,devices,out d,out error),"legacy index cannot bind a port");
  using(var h=new Harness()){
   var handle=h.Handle;
   Check(!h.cup.p_RTSP.bAutoConnCam,"manual connection test uses new default");
   Check(h.TryConnectCameras("手动连接")&&h.ConnectCalls==2&&h.cbDeviceList.Items.Count==2,"manual connection populates camera list without InitCam");
   int cam,job;Check(h.TryGetSelectedCameraBinding(out cam,out job)&&cam==1&&job==0,"list maps swapped physical camera to Job0");
   Check(!h.tbBirghtness.Enabled&&!h.toolBtSta_Triggle.Enabled,"no serial disables light controls");
   h.cbDeviceList.SelectedIndex=1;h.RefreshCameraSelectionItems();
   Check(h.cbDeviceList.SelectedIndex==1&&h.icamSelectInd==0,"refresh preserves selected Job1");
   h.cup.p_CD.camInd[1]=1;h.RefreshCameraSelectionItems();
   Check(h.cbDeviceList.SelectedIndex==1&&h.icamSelectInd==1,"enumeration reorder preserves selected job");
   h.cup.p_CD.camInd[1]=0;
   Bind(h.cup,0,"COM8","A");Bind(h.cup,1,"COM8","A");Bind(h.cup,3,"COM12","IO");
   Check(h.OpenBoundSerialPort(1,devices[0],devices),"shared COM can be opened starting from B slot");
   var port=h.sPortAll[0];
   Check(Object.ReferenceEquals(port,h.sPortAll[1])&&port.Opens==1,"both slots share one physical open");
   Check(h.OpenBoundSerialPort(0,devices[0],devices)&&port.Opens==1&&port.Closes==0,"connecting alias never closes or reopens owner");
   h.RefreshCameraSelectionItems();h.RefreshSelectedCameraLightUI();
   Check(h.tbBirghtness.Enabled&&h.nudContrast.Enabled&&h.cmbLs_Channel.Enabled&&h.toolBtSta_Triggle.Enabled,"connected serial and selected camera enable all light controls");
   Check(port.Writes.Count==0,"list and light UI refresh issue zero serial commands");
   string reason;Check(h.ResolveLightControllerSlot(0,out reason)==0&&h.ResolveLightControllerSlot(1,out reason)==0,"two faces share controller channel state");
   h.InitializeLightBrightness();
   Check(port.Writes.Count==1&&port.Writes[0]=="S010T020T030T040TC#","shared controller initialized once using first enabled slot");
   Check(h.cup.p_LS2[0].indent.Skip(1).SequenceEqual(h.cup.p_LS.indent.Skip(1)),"successful initialization synchronizes alias settings");
   h.cmbLs_Channel.SelectedIndex=1;
   h.ApplySelectedCameraLightBrightness(null,99);
   Check(port.Writes.Last()=="S010T099T030T040TC#","B changes CH2 while preserving other physical channels");
   Check(h.cup.p_LS.indent[2]==99&&h.cup.p_LS2[0].indent[2]==99,"shared brightness persists in both slot settings");
   h.cbDeviceList.SelectedIndex=0;h.cmbLs_Channel.SelectedIndex=0;h.LightControlTurnOn();
   Check(port.Writes.Last()=="S000T099T030T040TC#","A turns off CH1 preserving B CH2");
   h.cbDeviceList.SelectedIndex=1;h.LightControlTurnOn();
   Check(port.Writes.Last()=="S010T099T030T040TC#","B sees same CH1 switch state and turns it back on");
   int count=port.Writes.Count;h.RefreshCameraSelectionItems();h.RefreshSelectedCameraLightUI();
   Check(port.Writes.Count==count,"refresh never flips light output");
   port.FailWrite=true;h.LightControlTurnOn();
   Check(h.lightOnStates[0,0],"failed switch write retains successful state");
   Check(!h.ApplySelectedCameraLightBrightness(2,123)&&h.cup.p_LS.indent[2]==99&&h.cup.p_LS2[0].indent[2]==99,"failed brightness write never saves new values");
   port.FailWrite=false;h.chkLS_Sync.Checked=true;h.LightControlTurnOn();
   Check(port.Writes.Last()=="S000T000T000T000TC#","light sync switches all physical channels");
   Check(h.OpenBoundSerialPort(3,devices[1],devices)&&!Object.ReferenceEquals(h.sPortAll[3],port),"IO has distinct physical object");
   var io=h.sPortAll[3];Check(h.disconnect(),"disconnect shared serials succeeds");
   Check(port.Closes==1&&port.Disposals==1&&io.Closes==1&&io.Disposals==1&&h.sPortAll.All(x=>x==null),"disconnect disposes each physical port once and clears aliases");
   Check(!h.toolBtSta_Triggle.Enabled&&!h.tbBirghtness.Enabled,"disconnect disables light UI");
   Check(h.OpenBoundSerialPort(1,devices[0],devices)&&!Object.ReferenceEquals(port,h.sPortAll[1])&&Object.ReferenceEquals(h.sPortAll[0],h.sPortAll[1]),"reconnect creates a fresh shared owner");
   h.RefreshSelectedCameraLightUI();Check(h.toolBtSta_Triggle.Enabled,"reconnect refresh restores light controls");
   Check(h.sPortAll[0].Writes.Count==0,"reconnect transport and UI do not send light commands");
   h.cup.p_CD.bListProcess[0]=h.cup.p_CD.bListProcess[1]=false;h.RefreshCameraSelectionItems();
   Check(h.cbDeviceList.Items.Count==0&&!h.tbBirghtness.Enabled,"no enabled jobs clears list and disables light");
  }
  using(var h=new Harness()){
   Bind(h.cup,0,"COM8","A");Bind(h.cup,1,"COM12","IO");
   h.OpenBoundSerialPort(0,devices[0],devices);h.OpenBoundSerialPort(1,devices[1],devices);
   string reason;Check(!Object.ReferenceEquals(h.sPortAll[0],h.sPortAll[1])&&h.ResolveLightControllerSlot(1,out reason)==1,"distinct light controllers retain independent objects and state");
   h.RefreshCameraSelectionItems();h.InitializeLightBrightness();
   Check(h.sPortAll[0].Writes.Single()=="S010T020T030T040TC#"&&h.sPortAll[1].Writes.Single()=="S050T060T070T080TC#","distinct controllers retain own channel values");
  }
  using(var h=new Harness()){
   Bind(h.cup,1,"COM8","A");Bind(h.cup,2,"COM8","A");
   h.OpenBoundSerialPort(2,devices[0],devices);string reason;
   Check(h.ResolveLightControllerSlot(0,out reason)==1&&h.ResolveLightControllerSlot(1,out reason)==1,"two alias slots count as one fallback controller");
  }
  using(var h=new Harness()){
   Bind(h.cup,1,"COM8","A");
   SerialPort.Denied.Add("COM8");Check(!h.OpenBoundSerialPort(1,devices[0],devices),"open failure returned");
   h.RefreshCameraSelectionItems();Check(!h.tbBirghtness.Enabled&&!h.toolBtSta_Triggle.Enabled,"failed open never enables light");
   SerialPort.Denied.Clear();
  }
  using(var h=new Harness()){
   h.cup.p_CD.bListProcess[0]=h.cup.p_CD.bListProcess[1]=false;
   Check(!h.StartGrab()&&h.starts.Count==0,"enumerated cameras without enabled jobs never start");
   h.cup.p_CD.bListProcess[0]=true;
   Check(!h.StartGrab()&&h.starts.Count==0,"null camera array never reaches SDK");
   h.camera2=new object[0];Check(!h.StartGrab()&&h.starts.Count==0,"empty camera array never reaches SDK");
   h.camera2=new object[2];Check(!h.StartGrab()&&h.starts.Count==0,"missing camera object never reaches SDK");
   h.camera2[1]=new object();h.rts.cs.iStatus[1]=1;
   Check(!h.StartGrab()&&h.starts.Count==0,"unprepared buffer never reaches SDK");
   h.rts.cs.iStatus[1]=2;Check(h.StartGrab()&&h.starts.SequenceEqual(new[]{1}),"only enabled mapped ready camera starts");
   h.starts.Clear();h.cup.p_CD.bListProcess[1]=true;h.cup.p_CD.camInd[1]=-1;
   Check(!h.StartGrab()&&h.starts.SequenceEqual(new[]{1})&&h.rts.cs.iStatus[1]==3,"invalid second job does not stop healthy camera");
   h.starts.Clear();h.cup.p_CD.camInd[1]=0;h.camera2[0]=new object();h.rts.cs.iStatus[0]=2;h.sdkResults[0]=-1;h.sdkStatuses[0]=1;
   Check(!h.StartGrab()&&h.starts.SequenceEqual(new[]{1,0})&&h.rts.cs.iStatus[1]==3,"one start failure preserves other camera");
   h.starts.Clear();h.cup.p_CD.camInd[0]=99;h.cup.p_CD.bListProcess[1]=false;
   Check(!h.StartGrab()&&h.starts.Count==0,"out of range physical index skipped");
   h.cup.p_CD.camInd[0]=0;h.rts.cs.iStatus=null;
   Check(!h.StartGrab()&&h.starts.Count==0,"missing status array skipped");
  }
  Console.WriteLine("TOTAL "+checks+" passed");
 }
}
