using BatteryManager.Models;

namespace BatteryManager.Oem;

public sealed class GenericUnsupportedProvider : UnsupportedProviderBase
{
    public GenericUnsupportedProvider(string manufacturer, string model)
        : base(new CompatibilityReport(
            manufacturer,
            model,
            "Windows monitoring only",
            true,
            false,
            false,
            "Monitoring supported, charge limiting not available on this device",
            "Windows exposes battery telemetry broadly, but it does not provide a universal API to enforce a battery charge threshold across laptop vendors."))
    {
    }
}
