using BatteryManager.Models;

namespace BatteryManager.Services;

public sealed class BatteryMonitorService : IDisposable
{
    private readonly ChargeLimitCoordinator _chargeLimitCoordinator;
    private readonly PowerApi _powerApi = new();
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private BatterySnapshot? _previousSnapshot;

    public BatteryMonitorService(ChargeLimitCoordinator chargeLimitCoordinator)
    {
        _chargeLimitCoordinator = chargeLimitCoordinator;
    }

    public event EventHandler<BatterySnapshotEventArgs>? SnapshotUpdated;

    public void Start(int intervalSeconds)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _loopTask = RunLoopAsync(TimeSpan.FromSeconds(Math.Max(1, intervalSeconds)), _cts.Token);
    }

    public void Stop()
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    private async Task RunLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval);

        while (!cancellationToken.IsCancellationRequested)
        {
            PublishSnapshot();

            try
            {
                await timer.WaitForNextTickAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void PublishSnapshot()
    {
        var reading = _powerApi.Read();
        var compatibility = _chargeLimitCoordinator.Compatibility;
        var snapshot = BuildSnapshot(reading, compatibility);
        _previousSnapshot = snapshot;
        SnapshotUpdated?.Invoke(this, new BatterySnapshotEventArgs(snapshot));
    }

    private BatterySnapshot BuildSnapshot(PowerReading reading, CompatibilityReport compatibility)
    {
        var liveBatteryFlowWatts = reading.BatteryPowerWatts;
        var systemPowerWatts = reading.SystemPowerWatts;
        var adapterToSystemWatts = reading.SystemPowerFromAdapterWatts;
        var anyWattsEstimated = reading.AnyWattsEstimated;
        var systemPowerEstimated = false;
        var adapterPowerEstimated = false;
        var detailSuffix = reading.DetailText;

        if (systemPowerWatts is null && reading.IsDischarging && liveBatteryFlowWatts is not null)
        {
            systemPowerWatts = liveBatteryFlowWatts;
        }

        if (reading.IsOnAcPower && systemPowerWatts is null && TryEstimateSystemPower(out var estimatedSystemPower))
        {
            systemPowerWatts = estimatedSystemPower;
            anyWattsEstimated = true;
            systemPowerEstimated = true;
            detailSuffix = "Some live power paths are estimated from recent battery discharge history because Windows does not expose adapter-to-system routing on this device.";
        }

        if (reading.IsOnAcPower && systemPowerWatts is null && liveBatteryFlowWatts is not null)
        {
            systemPowerWatts = liveBatteryFlowWatts;
            anyWattsEstimated = true;
            systemPowerEstimated = true;
            detailSuffix = "System draw is approximated from the current live battery flow because Windows does not expose direct adapter-routing telemetry on this device.";
        }

        if (reading.IsOnAcPower)
        {
            if (adapterToSystemWatts is null && systemPowerWatts is not null)
            {
                adapterToSystemWatts = systemPowerWatts;
                anyWattsEstimated = true;
                adapterPowerEstimated = true;
            }
        }
        else
        {
            adapterToSystemWatts = 0;
        }

        var statusText = !reading.IsBatteryPresent
            ? "No battery detected"
            : reading.IsCharging
                ? "Charging"
                : reading.IsDischarging
                    ? "Running on battery"
                    : reading.IsOnAcPower
                        ? "Plugged in"
                        : "Idle";

        var detail = compatibility.ChargeLimitSupported
            ? "Real hardware charge limiting supported"
            : compatibility.SupportHeadline;

        return new BatterySnapshot(
            DateTime.Now,
            reading.IsBatteryPresent,
            reading.IsOnAcPower,
            reading.IsCharging,
            reading.IsDischarging,
            reading.BatteryPercent,
            reading.BatteryPowerWatts,
            reading.ChargingPowerWatts,
            systemPowerWatts,
            adapterToSystemWatts,
            reading.BatteryHealthPercent,
            reading.BatteryTemperatureCelsius,
            reading.EstimatedTimeRemaining,
            reading.CycleCount,
            anyWattsEstimated,
            systemPowerEstimated,
            adapterPowerEstimated,
            reading.RateSource,
            statusText,
            $"{detail}  {detailSuffix}".Trim());
    }

    private bool TryEstimateSystemPower(out double? watts)
    {
        watts = null;
        if (_previousSnapshot is null ||
            _previousSnapshot.SystemPowerWatts is null ||
            _previousSnapshot.SystemPowerEstimated ||
            DateTime.Now - _previousSnapshot.Timestamp > TimeSpan.FromMinutes(10))
        {
            return false;
        }

        watts = _previousSnapshot.SystemPowerWatts;
        return true;
    }

    public void Dispose()
    {
        Stop();
    }
}

public sealed class BatterySnapshotEventArgs : EventArgs
{
    public BatterySnapshotEventArgs(BatterySnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public BatterySnapshot Snapshot { get; }
}
