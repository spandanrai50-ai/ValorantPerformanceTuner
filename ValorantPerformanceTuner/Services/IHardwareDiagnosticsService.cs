using ValorantPerformanceTuner.Models;

namespace ValorantPerformanceTuner.Services
{
    public interface IHardwareDiagnosticsService
    {
        Task<HardwareSnapshot> GetHardwareSnapshotAsync();
        Task<List<string>> GetBackgroundProcessesAsync();
        Task<List<string>> DetectDriverIssuesAsync();
        Task<double> GetCpuTemperatureAsync();
        Task<double> GetGpuTemperatureAsync();
    }
}
