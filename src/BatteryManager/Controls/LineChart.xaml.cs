using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using BatteryManager.Models;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using DependencyObject = System.Windows.DependencyObject;
using DependencyProperty = System.Windows.DependencyProperty;
using DependencyPropertyChangedEventArgs = System.Windows.DependencyPropertyChangedEventArgs;
using Point = System.Windows.Point;
using StreamGeometry = System.Windows.Media.StreamGeometry;
using UserControl = System.Windows.Controls.UserControl;

namespace BatteryManager.Controls;

public partial class LineChart : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(LineChart), new PropertyMetadata(null, OnChartPropertyChanged));

    public static readonly DependencyProperty StrokeProperty =
        DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(LineChart), new PropertyMetadata(Brushes.Cyan));

    public static readonly DependencyProperty FillProperty =
        DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(LineChart), new PropertyMetadata(Brushes.Transparent));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(LineChart), new PropertyMetadata(100d, OnChartPropertyChanged));

    public LineChart()
    {
        InitializeComponent();
        SizeChanged += (_, _) => RenderChart();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    private static void OnChartPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (LineChart)d;
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
        if (ActualWidth <= 0 || ActualHeight <= 0 || ItemsSource is null)
        {
            return;
        }

        var points = ItemsSource.Cast<HistoricalPoint>().Where(point => point.Value.HasValue).ToList();
        if (points.Count < 2)
        {
            LinePath.Data = null;
            FillPath.Data = null;
            return;
        }

        var max = Math.Max(1, Maximum);
        var geometry = new StreamGeometry();
        var fillGeometry = new StreamGeometry();

        using var context = geometry.Open();
        using var fillContext = fillGeometry.Open();

        for (var index = 0; index < points.Count; index++)
        {
            var point = points[index];
            var x = index * (ActualWidth / (points.Count - 1));
            var y = ActualHeight - (Math.Clamp(point.Value!.Value, 0, max) / max * ActualHeight);
            var chartPoint = new Point(x, y);

            if (index == 0)
            {
                context.BeginFigure(chartPoint, false, false);
                fillContext.BeginFigure(new Point(x, ActualHeight), true, true);
                fillContext.LineTo(chartPoint, true, false);
            }
            else
            {
                context.LineTo(chartPoint, true, false);
                fillContext.LineTo(chartPoint, true, false);
            }

            if (index == points.Count - 1)
            {
                fillContext.LineTo(new Point(x, ActualHeight), true, false);
            }
        }

        geometry.Freeze();
        fillGeometry.Freeze();
        LinePath.Data = geometry;
        FillPath.Data = fillGeometry;
    }
}
