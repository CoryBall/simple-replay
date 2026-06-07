# Simple Replay — Installer
# Run this once before launching SimpleReplay.exe
# Usage: Right-click → "Run with PowerShell"  (or: pwsh -ExecutionPolicy Bypass -File install.ps1)

$ErrorActionPreference = "Stop"
$script:root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Step($n, $total, $msg) {
    Write-Host "`n[$n/$total] $msg" -ForegroundColor Cyan
}
function Write-Ok($msg)  { Write-Host "      $msg" -ForegroundColor Green }
function Write-Err($msg) { Write-Host "ERROR: $msg" -ForegroundColor Red }

$totalSteps = 3

# ── 1. .NET 8 Desktop Runtime ─────────────────────────────────────────────────
Write-Step 1 $totalSteps ".NET 8 Desktop Runtime"

$hasRuntime = dotnet --list-runtimes 2>$null |
    Where-Object { $_ -match "^Microsoft\.WindowsDesktop\.App 8\." }

if ($hasRuntime) {
    Write-Ok "Already installed."
} else {
    Write-Host "      Not found — downloading .NET 8 Desktop Runtime..." -ForegroundColor Yellow
    $runtimeUrl  = "https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe"
    $runtimeExe  = Join-Path $env:TEMP "dotnet8-runtime.exe"
    Invoke-WebRequest -Uri $runtimeUrl -OutFile $runtimeExe -UseBasicParsing
    Write-Host "      Installing (may require UAC)..."
    Start-Process $runtimeExe -ArgumentList "/quiet /install /norestart" -Wait
    Remove-Item $runtimeExe -Force
    Write-Ok ".NET 8 Desktop Runtime installed."
}

# ── 2. ffmpeg ──────────────────────────────────────────────────────────────────
Write-Step 2 $totalSteps "ffmpeg"

$ffmpegDir = Join-Path $env:APPDATA "SimpleReplay\tools"
$ffmpegExe = Join-Path $ffmpegDir "ffmpeg.exe"

if (Test-Path $ffmpegExe) {
    Write-Ok "Already installed at $ffmpegExe"
} else {
    New-Item -ItemType Directory -Force $ffmpegDir | Out-Null

    $url = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl-small.zip"
    $zip = Join-Path $env:TEMP "ffmpeg_setup.zip"

    Write-Host "      Downloading ffmpeg (~80 MB)..."
    Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing

    Write-Host "      Extracting..."
    $extractDir = Join-Path $env:TEMP "ffmpeg_setup_extract"
    Expand-Archive -Path $zip -DestinationPath $extractDir -Force

    $found = Get-ChildItem $extractDir -Filter "ffmpeg.exe" -Recurse | Select-Object -First 1
    if (-not $found) {
        Write-Err "ffmpeg.exe not found in downloaded archive."
        exit 1
    }
    Move-Item $found.FullName $ffmpegExe -Force
    Remove-Item $zip, $extractDir -Recurse -Force
    Write-Ok "ffmpeg installed to $ffmpegExe"
}

# ── 3. Shortcut ────────────────────────────────────────────────────────────────
Write-Step 3 $totalSteps "Creating shortcuts"

$exePath = Join-Path $script:root "SimpleReplay.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "      SimpleReplay.exe not found next to install.ps1 — skipping shortcut." -ForegroundColor Yellow
} else {
    $wsh = New-Object -ComObject WScript.Shell

    # Start Menu
    $startMenu = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\SimpleReplay.lnk"
    $lnk = $wsh.CreateShortcut($startMenu)
    $lnk.TargetPath = $exePath
    $lnk.WorkingDirectory = $script:root
    $lnk.Description = "Simple Replay — global hotkey screen capture buffer"
    $lnk.Save()

    # Desktop
    $desktop = Join-Path ([Environment]::GetFolderPath("Desktop")) "SimpleReplay.lnk"
    $lnk2 = $wsh.CreateShortcut($desktop)
    $lnk2.TargetPath = $exePath
    $lnk2.WorkingDirectory = $script:root
    $lnk2.Description = "Simple Replay — global hotkey screen capture buffer"
    $lnk2.Save()

    Write-Ok "Shortcuts created on Desktop and in Start Menu."
}

# ── Done ───────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Setup complete! Launch SimpleReplay.exe (or use the Desktop shortcut)." -ForegroundColor Green
Write-Host "The app sits in your system tray. Right-click to configure or press" -ForegroundColor Green
Write-Host "Ctrl+Shift+F9 (default) to save the last 5 minutes of screen capture." -ForegroundColor Green
