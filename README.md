# Pulse System Monitor

<div align="center">
  <img src="Pulse.App/pulse_logo.png" alt="Pulse Logo" height="200" />
</div>

![Build Status](https://img.shields.io/badge/build-passing-brightgreen) ![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg) ![Version](https://img.shields.io/badge/version-3.0.0-blue)

> A professional-grade Windows telemetry and offensive diagnostics engine.

## Overview
Pulse transforms standard system monitoring into an active enforcement engine. Instead of just watching your CPU and RAM spike, Pulse aggressively quarantines malicious processes via dynamic Windows Firewall routing, predicts memory leaks before they crash your OS, and instantly pipes alerts anywhere in the world via Discord and Telegram. 

## ✨ Features
- **Auto-Quarantine Isolation**: Instantly injects `netsh advfirewall` rules to cleanly block inbound and outbound network traffic of hijacked processes without forcefully killing them.
- **Dynamic Anomaly Detection**: Uses standard deviation and moving averages instead of static thresholds to natively learn what "normal" looks like on your machine.
- **Remote Webhooks Architecture**: Completely integrated Discord & Telegram API payloads for off-site diagnostics.
- **Deep Plugin Ecosystem**: Built-in C# reflection loader seamlessly integrates isolated `.dll` extensions like internal battery drop tracking and monotonic memory leak hunting.
- **SQLite Analytics Dashboard**: Local event database rendering an internal Graphical interface log of every historical system infraction.

## 📦 Installation

To deploy Pulse on any Windows environment:

1. **Clone the repository:**
   ```bash
   git clone https://github.com/Dant3B/Pulse.git
   cd Pulse
   ```
2. **Build the framework:**
   ```bash
   dotnet build -c Release
   ```
3. **Execute the client:**
   ```bash
   dotnet run --project Pulse.App --configuration Release
   ```

*(Requires .NET 8.0 SDK)*

## 🚀 Quick Start
Pulse natively minimizes to the Windows System Tray on launch. 
Double-click the green sphere icon `pulse.ico` to open the central Dashboard. Right-click the icon for quick actions. 
If an anomaly is detected, the icon will dynamically switch to `red_pulse.ico` and a balloon notification will be dispatched alongside any configured webhooks.

## 🔧 Configuration
Configuration is securely retained in `appsettings.json` within your output binary folder and natively configurable within the **Dashboard -> Settings** UI.

| Key | Description | Default |
|-----|-------------|---------|
| `NetworkPollingIntervalMs` | Interval for ICMP ping tests | `5000` |
| `ProcessPollingIntervalMs` | Interval for CPU/Memory heuristics | `10000` |
| `CpuThresholdPercent` | Tolerance limit before flagging CPU | `80.0` |
| `SuspiciousProcesses` | Blacklist for strict monitoring | `notepad, miner, malware` |
| `DiscordWebhookUrl` | Off-site Discord channel endpoint | ` ` |
| `TelegramBotToken` | Off-site Telegram API Bot Auth | ` ` |
| `TelegramChatId` | Target User ID for Telegram routing | ` ` |

## 💡 Advanced Usage: Plugins
To add your own heuristics (like checking GPU temps or external servers):
1. Create a `.NET 8 Class Library`.
2. Reference `Pulse.Core` and implement the `IModule` interface.
3. Drop the compiled `.dll` into the `Plugins/` directory next to the executable. Pulse will automatically discover and run it on reboot.

## 🏗️ Architecture
Pulse operates on a completely decoupled Event-Driven architecture:
- `Pulse.Core`: The central nervous system containing the `ActionEngine` and universal SQLite trackers.
- `Pulse.App`: The WinForms GUI handling contextual rendering and user settings mapping.
- `Pulse.Plugins`: Extracted C# interfaces loaded entirely during runtime to prevent core memory pollution.

## 🤝 Contributing
Contributions are extremely welcome! We are seeking developers to build out the `Pulse.Plugins` ecosystem. Please review our [Contributing Guidelines](.github/CONTRIBUTING.md) to get started!

## 📄 License
Released under the [MIT License](LICENSE).

## 💬 Support
For issues or feature requests, please log a ticket on the GitHub Issues board. For critical zero-day vulnerabilities, review the [Security Policy](.github/SECURITY.md).
