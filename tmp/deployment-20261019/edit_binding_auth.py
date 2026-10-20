from edit_support import read, write, replace

p='VisionApplication/Frm_RuntimeSet.cs'; s=read(p)
s=replace(s,'        string[] strPortName;', '        string[] strPortName;\n        private List<SerialDeviceInfo> serialDevices = new List<SerialDeviceInfo>();')
a=s.index('        private List<string> GetAvailableSerialPorts()'); b=s.index('        private bool ValidateBoardAssignParam()',a)
s=s[:a]+'''        private List<string> GetAvailableSerialPorts()
        {
            serialDevices = SerialPortBinding.Enumerate();
            return serialDevices.Select(device => device.PortName).ToList();
        }

        private ComboBox SerialCombo(int slot)
        {
            return (ComboBox)Controls.Find("cbxPortName" + (slot + 1), true)[0];
        }

        private void PopulateSerialChoices(bool preserveSelection)
        {
            SerialPortBinding.EnsureArrays(ref cup.p_SD);
            strPortName = GetAvailableSerialPorts().ToArray();
            for (int slot = 0; slot < 4; slot++)
            {
                ComboBox combo = SerialCombo(slot);
                SerialDeviceInfo previous = preserveSelection ? combo.SelectedItem as SerialDeviceInfo : null;
                string name = previous != null ? previous.PortName : cup.p_SD.portNames[slot];
                string identity = previous != null ? previous.DeviceId : cup.p_SD.deviceIds[slot];
                combo.Items.Clear();
                combo.Items.Add(new SerialDeviceInfo());
                foreach (var device in serialDevices) combo.Items.Add(device);
                var matches = serialDevices.Where(device => !string.IsNullOrEmpty(identity)
                    ? string.Equals(device.DeviceId, identity, StringComparison.OrdinalIgnoreCase)
                    : !string.IsNullOrEmpty(name) && string.Equals(device.PortName, name, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matches.Count == 1) combo.SelectedItem = matches[0];
                else if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(identity))
                {
                    var missing = new SerialDeviceInfo { PortName = name, DeviceId = identity,
                        Description = "原绑定设备未连接或无法唯一确认" };
                    combo.Items.Add(missing);
                    combo.SelectedItem = missing;
                }
                else combo.SelectedIndex = 0;
            }
        }

        private void RefreshSerialPortComboBoxes()
        {
            PopulateSerialChoices(true);
        }

        private bool ValidateSerialConfiguration()
        {
            SerDistrParam candidate = cup.p_SD;
            candidate.portNames = new string[4];
            candidate.deviceIds = new string[4];
            candidate.bUsed = new bool[4];
            for (int slot = 0; slot < 4; slot++)
            {
                var selected = SerialCombo(slot).SelectedItem as SerialDeviceInfo;
                candidate.portNames[slot] = selected?.PortName;
                candidate.deviceIds[slot] = selected?.DeviceId;
                candidate.bUsed[slot] = ((CheckBox)Controls.Find("chkSerUsed" + (slot + 1), true)[0]).Checked;
            }
            var available = SerialPortBinding.Enumerate();
            for (int slot = 0; slot < 4; slot++)
            {
                if (!candidate.bUsed[slot]) continue;
                SerialDeviceInfo device;
                string reason;
                if (SerialPortBinding.TryResolve(candidate, slot, available, out device, out reason)) continue;
                MessageBox.Show($"串口槽{slot + 1}：{reason}", "串口分配", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

'''+s[b:]
s=replace(s,'        private void btOK_Click(object sender, EventArgs e)\n        {',
    '        private void btOK_Click(object sender, EventArgs e)\n        {\n            DialogResult = DialogResult.None;\n            if (!ValidateSerialConfiguration()) return;')
s=replace(s,'''                FileOper.SaveUserParam(cup);
                this.Close();''', '''                if (!FileOper.SaveUserParam(cup))
                {
                    MessageBox.Show("参数保存失败，请检查数据目录写入权限。", "保存参数");
                    return;
                }
                DialogResult = DialogResult.OK;
                this.Close();''')
s=replace(s,'                this.Close();\n\n            }\n            \n        }',
    '                this.Close();\n\n            }\n            else DialogResult = DialogResult.OK;\n        }')
s=replace(s,'''                    if (strPortName.Length > 0)
                        cup.p_SD.serInd[i] = ((ComboBox)this.Controls.Find("cbxPortName" + (i + 1).ToString(), true)[0]).SelectedIndex;

''','''                    SerialPortBinding.EnsureArrays(ref cup.p_SD);
                    var selected = SerialCombo(i).SelectedItem as SerialDeviceInfo;
                    cup.p_SD.portNames[i] = selected?.PortName ?? "";
                    cup.p_SD.deviceIds[i] = selected?.DeviceId ?? "";
                    cup.p_SD.serInd[i] = Array.IndexOf(strPortName, cup.p_SD.portNames[i]);

''')
a=s.index('                for (int i = 0; i < strPortName.Length; i++)',s.index('private void SetSerialParam()'))
b=s.index('                cbxBaudRate1.Text',a)
s=s[:a]+'''                PopulateSerialChoices(false);
                for (int i = 0; i < 4; i++)
                    ((CheckBox)Controls.Find("chkSerUsed" + (i + 1), true)[0]).Checked = cup.p_SD.bUsed[i];
'''+s[b:]
s=replace(s,'"UserData\\\\serRecordList.dat"','Path.Combine(ApplicationPaths.UserDataDirectory, "serRecordList.dat")',3)
s=replace(s,'                FileStream fs = new FileStream(Path.Combine(ApplicationPaths.UserDataDirectory, "serRecordList.dat"), FileMode.Create);',
    '                Directory.CreateDirectory(ApplicationPaths.UserDataDirectory);\n                FileStream fs = new FileStream(Path.Combine(ApplicationPaths.UserDataDirectory, "serRecordList.dat"), FileMode.Create);')
s=replace(s,'string strLineScene = "Scene\\\\" + cup.p_LI.strLineScence;',
'''string strLineScene = ApplicationPaths.Data("Scene", Path.GetFileName(cup.p_LI.strLineScence));
                if (!File.Exists(strLineScene)) strLineScene = ApplicationPaths.Resource("Scene", Path.GetFileName(cup.p_LI.strLineScence));''')
s=replace(s,'File.Copy(selectedFilePath, "Scene\\\\" + cup.p_LI.strLineScence,true);',
'''Directory.CreateDirectory(ApplicationPaths.Data("Scene"));
                File.Copy(selectedFilePath, ApplicationPaths.Data("Scene", Path.GetFileName(cup.p_LI.strLineScence)), true);''')
write(p,s)

p='CommonCls/Protect.cs'; s=read(p)
s=replace(s,'using System.IO;','using System.IO;\nusing System.Globalization;')
s=replace(s,'            mFilename = fname;','            mFilename = Path.GetFullPath(fname);',2)
marker='        public bool PasswordFileFound'
s=replace(s,marker,'''        public static Protect LoadOrCreate(string path)
        {
            var file = new Protect(path);
            if (File.Exists(path)) return file;
            // No machine binding, valid term or preset login passwords are copied from another PC.
            file.SetCID("");
            file.SetDate(DateType.DeadLine, new DateTime(2000, 1, 1));
            file.SetDate(DateType.LastLog, new DateTime(2000, 1, 1));
            try { file.WritePasswordFile(false); }
            catch (IOException) { if (!File.Exists(path)) throw; }
            return new Protect(path);
        }

        public bool HasConfiguredPasswords => Enum.GetValues(typeof(AccessLevel)).Cast<AccessLevel>()
            .All(level => !string.IsNullOrWhiteSpace(GetPasswordForAccessLevel(level)));
        public string RegistrationError { get; private set; }
        public static bool IsValidMachineCode(string code)
        {
            return !string.IsNullOrEmpty(code) && code.Length == 12 && code != "000000000000"
                && code.All(character => Uri.IsHexDigit(character));
        }

'''+marker)
s=replace(s,'            DateTime dt = DateTime.Now;', '            DateTime dt = new DateTime(2000, 1, 1);')
a=s.index('            mIsValid = false;',s.index('private void ReadPasswordFile()')); b=s.index('\n        }\n\n        private void AddString',a)
s=s[:a]+'''            try
            {
'''+s[a:b]+'''
            }
            catch (Exception)
            {
                mLoadedPasswords.Clear();
                mIsValid = false;
            }'''+s[b:]
s=replace(s,'            while (i < bytes.Length)','            while (i + 1 < bytes.Length)')
s=replace(s,'                bytes = new byte[str.Length / 2];',
    '                if (string.IsNullOrWhiteSpace(str) || str.Length % 2 != 0) { bytes = null; return false; }\n                bytes = new byte[str.Length / 2];')
a=s.index('        public void WritePasswordFile()'); b=s.index('        private bool LoadedPasswordsSameAsDefault()',a)
s=s[:a]+'''        public void WritePasswordFile()
        {
            WritePasswordFile(true);
        }

        private void WritePasswordFile(bool overwrite)
        {
            string contents = "Version 1" + Environment.NewLine;
            foreach (System.Collections.DictionaryEntry entry in mLoadedPasswords)
            {
                byte[] bytes = Encode(entry.Key.ToString(), entry.Value.ToString());
                AddBytesToString(bytes, ref contents);
                contents += Environment.NewLine;
            }
            ApplicationPaths.WriteTextAtomic(mFilename, contents, overwrite);
            mFound = true;
            mIsValid = true;
        }

'''+s[b:]
a=s.index('          public string GetMacAddress()'); b=s.index('        public string GetCIDEncode()',a)
s=s[:a]+'''        public string GetMacAddress()
        {
            try
            {
                var networks = NetworkInterface.GetAllNetworkInterfaces().Where(network =>
                    network.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                    network.NetworkInterfaceType == NetworkInterfaceType.Wireless80211).ToList();
                string bound = PasswordFileFound && PasswordFileValid ? GetCID() : "";
                if (IsValidMachineCode(bound) && networks.Any(network =>
                    string.Equals(network.GetPhysicalAddress().ToString(), bound, StringComparison.OrdinalIgnoreCase)))
                    return bound;
                return networks.OrderBy(network => network.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? 0 : 1)
                    .ThenBy(network => network.Id, StringComparer.Ordinal)
                    .Select(network => network.GetPhysicalAddress().ToString())
                    .FirstOrDefault(IsValidMachineCode) ?? "";
            }
            catch { return ""; }
        }

'''+s[b:]
s=replace(s,'            byte[] bytes = Encode(strCID, dt.ToString());',
    '            byte[] bytes = Encode(strCID, dt.ToString("O", CultureInfo.InvariantCulture));')
a=s.index('        public bool Regist(string strCode,ref DateTime dt)'); b=s.index('        private Byte[] EncodeCID',a)
s=s[:a]+'''        public bool Regist(string strCode, ref DateTime dt)
        {
            RegistrationError = "注册码格式有误";
            byte[] bytes;
            string machine, date;
            if (!ExtractBytesFromString(strCode, out bytes)) return false;
            DoXOrDecode(bytes);
            if (!Decode(bytes, out machine, out date)) return false;
            if (!IsValidMachineCode(machine) || !string.Equals(machine, GetMacAddress(), StringComparison.OrdinalIgnoreCase))
            {
                RegistrationError = "注册码不属于本机，请使用本机机器码重新申请";
                return false;
            }
            DateTime deadline;
            if ((!DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out deadline)
                && !DateTime.TryParse(date, out deadline)) || deadline < DateTime.Now)
            {
                RegistrationError = "注册码期限无效或已过期";
                return false;
            }
            var previous = new StringDictionary();
            foreach (DictionaryEntry entry in mLoadedPasswords) previous.Add((string)entry.Key, (string)entry.Value);
            try
            {
                SetCID(machine.ToUpperInvariant());
                SetDate(DateType.DeadLine, deadline);
                WritePasswordFile();
                dt = deadline;
                RegistrationError = "";
                return true;
            }
            catch (Exception ex)
            {
                mLoadedPasswords = previous;
                RegistrationError = "授权保存失败：" + ex.Message;
                return false;
            }
        }

'''+s[b:]
write(p,s)

p='VisionApplication/FrmExpiration.cs'; s=read(p)
s=replace(s,'            mPasswordFile = new Protect("authentication.txt");',
    '            mPasswordFile = Protect.LoadOrCreate(ApplicationPaths.AuthorizationFile);')
s=replace(s,'            txtCID.Text=mPasswordFile.GetCIDEncode();',
'''            if (!Protect.IsValidMachineCode(mPasswordFile.GetMacAddress()))
            {
                MessageBox.Show("未找到有效网卡机器码，请检查网卡驱动后重试。");
                return;
            }
            txtCID.Text=mPasswordFile.GetCIDEncode();''')
s=replace(s,'                MessageBox.Show("注册失败");\n            else',
    '                MessageBox.Show(mPasswordFile.RegistrationError, "注册失败");\n            else')
s=replace(s,'                MessageBox.Show("注册成功");',
    '                MessageBox.Show("注册成功");\n                DialogResult = DialogResult.OK;\n                Close();')
write(p,s)

p='VisionApplication/FormSetPasswords.cs'; s=read(p)
s=replace(s,'this.textBox_OperaterPassword.Text = mPasswordFile.GetPasswordForAccessLevel(AccessLevel.Administrator);',
    'this.textBox_OperaterPassword.Text = mPasswordFile.GetPasswordForAccessLevel(AccessLevel.Operator);')
s=replace(s,'       mPasswordFile.WritePasswordFile();', '''       try { mPasswordFile.WritePasswordFile(); }
       catch (Exception ex)
       {
           DialogResult = DialogResult.None;
           MessageBox.Show("密码保存失败：" + ex.Message, "密码设置");
       }''')
write(p,s)

p='VisionApplication/FileRW.cs'; s=read(p)
s=replace(s,'System.Environment.CurrentDirectory + "\\\\pwd"','CommonCls.ApplicationPaths.Data("UserData", "pwd")',2)
s=replace(s,'            FileStream fs = new FileStream(strPath, FileMode.Create, FileAccess.Write);',
    '            Directory.CreateDirectory(Path.GetDirectoryName(strPath));\n            FileStream fs = new FileStream(strPath, FileMode.Create, FileAccess.Write);')
write(p,s)

p='CommonCls/ClsList.cs'; s=read(p)
s=replace(s,'"data\\\\predefined_classes.txt"','Path.Combine(ApplicationPaths.WorkingDataDirectory, "predefined_classes.txt")',2)
# ClsList's editor writes machine data, never the installed model label file.
s=replace(s,'                File.WriteAllLines(Path.Combine(ApplicationPaths.WorkingDataDirectory, "predefined_classes.txt"), classNames);',
    '                Directory.CreateDirectory(ApplicationPaths.WorkingDataDirectory);\n                File.WriteAllLines(Path.Combine(ApplicationPaths.WorkingDataDirectory, "predefined_classes.txt"), classNames);')
write(p,s)
