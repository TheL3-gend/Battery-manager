using System.Collections.ObjectModel;
using BatteryManager.Models;

namespace BatteryManager.Services;

public sealed class HistoryBuffer
{
    private readonly int _capacity;
    private readonly ObservableCollection<HistoricalPoint> _points = [];

    public HistoryBuffer(int capacity)
    {
        _capacity = capacity;
    }

    public ObservableCollection<HistoricalPoint> Points => _points;

    public void Add(DateTime timestamp, double? value)
    {
        _points.Add(new HistoricalPoint(timestamp, value));
        while (_points.Count > _capacity)
        {
            _points.RemoveAt(0);
        }
    }
}
