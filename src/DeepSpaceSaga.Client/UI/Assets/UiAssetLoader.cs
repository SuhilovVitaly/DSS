using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Assets;

/// <summary>
/// Process-lifetime cache for UI bitmaps. Paths are always resolved relative to the
/// application output directory, so rendering is independent of the working directory.
/// Missing and corrupt assets are cached as <see langword="null"/>.
/// </summary>
public static class UiAssetLoader
{
    private static readonly object Sync = new();
    private static readonly Dictionary<string, SKBitmap?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static SKBitmap? LoadBitmap(string relativePath) =>
        LoadBitmapFrom(relativePath, AppContext.BaseDirectory);

    internal static SKBitmap? LoadBitmapFrom(string relativePath, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        string fullPath;
        try
        {
            var normalized = relativePath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            fullPath = Path.GetFullPath(Path.Combine(baseDirectory, normalized));
            var fullBase = Path.GetFullPath(baseDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase))
                return null;
        }
        catch
        {
            return null;
        }

        lock (Sync)
        {
            if (Cache.TryGetValue(fullPath, out var cached))
                return cached;

            SKBitmap? bitmap = null;
            try
            {
                if (File.Exists(fullPath))
                    bitmap = SKBitmap.Decode(fullPath);
            }
            catch
            {
                bitmap = null;
            }

            Cache[fullPath] = bitmap;
            return bitmap;
        }
    }
}
