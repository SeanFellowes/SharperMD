# ============================================
# SharperMD Installer Build Script (PowerShell)
# ============================================
# Prerequisites:
#   1. .NET 8 SDK installed
#   2. Inno Setup installed (https://jrsoftware.org/isinfo.php)
# ============================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================"
Write-Host "Building SharperMD Installer"
Write-Host "========================================"
Write-Host ""

# Set paths
$SolutionDir = $PSScriptRoot
$ProjectDir = Join-Path $SolutionDir "src\SharperMD"
$PublishDir = Join-Path $ProjectDir "bin\Release\net8.0-windows\publish"
$InstallerDir = Join-Path $SolutionDir "installer"
$OutputDir = Join-Path $InstallerDir "output"

# Find Inno Setup
$IsccPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
)

$IsccPath = $null
foreach ($path in $IsccPaths) {
    if (Test-Path $path) {
        $IsccPath = $path
        break
    }
}

if (-not $IsccPath) {
    Write-Host "ERROR: Inno Setup not found." -ForegroundColor Red
    Write-Host "Please install Inno Setup from https://jrsoftware.org/isinfo.php"
    exit 1
}

Write-Host "Using Inno Setup: $IsccPath"

# Step 1: Clean previous builds
Write-Host "[1/4] Cleaning previous builds..." -ForegroundColor Cyan
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Step 2: Restore NuGet packages
Write-Host "[2/4] Restoring NuGet packages..." -ForegroundColor Cyan
dotnet restore "$ProjectDir\SharperMD.csproj"
if ($LASTEXITCODE -ne 0) { throw "Failed to restore packages" }

# Step 3: Publish the application
Write-Host "[3/4] Publishing application (self-contained for Windows x64)..." -ForegroundColor Cyan
dotnet publish "$ProjectDir\SharperMD.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishReadyToRun=true `
    -o $PublishDir

if ($LASTEXITCODE -ne 0) { throw "Failed to publish application" }

# Step 4: Build installer
Write-Host "[4/4] Building installer with Inno Setup..." -ForegroundColor Cyan
& $IsccPath "$InstallerDir\SharperMD.iss"
if ($LASTEXITCODE -ne 0) { throw "Failed to build installer" }

Write-Host ""
Write-Host "========================================"
Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "========================================"
Write-Host ""
Write-Host "Installer location: $OutputDir\SharperMD-Setup-1.1.0.exe"
Write-Host ""
