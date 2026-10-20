from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
def read(name):
    return (ROOT/name).read_text(encoding='utf-8-sig')
def write(name, text):
    p=ROOT/name
    bom=p.read_bytes().startswith(b'\xef\xbb\xbf')
    p.write_bytes((b'\xef\xbb\xbf' if bom else b'')+text.replace('\r\n','\n').replace('\n','\r\n').encode('utf-8'))
def replace(text, old, new, count=1):
    assert text.count(old)==count, (old[:110],text.count(old),count)
    return text.replace(old,new)

p='CommonCls/CommonCls.csproj'; s=read(p)
s=replace(s,'    <Compile Include="CUserParam.cs" />',
    '    <Compile Include="ApplicationPaths.cs" />\n    <Compile Include="SerialPortBinding.cs" />\n    <Compile Include="CUserParam.cs" />')
write(p,s)

p='CommonCls/CUserParam.cs'; s=read(p)
s=replace(s,'        public int[] serInd;','        public int[] serInd; // Legacy cache only; never used to reopen a device.\n        [OptionalField]\n        public string[] portNames;\n        [OptionalField]\n        public string[] deviceIds;')
s=replace(s,'                p_CD.camInd[i] = i;','                p_CD.camInd[i] = -1;')
s=replace(s,'                p_CD.bListProcess[i] = true;','                p_CD.bListProcess[i] = false;')
a=s.index('            p_Paf.strVPPFilePath =',s.index('public void InitPathAndFile()')); b=s.index('\n        }',a)
s=s[:a]+'''            p_Paf.strVPPFilePath = ApplicationPaths.DefaultVppPath;
            p_Paf.strSigSavePath = ApplicationPaths.Data("Images", "Single");
            p_Paf.strConSavePath = ApplicationPaths.Data("Images", "Continuous");
            p_Paf.strLogPath = ApplicationPaths.Data("Logs");
            p_Paf.strResSavePath = ApplicationPaths.WorkingDataDirectory;
            p_Paf.strPlaybackPath = ApplicationPaths.Data("Images", "Playback");
            p_Paf.strErrPath = ApplicationPaths.Data("Images", "Error");
            p_Paf.strExportPath = ApplicationPaths.Data("Export");
            p_Paf.MarkPicPath = ApplicationPaths.Data("Annotations", "Images");
            p_Paf.AutoMarkPath = ApplicationPaths.Data("Annotations", "Auto");
            p_Paf.ManuMarkPath = ApplicationPaths.Data("Annotations", "Manual");'''+s[b:]
s=replace(s,'            p_VPPRTP.bRecord = true;','            p_VPPRTP.bRecord = false;')
s=replace(s,'            p_VPPRTP.iFeedbackType = 1;','            p_VPPRTP.iFeedbackType = 0;')
s=replace(s,'            p_SD.bUsed = new bool[4];','            p_SD.bUsed = new bool[4];\n            p_SD.portNames = new string[4];\n            p_SD.deviceIds = new string[4];')
s=replace(s,'                p_SD.serInd[i] = i;\n                p_SD.bUsed[i] = true;','                p_SD.serInd[i] = -1;\n                p_SD.bUsed[i] = false;')
s=replace(s,'            p_DL.modelPath = @"H:\\st\\xb\\XBmodel\\infer\\inference_model";', '            p_DL.modelPath = ApplicationPaths.DefaultModelDirectory;')
for flag in ['bAutoConnCam','bAutoConnSerial','bAutoConnJCZ','bAutoConnMotionCard','bAutoRunVPP']:
    s=replace(s,'            p_RTSP.'+flag+' = true;','            p_RTSP.'+flag+' = false;')
a=s.index('        #region 旧版二进制文件迁移'); b=s.index('        #region 兼容性检查',a)
s=s[:a]+'''        // No automatic import from the installation directory or legacy DAT files on a new PC.
        private readonly string userDataDirectory;
        private static readonly object userParamWriteSync = new object();
        public string LastUserParamError { get; private set; }

        public FileOper() : this(ApplicationPaths.UserDataDirectory) { }
        public FileOper(string directory)
        {
            userDataDirectory = Path.GetFullPath(directory);
        }

        public bool ReadUserParam(ref CUserParam cup)
        {
            string path = Path.Combine(userDataDirectory, "serUserParam.json");
            LastUserParamError = "";
            if (File.Exists(path)) return ReadUserParam(path, ref cup);
            cup = new CUserParam();
            if (!SaveUserParam(cup, userDataDirectory))
                LastUserParamError = "无法创建新机参数，请检查数据目录写入权限：" + path;
            return false; // New parameters still require manual device assignment.
        }

        public bool ReadUserParam(string strUserParam, ref CUserParam cup)
        {
            LastUserParamError = "";
            try
            {
                CUserParam loaded = DeserializeFromJson(File.ReadAllText(strUserParam));
                if (loaded == null) throw new InvalidDataException("用户参数为空");
                CheckParamCompatibility(loaded);
                ApplicationPaths.Normalize(loaded);
                cup = loaded;
                return true;
            }
            catch (Exception ex)
            {
                // Leave the original file and caller's parameters intact on invalid JSON or IO failure.
                LastUserParamError = "读取参数失败：" + strUserParam + Environment.NewLine + ex;
                return false;
            }
        }

        public static bool SaveUserParam(CUserParam cup)
        {
            return SaveUserParam(cup, ApplicationPaths.UserDataDirectory);
        }

        public static bool SaveUserParam(CUserParam cup, string dir)
        {
            try
            {
                lock (userParamWriteSync)
                {
                    ApplicationPaths.Normalize(cup);
                    ApplicationPaths.WriteTextAtomic(Path.Combine(dir, "serUserParam.json"), SerializeToJson(cup));
                }
                return true;
            }
            catch (Exception) { return false; }
        }

'''+s[b:]
s=replace(s,'            if (cup.p_SD.stationCardMap == null || cup.p_SD.stationCardMap.Length < 2)',
    '            SerialPortBinding.EnsureArrays(ref cup.p_SD);\n            if (cup.p_SD.stationCardMap == null || cup.p_SD.stationCardMap.Length < 2)')
s=replace(s,'                cup.p_Paf.ManuMarkPath = @"..\\..\\EZDModel";',
    '                cup.p_Paf.ManuMarkPath = ApplicationPaths.Data("Annotations", "Manual");')
write(p,s)

p='VisionApplication/FrmMainUI.cs'; s=read(p)
s=replace(s,'new Protect("C:\\\\Users\\\\GearDet\\\\authentication.txt")','new Protect(ApplicationPaths.AuthorizationFile)')
s=replace(s,'            FileOper.MigrateOldBinaryIfNeeded();\n','')
# Normalize configured paths without guessing another drive when a file is missing.
a=s.index('        private void ReplaceDrive()'); b=s.index('        //初始化用户参数',a)
s=s[:a]+'''        private void ReplaceDrive()
        {
            ApplicationPaths.Normalize(cup);
        }

'''+s[b:]
s=replace(s,'            InitUserParam(); ','''            ApplicationPaths.PrepareMachineData();
            InitUserParam(); ''')
s=replace(s,'            RuntimeLog.Configure(cup.p_Paf.strLogPath);\n            string strPath',
'''            RuntimeLog.Configure(cup.p_Paf.strLogPath);
            if (!string.IsNullOrEmpty(fo.LastUserParamError))
                MessageBox.Show(fo.LastUserParamError, "用户参数", MessageBoxButtons.OK, MessageBoxIcon.Error);
            string strPath''')
a=s.index('            //用户app数据目录，不要求管理员权限',s.index('private void RoleCheck()')); b=s.index('            string strMAC =',a)
s=s[:a]+'''            rts.initStatus.bModelRoleCert = false;
            rts.iModelRole = -1;
            try
            {
                mPasswordFile = Protect.LoadOrCreate(ApplicationPaths.AuthorizationFile);
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "初始化授权文件";
                operLog.strFailReason = ex.ToString();
                AddLog();
                MessageBox.Show("无法读写授权文件：" + ApplicationPaths.AuthorizationFile + "\\n" + ex.Message,
                    "软件授权", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
'''+s[b:]
s=replace(s,'            if (strMAC == strEnableMAC || strMAC == authorizationManagerMac',
    '            if (Protect.IsValidMachineCode(strMAC) && (strMAC == strEnableMAC || strMAC == authorizationManagerMac')
s=replace(s,'|| strMAC == "00E269A99B04")  //','|| strMAC == "00E269A99B04"))  //')
s=replace(s,'            if (rts.initStatus.bModelRoleCert)  // 是合法授权用户，但有可能授权过期或者篡改日期',
    '            if (rts.initStatus.bModelRoleCert && EnsureInitialPasswords())  // 授权及首次密码设置完成后初始化')
marker='        bool bLoadLaser = false;'
s=replace(s,marker,'''        private bool EnsureInitialPasswords()
        {
            if (rts.iModelRole < 0) return false;
            if (mPasswordFile.HasConfiguredPasswords) return true;
            MessageBox.Show("首次授权后，请设置操作员、管理员和主管密码。", "首次设置",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            using (var passwords = new FormSetPasswords(mPasswordFile))
                if (passwords.ShowDialog() == DialogResult.OK && mPasswordFile.HasConfiguredPasswords) return true;
            rts.iModelRole = -1;
            return false;
        }

'''+marker)
s=replace(s,'                RoleCheck();\n            }\n            operLog.iEventStatus = rts.iModelRole;',
'''                RoleCheck();
                if (rts.initStatus.bModelRoleCert && EnsureInitialPasswords())
                    MessageBox.Show("授权已保存，请重新启动软件完成初始化。", "软件授权");
            }
            operLog.iEventStatus = rts.iModelRole;''')
s=replace(s,'            if (!vpWin.bMainTaskFinished)\n            {\n                operLog.iEventStatus = -1;\n                operLog.strOperEvent = "用户参数设置";',
    '            if (vpWin != null && !vpWin.bMainTaskFinished)\n            {\n                operLog.iEventStatus = -1;\n                operLog.strOperEvent = "用户参数设置";')
s=replace(s,'            Frm_RuntimeSet frs = new Frm_RuntimeSet(cup);',
    '            string previousSerial = JsonConvert.SerializeObject(cup.p_SD);\n            Frm_RuntimeSet frs = new Frm_RuntimeSet(cup);')
s=replace(s,'                vpWin._cup = cup;\n                serialInd = cup.p_SD.serInd;',
'''                if (vpWin != null) vpWin._cup = cup;
                if (previousSerial != JsonConvert.SerializeObject(cup.p_SD)) disconnect();
                serialInd = cup.p_SD.serInd;''')
# Reconnect and first connect share the same physical-device resolver.
a=s.index('        private bool InitSerialIndex(int i)'); b=s.index('        private void RegisterIOReceive()',a)
s=s[:a]+'''        private bool InitSerialIndex(int i)
        {
            if (i < 0 || i >= sPortAll.Length) return false;
            SerialDeviceInfo device;
            string error;
            if (!SerialPortBinding.TryResolve(cup.p_SD, i, SerialPortBinding.Enumerate(), out device, out error))
            {
                LogInvalidSerialConfigurationOnce(i, error);
                return false;
            }
            serialConfigErrorLogged[i] = false;
            if (sPortAll[i] == null) sPortAll[i] = new SerialPort();
            bool opened = OpenSerialPort(sPortAll[i], device.PortName);
            if (i == 0) csd._sPort = sPortMain = sPortAll[i];
            if (i == 1 || i == 2) { sPortSub[i - 1] = sPortAll[i]; csd._sPortSub = sPortSub; }
            if (i == 3) RegisterIOReceive();
            return opened;
        }

'''+s[b:]
a=s.index('        private void LogInvalidSerialConfigurationOnce('); b=s.index('        private void InitSerial()',a)
s=s[:a]+'''        private void LogInvalidSerialConfigurationOnce(int serialSlot, string reason)
        {
            if (serialConfigErrorLogged[serialSlot]) return;
            serialConfigErrorLogged[serialSlot] = true;
            operLog.iEventStatus = -1;
            operLog.strOperEvent = $"串口{serialSlot + 1}配置";
            operLog.strFailReason = reason;
            AddLog();
        }

'''+s[b:]
s=replace(s,'            List<string> availablePorts = GetSortedPorts();','            List<SerialDeviceInfo> availablePorts = SerialPortBinding.Enumerate();')
s=replace(s,'string.Join(", ", availablePorts)','string.Join(", ", availablePorts.Select(port => port.PortName))')
a=s.index('                // 获取配置的串口索引',s.index('private void InitSerial()')); b=s.index('                serialConfigErrorLogged[i] = false;',a)
s=s[:a]+'''                SerialDeviceInfo device;
                string reason;
                if (!SerialPortBinding.TryResolve(cup.p_SD, i, availablePorts, out device, out reason))
                {
                    LogInvalidSerialConfigurationOnce(i, reason);
                    UpdateSerialStatusIndicator(i, SerialStatus.Failed);
                    continue;
                }
                string portName = device.PortName;

'''+s[b:]
s=replace(s,'                string dataDir = Path.Combine(Application.StartupPath, "data");',
    '                string dataDir = ApplicationPaths.WorkingDataDirectory;')
write(p,s)
