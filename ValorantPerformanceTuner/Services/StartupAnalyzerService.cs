using Microsoft.Win32;
using ValorantPerformanceTuner.Models;

namespace ValorantPerformanceTuner.Services
{
    public class StartupAnalyzerService
    {
        public List<StartupEntry> GetStartupItems()
        {
            var result = new List<StartupEntry>();

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                if (key != null)
                {
                    foreach (var valueName in key.GetValueNames())
                    {
                        var value = key.GetValue(valueName);
                        result.Add(new StartupEntry
                        {
                            Name = valueName,
                            Path = value?.ToString() ?? string.Empty,
                            StartupType = "User Startup",
                            IsEnabled = true
                        });
                    }
                }
            }
            catch
            {
                // ignore
            }

            return result;
        }
    }
}
