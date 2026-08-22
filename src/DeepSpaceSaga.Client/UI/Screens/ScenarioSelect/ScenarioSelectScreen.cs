using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.ScenarioSelect;

/// <summary>
/// New Game -&gt; scenario picker: full-screen list of every scenario found under the
/// Scenarios/ directory, each with its Name and Description. Two nine-sliced panels sit
/// side by side on the outer background — a left content panel with the (button-free)
/// scenario list, and a right action panel holding the shared PLAY/BACK buttons at its
/// bottom. Clicking a row selects it (<see cref="ScenarioSelectZone.Row"/>, highlighted,
/// does not play); PLAY then acts on whichever scenario is currently selected — replacing
/// the old one-PLAY-button-per-row design, where every row played immediately.
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

    /// <summary>Absolute index into <see cref="_scenarios"/> of the row PLAY currently acts on, or -1 if none.</summary>
    private int _selectedIndex = -1;

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

    /// <summary>
    /// Panel background at the exact 900×620 panel size. Loaded once and shared by
    /// every ScenarioSelectScreen instance; falls back to MenuStyle.DrawPanel's plain
    /// fill if the file is missing.
    /// </summary>
    private static readonly SKBitmap? BackgroundImage =
        LoadImage("Images/UI/window-background-900x620.png");

    private static SKBitmap? LoadImage(string path)
    {
        try { return File.Exists(path) ? SKBitmap.Decode(path) : null; }
        catch { return null; }
    }

    /// <summary>True if the background PNG file was found and decoded at startup.</summary>
    internal static bool HasLoadedBackground => BackgroundImage is not null;

    /// <summary>
    /// The nine-sliced panel image shared by the content panel (scenario list) and the
    /// action panel (PLAY/BACK) — see <see cref="ScenarioSelectLayout.ContentPanelX"/>/Y/
    /// Width/Height and <see cref="ScenarioSelectLayout.ActionPanelX"/>/Y/Width/Height.
    /// Drawn via <see cref="NinePatch"/> so a single small source image covers any panel
    /// size while its rounded, transparent corners stay unscaled and unstretched.
    /// </summary>
    private static readonly SKBitmap? PanelImage =
        LoadImage("Images/UI/Panels/micro-panel.png");

    /// <summary>
    /// Corner/edge-sample size (in PanelImage source pixels) for <see cref="NinePatch"/> —
    /// a 20×20 cut at each corner, and a 20×20 sample from the middle of each edge for the
    /// stretched borders.
    /// </summary>
    private const float PanelCornerInset = 20f;

    /// <summary>True if the panel PNG file was found and decoded at startup.</summary>
    internal static bool HasLoadedContentPanel => PanelImage is not null;

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

    /// <summary>
    /// Selection is shown as a bright outline, not a lighter fill — ButtonFillPressed
    /// (78,78,78) sits too close to ColorTextDim (90,90,90) for the description line to
    /// stay legible against it, so the row fill always stays at its normal/hovered
    /// darkness and only the border brightens.
    /// </summary>
    private static readonly SKPaint _selectedRowBorder = new()
    {
        Color = MenuStyle.ColorText,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2f
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
                if (_selectedIndex < 0 || _selectedIndex >= _scenarios.Count)
                    return ScreenEvent.None;

                LastSelectedScenarioPath = _scenarios[_selectedIndex].ScenarioPath;
                return ScreenEvent.ScenarioSelected;
            }

            case ScenarioSelectZone.Row:
                _selectedIndex = AbsoluteIndex(hit.RowIndex);
                return ScreenEvent.None;

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
        if (BackgroundImage is not null)
            canvas.DrawBitmap(BackgroundImage, panelRect);
        else
            MenuStyle.DrawPanel(canvas, panelRect);

        float cx = pl + ScenarioSelectLayout.PanelWidth / 2f;
        canvas.DrawText("SELECT SCENARIO", cx, pt + ScenarioSelectLayout.TitleY, _titleTextPaint);

        DrawPanel(canvas, pl + ScenarioSelectLayout.ContentPanelX, pt + ScenarioSelectLayout.ContentPanelY,
            ScenarioSelectLayout.ContentPanelWidth, ScenarioSelectLayout.ContentPanelHeight);
        DrawScenarioList(canvas, pl, pt);
        if (_scenarios.Count > ScenarioSelectLayout.VisibleRows)
            DrawScrollbar(canvas, pl, pt);

        DrawActionPanel(canvas, pl, pt);
    }

    private void DrawPanel(SKCanvas canvas, float left, float top, float width, float height)
    {
        var rect = new SKRect(left, top, left + width, top + height);
        if (PanelImage is not null)
            NinePatch.Draw(canvas, PanelImage, rect, PanelCornerInset);
        else
            MenuStyle.DrawPanel(canvas, rect);
    }

    private void DrawActionPanel(SKCanvas canvas, float panelLeft, float panelTop)
    {
        DrawPanel(canvas, panelLeft + ScenarioSelectLayout.ActionPanelX, panelTop + ScenarioSelectLayout.ActionPanelY,
            ScenarioSelectLayout.ActionPanelWidth, ScenarioSelectLayout.ActionPanelHeight);

        float actionLeft = panelLeft + ScenarioSelectLayout.ActionPanelX;
        float actionTop = panelTop + ScenarioSelectLayout.ActionPanelY;

        var backRect = CombinedRect(actionLeft, actionTop, ScenarioSelectLayout.BackButtonRect());
        MenuStyle.DrawButton(canvas, backRect, "BACK",
            _hoveredZone == ScenarioSelectZone.Back ? ButtonState.Hovered : ButtonState.Normal);

        bool canPlay = _selectedIndex >= 0 && _selectedIndex < _scenarios.Count;
        var playRect = CombinedRect(actionLeft, actionTop, ScenarioSelectLayout.PlayButtonRect());
        var playState = !canPlay
            ? ButtonState.Disabled
            : _hoveredZone == ScenarioSelectZone.Play ? ButtonState.Hovered : ButtonState.Normal;
        MenuStyle.DrawButton(canvas, playRect, "PLAY", playState);
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

            bool isSelected = AbsoluteIndex(i) == _selectedIndex;
            bool isHovered = _hoveredZone == ScenarioSelectZone.Row && _hoveredRowIndex == i;
            var fill = isHovered ? MenuStyle.ButtonFillHover : MenuStyle.ButtonFillNormal;

            canvas.DrawRect(rowRect, fill);
            canvas.DrawRect(rowRect, isSelected ? _selectedRowBorder : MenuStyle.ButtonBorder);

            canvas.Save();
            canvas.ClipRect(rowRect);
            canvas.DrawText(scenario.Name, rowRect.Left + 10f, rowRect.Top + 22f, _rowNamePaint);
            canvas.DrawText(scenario.Description, rowRect.Left + 10f, rowRect.Top + 42f, _rowDescriptionPaint);
            canvas.Restore();
        }
    }

    private static SKRect CombinedRect(float panelLeft, float panelTop, (float X, float Y, float W, float H) local) =>
        new(panelLeft + local.X, panelTop + local.Y, panelLeft + local.X + local.W, panelTop + local.Y + local.H);

    private void RefreshScenarios()
    {
        _scenarios = _listScenarios() ?? Array.Empty<ScenarioInfo>();
        int maxOffset = Math.Max(0, _scenarios.Count - ScenarioSelectLayout.VisibleRows);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, maxOffset);
        _selectedIndex = _scenarios.Count > 0 ? 0 : -1;
    }

    private int VisibleScenarioCount => Math.Max(0, Math.Min(ScenarioSelectLayout.VisibleRows, _scenarios.Count - _scrollOffset));

    private int AbsoluteIndex(int visibleRowIndex) => visibleRowIndex < 0 ? -1 : _scrollOffset + visibleRowIndex;
}
