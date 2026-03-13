using BatteryManager.Models;

namespace BatteryManager.Oem;

public abstract class UnsupportedProviderBase : IChargeLimitProvider
{
    private readonly CompatibilityReport _report;

    protected UnsupportedProviderBase(CompatibilityReport report)
    {
        _report = report;
    }

    public string ProviderName => _report.ProviderName;

    public CompatibilityReport GetCompatibility() => _report;

    public virtual Task<ChargeLimitResult> ApplyChargeLimitAsync(int percent, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ChargeLimitResult(false, false, false, _report.SupportDetail));
    }

    public virtual Task<ChargeLimitResult> EnableTopUpAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new ChargeLimitResult(false, false, false, _report.SupportDetail));
    }

    public virtual Task<ChargeLimitResult> RestorePreviousChargeLimitAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new ChargeLimitResult(false, false, false, _report.SupportDetail));
    }
}
