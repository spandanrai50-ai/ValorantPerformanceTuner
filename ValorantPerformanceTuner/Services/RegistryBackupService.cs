using System.Diagnostics;
using Microsoft.Win32;

namespace ValorantPerformanceTuner.Services
{
    public class RegistryBackupService
    {
        public string BackupRegistry(string backupName)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ValorantPerformanceTuner", "Backups");
            Directory.CreateDirectory(path);

            var file = Path.Combine(path, $"{DateTime.Now:yyyyMMdd_HHmmss}_{backupName}.reg");
            var keyList = new[]
            {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation",
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run"
            };

            foreach (var key in keyList)
            {
                try
                {
                    var process = new ProcessStartInfo
                    {
                        FileName = "reg",
                        Arguments = $"export \"{key}\" \"{file}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var p = Process.Start(process);
                    p?.WaitForExit();
                }
                catch
                {
                    // ignore
                }
            }

            return file;
        }

        public bool RestoreBackup(string filePath)
        {
            try
            {
                var process = new ProcessStartInfo
                {
                    FileName = "reg",
                    Arguments = $"import \"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var p = Process.Start(process);
                p?.WaitForExit();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
