using DeepSpaceSaga.Motion.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed class TacticalBattleSetupLayout
{
    private static readonly double[] ColumnEdges = [0, 0.10, 0.30, 0.58, 0.76, 1];
    private readonly float _rowStart;
    public SKRect Viewport { get; }
    public SKRect Content { get; }
    public SKRect ScenarioCaption { get; }
    public SKRect Scenario { get; }
    public SKRect Description { get; }
    public SKRect Rules { get; }
    public SKRect OwnTactic { get; }
    public SKRect EnemyTactic { get; }
    public SKRect Seed { get; }
    public SKRect RandomSeed { get; }
    public SKRect Help { get; }
    public SKRect Start { get; }
    public SKRect Close { get; }
    public SKRect Status { get; }
    public float Scroll { get; }
    public float MaximumScroll { get; }
    public int DroneCount { get; }

    public TacticalBattleSetupLayout(int width, int height, float uiScale, int droneCount, float scroll)
    {
        float w = (float)PyMath.Max(1, width / (double)uiScale), h = (float)PyMath.Max(1, height / (double)uiScale);
        Viewport = new(0, 0, w, h);
        Content = new(20, 54, w - 20, h - 68);
        DroneCount = droneCount;
        float fullHeight = 412 + 42 * droneCount;
        MaximumScroll = (float)PyMath.Max(0, fullHeight - Content.Height);
        Scroll = (float)PyMath.Clamp(scroll, 0, MaximumScroll);
        float y = Content.Top - Scroll, x = Content.Left;
        float available = (float)PyMath.Max(1, Content.Width);
        float column = (float)PyMath.Min(420, PyMath.Max(1, (available - 12) * 0.5));
        ScenarioCaption = SKRect.Create(x, y, column, 18);
        Scenario = SKRect.Create(x, y + 20, column, 32);
        Description = SKRect.Create(x, y + 56, available, 54);
        Rules = SKRect.Create(x, y + 116, available, 60);
        _rowStart = y + 206;
        float tactics = _rowStart + 42 * droneCount + 24;
        OwnTactic = SKRect.Create(x, tactics, column, 32);
        EnemyTactic = SKRect.Create(x + column + 12, tactics, column, 32);
        Seed = SKRect.Create(x, tactics + 56, (float)PyMath.Min(212, available * 0.45), 32);
        RandomSeed = SKRect.Create(Seed.Right + 12, Seed.Top, (float)PyMath.Min(190, available * 0.38), 32);
        Help = SKRect.Create(x, Seed.Bottom + 6, available, 88);
        float button = (float)PyMath.Min(225, available * 0.35);
        Close = SKRect.Create(x, h - 52, button, 36);
        Start = SKRect.Create(Content.Right - button, h - 52, button, 36);
        Status = new(Close.Right + 8, Close.Top, Start.Left - 8, Close.Bottom);
    }
    private SKRect Cell(int column, float top, float height) => new(
        Content.Left + (float)(Content.Width * ColumnEdges[column]) + 3, top,
        Content.Left + (float)(Content.Width * ColumnEdges[column + 1]) - 3, top + height);
    public SKRect ColumnTitle(int column) => Cell(column, _rowStart - 26, 24);
    public SKRect RowCell(int row, int column) => Cell(column, _rowStart + 42 * row, column < 2 ? 40 : 32);
    public static SKRect Above(SKRect rect) => new(rect.Left, rect.Top - 20, rect.Right, rect.Top - 2);
}
