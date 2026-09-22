<#
.SYNOPSIS
    Disables NoteBar from automatically launching on Windows system boot / user logon.
.DESCRIPTION
    Removes NoteBar Startup shortcuts from both All Users and Current User startup directories.
.EXAMPLE
    .\Disable-NoteBarStartup.ps1
#>
[CmdletBinding()]
param()

$locations = @(
    "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Startup\NoteBar.lnk",
    "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\NoteBar.lnk"
)

$removed = $false
foreach ($loc in $locations) {
    if (Test-Path $loc) {
        Remove-Item -Path $loc -Force -ErrorAction SilentlyContinue
        Write-Host "Removed startup shortcut: $loc" -ForegroundColor Yellow
        $removed = $true
    }
}

if ($removed) {
    Write-Host "NoteBar auto-startup on boot disabled successfully." -ForegroundColor Green
} else {
    Write-Host "No NoteBar startup shortcut was found." -ForegroundColor Cyan
}
