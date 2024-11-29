# LighthouseManager
Command-Line tool to manage SteamVR Lighthouse 

Inspired by [Lighthouse Keeper](https://github.com/rossbearman/lighthouse-keeper#lighthouse-keeper) this is a .NET 8 solution to discover and manage Valve Lighthouse Base Stations.
It uses [Windows Bluetooth Low Energy SDK](https://docs.microsoft.com/de-de/windows/uwp/devices-sensors/bluetooth-low-energy-overview) to communicate with Base Stations.

## Requirements
- Windows 10 64bit (minimum 10.0.19041.0)
- Integrated Bluetooth or dongle managed by Windows

## Usage
With Command Promt or Powershell navigate to the location where LighthouseManager.exe is located.

Functions / parameters:

- `-d` or `--discover` Discover new Base Stations to get their MAC-Addresses.
- `-i` or `--identify` `-a AA:AA:AA:AA:AA` Identify a base station by flashing the LED.
- `-w` or `--wake` `-a AA:AA:AA:AA:AA:AA,BB:BB:BB:BB:BB:BB` Wake one or more Base Stations (write one or more addresses)
- `-s` or `--sleep` `-a AA:AA:AA:AA:AA:AA,BB:BB:BB:BB:BB:BB` Sleep one or more Base Stations (write one or more addresses)

## Exit codes
Based on success or not LighthouseManager will exit with different codes
- `0` All commands executed successfully
- `1` One or more commands failed after given retry attempt

## LighthouseManagerService
LighthouseManagerService is a litte tool (Windows Worker Service) that monitors Pimax' vrserver process (pi_server.exe) for starting or closing in a given interval (default 2500ms) and then starting LighthouseManager with corresponding parameters. You can configure interval (minimum 1000 milliseconds) and base station MAC addresses in appsettings.json.
Open appsettings.json and set your Base Station Mac Addresses (You can disover them with LighthouseManager `--discover` parameter).
It is possible to just run LighthouseManagerService or use it as a Windows Service (recommended).

## Installation as Windows Service
[Manually](https://github.com/seader/LighthouseManagerPimax/releases/download/%23first_release/LighthouseManager-manual-files.zip):
- Open a Command Prompt as Administrator and type `sc create LighthouseManager DisplayName="LighthouseManager" binPath="C:\PATHTOEXTRACTEDFILES\LighthouseManagerService.exe"` to create the Windows Service and then `sc start LighthouseManager` to start it. For auto start type `sc config LighthouseManager start=auto`.
- You can uninstall it with `sc delete LighthouseManager` (If you want stop it before uninstalling with `sc stop LighthouseManager`).

[Installer](https://github.com/seader/LighthouseManagerPimax/releases/download/%23first_release/LighthouseManager-Service-setup-files.zip):
- Extract the installer archive and just run the LighthouseManagement.msi

## Building with VS2022
Needed:
- Depends on .net 8.0, publishing results in selfcontaining .exe files

For .msi installer creation:
- wix Toolset v4 
- HeatWave

Everything else should be already available when using latest VS2022

First publish Manager, Service and Shared, afterwards build the installer. 

# Acknowledgements
[Rossbearman](https://github.com/rossbearman) for helping me with some Bluetooth LE perfomance problems in Windows BLE API.
[BenWoodford](https://gist.github.com/BenWoodford) for [this](https://gist.github.com/BenWoodford/3a1e500a4ea2673525f5adb4120fd47c) awesome Lighthouse 2.0 GATT documentation.

