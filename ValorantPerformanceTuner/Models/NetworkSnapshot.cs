namespace ValorantPerformanceTuner.Models
{
    public class NetworkSnapshot
    {
        public string AdapterName { get; set; } = string.Empty;
        public string IpV4 { get; set; } = string.Empty;
        public string IpV6 { get; set; } = string.Empty;
        public string Gateway { get; set; } = string.Empty;
        public double PingMs { get; set; }
        public double JitterMs { get; set; }
        public double PacketLossPercent { get; set; }
        public string DnsProfile { get; set; } = "Auto";
        public string HealthScore { get; set; } = "0";
        public bool IsStable { get; set; }
    }
}
