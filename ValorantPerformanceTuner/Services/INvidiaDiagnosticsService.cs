namespace ValorantPerformanceTuner.Services
{
    public interface INvidiaDiagnosticsService
    {
        Task<string> GetNvidiaDriverVersionAsync();
        Task<bool> IsNvidiaInstalledAsync();
        Task<string> GetNvidiaControlPanelStatusAsync();
    }
}
