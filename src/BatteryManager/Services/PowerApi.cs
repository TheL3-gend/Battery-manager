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

        var batteryData = TryReadBatteryData();
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
            : EstimateTimeRemaining(state, systemPowerWatts);

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
            batteryData.DetailText);
    }

    private static TimeSpan? EstimateTimeRemaining(SYSTEM_BATTERY_STATE state, double? systemPowerWatts)
    {
        if (!state.Discharging || systemPowerWatts is null || systemPowerWatts <= 0)
        {
            return null;
        }

        return TimeSpan.FromHours(state.RemainingCapacity / 1000d / systemPowerWatts.Value);
    }

    private static BatteryData TryReadBatteryData()
    {
        try
        {
            using var staticSearcher = new ManagementObjectSearcher(@"root\WMI", "SELECT DesignedCapacity, CycleCount FROM BatteryStaticData");
            using var staticResults = staticSearcher.Get();
            var staticData = staticResults.Cast<ManagementObject>().FirstOrDefault();

            using var fullSearcher = new ManagementObjectSearcher(@"root\WMI", "SELECT FullChargedCapacity FROM BatteryFullChargedCapacity");
            using var fullResults = fullSearcher.Get();
            var fullData = fullResults.Cast<ManagementObject>().FirstOrDefault();

            using var tempSearcher = new ManagementObjectSearcher(@"root\WMI", "SELECT Temperature FROM BatteryTemperature");
            using var tempResults = tempSearcher.Get();
            var tempData = tempResults.Cast<ManagementObject>().FirstOrDefault();

            var designedCapacity = ToDouble(staticData?["DesignedCapacity"]);
            var fullChargeCapacity = ToDouble(fullData?["FullChargedCapacity"]);
            var healthPercent = designedCapacity > 0 && fullChargeCapacity > 0
                ? fullChargeCapacity / designedCapacity * 100d
                : null;

            var cycleCount = ToNullableInt(staticData?["CycleCount"]);
            var temperatureRaw = ToDouble(tempData?["Temperature"]);
            double? temperatureCelsius = temperatureRaw > 0
                ? (temperatureRaw / 10d) - 273.15d
                : null;

            return new BatteryData(healthPercent, temperatureCelsius, cycleCount, false, "CallNtPowerInformation", "Windows battery telemetry");
        }
        catch
        {
            return new BatteryData(null, null, null, false, "CallNtPowerInformation", "Extended battery telemetry unavailable");
        }
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
    string DetailText)
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
        "Battery telemetry is unavailable on this device.");
}
