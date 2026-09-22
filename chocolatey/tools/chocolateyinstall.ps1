#Requires -RunAsAdministrator

$ErrorActionPreference = 'Stop';

$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$NoteBarDir = "$env:ProgramFiles\NoteBar"

# Stop any running processes before install
Stop-Process -Name "NoteBar*", "notebar" -Force -ErrorAction SilentlyContinue

# Create or clear NoteBar directory
if (!(Test-Path -Path $NoteBarDir)) {
    New-Item -ItemType Directory -Path $NoteBarDir -Force
} 
else {
    Remove-Item -Path "$NoteBarDir\*" -Recurse -Force -ErrorAction SilentlyContinue
}

# Extract or copy binary payload
if (Test-Path "$toolsDir\src.zip") {
    Get-ChocolateyUnzip "$toolsDir\src.zip" "$NoteBarDir"
} elseif (Test-Path "$toolsDir\src") {
    Copy-Item -Path "$toolsDir\src\*" -Destination $NoteBarDir -Recurse -Force
} else {
    Copy-Item -Path "$toolsDir\*" -Destination $NoteBarDir -Recurse -Force -Exclude "*.ps1","*.txt","*.nuspec"
}

# Add NoteBar to system PATH
$Path = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::Machine)
if (!($Path.Split(";") -contains $NoteBarDir)) {
    [Environment]::SetEnvironmentVariable("Path", "$Path;$NoteBarDir", [System.EnvironmentVariableTarget]::Machine)
    $env:Path = "$env:Path;$NoteBarDir"
}

# Create AppData directory for local icons and configuration
$NoteBarAppDataDir = "$env:APPDATA\NoteBar"
if (!(Test-Path -Path $NoteBarAppDataDir)) {
    New-Item -ItemType Directory -Path $NoteBarAppDataDir -Force
}

# Copy startup management scripts to the installation directory
if (Test-Path "$toolsDir\Enable-NoteBarStartup.ps1") {
    Copy-Item -Path "$toolsDir\Enable-NoteBarStartup.ps1" -Destination $NoteBarDir -Force
}
if (Test-Path "$toolsDir\Disable-NoteBarStartup.ps1") {
    Copy-Item -Path "$toolsDir\Disable-NoteBarStartup.ps1" -Destination $NoteBarDir -Force
}

# Create Start Menu shortcut
$startMenuDir = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\NoteBar"
if (!(Test-Path $startMenuDir)) {
    New-Item -ItemType Directory -Path $startMenuDir -Force | Out-Null
}
$wscript = New-Object -ComObject WScript.Shell
$shortcut = $wscript.CreateShortcut("$startMenuDir\NoteBar.lnk")
$shortcut.TargetPath = "$NoteBarDir\NoteBar.Wpf.exe"
$shortcut.WorkingDirectory = $NoteBarDir
$shortcut.Description = "NoteBar Status Indicator"
$shortcut.Save()

# Configure AutoStart on boot if requested via --params "'/AutoStart'"
$packageParameters = $env:chocolateyPackageParameters
$autoStart = $false
if ($packageParameters) {
    if ($packageParameters -match '(?i)/(AutoStart|Startup)') {
        $autoStart = $true
    }
}

if ($autoStart) {
    Write-Host "AutoStart parameter detected. Enabling NoteBar to start on boot..." -ForegroundColor Cyan
    & "$toolsDir\Enable-NoteBarStartup.ps1" -AllUsers
}