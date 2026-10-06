using System.Diagnostics;
using Microsoft.Win32;

namespace ValorantPerformanceTuner.Services
{
    public class NvidiaDiagnosticsService : INvidiaDiagnosticsService
    {
        public async Task<string> GetNvidiaDriverVersionAsync()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\NVIDIA Corporation\Global\NGXCore");
                if (key != null)
                {
                    var value = key.GetValue("Version");
                    if (value != null) return value.ToString() ?? "Unknown";
                }

                var output = RunCommand("nvidia-smi", "--query-gpu=driver_version --format=csv,noheader");
                if (!string.IsNullOrWhiteSpace(output))
                {
                    return output.Trim();
                }
            }
            catch
            {
                // ignore
            }

            return "Not detected";
        }

        public async Task<bool> IsNvidiaInstalledAsync()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\NVIDIA Corporation\Global\NGXCore");
                if (key != null) return true;

                var output = RunCommand("nvidia-smi", "--query-gpu=name --format=csv,noheader");
                return !string.IsNullOrWhiteSpace(output);
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetNvidiaControlPanelStatusAsync()
        {
            try
            {
                var output = RunCommand("powershell", "-NoProfile -Command \"Get-ChildItem 'HKLM:\\SOFTWARE\\NVIDIA Corporation\\Global' -ErrorAction SilentlyContinue | Format-Table -AutoSize\"");

                if (!string.IsNullOrWhiteSpace(output))
                    return "NVIDIA settings detected";

                return "NVIDIA configuration not present";
            }
            catch
            {
                return "NVIDIA configuration not present";
            }
        }

        private static string RunCommand(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var p = Process.Start(psi);
                if (p == null) return string.Empty;

                var output = p.StandardOutput.ReadToEnd();
                var error = p.StandardError.ReadToEnd();
                p.WaitForExit();

                return string.IsNullOrWhiteSpace(output) ? error : output;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
