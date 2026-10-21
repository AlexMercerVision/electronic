using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Web.Script.Serialization;
class MetadataAudit {
 static void Main(string[] args) {
  var rows=new List<object>();
  foreach(var p in Directory.GetFiles(args[0]).Where(x=>x.EndsWith(".dll")||x.EndsWith(".exe"))) {
   try {
    var a=Assembly.ReflectionOnlyLoadFrom(p);
    rows.Add(new {File=Path.GetFileName(p), Path=p, Name=a.GetName().Name, Version=a.GetName().Version.ToString(), References=a.GetReferencedAssemblies().Select(r=>new {Name=r.Name, Version=r.Version.ToString(), Framework=IsFramework(r)}).ToArray()});
   } catch(BadImageFormatException) {}
  }
  File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(rows));
 }
 static bool IsFramework(AssemblyName r) {
  try {var a=Assembly.ReflectionOnlyLoad(r.FullName);return a.GlobalAssemblyCache && (a.Location.IndexOf("Microsoft.NET",StringComparison.OrdinalIgnoreCase)>=0 || a.Location.IndexOf("Windows\\assembly",StringComparison.OrdinalIgnoreCase)>=0) && !r.Name.StartsWith("Cognex") && !r.Name.StartsWith("halcon");}catch{return false;}
 }
}
