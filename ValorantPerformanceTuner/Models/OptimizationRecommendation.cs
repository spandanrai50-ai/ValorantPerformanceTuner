namespace ValorantPerformanceTuner.Models
{
    public class OptimizationRecommendation
    {
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Why { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = "Safe";
        public bool RequiresUserApproval { get; set; } = true;
        public bool Applies { get; set; } = true;
    }
}
