from pathlib import Path

task = Path(__file__).resolve().parent
source = (task.parents[1] / 'VisionApplication' / 'FrmMainUI.cs').read_text(encoding='utf-8-sig')
start = source.index('        private void WarmupDL()')
body = source.index('{', start)
depth = 1
end = body + 1
while depth:
    depth += (source[end] == '{') - (source[end] == '}')
    end += 1
method = source[start:end]
load = source[source.index('private void FrmMainUI_Load('):]
assert load.index('InitCam();') < load.index('WarmupDL();') < load.index('InitSerial();') < load.index('this.Show();')
assert 'if (cup.p_VPPRTP.bDeepInfer && rts.initStatus.bDLModelInit)' in load[:load.index('WarmupDL();')]
support = r'''
using System;
using System.Collections.Generic;
class Param { public Distribution p_CD = new Distribution(); public Camera[] p_Cam = {new Camera(300,100),new Camera(400,200),new Camera(500,250)}; }
class Distribution { public bool[] bListProcess = {true,true,true}; public int[] camInd = {1,0,2}; }
struct Camera { public uint iWidth, iHeight; public Camera(uint w,uint h) {iWidth=w;iHeight=h;} }
class Log { public string strOperEvent, strFailReason; public int iEventStatus; }
class Infer { public int Calls, Width, Height; public bool Success=true; public bool det_infer_img(int w,int h) {Calls++;Width=w;Height=h;return Success;} }
class Harness {
    const int maxJobNum=2, maxCameraNum=2;
    Param cup = new Param();
    Infer cdl = new Infer();
    Log operLog = new Log();
    List<string> logs = new List<string>();
    int published;
    void WriteStartupLog(string s) {logs.Add(s);}
    void AddLog() {published++;}
'''
checks = r'''
    static int cases;
    static void Check(string name, Action<Harness> setup, int calls, int width, bool success) {
        var h = new Harness(); setup(h); h.WarmupDL();
        if(h.cdl.Calls!=calls || (calls>0 && h.cdl.Width!=width) || h.published!=1 || h.operLog.iEventStatus!=(success?0:-1))
            throw new Exception(name+" failed");
        if(!success && String.IsNullOrEmpty(h.operLog.strFailReason)) throw new Exception(name+" missing reason");
        cases++; Console.WriteLine("PASS "+name);
    }
    static void Main() {
        Check("Job0 uses mapped Cam1 and calls once", h=>{}, 1,400,true);
        Check("Disabled Job0 selects Job1 Cam0", h=>h.cup.p_CD.bListProcess[0]=false,1,300,true);
        Check("Invalid Job0 mapping isolates and selects Job1", h=>h.cup.p_CD.camInd[0]=-1,1,300,true);
        Check("Reserved physical camera rejected", h=>h.cup.p_CD.camInd[0]=2,1,300,true);
        Check("Zero-size camera skipped", h=>h.cup.p_Cam[1]=new Camera(0,200),1,300,true);
        Check("Byte-count overflow skipped", h=>h.cup.p_Cam[1]=new Camera(100000,100000),1,300,true);
        Check("UInt dimension overflow skipped", h=>h.cup.p_Cam[1]=new Camera(UInt32.MaxValue,2),1,300,true);
        Check("Missing cameras rejected", h=>h.cup.p_Cam=null,0,0,false);
        Check("Missing mapping array rejected", h=>h.cup.p_CD.camInd=null,0,0,false);
        Check("Missing enable array rejected", h=>h.cup.p_CD.bListProcess=null,0,0,false);
        Check("Short enable and mapping arrays rejected", h=>{h.cup.p_CD.bListProcess=new[]{true};h.cup.p_CD.camInd=new int[0];},0,0,false);
        Check("Reserved Job2 never used", h=>h.cup.p_CD.bListProcess=new[]{false,false,true},0,0,false);
        Check("Inference failure stays failed without retry", h=>h.cdl.Success=false,1,400,false);
        Console.WriteLine(cases+" startup selection checks passed; hardware and UI methods are not invoked.");
    }
}
'''
(task / 'StartupSelectionChecks.cs').write_text(support + method + checks, encoding='utf-8-sig')
print('Actual WarmupDL method extracted; startup order and initialization guard verified.')
