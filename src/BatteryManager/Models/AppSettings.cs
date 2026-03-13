namespace BatteryManager.Models;

public sealed class AppSettings
{
    public bool UseDarkMode { get; set; } = true;
    public bool BackgroundMonitoringEnabled { get; set; } = true;
    public bool StartMinimizedToTray { get; set; }
    public int RefreshIntervalSeconds { get; set; } = 2;
    public int SelectedChargeLimitPercent { get; set; } = 80;
    public bool UseCustomChargeLimit { get; set; }
    public int CustomChargeLimitPercent { get; set; } = 85;
}
