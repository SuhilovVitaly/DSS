using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.ScenarioSelect;

/// <summary>
/// New Game -&gt; scenario picker: full-screen list of every scenario found under the
/// Scenarios/ directory, each with its Name and Description, and a PLAY button per row.
/// Structural sibling of <see cref="Load.LoadScreen"/> (dim-free, since this is a
/// top-level screen replacing MainMenu rather than an overlay on top of a paused game;
/// scrollable row list; pure <see cref="ScenarioSelectLayout.HitTest"/> geometry), minus
/// the two-stage Delete machinery Load needs and ScenarioSelect has no use for.
/// A PLAY click returns <see cref="ScreenEvent.ScenarioSelected"/> and the chosen
/// scenario's path is exposed via <see cref="LastSelectedScenarioPath"/>, set
/// synchronously in the same call as <see cref="OnMouseDown(float, float, MouseButton)"/>
/// returning that event — the caller (SkiaWindow's mouse-dispatch site) reads it in the
/// same synchronous frame, before any await, mirroring how LoadScreen's
/// LastRequestedSlotId is consumed (see LoadScreen's doc comment for why).
/// BACK returns <see cref="ScreenEvent.MainMenu"/> directly (this screen never has a
/// session to protect — there's nothing to pause/resume, unlike GameMenu's overlays).
/// </summary>
public sealed class ScenarioSelectScreen : IScreen
{
    private readonly Func<IReadOnlyList<ScenarioInfo>> _listScenarios;

    private IReadOnlyList<ScenarioInfo> _scenarios = Array.Empty<ScenarioInfo>();
    private int _scrollOffset;

    private int _screenWidth;
    private int _screenHeight;

    private ScenarioSelectZone _hoveredZone = ScenarioSelectZone.None;
    private int _hoveredRowIndex = -1;

    /// <summary>
    /// The scenario path of the most recent PLAY click that returned
    /// <see cref="ScreenEvent.ScenarioSelected"/> from <see cref="OnMouseDown(float, float, MouseButton)"/>.
    /// Only meaningful when read synchronously, immediately after that call returns — see
    /// the type doc comment.
    /// </summary>
    public string? LastSelectedScenarioPath { get; private set; }

    private static readonly SKPaint _titleTextPaint = MenuStyle.TextTitle;

    private static readonly SKPaint _rowNamePaint = new()
    {
        Color = MenuStyle.ColorText,
        TextSize = MenuStyle.ButtonFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Left,
        Typeface = MenuStyle.TypefaceBold
    };

    private static readonly SKPaint _rowDescriptionPaint = new()
    {
        Color = MenuStyle.ColorTextDim,
        TextSize = MenuStyle.StatusFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Left,
        Typeface = MenuStyle.TypefaceRegular
    };

    private static readonly SKPaint _buttonTextPaint = new()
    {
        Color = MenuStyle.ColorText,
        TextSize = MenuStyle.ButtonFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = MenuStyle.TypefaceBold
    };

    /// <param name="listScenarios">Enumerates every playable scenario found on disk. Called at construction and on every activation.</param>
    public ScenarioSelectScreen(Func<IReadOnlyList<ScenarioInfo>> listScenarios)
    {
        _listScenarios = listScenarios;
        RefreshScenarios();
    }

    public void OnActivated()
    {
        _hoveredZone = ScenarioSelectZone.None;
        _hoveredRowIndex = -1;
        RefreshScenarios();
    }

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key) =>
        key == Key.Escape ? ScreenEvent.MainMenu : ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        var hit = ScenarioSelectLayout.HitTest(x, y, _screenWidth, _screenHeight, VisibleScenarioCount);

        switch (hit.Zone)
        {
            case ScenarioSelectZone.Back:
                return ScreenEvent.MainMenu;

            case ScenarioSelectZone.Play:
            {
                int index = AbsoluteIndex(hit.RowIndex);
                if (index < 0 || index >= _scenarios.Count)
                    return ScreenEvent.None;

                LastSelectedScenarioPath = _scenarios[index].ScenarioPath;
                return ScreenEvent.ScenarioSelected;
            }

            default:
                return ScreenEvent.None;
        }
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call-site/test conventions.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        var hit = ScenarioSelectLayout.HitTest(x, y, _screenWidth, _screenHeight, VisibleScenarioCount);
        _hoveredZone = hit.Zone;
        _hoveredRowIndex = hit.RowIndex;
        return hit.Zone != ScenarioSelectZone.None;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        int maxOffset = Math.Max(0, _scenarios.Count - ScenarioSelectLayout.VisibleRows);
        _scrollOffset = Math.Clamp(_scrollOffset - Math.Sign(delta), 0, maxOffset);
        return ScreenEvent.None;
    }

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        MenuStyle.DrawBackground(canvas, width, height);

        float pl = ScenarioSelectLayout.PanelLeft(width);
        float pt = ScenarioSelectLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + ScenarioSelectLayout.PanelWidth, pt + ScenarioSelectLayout.PanelHeight);
        MenuStyle.DrawPanel(canvas, panelRect);

        float cx = pl + ScenarioSelectLayout.PanelWidth / 2f;
        canvas.DrawText("SELECT SCENARIO", cx, pt + ScenarioSelectLayout.TitleY, _titleTextPaint);

        DrawScenarioList(canvas, pl, pt);
        if (_scenarios.Count > ScenarioSelectLayout.VisibleRows)
            DrawScrollbar(canvas, pl, pt);

        DrawButton(canvas, CombinedRect(pl, pt, ScenarioSelectLayout.BackButtonRect()), "BACK", ScenarioSelectZone.Back, -1);
    }

    private void DrawScrollbar(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var track = CombinedRect(panelLeft, panelTop, ScenarioSelectLayout.ScrollbarTrackRect());
        canvas.DrawRect(track, MenuStyle.ButtonFillNormal);
        canvas.DrawRect(track, MenuStyle.ButtonBorder);

        var thumb = CombinedRect(panelLeft, panelTop, ScenarioSelectLayout.ScrollbarThumbRect(_scrollOffset, _scenarios.Count));
        canvas.DrawRect(thumb, MenuStyle.ButtonFillHover);
    }

    private void DrawScenarioList(SKCanvas canvas, float panelLeft, float panelTop)
    {
        for (int i = 0; i < VisibleScenarioCount; i++)
        {
            var scenario = _scenarios[_scrollOffset + i];
            var row = ScenarioSelectLayout.RowRect(i);
            var rowRect = CombinedRect(panelLeft, panelTop, row);

            canvas.DrawRect(rowRect, MenuStyle.ButtonFillNormal);
            canvas.DrawRect(rowRect, MenuStyle.ButtonBorder);

            canvas.Save();
            canvas.ClipRect(new SKRect(rowRect.Left, rowRect.Top, rowRect.Right - ScenarioSelectLayout.PlayButtonWidth - 10f, rowRect.Bottom));
            canvas.DrawText(scenario.Name, rowRect.Left + 10f, rowRect.Top + 22f, _rowNamePaint);
            canvas.DrawText(scenario.Description, rowRect.Left + 10f, rowRect.Top + 42f, _rowDescriptionPaint);
            canvas.Restore();

            DrawButton(canvas, CombinedRect(panelLeft, panelTop, ScenarioSelectLayout.PlayButtonRect(i)), "PLAY", ScenarioSelectZone.Play, i);
        }
    }

    private void DrawButton(SKCanvas canvas, SKRect rect, string text, ScenarioSelectZone zone, int rowIndex)
    {
        bool isHovered = _hoveredZone == zone && _hoveredRowIndex == rowIndex;

        canvas.DrawRect(rect, isHovered ? MenuStyle.ButtonFillHover : MenuStyle.ButtonFillNormal);
        canvas.DrawRect(rect, MenuStyle.ButtonBorder);

        float textY = rect.MidY + _buttonTextPaint.TextSize / 3f;
        canvas.DrawText(text, rect.MidX, textY, _buttonTextPaint);
    }

    private static SKRect CombinedRect(float panelLeft, float panelTop, (float X, float Y, float W, float H) local) =>
        new(panelLeft + local.X, panelTop + local.Y, panelLeft + local.X + local.W, panelTop + local.Y + local.H);

    private void RefreshScenarios()
    {
        _scenarios = _listScenarios() ?? Array.Empty<ScenarioInfo>();
        int maxOffset = Math.Max(0, _scenarios.Count - ScenarioSelectLayout.VisibleRows);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, maxOffset);
    }

    private int VisibleScenarioCount => Math.Max(0, Math.Min(ScenarioSelectLayout.VisibleRows, _scenarios.Count - _scrollOffset));

    private int AbsoluteIndex(int visibleRowIndex) => visibleRowIndex < 0 ? -1 : _scrollOffset + visibleRowIndex;
}
