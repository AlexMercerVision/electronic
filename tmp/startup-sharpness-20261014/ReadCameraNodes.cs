using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using MvCamCtrl.NET;

internal static class Entry
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SetDllDirectory(string path);
    private static int Main(string[] args)
    {
        string sdk = Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            new AssemblyName(e.Name).Name == "MvCameraControl.Net" ? Assembly.LoadFrom(sdk) : null;
        SetDllDirectory(@"C:\Program Files (x86)\Common Files\MVS\Runtime\Win64_x64");
        try { return Probe.Run(); }
        catch (Exception ex) { Console.WriteLine(ex); return 1; }
    }
}

internal static class Probe
{
    private static string Code(int ret) { return "0x" + unchecked((uint)ret).ToString("X8"); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static int Run()
    {
        var devices = new MyCamera.MV_CC_DEVICE_INFO_LIST();
        int ret = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref devices);
        Console.WriteLine("EnumDevices=" + Code(ret) + ", count=" + devices.nDeviceNum);
        if (ret != MyCamera.MV_OK) return 1;
        for (int i = 0; i < devices.nDeviceNum; i++)
        {
            var info = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(devices.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
            var camera = new MyCamera();
            bool opened = false;
            try
            {
                ret = camera.MV_CC_CreateDevice_NET(ref info);
                Console.WriteLine("Camera=" + i + ", Create=" + Code(ret));
                if (ret != MyCamera.MV_OK) continue;
                ret = camera.MV_CC_OpenDevice_NET();
                Console.WriteLine("Camera=" + i + ", Open=" + Code(ret));
                if (ret != MyCamera.MV_OK) continue;
                opened = true;
                foreach (string name in new[] { "SharpnessEnable", "Sharpness", "SharpnessAuto", "PixelFormat" })
                {
                    var access = MyCamera.MV_XML_AccessMode.AM_Undefined;
                    ret = camera.MV_XML_GetNodeAccessMode_NET(name, ref access);
                    Console.WriteLine("Camera=" + i + ", node=" + name + ", access=" + access + ", SDK=" + Code(ret));
                }
                bool enable = false;
                ret = camera.MV_CC_GetBoolValue_NET("SharpnessEnable", ref enable);
                Console.WriteLine("Camera=" + i + ", SharpnessEnable=" + enable + ", SDK=" + Code(ret));
                var range = new MyCamera.MVCC_INTVALUE();
                ret = camera.MV_CC_GetIntValue_NET("Sharpness", ref range);
                Console.WriteLine("Camera=" + i + ", Sharpness.GetInt=" + Code(ret) + ", current=" + range.nCurValue + ", min=" + range.nMin + ", max=" + range.nMax);
                var format = new MyCamera.MVCC_ENUMVALUE();
                ret = camera.MV_CC_GetEnumValue_NET("PixelFormat", ref format);
                Console.WriteLine("Camera=" + i + ", PixelFormat=" + Code(ret) + ", current=" + format.nCurValue.ToString("X8"));
            }
            finally
            {
                if (opened) Console.WriteLine("Camera=" + i + ", Close=" + Code(camera.MV_CC_CloseDevice_NET()));
                Console.WriteLine("Camera=" + i + ", Destroy=" + Code(camera.MV_CC_DestroyDevice_NET()));
            }
        }
        return 0;
    }
}
