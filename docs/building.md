# Build

Run the build script from the repository root in PowerShell:

```powershell
.\build.ps1
```

By default, it builds and publishes Debug/Release outputs for `x86`, `x64`, and `ARM64`. To build only selected platforms, pass one or more values:

```powershell
.\build.ps1 -Platform x64
.\build.ps1 -Platform x86, x64
```

## Local Package Signing

MosaiCal uses a machine-local certificate thumbprint for MSIX package signing. Certificate information should **not** be committed to the repository.

Create `Local.Build.props` alongside `MosaiCal.sln`:

```text
src/MosaiCal/Local.Build.props
```

Add your local signing certificate thumbprint:
```xml
<Project>
  <PropertyGroup>
    <LocalPackageCertificateThumbprint>YOUR_CERTIFICATE_THUMBPRINT</LocalPackageCertificateThumbprint>
  </PropertyGroup>
</Project>
```

`MosaiCal.csproj` automatically imports this file when present and uses the supplied thumbprint for package signing.

`Local.Build.props` is ignored by Git because certificate thumbprints are machine-specific. Each contributor can configure their own signing certificate without changing the shared project configuration.

CI builds disable package signing and do not require this file.