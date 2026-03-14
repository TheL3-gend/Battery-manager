using System;
using BatteryManager.Services;
using BatteryManager.ViewModels;
using BatteryManager.Views;
using Application = System.Windows.Application;
using ExitEventArgs = System.Windows.ExitEventArgs;
using StartupEventArgs = System.Windows.StartupEventArgs;

namespace BatteryManager;

public partial class App : Application
{
    private MainViewModel? _mainViewModel;
    private MainWindow? _mainWindow;
    private TrayIconService? _trayIconService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = new SettingsService();
        var settings = settingsService.Load();
        var deviceInfoService = new DeviceInfoService();
        var oemDetectionService = new OemDetectionService(deviceInfoService);
        var chargeLimitCoordinator = new ChargeLimitCoordinator(oemDetectionService);
        var batteryMonitorService = new BatteryMonitorService(chargeLimitCoordinator);
        var batteryHistoryService = new BatteryHistoryService();

        _mainViewModel = new MainViewModel(
            Dispatcher,
            settings,
            settingsService,
            batteryMonitorService,
            batteryHistoryService,
            chargeLimitCoordinator);

        _mainWindow = new MainWindow
        {
            DataContext = _mainViewModel,
            Icon = AppIconFactory.CreateWindowIconSource()
        };

        _trayIconService = new TrayIconService(_mainViewModel, _mainWindow);
        _mainViewModel.RequestClose += OnRequestClose;
        _mainViewModel.Initialize();

        if (settings.StartMinimizedToTray)
        {
            _mainWindow.Hide();
        }
        else
        {
            _mainWindow.Show();
        }
    }

    private void OnRequestClose(object? sender, EventArgs e)
    {
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_mainViewModel is not null)
        {
            _mainViewModel.RequestClose -= OnRequestClose;
            _mainViewModel.Dispose();
        }

        _trayIconService?.Dispose();
        base.OnExit(e);
    }
}
