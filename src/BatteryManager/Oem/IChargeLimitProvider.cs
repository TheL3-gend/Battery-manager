using BatteryManager.Models;

namespace BatteryManager.Oem;

public interface IChargeLimitProvider
{
    string ProviderName { get; }
    CompatibilityReport GetCompatibility();
    Task<ChargeLimitResult> ApplyChargeLimitAsync(int percent, CancellationToken cancellationToken);
    Task<ChargeLimitResult> EnableTopUpAsync(CancellationToken cancellationToken);
    Task<ChargeLimitResult> RestorePreviousChargeLimitAsync(CancellationToken cancellationToken);
}
