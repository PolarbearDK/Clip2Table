using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Microsoft.Win32;

namespace Clip2Table;

/// <summary>
/// Draws the tray glyph in the style of the Windows system tray icons: a monochrome outline on a
/// transparent background, white on a dark taskbar and black on a light one. The glyph is a table
/// grid, a solid header row over three rows of three cells; with auto-convert on, a green checkmark
/// sits in the lower-right corner.
/// </summary>
internal static class TrayIcons
{
    /// <summary>The glyph is designed on a 16-unit square and scaled to the actual icon size.</summary>
    private const float DesignSize = 16f;
    private const float CheckStroke = 2.5f;
    private const float CheckGap = 1.25f;

    private static readonly Color DarkTaskbarInk = Color.White;
    private static readonly Color LightTaskbarInk = Color.Black;

    // Windows' own success greens: a bright one reads on a dark taskbar, a deep one on a light taskbar.
    private static readonly Color DarkTaskbarCheck = Color.FromArgb(0x6C, 0xCB, 0x5F);
    private static readonly Color LightTaskbarCheck = Color.FromArgb(0x0F, 0x7B, 0x0F);

    /// <summary>
    /// Draws the tray icon at the system small-icon size, inked for the current taskbar theme.
    /// The returned icon wraps an HICON it does not own:
    /// the caller must dispose the icon and then call <c>NativeMethods.DestroyIcon</c> on its handle.
    /// </summary>
    public static Icon Create(bool autoOn)
    {
        var ink = TaskbarIsLight() ? LightTaskbarInk : DarkTaskbarInk;
        using var bitmap = Render(SystemInformation.SmallIconSize.Width, autoOn, ink);
        return Icon.FromHandle(bitmap.GetHicon());
    }

    /// <summary>Renders the glyph onto a transparent square bitmap of the given pixel size.</summary>
    internal static Bitmap Render(int size, bool autoOn, Color ink)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            var scale = size / DesignSize;
            DrawGrid(g, scale, ink);
            if (autoOn)
            {
                DrawCheck(g, scale, ink.GetBrightness() > 0.5f ? DarkTaskbarCheck : LightTaskbarCheck);
            }
        }

        return bitmap;
    }

    private static void DrawCheck(Graphics g, float scale, Color color)
    {
        // A checkmark in the lower-right corner, in design units.
        PointF[] points = [new(7.5f, 11.5f), new(10.5f, 14.5f), new(15.5f, 8.5f)];
        for (var i = 0; i < points.Length; i++)
        {
            points[i] = new PointF(points[i].X * scale, points[i].Y * scale);
        }

        g.SmoothingMode = SmoothingMode.AntiAlias;

        // First cut a transparent gap around the check so the grid lines don't run into it.
        var stroke = CheckStroke * scale;
        g.CompositingMode = CompositingMode.SourceCopy;
        using (var gap = new Pen(Color.Transparent, stroke + (2 * CheckGap * scale)) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawLines(gap, points);
        }

        g.CompositingMode = CompositingMode.SourceOver;
        using var pen = new Pen(color, stroke) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLines(pen, points);
    }

    private static void DrawGrid(Graphics g, float scale, Color ink)
    {
        // Laid out in whole pixels with filled rectangles, so every line is crisp and the rows
        // and columns come out even at any icon size.
        // The grid fills the whole icon square: it is the only detail, so it can use every pixel.
        const int margin = 0;
        var line = Math.Max(1, (int)Math.Round(scale * 0.75f, MidpointRounding.AwayFromZero));
        var span = (int)Math.Round(DesignSize * scale) - (2 * margin);

        // Height: a solid header, then three rows, each closed by a line below it.
        // Spare pixels go to the header, which keeps it at least as tall as a row.
        var row = (span - (3 * line)) / 4;
        var header = span - (3 * line) - (3 * row);

        // Width: three columns between four lines; any spare pixel goes to the middle column.
        var column = (span - (4 * line)) / 3;
        var middle = span - (4 * line) - (2 * column);

        using var brush = new SolidBrush(ink);

        // A solid header row is what makes the grid read as a table.
        g.FillRectangle(brush, margin, margin, span, header);

        var y = margin + header;
        for (var i = 0; i < 3; i++)
        {
            y += row;
            g.FillRectangle(brush, margin, y, span, line);
            y += line;
        }

        var x = margin;
        foreach (var width in new[] { column, middle, column })
        {
            g.FillRectangle(brush, x, margin, line, span);
            x += line + width;
        }

        g.FillRectangle(brush, x, margin, line, span);
    }

    /// <summary>Whether the taskbar uses the light theme; dark, the Windows default, when unknown.</summary>
    private static bool TaskbarIsLight()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("SystemUsesLightTheme") is int value && value != 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
