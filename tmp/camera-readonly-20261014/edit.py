from pathlib import Path
import json, hashlib

root = Path(__file__).resolve().parents[2]
task = Path(__file__).resolve().parent
paths = ['CommonCls/HIKDeviceSource.cs', 'VisionApplication/FrmMainUI.cs', 'VisionApplication/FrmMainUIExtention.cs']
for name in paths:
    p = task / 'backup' / name
    p.parent.mkdir(parents=True, exist_ok=True)
    if not p.exists():
        p.write_bytes((root / name).read_bytes())
preserved = list((root / 'bin').rglob('*')) + list(root.glob('**/*.csproj')) + list(root.glob('*.sln')) + [root/'VisionApplication/FrmMainUI.designer.cs', root/'CommonCls/AcqFIFO.cs', root/'VisionControl/VisionControlExtention.cs']
preserved = [p for p in preserved if p.is_file() and 'tmp' not in p.relative_to(root).parts]
if not (task/'preserved-before.json').exists():
    (task/'preserved-before.json').write_text(json.dumps({str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in preserved},ensure_ascii=False,indent=2),encoding='utf-8')

def read(name):
    return (task/'backup'/name).read_text(encoding='utf-8-sig')
def write(name, s):
    original = (root/name).read_bytes()
    (root/name).write_bytes((b'\xef\xbb\xbf' if original.startswith(b'\xef\xbb\xbf') else b'') + s.replace('\n','\r\n').encode('utf-8'))
def replace(s, old, new, count=1):
    assert s.count(old) == count, (old[:100], s.count(old), count)
    return s.replace(old,new)
def method(s, signature, new):
    start=s.index(signature)
    opening=s.index('{', start)
    depth=1
    i=opening+1
    while depth:
        depth += (s[i]=='{') - (s[i]=='}')
        i+=1
    return s[:start] + new + s[i:]

s=read(paths[0])
s=replace(s, '                CheckSdkResult(cam, "PixelFormat", $"0x{fmt:X8} (候选格式)", ret);\n', '                // 候选失败属于格式适配过程；任一候选成功即成功，不产生SDK失败事件。\n')
s=replace(s, '            CheckSdkResult(cam, "Gamma.GetRange", value.ToString(), cam.MV_CC_GetGamma_NET(ref range));', '            if (!CheckSdkResult(cam, "Gamma.GetRange", value.ToString(), cam.MV_CC_GetGamma_NET(ref range))) return false;')
s=replace(s, '            SetEnumValueChecked(cam, "GammaAuto", 0);\n', '')
s=replace(s, '            success &= SetSharpness(ref param.iSharp, cam);\n', '')
s=method(s, 'public bool GetCamParam(CameraParam param, MyCamera cam)', '''public bool GetCamParam(ref CameraParam param, MyCamera cam,
            out bool autoExposure, out bool gammaReadable)
        {
            autoExposure = false;
            gammaReadable = false;
            CameraParam local = param;
            var floatVal = new MyCamera.MVCC_FLOATVALUE();
            var intVal = new MyCamera.MVCC_INTVALUE();
            var enumVal = new MyCamera.MVCC_ENUMVALUE();

            if (!CheckSdkResult(cam, "ExposureTime.Read", "read", cam.MV_CC_GetExposureTime_NET(ref floatVal))) return false;
            local.iExposeTime = floatVal.fCurValue;
            if (!CheckSdkResult(cam, "ExposureAuto.Read", "read", cam.MV_CC_GetEnumValue_NET("ExposureAuto", ref enumVal))) return false;
            autoExposure = enumVal.nCurValue != 0;
            if (!CheckSdkResult(cam, "Gain.Read", "read", cam.MV_CC_GetGain_NET(ref floatVal))) return false;
            local.dGain = floatVal.fCurValue;
            if (!CheckSdkResult(cam, "Width.Read", "read", cam.MV_CC_GetWidth_NET(ref intVal))) return false;
            local.iWidth = intVal.nCurValue;
            if (!CheckSdkResult(cam, "Height.Read", "read", cam.MV_CC_GetHeight_NET(ref intVal))) return false;
            local.iHeight = intVal.nCurValue;
            if (!CheckSdkResult(cam, "OffsetX.Read", "read", cam.MV_CC_GetIntValue_NET("OffsetX", ref intVal))) return false;
            uint offsetX = intVal.nCurValue;
            if (!CheckSdkResult(cam, "OffsetY.Read", "read", cam.MV_CC_GetIntValue_NET("OffsetY", ref intVal))) return false;
            local.iROI = new uint[] { offsetX, intVal.nCurValue, local.iWidth, local.iHeight };
            if (GetPixelFormat(ref local.iMonoType, cam) != MyCamera.MV_OK) return false;

            // Gamma 可选；不可读时不伪造数值，也不把已连接设备改判为断开。
            var access = MyCamera.MV_XML_AccessMode.AM_Undefined;
            int accessRet = cam.MV_XML_GetNodeAccessMode_NET("Gamma", ref access);
            if (accessRet != MyCamera.MV_OK ||
                (access == MyCamera.MV_XML_AccessMode.AM_RO || access == MyCamera.MV_XML_AccessMode.AM_RW))
            {
                gammaReadable = CheckSdkResult(cam, "Gamma.Read", "read", cam.MV_CC_GetGamma_NET(ref floatVal));
                if (gammaReadable) local.dGamma = floatVal.fCurValue;
            }
            // iBrightness 为项目光源亮度；锐度停用。两者均不由相机读取覆盖。
            param = local;
            return true;
        }''')
s=method(s, 'public int GetPixelFormat(ref int format, MyCamera cam)', '''public int GetPixelFormat(ref int format, MyCamera cam)
        {
            var enumVal = new MyCamera.MVCC_ENUMVALUE();
            int ret = cam.MV_CC_GetPixelFormat_NET(ref enumVal);
            if (!CheckSdkResult(cam, "PixelFormat.Read", "read", ret)) return ret;
            switch ((MyCamera.MvGvspPixelType)enumVal.nCurValue)
            {
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8:
                    format = 0;
                    return MyCamera.MV_OK;
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG8:
                    format = 1;
                    return MyCamera.MV_OK;
                default:
                    CameraDiagnostic?.Invoke($"cameraIndex={Array.IndexOf(_cameraArray, cam)}, node=PixelFormat.Read, 不支持当前格式=0x{enumVal.nCurValue:X8}，保留设备格式未写入");
                    return MyCamera.MV_E_SUPPORT;
            }
        }''')
write(paths[0],s)

s=read(paths[1])
s=replace(s,'        private bool _cameraConnectionInProgress;','''        private bool _cameraConnectionInProgress;
        private volatile bool _cameraManuallyDisconnected;
        private readonly bool[] _cameraAutoExposure = new bool[maxCameraNum];
        private readonly bool[] _cameraGammaReadable = new bool[maxCameraNum];''')
s=replace(s,'            InitializeComponent();\n','            InitializeComponent();\n            DisableUnusedCameraControls();\n')
start=s.index('        public bool SetTrigger(int iCamIndex)')
s=s[:start]+'''        private void DisableUnusedCameraControls()
        {
            chkGamma_Enable.CheckedChanged -= chkGamma_Enable_CheckedChanged;
            chkGamma_Enable.Click -= chkAutoExposure_Click;
            chkGamma_UserMode.CheckedChanged -= chkGamma_UserMode_CheckedChanged;
            chkGamma_UserMode.Click -= chkAutoExposure_Click;
            chkGamma_Enable.Visible = chkGamma_UserMode.Visible = false;
            chkGamma_Enable.Enabled = chkGamma_UserMode.Enabled = false;
            tbSharp.ValueChanged -= tbSharp_ValueChanged;
            nudSharp.ValueChanged -= nudSharp_ValueChanged;
            label39.Visible = tbSharp.Visible = nudSharp.Visible = false;
            tbSharp.Enabled = nudSharp.Enabled = false;
        }

'''+s[start:]
for sig in ['private void chkGamma_Enable_CheckedChanged(object sender, EventArgs e)', 'private void chkGamma_UserMode_CheckedChanged(object sender, EventArgs e)']:
    s=method(s,sig,sig+'\n        {\n            // 新增 Gamma 使能/模式功能停用，不调用相机SDK。\n        }')
s=s.replace('SetCamParam(cameraIndex)', 'ReadCamParam(cameraIndex)').replace('SetCamParam(iCamInd)', 'ReadCamParam(iCamInd)')
s=method(s,'private bool SetCamParam(int iCamIndex)', '''private bool ReadCamParam(int iCamIndex)
        {
            try
            {
                if (iCamIndex < 0 || iCamIndex >= maxCameraNum || cup.p_Cam == null ||
                    iCamIndex >= cup.p_Cam.Length || !chooseHIK || camera2 == null ||
                    iCamIndex >= camera2.Length || camera2[iCamIndex] == null)
                {
                    LogCameraConnection("读取相机参数", false, $"cameraIndex={iCamIndex}, 相机对象或索引无效，未调用SDK");
                    return false;
                }
                CameraParam local = cup.p_Cam[iCamIndex];
                bool autoExposure, gammaReadable;
                if (!hIKVisionCamera.GetCamParam(ref local, camera2[iCamIndex], out autoExposure, out gammaReadable)) return false;
                cup.p_Cam[iCamIndex] = local;
                _cameraAutoExposure[iCamIndex] = autoExposure;
                _cameraGammaReadable[iCamIndex] = gammaReadable;
                UpdateCameraParamCache(iCamIndex);
                if (icamSelectInd == iCamIndex) UpdateCamParamToUI();
                return true;
            }
            catch (Exception ex)
            {
                LogCameraConnection("读取相机参数", false, $"cameraIndex={iCamIndex}, exception={ex}");
                return false;
            }
        }''')
s=replace(s,'                    // 5. 一次性设置所有相机参数（包括 ROI、分辨率、曝光、增益、锐度、伽马等）','                    // 5. 只读取当前设备成像参数，不恢复项目保存值。')
s=replace(s,'                    $"参数恢复失败 cameraIndex={cameraIndex}, serial={currentSerial}"','                    $"参数读取失败 cameraIndex={cameraIndex}, serial={currentSerial}"')
# 原采集/触发业务从参数恢复入口移出，保留其调用时机与既有语义。
s=replace(s,'                MyCamera.MVCC_INTVALUE payloadParam = new MyCamera.MVCC_INTVALUE();','                if (cup.p_CD.triggleCam == cameraIndex) SetTrigger(cameraIndex);\n                MyCamera.MVCC_INTVALUE payloadParam = new MyCamera.MVCC_INTVALUE();')
s=replace(s,'                    // 6. 获取 PayloadSize（图像数据量）','                    if (cup.p_CD.triggleCam == iCamInd) SetTrigger(iCamInd);\n                    // 6. 获取 PayloadSize（图像数据量）')
s=replace(s,'                    if (rts.cs.iStatus[cameraIndex] == 3)\n                    {','                    if (rts.cs.iStatus[cameraIndex] == 3)\n                    {\n                        if (!ReadCamParam(cameraIndex)) return false;')
s=replace(s,'                _cameraConnectionInProgress = true;','''                if (operation == "自动重连" && _cameraManuallyDisconnected) return false;
                if (operation == "手动连接") _cameraManuallyDisconnected = false;
                _cameraConnectionInProgress = true;''')
s=replace(s,'            // 自动重连（仅在启用且从相机采集时）\n            if (cup.p_RTSP.bAutoConnCam)','            // 自动重连（主动断开后等待手动连接）\n            if (cup.p_RTSP.bAutoConnCam && !_cameraManuallyDisconnected)')
# 关闭流程返回真实成功状态；其余原调用者可忽略返回值。
s=replace(s,'        private void StopDevice()\n        {\n            if (!StopGrab()) return;','        private bool StopDevice()\n        {\n            if (!StopGrab()) return false;')
s=replace(s,'                    hIKVisionCamera.MultiDestroyCamera();','''                    bool released = true;
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        int stopRet, closeRet, destroyRet;
                        hIKVisionCamera.ReleaseCamera(i, out stopRet, out closeRet, out destroyRet);
                        // 未采集时 Stop 可返回调用顺序错误；断开以 Close/Destroy 真实结果为准。
                        if ((closeRet != int.MinValue && closeRet != MyCamera.MV_OK) ||
                            (destroyRet != int.MinValue && destroyRet != MyCamera.MV_OK))
                        {
                            released = false;
                            LogCameraConnection("断开相机", false, $"cameraIndex={i}, Stop=0x{unchecked((uint)stopRet):X8}, Close=0x{unchecked((uint)closeRet):X8}, Destroy=0x{unchecked((uint)destroyRet):X8}");
                        }
                    }
                    if (!released) return false;''')
s=replace(s,'                    MessageBox.Show($"关闭相机失败：{ex.Message}，请重启程序！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);\n                    return;','                    LogCameraConnection("断开相机", false, $"exception={ex}");\n                    return false;')
s=replace(s,'                acqFIFO.FreeAllBuffers();\n            }\n        }\n        //停止采集','                acqFIFO.FreeAllBuffers();\n            }\n            return true;\n        }\n        //停止采集')
s=method(s,'private void CloseCam()', '''private bool CloseCam()
        {
            if (!StopDevice()) return false;
            CloseCamStatus();
            return true;
        }''')
s=replace(s,'''            CloseCam();
            rts.initStatus.bCamInit = false;
            operLog.strOperEvent = "断开相机";
            operLog.iEventStatus = 0;
            AddLog();''','''            lock (_cameraConnectionSync)
            {
                if (_cameraConnectionInProgress)
                {
                    LogCameraConnection("断开相机", false, "相机连接操作尚未结束");
                    return;
                }
                _cameraManuallyDisconnected = true;
                _cameraConnectionInProgress = true;
            }
            try
            {
                if (!CloseCam()) return;
                rts.initStatus.bCamInit = false;
                rts.initStatus.bCamPrepared = false;
                LogCameraConnection("断开相机", true, "设备连接已释放，等待手动连接");
            }
            finally
            {
                lock (_cameraConnectionSync) _cameraConnectionInProgress = false;
            }''')
write(paths[1],s)

s=read(paths[2])
for sig in ['private void tbSharp_ValueChanged(object sender, EventArgs e)', 'private void nudSharp_ValueChanged(object sender, EventArgs e)']:
    s=method(s,sig,sig+'\n        {\n            // 锐度功能停用。\n        }')
s=replace(s,'                cup.p_Cam[icamSelectInd].iSharp = (uint)nudSharp.Value;\n','')
begin=s.index('        private void UpdateCamParamToUI()\n', s.index('        }*/',s.index('private bool _isUpdatingUI')))
prefix=s[:begin]; live=s[begin:]
live=replace(live,'''                    // 其他参数（曝光、增益等）保持不变
                    nudSharp.Value = cup.p_Cam[icamInd].iSharp;
                    nudExposure.Value = (decimal)cup.p_Cam[icamInd].iExposeTime;
                    nudGain.Value = (decimal)cup.p_Cam[icamInd].dGain;
                    nudGamma.Value = (decimal)cup.p_Cam[icamInd].dGamma;''','''                    // 同步设备真实值时仅更新控件，所有参数事件由 _isUpdatingUI 屏蔽。
                    SetCameraNumericValue(nudExposure, (decimal)cup.p_Cam[icamInd].iExposeTime);
                    SetCameraNumericValue(nudGain, (decimal)cup.p_Cam[icamInd].dGain);
                    SetCameraTrackValue(tbExposure, (int)cup.p_Cam[icamInd].iExposeTime);
                    SetCameraTrackValue(tbGain, (int)(cup.p_Cam[icamInd].dGain * 10));
                    chkAutoExposure.Checked = _cameraAutoExposure[icamInd];
                    nudExposure.Enabled = tbExposure.Enabled = !chkAutoExposure.Checked;
                    nudGamma.Enabled = tbGamma.Enabled = _cameraGammaReadable[icamInd];
                    if (_cameraGammaReadable[icamInd])
                    {
                        SetCameraNumericValue(nudGamma, (decimal)cup.p_Cam[icamInd].dGamma);
                        SetCameraTrackValue(tbGamma, (int)(cup.p_Cam[icamInd].dGamma * 10));
                    }''')
s=prefix+live
s=replace(s,'            hIKVisionCamera.GetCamParam(cup.p_Cam[icamInd], camera2[icamInd]);\n            UpdateCamParamToUI();','            if (ReadCamParam(icamInd)) UpdateCamParamToUI();')
idx=s.index('        private void SetLightBrightnessFromUI')
s=s[:idx]+'''        private static void SetCameraNumericValue(NumericUpDown control, decimal value)
        {
            if (value < control.Minimum) control.Minimum = value;
            if (value > control.Maximum) control.Maximum = value;
            control.Value = value;
        }

        private static void SetCameraTrackValue(TrackBar control, int value)
        {
            if (value < control.Minimum) control.Minimum = value;
            if (value > control.Maximum) control.Maximum = value;
            control.Value = value;
        }

'''+s[idx:]
write(paths[2],s)
print('Edited only the three authorized business files; backup and preservation hashes recorded.')
