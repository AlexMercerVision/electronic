param([Parameter(Mandatory=$true)][string]$CommandLine, [Parameter(Mandatory=$true)][string]$WorkingDirectory)
$ErrorActionPreference = 'Stop'
# Diagnostic helper only. Runs under the current user's token with Administrators disabled,
# privileges removed and medium integrity. Does not create accounts or modify account policy.
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class RestrictedUserRunner {
  [StructLayout(LayoutKind.Sequential)] struct SIDATTR { public IntPtr Sid; public uint Attributes; }
  [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] struct STARTUPINFO {
    public uint cb; public string reserved, desktop, title; public uint x,y,xSize,ySize,xCount,yCount,fill,flags;
    public ushort show, reserved2; public IntPtr reservedPtr, stdin, stdout, stderr;
  }
  [StructLayout(LayoutKind.Sequential)] struct PROCESSINFO { public IntPtr process, thread; public uint pid, tid; }
  [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
  [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
  [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr h, uint ms);
  [DllImport("kernel32.dll")] static extern bool GetExitCodeProcess(IntPtr h, out uint code);
  [DllImport("kernel32.dll")] static extern IntPtr LocalFree(IntPtr h);
  [DllImport("advapi32.dll", SetLastError=true)] static extern bool OpenProcessToken(IntPtr p,uint a,out IntPtr t);
  [DllImport("advapi32.dll", SetLastError=true)] static extern bool CreateRestrictedToken(IntPtr t,uint flags,uint disabled,ref SIDATTR groups,uint count,IntPtr privileges,uint restricted,IntPtr sids,out IntPtr result);
  [DllImport("advapi32.dll", CharSet=CharSet.Unicode,SetLastError=true)] static extern bool ConvertStringSidToSid(string s,out IntPtr sid);
  [DllImport("advapi32.dll")] static extern uint GetLengthSid(IntPtr s);
  [DllImport("advapi32.dll",SetLastError=true)] static extern bool SetTokenInformation(IntPtr t,int c,ref SIDATTR value,uint size);
  [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool CreateProcessAsUser(IntPtr token,string app,StringBuilder cmd,IntPtr pa,IntPtr ta,bool inherit,uint flags,IntPtr env,string cwd,ref STARTUPINFO si,out PROCESSINFO pi);
  static void Check(bool result) { if(!result) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()); }
  public static uint Run(string cmd,string cwd) {
    IntPtr original=IntPtr.Zero, restricted=IntPtr.Zero, admin=IntPtr.Zero, medium=IntPtr.Zero;
    PROCESSINFO pi=new PROCESSINFO();
    try {
      Check(OpenProcessToken(GetCurrentProcess(),0xF01FF,out original));
      Check(ConvertStringSidToSid("S-1-5-32-544",out admin));
      SIDATTR disabled=new SIDATTR { Sid=admin, Attributes=0 };
      Check(CreateRestrictedToken(original,1,1,ref disabled,0,IntPtr.Zero,0,IntPtr.Zero,out restricted));
      Check(ConvertStringSidToSid("S-1-16-8192",out medium));
      SIDATTR label=new SIDATTR { Sid=medium, Attributes=0x20 };
      Check(SetTokenInformation(restricted,25,ref label,(uint)Marshal.SizeOf(typeof(SIDATTR))+GetLengthSid(medium)));
      STARTUPINFO si=new STARTUPINFO(); si.cb=(uint)Marshal.SizeOf(typeof(STARTUPINFO)); si.desktop="winsta0\\default";
      Check(CreateProcessAsUser(restricted,null,new StringBuilder(cmd),IntPtr.Zero,IntPtr.Zero,false,0x08000000,IntPtr.Zero,cwd,ref si,out pi));
      if(WaitForSingleObject(pi.process,120000)!=0) throw new Exception("Restricted diagnostic exceeded 120 seconds");
      uint code; Check(GetExitCodeProcess(pi.process,out code)); return code;
    } finally {
      if(pi.thread!=IntPtr.Zero) CloseHandle(pi.thread); if(pi.process!=IntPtr.Zero) CloseHandle(pi.process);
      if(restricted!=IntPtr.Zero) CloseHandle(restricted); if(original!=IntPtr.Zero) CloseHandle(original);
      if(admin!=IntPtr.Zero) LocalFree(admin); if(medium!=IntPtr.Zero) LocalFree(medium);
    }
  }
}
'@
$result = [RestrictedUserRunner]::Run($CommandLine, $WorkingDirectory)
if ($result -ne 0) { throw "Restricted user diagnostic failed: exit $result" }
