using BatteryManager.Models;
using BatteryManager.Oem;

namespace BatteryManager.Services;

public sealed class OemDetectionService
{
    private readonly DeviceInfoService _deviceInfoService;

    public OemDetectionService(DeviceInfoService deviceInfoService)
    {
        _deviceInfoService = deviceInfoService;
    }

    public IChargeLimitProvider CreateProvider()
    {
        var (manufacturer, model) = _deviceInfoService.GetDeviceIdentity();
        var normalized = manufacturer.ToLowerInvariant();

        return normalized switch
        {
            var value when value.Contains("lenovo") => new LenovoUnsupportedProvider(manufacturer, model),
            var value when value.Contains("asus") => new AsusUnsupportedProvider(manufacturer, model),
            var value when value.Contains("dell") => new DellUnsupportedProvider(manufacturer, model),
            var value when value.Contains("hp") || value.Contains("hewlett") => new HpUnsupportedProvider(manufacturer, model),
            _ => new GenericUnsupportedProvider(manufacturer, model)
        };
    }
}
