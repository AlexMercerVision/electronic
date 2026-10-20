using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Globalization;
using System.Management;

using System.Net.NetworkInformation;

namespace CommonCls
{
    public enum AccessLevel { Operator, Administrator, Supervisor }
    public enum DateType { DeadLine, LastLog, Now }
    public  class Protect
    {
        private string mFilename;
        private bool mFound;
        private bool mIsValid;
        private StringDictionary mDefaultPasswords;
        private StringDictionary mLoadedPasswords;

        public Protect(string fname)
        {
            mFilename = Path.GetFullPath(fname);
            mFound = false;
            mIsValid = false;
            mDefaultPasswords = new StringDictionary();
            mLoadedPasswords = new StringDictionary();

            // look for file & validate & load passwords
            if (File.Exists(mFilename))
            {
                mFound = true;
                ReadPasswordFile();
            }
        }

        public static Protect LoadOrCreate(string path)
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

        public bool PasswordFileFound
        {
            get { return mFound; }
        }

        public string PasswordFilename
        {
            get { return mFilename; }
        }

        public bool PasswordFileValid
        {
            get { return mIsValid; }
        }

        public void SetDefaultPassword(AccessLevel level, string pass)
        {
            this.mDefaultPasswords.Add(level.ToString(), pass);
        }

        public string GetPasswordForAccessLevel(AccessLevel level)
        {
            if (PasswordFileFound && PasswordFileValid)
            {
                return this.mLoadedPasswords[level.ToString()];
            }

            return this.mDefaultPasswords[level.ToString()];
        }

        public void SetPasswordForAccessLevel(AccessLevel level, string pass)
        {
            mLoadedPasswords[level.ToString()] = pass;
        }

        //设置期限
        public void SetDate(DateType dtType, DateTime dt)
        {
            mLoadedPasswords[dtType.ToString()] = dt.ToString("O", CultureInfo.InvariantCulture);
        }
        //设置机器码
        public void SetCID( string strCID)
        {
            mLoadedPasswords["CID"] = strCID;
        }

        //获取期限
        public DateTime GetDate(DateType dtType)
        {
            DateTime value;
            string text = mLoadedPasswords[dtType.ToString()];
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value)
                || DateTime.TryParse(text, out value)) return value;
            return new DateTime(2000, 1, 1);
        }

        //获取机器码
        public string GetCID()
        {
          return  mLoadedPasswords["CID"];
        }


        private void ReadPasswordFile()
        {
            mIsValid = false;
            mLoadedPasswords.Clear();
            try
            {
                string[] lines = File.ReadAllLines(mFilename);
                int version;
                if (lines.Length == 0 || !lines[0].StartsWith("Version ")
                    || !int.TryParse(lines[0].Substring(8), out version) || version != 1) return;
                var loaded = new StringDictionary();
                for (int index = 1; index < lines.Length; index++)
                {
                    byte[] bytes;
                    string key, value;
                    if (!ExtractBytesFromString(lines[index], out bytes)) return;
                    DoXOrDecode(bytes);
                    if (!Decode(bytes, out key, out value)) return;
                    loaded.Add(key, value);
                }
                mLoadedPasswords = loaded;
                mIsValid = true;
            }
            catch (Exception)
            {
                mLoadedPasswords.Clear();
                mIsValid = false;
            }
        }

        private void AddString(byte[] bytes, ref int i, string s)
        {
            foreach (char c in s)
            {
                UInt16 val = (UInt16)c;

                byte high = (byte)(val >> 8);
                byte low = (byte)(val & 0xff);
                bytes[i] = low;
                ++i;
                bytes[i] = high;
                ++i;
            }

            bytes[i] = 0;
            ++i;
            bytes[i] = 0;
            ++i;
        }

        private bool RemoveString(byte[] bytes, ref int i, out string s)
        {
            s = "";
            while (i + 1 < bytes.Length)
            {
                byte low = bytes[i];
                ++i;
                byte high = bytes[i];
                ++i;

                UInt16 val = (UInt16)((high << 8) + low);
                if (val == 0)
                    return true;

                s += (char)val;
            }

            return false;
        }

        private void DoXOrEncode(byte[] bytes)
        {
            byte xor = 0x3c;
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = (byte)(bytes[i] ^ xor);
                xor += bytes[i];
            }
        }

        private void DoXOrDecode(byte[] bytes)
        {
            byte xor = 0x3c;
            for (int i = 0; i < bytes.Length; ++i)
            {
                byte encodedval = bytes[i];
                bytes[i] = (byte)(bytes[i] ^ xor);
                xor += encodedval;
            }
        }

        private static int seed = unchecked((int)DateTime.Now.Ticks);
        private Byte[] Encode(string level, string pass)
        {
            int len = level.Length + 1 + pass.ToString().Length + 1;
            len *= 2;
            if (len < 64)
                len = 64;

            // create & fill with random - careful to use a seed that changes
            byte[] bytes = new byte[len];
            Random rand = new Random(seed);
            ++seed;
            rand.NextBytes(bytes);

            // access level, zero, password, zero
            int i = 0;
            AddString(bytes, ref i, level);
            AddString(bytes, ref i, pass);

            // simple xor mod
            DoXOrEncode(bytes);

            return bytes;
        }

      
        private bool Decode(byte[] bytes, out string key, out string value)
        {
            string accessLevelString;
            value = "";
            key = "";
            int i = 0;
            if (!RemoveString(bytes, ref i, out key))
                return false;
            if (!RemoveString(bytes, ref i, out value))
                return false;

            //   public enum AccessLevel { Operator, Administrator, Supervisor }
            //   public enum DateType { DeadLine, LastLog, Now }

          
            return true;
        }


        private bool DecodeOld(byte[] bytes, out AccessLevel level, out string pass)
        {
            string accessLevelString;
            pass = "";
            level = AccessLevel.Operator;

            int i = 0;
            if (!RemoveString(bytes, ref i, out accessLevelString))
                return false;
            if (!RemoveString(bytes, ref i, out pass))
                return false;

            try
            {
                object obj = Enum.Parse(typeof(AccessLevel), accessLevelString, true);
                level = (AccessLevel)obj;
            }
            catch (Exception) { return false; }

            return true;
        }

        private void AddBytesToString(byte[] bytes, ref string str)
        {
            foreach (byte b in bytes)
            {
                str += b.ToString("X2");
            }
        }

        private bool ExtractBytesFromString(string str, out byte[] bytes)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(str) || str.Length % 2 != 0) { bytes = null; return false; }
                bytes = new byte[str.Length / 2];

                for (int i = 0; i < str.Length; i += 2)
                {
                    string bstr = str.Substring(i, 2);
                    bytes[i / 2] = byte.Parse(bstr, System.Globalization.NumberStyles.HexNumber);
                }
            }
            catch (Exception)
            {
                bytes = null;
                return false;
            }

            return true;
        }

        public void WritePasswordFile()
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

        private bool LoadedPasswordsSameAsDefault()
        {
            if (this.mLoadedPasswords.Count != this.mDefaultPasswords.Count)
                return false;

            foreach (System.Collections.DictionaryEntry de in this.mLoadedPasswords)
            {
                if (de.Value.ToString() != mDefaultPasswords[de.Key.ToString()])
                    return false;
            }
            return true;
        }

       
        public string GetMacAddress()
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

        public string GetCIDEncode()
        {
            string cid = GetMacAddress();
           byte[] bytes= EncodeCID(cid);
            string strCIDEncode = "";
            AddBytesToString(bytes, ref strCIDEncode);
            return strCIDEncode;
        }
        public string GetCIDDecode(string strCIDEncode)
        {
            byte[] bytes;
            ExtractBytesFromString(strCIDEncode, out bytes);

            string cid="" ;
            if (bytes != null)
            {
                DecodeCID(bytes, out cid);
            }
            return cid;
        }

        public string GetCIDandDTEncode(string strCID, DateTime dt)
        { 
            byte[] bytes = Encode(strCID, dt.ToString("O", CultureInfo.InvariantCulture));
            string strCIDandDTEncode = "";
            AddBytesToString(bytes, ref strCIDandDTEncode);
            return strCIDandDTEncode;
        }
        public bool Regist(string strCode, ref DateTime dt)
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

        private Byte[] EncodeCID(string pass)
        {
            int len = pass.ToString().Length + 1;
            len *= 2;
            /*  if (len < 64)
                  len = 64;*/

            // create & fill with random - careful to use a seed that changes
            byte[] bytes = new byte[len];
            Random rand = new Random(seed);
            ++seed;
            rand.NextBytes(bytes);

            // access level, zero, password, zero
            int i = 0;
            AddString(bytes, ref i, pass);

            return bytes;
        }
        private bool DecodeCID(byte[] bytes, out string value)
        {
            int i = 0;
            if (!RemoveString(bytes, ref i, out value))
                return false;
            return true;
        }
   
        //直接写入机器码，不经过加密机器码转换
        public void WriteCID()
        {
            string cid = GetMacAddress();
            SetCID(cid);
            WritePasswordFile(); 
        }
        
        DateTime dtLast;
        public int DateVerify(int iModelRole)
        {
            dtLast = GetDate(DateType.LastLog);
            DateTime dtDeadLine = GetDate(DateType.DeadLine);
            DateTime dtNow = DateTime.Now;
            SetDate(DateType.LastLog, dtNow);
            WritePasswordFile();
            if (iModelRole == 9)
                return 0;
            if (dtNow < dtLast)
                return 1;  //修改过系统时间
            else if (dtNow > dtDeadLine)
                return 2;   //权限过期
            else
            {
                
                return 0;
            }

        }
    }
}
