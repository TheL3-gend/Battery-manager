using System.IO;
using System.Text.Json;
using BatteryManager.Models;

namespace BatteryManager.Services;

public sealed class BatteryHistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly string _historyPath;
    private readonly List<BatteryHistoryEntry> _entries;

    public BatteryHistoryService()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BatteryManager");

        Directory.CreateDirectory(root);
        _historyPath = Path.Combine(root, "battery-history.json");
        _entries = LoadEntries();
        PruneExpiredEntries();
    }

    public void Append(DateTime timestamp, double? batteryPercent, bool isCharging)
    {
        if (batteryPercent is null)
        {
            return;
        }

        var normalizedPercent = Math.Clamp(batteryPercent.Value, 0, 100);
        var lastEntry = _entries.LastOrDefault();

        if (lastEntry is not null)
        {
            var elapsed = timestamp - lastEntry.Timestamp;
            var percentDelta = Math.Abs(lastEntry.BatteryPercent - normalizedPercent);
            if (elapsed < TimeSpan.FromMinutes(5) &&
                percentDelta < 1 &&
                lastEntry.IsCharging == isCharging)
            {
                return;
            }
        }

        _entries.Add(new BatteryHistoryEntry(timestamp, normalizedPercent, isCharging));
        PruneExpiredEntries();
        SaveEntries();
    }

    public IReadOnlyList<BatteryHistoryEntry> GetEntries(BatteryHistoryPeriod period)
    {
        var range = period == BatteryHistoryPeriod.Day
            ? TimeSpan.FromHours(24)
            : TimeSpan.FromDays(7);

        var cutoff = DateTime.Now - range;
        var recentEntries = _entries
            .Where(entry => entry.Timestamp >= cutoff)
            .OrderBy(entry => entry.Timestamp)
            .ToList();

        if (recentEntries.Count == 0)
        {
            return [];
        }

        var maxPoints = period == BatteryHistoryPeriod.Day ? 96 : 168;
        if (recentEntries.Count <= maxPoints)
        {
            return recentEntries;
        }

        var stride = (double)(recentEntries.Count - 1) / (maxPoints - 1);
        var sampledPoints = new List<BatteryHistoryEntry>(maxPoints);
        for (var index = 0; index < maxPoints; index++)
        {
            var sourceIndex = Math.Min(recentEntries.Count - 1, (int)Math.Round(index * stride));
            var entry = recentEntries[sourceIndex];
            sampledPoints.Add(entry);
        }

        return sampledPoints;
    }

    private List<BatteryHistoryEntry> LoadEntries()
    {
        try
        {
            if (!File.Exists(_historyPath))
            {
                return [];
            }

            var json = File.ReadAllText(_historyPath);
            return JsonSerializer.Deserialize<List<BatteryHistoryEntry>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void SaveEntries()
    {
        try
        {
            var json = JsonSerializer.Serialize(_entries, JsonOptions);
            File.WriteAllText(_historyPath, json);
        }
        catch
        {
        }
    }

    private void PruneExpiredEntries()
    {
        var cutoff = DateTime.Now - TimeSpan.FromDays(8);
        _entries.RemoveAll(entry => entry.Timestamp < cutoff);
    }
}
