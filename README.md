# NoteBar: Windows taskbar & system tray indicators

NoteBar is a status indicator for Windows that displays colored dots or custom icons, fully updated for **Windows 11** and **.NET 8**.

NoteBar is a Windows clone of [AnyBar](https://github.com/tonsky/AnyBar).

---

## Features & Windows 11 Support

Because Windows 11 removed legacy DeskBand toolbars, NoteBar has been completely modernized with dual display modes:

1. **System Tray Icon (`NotifyIcon`)**: Directly in your Windows 11 notification area next to the clock.
   > *Tip: Windows 11 places new notification icons inside the `^` overflow menu by default. Click `^` and drag NoteBar to your visible taskbar to keep it permanently pinned.*
2. **Floating Taskbar Bar**: A sleek, draggable fluent pill widget docked near your taskbar.
   - Click and drag the `⋮⋮` handle to move it anywhere.
   - Right-click for options: **Center Bar on Screen**, **Reset Bar Position**, **Hide Floating Bar (Keep in Tray)**, or **Exit NoteBar**.
3. **Dual-Stack UDP Listening**: Binds to `0.0.0.0` (IPv4) and `[::]` (IPv6), making it easily accessible from local tools, WSL, local network machines, or Docker containers.

---

## Installation

### Option 1: Install with Chocolatey (from GitHub Releases)

You can install NoteBar via [Chocolatey](https://chocolatey.org/) using the `.nupkg` package hosted directly in the [zodman/NoteBar GitHub Releases](https://github.com/zodman/NoteBar/releases).

Open **PowerShell as Administrator** and run:

```powershell
# Download the package from the GitHub release
Invoke-WebRequest -Uri "https://github.com/zodman/NoteBar/releases/latest/download/notebar.1.1.0.nupkg" -OutFile "$env:TEMP\notebar.1.1.0.nupkg"

# Install using Chocolatey pointing to the download directory
choco install notebar --source="'$env:TEMP'" -y
```

Or as a single command:
```powershell
irm https://github.com/zodman/NoteBar/releases/latest/download/notebar.1.1.0.nupkg -OutFile "$env:TEMP\notebar.1.1.0.nupkg"; choco install notebar --source="'$env:TEMP'" -y
```

#### Upgrading
```powershell
Invoke-WebRequest -Uri "https://github.com/zodman/NoteBar/releases/latest/download/notebar.1.1.0.nupkg" -OutFile "$env:TEMP\notebar.1.1.0.nupkg"
choco upgrade notebar --source="'$env:TEMP'" -y
```

#### Uninstalling
```powershell
choco uninstall notebar -y
```

---

### Option 2: Build and Install from Source (Custom Repo)

If you have cloned this repository and have the .NET 8 SDK and Chocolatey installed:

```powershell
# Clone the repository
git clone https://github.com/zodman/NoteBar.git
cd NoteBar

# Publish executables into the Chocolatey packaging payload directory
dotnet publish src/NoteBar.App/NoteBar.App.csproj -c Release -o ./chocolatey/tools/src
dotnet publish src/NoteBar.Wpf/NoteBar.Wpf.csproj -c Release -o ./chocolatey/tools/src

# Create the Chocolatey package
choco pack chocolatey/notebar.nuspec

# Install locally (in PowerShell as Administrator)
choco install notebar --source="'.'" -y
```

---

### Option 3: Standalone Portable ZIP

1. Download `NoteBar-windows-x64.zip` from [Releases](https://github.com/zodman/NoteBar/releases).
2. Extract the archive to any folder (e.g. `C:\Tools\NoteBar`).
3. Add the folder to your `PATH` or launch `notebar.exe` directly.

---

## Getting Started

### Launching NoteBar

Run the CLI command in PowerShell, CMD, or Windows Terminal:

```powershell
notebar
```

This starts the NoteBar host and adds an indicator listening on UDP port **1738** bound to `0.0.0.0` and `[::]`.

### Changing Colors via UDP

Send a UDP packet containing the color or icon name to port 1738. Both the floating bar and the system tray icon will change color immediately.

#### PowerShell:
```powershell
$Message = [System.Text.Encoding]::UTF8.GetBytes("red");
(New-Object System.Net.Sockets.UDPClient).Send($Message, $Message.length, "127.0.0.1", 1738)
```

#### ncat / netcat:
```powershell
echo -n "green" | ncat -4u -w1 127.0.0.1 1738
```

#### WSL / Linux / Bash:
```bash
echo -n "orange" > /dev/udp/127.0.0.1/1738
```

#### Python:
```python
import socket
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.sendto(b"blue", ("127.0.0.1", 1738))
```

---

### Supported Default Colors & Icons

- `white`
- `red`
- `orange`
- `yellow`
- `green`
- `cyan`
- `blue`
- `purple`
- `black`
- `question`
- `exclamation`

Sending `quit` over UDP closes the indicator on that port.

---

## Running Multiple Indicators

You can run several indicators concurrently on different ports:

```powershell
notebar -p 1738
notebar -p 1739
notebar -p 1740
```

To close a specific indicator:
```powershell
notebar -q -p 1739
```

To stop NoteBar completely:
```powershell
notebar --exit
```

---

## Custom Icons

NoteBar detects and uses local custom images stored in `%APPDATA%\NoteBar`. E.g., if you place `%APPDATA%\NoteBar\mybadge.png`, sending `mybadge` over UDP will display it. Recommended dimensions: 16×16 to 32×32 pixels PNG.

---

## CI / CD & GitHub Actions

This repository uses GitHub Actions (`.github/workflows/build-and-release.yml`) to automatically:
- Build and test the .NET 8 solution on every push and pull request.
- Create portable zip packages (`NoteBar-windows-x64.zip`).
- Build self-contained Chocolatey packages (`notebar.<version>.nupkg`).
- Publish releases and release assets automatically when a tag `v*` is pushed or a GitHub release is created.
