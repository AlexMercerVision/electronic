using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenCvSharp;
using Cognex.VisionPro;
using Cognex.VisionPro.QuickBuild;
class AppProbe {
 static string app;
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern bool SetDllDirectory(string path);
 [STAThread] static int Main(string[] args) {
  app=args[0];Directory.SetCurrentDirectory(app);
  AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{var p=Path.Combine(app,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
  try {
   if(args[1]=="opencv")TestCv();else TestVpp();
   File.WriteAllLines(args[2],Process.GetCurrentProcess().Modules.Cast<ProcessModule>().Select(m=>m.FileName).OrderBy(p=>p));return 0;
  }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
 }
 static void TestCv(){SetDllDirectory(Path.Combine(app,"dll","x64"));using(var m=new Mat(32,32,MatType.CV_8UC3,Scalar.All(0)))using(var g=new Mat()){Cv2.CvtColor(m,g,ColorConversionCodes.BGR2GRAY);if(g.Rows!=32||g.Cols!=32)throw new Exception("conversion failed");Console.WriteLine("OpenCV conversion OK "+g.Rows+"x"+g.Cols);}}
 static void TestVpp(){var jm=CogSerializer.LoadObjectFromFile(Path.Combine(app,"VPP","LD.vpp")) as CogJobManager;if(jm==null)throw new Exception("not CogJobManager");Console.WriteLine("LD.vpp deserialize OK; Jobs="+jm.JobCount);for(int i=0;i<jm.JobCount;i++)Console.WriteLine("Job "+i+": "+jm.Job(i).Name);jm.Shutdown();}
}
