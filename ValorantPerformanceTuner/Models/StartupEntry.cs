namespace ValorantPerformanceTuner.Models
{
    public class StartupEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string StartupType { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public bool IsRecommendedToDisable { get; set; }
    }
}
