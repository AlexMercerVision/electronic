using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
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
            mFilename = fname;
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
            mLoadedPasswords[dtType.ToString()] = dt.ToString();
        }
        //设置机器码
        public void SetCID( string strCID)
        {
            mLoadedPasswords["CID"] = strCID;
        }

        //获取期限
        public DateTime GetDate(DateType dtType)
        {
            DateTime dt = DateTime.Now;
            try
            {
                dt = DateTime.Parse(mLoadedPasswords[dtType.ToString()]);
            }
            catch
            {

            }
            return dt;
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

            StringDictionary filePasswords = new StringDictionary();

            // read _filename and fill _loadedPasswords
            ArrayList fileLines = new ArrayList();
            string line;
            try
            {
                using (StreamReader sr = new StreamReader(mFilename))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        fileLines.Add(line);
                    }
                }
            }
            catch (Exception)
            {
                return;
            }

            // first line is "Version x"
            if (fileLines.Count < 1)
                return;
            line = (string)fileLines[0];
            if (!line.StartsWith("Version "))
                return;
            line = line.Substring(8);

            int ver = -1;
            try
            {
                ver = int.Parse(line);
            }
            catch (Exception) { return; }

            // only version 1 supported at this time
            if (ver != 1)
                return;

            // next lines are password lines
            for (int i = 1; i < fileLines.Count; ++i)
            {
                // line to bytes
                byte[] bytes;
                if (!ExtractBytesFromString((string)fileLines[i], out bytes))
                    return;

                // doxor
                this.DoXOrDecode(bytes);

                // decode accesslevel & password
                AccessLevel level;
                string pass;
                string key;
                if (!this.Decode(bytes, out key, out pass))
                    return;

                // put in table
                filePasswords.Add(key, pass);
            }

            mIsValid = true;
            this.mLoadedPasswords = filePasswords;
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
            while (i < bytes.Length)
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
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string fname = appData + "\\GearDet\\authentication.txt";
            if (this.mFound)
                fname = this.mFilename;

            // check if passwords are same as default - if same as default and we
            // don't have a file open, then don't write a file
            if (!this.mFound && LoadedPasswordsSameAsDefault())
                return;

            string fileContents = "";
            fileContents += "Version 1" + System.Environment.NewLine;

            foreach (System.Collections.DictionaryEntry de in this.mLoadedPasswords)
            {
                byte[] bytes = Encode(de.Key.ToString(), de.Value.ToString());
                AddBytesToString(bytes, ref fileContents);
                fileContents += System.Environment.NewLine;
            }

            using (StreamWriter sw = new StreamWriter(fname))
            {
                sw.Write(fileContents);
            }

            // tbd - or read password file in again
            mFound = true;
            mFilename = fname;
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
                     NetworkInterface[] networks = NetworkInterface.GetAllNetworkInterfaces();
                     foreach (NetworkInterface network in networks)
                     {
                         if (network.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                         {
                             return network.GetPhysicalAddress().ToString();
                         }
                     }
                     return string.Empty;
                 }
                 catch
                 {
                     return "unknow";
                 }
                 finally
                 {
                 }
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
            byte[] bytes = Encode(strCID, dt.ToString());
            string strCIDandDTEncode = "";
            AddBytesToString(bytes, ref strCIDandDTEncode);
            return strCIDandDTEncode;
        }
        public bool Regist(string strCode,ref DateTime dt)
        {
            byte[] bytes;
            if (!ExtractBytesFromString(strCode, out bytes))
                return false;

            // doxor
            this.DoXOrDecode(bytes);
 
            string pass;
            string key;
            if (!this.Decode(bytes, out key, out pass))
                return false;
            
            try
            {
                dt = DateTime.Parse(pass);
                if (dt < DateTime.Now)
                    return false;
            }
            catch
            {
                return false;
            }
            SetCID(key);
            SetDate(DateType.DeadLine, dt);
            WritePasswordFile();
            return true;
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
            if (iModelRole == 10)
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
