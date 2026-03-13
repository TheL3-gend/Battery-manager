using BatteryManager.Models;
using BatteryManager.Oem;

namespace BatteryManager.Services;

public sealed class ChargeLimitCoordinator
{
    private readonly IChargeLimitProvider _provider;
    private int? _previousLimit;

    public ChargeLimitCoordinator(OemDetectionService detectionService)
    {
        _provider = detectionService.CreateProvider();
    }

    public CompatibilityReport Compatibility => _provider.GetCompatibility();

    public Task<ChargeLimitResult> ApplyChargeLimitAsync(int percent, CancellationToken cancellationToken)
    {
        _previousLimit = percent;
        return _provider.ApplyChargeLimitAsync(percent, cancellationToken);
    }

    public Task<ChargeLimitResult> EnableTopUpAsync(CancellationToken cancellationToken)
    {
        return _provider.EnableTopUpAsync(cancellationToken);
    }

    public Task<ChargeLimitResult> RestorePreviousChargeLimitAsync(CancellationToken cancellationToken)
    {
        return _provider.RestorePreviousChargeLimitAsync(cancellationToken);
    }
}
