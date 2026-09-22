#Requires -RunAsAdministrator

$ErrorActionPreference = 'SilentlyContinue';

# Stop any running NoteBar processes before upgrading/modifying
Stop-Process -Name "NoteBar*", "notebar" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1