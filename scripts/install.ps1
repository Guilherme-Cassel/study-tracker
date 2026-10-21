<#
.SYNOPSIS
    Publishes Study Tracker to %LOCALAPPDATA%\StudyTracker\app and creates desktop and Start menu shortcuts.

.DESCRIPTION
    Run it again to update: it closes the running app, publishes the current code and keeps the settings.
    Settings live in %LOCALAPPDATA%\StudyTracker\appsettings.Local.json. On the first install they are copied
    from src\StudyTracker.Desktop\appsettings.Local.example.json; set StudyFolder:RootPath there.
#>

$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo 'src\StudyTracker.Desktop'
$baseDir = Join-Path $env:LOCALAPPDATA 'StudyTracker'
$appDir = Join-Path $baseDir 'app'
$exe = Join-Path $appDir 'StudyTracker.exe'
$settings = Join-Path $baseDir 'appsettings.Local.json'

$running = Get-Process -Name 'StudyTracker' -ErrorAction SilentlyContinue
if ($running) {
    Write-Host 'Closing Study Tracker...'
    $running | ForEach-Object { $_.CloseMainWindow() | Out-Null }
    $running | Wait-Process -Timeout 10 -ErrorAction SilentlyContinue
    Get-Process -Name 'StudyTracker' -ErrorAction SilentlyContinue | Stop-Process -Force
}

Write-Host "Publishing to $appDir ..."
if (Test-Path $appDir) { Remove-Item $appDir -Recurse -Force }
dotnet publish $project -c Release -o $appDir --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }
foreach ($required in @('StudyTracker.exe', 'wwwroot\index.html', 'appsettings.json')) {
    if (-not (Test-Path (Join-Path $appDir $required))) { throw "Published app is missing $required." }
}

New-Item -ItemType Directory -Force -Path $baseDir | Out-Null
if (-not (Test-Path $settings)) {
    Copy-Item (Join-Path $project 'appsettings.Local.example.json') $settings
    Write-Host "Settings copied to $settings. Set StudyFolder:RootPath before opening the app."
}
else {
    Write-Host "Keeping settings in $settings"
}

function New-Shortcut([string]$path) {
    $shell = New-Object -ComObject WScript.Shell
    $link = $shell.CreateShortcut($path)
    $link.TargetPath = $exe
    $link.WorkingDirectory = $appDir
    $link.IconLocation = "$exe,0"
    $link.Description = 'Study Tracker'
    $link.Save()
}

$desktopLink = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Study Tracker.lnk'
$startMenuLink = Join-Path ([Environment]::GetFolderPath('Programs')) 'Study Tracker.lnk'
New-Shortcut $desktopLink
New-Shortcut $startMenuLink
Write-Host "Shortcuts: $desktopLink"
Write-Host "           $startMenuLink"

$oldStartup = Join-Path ([Environment]::GetFolderPath('Startup')) 'Study Tracker.lnk'
if (Test-Path $oldStartup) {
    Remove-Item $oldStartup
    Write-Host 'Removed the Startup shortcut from the previous (web) version.'
}

Write-Host 'Done. Open "Study Tracker" from the desktop or the Start menu.'
