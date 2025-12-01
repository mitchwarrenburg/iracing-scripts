# Build Script for iRacing Overlay

Write-Host "================================" -ForegroundColor Cyan
Write-Host "iRacing Overlay Build Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Check .NET SDK
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: .NET SDK not found" -ForegroundColor Red
    Write-Host "Install .NET 8.0 SDK from: https://dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}
Write-Host "✓ .NET SDK $dotnetVersion found" -ForegroundColor Green
Write-Host ""

# Configuration
$configuration = "Release"
$runtime = "win-x64"
$outputDir = ".\publish"
$projectPath = ".\src\iRacingOverlay\iRacingOverlay.csproj"
$solutionPath = ".\iRacingOverlay.sln"

# Clean
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path $outputDir) {
    Remove-Item -Recurse -Force $outputDir
}
Write-Host "✓ Clean complete" -ForegroundColor Green
Write-Host ""

# Restore
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore $solutionPath
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Restore failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Restore complete" -ForegroundColor Green
Write-Host ""

# Build
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build $solutionPath --configuration $configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build complete" -ForegroundColor Green
Write-Host ""

# Publish
Write-Host "Publishing application..." -ForegroundColor Yellow
dotnet publish $projectPath `
    --configuration $configuration `
    --output $outputDir `
    --self-contained true `
    --runtime $runtime `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:PublishReadyToRun=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Publish failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Publish complete" -ForegroundColor Green
Write-Host ""

# Success
Write-Host "================================" -ForegroundColor Cyan
Write-Host "BUILD SUCCESSFUL!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output directory: $outputDir" -ForegroundColor Yellow
Write-Host "Executable: $outputDir\iRacingOverlay.exe" -ForegroundColor Yellow
Write-Host ""
Write-Host "To create installer, use WiX Toolset:" -ForegroundColor Cyan
Write-Host "  See .github/workflows/dotnet-build.yml for details" -ForegroundColor Cyan

