# GCalWidget

## Build

Run the build script from the repository root in PowerShell:

```powershell
.\build.ps1
```

By default, it builds and publishes Debug/Release outputs for `x86`, `x64`, and `ARM64`. To build only selected platforms, pass one or more values:

```powershell
.\build.ps1 -Platform x64
.\build.ps1 -Platform x86, x64
```

## Logs

The app writes its log file to the Windows local app data folder under `GCWidget`:

- `x86`: `%LOCALAPPDATA%\GCWidget\GCWLogs.txt`
- `x64`: `%LOCALAPPDATA%\GCWidget\GCWLogs.txt`
- `ARM64`: `%LOCALAPPDATA%\GCWidget\GCWLogs.txt`

Each build target shares the same log path, so you can check the same file regardless of which platform you built.
