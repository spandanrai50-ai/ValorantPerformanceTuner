using System.Text;
using ValorantPerformanceTuner.Models;

namespace ValorantPerformanceTuner.Services
{
    public class ReportExporter
    {
        public string ExportHtmlReport(HardwareSnapshot snapshot, List<OptimizationRecommendation> recommendations)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h1>Valorant Performance Tuner Report</h1>");
            sb.AppendLine($"<p>Computer: {snapshot.ComputerName}</p>");
            sb.AppendLine($"<p>Windows: {snapshot.WindowsVersion}</p>");
            sb.AppendLine($"<p>CPU: {snapshot.CpuName}</p>");
            sb.AppendLine($"<p>GPU: {snapshot.GpuName}</p>");
            sb.AppendLine($"<p>Estimated FPS: {snapshot.EstimatedFps}</p>");
            sb.AppendLine("<h2>Recommendations</h2>");
            foreach (var item in recommendations)
            {
                sb.AppendLine($"<li><strong>{item.Title}</strong> - {item.Description}</li>");
            }
            sb.AppendLine("</body></html>");

            var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ValorantPerformanceTuner", "Reports");
            Directory.CreateDirectory(file);

            var reportPath = Path.Combine(file, $"report_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            File.WriteAllText(reportPath, sb.ToString());

            return reportPath;
        }
    }
}
