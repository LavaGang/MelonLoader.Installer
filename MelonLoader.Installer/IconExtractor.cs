#if WINDOWS
using System.Drawing;

namespace MelonLoader.Installer;

#pragma warning disable CA1416

internal class IconExtractor
{
    public static Avalonia.Media.Imaging.Bitmap? GetExeIcon(string filePath)
    {
        try
        {
            using var icon = Icon.ExtractAssociatedIcon(filePath);
            if (icon == null)
                return null;

            using var ms = new MemoryStream();
            icon.ToBitmap().Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);

            return new Avalonia.Media.Imaging.Bitmap(ms);
        }
        catch
        {
            return null;
        }
    }
}
#endif