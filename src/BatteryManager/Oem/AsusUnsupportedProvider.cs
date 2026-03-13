using BatteryManager.Models;

namespace BatteryManager.Oem;

public sealed class AsusUnsupportedProvider : UnsupportedProviderBase
{
    public AsusUnsupportedProvider(string manufacturer, string model)
        : base(new CompatibilityReport(
            manufacturer,
            model,
            "ASUS OEM bridge",
            true,
            false,
            false,
            "Monitoring supported, charge limiting not available on this device",
            "ASUS hardware was detected. ASUS battery-care modes are usually exposed through model-specific firmware or ASUS utilities rather than a stable public Windows API."))
    {
    }
}
