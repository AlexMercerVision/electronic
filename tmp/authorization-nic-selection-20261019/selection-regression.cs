using System;
public enum NetworkInterfaceType { Ethernet, Wireless80211, Loopback }
public class PhysicalAddress { string value; public PhysicalAddress(string s) {value=s;} public override string ToString(){return value;} }
public class NetworkInterface {
 public static NetworkInterface[] Snapshot=new NetworkInterface[0]; public static bool Fail;
 public NetworkInterfaceType NetworkInterfaceType; string mac;
 public NetworkInterface(NetworkInterfaceType type,string value){NetworkInterfaceType=type;mac=value;}
 public PhysicalAddress GetPhysicalAddress(){return new PhysicalAddress(mac);}
 public static NetworkInterface[] GetAllNetworkInterfaces(){if(Fail)throw new InvalidOperationException("Enumeration failed");return Snapshot;}
}
public class SelectionFixture {
 public bool PasswordFileFound=true,PasswordFileValid=true; public string Cid="BOUND";
 public string GetCID(){return Cid;}
public string GetMacAddress()
             {
                 try
                 {
                     NetworkInterface[] networks = NetworkInterface.GetAllNetworkInterfaces();
                     if (PasswordFileFound && PasswordFileValid)
                     {
                         string authorizedCid = GetCID();
                         if (!string.IsNullOrEmpty(authorizedCid))
                         {
                             // 已授权网卡优先，避免虚拟网卡或枚举顺序变化影响机器绑定。
                             foreach (NetworkInterface network in networks)
                             {
                                 if (network.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                                     network.GetPhysicalAddress().ToString() == authorizedCid)
                                 {
                                     return authorizedCid;
                                 }
                             }
                         }
                     }
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
public string GetOldMacAddress()
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
}
public class Regression {
 static int count;
 static NetworkInterface E(string s){return new NetworkInterface(NetworkInterfaceType.Ethernet,s);}
 static NetworkInterface W(string s){return new NetworkInterface(NetworkInterfaceType.Wireless80211,s);}
 static void Check(string name, NetworkInterface[] adapters, bool found, bool valid, string cid, string expectedNew,string expectedOld){
  NetworkInterface.Snapshot=adapters;var f=new SelectionFixture{PasswordFileFound=found,PasswordFileValid=valid,Cid=cid};
  string actual=f.GetMacAddress(), prior=f.GetOldMacAddress();
  if(actual!=expectedNew||prior!=expectedOld)throw new Exception(name+": unexpected selected adapter");
  count++;Console.WriteLine("PASS "+name);
 }
 public static int Main(){
  Check("virtual-before-bound",new[]{E("VIRTUAL"),E("BOUND")},true,true,"BOUND","BOUND","VIRTUAL");
  Check("bound-before-virtual",new[]{E("BOUND"),E("VIRTUAL")},true,true,"BOUND","BOUND","BOUND");
  Check("bound-last-of-three",new[]{E("OTHER"),E("VIRTUAL"),E("BOUND")},true,true,"BOUND","BOUND","OTHER");
  Check("wifi-is-not-ethernet",new[]{W("BOUND"),E("VIRTUAL")},true,true,"BOUND","VIRTUAL","VIRTUAL");
  Check("wifi-before-real-bound",new[]{W("OTHER"),E("VIRTUAL"),E("BOUND")},true,true,"BOUND","BOUND","VIRTUAL");
  Check("missing-file-keeps-original-selection",new[]{E("VIRTUAL"),E("BOUND")},false,true,"BOUND","VIRTUAL","VIRTUAL");
  Check("invalid-file-keeps-original-selection",new[]{E("VIRTUAL"),E("BOUND")},true,false,"BOUND","VIRTUAL","VIRTUAL");
  Check("unregistered-keeps-original-selection",new[]{E("VIRTUAL"),E("BOUND")},false,false,null,"VIRTUAL","VIRTUAL");
  Check("missing-cid-keeps-original-selection",new[]{E("VIRTUAL"),E("BOUND")},true,true,null,"VIRTUAL","VIRTUAL");
  Check("empty-cid-keeps-original-selection",new[]{E("VIRTUAL"),E("BOUND")},true,true,"","VIRTUAL","VIRTUAL");
  Check("different-machine-does-not-return-stored-cid",new[]{E("VIRTUAL"),E("OTHER")},true,true,"BOUND","VIRTUAL","VIRTUAL");
  Check("removed-bound-nic-does-not-return-stored-cid",new[]{E("OTHER")},true,true,"BOUND","OTHER","OTHER");
  Check("case-comparison-unchanged",new[]{E("OTHER"),E("BOUND")},true,true,"bound","OTHER","OTHER");
  Check("no-adapters",new NetworkInterface[0],true,true,"BOUND","","");
  Check("wifi-only",new[]{W("BOUND")},true,true,"BOUND","","");
  NetworkInterface.Fail=true;
  Check("enumeration-failure-unchanged",new NetworkInterface[0],true,true,"BOUND","unknow","unknow");
  Console.WriteLine("TOTAL "+count+" passed; old code reproduces the virtual-first regression.");return 0;
 }
}
