using ValorantPerformanceTuner.Models;

namespace ValorantPerformanceTuner.Services
{
    public interface INetworkDiagnosticsService
    {
        Task<NetworkSnapshot> GetNetworkMetricsAsync();
        Task<string> RunPingTestAsync(string host = "1.1.1.1");
        Task<string> RunDnsCheckAsync(string dnsServer);
    }
}
