using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class BattleResultsScreen
{
    private const float CardHeight = 56;
    private const float RowHeight = 58;
    private readonly HashSet<string> _expandedSources = new(StringComparer.Ordinal);
    private readonly List<(string Id, SKRect Bounds)> _sourceHits = [];
    private float _scroll;
    private float _maxScroll;

    internal IReadOnlyList<(string Id, SKRect Bounds)> SourceHits => _sourceHits;
    internal bool IsExpanded(string objectId) => _expandedSources.Contains(objectId);

    private LootSessionSnapshot? CurrentLoot
    {
        get
        {
            var campaign = Flow.Session.DemoCampaign;
            var loot = campaign?.Rewards?.Loot;
            if (loot is not null && loot.BattleId == _terminalBattle?.BattleId)
                _lootPresentation.Observe(loot, campaign!.Revision);
            return _lootPresentation.State is { } current && current.BattleId == _terminalBattle?.BattleId && Report?.CanLoot == true
                ? current : null;
        }
    }

    private void DrawSources(SKCanvas canvas)
    {
        _sourceHits.Clear();
        _rowHits.Clear();
        if (CurrentLoot is not { } loot || Contents.IsEmpty) return;
        var sources = loot.Objects.Select(source => (Source: source, Rows: IsExpanded(source.ObjectId) ? LootRows(source) : null)).ToArray();
        float height = sources.Sum(source => CardHeight + (source.Rows is { } rows ? Math.Max(1, rows.Count) * RowHeight : 0));
        _maxScroll = Math.Max(0, height - Contents.Height);
        _scroll = Math.Clamp(_scroll, 0, _maxScroll);
        float y = Contents.Top - _scroll;
        canvas.Save();
        try
        {
            canvas.ClipRect(Contents);
            foreach (var (source, rows) in sources)
            {
                var card = new SKRect(Contents.Left, y, Math.Min(Contents.Left + 350, Contents.Right), y + CardHeight - 5);
                var visible = SKRect.Intersect(card, Contents);
                if (!visible.IsEmpty)
                {
                    _sourceHits.Add((source.ObjectId, visible));
                    DrawSourceCard(canvas, source, card, rows is not null);
                }
                y += CardHeight;
                if (rows is null) continue;
                if (rows.Count == 0)
                {
                    T(canvas, ResultsText("EmptySource"), Contents.Left + 16, y + 22, 16, Muted);
                    y += RowHeight;
                }
                foreach (var row in rows)
                {
                    var rect = new SKRect(Contents.Left, y, Contents.Right, y + RowHeight - 4);
                    if (!SKRect.Intersect(rect, Contents).IsEmpty) DrawLootRow(canvas, source.ObjectId, row, rect, loot);
                    y += RowHeight;
                }
            }
        }
        finally { canvas.Restore(); }
    }

    private void DrawSourceCard(SKCanvas canvas, LootObjectSnapshot source, SKRect card, bool expanded)
    {
        var unit = _terminalBattle!.Units.FirstOrDefault(unit => unit.Id == source.ObjectId);
        string name = unit is null ? source.ObjectId : PilotCardModel.Name(unit);
        string kind = Localization.Get("loot.source." + source.Kind);
        Round(canvas, card, expanded ? PanelSel : Box, BoxEdge);
        T(canvas, expanded ? "−" : "+", card.Left + 10, card.Top + 24, 18, Yellow, true);
        T(canvas, name, card.Left + 34, card.Top + 21, FitSize(name, 17, card.Width - 46), Text, true);
        T(canvas, kind, card.Left + 34, card.Top + 40, FitSize(kind, 13, card.Width - 46), Muted);
    }

    private void ToggleSource(float x, float y)
    {
        foreach (var hit in _sourceHits)
        {
            if (!hit.Bounds.Contains(x, y)) continue;
            if (!_expandedSources.Add(hit.Id)) _expandedSources.Remove(hit.Id);
            _sourceHits.Clear();
            _rowHits.Clear();
            break;
        }
    }
}
