$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "src\GCaLink\GCaLink\GCaLink.csproj"

& dotnet publish $projectPath `
    --configuration Release `
    --runtime win-arm64 `
    --self-contained true `
    --property:Platform=ARM64 `
    --property:PublishProfile=win-ARM64

if ($LASTEXITCODE -ne 0) {
    throw "The ARM64 build failed with exit code $LASTEXITCODE."
}

Write-Host "ARM64 build completed successfully."
