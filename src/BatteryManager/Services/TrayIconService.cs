using System.Drawing;
using System.Windows;
using BatteryManager.ViewModels;
using Application = System.Windows.Application;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using ToolStripMenuItem = System.Windows.Forms.ToolStripMenuItem;
using ContextMenuStrip = System.Windows.Forms.ContextMenuStrip;
using SystemIcons = System.Drawing.SystemIcons;

namespace BatteryManager.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly MainViewModel _viewModel;
    private readonly Window _window;
    private readonly Icon _trayIcon;

    public TrayIconService(MainViewModel viewModel, Window window)
    {
        _viewModel = viewModel;
        _window = window;

        _trayIcon = AppIconFactory.CreateTrayIcon();
        _notifyIcon = new NotifyIcon
        {
            Text = "Battery Manager",
            Icon = _trayIcon,
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        _notifyIcon.DoubleClick += (_, _) => ShowWindow();
        _viewModel.NotificationRaised += OnNotificationRaised;
        _window.StateChanged += OnWindowStateChanged;
        _window.Closing += OnWindowClosing;
    }

    private void OnNotificationRaised(object? sender, NotificationEventArgs e)
    {
        _notifyIcon.BalloonTipTitle = e.Title;
        _notifyIcon.BalloonTipText = e.Message;
        _notifyIcon.BalloonTipIcon = e.Icon;
        _notifyIcon.ShowBalloonTip(3500);
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (_window.WindowState == WindowState.Minimized)
        {
            _window.Hide();
        }
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_viewModel.ShouldKeepRunningInBackground && !_viewModel.AllowWindowClose)
        {
            e.Cancel = true;
            _window.Hide();
        }
    }

    private void ShowWindow()
    {
        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    public void Dispose()
    {
        _viewModel.NotificationRaised -= OnNotificationRaised;
        _window.StateChanged -= OnWindowStateChanged;
        _window.Closing -= OnWindowClosing;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _trayIcon.Dispose();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("Open", null, (_, _) => ShowWindow()));
        menu.Items.Add(new ToolStripMenuItem("Top Up", null, async (_, _) => await _viewModel.EnableTopUpAsync()));
        menu.Items.Add("-");
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) =>
        {
            _viewModel.PrepareForExit();
            Application.Current.Shutdown();
        }));
        return menu;
    }
}
