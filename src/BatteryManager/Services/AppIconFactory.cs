using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace BatteryManager.Services;

public static class AppIconFactory
{
    public static Icon CreateTrayIcon()
    {
        using var bitmap = new Bitmap(64, 64);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var bodyBrush = new SolidBrush(Color.FromArgb(52, 120, 246));
        using var capBrush = new SolidBrush(Color.FromArgb(29, 36, 48));
        using var chargeBrush = new SolidBrush(Color.FromArgb(255, 255, 255));
        using var detailPen = new Pen(Color.FromArgb(255, 255, 255), 3f);

        var body = new RectangleF(10, 14, 40, 36);
        graphics.FillRoundedRectangle(bodyBrush, body, 10);
        graphics.FillRoundedRectangle(capBrush, new RectangleF(50, 25, 6, 14), 3);
        graphics.DrawLine(detailPen, 18, 32, 28, 32);
        graphics.DrawLine(detailPen, 32, 24, 24, 40);
        graphics.DrawLine(detailPen, 32, 24, 42, 24);
        graphics.FillEllipse(chargeBrush, 14, 18, 6, 6);

        var iconHandle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(iconHandle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
    }

    public static BitmapSource CreateWindowIconSource()
    {
        using var bitmap = new Bitmap(128, 128);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var backgroundBrush = new SolidBrush(Color.FromArgb(234, 241, 255));
        using var bodyBrush = new SolidBrush(Color.FromArgb(52, 120, 246));
        using var topBrush = new SolidBrush(Color.FromArgb(29, 36, 48));
        using var whiteBrush = new SolidBrush(Color.White);
        using var innerPen = new Pen(Color.FromArgb(29, 36, 48), 5f);

        graphics.FillEllipse(backgroundBrush, 4, 4, 120, 120);
        graphics.FillRoundedRectangle(bodyBrush, new RectangleF(24, 28, 72, 56), 18);
        graphics.FillRoundedRectangle(topBrush, new RectangleF(96, 46, 10, 20), 5);
        graphics.FillRectangle(whiteBrush, 34, 38, 24, 34);
        graphics.DrawLine(innerPen, 66, 40, 80, 40);
        graphics.DrawLine(innerPen, 68, 38, 58, 72);
        graphics.DrawLine(innerPen, 68, 38, 82, 62);

        var hBitmap = bitmap.GetHbitmap();
        try
        {
            return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF rectangle, float radius)
    {
        using var path = CreateRoundedRectanglePath(rectangle, radius);
        graphics.FillPath(brush, path);
    }

    private static GraphicsPath CreateRoundedRectanglePath(RectangleF rectangle, float radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Y, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.X, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
