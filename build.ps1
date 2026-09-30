param(
    [ValidateSet("x86", "x64", "ARM64")]
    [string[]]$Platform = @("x86", "x64", "ARM64")
)

$ErrorActionPreference = "Stop"
$projectPath = Join-Path $PSScriptRoot "src\GCaLink\GCaLink\GCaLink.csproj"

foreach ($targetPlatform in $Platform) {
    Write-Host "Building Debug for $targetPlatform..."
    & dotnet build $projectPath --configuration Debug "-p:Platform=$targetPlatform"
    if ($LASTEXITCODE -ne 0) {
        throw "Debug build failed for $targetPlatform."
    }

    Write-Host "Publishing Release for $targetPlatform..."
    & dotnet publish $projectPath --configuration Release "-p:Platform=$targetPlatform"
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed for $targetPlatform."
    }
}