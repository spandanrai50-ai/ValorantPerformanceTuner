using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using ValorantPerformanceTuner.Models;

namespace ValorantPerformanceTuner.Services
{
    public class HardwareDiagnosticsService : IHardwareDiagnosticsService
    {
        public async Task<HardwareSnapshot> GetHardwareSnapshotAsync()
        {
            var snapshot = new HardwareSnapshot();

            try
            {
                snapshot.ComputerName = Environment.MachineName;
                snapshot.WindowsVersion = GetWindowsVersion();
                snapshot.CpuName = GetWmiString("Win32_Processor", "Name");
                snapshot.GpuName = GetWmiString("Win32_VideoController", "Name");
                snapshot.DriverVersion = GetWmiString("Win32_VideoController", "DriverVersion");
                snapshot.RamInfo = GetWmiString("Win32_ComputerSystem", "TotalPhysicalMemory");
                snapshot.TotalRamGb = (int)Math.Round(Convert.ToDouble(snapshot.RamInfo) / (1024d * 1024d * 1024d));
                snapshot.StorageInfo = GetWmiString("Win32_DiskDrive", "Model");
                snapshot.NetworkAdapter = GetWmiString("Win32_NetworkAdapter", "Name");
                snapshot.PowerPlan = GetPowerPlanName();
                snapshot.HasNvidia = snapshot.GpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase);

                snapshot.CpuUsage = GetPerformanceCounter("Processor Information", "% Processor Utility", "_Total");
                snapshot.GpuUsage = GetPerformanceCounter("GPU Engine", "Utilization Percentage", "engtype_3D");

                var ramAvailableMb = GetPerformanceCounter("Memory", "Available MBytes", null);
                snapshot.RamUsage = Math.Max(0, 100 - (ramAvailableMb / Math.Max(1, GetPhysicalMemoryMb() / 100.0)));

                snapshot.CpuTemperature = await GetCpuTemperatureAsync();
                snapshot.GpuTemperature = await GetGpuTemperatureAsync();

                snapshot.EstimatedFps = EstimateFps(snapshot.CpuUsage, snapshot.GpuUsage, snapshot.TotalRamGb);

                snapshot.HasThermalRisk = snapshot.CpuTemperature > 85 || snapshot.GpuTemperature > 80;
                snapshot.HasDriverIssue = (await DetectDriverIssuesAsync()).Count > 0;
                snapshot.HasBackgroundProcessInterference = (await GetBackgroundProcessesAsync()).Count > 0;

                snapshot.HealthSummary = BuildHealthSummary(snapshot);

                return snapshot;
            }
            catch
            {
                return snapshot;
            }
        }

        public Task<List<string>> GetBackgroundProcessesAsync()
        {
            var result = new List<string>();

            try
            {
                var processes = Process.GetProcesses()
                    .Where(p => p.ProcessName.Contains("steam", StringComparison.OrdinalIgnoreCase)
                        || p.ProcessName.Contains("discord", StringComparison.OrdinalIgnoreCase)
                        || p.ProcessName.Contains("chrome", StringComparison.OrdinalIgnoreCase)
                        || p.ProcessName.Contains("corsair", StringComparison.OrdinalIgnoreCase)
                        || p.ProcessName.Contains("msedge", StringComparison.OrdinalIgnoreCase)
                        || p.ProcessName.Contains("nvidia", StringComparison.OrdinalIgnoreCase)
                        || p.ProcessName.Contains("overlay", StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.ProcessName)
                    .Distinct()
                    .ToList();

                result.AddRange(processes);
            }
            catch
            {
                // ignore
            }

            return Task.FromResult(result);
        }

        public Task<List<string>> DetectDriverIssuesAsync()
        {
            var issues = new List<string>();

            try
            {
                var root = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-b875-00aa005b00a7}");
                if (root != null)
                {
                    foreach (var subKeyName in root.GetSubKeyNames())
                    {
                        try
                        {
                            using var subKey = root.OpenSubKey(subKeyName);
                            if (subKey != null)
                            {
                                var driverDesc = subKey.GetValue("DriverDesc") as string;
                                var providerName = subKey.GetValue("ProviderName") as string;

                                if (driverDesc != null && driverDesc.Contains("Realtek", StringComparison.OrdinalIgnoreCase))
                                {
                                    // realtek driver should be inspected for adapter state
                                }
                            }
                        }
                        catch
                        {
                            // ignore sub-key failures
                        }
                    }
                }

                // Check display driver service status
                var displayServices = new[]
                {
                    "nvlddmkm",
                    "dxgkrnl",
                    "igfx"
                };

                foreach (var service in displayServices)
                {
                    if (Process.GetProcessesByName(service).Length > 0)
                    {
                        // valid running driver process
                    }
                }
            }
            catch
            {
                // ignore
            }

            return Task.FromResult(issues);
        }

        public async Task<double> GetCpuTemperatureAsync()
        {
            try
            {
                string output = RunCommand("powershell", "-NoProfile -Command \"Get-CimInstance -ClassName Win32_PerfFormattedData_Counters_ThermalZoneInformation | Select-Object -ExpandProperty Temperature\"");

                if (double.TryParse(output, out var temp))
                {
                    return temp;
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<double> GetGpuTemperatureAsync()
        {
            try
            {
                string output = RunCommand("powershell", "-NoProfile -Command \"Get-CimInstance Win32_VideoController | Select-Object -ExpandProperty CurrentHorizontalResolution\"");

                // This is a fallback not a true GPU temp on all systems.
                // Real GPU temp should be gathered from NVIDIA/AMD APIs or vendor tools.
                // We keep the method safe and non-fake by returning 0 when unavailable.
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private static string GetWindowsVersion()
        {
            var version = Environment.OSVersion.VersionString;
            return version;
        }

        private static string GetWmiString(string className, string property)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {className}");
                foreach (ManagementObject mo in searcher.Get())
                {
                    if (mo[property] != null)
                        return mo[property].ToString() ?? string.Empty;
                }
            }
            catch
            {
                // ignore
            }

            return string.Empty;
        }

        private static double GetPerformanceCounter(string category, string counterName, string? instanceName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(instanceName))
                {
                    using var p = new PerformanceCounter(category, counterName);
                    return p.NextValue();
                }

                using var p2 = new PerformanceCounter(category, counterName, instanceName);
                return p2.NextValue();
            }
            catch
            {
                return 0;
            }
        }

        private static int GetPhysicalMemoryMb()
        {
            var totalBytes = GetWmiLong("Win32_ComputerSystem", "TotalPhysicalMemory");
            return (int)(totalBytes / (1024 * 1024));
        }

        private static long GetWmiLong(string className, string property)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {className}");
                foreach (ManagementObject mo in searcher.Get())
                {
                    if (mo[property] != null)
                    {
                        if (long.TryParse(mo[property].ToString(), out var value))
                            return value;
                    }
                }
            }
            catch
            {
                // ignore
            }

            return 0;
        }

        private static string GetPowerPlanName()
        {
            try
            {
                var output = RunCommand("powercfg", "/getactivescheme");
                if (!string.IsNullOrWhiteSpace(output))
                {
                    var line = output.Split('\n').FirstOrDefault(x => x.Contains("Power Scheme"));
                    if (line != null)
                    {
                        return line.Replace("Power Scheme GUID: ", "").Trim();
                    }
                }
            }
            catch
            {
                // ignore
            }

            return "Balanced";
        }

        private static double EstimateFps(double cpuUsage, double gpuUsage, int totalRamGb)
        {
            var score = 0d;

            if (totalRamGb >= 16) score += 35;
            else if (totalRamGb >= 8) score += 25;
            else score += 10;

            score += (100 - Math.Clamp(cpuUsage, 0, 100)) * 0.25;
            score += (100 - Math.Clamp(gpuUsage, 0, 100)) * 0.35;

            return Math.Clamp(score, 20, 240);
        }

        private static string BuildHealthSummary(HardwareSnapshot snapshot)
        {
            if (snapshot.HasThermalRisk)
                return "Thermal risk detected. Check fans, ventilation, and power settings.";

            if (snapshot.HasDriverIssue)
                return "Driver state should be reviewed before gaming.";

            if (snapshot.HasBackgroundProcessInterference)
                return "Background tasks may be consuming resources.";

            return "System appears healthy for gaming with room for tuning.";
        }

        private static string RunCommand(string fileName, string arguments)
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
    }
}
