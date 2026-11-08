using System.Text.Json;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed class PilotPortraitCatalog : IDisposable
{
    private readonly Dictionary<string, (SKBitmap? Bitmap, bool Female)> _pilots = new(StringComparer.Ordinal);
    // The four immutable decoded portraits share the application lifetime, as do the bundled fonts.
    internal static PilotPortraitCatalog Default { get; } = Load(AppContext.BaseDirectory);
    public static PilotPortraitCatalog Load(string baseDir)
    {
        var catalog = new PilotPortraitCatalog();
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
                SKBitmap? bitmap = File.Exists(path) ? SKBitmap.Decode(path) : null;
                if (!catalog._pilots.TryAdd(pilot.Name, (bitmap, pilot.Value.GetProperty("female").GetBoolean())))
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
    public bool TryGet(string pilotId, out SKBitmap bitmap)
    { bitmap = _pilots.TryGetValue(pilotId, out var entry) ? entry.Bitmap! : null!; return bitmap is not null; }
    public bool IsFemale(string pilotId) => _pilots.TryGetValue(pilotId, out var entry) && entry.Female;
    public void Dispose() { foreach (var item in _pilots.Values) item.Bitmap?.Dispose(); _pilots.Clear(); }
}
