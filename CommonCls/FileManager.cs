using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Management;

namespace CommonCls
{
   public class FileManager
    {


        public bool DataSaveToServer()
        {
            try
            {
                //本地文件夹
                string dataDir = Path.Combine("LocalPath", "Data");
                //服务器文件夹
                string serverDir = @"\\192.168.20.10\数据";

                var state = ConnectState(serverDir, "Admin", "123456");

                if (state == false)
                {
                    //无法连接服务器
                    return false;
                }

                //目标文件夹名称
                string destDir = Path.Combine(serverDir, "Dest");

                CopyFileAndDir(dataDir, destDir);

                return true;
            }
            catch (Exception ex)
            {
                //操作失败
                return false;
            }
        }

        /// <summary>
        /// 连接远程共享文件夹
        /// </summary>
        /// <param name="path">远程共享文件夹的路径</param>
        /// <param name="userName">用户名</param>
        /// <param name="passWord">密码</param>
        /// <returns></returns>
        private static bool ConnectState(string path, string userName, string passWord)
        {
            bool Flag = false;
            Process proc = new Process();
            try
            {
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                string dosLine = "net use " + path + " " + passWord + " /user:" + userName;
                proc.StandardInput.WriteLine(dosLine);
                proc.StandardInput.WriteLine("exit");
                while (!proc.HasExited)
                {
                    proc.WaitForExit(1000);
                }
                string errormsg = proc.StandardError.ReadToEnd();
                proc.StandardError.Close();
                if (string.IsNullOrEmpty(errormsg))
                {
                    Flag = true;
                }
                else
                {
                    throw new Exception(errormsg);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                proc.Close();
                proc.Dispose();
            }
            return Flag;
        }


        /// <summary>
        /// 复制文件夹下的所有文件、目录到指定的文件夹
        /// </summary>
        /// <param name="dir">源文件夹地址</param>
        /// <param name="desDir">指定的文件夹地址</param>
        private static void CopyFileAndDir(string dir, string desDir)
        {
            //文件夹不存在，则新建
            if (!Directory.Exists(desDir))
            {
                Directory.CreateDirectory(desDir);
            }
            IEnumerable<string> files = Directory.EnumerateFileSystemEntries(dir);
            if (files != null && files.Count() > 0)
            {
                foreach (var item in files)
                {
                    string desPath =Path.Combine(desDir, Path.GetFileName(item));

                    //如果是文件
                    var fileExist = File.Exists(item);
                    if (fileExist)
                    {
                        //复制文件到指定目录下                     
                        File.Copy(item, desPath, true);
                        continue;
                    }

                    //如果是文件夹                   
                    CopyFileAndDir(item, desPath);

                }
            }
        }


        public void CopyFilesRecursively(string sourceFolder, string destinationFolder)
        {
            // 获取源文件夹中的所有文件
            string[] files = Directory.GetFiles(sourceFolder);

            // 复制每个文件到目标文件夹
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string destinationFilePath = Path.Combine(destinationFolder, fileName);
                if(!File.Exists(destinationFilePath))
                {
                    if (!Directory.Exists(Path.GetDirectoryName(destinationFilePath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));
                    File.Copy(file, destinationFilePath, true);
                }
                    
            }

            // 递归处理子文件夹
            string[] subFolders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in subFolders)
            {
                string folderName = Path.GetFileName(folder);
                string destinationSubFolder = Path.Combine(destinationFolder, folderName);

                // 递归调用自身处理子文件夹
                CopyFilesRecursively(folder, destinationSubFolder);
            }
        }


        public void RomoteStartProcess()
        {
            //    string remoteMachineName = "Desktop-093o6fs"; LAPTOP-AIJGBJ6F // 远程计算机的名称或 IP 地址
            string remoteMachineName = "Desktop-093o6fs";

            string processPath = "杂\\GetRGB.exe"; // 进程的路径

            // 创建一个进程启动信息对象
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = processPath;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;

            // 设置要启动的进程在远程计算机上运行
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.ErrorDialog = false;
            psi.UserName = ""; // 远程计算机的用户名
            psi.Password = new NetworkCredential("", "").SecurePassword; // 远程计算机的密码
        //    new NetworkCredential()
           // SecureString theSecureString = new NetworkCredential("", "2022").SecurePassword;
            // 创建一个进程对象，并指定远程计算机的名称
            Process process = new Process();
            process.StartInfo = psi;
            process.StartInfo.WorkingDirectory = @"\\Desktop-093o6fs\杂"; // 进程的工作目录
            process.StartInfo.FileName = $"\\\\{remoteMachineName}\\{process.StartInfo.FileName}";

            try
            {
                // 启动进程
                process.Start();
                Console.WriteLine("进程已在远程计算机上启动！");
            }
            catch (Exception ex)
            {
                Console.WriteLine("无法启动进程：" + ex.Message);
            }

        }
        
        public bool RemoteCloseProcess()
        {
            // 远程计算机的名称或IP地址
            string remoteComputer = "Desktop-093o6fs";

            // 要关闭的进程名称
            string processName = "GetRGB";

            try
            {
                // 创建管理范围对象，连接到远程计算机
                ConnectionOptions options = new ConnectionOptions();
                options.Username = "tsong"; // 远程计算机的用户名
                options.Password = "2022"; // 远程计算机的密码
                ManagementScope scope = new ManagementScope($"\\\\{remoteComputer}\\root\\cimv2", options);
                scope.Connect();

                // 查询要关闭的进程
                ObjectQuery query = new ObjectQuery($"SELECT * FROM Win32_Process WHERE Name='{processName}'");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
                ManagementObjectCollection processCollection = searcher.Get();

                // 遍历匹配的进程并关闭它们
                foreach (ManagementObject process in processCollection)
                {
                    // 使用 Terminate() 方法关闭进程
                    process.InvokeMethod("Terminate", null);
                    Console.WriteLine("远程进程关闭成功！");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("发生错误：" + ex.Message);
            }
       
            return true;
        }
        public bool RemoteStartProcess()
        {
            string remoteComputerName = "Desktop-093o6fs";  // 替换为目标计算机的名称或IP地址
            string processName = "mspaint"; // "GetRGB.exe";  // 替换为要启动或关闭的程序的名称

           
            ConnectionOptions connectionOptions = new ConnectionOptions();
            connectionOptions.Username = "tsong";  // 替换为具有远程访问权限的用户名
            connectionOptions.Password = "2022";  // 替换为用户名的密码

            ManagementScope scope = new ManagementScope("\\\\" + remoteComputerName + "\\root\\cimv2", connectionOptions);
            scope.Connect();

            ObjectGetOptions objectGetOptions = new ObjectGetOptions();
            //     ManagementPath managementPath = new ManagementPath("Win32_Process");  // Win32_LogicalDisk.DeviceID=\"C:\"
            ManagementPath managementPath = new ManagementPath("Win32_Process");
         //   ManagementPath managementPath = new ManagementPath()
           
            using (ManagementClass processClass = new ManagementClass(scope, managementPath, objectGetOptions))
            {
                ManagementBaseObject inParams = processClass.GetMethodParameters("Create");

                inParams["CommandLine"] = processName;  // 启动或关闭程序的命令行参数

               /* int ProcessId = 0;
                object[] cmdline = { "cmd /c " + processName, "c:\\", null, ProcessId };
                processClass.InvokeMethod("Create", cmdline);*/

                ManagementBaseObject outParams = processClass.InvokeMethod("Create", inParams, null);
               
                uint returnValue = (uint)(outParams["returnValue"]);

                if (returnValue == 0)
                {
                    Console.WriteLine("程序启动或关闭成功！");
                }
                else
                {
                    Console.WriteLine("程序启动或关闭失败，错误代码：" + returnValue);
                }
            }
            return true;
        }


    }
}
