# Pulse App Installer
$ErrorActionPreference = "Stop"

$sourceExe = Join-Path $PSScriptRoot "Publish\Pulse.App.exe"
if (-not (Test-Path $sourceExe)) {
    Write-Host "Please build the project first. Searched: $sourceExe" -ForegroundColor Red
    exit 1
}

$installDir = Join-Path $env:LOCALAPPDATA "SoftcursePulse"
if (-not (Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir | Out-Null
}

$targetExe = Join-Path $installDir "Pulse.App.exe"

Write-Host "Copying files to $installDir..."
Copy-Item -Path $sourceExe -Destination $targetExe -Force

Write-Host "Creating Desktop Shortcut..."
$wshShell = New-Object -ComObject WScript.Shell
$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcut = $wshShell.CreateShortcut((Join-Path $desktopPath "Softcurse Pulse.lnk"))
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = $installDir
$shortcut.IconLocation = "$targetExe,0"
$shortcut.Save()

Write-Host "Creating Startup Shortcut (Run on Boot)..."
$startupPath = [Environment]::GetFolderPath("Startup")
$startupShortcut = $wshShell.CreateShortcut((Join-Path $startupPath "Pulse.lnk"))
$startupShortcut.TargetPath = $targetExe
$startupShortcut.WorkingDirectory = $installDir
$startupShortcut.IconLocation = "$targetExe,0"
$startupShortcut.Save()

Write-Host "Installation Complete! You can now launch Pulse Dashboard from your desktop." -ForegroundColor Green
