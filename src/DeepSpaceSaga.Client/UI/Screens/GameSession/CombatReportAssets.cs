using System.Text.Json;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal sealed record CombatReportVisual(string Path, string Accent);

/// <summary>Prepare and dispose on the UI thread; Resolve only reads decoded resources.</summary>
internal sealed class CombatReportAssets(string? root = null, Func<string, SKBitmap?>? decode = null) : IDisposable
{
    internal const int PortraitLimit = 32;
    private readonly string _root = System.IO.Path.GetFullPath(root ?? AppContext.BaseDirectory);
    private readonly Func<string, SKBitmap?> _decode = decode ?? SKBitmap.Decode;
    private readonly Dictionary<string, CombatReportVisual> _catalog = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SKBitmap?> _illustrations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SKBitmap?> _portraits = new(StringComparer.Ordinal);
    private readonly LinkedList<string> _portraitLru = [];
    private bool _prepared;
    private bool _disposed;
    internal int IllustrationCount => _illustrations.Values.Count(b => b is not null);
    internal int PortraitCount => _portraits.Count;
    internal static readonly string[] Keys = ["enemy-in-range", "enemy-torpedo-launch", "player-torpedo-launch",
        "player-countermeasure-launch", "enemy-torpedo-intercepted", "enemy-ship-hit", "enemy-ship-destroyed", "player-ship-damaged"];

    internal void Prepare(IEnumerable<BattleReportSnapshot> visibleReports)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_prepared)
        {
            _prepared = true;
            try
            {
                var path = System.IO.Path.Combine(_root, "Data", "UI", "combat-report-visuals.json");
                var catalog = JsonSerializer.Deserialize<Dictionary<string, CombatReportVisual>>(File.ReadAllText(path),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                foreach (string key in Keys)
                {
                    if (catalog?.TryGetValue(key, out var visual) == true && visual is not null)
                    {
                        _catalog[key] = visual;
                        _illustrations[key] = Load(visual.Path);
                    }
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { }
        }
        foreach (var path in visibleReports.Select(r => r.Reporter.PortraitImage).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.Ordinal).Take(PortraitLimit))
        {
            if (_portraits.ContainsKey(path!))
            {
                _portraitLru.Remove(path!);
                _portraitLru.AddLast(path!);
                continue;
            }
            if (_portraits.Count >= PortraitLimit)
            {
                string oldest = _portraitLru.First!.Value;
                _portraitLru.RemoveFirst();
                _portraits[oldest]?.Dispose();
                _portraits.Remove(oldest);
            }
            var portrait = Load(path!);
            if (portrait is not null && Math.Max(portrait.Width, portrait.Height) > 128)
            {
                float factor = 128f / Math.Max(portrait.Width, portrait.Height);
                var small = portrait.Resize(new SKImageInfo(Math.Max(1, (int)(portrait.Width * factor)),
                    Math.Max(1, (int)(portrait.Height * factor))), SKFilterQuality.Medium);
                if (small is not null) { portrait.Dispose(); portrait = small; }
            }
            _portraits.Add(path!, portrait);
            _portraitLru.AddLast(path!);
        }
    }

    private SKBitmap? Load(string path)
    {
        try
        {
            string full = System.IO.Path.GetFullPath(System.IO.Path.Combine(_root, path));
            if (!full.StartsWith(_root.TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase)) return null;
            return _decode(full);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException) { return null; }
    }

    internal SKBitmap? Resolve(string key) => _illustrations.GetValueOrDefault(key);
    internal SKBitmap? ResolvePortrait(string? path) => path is null ? null : _portraits.GetValueOrDefault(path);
    internal SKColor Accent(string key) => _catalog.TryGetValue(key, out var visual) && SKColor.TryParse(visual.Accent, out var color)
        ? color : new SKColor(112, 215, 230);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var bitmap in _illustrations.Values.Concat(_portraits.Values)) bitmap?.Dispose();
        _illustrations.Clear(); _portraits.Clear(); _portraitLru.Clear(); _catalog.Clear();
    }
}
