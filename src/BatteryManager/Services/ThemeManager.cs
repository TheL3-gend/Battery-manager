using System.Windows;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace BatteryManager.Services;

public static class ThemeManager
{
    public static void Apply(ResourceDictionary resources, bool darkMode)
    {
        resources["WindowBackgroundBrush"] = CreateBrush(darkMode ? "#CC11161D" : "#D9EEF1F5");
        resources["CardBackgroundBrush"] = CreateBrush(darkMode ? "#B31A2029" : "#CCF7F9FC");
        resources["CardSecondaryBrush"] = CreateBrush(darkMode ? "#B3232B37" : "#E6FFFFFF");
        resources["HeroPanelBrush"] = CreateBrush(darkMode ? "#AA20293A" : "#BFEAF1FF");
        resources["PrimaryTextBrush"] = CreateBrush(darkMode ? "#F5F7FB" : "#1D2430");
        resources["SecondaryTextBrush"] = CreateBrush(darkMode ? "#A8B3C2" : "#667085");
        resources["AccentBrush"] = CreateBrush(darkMode ? "#4C86FF" : "#3478F6");
        resources["AccentSecondaryBrush"] = CreateBrush(darkMode ? "#7CB0FF" : "#6AA8FF");
        resources["WarningBrush"] = CreateBrush(darkMode ? "#F3B84F" : "#D99224");
        resources["DangerBrush"] = CreateBrush(darkMode ? "#E85C5C" : "#D84C4C");
        resources["BorderBrush"] = CreateBrush(darkMode ? "#8A313A49" : "#AAD6DCE5");
        resources["UsageHeaderBrush"] = CreateBrush(darkMode ? "#333038" : "#E4E9F0");
        resources["UsagePanelBrush"] = CreateBrush(darkMode ? "#231F24" : "#F6F8FC");
        resources["UsageGridBrush"] = CreateBrush(darkMode ? "#6E737B" : "#B8C3D4");
        resources["UsageAxisBrush"] = CreateBrush(darkMode ? "#C3CBD6" : "#67758A");
        resources["UsageBarBrush"] = CreateBrush(darkMode ? "#27AFC3" : "#1598B3");
        resources["UsageChargingBarBrush"] = CreateBrush(darkMode ? "#32C6D6" : "#1EB2C7");
        resources["UsageBarBorderBrush"] = CreateBrush(darkMode ? "#1D7D8F" : "#1A8EA4");
    }

    private static SolidColorBrush CreateBrush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
