# Pulse

Pulse is a lightweight desktop background application designed for network monitoring and process tracking with minimal system impact.

## Features

- **Network Monitoring**: Periodic ping checks to track latency and connection stability.
- **Process Monitoring**: Detects high-resource processes and logs potential issues.
- **Smart Action Engine**: A trigger-based system for automated responses (e.g., notifications).
- **System Tray UI**: Minimal interface for status checks and module control.
- **Lightweight Logging**: Efficient event logging to `pulse.log`.

## Architecture

- **Pulse.Core**: Contains the core logic, interfaces, and monitoring modules.
- **Pulse.App**: The Windows Forms application that hosts the system tray icon and manages the lifecycle of the modules.

## Requirements

- .NET 8.0 SDK
- Windows OS (for System Tray and WinForms support)

## How to Run

1. Open a terminal in the project root.
2. Run the application:
   ```bash
   dotnet run --project Pulse.App
   ```
3. Look for the Pulse icon in your system tray.

## MVP Scope

- [x] Network monitoring (ping + latency)
- [x] Basic alerts
- [x] Simple process monitoring
- [x] Tray UI
- [x] Logging
