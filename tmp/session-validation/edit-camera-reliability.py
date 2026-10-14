from pathlib import Path

def edit(path, action):
    p = Path(path)
    raw = p.read_bytes()
    bom = raw.startswith(b'\xef\xbb\xbf')
    source = raw.decode('utf-8-sig').replace('\r\n', '\n')
    source = action(source)
    p.write_bytes((b'\xef\xbb\xbf' if bom else b'') + source.replace('\n', '\r\n').encode('utf-8'))

def section(source, begin, end, value):
    a = source.index(begin)
    b = source.index(end, a)
    return source[:a] + value + source[b:]

def main(source):
    source = section(source, '        private bool SetCamParam(int iCamIndex)', '        private bool StartGrab(int iCamInd)', '''        private bool SetCamParam(int iCamIndex)
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

''')
    source = source.replace('                SyncCameraStatus(iCamInd, success ? 3 : 1);\n', '')
    source = source.replace('                string runtimeState = GetCameraRuntimeState(iCamInd);', '''                string runtimeState;
                bool stateRead = GetCameraRuntimeState(iCamInd, out runtimeState);
                success &= stateRead;
                SyncCameraStatus(iCamInd, success ? 3 : 1);''')
    source = source.replace('                AddLog();\n                return success;\n', '''                WriteCameraDiagnostic(operLog.strOperEvent, operLog.iEventStatus, operLog.strFailReason);
                AddLog();
                return success;
''', 1)
    source = source.replace('                    $"cameraArrayLength={(camera2 == null ? 0 : camera2.Length)}, exception={ex}";\n                AddLog();', '                    $"cameraArrayLength={(camera2 == null ? 0 : camera2.Length)}, exception={ex}";\n                WriteCameraDiagnostic(operLog.strOperEvent, -1, operLog.strFailReason);\n                AddLog();', 1)
    source = section(source, '        private string GetCameraRuntimeState(int cameraIndex)', '        /// <summary>\n        /// 同步相机状态', '''        private bool GetCameraRuntimeState(int cameraIndex, out string state)
        {
            state = "相机对象无效";
            if (camera2 == null || cameraIndex < 0 || cameraIndex >= camera2.Length || camera2[cameraIndex] == null)
                return false;
            try
            {
                MyCamera camera = camera2[cameraIndex];
                string[] nodes = { "AcquisitionMode", "TriggerMode", "TriggerSource", "PixelFormat" };
                List<string> values = new List<string>();
                bool success = true;
                foreach (string node in nodes)
                {
                    MyCamera.MVCC_ENUMVALUE value = new MyCamera.MVCC_ENUMVALUE();
                    int ret = camera.MV_CC_GetEnumValue_NET(node, ref value);
                    success &= hIKVisionCamera.CheckSdkResult(camera, node + ".ReadBack", "read", ret);
                    values.Add(ret == MyCamera.MV_OK ? $"{node}={value.nCurValue}" : $"{node}=读取失败(0x{unchecked((uint)ret):X8})");
                }
                state = string.Join(", ", values);
                return success;
            }
            catch (Exception ex)
            {
                state = $"cameraIndex={cameraIndex}, 状态读回异常: {ex}";
                LogCameraConnection("采集状态读回", false, state);
                return false;
            }
        }

''')
    source = section(source, '        private void StopGrab()', '        private void CloseCamStatus()', '''        private bool StopGrab()
        {
            if (!chooseHIK) return true;
            try
            {
                bool stopped = true;
                for (int i = 0; i < maxCameraNum; i++)
                    stopped &= vpWin.previewOnlive(false, i);
                if (stopped) hikCanGrab = false;
                return stopped;
            }
            catch (Exception ex)
            {
                LogCameraConnection("停止预览", false, $"exception={ex}");
                return false;
            }
        }

''')
    source = source.replace('                    StopGrab();\n                    // vpWin.OpenOrCloseCam(false);', '                    if (!StopGrab()) return false;\n                    // vpWin.OpenOrCloseCam(false);')
    source = source.replace('            StopGrab();\n            StopDevice();', '            if (!StopGrab()) return;\n            StopDevice();')
    source = source.replace('        private void StopDevice()\n        {', '        private void StopDevice()\n        {\n            if (!StopGrab()) return;')
    return source

edit('VisionApplication/FrmMainUI.cs', main)
