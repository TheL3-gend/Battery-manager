using System.Windows;
using System.ComponentModel;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Input;
using BatteryManager.Services;
using BatteryManager.ViewModels;

namespace BatteryManager.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SourceInitialized += OnSourceInitialized;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            WindowBackdropService.Apply(this, viewModel.UseDarkMode);
        }
        else
        {
            WindowBackdropService.Apply(this, true);
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = e.NewValue as MainViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            WindowBackdropService.Apply(this, _viewModel.UseDarkMode);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.UseDarkMode) && _viewModel is not null)
        {
            WindowBackdropService.Apply(this, _viewModel.UseDarkMode);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        ShellRoot.BeginAnimation(OpacityProperty, fade);

        var leftSlide = new DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(320))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var rightSlide = new DoubleAnimation(14, 0, TimeSpan.FromMilliseconds(360))
        {
            BeginTime = TimeSpan.FromMilliseconds(60),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        if (LeftPanel.RenderTransform is TranslateTransform leftTransform)
        {
            leftTransform.BeginAnimation(TranslateTransform.YProperty, leftSlide);
        }

        if (RightPanel.RenderTransform is TranslateTransform rightTransform)
        {
            rightTransform.BeginAnimation(TranslateTransform.YProperty, rightSlide);
        }
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }

        DragMove();
    }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
