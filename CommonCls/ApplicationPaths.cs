using System;
using System.IO;
using System.Text;

namespace CommonCls
{
    // Installed resources are read-only; machine settings and working files are separate.
    public static class ApplicationPaths
    {
        public static string InstallDirectory => AppDomain.CurrentDomain.BaseDirectory;
        public static string DataDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ElectrodeDet");
        public static string UserDataDirectory => Path.Combine(DataDirectory, "UserData");
        public static string WorkingDataDirectory => Path.Combine(DataDirectory, "data");
        public static string WorkspaceDirectory => Path.Combine(DataDirectory, "Workspaces", "Default");
        public static string DefaultVppPath => Path.Combine(WorkspaceDirectory, "VPP", "LD.vpp");
        public static string DefaultModelDirectory => Resource("Assets", "Models", "Default", "inference_model");
        public static string AuthorizationFile => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GearDet", "authentication.txt");

        public static string Resource(params string[] parts)
        {
            return Path.GetFullPath(Path.Combine(InstallDirectory, Path.Combine(parts)));
        }

        public static string Data(params string[] parts)
        {
            return Path.GetFullPath(Path.Combine(DataDirectory, Path.Combine(parts)));
        }

        private static string Resolve(string path, string root, string fallback)
        {
            if (string.IsNullOrWhiteSpace(path)) return fallback;
            // Reject drive-relative paths: C:foo and \foo still depend on the working directory.
            if (Path.IsPathRooted(path) && Path.GetPathRoot(path).Length < 3)
                throw new InvalidDataException("路径必须是完整绝对路径或普通相对路径：" + path);
            return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(root, path));
        }

        public static void Normalize(CUserParam parameters)
        {
            PathAndFile p = parameters.p_Paf;
            p.strVPPFilePath = Resolve(p.strVPPFilePath, WorkspaceDirectory, DefaultVppPath);
            p.strSigSavePath = Resolve(p.strSigSavePath, DataDirectory, Data("Images", "Single"));
            p.strConSavePath = Resolve(p.strConSavePath, DataDirectory, Data("Images", "Continuous"));
            p.strLogPath = Resolve(p.strLogPath, DataDirectory, Data("Logs"));
            p.strResSavePath = Resolve(p.strResSavePath, DataDirectory, WorkingDataDirectory);
            p.strPlaybackPath = Resolve(p.strPlaybackPath, DataDirectory, Data("Images", "Playback"));
            p.strErrPath = Resolve(p.strErrPath, DataDirectory, Data("Images", "Error"));
            p.strExportPath = Resolve(p.strExportPath, DataDirectory, Data("Export"));
            p.MarkPicPath = Resolve(p.MarkPicPath, DataDirectory, Data("Annotations", "Images"));
            p.AutoMarkPath = Resolve(p.AutoMarkPath, DataDirectory, Data("Annotations", "Auto"));
            p.ManuMarkPath = Resolve(p.ManuMarkPath, DataDirectory, Data("Annotations", "Manual"));
            parameters.p_Paf = p;
            parameters.p_DL.modelPath = Resolve(parameters.p_DL.modelPath, InstallDirectory, DefaultModelDirectory);
        }

        public static void PrepareMachineData()
        {
            PrepareMachineData(InstallDirectory, DataDirectory);
        }

        internal static void PrepareMachineData(string installDirectory, string dataDirectory)
        {
            Directory.CreateDirectory(Path.Combine(dataDirectory, "UserData"));
            Directory.CreateDirectory(Path.Combine(dataDirectory, "data"));
            Directory.CreateDirectory(Path.Combine(dataDirectory, "Scene"));
            string destination = Path.Combine(dataDirectory, "Workspaces", "Default", "VPP", "LD.vpp");
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            // First installation only. Updates never replace the customer's edited VPP.
            string bundled = Path.Combine(installDirectory, "VPP", "LD.vpp");
            if (!File.Exists(destination) && File.Exists(bundled))
                File.Copy(bundled, destination, false);
        }

        internal static string PortableModelPath(string path)
        {
            string root = Path.GetFullPath(InstallDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? path.Substring(root.Length) : path;
        }

        internal static void WriteTextAtomic(string path, string text, bool overwrite = true)
        {
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(text);
                    writer.Flush();
                    stream.Flush(true);
                }
                if (overwrite && File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }
}
