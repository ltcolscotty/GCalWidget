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
