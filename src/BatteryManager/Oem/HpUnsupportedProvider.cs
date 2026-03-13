using BatteryManager.Models;

namespace BatteryManager.Oem;

public sealed class HpUnsupportedProvider : UnsupportedProviderBase
{
    public HpUnsupportedProvider(string manufacturer, string model)
        : base(new CompatibilityReport(
            manufacturer,
            model,
            "HP OEM bridge",
            true,
            false,
            false,
            "Monitoring supported, charge limiting not available on this device",
            "HP hardware was detected. HP charge-management behavior is typically firmware-driven and not exposed through a safe generic control surface in Windows user space."))
    {
    }
}
