using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed record BattleText(string Text, SKColor Color);
internal sealed class BattleLayout
{
    public Dictionary<string, SKRect> Rects { get; } = [];
    public List<BattleText> Texts { get; } = [];
    public TacticalBattleView Camera { get; set; }
}
