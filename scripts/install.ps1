<#
.SYNOPSIS
    Publishes Study Tracker to %LOCALAPPDATA%\StudyTracker\app and creates a desktop shortcut.

.DESCRIPTION
    Run it again to update: it stops the running copy, publishes the current code and keeps the settings.
    Settings live in %LOCALAPPDATA%\StudyTracker\appsettings.Local.json. On the first install they are copied
    from src\StudyTracker.Web\appsettings.Local.json if it exists, otherwise from the example file.

.PARAMETER StartWithWindows
    Also creates a shortcut in the Startup folder, so the app starts (without opening the browser) when you sign in.
#>
param(
    [switch]$StartWithWindows
)

$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo 'src\StudyTracker.Web'
$launcherProject = Join-Path $repo 'src\StudyTracker.Launcher'
$baseDir = Join-Path $env:LOCALAPPDATA 'StudyTracker'
$appDir = Join-Path $baseDir 'app'
$launcher = Join-Path $appDir 'StudyTracker.Launcher.exe'
$settings = Join-Path $baseDir 'appsettings.Local.json'

Write-Host 'Stopping Study Tracker if it is running...'
Get-Process -Name 'StudyTracker' -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

Write-Host "Publishing to $appDir ..."
if (Test-Path $appDir) { Remove-Item $appDir -Recurse -Force }
dotnet publish $project -c Release -o $appDir --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed (StudyTracker.Web).' }
dotnet publish $launcherProject -c Release -o $appDir --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed (StudyTracker.Launcher).' }

# The Blazor script is only published when StudyTracker.Web is a console app (OutputType Exe).
$endpoints = Get-Content (Join-Path $appDir 'StudyTracker.staticwebassets.endpoints.json') -Raw
if ($endpoints -notmatch '_framework/blazor\.web') {
    throw 'blazor.web.js is missing from the published app; the pages would not be interactive.'
}

New-Item -ItemType Directory -Force -Path $baseDir | Out-Null
if (-not (Test-Path $settings)) {
    $source = Join-Path $project 'appsettings.Local.json'
    if (-not (Test-Path $source)) { $source = Join-Path $project 'appsettings.Local.example.json' }
    Copy-Item $source $settings
    Write-Host "Settings copied to $settings. Check StudyFolder:RootPath before opening the app."
}
else {
    Write-Host "Keeping settings in $settings"
}

function New-Shortcut([string]$path, [string]$arguments) {
    $shell = New-Object -ComObject WScript.Shell
    $link = $shell.CreateShortcut($path)
    $link.TargetPath = $launcher
    $link.Arguments = $arguments
    $link.WorkingDirectory = $appDir
    $link.Description = 'Study Tracker'
    $link.IconLocation = "$launcher,0"
    $link.Save()
}

$desktop = [Environment]::GetFolderPath('Desktop')
New-Shortcut (Join-Path $desktop 'Study Tracker.lnk') '--OpenBrowser=true'
Write-Host "Shortcut created on the desktop: $desktop\Study Tracker.lnk"

$startupLink = Join-Path ([Environment]::GetFolderPath('Startup')) 'Study Tracker.lnk'
if ($StartWithWindows) {
    New-Shortcut $startupLink ''
    Write-Host 'Study Tracker will start when you sign in to Windows.'
}
elseif (Test-Path $startupLink) {
    Write-Host 'Note: a Startup shortcut from a previous install is still there. Delete it to stop starting with Windows:'
    Write-Host "  $startupLink"
}

Write-Host 'Done. Open "Study Tracker" from the desktop.'
