using System.Globalization;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;
using SkinStyle = DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleSkinRenderScope;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed class TacticalBattleSidesTab
{
    internal sealed record Card(TacticalUnitSnapshot Unit, bool Own, string Health, string Details, bool Selectable);
    private readonly List<(string Id, SKRect Bounds)> _selectable = [];
    private SKRect _bounds;
    private float _scroll, _maximumScroll, _scale;
    internal IReadOnlyList<(string Id, SKRect Bounds)> Selectable => _selectable;

    internal static IEnumerable<Card> Cards(TacticalBattleFrame frame)
    {
        var battle = frame.Battle;
        var units = frame.Presentation == TacticalBattlePresentation.SalvoFlight && !battle.BeforeSalvoUnits.IsDefaultOrEmpty
            ? battle.BeforeSalvoUnits : battle.Units;
        return units.OrderBy(u => u.Side == battle.PlayerSide ? 0 : 1).ThenBy(u => u.Kind == TacticalUnitKind.Carrier ? 0 : 1)
            .Select(u => Describe(u, u.Side == battle.PlayerSide));
    }

    internal void Draw(SKCanvas canvas, TacticalBattleFrame frame, SKRect bounds)
    {
        _bounds = bounds;
        _scale = frame.UiScale;
        _selectable.Clear();
        if (bounds.IsEmpty) return;
        float s = _scale, width = (float)PyMath.Max(0, bounds.Width - 6 * s);
        using var text = new SKPaint { Typeface = MenuStyle.TypefaceRegular, TextSize = 14 * s };
        float detailsWidth = (float)PyMath.Max(1, width - 16 * s);
        var rows = Cards(frame).Select(card =>
        {
            var lines = ControlText.Wrap(card.Details, detailsWidth, text);
            float height = (card.Unit.MaxArmor > 0 ? 64 : 54) * s + lines.Length * 17 * s;
            return (Card: card, Lines: lines, Height: height);
        }).ToArray();
        float total = rows.Sum(row => row.Height + 6 * s) + 52 * s;
        _maximumScroll = (float)PyMath.Max(0, total - bounds.Height);
        _scroll = (float)PyMath.Clamp(_scroll, 0, _maximumScroll);
        float y = bounds.Top - _scroll;
        bool? lastSide = null;
        canvas.Save();
        canvas.ClipRect(bounds);
        if (SkinStyle.HasBoxStyle("tabs.sides"))
            SkinStyle.Box(canvas, "tabs.sides", bounds, SKColors.Transparent, null, s);
        foreach (var row in rows)
        {
            var card = row.Card;
            string side = card.Own ? "own" : "enemy";
            if (lastSide != card.Own)
            {
                ControlVisuals.Label(canvas, F(card.Own ? "Sides.Own" : "Sides.Enemy", card.Unit.Side),
                    SKRect.Create(bounds.Left, y, width, 24 * s), true, s, role: $"tabs.sides.title.{side}");
                y += 26 * s;
                lastSide = card.Own;
            }
            var rect = SKRect.Create(bounds.Left, y, width, row.Height);
            y += row.Height + 6 * s;
            if (!rect.IntersectsWith(bounds)) continue;
            bool selected = card.Unit.Id == frame.SelectedDroneId;
            if (card.Selectable) _selectable.Add((card.Unit.Id, rect));
            string state = card.Unit.State.ToString().ToLowerInvariant();
            string role = $"tabs.sides.card.{side}.{state}";
            if (selected) role += ".selected";
            ControlVisuals.Frame(canvas, rect, selected, true, s, role);
            float left = rect.Left + 8 * s, right = rect.Right - 8 * s, line = rect.Top + 4 * s;
            ControlVisuals.Label(canvas, card.Unit.Id, new(left, line, right, line + 18 * s), true, s, role: role + ".title");
            line += 20 * s;
            bool inactive = card.Unit.State is TacticalUnitState.Destroyed or TacticalUnitState.Disabled;
            if (card.Unit.MaxArmor > 0)
            {
                using (SkinStyle.EnterRole($"tabs.sides.bar.armor.{side}.{state}"))
                    ProgressBar.Draw(canvas, SKRect.Create(left, line, right - left, 5 * s),
                        card.Unit.Armor / (double)card.Unit.MaxArmor, inactive ? TacticalBattleMapRenderer.Inactive : new(126, 159, 181));
                line += 10 * s;
            }
            if (card.Unit.MaxHull > 0)
            {
                using var bar = SkinStyle.EnterRole($"tabs.sides.bar.hull.{side}.{state}");
                ProgressBar.Draw(canvas, SKRect.Create(left, line, right - left, 5 * s),
                    card.Unit.Hull / (double)card.Unit.MaxHull, inactive ? TacticalBattleMapRenderer.Inactive :
                        card.Own ? TacticalBattleMapRenderer.Own : TacticalBattleMapRenderer.Enemy);
            }
            line += 8 * s;
            ControlVisuals.Label(canvas, card.Health, new(left, line, right, line + 18 * s), true, s, role: role + ".health");
            line += 18 * s;
            using var details = ControlText.Paint(s, role + ".text", card.Details);
            details.TextSize = (float)PyMath.Min(details.TextSize, 16 * s);
            foreach (var detail in ControlText.Wrap(card.Details, detailsWidth, details).Take(row.Lines.Length))
            {
                ControlVisuals.DrawLabel(canvas, detail, new(left, line, right, line + 17 * s), details);
                line += 17 * s;
            }
        }
        if (_maximumScroll > 0)
        {
            float thumb = (float)(bounds.Height * (bounds.Height / (double)total));
            float offset = (bounds.Height - thumb) * (float)(_scroll / (double)_maximumScroll);
            if (SkinStyle.HasBoxStyle("tabs.sides.scroll.track"))
                SkinStyle.Box(canvas, "tabs.sides.scroll.track",
                    SKRect.Create(bounds.Right - 3 * s, bounds.Top, 2 * s, bounds.Height), SKColors.Transparent, null, s, antialias: false);
            SkinStyle.Box(canvas, "tabs.sides.scroll.thumb",
                SKRect.Create(bounds.Right - 3 * s, bounds.Top + offset, 2 * s, thumb), new(75, 127, 149), null, s, antialias: false);
        }
        canvas.Restore();
    }

    internal string? Hit(float x, float y, MouseButton button) => button == MouseButton.Left && _bounds.Contains(x, y)
        ? _selectable.FirstOrDefault(card => card.Bounds.Contains(x, y)).Id : null;
    internal bool Hover(float x, float y) => _bounds.Contains(x, y) && _selectable.Any(card => card.Bounds.Contains(x, y));
    internal bool OnMouseWheel(float x, float y, float delta)
    {
        if (!_bounds.Contains(x, y) || !float.IsFinite(delta)) return false;
        _scroll = (float)PyMath.Clamp(_scroll + delta / 100.0 * 54 * _scale, 0, _maximumScroll);
        return true;
    }

    internal static Card Describe(TacticalUnitSnapshot unit, bool own)
    {
        string health = F("Sides.Hull", N(unit.Hull), N(unit.MaxHull));
        if (unit.MaxArmor > 0) health = F("Sides.Armor", N(unit.Armor), N(unit.MaxArmor)) + " · " + health;
        string details = unit.State switch
        {
            TacticalUnitState.Destroyed => L(unit.Kind == TacticalUnitKind.Carrier ? "SalvoCarrierDestroyed" : "SalvoDestroyed"),
            TacticalUnitState.Disabled => F("Sides.Disabled", N(unit.Hull)),
            _ when unit.Kind == TacticalUnitKind.Carrier => L("Sides.Carrier"),
            _ => BuildDetails(unit)
        };
        if (unit.Kind == TacticalUnitKind.Drone)
            details += " · " + F("Initiative", unit.Initiative is { } initiative ? N(initiative) : "—");
        return new(unit, own, health, details, own && unit.Kind == TacticalUnitKind.Drone && unit.State == TacticalUnitState.Air);
    }

    private static string BuildDetails(TacticalUnitSnapshot unit)
    {
        if (unit.Build is not { } build) return L("UnitState." + unit.State);
        var details = new List<string>
        {
            build.FrameName, F("Sides.Pilot", build.PilotName), F("SetupSkills", N(unit.Accuracy), N(unit.Maneuver)),
            F("Sides.Gun", build.WeaponName, BattleFormat.Degrees(2 * build.ArcDegrees), N(build.Range)),
            build.ArmorName, build.ShieldCount > 0 ? F("Sides.Shield", build.ShieldName ?? "", N(build.ShieldCount)) : L("NoShield"),
            L("Speed") + " " + BattleFormat.Number(unit.Speed, 1)
        };
        if ((unit.RoleName ?? unit.RoleId) is { Length: > 0 } role) details.Add(role);
        if (unit.Jammed) details.Add(L("Sides.Jammed"));
        foreach (var engine in unit.Engines)
            if (engine.Factor < 1) details.Add(F("Sides.Engine", engine.Name, BattleFormat.Probability(engine.Factor)));
        return string.Join(" · ", details);
    }

    private static string N(double number) => BattleFormat.Number(number);
    private static string L(string key) => BattleFormat.Localize(key);
    private static string F(string key, params object[] args) => BattleFormat.Text(string.Format(CultureInfo.InvariantCulture, L(key), args));
}
