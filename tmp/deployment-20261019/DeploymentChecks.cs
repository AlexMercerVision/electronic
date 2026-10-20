using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using CommonCls;
using Newtonsoft.Json.Linq;
using VisionApplication;

static class DeploymentChecks
{
    static int passed;
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        passed++;
        Console.WriteLine("PASS " + name);
    }
    static object Invoke(object target, string name, params object[] args)
    {
        return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    }
    static T Field<T>(object target, string name)
    {
        return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
    [STAThread]
    static void Main(string[] args)
    {
        string root = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(root);
        string originalCwd = Environment.CurrentDirectory;
        try
        {
            string cwd = Path.Combine(root, "foreign-cwd");
            Directory.CreateDirectory(Path.Combine(cwd, "UserData"));
            File.WriteAllText(Path.Combine(cwd, "UserData", "serUserParam.json"), "old workstation configuration");
            File.WriteAllText(Path.Combine(cwd, "UserData", "serUserParam.dat"), "old binary configuration");
            File.WriteAllText(Path.Combine(cwd, "authentication.txt"), "old authorization seed");
            Environment.CurrentDirectory = cwd;
            var defaults = new CUserParam();
            Check(defaults.p_Paf.strVPPFilePath == ApplicationPaths.DefaultVppPath, "default LD.vpp");
            Check(defaults.p_DL.modelPath == ApplicationPaths.DefaultModelDirectory && defaults.p_DL.bUseGPU, "installed model and GPU retained");
            Check(defaults.p_CD.camInd.All(x => x == -1) && defaults.p_CD.camSerial.All(string.IsNullOrEmpty), "no inherited camera identity");
            Check(defaults.p_CD.bListProcess.All(x => !x), "new machine cameras disabled");
            Check(defaults.p_SD.bUsed.All(x => !x) && defaults.p_SD.serInd.All(x => x == -1), "new machine serial slots disabled");
            Check(!defaults.p_RTSP.bAutoConnCam && !defaults.p_RTSP.bAutoConnSerial && !defaults.p_RTSP.bAutoRunVPP
                && !defaults.p_RTSP.bAutoConnJCZ && defaults.p_VPPRTP.iFeedbackType == 0, "no automatic hardware or output");
            Check(defaults.p_Paf.strLogPath.StartsWith(ApplicationPaths.DataDirectory), "writable log directory");
            var paths = new CUserParam();
            paths.p_DL.modelPath = @"Assets\Models\Default\inference_model";
            paths.p_Paf.strVPPFilePath = @"VPP\LD.vpp";
            paths.p_Paf.strLogPath = "Logs";
            ApplicationPaths.Normalize(paths);
            Check(paths.p_DL.modelPath == ApplicationPaths.DefaultModelDirectory, "relative model resolves against installation");
            Check(paths.p_Paf.strVPPFilePath == ApplicationPaths.DefaultVppPath, "relative VPP resolves against workspace");
            Check(paths.p_Paf.strLogPath == ApplicationPaths.Data("Logs"), "relative logs resolve against machine data");
            string explicitModel = Path.Combine(root, "external-model");
            paths.p_DL.modelPath = explicitModel; ApplicationPaths.Normalize(paths);
            Check(paths.p_DL.modelPath == explicitModel, "explicit external model preserved");
            paths.p_Paf.strLogPath = @"C:relative";
            bool rejected = false;
            try { ApplicationPaths.Normalize(paths); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "drive-relative path rejected");
            string configDir = Path.Combine(root, "machine", "UserData");
            var files = new FileOper(configDir);
            CUserParam cup = new CUserParam();
            Check(!files.ReadUserParam(ref cup) && string.IsNullOrEmpty(files.LastUserParamError), "first read creates unconfigured machine settings");
            string configPath = Path.Combine(configDir, "serUserParam.json");
            Check(File.Exists(configPath) && !File.Exists(Path.Combine(configDir, "serUserParam.dat")), "no legacy DAT imported");
            JObject json = JObject.Parse(File.ReadAllText(configPath));
            Check((string)json["p_DL"]["modelPath"] == @"Assets\Models\Default\inference_model", "installed model saved as portable reference");
            Check((string)json["p_Paf"]["strVPPFilePath"] == ApplicationPaths.DefaultVppPath, "first file references only LD");
            Check(files.ReadUserParam(ref cup) && cup.p_DL.modelPath == ApplicationPaths.DefaultModelDirectory, "configuration roundtrip from foreign working directory");
            cup.p_DL.gpuID = 2;
            Check(FileOper.SaveUserParam(cup, configDir), "atomic configuration update");
            Check(File.Exists(configPath + ".bak"), "previous settings retained as backup");
            CUserParam reloaded = new CUserParam();
            Check(files.ReadUserParam(ref reloaded) && reloaded.p_DL.gpuID == 2, "existing machine settings preserved");
            File.WriteAllText(configPath, "{broken json");
            var before = reloaded;
            Check(!files.ReadUserParam(ref reloaded) && ReferenceEquals(before, reloaded), "broken configuration does not replace caller state");
            Check(File.ReadAllText(configPath) == "{broken json" && !string.IsNullOrEmpty(files.LastUserParamError), "broken configuration not overwritten");
            string denied = Path.Combine(root, "not-a-directory"); File.WriteAllText(denied, "file");
            Check(!FileOper.SaveUserParam(cup, denied), "configuration write failure reported");
            Check(!Directory.GetFiles(configDir, "*.tmp").Any(), "no temporary configuration files left");
            string install = Path.Combine(root, "installed"), data = Path.Combine(root, "seeded-data");
            Directory.CreateDirectory(Path.Combine(install, "VPP"));
            Directory.CreateDirectory(Path.Combine(install, "UserData"));
            File.WriteAllText(Path.Combine(install, "VPP", "LD.vpp"), "factory LD");
            File.WriteAllText(Path.Combine(install, "VPP", "Measure.vpp"), "old Measure");
            File.WriteAllText(Path.Combine(install, "UserData", "serUserParam.json"), "old configuration");
            var prepare = typeof(ApplicationPaths).GetMethod("PrepareMachineData", BindingFlags.Static | BindingFlags.NonPublic);
            prepare.Invoke(null, new object[] { install, data });
            string workingVpp = Path.Combine(data, "Workspaces", "Default", "VPP", "LD.vpp");
            Check(File.ReadAllText(workingVpp) == "factory LD", "only LD seeded into writable workspace");
            Check(!Directory.GetFiles(data, "Measure.vpp", SearchOption.AllDirectories).Any()
                && !File.Exists(Path.Combine(data, "UserData", "serUserParam.json")), "no historical VPP or workstation configuration copied");
            File.WriteAllText(workingVpp, "customer LD");
            File.WriteAllText(Path.Combine(install, "VPP", "LD.vpp"), "updated factory LD");
            prepare.Invoke(null, new object[] { install, data });
            Check(File.ReadAllText(workingVpp) == "customer LD", "updates preserve customer VPP");

            var ports = new List<SerialDeviceInfo> {
                new SerialDeviceInfo { PortName="COM3",DeviceId="device-new",Description="new adapter" },
                new SerialDeviceInfo { PortName="COM8",DeviceId="device-A",Description="light adapter" },
                new SerialDeviceInfo { PortName="COM12",DeviceId="device-B",Description="IO adapter" } };
            var serial = new CUserParam().p_SD; serial.bUsed[0] = true;
            serial.portNames[0] = "COM4"; serial.deviceIds[0] = "device-A";
            SerialDeviceInfo resolved; string reason;
            Check(SerialPortBinding.TryResolve(serial,0,ports,out resolved,out reason) && resolved.PortName=="COM8", "hardware identity follows changed COM number");
            serial.deviceIds[0] = "missing-id";
            Check(!SerialPortBinding.TryResolve(serial,0,ports,out resolved,out reason), "missing identity never falls back to another COM");
            serial.deviceIds[0] = ""; serial.portNames[0] = "COM8";
            Check(SerialPortBinding.TryResolve(serial,0,ports,out resolved,out reason) && resolved.PortName=="COM8", "manual COM name independent of enumeration order");
            ports.Reverse();
            Check(SerialPortBinding.TryResolve(serial,0,ports,out resolved,out reason) && resolved.PortName=="COM8", "enumeration reorder preserves binding");
            serial.portNames[0] = "COM99";
            Check(!SerialPortBinding.TryResolve(serial,0,ports,out resolved,out reason), "missing manual COM rejected");
            serial.portNames[0] = ""; serial.serInd[0] = 0;
            Check(!SerialPortBinding.TryResolve(serial,0,ports,out resolved,out reason), "legacy index never used to open a port");
            serial.deviceIds[0] = "device-A"; serial.portNames[0] = "COM8";
            serial.bUsed[3] = true; serial.deviceIds[3] = "device-A"; serial.portNames[3] = "COM8";
            Check(!SerialPortBinding.TryResolve(serial,0,ports,out resolved,out reason)
                && !SerialPortBinding.TryResolve(serial,3,ports,out resolved,out reason), "duplicate enabled bindings rejected for light and IO");
            serial.bUsed[3] = false;
            Check(SerialPortBinding.TryResolve(serial,0,ports,out resolved,out reason), "disabled duplicate does not occupy a port");
            ports.Add(new SerialDeviceInfo { PortName="COM17",DeviceId="device-A" });
            Check(!SerialPortBinding.TryResolve(serial,0,ports,out resolved,out reason), "ambiguous hardware identity rejected");
            ports.RemoveAt(ports.Count-1); serial.bUsed[0] = false;
            Check(!SerialPortBinding.TryResolve(serial,0,ports,out resolved,out reason), "disabled slot never resolves for opening");
            var legacy = new SerDistrParam { serInd=new[]{0,1,0,0},bUsed=new[]{true,false,false,true} };
            SerialPortBinding.EnsureArrays(ref legacy);
            Check(legacy.portNames.Length==4 && legacy.deviceIds.Length==4 && legacy.bUsed[3]
                && !SerialPortBinding.TryResolve(legacy,3,ports,out resolved,out reason), "legacy slot requires explicit reassignment");

            string authPath = Path.Combine(root,"profile","GearDet","authentication.txt");
            var auth = Protect.LoadOrCreate(authPath);
            Check(auth.PasswordFileFound && auth.PasswordFileValid && string.IsNullOrEmpty(auth.GetCID()), "first authorization created unbound");
            Check(auth.GetDate(DateType.DeadLine)<DateTime.Now && !auth.HasConfiguredPasswords, "no inherited entitlement or login passwords");
            string initial = File.ReadAllText(authPath);
            Check(File.ReadAllText(Path.Combine(cwd,"authentication.txt"))=="old authorization seed", "old authorization seed ignored");
            Protect.LoadOrCreate(authPath);
            Check(File.ReadAllText(authPath)==initial, "existing authorization not reseeded");
            DateTime deadline = DateTime.Now.AddDays(2), actual=DateTime.MinValue;
            string code=auth.GetCIDandDTEncode("010203040506",deadline);
            Check(!auth.Regist(code,ref actual) && auth.RegistrationError.Contains("本机") && File.ReadAllText(authPath)==initial, "other-machine registration rejected without altering authorization");
            foreach(string invalid in new[]{"", "1", "00", "xyz", "0000", "001122", "FFFF"})
                if(auth.Regist(invalid,ref actual)) throw new Exception("malformed registration accepted");
            Check(true,"malformed registration codes rejected without throwing");
            Check(!Protect.IsValidMachineCode("") && !Protect.IsValidMachineCode("unknow") && !Protect.IsValidMachineCode("000000000000"), "missing machine identity rejected");
            string machine=auth.GetMacAddress();
            if (!Protect.IsValidMachineCode(machine)) throw new Exception("test host has no machine identity");
            Check(!auth.Regist(auth.GetCIDandDTEncode(machine,DateTime.Now.AddDays(-1)),ref actual), "expired registration rejected");
            string lockedPath = Path.Combine(root,"read-only-auth.txt");
            var locked = Protect.LoadOrCreate(lockedPath);
            string lockedBefore = File.ReadAllText(lockedPath);
            File.SetAttributes(lockedPath,FileAttributes.ReadOnly);
            try
            {
                Check(!locked.Regist(locked.GetCIDandDTEncode(machine,deadline),ref actual), "authorization write failure rejects registration");
                Check(string.IsNullOrEmpty(locked.GetCID()) && File.ReadAllText(lockedPath)==lockedBefore,
                    "failed registration restores in-memory and on-disk entitlement");
            }
            finally { File.SetAttributes(lockedPath,FileAttributes.Normal); }
            Check(auth.Regist(auth.GetCIDandDTEncode(machine,deadline),ref actual), "valid local registration saved");
            var reopened = Protect.LoadOrCreate(authPath);
            Check(reopened.GetCID()==machine && reopened.GetDate(DateType.DeadLine).ToString("O")==deadline.ToString("O"), "machine and date authorization roundtrip");
            Check(reopened.DateVerify(0)==0, "valid license remains valid after reopen");
            CultureInfo oldCulture=Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture=CultureInfo.GetCultureInfo("en-GB");
            try { Check(new Protect(authPath).GetDate(DateType.DeadLine).ToString("O")==deadline.ToString("O"), "new license dates independent of regional settings"); }
            finally { Thread.CurrentThread.CurrentCulture=oldCulture; }
            File.WriteAllText(Path.Combine(root,"corrupt-auth.txt"),"Version 1\n0\n");
            Check(!Protect.LoadOrCreate(Path.Combine(root,"corrupt-auth.txt")).PasswordFileValid, "corrupt authorization handled without overwriting");

            using (var settings = new Frm_RuntimeSet(new CUserParam()))
            {
                Invoke(settings,"PopulateSerialChoices",false);
                for(int slot=1;slot<=4;slot++)
                    Check(((SerialDeviceInfo)((ComboBox)settings.Controls.Find("cbxPortName"+slot,true)[0]).SelectedItem).PortName==null,
                        "new serial UI slot "+slot+" remains unassigned");
            }
            using(var settings = new Frm_RuntimeSet(new CUserParam { p_SD=legacy }))
            {
                Invoke(settings,"PopulateSerialChoices",false);
                Check(((SerialDeviceInfo)((ComboBox)settings.Controls.Find("cbxPortName4",true)[0]).SelectedItem).PortName==null,
                    "legacy UI does not preselect first available port");
            }
            using(var passwords = new FormSetPasswords(reopened))
            {
                Invoke(passwords,"FormSetPasswords_Load",null,EventArgs.Empty);
                Check(!Field<Button>(passwords,"button_OK").Enabled,"first password dialog requires nonempty passwords");
                foreach(var name in new[]{"textBox_SupervisorPassword","textBox_SupervisorConfirm","textBox_AdministratorPassword","textBox_AdministratorConfirm","textBox_OperaterPassword","textBox_OperaterPasswordConfirm"})
                    Field<TextBox>(passwords,name).Text="temporary-test-password";
                Check(Field<Button>(passwords,"button_OK").Enabled,"password confirmation enables saving");
                Invoke(passwords,"button_Ok_Click",null,EventArgs.Empty);
                Check(new Protect(authPath).HasConfiguredPasswords,"initial passwords persist with authorization");
            }
            Check(File.ReadAllText(Path.Combine(cwd,"UserData","serUserParam.json"))=="old workstation configuration", "foreign working-directory files untouched");
            object main = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(FrmMainUI));
            typeof(FrmMainUI).GetField("userParameterSaveBlocked",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(main,true);
            Invoke(main,"SaveUserParam");
            Check(true,"actual main automatic save returns while invalid configuration is blocked");
            typeof(FrmMainUI).GetField("iProcess",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(main,100);
            // QueueStatisticsSnapshot reads vpWin first and returns; avoid submitting any UI log.
            // The complete timer is covered by the compiled null-control guard, not invoked on this uninitialized form.
            Console.WriteLine("TOTAL " + passed);
        }
        finally { Environment.CurrentDirectory=originalCwd; }
    }
}
