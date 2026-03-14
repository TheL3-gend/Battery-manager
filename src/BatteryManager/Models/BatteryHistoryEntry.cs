namespace BatteryManager.Models;

public sealed record BatteryHistoryEntry(
    DateTime Timestamp,
    double BatteryPercent,
    bool IsCharging);
