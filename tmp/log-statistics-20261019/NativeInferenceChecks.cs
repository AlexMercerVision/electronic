using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using CommonCls;
using Cognex.VisionPro;
using Newtonsoft.Json;
public static class NativeInferenceChecks
{
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] static extern bool SetDllDirectory(string path);
    static DLResult Result() { return new DLResult { AL_Rect=new List<CogRectangleAffine>(), AL_Type=new List<int>(), AL_score=new List<float>(), Al_bDiff=new List<bool>() }; }
    public static void Main(string[] args)
    {
        string publicBin=args[0], work=args[1];
        Directory.CreateDirectory(work); Environment.CurrentDirectory=work;
        RuntimeLog.Configure(Path.Combine(work,"Log"));
        if(!SetDllDirectory(publicBin)) throw new Exception("native path failed");
        var cup=JsonConvert.DeserializeObject<CUserParam>(File.ReadAllText(Path.Combine(publicBin,"UserData","serUserParam.json")));
        var cdl=new CDL(); bool success; var result=Result();
        if(cdl.det_infer_img_ByteColor(new byte[12],2,2,.5f,.5f,ref result,out success) || success) throw new Exception("uninitialized accepted");
        try {
            if(cdl.InitDL(cup.p_DL)!=0) throw new Exception("InitDL failed");
            int camera=cup.p_CD.camInd[0], width=(int)cup.p_Cam[camera].iWidth, height=(int)cup.p_Cam[camera].iHeight;
            var watch=Stopwatch.StartNew();
            bool oldColor=cdl.det_infer_img_ByteColor(new byte[width*height*3],width,height,.5f,.5f,ref result,out success);
            long colorMs=watch.ElapsedMilliseconds;
            if(!success || oldColor || result.AL_Rect.Count!=0) throw new Exception("real zero-box color result invalid");
            watch.Restart(); result=Result();
            bool oldGray=cdl.det_infer_img_ByteGray(new byte[width*height],width,height,.5f,.5f,ref result,out success);
            long grayMs=watch.ElapsedMilliseconds;
            if(!success || oldGray || result.AL_Rect.Count!=0) throw new Exception("real zero-box gray result invalid");
            if(cdl.det_infer_img_ByteColor(new byte[1],width,height,.5f,.5f,ref result,out success) || success) throw new Exception("short frame accepted");
            Console.WriteLine(JsonConvert.SerializeObject(new { UseGPU=cup.p_DL.bUseGPU, GPUId=cup.p_DL.gpuID, Width=width, Height=height,
                ColorSuccess=true, ColorRawZeroBoxes=!oldColor, ColorMilliseconds=colorMs,
                GraySuccess=true, GrayRawZeroBoxes=!oldGray, GrayMilliseconds=grayMs, UninitializedRejected=true, ShortFrameRejected=true },Formatting.Indented));
        }
        finally { cdl.CloseDL(); RuntimeLog.Shutdown(1500); }
    }
}
