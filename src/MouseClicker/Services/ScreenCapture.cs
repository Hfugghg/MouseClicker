using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace MouseClicker;

internal sealed class ScreenCapture
{
    public static Bitmap Capture(Rectangle area)
    {
        ValidateArea(area, Screen.AllScreens.Select(screen => screen.Bounds));
        var bitmap = new Bitmap(area.Width, area.Height, PixelFormat.Format24bppRgb);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(area.Location, Point.Empty, area.Size, CopyPixelOperation.SourceCopy);
            return bitmap;
        }
        catch { bitmap.Dispose(); throw; }
    }

    internal static void ValidateArea(Rectangle area, IEnumerable<Rectangle> screens)
    {
        if (area.Width <= 0 || area.Height <= 0 || (long)area.Width * area.Height > 32L * 1024 * 1024 ||
            (long)area.X + area.Width > int.MaxValue || (long)area.Y + area.Height > int.MaxValue)
            throw new InvalidDataException(Localization.Get("ConditionErrorCaptureArea"));
        using var uncovered = new Region(area);
        foreach (var screen in screens) uncovered.Exclude(screen);
        using var matrix = new System.Drawing.Drawing2D.Matrix();
        if (uncovered.GetRegionScans(matrix).Length != 0)
            throw new InvalidDataException(Localization.Get("ConditionErrorOutsideDisplays"));
    }
}
