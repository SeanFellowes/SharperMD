@echo off
REM ============================================
REM SharperMD Installer Build Script
REM ============================================
REM Prerequisites:
REM   1. .NET 8 SDK installed
REM   2. Inno Setup installed (https://jrsoftware.org/isinfo.php)
REM      Default path: C:\Program Files (x86)\Inno Setup 6\ISCC.exe
REM ============================================

echo.
echo ========================================
echo Building SharperMD Installer
echo ========================================
echo.

REM Set paths
set SOLUTION_DIR=%~dp0
set PROJECT_DIR=%SOLUTION_DIR%src\SharperMD
set PUBLISH_DIR=%PROJECT_DIR%\bin\Release\net8.0-windows\publish
set INSTALLER_DIR=%SOLUTION_DIR%installer
set OUTPUT_DIR=%INSTALLER_DIR%\output

REM Check for Inno Setup
set ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe
if not exist "%ISCC_PATH%" (
    echo ERROR: Inno Setup not found at %ISCC_PATH%
    echo Please install Inno Setup from https://jrsoftware.org/isinfo.php
    echo Or update ISCC_PATH in this script if installed elsewhere.
    pause
    exit /b 1
)

REM Step 1: Clean previous builds
echo [1/4] Cleaning previous builds...
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
mkdir "%OUTPUT_DIR%"

REM Step 2: Restore NuGet packages
echo [2/4] Restoring NuGet packages...
dotnet restore "%PROJECT_DIR%\SharperMD.csproj"
if errorlevel 1 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)

REM Step 3: Publish the application (self-contained)
echo [3/4] Publishing application (self-contained for Windows x64)...
dotnet publish "%PROJECT_DIR%\SharperMD.csproj" -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true -o "%PUBLISH_DIR%"
if errorlevel 1 (
    echo ERROR: Failed to publish application
    pause
    exit /b 1
)

REM Step 4: Build installer
echo [4/4] Building installer with Inno Setup...
"%ISCC_PATH%" "%INSTALLER_DIR%\SharperMD.iss"
if errorlevel 1 (
    echo ERROR: Failed to build installer
    pause
    exit /b 1
)

echo.
echo ========================================
echo Build completed successfully!
echo ========================================
echo.
echo Installer location: %OUTPUT_DIR%\SharperMD-Setup-1.0.0.exe
echo.
pause
