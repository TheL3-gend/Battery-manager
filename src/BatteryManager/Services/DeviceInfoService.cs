using System.Management;

namespace BatteryManager.Services;

public sealed class DeviceInfoService
{
    public (string Manufacturer, string Model) GetDeviceIdentity()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem");
            using var results = searcher.Get();
            var info = results.Cast<ManagementObject>().FirstOrDefault();
            var manufacturer = info?["Manufacturer"]?.ToString()?.Trim() ?? "Unknown";
            var model = info?["Model"]?.ToString()?.Trim() ?? "Unknown";
            return (manufacturer, model);
        }
        catch
        {
            return ("Unknown", "Unknown");
        }
    }
}
