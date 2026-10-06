namespace ValorantPerformanceTuner.Models
{
    public class BenchmarkResult
    {
        public double BeforeFps { get; set; }
        public double AfterFps { get; set; }
        public double BeforePingMs { get; set; }
        public double AfterPingMs { get; set; }
        public double ImprovementPercent { get; set; }
    }
}
