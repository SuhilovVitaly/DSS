using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Save;

/// <summary>
/// Generic Type A modal overlay listing selectable save slots. CLOSE, DELETE, OVERWRITE,
/// and NEW SAVE share the bottom action row; New Save mode replaces them with
/// CLOSE/CANCEL/SAVE while the name field is shown inside the content panel.
/// <see cref="saveSlot"/>/<see cref="deleteSlot"/> are injected by the caller (SkiaWindow),
/// bound to <c>_session.SaveAsync</c>/<c>_sessionFactory.DeleteSaveSlot</c> — this class
/// has no knowledge of the session/factory types, keeping it independently testable.
/// </summary>
public sealed class SaveScreen : IScreen
{
    // Shared by the two-stage DELETE confirm and the New Save duplicate-name
    // overwrite confirm below — both require a second click within this window.
    private const long ConfirmWindowMs = 3000;

    private readonly Func<IReadOnlyList<SaveSlotInfo>> _listSlots;
    private readonly Action<string> _saveSlot;
    private readonly Action<string> _deleteSlot;
    private readonly Func<long> _nowMs;
    private readonly TextInputBox _nameInput = new();

    private IReadOnlyList<SaveSlotInfo> _slots = Array.Empty<SaveSlotInfo>();
    private int _scrollOffset;
    private int _selectedIndex = -1;
    private bool _isNewSaveActive;
    private string? _inlineError;

    private string? _pendingOverwriteName;
    private long _pendingOverwriteStartedAtMs;

    private int _deleteConfirmIndex = -1;
    private long _deleteConfirmStartedAtMs;

    private int _screenWidth;
    private int _screenHeight;

    private SaveZone _hoveredZone = SaveZone.None;
    private int _hoveredRowIndex = -1;

    private static readonly SKPaint _errorPaint = new()
    {
        Color = new SKColor(220, 80, 80),
        TextSize = MenuStyle.StatusFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Left,
        Typeface = MenuStyle.TypefaceRegular
    };

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

    private static readonly SKPaint _selectedRowIndicator = new()
    {
        Color = new SKColor(0xFF, 0x84, 0x04),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2f
    };

    private static readonly SKPaint _hoveredRowBorder = new()
    {
        Color = MenuStyle.ColorTextDim,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2f
    };

    /// <param name="listSlots">Enumerates every save slot on disk. Called at construction and after every mutating action.</param>
    /// <param name="saveSlot">Create a slot by name or overwrite the selected slot by id.</param>
    /// <param name="deleteSlot">Delete the selected slot after the second confirmation click.</param>
    /// <param name="nowMs">Clock used for the delete-confirm and duplicate-name-overwrite-confirm timeout windows; defaults to <see cref="Environment.TickCount64"/>. Overridable for tests.</param>
    public SaveScreen(
        Func<IReadOnlyList<SaveSlotInfo>> listSlots,
        Action<string> saveSlot,
        Action<string> deleteSlot,
        Func<long>? nowMs = null)
    {
        GenericWindowTypeA.Preload();
        GenericButtonTypeA.Preload();

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
        _pendingOverwriteName = null;
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

        // Two-stage delete: any click that isn't a second click for the selected row
        // clears the pending confirm state back to plain DELETE.
        if (!(hit.Zone == SaveZone.Delete && _selectedIndex == _deleteConfirmIndex))
            _deleteConfirmIndex = -1;

        switch (hit.Zone)
        {
            case SaveZone.NewSave:
                _isNewSaveActive = true;
                _nameInput.Clear();
                _inlineError = null;
                _pendingOverwriteName = null;
                return ScreenEvent.None;

            case SaveZone.ConfirmNewSave:
                ConfirmNewSave();
                return ScreenEvent.None;

            case SaveZone.CancelNewSave:
                CancelNewSave();
                return ScreenEvent.None;

            case SaveZone.Close:
                return ScreenEvent.CloseSaveWindow;

            case SaveZone.Row:
            {
                int index = AbsoluteIndex(hit.RowIndex);
                if (index >= 0 && index < _slots.Count)
                    _selectedIndex = index;
                return ScreenEvent.None;
            }

            case SaveZone.Overwrite:
            {
                if (_selectedIndex >= 0 && _selectedIndex < _slots.Count)
                {
                    _saveSlot(_slots[_selectedIndex].SlotId);
                    RefreshSlots();
                }
                return ScreenEvent.None;
            }

            case SaveZone.Delete:
            {
                if (_selectedIndex < 0 || _selectedIndex >= _slots.Count)
                    return ScreenEvent.None;

                bool isConfirmedSecondClick =
                    _deleteConfirmIndex == _selectedIndex && _nowMs() - _deleteConfirmStartedAtMs <= ConfirmWindowMs;

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

        float pl = SaveLayout.PanelLeft(width);
        float pt = SaveLayout.PanelTop(height);
        var panelRect = SaveLayout.PanelRect(width, height);
        GenericWindowTypeA.Draw(canvas, panelRect);
        GenericWindowTypeA.DrawTitle(canvas, panelRect, "SAVE GAME", _titleTextPaint);

        ImagePanel.Draw(canvas, CombinedRect(pl, pt, SaveLayout.ContentPanelRect()));

        if (_isNewSaveActive)
            DrawNewSaveEditor(canvas, pl, pt);
        else
        {
            DrawSlotList(canvas, pl, pt);
            if (_slots.Count > SaveLayout.VisibleRows)
                DrawScrollbar(canvas, pl, pt);
        }

        DrawBottomButtons(canvas, pl, pt);
    }

    /// <summary>Only rendered when the slot list overflows <see cref="SaveLayout.VisibleRows"/>; scrolling itself works via <see cref="OnMouseWheel"/> regardless.</summary>
    private void DrawScrollbar(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var track = CombinedRect(panelLeft, panelTop, SaveLayout.ScrollbarTrackRect());
        canvas.DrawRect(track, MenuStyle.ButtonFillNormal);
        canvas.DrawRect(track, MenuStyle.ButtonBorder);

        var thumb = CombinedRect(panelLeft, panelTop, SaveLayout.ScrollbarThumbRect(_scrollOffset, _slots.Count));
        canvas.DrawRect(thumb, MenuStyle.ButtonFillHover);
    }

    private void DrawNewSaveEditor(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var inputRect = CombinedRect(panelLeft, panelTop, SaveLayout.NameInputRect());
        _nameInput.Render(canvas, inputRect);

        if (_inlineError is not null)
        {
            float errorY = inputRect.Bottom + 24f;
            canvas.DrawText(_inlineError, inputRect.Left, errorY, _errorPaint);
        }
    }

    private void DrawSlotList(SKCanvas canvas, float panelLeft, float panelTop)
    {
        for (int i = 0; i < VisibleSlotCount; i++)
        {
            int absoluteIndex = _scrollOffset + i;
            var slot = _slots[absoluteIndex];
            var row = SaveLayout.RowRect(i);
            var rowRect = CombinedRect(panelLeft, panelTop, row);

            bool isSelected = absoluteIndex == _selectedIndex;
            bool isHovered = _hoveredZone == SaveZone.Row && AbsoluteIndex(_hoveredRowIndex) == absoluteIndex;

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

    private void DrawBottomButtons(SKCanvas canvas, float panelLeft, float panelTop)
    {
        if (_isNewSaveActive)
        {
            GenericButtonTypeA.Draw(canvas,
                CombinedRect(panelLeft, panelTop, SaveLayout.CloseButtonRect(isNewSaveActive: true)),
                "CLOSE", StateFor(SaveZone.Close));
            GenericButtonTypeA.Draw(canvas,
                CombinedRect(panelLeft, panelTop, SaveLayout.CancelButtonRect()),
                "CANCEL", StateFor(SaveZone.CancelNewSave));
            GenericButtonTypeA.Draw(canvas,
                CombinedRect(panelLeft, panelTop, SaveLayout.ConfirmButtonRect()),
                "SAVE", StateFor(SaveZone.ConfirmNewSave));
            return;
        }

        bool hasSelection = _selectedIndex >= 0 && _selectedIndex < _slots.Count;
        bool isConfirming = hasSelection && _deleteConfirmIndex == _selectedIndex
            && _nowMs() - _deleteConfirmStartedAtMs <= ConfirmWindowMs;

        GenericButtonTypeA.Draw(canvas,
            CombinedRect(panelLeft, panelTop, SaveLayout.CloseButtonRect()),
            "CLOSE", StateFor(SaveZone.Close));
        GenericButtonTypeA.Draw(canvas,
            CombinedRect(panelLeft, panelTop, SaveLayout.DeleteButtonRect()),
            isConfirming ? "CONFIRM?" : "DELETE",
            !hasSelection ? ButtonState.Disabled
                : isConfirming ? ButtonState.Hovered : StateFor(SaveZone.Delete));
        GenericButtonTypeA.Draw(canvas,
            CombinedRect(panelLeft, panelTop, SaveLayout.OverwriteButtonRect()),
            "OVERWRITE", hasSelection ? StateFor(SaveZone.Overwrite) : ButtonState.Disabled);
        GenericButtonTypeA.Draw(canvas,
            CombinedRect(panelLeft, panelTop, SaveLayout.NewSaveButtonRect()),
            "NEW SAVE", StateFor(SaveZone.NewSave));
    }

    private ButtonState StateFor(SaveZone zone) =>
        _hoveredZone == zone ? ButtonState.Hovered : ButtonState.Normal;

    private static SKRect CombinedRect(float panelLeft, float panelTop, (float X, float Y, float W, float H) local) =>
        new(panelLeft + local.X, panelTop + local.Y, panelLeft + local.X + local.W, panelTop + local.Y + local.H);

    private void ConfirmNewSave()
    {
        string name = _nameInput.Text.Trim();

        if (name.Length == 0)
        {
            _inlineError = "Enter a name for the save.";
            _pendingOverwriteName = null;
            return;
        }

        // Rejected unconditionally — not just when it happens to already be in the
        // current slot list — so typing "quicksave" as a brand-new name before F5 has
        // ever run is still refused (the reserved slot is also hidden from the list
        // above, so the ordinary duplicate-name check alone can't catch this case).
        if (string.Equals(name, SaveSlots.Quicksave, StringComparison.OrdinalIgnoreCase))
        {
            _inlineError = "That name is reserved for quicksave.";
            _pendingOverwriteName = null;
            return;
        }

        bool isDuplicate = _slots.Any(s => string.Equals(s.DisplayName, name, StringComparison.OrdinalIgnoreCase));
        if (isDuplicate)
        {
            // Same two-stage pattern as DELETE: the first SAVE click on a
            // duplicate name only arms the overwrite (and re-arms if the name changed
            // since the last arm, or the window lapsed) — it takes a second SAVE click
            // on that same name within ConfirmWindowMs to actually overwrite.
            bool isConfirmedOverwrite =
                string.Equals(_pendingOverwriteName, name, StringComparison.OrdinalIgnoreCase)
                && _nowMs() - _pendingOverwriteStartedAtMs <= ConfirmWindowMs;

            if (!isConfirmedOverwrite)
            {
                _pendingOverwriteName = name;
                _pendingOverwriteStartedAtMs = _nowMs();
                _inlineError = "A save with that name already exists. Click SAVE again to overwrite.";
                return;
            }
        }

        _saveSlot(name);
        _isNewSaveActive = false;
        _nameInput.Clear();
        _inlineError = null;
        _pendingOverwriteName = null;
        RefreshSlots();
    }

    private void CancelNewSave()
    {
        _isNewSaveActive = false;
        _nameInput.Clear();
        _inlineError = null;
        _pendingOverwriteName = null;
    }

    private void RefreshSlots()
    {
        _slots = _listSlots() ?? Array.Empty<SaveSlotInfo>();
        int maxOffset = Math.Max(0, _slots.Count - SaveLayout.VisibleRows);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, maxOffset);
        _selectedIndex = _slots.Count > 0 ? Math.Clamp(_selectedIndex, 0, _slots.Count - 1) : -1;
    }

    private int VisibleSlotCount => Math.Max(0, Math.Min(SaveLayout.VisibleRows, _slots.Count - _scrollOffset));

    private int AbsoluteIndex(int visibleRowIndex) => visibleRowIndex < 0 ? -1 : _scrollOffset + visibleRowIndex;
}
