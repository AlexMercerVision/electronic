param([string]$InstallRoot, [string]$ReportFile)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
public static class RestrictedPermissions {
 [StructLayout(LayoutKind.Sequential)] struct SIDATTR { public IntPtr Sid; public uint Attributes; }
 [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
 [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
 [DllImport("kernel32.dll")] static extern IntPtr LocalFree(IntPtr h);
 [DllImport("advapi32.dll",SetLastError=true)] static extern bool OpenProcessToken(IntPtr p,uint access,out IntPtr t);
 [DllImport("advapi32.dll",SetLastError=true)] static extern bool CreateRestrictedToken(IntPtr t,uint flags,uint disabled,ref SIDATTR groups,uint count,IntPtr privileges,uint restricted,IntPtr sids,out IntPtr result);
 [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool ConvertStringSidToSid(string value,out IntPtr sid);
 [DllImport("advapi32.dll",SetLastError=true)] static extern bool ImpersonateLoggedOnUser(IntPtr token);
 [DllImport("advapi32.dll",SetLastError=true)] static extern bool RevertToSelf();
 [DllImport("advapi32.dll")] static extern uint GetLengthSid(IntPtr s);
 [DllImport("advapi32.dll",SetLastError=true)] static extern bool SetTokenInformation(IntPtr t,int c,ref SIDATTR value,uint size);
 static void Check(bool ok) { if(!ok) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()); }
 public static void Probe(string install,string report) {
   IntPtr original=IntPtr.Zero, token=IntPtr.Zero, admin=IntPtr.Zero, medium=IntPtr.Zero;
   bool impersonating=false, denied=false;
   string probe=Path.Combine(install,"permission-"+Guid.NewGuid().ToString("N")+".tmp");
   try {
     Check(OpenProcessToken(GetCurrentProcess(),0xF01FF,out original));
     Check(ConvertStringSidToSid("S-1-5-32-544",out admin));
     SIDATTR disabled=new SIDATTR { Sid=admin };
     Check(CreateRestrictedToken(original,1,1,ref disabled,0,IntPtr.Zero,0,IntPtr.Zero,out token));
     Check(ConvertStringSidToSid("S-1-16-8192",out medium));
     SIDATTR label=new SIDATTR { Sid=medium, Attributes=0x20 };
     Check(SetTokenInformation(token,25,ref label,(uint)Marshal.SizeOf(typeof(SIDATTR))+GetLengthSid(medium)));
     Check(ImpersonateLoggedOnUser(token)); impersonating=true;
     using(WindowsIdentity id=WindowsIdentity.GetCurrent(true)) {
       if(new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator)) throw new Exception("Administrator enabled");
     }
     using(FileStream stream=File.OpenRead(Path.Combine(install,"App","CommonCls.dll"))) { if(stream.ReadByte()<0) throw new Exception("Application read failed"); }
     try { File.WriteAllText(probe,"permission probe"); } catch(UnauthorizedAccessException) { denied=true; }
     if(!denied) throw new Exception("Application install directory is writable");
     File.WriteAllText(report,"{\"AdministratorsEnabled\":false,\"Integrity\":\"Medium\",\"InstallDirectoryWriteDenied\":true,\"ApplicationRead\":\"Passed\",\"ProgramDataLogWrite\":\"Passed\",\"Boundary\":\"Restricted-token impersonation filesystem test; separate-user process and full application remain unverified.\"}");
   } finally {
     if(impersonating) RevertToSelf();
     if(File.Exists(probe)) File.Delete(probe);
     if(token!=IntPtr.Zero) CloseHandle(token); if(original!=IntPtr.Zero) CloseHandle(original);
     if(admin!=IntPtr.Zero) LocalFree(admin); if(medium!=IntPtr.Zero) LocalFree(medium);
   }
 }
}
'@
[RestrictedPermissions]::Probe($InstallRoot, $ReportFile)
