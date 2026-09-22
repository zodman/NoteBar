<#
.SYNOPSIS
    Configures NoteBar to automatically launch on Windows system boot / user logon.
.DESCRIPTION
    Creates a Windows Startup shortcut pointing to NoteBar.Wpf.exe with desired arguments.
.PARAMETER AllUsers
    If specified, creates the startup shortcut in the All Users startup folder (requires Admin).
    Otherwise, creates it in the Current User startup folder.
.PARAMETER Port
    Default indicator UDP port (defaults to 1738).
.EXAMPLE
    .\Enable-NoteBarStartup.ps1
.EXAMPLE
    .\Enable-NoteBarStartup.ps1 -AllUsers -Port 1738
#>
[CmdletBinding()]
param(
    [switch]$AllUsers,
    [uint32]$Port = 1738
)

$ErrorActionPreference = 'Stop'

# Locate NoteBar.Wpf.exe
$targetExe = $null
$candidates = @(
    "$env:ProgramFiles\NoteBar\NoteBar.Wpf.exe",
    "$PSScriptRoot\NoteBar.Wpf.exe",
    "$PSScriptRoot\src\NoteBar.Wpf.exe",
    "$PSScriptRoot\..\..\src\NoteBar.Wpf\bin\Release\net8.0-windows\NoteBar.Wpf.exe",
    "$PSScriptRoot\..\..\src\NoteBar.Wpf\bin\Debug\net8.0-windows\NoteBar.Wpf.exe"
)

foreach ($c in $candidates) {
    if (Test-Path $c) {
        $targetExe = (Resolve-Path $c).Path
        break
    }
}

if (-not $targetExe) {
    Write-Error "NoteBar.Wpf.exe not found! Please ensure NoteBar is installed in '$env:ProgramFiles\NoteBar'."
    exit 1
}

# Determine startup directory
$startupDir = if ($AllUsers) {
    "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Startup"
} else {
    "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup"
}

if (!(Test-Path -Path $startupDir)) {
    New-Item -ItemType Directory -Path $startupDir -Force | Out-Null
}

$shortcutFile = Join-Path $startupDir "NoteBar.lnk"

# Create .lnk shortcut using WScript.Shell
$wscript = New-Object -ComObject WScript.Shell
$shortcut = $wscript.CreateShortcut($shortcutFile)
$shortcut.TargetPath = $targetExe
$shortcut.Arguments = "--port $Port"
$shortcut.WorkingDirectory = Split-Path -Parent $targetExe
$shortcut.IconLocation = "$targetExe,0"
$shortcut.Description = "NoteBar Status Indicator for Windows 11"
$shortcut.Save()

Write-Host "NoteBar auto-startup on boot enabled successfully!" -ForegroundColor Green
Write-Host "Target:   $targetExe --port $Port"
Write-Host "Shortcut: $shortcutFile"
