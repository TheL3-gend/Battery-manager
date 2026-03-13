using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace BatteryManager.Services;

public static class WindowBackdropService
{
    public static void Apply(Window window, bool darkMode)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var darkValue = darkMode ? 1 : 0;
            DwmSetWindowAttribute(handle, 20, ref darkValue, Marshal.SizeOf<int>());

            var backdropType = 2;
            DwmSetWindowAttribute(handle, 38, ref backdropType, Marshal.SizeOf<int>());

            var cornerPreference = 2;
            DwmSetWindowAttribute(handle, 33, ref cornerPreference, Marshal.SizeOf<int>());
        }
        catch
        {
            // Fall back to plain WPF styling when the current Windows build does not support these attributes.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
}
