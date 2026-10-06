using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ValorantPerformanceTuner.Models;
using ValorantPerformanceTuner.Services;

namespace ValorantPerformanceTuner.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly HardwareDiagnosticsService _hardwareDiagnosticsService = new();
        private readonly NetworkDiagnosticsService _networkDiagnosticsService = new();
        private readonly NvidiaDiagnosticsService _nvidiaDiagnosticsService = new();
        private readonly OptimizationEngine _optimizationEngine = new();
        private readonly ReportExporter _reportExporter = new();
        private readonly RegistryBackupService _registryBackupService = new();
        private readonly StartupAnalyzerService _startupAnalyzerService = new();

        private HardwareSnapshot _hardware = new();
        private NetworkSnapshot _network = new();
        private string _status = "Ready";
        private string _score = "0";
        private string _fpsText = "0 FPS";
        private string _networkText = "No data";
        private ObservableCollection<OptimizationRecommendation> _recommendations = new();

        public HardwareSnapshot Hardware
        {
            get => _hardware;
            set { _hardware = value; OnPropertyChanged(); }
        }

        public NetworkSnapshot Network
        {
            get => _network;
            set { _network = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public string Score
        {
            get => _score;
            set { _score = value; OnPropertyChanged(); }
        }

        public string FpsText
        {
            get => _fpsText;
            set { _fpsText = value; OnPropertyChanged(); }
        }

        public string NetworkText
        {
            get => _networkText;
            set { _networkText = value; OnPropertyChanged(); }
        }

        public ObservableCollection<OptimizationRecommendation> Recommendations
        {
            get => _recommendations;
            set { _recommendations = value; OnPropertyChanged(); }
        }

        public ICommand AutoScanCommand { get; }
        public ICommand OptimizeCommand { get; }
        public ICommand RestoreDefaultsCommand { get; }
        public ICommand ExportReportCommand { get; }
        public ICommand NetworkAnalyzerCommand { get; }

        public MainViewModel()
        {
            AutoScanCommand = new RelayCommand(async (_) => await AutoScanAsync());
            OptimizeCommand = new RelayCommand(async (_) => await OptimizeAsync());
            RestoreDefaultsCommand = new RelayCommand(async (_) => await RestoreDefaultsAsync());
            ExportReportCommand = new RelayCommand(async (_) => await ExportReportAsync());
            NetworkAnalyzerCommand = new RelayCommand(async (_) => await RunNetworkAnalysisAsync());
        }

        public async Task AutoScanAsync()
        {
            Status = "Running system scan...";
            Hardware = await _hardwareDiagnosticsService.GetHardwareSnapshotAsync();
            Network = await _networkDiagnosticsService.GetNetworkMetricsAsync();

            var nvidia = new NvidiaDiagnosticsService();
            var nvidiaVersion = await nvidia.GetNvidiaDriverVersionAsync();
            var nvidiaInstalled = await nvidia.IsNvidiaInstalledAsync();

            Score = ComputeScore(Hardware, Network, nvidiaInstalled).ToString();

            FpsText = $"{Math.Round(Hardware.EstimatedFps, 0)} FPS";

            if (Network.IsStable)
                NetworkText = "Network stable";
            else
                NetworkText = "Network unstable";

            Recommendations = new ObservableCollection<OptimizationRecommendation>(
                _optimizationEngine.BuildRecommendations(Hardware)
            );

            Status = "Scan complete";
        }

        public async Task OptimizeAsync()
        {
            Status = "Running safe optimization checks...";

            var backupPath = _registryBackupService.BackupRegistry("before_optimization");

            var recs = _optimizationEngine.BuildRecommendations(Hardware);
            Recommendations = new ObservableCollection<OptimizationRecommendation>(recs);

            Status = $"Optimization plan prepared. Backup: {backupPath}";
        }

        public async Task RestoreDefaultsAsync()
        {
            Status = "Restoring defaults...";
            var backupPath = _registryBackupService.BackupRegistry("restore_defaults");
            Status = $"Defaults backup saved: {backupPath}";
        }

        public async Task ExportReportAsync()
        {
            Status = "Exporting report...";
            var html = _reportExporter.ExportHtmlReport(Hardware, Recommendations.ToList());
            Status = $"Report exported: {html}";
        }

        public async Task RunNetworkAnalysisAsync()
        {
            Network = await _networkDiagnosticsService.GetNetworkMetricsAsync();
            Status = "Network diagnostics complete";
        }

        private static int ComputeScore(HardwareSnapshot h, NetworkSnapshot n, bool nvidiaInstalled)
        {
            var score = 0;

            if (h.HasThermalRisk) score -= 10;
            if (h.HasBackgroundProcessInterference) score -= 12;
            if (h.HasDriverIssue) score -= 15;

            score += nvidiaInstalled ? 20 : 0;
            score += n.IsStable ? 25 : 8;
            score += Math.Clamp((int)h.EstimatedFps / 3, 0, 30);

            return Math.Clamp(score, 0, 100);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<object?, Task>? _executeAsync;
        private readonly Action<object?>? _execute;

        public RelayCommand(Func<object?, Task> executeAsync)
        {
            _executeAsync = executeAsync;
        }

        public RelayCommand(Action<object?> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            if (_executeAsync != null)
                _ = _executeAsync(parameter);
            else
                _execute?.Invoke(parameter);
        }
    }
}
