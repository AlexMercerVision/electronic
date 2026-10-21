using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
class GpuProbe {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern bool SetDllDirectory(string path);
 [DllImport("model_infer.dll")] static extern void InitModel(string type,string model,string pars,string cfg,bool gpu,int id,ref byte output);
 [DllImport("model_infer.dll")] static extern int Det_ModelPredict(byte[] img,int w,int h,int c,IntPtr output,int[] count,ref byte label);
 [DllImport("model_infer.dll")] static extern void DestructModel();
 static int Main(string[] args) {
  Directory.SetCurrentDirectory(args[0]); SetDllDirectory(args[0]);
  var m=Path.Combine(args[0],"Assets","Models","Default","inference_model");
  var type=new byte[16]; IntPtr output=Marshal.AllocHGlobal(24*10000); bool init=false;
  try {
   InitModel("paddlex",Path.Combine(m,"model.pdmodel"),Path.Combine(m,"model.pdiparams"),Path.Combine(m,"model.yml"),true,0,ref type[0]);init=true;
   var counts=new int[1];var label=new byte[1024*1024];var watch=Stopwatch.StartNew();
   int ret=Det_ModelPredict(new byte[3072*2048*3],3072,2048,3,output,counts,ref label[0]);
   Console.WriteLine("GPU0 ret="+ret+" boxes="+counts[0]+" elapsedMs="+watch.ElapsedMilliseconds);
   File.WriteAllLines(args[1],Process.GetCurrentProcess().Modules.Cast<ProcessModule>().Select(x=>x.FileName).OrderBy(x=>x));
   return ret==0?0:1;
  } catch(Exception ex) {Console.Error.WriteLine(ex);return 1;}
  finally {if(init)DestructModel();Marshal.FreeHGlobal(output);}
 }
}
