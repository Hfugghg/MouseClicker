namespace MouseClicker;

internal static class SelectionActionLayout
{
    // Both rectangles use the caller's coordinate system (screen or canvas).
    internal static Rectangle Place(Rectangle selection, Rectangle viewport, Size toolbarSize, int gap)
    {
        int width = Math.Min(Math.Max(1, toolbarSize.Width), Math.Max(1, viewport.Width));
        int height = Math.Min(Math.Max(1, toolbarSize.Height), Math.Max(1, viewport.Height));
        int x = Math.Clamp(selection.Right - width, viewport.Left, Math.Max(viewport.Left, viewport.Right - width));
        int y = selection.Bottom + gap;
        if (y + height > viewport.Bottom) y = selection.Top - height - gap;
        y = Math.Clamp(y, viewport.Top, Math.Max(viewport.Top, viewport.Bottom - height));
        return new Rectangle(x, y, width, height);
    }
}
