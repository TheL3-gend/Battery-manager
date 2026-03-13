using BatteryManager.Models;

namespace BatteryManager.Oem;

public sealed class DellUnsupportedProvider : UnsupportedProviderBase
{
    public DellUnsupportedProvider(string manufacturer, string model)
        : base(new CompatibilityReport(
            manufacturer,
            model,
            "Dell OEM bridge",
            true,
            false,
            false,
            "Monitoring supported, charge limiting not available on this device",
            "Dell hardware was detected. Charge thresholds are commonly controlled through BIOS configuration or Dell vendor tooling, not universal Windows battery APIs."))
    {
    }
}
