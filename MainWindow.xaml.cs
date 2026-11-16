using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ValorantTuner;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<DiagnosticFinding> _findings = [];
    private DiagnosticSnapshot? _lastSnapshot;

    public MainWindow()
    {
        InitializeComponent();
        FindingList.ItemsSource = _findings;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await RunScanAsync();

    private async void ScanButton_Click(object sender, RoutedEventArgs e) => await RunScanAsync();

    private async Task RunScanAsync()
    {
        SetBusy(true, "Scanning Windows, NVIDIA telemetry and VALORANT readiness…");
        try
        {
            _lastSnapshot = await Task.Run(DiagnosticSnapshot.Collect);
            RenderSnapshot(_lastSnapshot);
            StatusText.Text = $"Scan complete at {_lastSnapshot.ScannedAt:t}. Nothing was changed.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "The scan could not finish: " + ex.Message;
            _findings.Clear();
            _findings.Add(DiagnosticFinding.Warning("Scan interrupted", "No settings were changed. Try running the scan again.", ex.GetType().Name));
        }
        finally
        {
            SetBusy(false, StatusText.Text);
        }
    }

    private void RenderSnapshot(DiagnosticSnapshot snapshot)
    {
        CpuValue.Text = snapshot.Cpu;
        GpuValue.Text = snapshot.GpuName;
        GpuTempValue.Text = snapshot.GpuTemperatureC is null ? "—" : $"{snapshot.GpuTemperatureC} °C";
        MemoryValue.Text = snapshot.AvailableMemoryGiB is null ? "—" : $"{snapshot.AvailableMemoryGiB:0.0} GB";
        ScoreValue.Text = $"{snapshot.ReadinessScore}/100";
        NextStepText.Text = snapshot.NextStep;

        _findings.Clear();
        foreach (var finding in snapshot.Findings)
            _findings.Add(finding);
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastSnapshot is null)
        {
            StatusText.Text = "Run a scan before exporting a report.";
            return;
        }

        try
        {
            var reportDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ValorantTuner", "Reports");
            Directory.CreateDirectory(reportDirectory);
            var reportPath = Path.Combine(reportDirectory, $"valorant-tuner-{DateTime.Now:yyyyMMdd-HHmmss}.html");
            File.WriteAllText(reportPath, _lastSnapshot.ToHtmlReport(), Encoding.UTF8);
            StatusText.Text = "Report saved: " + reportPath;
        }
        catch (Exception ex)
        {
            StatusText.Text = "Could not export the report: " + ex.Message;
        }
    }

    private void NvidiaButton_Click(object sender, RoutedEventArgs e)
    {
        var nvidiaApp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NVIDIA Corporation", "NVIDIA App", "CEF", "NVIDIA App.exe");
        if (!File.Exists(nvidiaApp))
        {
            StatusText.Text = "NVIDIA App was not found. NVIDIA telemetry can still work through nvidia-smi when the driver provides it.";
            return;
        }

        Process.Start(new ProcessStartInfo(nvidiaApp) { UseShellExecute = true });
    }

    private void ConfigButton_Click(object sender, RoutedEventArgs e)
    {
        var configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VALORANT", "Saved", "Config");
        if (!Directory.Exists(configDirectory))
        {
            StatusText.Text = "VALORANT's local configuration folder was not found on this Windows account.";
            return;
        }

        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{configDirectory}\"") { UseShellExecute = true });
    }

    private void SetBusy(bool isBusy, string status)
    {
        ScanButton.IsEnabled = !isBusy;
        ExportButton.IsEnabled = !isBusy;
        BusyBar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        StatusText.Text = status;
    }
}

public sealed record DiagnosticFinding(string Title, string Detail, string Evidence, string SeverityColor)
{
    public static DiagnosticFinding Ok(string title, string detail, string evidence) => new(title, detail, evidence, "#22C55E");
    public static DiagnosticFinding Info(string title, string detail, string evidence) => new(title, detail, evidence, "#60A5FA");
    public static DiagnosticFinding Warning(string title, string detail, string evidence) => new(title, detail, evidence, "#F59E0B");
}

public sealed record DiagnosticSnapshot(
    DateTimeOffset ScannedAt,
    string Cpu,
    string GpuName,
    int? GpuTemperatureC,
    double? AvailableMemoryGiB,
    int ReadinessScore,
    string NextStep,
    IReadOnlyList<DiagnosticFinding> Findings)
{
    public static DiagnosticSnapshot Collect()
    {
        var findings = new List<DiagnosticFinding>();
        var cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")?.Trim();
        if (string.IsNullOrWhiteSpace(cpu))
            cpu = $"{Environment.ProcessorCount} logical processors";

        var memory = ReadMemory();
        var nvidia = ReadNvidiaTelemetry();
        var valorantRunning = IsValorantRunning();
        var gameMode = ReadRegistryValue(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar", "AllowAutoGameMode");
        var mouseSpeed = ReadRegistryValue(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseSpeed");
        var powerPlan = RunCommand("powercfg.exe", "/getactivescheme", 2500);

        var score = 45;
        if (memory.AvailableGiB is >= 4) score += 15;
        if (nvidia.IsAvailable) score += 10;
        if (gameMode == "1") score += 10;
        if (mouseSpeed == "0") score += 10;
        if (powerPlan?.Contains("High performance", StringComparison.OrdinalIgnoreCase) == true) score += 5;
        if (valorantRunning) score += 5;
        score = Math.Clamp(score, 0, 100);

        findings.Add(valorantRunning
            ? DiagnosticFinding.Ok("VALORANT is running", "The game process was found. The tuner does not interact with it.", "Process: VALORANT-Win64-Shipping.exe")
            : DiagnosticFinding.Info("VALORANT is not running", "Launch the game before a gameplay-session scan if you want this confirmation.", "Process lookup only; no process modification"));

        if (nvidia.IsAvailable)
        {
            var temperatureDetail = nvidia.TemperatureC is null
                ? "Driver telemetry is available."
                : nvidia.TemperatureC >= 83
                    ? $"GPU temperature is {nvidia.TemperatureC} °C. Check cooling before a long session."
                    : $"GPU temperature is {nvidia.TemperatureC} °C.";
            findings.Add(nvidia.TemperatureC is >= 83
                ? DiagnosticFinding.Warning("NVIDIA GPU telemetry", temperatureDetail, nvidia.Evidence)
                : DiagnosticFinding.Ok("NVIDIA GPU telemetry", temperatureDetail, nvidia.Evidence));
        }
        else
        {
            findings.Add(DiagnosticFinding.Info("NVIDIA telemetry unavailable", "nvidia-smi was not available. Install or update an NVIDIA driver only if your PC has an NVIDIA GPU.", "Command: nvidia-smi"));
        }

        if (memory.AvailableGiB is null)
        {
            findings.Add(DiagnosticFinding.Info("Available memory unavailable", "Windows did not return a usable memory reading.", "GlobalMemoryStatusEx"));
        }
        else if (memory.AvailableGiB < 4)
        {
            findings.Add(DiagnosticFinding.Warning("Low available RAM", $"Only {memory.AvailableGiB:0.0} GB is currently available. Close unused launchers and browsers before playing.", "GlobalMemoryStatusEx"));
        }
        else
        {
            findings.Add(DiagnosticFinding.Ok("Available RAM", $"{memory.AvailableGiB:0.0} GB is currently available.", "GlobalMemoryStatusEx"));
        }

        findings.Add(gameMode == "1"
            ? DiagnosticFinding.Ok("Windows Game Mode", "Game Mode is enabled for this user.", "HKCU\\Software\\Microsoft\\GameBar: AllowAutoGameMode = 1")
            : DiagnosticFinding.Info("Windows Game Mode", "Game Mode could not be confirmed as enabled. Review it in Windows Settings > Gaming.", "HKCU\\Software\\Microsoft\\GameBar: AllowAutoGameMode"));

        findings.Add(mouseSpeed == "0"
            ? DiagnosticFinding.Ok("Pointer acceleration", "Windows mouse acceleration appears disabled, which helps keep desktop input predictable.", "HKCU\\Control Panel\\Mouse: MouseSpeed = 0")
            : DiagnosticFinding.Info("Pointer acceleration", "Windows reports pointer acceleration as enabled or unknown. VALORANT Raw Input remains the relevant in-game setting.", "HKCU\\Control Panel\\Mouse: MouseSpeed = " + (mouseSpeed ?? "not found")));

        findings.Add(string.IsNullOrWhiteSpace(powerPlan)
            ? DiagnosticFinding.Info("Power plan", "The active power plan could not be read.", "Command: powercfg /getactivescheme")
            : DiagnosticFinding.Info("Power plan", powerPlan.Trim(), "Command: powercfg /getactivescheme"));

        findings.Add(DiagnosticFinding.Info("Safety boundary", "This release is a read-only diagnostic. It does not alter registry settings, power plans, drivers, files, game memory or anti-cheat.", "Application policy"));

        var nextStep = nvidia.TemperatureC is >= 83
            ? "Start with cooling: elevate the laptop, clear vents, and use a balanced fan profile."
            : memory.AvailableGiB is < 4
                ? "Close unused background apps, then scan again before launching VALORANT."
                : "Use the report as a baseline, then compare it after driver or Windows updates.";

        return new DiagnosticSnapshot(DateTimeOffset.Now, cpu, nvidia.Name, nvidia.TemperatureC, memory.AvailableGiB, score, nextStep, findings);
    }

    public string ToHtmlReport()
    {
        var rows = string.Join(Environment.NewLine, Findings.Select(f => $"<section><h3>{WebUtility.HtmlEncode(f.Title)}</h3><p>{WebUtility.HtmlEncode(f.Detail)}</p><pre>{WebUtility.HtmlEncode(f.Evidence)}</pre></section>"));
        return $"""
            <!doctype html><html lang="en"><head><meta charset="utf-8"><title>Valorant Tuner report</title>
            <style>body{{font-family:Segoe UI,Arial;background:#0a0f17;color:#ecf2fa;max-width:900px;margin:36px auto;padding:0 20px}}h1{{color:#ff4655}}section{{background:#101722;border-left:4px solid #60a5fa;padding:14px 18px;margin:12px 0;border-radius:6px}}h3{{margin:0}}p{{color:#d4e0ef}}pre{{color:#9caec4;white-space:pre-wrap}}</style></head><body>
            <h1>Valorant Tuner</h1><p>Safe readiness report generated {ScannedAt.LocalDateTime:F}.</p>
            <p><strong>CPU:</strong> {WebUtility.HtmlEncode(Cpu)}<br><strong>NVIDIA GPU:</strong> {WebUtility.HtmlEncode(GpuName)}<br><strong>Available RAM:</strong> {(AvailableMemoryGiB is null ? "Unavailable" : $"{AvailableMemoryGiB:0.0} GB")}<br><strong>Readiness score:</strong> {ReadinessScore}/100</p>
            {rows}</body></html>
            """;
    }

    private static string? ReadRegistryValue(string subKey, string valueName) => Registry.GetValue(subKey, valueName, null)?.ToString();

    private static bool IsValorantRunning()
    {
        var processes = Process.GetProcessesByName("VALORANT-Win64-Shipping");
        foreach (var process in processes) process.Dispose();
        return processes.Length > 0;
    }

    private static (double? AvailableGiB, double? TotalGiB) ReadMemory()
    {
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref status)) return (null, null);
        const double bytesPerGiB = 1024d * 1024d * 1024d;
        return (status.AvailPhys / bytesPerGiB, status.TotalPhys / bytesPerGiB);
    }

    private static NvidiaTelemetry ReadNvidiaTelemetry()
    {
        var output = RunCommand("nvidia-smi.exe", "--query-gpu=name,driver_version,temperature.gpu,utilization.gpu,memory.total,memory.used --format=csv,noheader,nounits", 3000);
        if (string.IsNullOrWhiteSpace(output)) return new NvidiaTelemetry("Not detected", null, false, "nvidia-smi did not return data");

        var values = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Split(',').Select(part => part.Trim()).ToArray();
        if (values.Length < 3) return new NvidiaTelemetry("Detected", null, true, output.Trim());
        var temperature = int.TryParse(values[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedTemperature) ? parsedTemperature : null;
        var driver = values.Length > 1 ? values[1] : "unknown";
        var utilization = values.Length > 3 ? values[3] : "unknown";
        var memory = values.Length > 5 ? $"{values[5]}/{values[4]} MiB" : "unknown";
        return new NvidiaTelemetry(values[0], temperature, true, $"Driver {driver}; GPU {utilization}% ; VRAM {memory}; source nvidia-smi");
    }

    private static string? RunCommand(string fileName, string arguments, int timeoutMilliseconds)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            if (process is null) return null;
            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(timeoutMilliseconds))
            {
                process.Kill(true);
                return null;
            }
            return process.ExitCode == 0 ? output : null;
        }
        catch (Win32Exception)
        {
            return null;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
}

public sealed record NvidiaTelemetry(string Name, int? TemperatureC, bool IsAvailable, string Evidence);

