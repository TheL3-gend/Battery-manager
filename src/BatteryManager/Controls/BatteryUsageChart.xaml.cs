using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using BatteryManager.Models;
using Brush = System.Windows.Media.Brush;
using Canvas = System.Windows.Controls.Canvas;
using Rectangle = System.Windows.Shapes.Rectangle;
using UserControl = System.Windows.Controls.UserControl;

namespace BatteryManager.Controls;

public partial class BatteryUsageChart : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(BatteryUsageChart), new PropertyMetadata(null, OnChartPropertyChanged));

    public BatteryUsageChart()
    {
        InitializeComponent();
        SizeChanged += (_, _) => RenderChart();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private static void OnChartPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (BatteryUsageChart)d;
        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= chart.OnCollectionChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newCollection)
        {
            newCollection.CollectionChanged += chart.OnCollectionChanged;
        }

        chart.RenderChart();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RenderChart();
    }

    private void RenderChart()
    {
        if (ItemsSource is null || ChartCanvas.ActualWidth <= 0 || ChartCanvas.ActualHeight <= 0)
        {
            return;
        }

        ChartCanvas.Children.Clear();

        var entries = ItemsSource.Cast<BatteryHistoryEntry>().ToList();
        if (entries.Count == 0)
        {
            return;
        }

        var barCount = entries.Count;
        var spacing = barCount > 32 ? 5d : 10d;
        var totalGap = Math.Max(0, (barCount - 1) * spacing);
        var barWidth = Math.Max(8, Math.Min(18, (ChartCanvas.ActualWidth - totalGap) / barCount));
        var usedWidth = (barWidth * barCount) + totalGap;
        var x = Math.Max(0, (ChartCanvas.ActualWidth - usedWidth) / 2);

        var barBrush = (Brush)FindResource("UsageBarBrush");
        var chargingBrush = (Brush)FindResource("UsageChargingBarBrush");
        var barBorderBrush = (Brush)FindResource("UsageBarBorderBrush");

        foreach (var entry in entries)
        {
            var height = Math.Max(6, (entry.BatteryPercent / 100d) * ChartCanvas.ActualHeight);
            var rectangle = new Rectangle
            {
                Width = barWidth,
                Height = height,
                RadiusX = barWidth / 2,
                RadiusY = barWidth / 2,
                Fill = entry.IsCharging ? chargingBrush : barBrush,
                Stroke = barBorderBrush,
                StrokeThickness = 1
            };

            Canvas.SetLeft(rectangle, x);
            Canvas.SetTop(rectangle, ChartCanvas.ActualHeight - height);
            ChartCanvas.Children.Add(rectangle);
            x += barWidth + spacing;
        }
    }
}
