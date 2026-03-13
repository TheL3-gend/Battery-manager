namespace BatteryManager.Models;

public sealed record CompatibilityReport(
    string Manufacturer,
    string Model,
    string ProviderName,
    bool MonitoringSupported,
    bool ChargeLimitSupported,
    bool TopUpSupported,
    string SupportHeadline,
    string SupportDetail);
