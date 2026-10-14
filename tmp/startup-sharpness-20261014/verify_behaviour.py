from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[2]
task = Path(__file__).resolve().parent

def method(path, signature):
    text = path.read_text(encoding='utf-8-sig')
    start = text.index(signature)
    brace = text.index('{', start)
    depth = 1
    end = brace + 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}')
        end += 1
    return text[start:end]

sharp = method(root / 'CommonCls/HIKDeviceSource.cs', 'public bool SetSharpness(ref uint value, MyCamera cam, bool skipUnavailable)')
history = method(root / 'VisionApplication/FrmMainUI.cs', 'private void ReadUserParam()')
source = r'''
using System;
using System.Collections.Generic;
class MyCamera {
    public const int MV_OK = 0, MV_E_SUPPORT = unchecked((int)0x80000001);
    public enum MV_XML_AccessMode { AM_NI, AM_NA, AM_WO, AM_RO, AM_RW, AM_Undefined }
    public struct MVCC_INTVALUE { public uint nMin, nMax, nCurValue; }
    public bool Connected = true, Enabled = true;
    public int SharpAccessRet, EnableAccessRet, AutoAccessRet, BoolRet, RangeRet, WriteRet, AutoRet;
    public MV_XML_AccessMode SharpAccess = MV_XML_AccessMode.AM_RW, EnableAccess = MV_XML_AccessMode.AM_RW, AutoAccess = MV_XML_AccessMode.AM_RW;
    public int Queries, BoolReads, RangeReads, Writes, AutoWrites;
    public uint Target;
    public bool MV_CC_IsDeviceConnected_NET() { return Connected; }
    public int MV_XML_GetNodeAccessMode_NET(string name, ref MV_XML_AccessMode mode) {
        Queries++;
        if(name == "Sharpness") { mode=SharpAccess; return SharpAccessRet; }
        if(name == "SharpnessEnable") { mode=EnableAccess; return EnableAccessRet; }
        mode=AutoAccess; return AutoAccessRet;
    }
    public int MV_CC_GetBoolValue_NET(string name, ref bool enabled) { BoolReads++; enabled=Enabled; return BoolRet; }
    public int MV_CC_GetSharpness_NET(ref MVCC_INTVALUE range) { RangeReads++; if(RangeRet==0) { range.nMin=1; range.nMax=10; } return RangeRet; }
    public int MV_CC_SetSharpness_NET(uint target) { Writes++; Target=target; return WriteRet; }
    public int MV_CC_SetEnumValue_NET(string name, uint target) { AutoWrites++; return AutoRet; }
}
class SharpnessCheck {
    public List<string> Failures = new List<string>();
    private bool CheckSdkResult(MyCamera camera, string node, string target, int ret) { if(ret!=0) Failures.Add(node); return ret==0; }
    private bool SetEnumValueChecked(MyCamera camera, string node, uint value) { return CheckSdkResult(camera,node,value.ToString(),camera.MV_CC_SetEnumValue_NET(node,value)); }
    private uint Clamp(uint value, uint min, uint max) { return Math.Max(min,Math.Min(max,value)); }
    SHARP_METHOD
}
struct InitStatus { public bool bSerInit, bUserParamFileInit; }
struct Runtime { public InitStatus initStatus; public int detS; }
class UserParam { public Paths p_Paf = new Paths(); }
class Paths { public string strLogPath = "unchanged-log-path"; }
class FileOper {
    public bool UserResult=true, HistoricalSerial=true;
    public bool ReadUserParam(ref UserParam cup) { return UserResult; }
    public void ReadRunTimeStatus(string path, ref Runtime rts) { rts.initStatus.bSerInit=HistoricalSerial; rts.detS=37; }
}
class StartupCheck {
    public FileOper fo=new FileOper(); public UserParam cup=new UserParam(); public Runtime rts; public int detStat;
    public void Run() { ReadUserParam(); }
    HISTORY_METHOD
}
class Tests {
    static int passed;
    static void Assert(bool ok, string name) { if(!ok) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    static bool Apply(MyCamera c, SharpnessCheck h, ref uint value, bool optional) { return h.SetSharpness(ref value,c,optional); }
    static void Main() {
        foreach(var mode in new[]{MyCamera.MV_XML_AccessMode.AM_NI,MyCamera.MV_XML_AccessMode.AM_NA,MyCamera.MV_XML_AccessMode.AM_RO}) {
            var c=new MyCamera { SharpAccess=mode }; var h=new SharpnessCheck(); uint v=6;
            Assert(Apply(c,h,ref v,true) && v==6 && c.RangeReads==0 && c.Writes==0 && c.AutoWrites==0 && h.Failures.Count==0,"optional-unavailable-"+mode);
        }
        {
            var c=new MyCamera { Enabled=false }; var h=new SharpnessCheck(); uint v=0;
            Assert(Apply(c,h,ref v,true) && v==0 && c.Writes==0 && c.AutoWrites==0 && h.Failures.Count==0,"disabled-feature-preserves-zero-and-no-errors");
        }
        {
            var c=new MyCamera { Enabled=false }; var h=new SharpnessCheck(); uint v=6;
            Assert(Apply(c,h,ref v,true) && v==6 && c.Writes==0 && h.Failures.Count==0,"startup-does-not-enable-previously-disabled-feature");
        }
        {
            var c=new MyCamera(); var h=new SharpnessCheck(); uint v=99;
            Assert(Apply(c,h,ref v,true) && v==10 && c.Target==10 && c.Writes==1 && c.AutoWrites==1 && h.Failures.Count==0,"enabled-feature-clamps-and-writes-original-values");
        }
        {
            var c=new MyCamera { AutoAccess=MyCamera.MV_XML_AccessMode.AM_NI }; var h=new SharpnessCheck(); uint v=6;
            Assert(Apply(c,h,ref v,true) && c.Target==6 && c.AutoWrites==0 && h.Failures.Count==0,"missing-auto-node-does-not-block-supported-sharpness");
        }
        {
            var c=new MyCamera { AutoAccess=MyCamera.MV_XML_AccessMode.AM_RO }; var h=new SharpnessCheck(); uint v=6;
            Assert(Apply(c,h,ref v,true) && c.AutoWrites==0 && c.Writes==1 && h.Failures.Count==0,"read-only-auto-node-is-not-written");
        }
        {
            var c=new MyCamera { EnableAccess=MyCamera.MV_XML_AccessMode.AM_NI }; var h=new SharpnessCheck(); uint v=6;
            Assert(Apply(c,h,ref v,true) && c.BoolReads==0 && c.Writes==1 && h.Failures.Count==0,"camera-without-enable-node-keeps-original-write");
        }
        {
            var c=new MyCamera { SharpAccessRet=MyCamera.MV_E_SUPPORT, EnableAccessRet=MyCamera.MV_E_SUPPORT, AutoAccessRet=MyCamera.MV_E_SUPPORT }; var h=new SharpnessCheck(); uint v=6;
            Assert(Apply(c,h,ref v,true) && c.Writes==1 && c.AutoWrites==1 && h.Failures.Count==0,"unavailable-access-query-falls-back-to-original-calls");
        }
        {
            var c=new MyCamera { RangeRet=unchecked((int)0x80000106) }; var h=new SharpnessCheck(); uint v=6;
            Assert(!Apply(c,h,ref v,true) && v==6 && c.Writes==0 && h.Failures.Count==1 && h.Failures[0]=="Sharpness.GetRange","real-range-failure-does-not-write-zero");
        }
        {
            var c=new MyCamera { WriteRet=unchecked((int)0x80000004) }; var h=new SharpnessCheck(); uint v=99;
            Assert(!Apply(c,h,ref v,true) && v==99 && h.Failures.Count==1 && h.Failures[0]=="Sharpness","real-write-failure-is-retained-without-saving-adjustment");
        }
        {
            var c=new MyCamera { Connected=false }; var h=new SharpnessCheck(); uint v=6; bool threw=false;
            try { Apply(c,h,ref v,true); } catch(InvalidOperationException) { threw=true; }
            Assert(threw && c.Queries==0 && c.Writes==0,"disconnected-device-is-not-treated-as-optional-skip");
        }
        {
            var c=new MyCamera { WriteRet=unchecked((int)0x80000106), SharpAccess=MyCamera.MV_XML_AccessMode.AM_NI }; var h=new SharpnessCheck(); uint v=6;
            Assert(!Apply(c,h,ref v,false) && c.Queries==0 && c.Writes==1 && h.Failures.Count==1,"explicit-ui-setting-keeps-real-write-failure");
        }
        {
            var c=new MyCamera { RangeRet=MyCamera.MV_E_SUPPORT }; var h=new SharpnessCheck(); uint v=6;
            Assert(Apply(c,h,ref v,false) && c.Writes==1 && h.Failures.Count==1,"existing-two-argument-behaviour-preserves-legacy-range-path");
        }
        foreach(bool serial in new[]{true,false}) {
            var s=new StartupCheck(); s.fo.HistoricalSerial=serial; s.Run();
            Assert(!s.rts.initStatus.bSerInit && s.detStat==37 && s.rts.initStatus.bUserParamFileInit && s.cup.p_Paf.strLogPath=="unchanged-log-path","history-resets-serial-and-preserves-statistics-"+serial);
        }
        Console.WriteLine("TOTAL " + passed + " passed");
    }
}
'''.replace('SHARP_METHOD', sharp).replace('HISTORY_METHOD', history)
test_source = task / 'BehaviourChecks.cs'
test_source.write_text(source, encoding='utf-8-sig')
csc = Path(r'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe')
subprocess.run([str(csc), '/nologo', '/platform:x64', '/out:' + str(task/'BehaviourChecks.exe'), str(test_source)], check=True)
result = subprocess.run([str(task/'BehaviourChecks.exe')], text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
(task/'behaviour-checks.txt').write_text(result.stdout, encoding='utf-8')
print(result.stdout)
raise SystemExit(result.returncode)
