using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Load;

/// <summary>
/// Modal overlay listing save slots, with LOAD / two-stage DELETE icon actions per row
/// (Images/UI/GameLoadScreen) and a CLOSE icon in the panel's top-right corner.
/// Structural port of <see cref="Save.SaveScreen"/> (dim overlay, panel, scrollable row
/// list, pure <see cref="LoadLayout.HitTest"/> geometry), minus the New Save/Overwrite/
/// text-input machinery Load has no use for.
/// Unlike Save (which excludes the reserved <see cref="SaveSlots.Quicksave"/> slot from
/// its list entirely), Load shows it — a player must be able to load their last
/// quicksave from here — but it is never deletable: its row renders with no DELETE
/// button and a Delete click on it is a no-op, both checked directly against
/// <see cref="SaveSlotInfo.SlotId"/> rather than relying on the caller to filter it out.
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

    private int _deleteConfirmIndex = -1;
    private long _deleteConfirmStartedAtMs;

    private int _screenWidth;
    private int _screenHeight;

    private LoadZone _hoveredZone = LoadZone.None;
    private int _hoveredRowIndex = -1;

    /// <summary>
    /// The slot id of the most recent LOAD click that returned
    /// <see cref="ScreenEvent.LoadSlotRequested"/> from <see cref="OnMouseDown(float, float, MouseButton)"/>.
    /// Only meaningful when read synchronously, immediately after that call returns, in
    /// the same call frame as the click dispatch — see the type doc comment.
    /// </summary>
    public string? LastRequestedSlotId { get; private set; }

    private static readonly SKPaint _dimPaint = new()
    {
        Color = new SKColor(0, 0, 0, 160),
        Style = SKPaintStyle.Fill
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

    private static readonly SKPaint _buttonTextPaint = new()
    {
        Color = MenuStyle.ColorText,
        TextSize = MenuStyle.ButtonFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Left,
        Typeface = MenuStyle.TypefaceBold
    };

    private const float ButtonIconPadding = 6f;

    private static readonly SKBitmap? _closeIcon = LoadImage("Images/UI/GameLoadScreen/common.close.png");
    private static readonly SKBitmap? _closeIconActive = LoadImage("Images/UI/GameLoadScreen/common.close.active.png");
    private static readonly SKBitmap? _loadIcon = LoadImage("Images/UI/GameLoadScreen/load.load.png");
    private static readonly SKBitmap? _loadIconActive = LoadImage("Images/UI/GameLoadScreen/load.load.active.png");
    private static readonly SKBitmap? _deleteIcon = LoadImage("Images/UI/GameLoadScreen/load.delete.png");
    private static readonly SKBitmap? _deleteIconActive = LoadImage("Images/UI/GameLoadScreen/load.delete.active.png");

    private static SKBitmap? LoadImage(string path)
    {
        try { return File.Exists(path) ? SKBitmap.Decode(path) : null; }
        catch { return null; }
    }

    /// <param name="listSlots">Enumerates every save slot on disk. Called at construction and after every mutating action.</param>
    /// <param name="deleteSlot">Delete a slot by id (second click of the two-stage per-row Delete button).</param>
    /// <param name="nowMs">Clock used for the delete-confirm timeout window; defaults to <see cref="Environment.TickCount64"/>. Overridable for tests.</param>
    public LoadScreen(
        Func<IReadOnlyList<SaveSlotInfo>> listSlots,
        Action<string> deleteSlot,
        Func<long>? nowMs = null)
    {
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

        // Two-stage delete: any click that isn't a second click on the same confirming
        // row clears the pending confirm state back to plain DELETE.
        if (!(hit.Zone == LoadZone.Delete && AbsoluteIndex(hit.RowIndex) == _deleteConfirmIndex))
            _deleteConfirmIndex = -1;

        switch (hit.Zone)
        {
            case LoadZone.Close:
                return ScreenEvent.CloseLoadWindow;

            case LoadZone.Load:
            {
                int index = AbsoluteIndex(hit.RowIndex);
                if (index < 0 || index >= _slots.Count)
                    return ScreenEvent.None;

                LastRequestedSlotId = _slots[index].SlotId;
                return ScreenEvent.LoadSlotRequested;
            }

            case LoadZone.Delete:
            {
                int index = AbsoluteIndex(hit.RowIndex);
                if (index < 0 || index >= _slots.Count)
                    return ScreenEvent.None;

                if (_slots[index].SlotId == SaveSlots.Quicksave)
                    return ScreenEvent.None; // protected slot — never deletable from here

                bool isConfirmedSecondClick =
                    _deleteConfirmIndex == index && _nowMs() - _deleteConfirmStartedAtMs <= DeleteConfirmWindowMs;

                if (isConfirmedSecondClick)
                {
                    _deleteSlot(_slots[index].SlotId);
                    _deleteConfirmIndex = -1;
                    RefreshSlots();
                }
                else
                {
                    _deleteConfirmIndex = index;
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

        canvas.DrawRect(0, 0, width, height, _dimPaint);

        float pl = LoadLayout.PanelLeft(width);
        float pt = LoadLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + LoadLayout.PanelWidth, pt + LoadLayout.PanelHeight);
        MenuStyle.DrawPanel(canvas, panelRect);

        float cx = pl + LoadLayout.PanelWidth / 2f;
        canvas.DrawText("LOAD GAME", cx, pt + LoadLayout.TitleY, MenuStyle.TextTitle);

        DrawSlotList(canvas, pl, pt);
        if (_slots.Count > LoadLayout.VisibleRows)
            DrawScrollbar(canvas, pl, pt);
        DrawIconTextButton(canvas, CombinedRect(pl, pt, LoadLayout.CloseButtonRect()), "CLOSE", LoadZone.Close, -1, _closeIcon, _closeIconActive);
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
            var slot = _slots[_scrollOffset + i];
            var row = LoadLayout.RowRect(i);
            var rowRect = CombinedRect(panelLeft, panelTop, row);

            canvas.DrawRect(rowRect, MenuStyle.ButtonFillNormal);
            canvas.DrawRect(rowRect, MenuStyle.ButtonBorder);

            canvas.DrawText(slot.DisplayName, rowRect.Left + 10f, rowRect.MidY - 4f, _rowTextPaint);
            canvas.DrawText(
                slot.SavedAtUtc.ToLocalTime().ToString("g"),
                rowRect.Left + 10f, rowRect.MidY + 14f, _rowDatePaint);

            DrawIconTextButton(canvas, CombinedRect(panelLeft, panelTop, LoadLayout.LoadButtonRect(i)), "LOAD", LoadZone.Load, i, _loadIcon, _loadIconActive);

            if (slot.SlotId != SaveSlots.Quicksave)
            {
                int absoluteIndex = _scrollOffset + i;
                bool isConfirming = _deleteConfirmIndex == absoluteIndex && _nowMs() - _deleteConfirmStartedAtMs <= DeleteConfirmWindowMs;
                DrawIconTextButton(canvas, CombinedRect(panelLeft, panelTop, LoadLayout.DeleteButtonRect(i)),
                    isConfirming ? "CONFIRM?" : "DELETE", LoadZone.Delete, i, _deleteIcon, _deleteIconActive, forceActive: isConfirming);
            }
        }
    }

    /// <summary>
    /// Draws a button combining the previous fill/border/label chrome with an
    /// <c>Images/UI/GameLoadScreen</c> icon to the left of the label: fill+border swap on
    /// hover (as before), and the label's icon swaps to its "active" bitmap (baked-in
    /// orange border) on that same hover or when <paramref name="forceActive"/> is set
    /// (the two-stage delete confirm window, which must read as active even without
    /// hover). The icon is pinned to the button's left edge (fixed-width buttons, so a
    /// centered icon+label group would drift depending on label length); the label
    /// follows immediately after it.
    /// <paramref name="rowIndex"/> is -1 for non-row zones (Close), matching the -1
    /// default on <see cref="LoadHit.RowIndex"/> for those zones.
    /// </summary>
    private void DrawIconTextButton(
        SKCanvas canvas, SKRect rect, string text, LoadZone zone, int rowIndex,
        SKBitmap? iconNormal, SKBitmap? iconActive, bool forceActive = false)
    {
        bool isHovered = _hoveredZone == zone && _hoveredRowIndex == rowIndex;

        canvas.DrawRect(rect, isHovered ? MenuStyle.ButtonFillHover : MenuStyle.ButtonFillNormal);
        canvas.DrawRect(rect, MenuStyle.ButtonBorder);

        var icon = (isHovered || forceActive) ? iconActive : iconNormal;
        float iconSize = icon is not null ? rect.Height - ButtonIconPadding * 2f : 0f;
        float gap = icon is not null ? ButtonIconPadding : 0f;
        float textX = rect.Left + ButtonIconPadding + iconSize + gap;

        if (icon is not null)
        {
            var iconRect = new SKRect(rect.Left + ButtonIconPadding, rect.MidY - iconSize / 2f,
                rect.Left + ButtonIconPadding + iconSize, rect.MidY + iconSize / 2f);
            canvas.DrawBitmap(icon, iconRect);
        }

        float textY = rect.MidY + _buttonTextPaint.TextSize / 3f;
        canvas.DrawText(text, textX, textY, _buttonTextPaint);
    }

    private static SKRect CombinedRect(float panelLeft, float panelTop, (float X, float Y, float W, float H) local) =>
        new(panelLeft + local.X, panelTop + local.Y, panelLeft + local.X + local.W, panelTop + local.Y + local.H);

    private void RefreshSlots()
    {
        _slots = _listSlots() ?? Array.Empty<SaveSlotInfo>();
        int maxOffset = Math.Max(0, _slots.Count - LoadLayout.VisibleRows);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, maxOffset);
    }

    private int VisibleSlotCount => Math.Max(0, Math.Min(LoadLayout.VisibleRows, _slots.Count - _scrollOffset));

    private int AbsoluteIndex(int visibleRowIndex) => visibleRowIndex < 0 ? -1 : _scrollOffset + visibleRowIndex;
}
