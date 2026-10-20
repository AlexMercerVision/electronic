using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;

namespace CommonCls
{
    public sealed class SerialDeviceInfo
    {
        public string PortName { get; set; }
        public string DeviceId { get; set; }
        public string Description { get; set; }
        public override string ToString()
        {
            if (string.IsNullOrEmpty(PortName)) return "未分配";
            return string.IsNullOrEmpty(Description) ? PortName : PortName + " — " + Description;
        }
    }

    public static class SerialPortBinding
    {
        public static string ConfigurationKey(SerDistrParam parameters)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
        }

        public static List<SerialDeviceInfo> Enumerate()
        {
            var devices = SerialPort.GetPortNames().Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(port => new SerialDeviceInfo { PortName = port, DeviceId = "", Description = "" })
                .OrderBy(device => PortNumber(device.PortName)).ToList();
            try
            {
                using (var search = new ManagementObjectSearcher(
                    "SELECT Name, PNPDeviceID FROM Win32_PnPEntity WHERE Name LIKE '%(COM%)'"))
                using (ManagementObjectCollection rows = search.Get())
                {
                    foreach (ManagementObject row in rows)
                    using (row)
                    {
                        string description = Convert.ToString(row["Name"]);
                        Match match = Regex.Match(description, @"\((COM\d+)\)\s*$", RegexOptions.IgnoreCase);
                        if (!match.Success) continue;
                        var device = devices.FirstOrDefault(d => string.Equals(d.PortName,
                            match.Groups[1].Value, StringComparison.OrdinalIgnoreCase));
                        if (device == null) continue;
                        device.DeviceId = Convert.ToString(row["PNPDeviceID"]);
                        device.Description = description;
                    }
                }
            }
            catch (ManagementException) { } // COM names remain available for explicit manual assignment.
            catch (System.UnauthorizedAccessException) { }
            catch (System.Runtime.InteropServices.COMException) { }
            return devices;
        }

        private static int PortNumber(string name)
        {
            int number;
            return name.Length > 3 && int.TryParse(name.Substring(3), out number) ? number : int.MaxValue;
        }

        public static void EnsureArrays(ref SerDistrParam p)
        {
            p.portNames = Resize(p.portNames);
            p.deviceIds = Resize(p.deviceIds);
            if (p.serInd == null || p.serInd.Length != 4)
                p.serInd = new[] { -1, -1, -1, -1 };
            if (p.bUsed == null || p.bUsed.Length != 4)
            {
                var used = new bool[4];
                if (p.bUsed != null) Array.Copy(p.bUsed, used, Math.Min(p.bUsed.Length, 4));
                p.bUsed = used;
            }
        }

        private static string[] Resize(string[] source)
        {
            if (source != null && source.Length == 4) return source;
            var result = new string[4];
            if (source != null) Array.Copy(source, result, Math.Min(source.Length, 4));
            return result;
        }

        private static bool ResolveOne(SerDistrParam p, int slot, IList<SerialDeviceInfo> devices,
            out SerialDeviceInfo device, out string error)
        {
            device = null;
            error = "串口未启用或未分配，请在参数设置中选择实际设备";
            if (slot < 0 || slot >= 4 || p.bUsed == null || slot >= p.bUsed.Length || !p.bUsed[slot])
                return false;
            string name = p.portNames != null && slot < p.portNames.Length ? p.portNames[slot] : "";
            string identity = p.deviceIds != null && slot < p.deviceIds.Length ? p.deviceIds[slot] : "";
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(identity)) return false;
            var matches = devices.Where(d => !string.IsNullOrWhiteSpace(identity)
                ? string.Equals(d.DeviceId, identity, StringComparison.OrdinalIgnoreCase)
                : string.Equals(d.PortName, name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count != 1)
            {
                error = "原绑定设备 " + name + (matches.Count == 0 ? " 未连接或身份无法确认" : " 匹配不唯一")
                    + "，请重新分配；未改连其他串口";
                return false;
            }
            device = matches[0];
            error = "";
            return true;
        }

        public static bool TryResolve(SerDistrParam p, int slot, IList<SerialDeviceInfo> devices,
            out SerialDeviceInfo device, out string error)
        {
            if (!ResolveOne(p, slot, devices, out device, out error)) return false;
            for (int other = 0; other < 4; other++)
            {
                SerialDeviceInfo assigned;
                string ignored;
                if (other != slot && ResolveOne(p, other, devices, out assigned, out ignored) &&
                    string.Equals(assigned.PortName, device.PortName, StringComparison.OrdinalIgnoreCase))
                {
                    error = $"串口槽{slot + 1}和槽{other + 1}重复绑定{device.PortName}，请重新分配";
                    device = null;
                    return false;
                }
            }
            return true;
        }
    }
}
