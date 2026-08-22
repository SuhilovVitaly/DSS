using System.Text;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.ScenarioSelect;

/// <summary>
/// New Game -&gt; scenario picker: full-screen list of every scenario found under the
/// Scenarios/ directory, each with its Name and word-wrapped Description. Two nine-sliced
/// panels sit side by side on the outer background — a left content panel with the
/// (button-free) scenario list, and a right action panel holding the shared PLAY/BACK
/// buttons at its bottom. Clicking a row selects it (<see cref="ScenarioSelectZone.Row"/>,
/// highlighted, does not play); PLAY then acts on whichever scenario is currently selected.
/// A row's height grows with however many lines its description wraps to at the list's
/// width (<see cref="ComputeVisibleRows"/>/<see cref="ScenarioSelectLayout.RowHeightFor"/>),
/// so the number of rows that fit varies with their content, not a fixed row count.
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

    /// <summary>Absolute index of the hovered scenario row, meaningful only when <see cref="_hoveredZone"/> is Row.</summary>
    private int _hoveredScenarioIndex = -1;

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

    /// <summary>Left/right text inset within a row, so wrapped lines never touch its border.</summary>
    private const float RowTextInset = 10f;

    private static readonly SKPaint _scenarioNamePaint = new()
    {
        Color = MenuStyle.ColorText,
        TextSize = 20f,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = MenuStyle.TypefaceBold
    };

    private static readonly SKPaint _infoLabelPaint = new()
    {
        Color = new SKColor(0x00, 0xFF, 0xFF),
        TextSize = MenuStyle.ButtonFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Left,
        Typeface = MenuStyle.TypefaceBold
    };

    private static readonly SKPaint _infoValuePaint = new()
    {
        Color = new SKColor(0xE5, 0xE4, 0xE1),
        TextSize = MenuStyle.ButtonFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Right,
        Typeface = MenuStyle.TypefaceRegular
    };

    private static readonly SKPaint _infoSeparatorPaint = new()
    {
        Color = new SKColor(0xFF, 0x84, 0x04),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = ScenarioSelectLayout.InfoSeparatorStrokeWidth
    };

    /// <summary>
    /// Stub scenario stats shown on the action panel — ScenarioInfo has no
    /// Difficulty/Environment/Crew fields yet, so these are fixed placeholder values
    /// (same spirit as the "not available yet" placeholders on Station/Trade/Finance)
    /// until the Engine exposes real per-scenario metadata.
    /// </summary>
    private static readonly (string Label, string Value)[] InfoRows =
    {
        ("DIFFICULTY", "NORMAL"),
        ("ENVIRONMENT", "OPEN SPACE"),
        ("CREW", "1 person"),
    };

    /// <summary>One currently-visible scenario row, with its description already word-wrapped and its height resolved.</summary>
    private readonly record struct ScenarioRow(int AbsoluteIndex, string Name, IReadOnlyList<string> DescriptionLines, float Y, float Height)
    {
        public ScenarioSelectLayout.VisibleRow ToLayoutRow() => new(AbsoluteIndex, Y, Height);
    }

    /// <param name="listScenarios">Enumerates every playable scenario found on disk. Called at construction and on every activation.</param>
    public ScenarioSelectScreen(Func<IReadOnlyList<ScenarioInfo>> listScenarios)
    {
        _listScenarios = listScenarios;
        RefreshScenarios();
    }

    public void OnActivated()
    {
        _hoveredZone = ScenarioSelectZone.None;
        _hoveredScenarioIndex = -1;
        RefreshScenarios();
    }

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key) =>
        key == Key.Escape ? ScreenEvent.MainMenu : ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        var hit = HitTest(x, y);

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
                _selectedIndex = hit.RowIndex;
                return ScreenEvent.None;

            default:
                return ScreenEvent.None;
        }
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call-site/test conventions.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        var hit = HitTest(x, y);
        _hoveredZone = hit.Zone;
        _hoveredScenarioIndex = hit.RowIndex;
        return hit.Zone != ScenarioSelectZone.None;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        int visibleCount = Math.Max(1, ComputeVisibleRows().Count);
        int maxOffset = Math.Max(0, _scenarios.Count - visibleCount);
        _scrollOffset = Math.Clamp(_scrollOffset - Math.Sign(delta), 0, maxOffset);
        return ScreenEvent.None;
    }

    private ScenarioSelectHit HitTest(float x, float y)
    {
        var visibleRows = ComputeVisibleRows().Select(r => r.ToLayoutRow()).ToList();
        return ScenarioSelectLayout.HitTest(x, y, _screenWidth, _screenHeight, visibleRows);
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

        var titleRect = new SKRect(
            pl + ScenarioSelectLayout.TitleRectX, pt + ScenarioSelectLayout.TitleRectY,
            pl + ScenarioSelectLayout.TitleRectX + ScenarioSelectLayout.TitleRectWidth,
            pt + ScenarioSelectLayout.TitleRectY + ScenarioSelectLayout.TitleRectHeight);
        canvas.DrawText("SELECT SCENARIO", titleRect.MidX, MenuStyle.VerticalCenterBaseline(titleRect, _titleTextPaint), _titleTextPaint);

        ImagePanel.Draw(canvas, new SKRect(
            pl + ScenarioSelectLayout.ContentPanelX, pt + ScenarioSelectLayout.ContentPanelY,
            pl + ScenarioSelectLayout.ContentPanelX + ScenarioSelectLayout.ContentPanelWidth,
            pt + ScenarioSelectLayout.ContentPanelY + ScenarioSelectLayout.ContentPanelHeight));

        var visibleRows = ComputeVisibleRows();
        DrawScenarioList(canvas, pl, pt, visibleRows);

        bool hasOverflow = _scrollOffset > 0 || _scrollOffset + visibleRows.Count < _scenarios.Count;
        if (hasOverflow)
            DrawScrollbar(canvas, pl, pt, visibleRows.Count);

        DrawActionPanel(canvas, pl, pt);
    }

    private void DrawActionPanel(SKCanvas canvas, float panelLeft, float panelTop)
    {
        ImagePanel.Draw(canvas, new SKRect(
            panelLeft + ScenarioSelectLayout.ActionPanelX, panelTop + ScenarioSelectLayout.ActionPanelY,
            panelLeft + ScenarioSelectLayout.ActionPanelX + ScenarioSelectLayout.ActionPanelWidth,
            panelTop + ScenarioSelectLayout.ActionPanelY + ScenarioSelectLayout.ActionPanelHeight));

        float actionLeft = panelLeft + ScenarioSelectLayout.ActionPanelX;
        float actionTop = panelTop + ScenarioSelectLayout.ActionPanelY;
        float actionCenterX = actionLeft + ScenarioSelectLayout.ActionPanelWidth / 2f;

        if (_selectedIndex >= 0 && _selectedIndex < _scenarios.Count)
        {
            canvas.DrawText(_scenarios[_selectedIndex].Name, actionCenterX,
                actionTop + ScenarioSelectLayout.InfoNameBaselineY, _scenarioNamePaint);

            for (int i = 0; i < InfoRows.Length; i++)
            {
                float baseline = actionTop + ScenarioSelectLayout.InfoRowsTopY + i * ScenarioSelectLayout.InfoRowSpacing;
                DrawInfoRow(canvas, actionLeft, actionLeft + ScenarioSelectLayout.ActionPanelWidth, actionCenterX,
                    baseline, InfoRows[i].Label, InfoRows[i].Value);
            }
        }

        var backRect = CombinedRect(actionLeft, actionTop, ScenarioSelectLayout.BackButtonRect());
        ImageButton.Draw(canvas, backRect, "BACK",
            _hoveredZone == ScenarioSelectZone.Back ? ButtonState.Hovered : ButtonState.Normal);

        bool canPlay = _selectedIndex >= 0 && _selectedIndex < _scenarios.Count;
        var playRect = CombinedRect(actionLeft, actionTop, ScenarioSelectLayout.PlayButtonRect());
        var playState = !canPlay
            ? ButtonState.Disabled
            : _hoveredZone == ScenarioSelectZone.Play ? ButtonState.Hovered : ButtonState.Normal;
        ImageButton.Draw(canvas, playRect, "PLAY", playState);
    }

    /// <summary>
    /// One "LABEL | VALUE" info row: the cyan label pinned to the panel's left edge
    /// (InfoSideMargin in), the value pinned to its right edge, and the orange separator
    /// line — taller than the text — always exactly at <paramref name="centerX"/>,
    /// regardless of how long either label or value is.
    /// </summary>
    private void DrawInfoRow(SKCanvas canvas, float actionLeft, float actionRight, float centerX, float baselineY, string label, string value)
    {
        canvas.DrawText(label, actionLeft + ScenarioSelectLayout.InfoSideMargin, baselineY, _infoLabelPaint);
        canvas.DrawText(value, actionRight - ScenarioSelectLayout.InfoSideMargin, baselineY, _infoValuePaint);

        var metrics = _infoLabelPaint.FontMetrics;
        float overhang = ScenarioSelectLayout.InfoSeparatorOverhang;
        canvas.DrawLine(
            centerX, baselineY + metrics.Ascent - overhang,
            centerX, baselineY + metrics.Descent + overhang,
            _infoSeparatorPaint);
    }

    private void DrawScrollbar(SKCanvas canvas, float panelLeft, float panelTop, int visibleRowCount)
    {
        var track = CombinedRect(panelLeft, panelTop, ScenarioSelectLayout.ScrollbarTrackRect());
        canvas.DrawRect(track, MenuStyle.ButtonFillNormal);
        canvas.DrawRect(track, MenuStyle.ButtonBorder);

        var thumb = CombinedRect(panelLeft, panelTop,
            ScenarioSelectLayout.ScrollbarThumbRect(_scrollOffset, _scenarios.Count, visibleRowCount));
        canvas.DrawRect(thumb, MenuStyle.ButtonFillHover);
    }

    private void DrawScenarioList(SKCanvas canvas, float panelLeft, float panelTop, IReadOnlyList<ScenarioRow> rows)
    {
        foreach (var row in rows)
        {
            var rowRect = CombinedRect(panelLeft, panelTop, ScenarioSelectLayout.RowRect(row.Y, row.Height));

            bool isSelected = row.AbsoluteIndex == _selectedIndex;
            bool isHovered = _hoveredZone == ScenarioSelectZone.Row && _hoveredScenarioIndex == row.AbsoluteIndex;
            var fill = isHovered ? MenuStyle.ButtonFillHover : MenuStyle.ButtonFillNormal;

            canvas.DrawRect(rowRect, fill);
            canvas.DrawRect(rowRect, isSelected ? _selectedRowBorder : MenuStyle.ButtonBorder);

            canvas.Save();
            canvas.ClipRect(rowRect);
            canvas.DrawText(row.Name, rowRect.Left + RowTextInset, rowRect.Top + ScenarioSelectLayout.RowNameBaselineY, _rowNamePaint);
            for (int i = 0; i < row.DescriptionLines.Count; i++)
            {
                float baseline = rowRect.Top + ScenarioSelectLayout.RowDescriptionFirstBaselineY + i * ScenarioSelectLayout.RowDescriptionLineHeight;
                canvas.DrawText(row.DescriptionLines[i], rowRect.Left + RowTextInset, baseline, _rowDescriptionPaint);
            }
            canvas.Restore();
        }
    }

    /// <summary>
    /// Stacks scenarios from <see cref="_scrollOffset"/> downward, word-wrapping each
    /// description to the list's width and stopping once the accumulated height would
    /// exceed <see cref="ScenarioSelectLayout.ListHeight"/> — always includes at least one
    /// row (even if its wrapped description alone overflows the budget) so a very long
    /// description can never hide its own row.
    /// </summary>
    private List<ScenarioRow> ComputeVisibleRows()
    {
        var rows = new List<ScenarioRow>();
        float maxTextWidth = ScenarioSelectLayout.ListWidth - 2 * RowTextInset;
        float y = ScenarioSelectLayout.ListTop;
        float used = 0f;

        for (int i = _scrollOffset; i < _scenarios.Count; i++)
        {
            var scenario = _scenarios[i];
            var lines = WrapText(scenario.Description, _rowDescriptionPaint, maxTextWidth);
            float height = ScenarioSelectLayout.RowHeightFor(lines.Count);
            float needed = rows.Count == 0 ? height : height + ScenarioSelectLayout.RowSpacing;

            if (rows.Count > 0 && used + needed > ScenarioSelectLayout.ListHeight)
                break;

            rows.Add(new ScenarioRow(i, scenario.Name, lines, y, height));
            used += needed;
            y += height + ScenarioSelectLayout.RowSpacing;
        }

        return rows;
    }

    /// <summary>Greedy word-wrap: adds words to the current line while it still fits maxWidth.</summary>
    private static List<string> WrapText(string text, SKPaint paint, float maxWidth)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text))
        {
            lines.Add(string.Empty);
            return lines;
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();

        foreach (var word in words)
        {
            string candidate = current.Length == 0 ? word : current + " " + word;
            if (current.Length == 0 || paint.MeasureText(candidate) <= maxWidth)
            {
                current.Clear();
                current.Append(candidate);
            }
            else
            {
                lines.Add(current.ToString());
                current.Clear();
                current.Append(word);
            }
        }

        if (current.Length > 0)
            lines.Add(current.ToString());

        return lines;
    }

    private static SKRect CombinedRect(float panelLeft, float panelTop, (float X, float Y, float W, float H) local) =>
        new(panelLeft + local.X, panelTop + local.Y, panelLeft + local.X + local.W, panelTop + local.Y + local.H);

    private void RefreshScenarios()
    {
        _scenarios = _listScenarios() ?? Array.Empty<ScenarioInfo>();
        _scrollOffset = Math.Clamp(_scrollOffset, 0, Math.Max(0, _scenarios.Count - 1));
        _selectedIndex = _scenarios.Count > 0 ? 0 : -1;
    }
}
