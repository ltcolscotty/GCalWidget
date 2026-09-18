$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "src\GCaLink\GCaLink\GCaLink.csproj"

& dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --property:Platform=x64 `
    --property:PublishProfile=win-x64

if ($LASTEXITCODE -ne 0) {
    throw "The x64 build failed with exit code $LASTEXITCODE."
}

Write-Host "x64 build completed successfully."
