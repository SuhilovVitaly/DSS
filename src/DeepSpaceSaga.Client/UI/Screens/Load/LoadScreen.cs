using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Load;

/// <summary>
/// Generic Type A modal overlay listing save slots. Clicking a row only selects it
/// (highlighted, does not load/delete); the bottom CLOSE / two-stage DELETE / LOAD button
/// row then acts on that selection.
/// Unlike Save (which excludes the reserved <see cref="SaveSlots.Quicksave"/> slot from
/// its list entirely), Load shows it — a player must be able to load their last
/// quicksave from here — but it is never deletable: DELETE is disabled while it is the
/// selected slot, and a Delete click against it (however triggered) is a no-op, both
/// checked directly against <see cref="SaveSlotInfo.SlotId"/> rather than relying on the
/// caller to filter it out.
/// <see cref="_deleteSlot"/> is injected by the caller (SkiaWindow), bound to
/// <c>_sessionFactory.DeleteSaveSlot</c> — this class has no knowledge of the factory type.
/// Loading itself is NOT fire-and-forget from here: a LOAD click returns
/// <see cref="ScreenEvent.LoadSlotRequested"/> and the target slot id is exposed via
/// <see cref="LastRequestedSlotId"/>, set synchronously in the same call as
/// <see cref="OnMouseDown(float, float, MouseButton)"/> returning that event — the caller
/// (SkiaWindow's mouse-dispatch site) reads it in the same synchronous frame, before any
/// await, and threads it into the locked <c>HandleScreenEvent</c> switch as a parameter
/// rather than re-reading mutable screen state later (see SkiaWindow.cs for why: a rapid
/// double-click on LOAD must never risk an invalid cast off of stale <c>_screens.Current</c>).
/// </summary>
public sealed class LoadScreen : IScreen
{
    private const long DeleteConfirmWindowMs = 3000;

    private readonly Func<IReadOnlyList<SaveSlotInfo>> _listSlots;
    private readonly Action<string> _deleteSlot;
    private readonly Func<long> _nowMs;

    private IReadOnlyList<SaveSlotInfo> _slots = Array.Empty<SaveSlotInfo>();
    private int _scrollOffset;

    /// <summary>Absolute index into <see cref="_slots"/> of the row LOAD/DELETE currently act on, or -1 if none.</summary>
    private int _selectedIndex = -1;

    private int _deleteConfirmIndex = -1;
    private long _deleteConfirmStartedAtMs;

    private int _screenWidth;
    private int _screenHeight;

    private LoadZone _hoveredZone = LoadZone.None;

    /// <summary>Absolute index of the hovered row, meaningful only when <see cref="_hoveredZone"/> is Row.</summary>
    private int _hoveredRowIndex = -1;

    /// <summary>
    /// The slot id of the most recent LOAD click that returned
    /// <see cref="ScreenEvent.LoadSlotRequested"/> from <see cref="OnMouseDown(float, float, MouseButton)"/>.
    /// Only meaningful when read synchronously, immediately after that call returns, in
    /// the same call frame as the click dispatch — see the type doc comment.
    /// </summary>
    public string? LastRequestedSlotId { get; private set; }

    private static readonly SKPaint _titleTextPaint = new()
    {
        Color = MenuStyle.ColorText,
        TextSize = MenuStyle.TitleFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = MenuStyle.TypefaceHumaroid
    };

    private static readonly SKPaint _rowTextPaint = new()
    {
        Color = MenuStyle.ColorText,
        TextSize = MenuStyle.ButtonFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Left,
        Typeface = MenuStyle.TypefaceRegular
    };

    private static readonly SKPaint _rowDatePaint = new()
    {
        Color = MenuStyle.ColorTextDim,
        TextSize = MenuStyle.StatusFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Left,
        Typeface = MenuStyle.TypefaceRegular
    };

    /// <summary>Selection is shown as a full-height accent line down the row's left edge — same
    /// convention/color as <see cref="ScenarioSelect.ScenarioSelectScreen"/>'s row selection.</summary>
    private static readonly SKPaint _selectedRowIndicator = new()
    {
        Color = new SKColor(0xFF, 0x84, 0x04),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2f
    };

    /// <summary>Hover outline for an unselected row — rows have no fill (transparent over the
    /// content panel); hover/selection is shown by outline alone.</summary>
    private static readonly SKPaint _hoveredRowBorder = new()
    {
        Color = MenuStyle.ColorTextDim,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2f
    };

    /// <param name="listSlots">Enumerates every save slot on disk. Called at construction and after every mutating action.</param>
    /// <param name="deleteSlot">Delete a slot by id (second click of the two-stage DELETE button).</param>
    /// <param name="nowMs">Clock used for the delete-confirm timeout window; defaults to <see cref="Environment.TickCount64"/>. Overridable for tests.</param>
    public LoadScreen(
        Func<IReadOnlyList<SaveSlotInfo>> listSlots,
        Action<string> deleteSlot,
        Func<long>? nowMs = null)
    {
        GenericWindowTypeA.Preload();
        GenericButtonTypeA.Preload();

        _listSlots = listSlots;
        _deleteSlot = deleteSlot;
        _nowMs = nowMs ?? (() => Environment.TickCount64);

        RefreshSlots();
    }

    public void OnActivated()
    {
        _deleteConfirmIndex = -1;
        _hoveredZone = LoadZone.None;
        _hoveredRowIndex = -1;
        RefreshSlots();
    }

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key) =>
        key == Key.Escape ? ScreenEvent.CloseLoadWindow : ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        var hit = LoadLayout.HitTest(x, y, _screenWidth, _screenHeight, VisibleSlotCount);

        // Two-stage delete: any click that isn't a second DELETE click on the still-selected
        // row that armed it clears the pending confirm state back to plain DELETE.
        if (!(hit.Zone == LoadZone.Delete && _selectedIndex == _deleteConfirmIndex))
            _deleteConfirmIndex = -1;

        switch (hit.Zone)
        {
            case LoadZone.Close:
                return ScreenEvent.CloseLoadWindow;

            case LoadZone.Row:
            {
                int index = AbsoluteIndex(hit.RowIndex);
                if (index >= 0 && index < _slots.Count)
                    _selectedIndex = index;
                return ScreenEvent.None;
            }

            case LoadZone.Load:
            {
                if (_selectedIndex < 0 || _selectedIndex >= _slots.Count)
                    return ScreenEvent.None;

                LastRequestedSlotId = _slots[_selectedIndex].SlotId;
                return ScreenEvent.LoadSlotRequested;
            }

            case LoadZone.Delete:
            {
                if (_selectedIndex < 0 || _selectedIndex >= _slots.Count)
                    return ScreenEvent.None;

                if (_slots[_selectedIndex].SlotId == SaveSlots.Quicksave)
                    return ScreenEvent.None; // protected slot — never deletable from here

                bool isConfirmedSecondClick =
                    _deleteConfirmIndex == _selectedIndex && _nowMs() - _deleteConfirmStartedAtMs <= DeleteConfirmWindowMs;

                if (isConfirmedSecondClick)
                {
                    _deleteSlot(_slots[_selectedIndex].SlotId);
                    _deleteConfirmIndex = -1;
                    RefreshSlots();
                }
                else
                {
                    _deleteConfirmIndex = _selectedIndex;
                    _deleteConfirmStartedAtMs = _nowMs();
                }
                return ScreenEvent.None;
            }

            default:
                return ScreenEvent.None;
        }
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call-site/test conventions.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        var hit = LoadLayout.HitTest(x, y, _screenWidth, _screenHeight, VisibleSlotCount);
        _hoveredZone = hit.Zone;
        _hoveredRowIndex = hit.RowIndex;
        return hit.Zone != LoadZone.None;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        int maxOffset = Math.Max(0, _slots.Count - LoadLayout.VisibleRows);
        _scrollOffset = Math.Clamp(_scrollOffset - Math.Sign(delta), 0, maxOffset);
        return ScreenEvent.None;
    }

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        float pl = LoadLayout.PanelLeft(width);
        float pt = LoadLayout.PanelTop(height);
        var panelRect = LoadLayout.PanelRect(width, height);
        GenericWindowTypeA.Draw(canvas, panelRect);
        GenericWindowTypeA.DrawTitle(canvas, panelRect, "LOAD GAME", _titleTextPaint);

        ImagePanel.Draw(canvas, CombinedRect(pl, pt, LoadLayout.ContentPanelRect()));

        DrawSlotList(canvas, pl, pt);
        if (_slots.Count > LoadLayout.VisibleRows)
            DrawScrollbar(canvas, pl, pt);

        DrawBottomButtons(canvas, pl, pt);
    }

    /// <summary>Only rendered when the slot list overflows <see cref="LoadLayout.VisibleRows"/>; scrolling itself works via <see cref="OnMouseWheel"/> regardless.</summary>
    private void DrawScrollbar(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var track = CombinedRect(panelLeft, panelTop, LoadLayout.ScrollbarTrackRect());
        canvas.DrawRect(track, MenuStyle.ButtonFillNormal);
        canvas.DrawRect(track, MenuStyle.ButtonBorder);

        var thumb = CombinedRect(panelLeft, panelTop, LoadLayout.ScrollbarThumbRect(_scrollOffset, _slots.Count));
        canvas.DrawRect(thumb, MenuStyle.ButtonFillHover);
    }

    private void DrawSlotList(SKCanvas canvas, float panelLeft, float panelTop)
    {
        for (int i = 0; i < VisibleSlotCount; i++)
        {
            int absoluteIndex = _scrollOffset + i;
            var slot = _slots[absoluteIndex];
            var rowRect = CombinedRect(panelLeft, panelTop, LoadLayout.RowRect(i));

            // Rows have no fill — fully transparent over the content panel. The selected
            // row gets a full-height accent line down its left edge (always, even while
            // hovered); any other row gets a dim outline on hover; otherwise nothing.
            bool isSelected = absoluteIndex == _selectedIndex;
            bool isHovered = _hoveredZone == LoadZone.Row && AbsoluteIndex(_hoveredRowIndex) == absoluteIndex;

            if (isSelected)
                canvas.DrawLine(rowRect.Left, rowRect.Top, rowRect.Left, rowRect.Bottom, _selectedRowIndicator);
            else if (isHovered)
                canvas.DrawRect(rowRect, _hoveredRowBorder);

            canvas.Save();
            canvas.ClipRect(rowRect);
            canvas.DrawText(slot.DisplayName, rowRect.Left + 10f, rowRect.MidY - 4f, _rowTextPaint);
            canvas.DrawText(
                slot.SavedAtUtc.ToLocalTime().ToString("g"),
                rowRect.Left + 10f, rowRect.MidY + 14f, _rowDatePaint);
            canvas.Restore();
        }
    }

    private void DrawBottomButtons(SKCanvas canvas, float pl, float pt)
    {
        bool hasSelection = _selectedIndex >= 0 && _selectedIndex < _slots.Count;
        bool isQuicksaveSelected = hasSelection && _slots[_selectedIndex].SlotId == SaveSlots.Quicksave;
        bool isConfirming = hasSelection && _deleteConfirmIndex == _selectedIndex
            && _nowMs() - _deleteConfirmStartedAtMs <= DeleteConfirmWindowMs;

        var deleteRect = CombinedRect(pl, pt, LoadLayout.DeleteButtonRect());
        var deleteState = !hasSelection || isQuicksaveSelected
            ? ButtonState.Disabled
            : _hoveredZone == LoadZone.Delete ? ButtonState.Hovered : ButtonState.Normal;
        GenericButtonTypeA.Draw(canvas, deleteRect, isConfirming ? "CONFIRM?" : "DELETE", deleteState);

        var loadRect = CombinedRect(pl, pt, LoadLayout.LoadButtonRect());
        var loadState = !hasSelection
            ? ButtonState.Disabled
            : _hoveredZone == LoadZone.Load ? ButtonState.Hovered : ButtonState.Normal;
        GenericButtonTypeA.Draw(canvas, loadRect, "LOAD", loadState);

        var closeRect = CombinedRect(pl, pt, LoadLayout.CloseButtonRect());
        var closeState = _hoveredZone == LoadZone.Close ? ButtonState.Hovered : ButtonState.Normal;
        GenericButtonTypeA.Draw(canvas, closeRect, "CLOSE", closeState);
    }

    private static SKRect CombinedRect(float panelLeft, float panelTop, (float X, float Y, float W, float H) local) =>
        new(panelLeft + local.X, panelTop + local.Y, panelLeft + local.X + local.W, panelTop + local.Y + local.H);

    /// <summary>Reloads the slot list and defaults the selection to the first slot — mirrors
    /// <see cref="ScenarioSelect.ScenarioSelectScreen"/>'s RefreshScenarios.</summary>
    private void RefreshSlots()
    {
        _slots = _listSlots() ?? Array.Empty<SaveSlotInfo>();
        int maxOffset = Math.Max(0, _slots.Count - LoadLayout.VisibleRows);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, maxOffset);
        _selectedIndex = _slots.Count > 0 ? 0 : -1;
    }

    private int VisibleSlotCount => Math.Max(0, Math.Min(LoadLayout.VisibleRows, _slots.Count - _scrollOffset));

    private int AbsoluteIndex(int visibleRowIndex) => visibleRowIndex < 0 ? -1 : _scrollOffset + visibleRowIndex;
}
