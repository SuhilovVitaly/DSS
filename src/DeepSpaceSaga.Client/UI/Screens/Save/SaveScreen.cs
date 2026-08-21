using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Save;

/// <summary>
/// Modal overlay listing save slots, with New Save / Overwrite / two-stage DELETE icon
/// actions (Images/UI/GameSaveScreen) and a CLOSE icon in the panel's top-right corner.
/// The name-entry SAVE/CANCEL row (shown after New Save is clicked) has no icon assets
/// and stays text-based. Mirrors <see cref="Settings.SettingsScreen"/>'s dim-background
/// overlay pattern.
/// <see cref="saveSlot"/>/<see cref="deleteSlot"/> are injected by the caller (SkiaWindow),
/// bound to <c>_session.SaveAsync</c>/<c>_sessionFactory.DeleteSaveSlot</c> — this class
/// has no knowledge of the session/factory types, keeping it independently testable.
/// </summary>
public sealed class SaveScreen : IScreen
{
    private const long DeleteConfirmWindowMs = 3000;

    private readonly Func<IReadOnlyList<SaveSlotInfo>> _listSlots;
    private readonly Action<string> _saveSlot;
    private readonly Action<string> _deleteSlot;
    private readonly Func<long> _nowMs;
    private readonly TextInputBox _nameInput = new();

    private IReadOnlyList<SaveSlotInfo> _slots = Array.Empty<SaveSlotInfo>();
    private int _scrollOffset;
    private bool _isNewSaveActive;
    private string? _inlineError;

    private int _deleteConfirmIndex = -1;
    private long _deleteConfirmStartedAtMs;

    private int _screenWidth;
    private int _screenHeight;

    private SaveZone _hoveredZone = SaveZone.None;
    private int _hoveredRowIndex = -1;

    private static readonly SKPaint _dimPaint = new()
    {
        Color = new SKColor(0, 0, 0, 160),
        Style = SKPaintStyle.Fill
    };

    private static readonly SKPaint _errorPaint = new()
    {
        Color = new SKColor(220, 80, 80),
        TextSize = MenuStyle.StatusFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Left,
        Typeface = MenuStyle.TypefaceRegular
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

    private static readonly SKBitmap? _closeIcon = LoadImage("Images/UI/GameSaveScreen/common.close.png");
    private static readonly SKBitmap? _closeIconActive = LoadImage("Images/UI/GameSaveScreen/common.close.active.png");
    private static readonly SKBitmap? _newSaveIcon = LoadImage("Images/UI/GameSaveScreen/save.new.png");
    private static readonly SKBitmap? _newSaveIconActive = LoadImage("Images/UI/GameSaveScreen/save.new.active.png");
    private static readonly SKBitmap? _overwriteIcon = LoadImage("Images/UI/GameSaveScreen/save.overwrite.png");
    private static readonly SKBitmap? _overwriteIconActive = LoadImage("Images/UI/GameSaveScreen/save.overwrite.active.png");
    private static readonly SKBitmap? _deleteIcon = LoadImage("Images/UI/GameSaveScreen/save.delete.png");
    private static readonly SKBitmap? _deleteIconActive = LoadImage("Images/UI/GameSaveScreen/save.delete.active.png");

    private static SKBitmap? LoadImage(string path)
    {
        try { return File.Exists(path) ? SKBitmap.Decode(path) : null; }
        catch { return null; }
    }

    /// <param name="listSlots">Enumerates every save slot on disk. Called at construction and after every mutating action.</param>
    /// <param name="saveSlot">Create/overwrite a slot by id (New Save and per-row Overwrite).</param>
    /// <param name="deleteSlot">Delete a slot by id (second click of the two-stage per-row Delete button).</param>
    /// <param name="nowMs">Clock used for the delete-confirm timeout window; defaults to <see cref="Environment.TickCount64"/>. Overridable for tests.</param>
    public SaveScreen(
        Func<IReadOnlyList<SaveSlotInfo>> listSlots,
        Action<string> saveSlot,
        Action<string> deleteSlot,
        Func<long>? nowMs = null)
    {
        _listSlots = listSlots;
        _saveSlot = saveSlot;
        _deleteSlot = deleteSlot;
        _nowMs = nowMs ?? (() => Environment.TickCount64);

        RefreshSlots();
    }

    public void OnActivated()
    {
        _isNewSaveActive = false;
        _inlineError = null;
        _nameInput.Clear();
        _deleteConfirmIndex = -1;
        _hoveredZone = SaveZone.None;
        _hoveredRowIndex = -1;
        RefreshSlots();
    }

    public void OnDeactivated() { }

    /// <summary>
    /// Called by SkiaWindow once a fire-and-forget New Save/Overwrite write for
    /// <paramref name="slotId"/> has completed, so the still-open slot list reflects it.
    /// The caller is responsible for only invoking this while this SaveScreen instance
    /// is still the active screen — see <c>SkiaWindow.SaveToSlotAsync</c>.
    /// </summary>
    public void NotifySaveCompleted(string slotId)
    {
        RefreshSlots();
    }

    public ScreenEvent OnKeyDown(Key key)
    {
        if (_isNewSaveActive)
        {
            if (key == Key.Escape)
            {
                CancelNewSave();
                return ScreenEvent.None;
            }

            if (key == Key.Backspace)
            {
                _nameInput.OnKeyDown(key);
                return ScreenEvent.None;
            }

            return ScreenEvent.None;
        }

        return key == Key.Escape ? ScreenEvent.CloseSaveWindow : ScreenEvent.None;
    }

    public void OnTextInput(char c)
    {
        if (_isNewSaveActive)
            _nameInput.OnTextInput(c);
    }

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        var hit = SaveLayout.HitTest(x, y, _screenWidth, _screenHeight, VisibleSlotCount, _isNewSaveActive);

        // Two-stage delete: any click that isn't a second click on the same confirming
        // row clears the pending confirm state back to plain DELETE.
        if (!(hit.Zone == SaveZone.Delete && AbsoluteIndex(hit.RowIndex) == _deleteConfirmIndex))
            _deleteConfirmIndex = -1;

        switch (hit.Zone)
        {
            case SaveZone.NewSave:
                _isNewSaveActive = true;
                _nameInput.Clear();
                _inlineError = null;
                return ScreenEvent.None;

            case SaveZone.ConfirmNewSave:
                ConfirmNewSave();
                return ScreenEvent.None;

            case SaveZone.CancelNewSave:
                CancelNewSave();
                return ScreenEvent.None;

            case SaveZone.Close:
                return ScreenEvent.CloseSaveWindow;

            case SaveZone.Overwrite:
            {
                int index = AbsoluteIndex(hit.RowIndex);
                if (index >= 0 && index < _slots.Count)
                {
                    _saveSlot(_slots[index].SlotId);
                    RefreshSlots();
                }
                return ScreenEvent.None;
            }

            case SaveZone.Delete:
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
        var hit = SaveLayout.HitTest(x, y, _screenWidth, _screenHeight, VisibleSlotCount, _isNewSaveActive);
        _hoveredZone = hit.Zone;
        _hoveredRowIndex = hit.RowIndex;
        return hit.Zone != SaveZone.None;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        int maxOffset = Math.Max(0, _slots.Count - SaveLayout.VisibleRows);
        _scrollOffset = Math.Clamp(_scrollOffset - Math.Sign(delta), 0, maxOffset);
        return ScreenEvent.None;
    }

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        canvas.DrawRect(0, 0, width, height, _dimPaint);

        float pl = SaveLayout.PanelLeft(width);
        float pt = SaveLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + SaveLayout.PanelWidth, pt + SaveLayout.PanelHeight);
        MenuStyle.DrawPanel(canvas, panelRect);

        float cx = pl + SaveLayout.PanelWidth / 2f;
        canvas.DrawText("SAVE GAME", cx, pt + SaveLayout.TitleY, MenuStyle.TextTitle);

        DrawNewSaveRow(canvas, pl, pt);
        DrawSlotList(canvas, pl, pt);
        DrawIconButton(canvas, CombinedRect(pl, pt, SaveLayout.CloseButtonRect()), SaveZone.Close, -1, _closeIcon, _closeIconActive);
    }

    private void DrawNewSaveRow(SKCanvas canvas, float panelLeft, float panelTop)
    {
        if (_isNewSaveActive)
        {
            var inputLocal = SaveLayout.NameInputRect();
            var inputRect = CombinedRect(panelLeft, panelTop, inputLocal);
            _nameInput.Render(canvas, inputRect);

            DrawButton(canvas, CombinedRect(panelLeft, panelTop, SaveLayout.ConfirmButtonRect()), "SAVE", SaveZone.ConfirmNewSave);
            DrawButton(canvas, CombinedRect(panelLeft, panelTop, SaveLayout.CancelButtonRect()), "CANCEL", SaveZone.CancelNewSave);

            if (_inlineError is not null)
            {
                float errorY = panelTop + SaveLayout.NewSaveRowY + SaveLayout.NewSaveRowHeight + 16f;
                canvas.DrawText(_inlineError, panelLeft + SaveLayout.Margin, errorY, _errorPaint);
            }
        }
        else
        {
            DrawIconButton(canvas, CombinedRect(panelLeft, panelTop, SaveLayout.NewSaveButtonRect()), SaveZone.NewSave, -1, _newSaveIcon, _newSaveIconActive);
        }
    }

    private void DrawSlotList(SKCanvas canvas, float panelLeft, float panelTop)
    {
        for (int i = 0; i < VisibleSlotCount; i++)
        {
            var slot = _slots[_scrollOffset + i];
            var row = SaveLayout.RowRect(i);
            var rowRect = CombinedRect(panelLeft, panelTop, row);

            canvas.DrawRect(rowRect, MenuStyle.ButtonFillNormal);
            canvas.DrawRect(rowRect, MenuStyle.ButtonBorder);

            canvas.DrawText(slot.DisplayName, rowRect.Left + 10f, rowRect.MidY - 4f, _rowTextPaint);
            canvas.DrawText(
                slot.SavedAtUtc.ToLocalTime().ToString("g"),
                rowRect.Left + 10f, rowRect.MidY + 14f, _rowDatePaint);

            DrawIconButton(canvas, CombinedRect(panelLeft, panelTop, SaveLayout.OverwriteButtonRect(i)), SaveZone.Overwrite, i, _overwriteIcon, _overwriteIconActive);

            int absoluteIndex = _scrollOffset + i;
            bool isConfirming = _deleteConfirmIndex == absoluteIndex && _nowMs() - _deleteConfirmStartedAtMs <= DeleteConfirmWindowMs;
            DrawIconButton(canvas, CombinedRect(panelLeft, panelTop, SaveLayout.DeleteButtonRect(i)), SaveZone.Delete, i,
                _deleteIcon, _deleteIconActive, forceActive: isConfirming);
        }
    }

    /// <summary>
    /// Draws a per-zone icon button: the "active" bitmap (baked-in orange border) when
    /// hovered on this exact row/zone or when <paramref name="forceActive"/> is set (the
    /// two-stage delete confirm window, which must read as active even without hover).
    /// <paramref name="rowIndex"/> is -1 for non-row zones (NewSave, Close), matching the
    /// -1 default on <see cref="SaveHit.RowIndex"/> for those zones.
    /// </summary>
    private void DrawIconButton(
        SKCanvas canvas, SKRect rect, SaveZone zone, int rowIndex, SKBitmap? normal, SKBitmap? active, bool forceActive = false)
    {
        bool isHovered = _hoveredZone == zone && _hoveredRowIndex == rowIndex;
        var bitmap = (isHovered || forceActive) ? active : normal;
        if (bitmap is not null)
            canvas.DrawBitmap(bitmap, rect);
    }

    private void DrawButton(SKCanvas canvas, SKRect rect, string text, SaveZone zone, int rowIndex = -1)
    {
        var state = _hoveredZone == zone && _hoveredRowIndex == rowIndex ? ButtonState.Hovered : ButtonState.Normal;
        MenuStyle.DrawButton(canvas, rect, text, state);
    }

    private static SKRect CombinedRect(float panelLeft, float panelTop, (float X, float Y, float W, float H) local) =>
        new(panelLeft + local.X, panelTop + local.Y, panelLeft + local.X + local.W, panelTop + local.Y + local.H);

    private void ConfirmNewSave()
    {
        string name = _nameInput.Text.Trim();

        if (name.Length == 0)
        {
            _inlineError = "Enter a name for the save.";
            return;
        }

        // Rejected unconditionally — not just when it happens to already be in the
        // current slot list — so typing "quicksave" as a brand-new name before F5 has
        // ever run is still refused (the reserved slot is also hidden from the list
        // above, so the ordinary duplicate-name check alone can't catch this case).
        if (string.Equals(name, SaveSlots.Quicksave, StringComparison.OrdinalIgnoreCase))
        {
            _inlineError = "That name is reserved for quicksave.";
            return;
        }

        if (_slots.Any(s => string.Equals(s.DisplayName, name, StringComparison.OrdinalIgnoreCase)))
        {
            _inlineError = "A save with that name already exists.";
            return;
        }

        _saveSlot(name);
        _isNewSaveActive = false;
        _nameInput.Clear();
        _inlineError = null;
        RefreshSlots();
    }

    private void CancelNewSave()
    {
        _isNewSaveActive = false;
        _nameInput.Clear();
        _inlineError = null;
    }

    private void RefreshSlots()
    {
        _slots = _listSlots() ?? Array.Empty<SaveSlotInfo>();
        int maxOffset = Math.Max(0, _slots.Count - SaveLayout.VisibleRows);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, maxOffset);
    }

    private int VisibleSlotCount => Math.Max(0, Math.Min(SaveLayout.VisibleRows, _slots.Count - _scrollOffset));

    private int AbsoluteIndex(int visibleRowIndex) => visibleRowIndex < 0 ? -1 : _scrollOffset + visibleRowIndex;
}
