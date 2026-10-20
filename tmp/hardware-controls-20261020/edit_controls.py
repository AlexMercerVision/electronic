from pathlib import Path

root = Path(__file__).resolve().parents[2]
def read(name):
    return (root / name).read_bytes().decode('utf-8-sig')
def write(name, text):
    path = root / name
    bom = b'\xef\xbb\xbf' if path.read_bytes().startswith(b'\xef\xbb\xbf') else b''
    path.write_bytes(bom + text.encode('utf-8'))
def replace(text, old, new):
    old, new = old.replace('\n', '\r\n'), new.replace('\n', '\r\n')
    assert text.count(old) == 1, (old[:100], text.count(old))
    return text.replace(old, new)

name = 'VisionApplication/FrmMainUI.cs'
s = read(name)
start = s.index('            // ✅ 更新相机列表信息')
end = s.index('            // 更新状态栏显示', start)
s = s[:start] + '            RefreshCameraSelectionItems();\r\n\r\n' + s[end:]
pos = s.index('        private bool InitJobCamInd(int ind)')
helper = '''        private void RefreshCameraSelectionItems()
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
                if (jobIndex == previousJob && cameraIndex == previousCamera)
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

'''.replace('\n', '\r\n')
s = s[:pos] + helper + s[pos:]
s = replace(s, '                cameraNum = hIKVisionCamera.CameraNum();', '''                items = hIKVisionCamera.EnumDevices();
                cameraNum = (uint)items.Count;''')
s = replace(s, '''            if(vpWin!=null)
                vpWin._cameraNum = cameraNum;
        }''', '''            if(vpWin!=null)
                vpWin._cameraNum = cameraNum;
            RefreshCameraSelectionItems();
        }''')
s = replace(s, '''                chooseHIK = true;
                return true;
            }
            catch (Exception ex)
            {
                cameraNum = 0;''', '''                chooseHIK = true;
                items = latestItems;
                return true;
            }
            catch (Exception ex)
            {
                cameraNum = 0;''')
s = replace(s, '''            // 某一工位的绑定缺失时只拒绝该工位''', '''            RefreshCameraSelectionItems();
            // 某一工位的绑定缺失时只拒绝该工位''')
s = replace(s, '''                camera2 = hIKVisionCamera.GetCameraArray();
                acqFIFO._HikCam = camera2;''', '''                RefreshCameraSelectionItems();
                camera2 = hIKVisionCamera.GetCameraArray();
                acqFIFO._HikCam = camera2;''')
s = replace(s, '''                ApplyJobSelection(iJobIndex);

                //VPP文件改变''', '''                ApplyJobSelection(iJobIndex);
                RefreshCameraSelectionItems();

                //VPP文件改变''')
s = replace(s, '''                StartGrab(); 
                
          //''', '''                if (cup.p_RTSP.bAutoConnCam) StartGrab();
                
          //''')
s = replace(s, '''            for (int iCamInd = 0; iCamInd < cameraNum; iCamInd++)
            {
                if (!StartGrab(iCamInd))
                    allSucceeded = false;
            }''', '''            HashSet<int> startedCameras = new HashSet<int>();
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
            }''')
s = replace(s, 'allSucceeded &= cameraIndex >= 0 && cameraIndex < rts.cs.iStatus.Length', 'allSucceeded &= cameraIndex >= 0 && rts.cs.iStatus != null && cameraIndex < rts.cs.iStatus.Length')
# Both initial connection and automatic reconnection use the same physical owner.
s = replace(s, '''            if (!SerialPortBinding.TryResolve(cup.p_SD, i, SerialPortBinding.Enumerate(), out device, out error))''', '''            List<SerialDeviceInfo> availablePorts = SerialPortBinding.Enumerate();
            if (!SerialPortBinding.TryResolve(cup.p_SD, i, availablePorts, out device, out error))''')
s = replace(s, '''            if (sPortAll[i] == null) sPortAll[i] = new SerialPort();
            bool opened = OpenSerialPort(sPortAll[i], device.PortName);
            if (i == 0) csd._sPort = sPortMain = sPortAll[i];
            if (i == 1 || i == 2) { sPortSub[i - 1] = sPortAll[i]; csd._sPortSub = sPortSub; }
            if (i == 3) RegisterIOReceive();
            return opened;''', '''            bool opened = OpenBoundSerialPort(i, device, availablePorts);
            RefreshSelectedCameraLightUI();
            return opened;''')
pos = s.index('        private void RegisterIOReceive()')
helper = '''        private bool OpenBoundSerialPort(int slot, SerialDeviceInfo device, IList<SerialDeviceInfo> availablePorts)
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

'''.replace('\n', '\r\n')
s = s[:pos] + helper + s[pos:]
s = replace(s, '''            // 初始化串口数组
            for (int i = 0; i < 4; i++)
            {
                sPortAll[i] = new SerialPort();
            }
            RegisterIOReceive();''', '''            RegisterIOReceive();''')
s = replace(s, '                if (OpenSerialPort(sPortAll[i], portName))', '                if (OpenBoundSerialPort(i, device, availablePorts))')
start = s.index('                for (int i = 0; i < 4; i++)', s.index('        public bool disconnect()'))
end = s.index('              \r\n                {', start)
s = s[:start] + '''                // 光源槽可能共用同一对象，每个物理端口只关闭和释放一次。
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
'''.replace('\n', '\r\n') + s[end:]
write(name, s)

name = 'VisionApplication/FrmMainUIExtention.cs'
s = read(name)
s = replace(s, '                return jobIndex;', '                return GetSharedLightControllerSlot(jobIndex);')
s = replace(s, '                return enabledSlots[0];', '                return GetSharedLightControllerSlot(enabledSlots[0]);')
pos = s.index('        private SerialPort GetLightPortForJob')
helper = '''        private int GetSharedLightControllerSlot(int slot)
        {
            SerialPort port = sPortAll[slot];
            if (port != null)
                for (int other = 0; other < slot; other++)
                    if (cup.p_SD.bUsed[other] && ReferenceEquals(sPortAll[other], port))
                        return other;
            return slot;
        }

'''.replace('\n', '\r\n')
s = s[:pos] + helper + s[pos:]
write(name, s)

name = 'CommonCls/SerialPortBinding.cs'
s = read(name)
newline = '\r\n' if '\r\n' in s else '\n'
s = s.replace('if (other != slot && ResolveOne', 'if (other != slot && (slot == 3 || other == 3) && ResolveOne')
s = s.replace('重复绑定{device.PortName}，请重新分配', '重复绑定{device.PortName}；IO串口必须独占，请重新分配')
write(name, s)
