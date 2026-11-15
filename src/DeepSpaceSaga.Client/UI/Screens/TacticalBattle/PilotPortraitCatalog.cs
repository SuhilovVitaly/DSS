using System.Text.Json;
using DeepSpaceSaga.Client.UI.Portraits;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed class PilotPortraitCatalog : IDisposable
{
    private readonly Dictionary<string, (SKBitmap? Bitmap, bool Female, bool Composed)> _pilots = new(StringComparer.Ordinal);
    // Portraits decoded from the shared crew record's asset id, keyed by that stable id.
    private readonly Dictionary<string, SKBitmap> _assets = new(StringComparer.Ordinal);
    private readonly string _baseDir;
    // The four immutable decoded portraits share the application lifetime, as do the bundled fonts.
    internal static PilotPortraitCatalog Default { get; } = Load(AppContext.BaseDirectory);

    private PilotPortraitCatalog(string baseDir) => _baseDir = Path.GetFullPath(baseDir);

    public static PilotPortraitCatalog Load(string baseDir)
    {
        var catalog = new PilotPortraitCatalog(baseDir);
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(baseDir, "Data", "UI", "tactical-battle-pilots.json")));
            if (document.RootElement.GetProperty("version").GetInt32() != 1) throw new FormatException("Unknown pilot portrait catalog version");
            foreach (var pilot in document.RootElement.GetProperty("pilots").EnumerateObject())
            {
                string relative = pilot.Value.GetProperty("portrait").GetString()!;
                var segments = relative.Split('/');
                if (segments.Any(s => s is "" or "." or ".." || s.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_'))))
                    throw new FormatException("Invalid portrait path");
                string path = Path.Combine(new[] { baseDir, "Images", "Persons" }.Concat(segments).ToArray()) + ".png";
                bool female = pilot.Value.GetProperty("female").GetBoolean();
                SKBitmap? bitmap = female ? ModularCrewPortraits.Pilot(baseDir, pilot.Name) : null;
                bool composed = bitmap is not null;
                bitmap ??= File.Exists(path) ? SKBitmap.Decode(path) : null;
                bitmap?.SetImmutable();
                if (!catalog._pilots.TryAdd(pilot.Name, (bitmap, female, composed)))
                { bitmap?.Dispose(); throw new FormatException("Duplicate pilot ID"); }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException or InvalidOperationException or FormatException or ArgumentException)
        {
            catalog.Dispose();
            InterfaceLog.Write($"TACTICAL PORTRAITS: {ex.Message}");
        }
        return catalog;
    }

    public bool TryGet(string pilotId, out SKBitmap bitmap) => TryGet(pilotId, null, out bitmap);

    /// <summary>
    /// Portrait for one pilot card: the shared crew record's portrait asset wins; the legacy pilot
    /// alias is only the fallback when the asset is absent or unreadable.
    /// </summary>
    public bool TryGet(string pilotId, string? portraitAssetId, out SKBitmap bitmap)
    {
        if (TryGetAsset(portraitAssetId, out bitmap)) return true;
        bitmap = _pilots.TryGetValue(pilotId, out var entry) ? entry.Bitmap! : null!;
        return bitmap is not null;
    }

    public bool IsFemale(string pilotId) => _pilots.TryGetValue(pilotId, out var entry) && entry.Female;

    internal SKRect SourceRect(string pilotId, string? portraitAssetId = null) =>
        portraitAssetId is { Length: > 0 } && _assets.ContainsKey(portraitAssetId)
            ? new SKRect(170, 70, 854, 754)
            : _pilots.TryGetValue(pilotId, out var entry) && entry.Composed
                ? new SKRect(33, 0, 267, 234)
                : new SKRect(170, 70, 854, 754);

    private bool TryGetAsset(string? portraitAssetId, out SKBitmap bitmap)
    {
        bitmap = null!;
        if (string.IsNullOrWhiteSpace(portraitAssetId)) return false;
        if (_assets.TryGetValue(portraitAssetId, out var cached)) { bitmap = cached; return true; }
        if (!TryResolveAssetPath(portraitAssetId, out string path)) return false;
        try
        {
            var decoded = SKBitmap.Decode(path);
            if (decoded is null) return false;
            decoded.SetImmutable();
            _assets[portraitAssetId] = decoded;
            bitmap = decoded;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            InterfaceLog.Write($"TACTICAL PORTRAIT ASSET {portraitAssetId}: {ex.Message}");
            return false;
        }
    }

    /// <summary>Only a relative .png inside the content directory may be loaded; traversal and rooted paths are rejected.</summary>
    private bool TryResolveAssetPath(string portraitAssetId, out string path)
    {
        path = "";
        if (Path.IsPathRooted(portraitAssetId)) return false;
        string[] segments = portraitAssetId.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(s => s is "." or ".." || s.Contains(':'))) return false;
        if (!segments[^1].EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            string combined = Path.GetFullPath(Path.Combine(_baseDir, Path.Combine(segments)));
            string relative = Path.GetRelativePath(_baseDir, combined);
            if (Path.IsPathRooted(relative) || relative == ".." ||
                relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                return false;
            path = combined;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        foreach (var item in _pilots.Values) item.Bitmap?.Dispose();
        _pilots.Clear();
        foreach (var item in _assets.Values) item.Dispose();
        _assets.Clear();
    }
}
