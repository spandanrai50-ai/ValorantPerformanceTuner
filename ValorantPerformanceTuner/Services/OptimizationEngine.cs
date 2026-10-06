using ValorantPerformanceTuner.Models;

namespace ValorantPerformanceTuner.Services
{
    public class OptimizationEngine
    {
        public List<OptimizationRecommendation> BuildRecommendations(HardwareSnapshot snapshot)
        {
            var list = new List<OptimizationRecommendation>();

            list.Add(new OptimizationRecommendation
            {
                Title = "Set High Performance power plan",
                Category = "Windows Optimizer",
                Description = "Switch to the High Performance power plan and set the system to avoid sleep throttling.",
                Why = "Power settings affect CPU scheduling and background performance under load.",
                Evidence = $"Power plan currently: {snapshot.PowerPlan}",
                RiskLevel = "Safe",
                RequiresUserApproval = true
            });

            list.Add(new OptimizationRecommendation
            {
                Title = "Turn off nonessential startup apps",
                Category = "Startup Analyzer",
                Description = "Disable apps that are not required during gameplay.",
                Why = "Background processes can steal CPU and memory and raise input delay.",
                Evidence = snapshot.HasBackgroundProcessInterference ? "Detected startup interference" : "No strong interference detected",
                RiskLevel = "Safe",
                RequiresUserApproval = true
            });

            list.Add(new OptimizationRecommendation
            {
                Title = "Tune NVIDIA settings",
                Category = "NVIDIA Optimizer",
                Description = "Prefer maximum performance and low latency mode when available.",
                Why = "NVIDIA driver settings affect frame pacing and responsiveness.",
                Evidence = snapshot.HasNvidia ? "NVIDIA GPU detected" : "No NVIDIA GPU detected",
                RiskLevel = "Moderate",
                RequiresUserApproval = true
            });

            list.Add(new OptimizationRecommendation
            {
                Title = "Cap background overlays",
                Category = "Windows Optimizer",
                Description = "Disable overlays from Steam, Discord, or browser monitoring tools while playing.",
                Why = "Overlays can increase GPU overhead and input latency.",
                Evidence = snapshot.HasBackgroundProcessInterference ? "Overlay-like processes detected" : "No overlay risk detected",
                RiskLevel = "Safe",
                RequiresUserApproval = true
            });

            if (snapshot.HasThermalRisk)
            {
                list.Add(new OptimizationRecommendation
                {
                    Title = "Reduce thermal risk",
                    Category = "Advanced Tweaks Center",
                    Description = "Check cooling, fan curve, and ensure vents are clear.",
                    Why = "Thermal throttling reduces sustained FPS and causes jitter.",
                    Evidence = "Temperature threshold exceeded",
                    RiskLevel = "Moderate",
                    RequiresUserApproval = true
                });
            }

            return list;
        }
    }
}
