namespace ValorantPerformanceTuner.Models
{
    public class HardwareSnapshot
    {
        public string ComputerName { get; set; } = string.Empty;
        public string WindowsVersion { get; set; } = string.Empty;
        public string CpuName { get; set; } = string.Empty;
        public string GpuName { get; set; } = string.Empty;
        public string DriverVersion { get; set; } = string.Empty;
        public string RamInfo { get; set; } = string.Empty;
        public int TotalRamGb { get; set; }
        public string StorageInfo { get; set; } = string.Empty;
        public string NetworkAdapter { get; set; } = string.Empty;
        public string PowerPlan { get; set; } = string.Empty;
        public string CurrentProfile { get; set; } = string.Empty;
        public double CpuUsage { get; set; }
        public double GpuUsage { get; set; }
        public double RamUsage { get; set; }
        public double CpuTemperature { get; set; }
        public double GpuTemperature { get; set; }
        public double EstimatedFps { get; set; }
        public string HealthSummary { get; set; } = string.Empty;
        public bool HasNvidia { get; set; }
        public bool HasThermalRisk { get; set; }
        public bool HasBackgroundProcessInterference { get; set; }
        public bool HasDriverIssue { get; set; }
        public string LastScanTime { get; set; } = DateTime.Now.ToString("g");
    }
}
