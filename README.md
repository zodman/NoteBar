# NoteBar: Windows taskbar & system tray indicators

NoteBar is a status indicator for Windows that displays colored dots or custom icons, fully updated for **Windows 11** and **.NET 8**.

![NoteBar on Windows 11](https://i.imgur.com/T47sSle.png)

NoteBar is a Windows clone of [AnyBar](https://github.com/tonsky/AnyBar).

---

## Windows 11 Support & Features

In Windows 11, Microsoft completely removed support for legacy taskbar DeskBands and toolbars (which caused older NoteBar v1.0 and AnyBar Windows ports to stop working). 

NoteBar v1.1 has been rebuilt from the ground up to provide seamless Windows 11 support with dual display modes:

1. **Floating Taskbar Pill**:
   - A modern, draggable Fluent pill widget designed specifically for Windows 11.
   - Click and drag the `⋮⋮` handle to place it anywhere along your taskbar or anywhere on screen.
   - Right-click menu options: **Center Bar on Screen**, **Reset Bar Position**, **Hide Floating Bar (Keep in Tray)**, or **Exit NoteBar**.
   - Remembers its coordinates across restarts in `%APPDATA%\NoteBar\settings.json`.
2. **System Tray Icon (`NotifyIcon`)**:
   - Native icon in your Windows 11 notification area (system tray) next to the clock.
   - Fully interactive with context menu support and status tooltips.
   - *Tip: Windows 11 groups new icons inside the `^` overflow menu by default. Click `^` and drag the NoteBar icon down to your taskbar to keep it permanently visible.*
3. **Dual-Stack UDP Listening**:
   - Automatically binds to both `0.0.0.0` (IPv4) and `[::]` (IPv6).
   - Accepts status updates locally or across the network, including from WSL, Docker containers, VMs, and remote CI scripts.

---

## System Requirements

Before running NoteBar, ensure your machine meets the following requirements:

| Requirement | Details |
| :--- | :--- |
| **Operating System** | **Windows 11** (all editions) or Windows 10 (1809+, 64-bit) |
| **.NET Runtime** | [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (`Microsoft.WindowsDesktop.App` 8.0+) |
| **Network / UDP** | UDP port `1738` (default) or custom ports (`-p <port>`) open on loopback / LAN |

> **Quick Runtime Install:**
> If you don't have .NET 8 Desktop Runtime installed yet:
> ```powershell
> winget install Microsoft.DotNet.DesktopRuntime.8
> # or via Chocolatey:
> choco install dotnet-desktopruntime -y
> ```

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

# To install AND automatically configure NoteBar to start on Windows boot:
choco install notebar --source="'$env:TEMP'" --params "'/AutoStart'" -y
```

Or as a single command:
```powershell
irm https://github.com/zodman/NoteBar/releases/latest/download/notebar.1.1.0.nupkg -OutFile "$env:TEMP\notebar.1.1.0.nupkg"; choco install notebar --source="'$env:TEMP'" --params "'/AutoStart'" -y
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

# Or install with auto-start on boot:
choco install notebar --source="'.'" --params "'/AutoStart'" -y
```

---

### Option 3: Standalone Portable ZIP

1. Download `NoteBar-windows-x64.zip` from [Releases](https://github.com/zodman/NoteBar/releases).
2. Extract the archive to any folder (e.g. `C:\Tools\NoteBar`).
3. Add the folder to your `PATH` or launch `notebar.exe` directly.

---

## Launch on System Boot (Auto-Start)

NoteBar can be configured to start automatically whenever you log into Windows or when the system boots up:

### Method 1: During Chocolatey Installation
Pass the `--params "'/AutoStart'"` flag when installing:
```powershell
choco install notebar --source="'$env:TEMP'" --params "'/AutoStart'" -y
```

### Method 2: Using the Included PowerShell Scripts
The Chocolatey package installs helper scripts directly into `$env:ProgramFiles\NoteBar\` (and they are also available in `chocolatey/tools/`):

- **Enable Auto-Start for Current User:**
  ```powershell
  & "$env:ProgramFiles\NoteBar\Enable-NoteBarStartup.ps1"
  ```
- **Enable Auto-Start for All Users (Requires Administrator):**
  ```powershell
  & "$env:ProgramFiles\NoteBar\Enable-NoteBarStartup.ps1" -AllUsers
  ```
- **Enable Auto-Start on a Custom UDP Port:**
  ```powershell
  & "$env:ProgramFiles\NoteBar\Enable-NoteBarStartup.ps1" -Port 1739
  ```
- **Disable Auto-Start:**
  ```powershell
  & "$env:ProgramFiles\NoteBar\Disable-NoteBarStartup.ps1"
  ```

### Method 3: Manual Startup Shortcut
1. Press <kbd>Win</kbd> + <kbd>R</kbd>, type `shell:startup`, and press <kbd>Enter</kbd>.
2. Right-click inside the folder &rarr; **New** &rarr; **Shortcut**.
3. Set the target to `"C:\Program Files\NoteBar\NoteBar.Wpf.exe" --port 1738`.
4. Click **Next**, name it `NoteBar`, and click **Finish**.

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
