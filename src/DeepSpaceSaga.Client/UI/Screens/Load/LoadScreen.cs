using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Load;

/// <summary>
/// Modal overlay listing save slots, with LOAD / two-stage DELETE actions per row.
/// Structural port of <see cref="Save.SaveScreen"/> (dim overlay, panel, scrollable row
/// list, <c>MenuStyle</c> hover states, pure <see cref="LoadLayout.HitTest"/> geometry),
/// minus the New Save/Overwrite/text-input machinery Load has no use for.
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
        DrawButton(canvas, CombinedRect(pl, pt, LoadLayout.CloseButtonRect()), "CLOSE", LoadZone.Close);
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

            DrawButton(canvas, CombinedRect(panelLeft, panelTop, LoadLayout.LoadButtonRect(i)), "LOAD", LoadZone.Load, i);

            int absoluteIndex = _scrollOffset + i;
            bool isConfirming = _deleteConfirmIndex == absoluteIndex && _nowMs() - _deleteConfirmStartedAtMs <= DeleteConfirmWindowMs;
            DrawButton(canvas, CombinedRect(panelLeft, panelTop, LoadLayout.DeleteButtonRect(i)),
                isConfirming ? "CONFIRM?" : "DELETE", LoadZone.Delete, i);
        }
    }

    private void DrawButton(SKCanvas canvas, SKRect rect, string text, LoadZone zone, int rowIndex = -1)
    {
        var state = _hoveredZone == zone && _hoveredRowIndex == rowIndex ? ButtonState.Hovered : ButtonState.Normal;
        MenuStyle.DrawButton(canvas, rect, text, state);
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
