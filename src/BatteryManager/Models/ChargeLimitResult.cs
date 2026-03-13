namespace BatteryManager.Models;

public sealed record ChargeLimitResult(
    bool Success,
    bool ChargeLimitSupported,
    bool TopUpSupported,
    string Message);
