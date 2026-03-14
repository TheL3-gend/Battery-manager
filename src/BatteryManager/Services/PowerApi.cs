using System.Management;
using System.Runtime.InteropServices;

namespace BatteryManager.Services;

public sealed class PowerApi
{
    public PowerReading Read()
    {
        if (CallNtPowerInformation(
                POWER_INFORMATION_LEVEL.SystemBatteryState,
                IntPtr.Zero,
                0,
                out var state,
                Marshal.SizeOf<SYSTEM_BATTERY_STATE>()) != 0)
        {
            return PowerReading.Empty;
        }

        var batteryData = ReadBatteryData();
        var batteryPresent = state.BatteryPresent;

        double? batteryPercent = null;
        if (batteryPresent && state.MaxCapacity > 0)
        {
            batteryPercent = state.RemainingCapacity * 100d / state.MaxCapacity;
        }

        var signedRateMilliwatts = state.Rate;
        var absoluteRateMilliwatts = Math.Abs((double)signedRateMilliwatts);
        double? rateWatts = absoluteRateMilliwatts > 0 ? absoluteRateMilliwatts / 1000d : null;
        var isCharging = state.Charging;
        var isDischarging = state.Discharging;

        double? chargingWatts = isCharging ? rateWatts : null;
        double? systemPowerWatts = isDischarging ? rateWatts : null;
        double? batteryPowerWatts = (isCharging || isDischarging) ? rateWatts : null;

        var estimatedTime = state.EstimatedTime > 0 && state.EstimatedTime < uint.MaxValue
            ? TimeSpan.FromSeconds(state.EstimatedTime)
            : batteryData.EstimatedRuntime
                ?? batteryData.Win32EstimatedRuntime
                ?? EstimateTimeRemaining(state, systemPowerWatts);

        return new PowerReading(
            batteryPresent,
            state.AcOnLine,
            isCharging,
            isDischarging,
            batteryPercent,
            batteryPowerWatts,
            chargingWatts,
            systemPowerWatts,
            null,
            batteryData.HealthPercent,
            batteryData.TemperatureCelsius,
            estimatedTime,
            batteryData.CycleCount,
            batteryData.HasEstimatedRate,
            batteryData.RateSource,
            batteryData.DetailText,
            batteryData.RemainingCapacityMilliwattHours,
            batteryData.FullChargedCapacityMilliwattHours);
    }

    private static TimeSpan? EstimateTimeRemaining(SYSTEM_BATTERY_STATE state, double? systemPowerWatts)
    {
        if (!state.Discharging || systemPowerWatts is null || systemPowerWatts <= 0)
        {
            return null;
        }

        return TimeSpan.FromHours(state.RemainingCapacity / 1000d / systemPowerWatts.Value);
    }

    private static BatteryData ReadBatteryData()
    {
        var designedCapacity = QueryWmiValue(@"root\WMI", "SELECT DesignedCapacity FROM BatteryStaticData", "DesignedCapacity");
        var fullChargeCapacity = QueryWmiValue(@"root\WMI", "SELECT FullChargedCapacity FROM BatteryFullChargedCapacity", "FullChargedCapacity");
        var cycleCount = QueryWmiInt(@"root\WMI", "SELECT CycleCount FROM BatteryCycleCount", "CycleCount")
            ?? QueryWmiInt(@"root\WMI", "SELECT CycleCount FROM BatteryStaticData", "CycleCount");
        var batteryTemperature = QueryWmiTemperature(@"root\WMI", "SELECT Temperature FROM BatteryTemperature", "Temperature");
        var deviceTemperature = QueryFormattedDeviceTemperature();
        var runtimeSeconds = QueryWmiValue(@"root\WMI", "SELECT EstimatedRuntime FROM BatteryRuntime", "EstimatedRuntime");
        var remainingCapacity = QueryWmiValue(@"root\WMI", "SELECT RemainingCapacity FROM BatteryStatus", "RemainingCapacity");
        var win32EstimatedRuntime = QueryWmiValue(@"root\\cimv2", "SELECT EstimatedRunTime FROM Win32_Battery", "EstimatedRunTime");

        var healthPercent = designedCapacity > 0 && fullChargeCapacity > 0
            ? fullChargeCapacity / designedCapacity * 100d
            : null;

        var detail = batteryTemperature is not null
            ? "Windows battery telemetry"
            : deviceTemperature is not null
                ? "Windows battery telemetry with system thermal fallback"
                : "Windows battery telemetry";

        return new BatteryData(
            healthPercent,
            batteryTemperature ?? deviceTemperature,
            cycleCount,
            runtimeSeconds > 0 ? TimeSpan.FromSeconds(runtimeSeconds.Value) : null,
            win32EstimatedRuntime > 0 ? TimeSpan.FromMinutes(win32EstimatedRuntime.Value) : null,
            remainingCapacity,
            fullChargeCapacity,
            false,
            "CallNtPowerInformation",
            detail);
    }

    private static double? QueryWmiValue(string scopePath, string query, string propertyName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(scopePath, query);
            using var results = searcher.Get();
            var data = results.Cast<ManagementObject>().FirstOrDefault();
            return ToDouble(data?[propertyName]);
        }
        catch
        {
            return null;
        }
    }

    private static int? QueryWmiInt(string scopePath, string query, string propertyName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(scopePath, query);
            using var results = searcher.Get();
            var data = results.Cast<ManagementObject>().FirstOrDefault();
            return ToNullableInt(data?[propertyName]);
        }
        catch
        {
            return null;
        }
    }

    private static double? QueryWmiTemperature(string scopePath, string query, string propertyName)
    {
        var rawValue = QueryWmiValue(scopePath, query, propertyName);
        return rawValue > 0 ? (rawValue / 10d) - 273.15d : null;
    }

    private static double? QueryFormattedDeviceTemperature()
    {
        var thermalZoneTemperature = QueryWmiValue(
            @"root\cimv2",
            "SELECT Temperature, HighPrecisionTemperature FROM Win32_PerfFormattedData_Counters_ThermalZoneInformation",
            "HighPrecisionTemperature");

        if (thermalZoneTemperature > 0)
        {
            return (thermalZoneTemperature.Value / 10d) - 273.15d;
        }

        thermalZoneTemperature = QueryWmiValue(
            @"root\cimv2",
            "SELECT Temperature FROM Win32_PerfFormattedData_Counters_ThermalZoneInformation",
            "Temperature");

        return thermalZoneTemperature > 0 ? thermalZoneTemperature - 273.15d : null;
    }

    private static double? ToDouble(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return double.TryParse(value.ToString(), out var result) ? result : null;
    }

    private static int? ToNullableInt(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return int.TryParse(value.ToString(), out var result) ? result : null;
    }

    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern uint CallNtPowerInformation(
        POWER_INFORMATION_LEVEL informationLevel,
        IntPtr inputBuffer,
        uint inputBufferLength,
        out SYSTEM_BATTERY_STATE outputBuffer,
        int outputBufferLength);

    private enum POWER_INFORMATION_LEVEL
    {
        SystemBatteryState = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_BATTERY_STATE
    {
        [MarshalAs(UnmanagedType.U1)]
        public bool AcOnLine;

        [MarshalAs(UnmanagedType.U1)]
        public bool BatteryPresent;

        [MarshalAs(UnmanagedType.U1)]
        public bool Charging;

        [MarshalAs(UnmanagedType.U1)]
        public bool Discharging;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Spare1;

        public byte Tag;
        public uint MaxCapacity;
        public uint RemainingCapacity;
        public int Rate;
        public uint EstimatedTime;
        public uint DefaultAlert1;
        public uint DefaultAlert2;
    }

    private sealed record BatteryData(
        double? HealthPercent,
        double? TemperatureCelsius,
        int? CycleCount,
        TimeSpan? EstimatedRuntime,
        TimeSpan? Win32EstimatedRuntime,
        double? RemainingCapacityMilliwattHours,
        double? FullChargedCapacityMilliwattHours,
        bool HasEstimatedRate,
        string RateSource,
        string DetailText);
}

public sealed record PowerReading(
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
    string RateSource,
    string DetailText,
    double? RemainingCapacityMilliwattHours,
    double? FullChargedCapacityMilliwattHours)
{
    public static PowerReading Empty { get; } = new(
        false,
        false,
        false,
        false,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        false,
        "Unavailable",
        "Battery telemetry is unavailable on this device.",
        null,
        null);
}
