namespace BatteryManager.Models;

public sealed record BatterySnapshot(
    DateTime Timestamp,
    bool IsBatteryPresent,
    bool IsOnAcPower,
    bool IsCharging,
    bool IsDischarging,
    double? BatteryPercent,
    double? BatteryPowerWatts,
    double? ChargingPowerWatts,
    double? SystemPowerWatts,
    double? SystemPowerFromAdapterWatts,
    double? BatteryHealthPercent,
    double? BatteryTemperatureCelsius,
    TimeSpan? EstimatedTimeRemaining,
    int? CycleCount,
    bool AnyWattsEstimated,
    bool SystemPowerEstimated,
    bool AdapterPowerEstimated,
    string RateSource,
    string StatusText,
    string DetailText);
