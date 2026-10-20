from edit_support import read, write, replace
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
