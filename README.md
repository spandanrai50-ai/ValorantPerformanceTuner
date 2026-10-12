# Valorant Tuner (WPF / .NET 8)

Measurement-driven Valorant tuning for Windows 11. No fake optimizations: every card is backed by WMI,
performance counters, nvidia-smi, netsh, powercfg, registry reads or live ping tests, and shows its evidence.

## Build
1. Install **.NET 8 SDK** (and Visual Studio 2022 with ".NET desktop development", optional).
2. `cd ValorantTuner && dotnet restore && dotnet build -c Release`
3. Run `bin\Release\net8.0-windows\ValorantTuner.exe` (it asks for Administrator: needed for powercfg, HKLM, restore points).
4. Single-file publish: `dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true`

## Use
- **Auto Scan**: system info + 8 s load sampling + process interference + network test. Scan *while Valorant is running* for bottleneck detection.
- **Optimize Valorant**: applies only Safe tweaks (GPU pinned to RTX, Game Mode, Game Bar capture off, mouse acceleration off) after approval. Backup + restore point first.
- **Restore Defaults**: undoes every change using the journal (`%AppData%\ValorantTuner\journal.json`) with exact previous values.
- **Export Report**: HTML in Documents\ValorantTuner (open it and Print > Save as PDF).
- Logs: `%AppData%\ValorantTuner\logs`. Registry backups: `%AppData%\ValorantTuner\backups`.

## Architecture
`Core` (MVVM base) / `Models` / `Services` (WMI, counters, nvidia-smi, shell, journal, backup, report) /
`Modules` (analyzers + TweakCatalog) / `ViewModels` / XAML views.

## Deliberately NOT included (and why)
- Registry TCP/network "tweaks": your measurements showed the added latency is in the ISP route, not Windows.
- NVIDIA Profile Inspector flags, AMD Memory Optimiser "Gaming", disabling the iGPU: froze or destabilised your laptop earlier.
- CPU temperature: no reliable supported Windows API; the app says so instead of faking a number.

## Roadmap
DOCX/PDF export, before/after PresentMon benchmark import, per-adapter Wi-Fi property editing with backup,
charts (live history), scheduled scans.

> Status: written without access to a Windows build machine. Expect to fix a few compile warnings on first build.
