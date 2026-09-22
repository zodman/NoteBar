#Requires -RunAsAdministrator

$ErrorActionPreference = 'Stop';

$NoteBarDir = "$env:Programfiles\NoteBar"
$NoteBarAppData = "$env:APPDATA\NoteBar"

# Stop any running NoteBar processes before uninstall
Stop-Process -Name "NoteBar*", "notebar" -Force -ErrorAction SilentlyContinue

# Delete NoteBar AppData directory
if (Test-Path -Path $NoteBarAppData) {
    Remove-Item -Path $NoteBarAppData -Recurse -Force -ErrorAction SilentlyContinue
} 

# Delete EventSource of NoteBar logging if present
try {
    if ([System.Diagnostics.EventLog]::SourceExists("NoteBar")) {
        [System.Diagnostics.EventLog]::DeleteEventSource("NoteBar")
    }
} catch {
    # Ignore if not found or lacks privileges
}

# Delete Startup shortcuts if created
$startupShortcuts = @(
    "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Startup\NoteBar.lnk",
    "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\NoteBar.lnk"
)
foreach ($lnk in $startupShortcuts) {
    if (Test-Path $lnk) {
        Remove-Item -Path $lnk -Force -ErrorAction SilentlyContinue
    }
}

# Delete Start Menu shortcuts
$startMenuDir = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\NoteBar"
if (Test-Path $startMenuDir) {
    Remove-Item -Path $startMenuDir -Recurse -Force -ErrorAction SilentlyContinue
}

# Delete NoteBar installation directory
if (Test-Path -Path $NoteBarDir) {
    Remove-Item -Path $NoteBarDir -Recurse -Force -ErrorAction SilentlyContinue
}

# Delete notebar from PATH
$Path = [Environment]::GetEnvironmentVariable("Path")
$NewPath = ($Path.Split(";") | Where-Object { $_ -ne $NoteBarDir }) -join ";"
[Environment]::SetEnvironmentVariable("Path", 
    $NewPath, [System.EnvironmentVariableTarget]::Machine)