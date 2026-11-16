# Valorant Tuner

A safe, read-only Windows 10/11 readiness scanner for VALORANT. It helps you create a baseline before playing without touching game memory, Vanguard, drivers or hidden registry tweaks.

## What it checks

- CPU identification and available system memory
- VALORANT process presence (read only)
- Optional NVIDIA GPU telemetry from `nvidia-smi`—GPU name, driver, temperature, usage and VRAM
- Windows Game Mode and mouse-acceleration registry values
- Active Windows power plan
- An exportable HTML report saved under `Documents\ValorantTuner\Reports`

It does **not** inject, hook, scan or modify VALORANT; alter Vanguard; change NVIDIA settings; or apply automatic Windows tweaks.

## Build and run

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and Visual Studio's **.NET desktop development** workload (optional but convenient).
2. Run `dotnet build -c Release`.
3. Run `bin\Release\net8.0-windows\ValorantTuner.exe`.

No administrator rights are required.

## NVIDIA support

When the NVIDIA driver exposes `nvidia-smi`, the app displays real driver and GPU telemetry. If it is not available, the app stays functional and explains that no NVIDIA reading was returned. It never changes driver settings.

## Limitations

The readiness score is a transparent heuristic, not an FPS benchmark. GPU temperature and utilization are a snapshot at scan time. For meaningful comparisons, scan under similar conditions.

