using BatteryManager.Models;

namespace BatteryManager.Oem;

public sealed class LenovoUnsupportedProvider : UnsupportedProviderBase
{
    public LenovoUnsupportedProvider(string manufacturer, string model)
        : base(new CompatibilityReport(
            manufacturer,
            model,
            "Lenovo OEM bridge",
            true,
            false,
            false,
            "Monitoring supported, charge limiting not available on this device",
            "Lenovo hardware was detected, but no tested local Lenovo battery-threshold provider has been added for this model family yet. Lenovo systems often require firmware-specific interfaces or Lenovo software integration."))
    {
    }
}
