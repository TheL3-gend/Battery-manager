using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using BatteryManager.Models;
using BatteryManager.Services;
using ToolTipIcon = System.Windows.Forms.ToolTipIcon;

namespace BatteryManager.ViewModels;

public sealed class MainViewModel : BindableBase, IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly BatteryMonitorService _batteryMonitorService;
    private readonly ChargeLimitCoordinator _chargeLimitCoordinator;
    private readonly HistoryBuffer _batteryPercentHistory = new(90);
    private readonly HistoryBuffer _chargePowerHistory = new(90);
    private readonly HistoryBuffer _systemPowerHistory = new(90);
    private readonly RelayCommand _applyChargeLimitCommand;
    private readonly RelayCommand _enableTopUpCommand;
    private readonly RelayCommand _closeCommand;
    private readonly RelayCommand _dismissInfoBannerCommand;
    private BatterySnapshot? _latestSnapshot;
    private bool _allowWindowClose;
    private bool _chargeLimitReachedNotified;
    private bool _highTemperatureNotified;
    private bool _topUpActive;
    private bool _useDarkMode;
    private bool _backgroundMonitoringEnabled;
    private bool _startMinimizedToTray;
    private int _refreshIntervalSeconds;
    private int _selectedChargeLimit;
    private bool _useCustomChargeLimit;
    private int _customChargeLimit;
    private string _statusHeadline = "Waiting for telemetry";
    private string _statusDetail = "Starting battery monitor";
    private string _compatibilityHeadline = "Detecting support";
    private string _compatibilityDetail = string.Empty;
    private string _providerName = "Windows monitoring";
    private string _chargeLimitAvailabilityText = "Detecting hardware support";
    private string _topUpAvailabilityText = "Top Up support unknown";
    private string _manufacturer = "Unknown";
    private string _model = "Unknown";
    private string _batteryPercentText = "--";
    private string _chargeWattsText = "--";
    private string _systemWattsText = "--";
    private string _systemAdapterWattsText = "0.0 W";
    private string _batteryHealthText = "--";
    private string _temperatureText = "--";
    private string _timeRemainingText = "--";
    private string _cycleCountText = "--";
    private string _chargeLimitMessage = "No charge limit has been applied.";
    private string _liveUpdateNote = "Collecting data from Windows power telemetry.";
    private bool _isInfoBannerVisible = true;
    private double _batteryPercentValue;

    public MainViewModel(
        Dispatcher dispatcher,
        AppSettings settings,
        SettingsService settingsService,
        BatteryMonitorService batteryMonitorService,
        ChargeLimitCoordinator chargeLimitCoordinator)
    {
        _dispatcher = dispatcher;
        _settings = settings;
        _settingsService = settingsService;
        _batteryMonitorService = batteryMonitorService;
        _chargeLimitCoordinator = chargeLimitCoordinator;

        _useDarkMode = settings.UseDarkMode;
        _backgroundMonitoringEnabled = settings.BackgroundMonitoringEnabled;
        _startMinimizedToTray = settings.StartMinimizedToTray;
        _refreshIntervalSeconds = settings.RefreshIntervalSeconds;
        _selectedChargeLimit = settings.SelectedChargeLimitPercent;
        _useCustomChargeLimit = settings.UseCustomChargeLimit;
        _customChargeLimit = settings.CustomChargeLimitPercent;

        ChargeLimitPresets = new ObservableCollection<int>([60, 70, 80, 90]);
        BatteryPercentHistory = _batteryPercentHistory.Points;
        ChargePowerHistory = _chargePowerHistory.Points;
        SystemPowerHistory = _systemPowerHistory.Points;

        _applyChargeLimitCommand = new RelayCommand(async value => await ApplyChargeLimitAsync(value), _ => ChargeLimitSupported);
        _enableTopUpCommand = new RelayCommand(async () => await EnableTopUpAsync(), () => TopUpSupported);
        _dismissInfoBannerCommand = new RelayCommand(() => IsInfoBannerVisible = false);
        _closeCommand = new RelayCommand(() =>
        {
            PrepareForExit();
            RequestClose?.Invoke(this, EventArgs.Empty);
        });
    }

    public event EventHandler? RequestClose;
    public event EventHandler<NotificationEventArgs>? NotificationRaised;

    public ObservableCollection<int> ChargeLimitPresets { get; }
    public ObservableCollection<HistoricalPoint> BatteryPercentHistory { get; }
    public ObservableCollection<HistoricalPoint> ChargePowerHistory { get; }
    public ObservableCollection<HistoricalPoint> SystemPowerHistory { get; }

    public ICommand ApplyChargeLimitCommand => _applyChargeLimitCommand;
    public ICommand EnableTopUpCommand => _enableTopUpCommand;
    public ICommand DismissInfoBannerCommand => _dismissInfoBannerCommand;
    public ICommand CloseCommand => _closeCommand;

    public bool ShouldKeepRunningInBackground => BackgroundMonitoringEnabled;
    public bool AllowWindowClose => _allowWindowClose;
    public bool ChargeLimitSupported => _chargeLimitCoordinator.Compatibility.ChargeLimitSupported;
    public bool TopUpSupported => _chargeLimitCoordinator.Compatibility.TopUpSupported;
    public string AppTitle => "Battery Manager";

    public bool IsInfoBannerVisible
    {
        get => _isInfoBannerVisible;
        set => SetProperty(ref _isInfoBannerVisible, value);
    }

    public bool UseDarkMode
    {
        get => _useDarkMode;
        set
        {
            if (SetProperty(ref _useDarkMode, value))
            {
                _settings.UseDarkMode = value;
                ThemeManager.Apply(App.Current.Resources, value);
                SaveSettings();
            }
        }
    }

    public bool BackgroundMonitoringEnabled
    {
        get => _backgroundMonitoringEnabled;
        set
        {
            if (SetProperty(ref _backgroundMonitoringEnabled, value))
            {
                _settings.BackgroundMonitoringEnabled = value;
                SaveSettings();
            }
        }
    }

    public bool StartMinimizedToTray
    {
        get => _startMinimizedToTray;
        set
        {
            if (SetProperty(ref _startMinimizedToTray, value))
            {
                _settings.StartMinimizedToTray = value;
                SaveSettings();
            }
        }
    }

    public int RefreshIntervalSeconds
    {
        get => _refreshIntervalSeconds;
        set
        {
            if (SetProperty(ref _refreshIntervalSeconds, value))
            {
                _settings.RefreshIntervalSeconds = value;
                SaveSettings();
                RestartMonitoring();
            }
        }
    }

    public int SelectedChargeLimit
    {
        get => _selectedChargeLimit;
        set
        {
            if (SetProperty(ref _selectedChargeLimit, value))
            {
                _settings.SelectedChargeLimitPercent = value;
                SaveSettings();
            }
        }
    }

    public bool UseCustomChargeLimit
    {
        get => _useCustomChargeLimit;
        set
        {
            if (SetProperty(ref _useCustomChargeLimit, value))
            {
                _settings.UseCustomChargeLimit = value;
                SaveSettings();
            }
        }
    }

    public int CustomChargeLimit
    {
        get => _customChargeLimit;
        set
        {
            if (SetProperty(ref _customChargeLimit, value))
            {
                _settings.CustomChargeLimitPercent = value;
                SaveSettings();
            }
        }
    }

    public bool TopUpActive
    {
        get => _topUpActive;
        private set
        {
            if (SetProperty(ref _topUpActive, value))
            {
                RaisePropertyChanged(nameof(TopUpStateText));
            }
        }
    }

    public string TopUpStateText => TopUpActive
        ? "Top Up active. Threshold is temporarily bypassed."
        : "Top Up inactive. Hardware threshold rules remain in effect.";

    public string StatusHeadline
    {
        get => _statusHeadline;
        private set => SetProperty(ref _statusHeadline, value);
    }

    public string StatusDetail
    {
        get => _statusDetail;
        private set => SetProperty(ref _statusDetail, value);
    }

    public string CompatibilityHeadline
    {
        get => _compatibilityHeadline;
        private set => SetProperty(ref _compatibilityHeadline, value);
    }

    public string ProviderName
    {
        get => _providerName;
        private set => SetProperty(ref _providerName, value);
    }

    public string ChargeLimitAvailabilityText
    {
        get => _chargeLimitAvailabilityText;
        private set => SetProperty(ref _chargeLimitAvailabilityText, value);
    }

    public string TopUpAvailabilityText
    {
        get => _topUpAvailabilityText;
        private set => SetProperty(ref _topUpAvailabilityText, value);
    }

    public string CompatibilityDetail
    {
        get => _compatibilityDetail;
        private set => SetProperty(ref _compatibilityDetail, value);
    }

    public string Manufacturer
    {
        get => _manufacturer;
        private set => SetProperty(ref _manufacturer, value);
    }

    public string Model
    {
        get => _model;
        private set => SetProperty(ref _model, value);
    }

    public string BatteryPercentText
    {
        get => _batteryPercentText;
        private set => SetProperty(ref _batteryPercentText, value);
    }

    public double BatteryPercentValue
    {
        get => _batteryPercentValue;
        private set => SetProperty(ref _batteryPercentValue, value);
    }

    public string ChargeWattsText
    {
        get => _chargeWattsText;
        private set => SetProperty(ref _chargeWattsText, value);
    }

    public string SystemWattsText
    {
        get => _systemWattsText;
        private set => SetProperty(ref _systemWattsText, value);
    }

    public string SystemAdapterWattsText
    {
        get => _systemAdapterWattsText;
        private set => SetProperty(ref _systemAdapterWattsText, value);
    }

    public string BatteryHealthText
    {
        get => _batteryHealthText;
        private set => SetProperty(ref _batteryHealthText, value);
    }

    public string TemperatureText
    {
        get => _temperatureText;
        private set => SetProperty(ref _temperatureText, value);
    }

    public string TimeRemainingText
    {
        get => _timeRemainingText;
        private set => SetProperty(ref _timeRemainingText, value);
    }

    public string CycleCountText
    {
        get => _cycleCountText;
        private set => SetProperty(ref _cycleCountText, value);
    }

    public string ChargeLimitMessage
    {
        get => _chargeLimitMessage;
        private set => SetProperty(ref _chargeLimitMessage, value);
    }

    public string LiveUpdateNote
    {
        get => _liveUpdateNote;
        private set => SetProperty(ref _liveUpdateNote, value);
    }

    public void Initialize()
    {
        ThemeManager.Apply(App.Current.Resources, UseDarkMode);

        var compatibility = _chargeLimitCoordinator.Compatibility;
        Manufacturer = compatibility.Manufacturer;
        Model = compatibility.Model;
        ProviderName = compatibility.ProviderName;
        CompatibilityHeadline = compatibility.SupportHeadline;
        CompatibilityDetail = compatibility.SupportDetail;
        ChargeLimitAvailabilityText = compatibility.ChargeLimitSupported
            ? "Real hardware charge limiting supported"
            : "Charge limiting not available on this device";
        TopUpAvailabilityText = compatibility.TopUpSupported
            ? "Top Up supported by the active provider"
            : "Top Up requires hardware-level provider support";

        _batteryMonitorService.SnapshotUpdated += OnSnapshotUpdated;
        RestartMonitoring();
    }

    public async Task ApplyChargeLimitAsync(object? presetValue = null)
    {
        if (presetValue is not null && int.TryParse(presetValue.ToString(), out var preset))
        {
            SelectedChargeLimit = preset;
            UseCustomChargeLimit = false;
        }

        var target = UseCustomChargeLimit ? CustomChargeLimit : SelectedChargeLimit;
        var result = await _chargeLimitCoordinator.ApplyChargeLimitAsync(Math.Clamp(target, 50, 100), CancellationToken.None);
        ChargeLimitMessage = result.Message;
    }

    public async Task EnableTopUpAsync()
    {
        var result = await _chargeLimitCoordinator.EnableTopUpAsync(CancellationToken.None);
        TopUpActive = result.Success;
        ChargeLimitMessage = result.Message;

        if (TopUpActive)
        {
            RaiseNotification("Top Up active", "Charging will continue to 100% before the charge limit is restored.", ToolTipIcon.Info);
        }
    }

    public void PrepareForExit()
    {
        _allowWindowClose = true;
    }

    private void RestartMonitoring()
    {
        _batteryMonitorService.Start(RefreshIntervalSeconds);
    }

    private void OnSnapshotUpdated(object? sender, BatterySnapshotEventArgs e)
    {
        _dispatcher.Invoke(() => ApplySnapshot(e.Snapshot));
    }

    private void ApplySnapshot(BatterySnapshot snapshot)
    {
        _latestSnapshot = snapshot;

        StatusHeadline = snapshot.StatusText;
        StatusDetail = snapshot.DetailText;
        BatteryPercentValue = snapshot.BatteryPercent ?? 0;
        BatteryPercentText = snapshot.BatteryPercent is null ? "--" : $"{snapshot.BatteryPercent:0}%";
        ChargeWattsText = FormatLiveWatts(snapshot.ChargingPowerWatts);
        SystemWattsText = FormatLiveWatts(snapshot.SystemPowerWatts, snapshot.SystemPowerEstimated);
        SystemAdapterWattsText = FormatLiveWatts(snapshot.SystemPowerFromAdapterWatts, snapshot.AdapterPowerEstimated);
        BatteryHealthText = snapshot.BatteryHealthPercent is null ? "--" : $"{snapshot.BatteryHealthPercent:0}% health";
        TemperatureText = snapshot.BatteryTemperatureCelsius is null ? "--" : $"{snapshot.BatteryTemperatureCelsius:0.0} C";
        TimeRemainingText = snapshot.EstimatedTimeRemaining is null
            ? (snapshot.IsOnAcPower ? "AC connected" : "Live")
            : $"{snapshot.EstimatedTimeRemaining:hh\\:mm} remaining";
        CycleCountText = snapshot.CycleCount?.ToString() ?? "--";
        LiveUpdateNote = snapshot.AnyWattsEstimated ? "Some watt values are estimated." : $"Telemetry source: {snapshot.RateSource}";

        _batteryPercentHistory.Add(snapshot.Timestamp, snapshot.BatteryPercent);
        _chargePowerHistory.Add(snapshot.Timestamp, snapshot.ChargingPowerWatts);
        _systemPowerHistory.Add(snapshot.Timestamp, snapshot.SystemPowerWatts);

        var activeTargetLimit = UseCustomChargeLimit ? CustomChargeLimit : SelectedChargeLimit;

        if (ChargeLimitSupported &&
            snapshot.BatteryPercent is not null &&
            snapshot.BatteryPercent >= activeTargetLimit &&
            snapshot.IsCharging)
        {
            if (!_chargeLimitReachedNotified)
            {
                _chargeLimitReachedNotified = true;
                RaiseNotification("Charge limit reached", $"Battery reached {snapshot.BatteryPercent:0}% and the hardware limit should now take over.", ToolTipIcon.Info);
            }
        }
        else if (!snapshot.IsCharging || snapshot.BatteryPercent is null || snapshot.BatteryPercent < activeTargetLimit - 1)
        {
            _chargeLimitReachedNotified = false;
        }

        if (TopUpActive && snapshot.BatteryPercent >= 99.5)
        {
            TopUpActive = false;
            _ = _chargeLimitCoordinator.RestorePreviousChargeLimitAsync(CancellationToken.None);
            RaiseNotification("Top Up completed", "Battery reached full charge and the previous limit has been restored.", ToolTipIcon.Info);
        }

        if (snapshot.BatteryTemperatureCelsius >= 45)
        {
            if (!_highTemperatureNotified)
            {
                _highTemperatureNotified = true;
                RaiseNotification("Battery temperature high", $"Battery temperature reached {snapshot.BatteryTemperatureCelsius:0.0} C.", ToolTipIcon.Warning);
            }
        }
        else if (snapshot.BatteryTemperatureCelsius is null || snapshot.BatteryTemperatureCelsius <= 42)
        {
            _highTemperatureNotified = false;
        }
    }

    private static string FormatLiveWatts(double? watts, bool estimated = false)
    {
        watts ??= 0;

        return estimated ? $"{watts:0.0} W est." : $"{watts:0.0} W";
    }

    private void RaiseNotification(string title, string message, ToolTipIcon icon)
    {
        NotificationRaised?.Invoke(this, new NotificationEventArgs(title, message, icon));
    }

    private void SaveSettings()
    {
        _settingsService.Save(_settings);
    }

    public void Dispose()
    {
        _batteryMonitorService.SnapshotUpdated -= OnSnapshotUpdated;
        _batteryMonitorService.Dispose();
    }
}

public sealed class NotificationEventArgs : EventArgs
{
    public NotificationEventArgs(string title, string message, ToolTipIcon icon)
    {
        Title = title;
        Message = message;
        Icon = icon;
    }

    public string Title { get; }
    public string Message { get; }
    public ToolTipIcon Icon { get; }
}
