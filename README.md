# Battery Manager

Battery Manager is a native Windows desktop application built with WPF for low-overhead battery telemetry, OEM capability detection, tray/background monitoring, and an honest charge-limit workflow.

## What it does

- Monitors battery percentage, charge/discharge state, estimated time remaining, battery health, cycle count, and battery temperature when Windows exposes them.
- Reads live battery charge or discharge rate in watts through native Windows power telemetry.
- Shows historical charts for battery percentage, charge watts, and system power draw.
- Detects the laptop OEM and surfaces whether real hardware charge limiting is available.
- Includes a `Top Up` workflow and charge-threshold UI that stay disabled on unsupported systems instead of pretending control exists.
- Runs in the system tray and can keep monitoring in the background.
- Throttles repetitive notifications so limit-reached and high-temperature alerts are tied to state changes instead of every polling cycle.

## Project structure

- `src/BatteryManager/App.xaml.cs`: application bootstrap and service wiring.
- `src/BatteryManager/Views`: WPF windows.
- `src/BatteryManager/ViewModels`: presentation logic and live state updates.
- `src/BatteryManager/Services`: telemetry polling, theme management, settings, tray integration, and OEM detection.
- `src/BatteryManager/Oem`: charge-limit provider abstraction plus OEM-specific placeholder providers for Lenovo, ASUS, Dell, HP, and generic Windows-only systems.
- `src/BatteryManager/Controls`: lightweight custom chart control for history graphs.

## Build instructions

1. Install the .NET 8 SDK or newer with Windows Desktop workload support.
2. Restore packages:

```powershell
dotnet restore .\src\BatteryManager\BatteryManager.csproj
```

3. Build:

```powershell
dotnet build .\src\BatteryManager\BatteryManager.csproj -c Release
```

4. Run:

```powershell
dotnet run --project .\src\BatteryManager\BatteryManager.csproj -c Release
```

You can also open `BatteryManager.sln` in Visual Studio 2022 and run it there.

## How metrics are collected

### Native Windows telemetry

The app uses `CallNtPowerInformation(SystemBatteryState)` from `powrprof.dll` as the primary live data source. That provides:

- AC / battery state
- charging vs. discharging state
- remaining capacity
- maximum capacity
- instantaneous battery rate in milliwatts when the platform reports it
- estimated remaining runtime when available

The app converts the reported battery rate to watts for the dashboard.

### WMI extensions

When available, the app also queries `root\\WMI` battery classes such as:

- `BatteryStaticData`
- `BatteryFullChargedCapacity`
- `BatteryTemperature`

Those are used for battery health, cycle count, and temperature. If they are not exposed by the firmware, the UI explicitly marks those fields as unavailable.

## Important limitations

### Universal Windows limitations

Windows does **not** expose a universal API to enforce a battery charge threshold across all OEM laptops. That is why the app separates:

- monitoring support
- OEM charge-limit support

On unsupported systems, Battery Manager remains a monitoring dashboard only.

### Watt breakdown limitations

These values are not universally available from Windows:

- how much adapter power is going directly to the system
- total adapter input power while charging

Because of that, the app only shows those values when a supported provider exposes them. Otherwise it labels them as unavailable instead of fabricating numbers.

### OEM restrictions

Real charge limiting usually depends on OEM BIOS, ACPI, EC, WMI, or vendor service integrations. The codebase already includes a provider interface so model-specific modules can be added later for Lenovo, ASUS, Dell, HP, and others.

## Extending OEM support

Implement `IChargeLimitProvider` in `src/BatteryManager/Oem` and update `OemDetectionService` to return the provider for the detected model family. The project already splits OEM placeholder providers into separate classes, so real hardware threshold control and top-up restore logic can stay isolated to the relevant vendor module once the required interface is known and tested.
