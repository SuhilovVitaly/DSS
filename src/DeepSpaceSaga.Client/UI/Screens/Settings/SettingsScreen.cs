using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Settings;

public sealed class SettingsScreen : IScreen
{
    private static readonly double[] UiScaleValues = { 0.8, 1.0, 1.2, 1.5 };
    private static readonly string[] LanguageValues = { "English", "Russian" };

    private readonly IReadOnlyList<string> _monitorNames;
    private readonly Action<int> _onMonitorSelected;
    private readonly Action<double> _onUiScaleSelected;
    private readonly Action<string> _onLanguageSelected;

    private int _screenWidth;
    private int _screenHeight;

    private int _selectedMonitorIndex;
    private bool _isMonitorComboOpen;
    private int _hoveredMonitorOption = -1;

    private int _selectedUiScaleIndex;
    private bool _isUiScaleComboOpen;
    private int _hoveredUiScaleOption = -1;

    private int _selectedLanguageIndex;
    private bool _isLanguageComboOpen;
    private int _hoveredLanguageOption = -1;

    private SettingsButton _hoveredButton = SettingsButton.None;
    private SettingsButton _pressedButton = SettingsButton.None;

    /// <summary>
    /// Panel background at the exact 500×550 panel size — same texture and loading
    /// pattern as <see cref="MainMenu.MainMenuScreen"/> (this panel is the same size and
    /// position as MainMenu's, see <see cref="SettingsLayout"/>'s doc comment). Loaded
    /// once and shared by every SettingsScreen instance; falls back to
    /// MenuStyle.DrawPanel's plain fill if the file is missing.
    /// </summary>
    private static readonly SKBitmap? BackgroundImage =
        LoadImage("Images/UI/window-background-500x550.png");

    private static SKBitmap? LoadImage(string path)
    {
        try { return File.Exists(path) ? SKBitmap.Decode(path) : null; }
        catch { return null; }
    }

    /// <summary>True if the background PNG file was found and decoded at startup.</summary>
    internal static bool HasLoadedBackground => BackgroundImage is not null;

    private static readonly SKPaint _arrowPaint = new()
    {
        Color = MenuStyle.ColorText,
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    /// <summary>Left-aligned row label — MenuStyle's text paints are all center-aligned,
    /// which doesn't fit the label-left/value-right row layout below.</summary>
    private static readonly SKPaint _labelPaint = new()
    {
        Color = MenuStyle.ColorText,
        TextSize = MenuStyle.StatusFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Left,
        Typeface = MenuStyle.TypefaceHumaroid
    };

    /// <summary>Same size/color/alignment as <see cref="MenuStyle.TextTitle"/>, but in
    /// Humaroid — a local copy rather than mutating the shared paint, which other screens
    /// (GameMenu, Trade, Station, ...) also draw their own titles with.</summary>
    private static readonly SKPaint _titleTextPaint = new()
    {
        Color = MenuStyle.ColorText,
        TextSize = MenuStyle.TitleFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = MenuStyle.TypefaceHumaroid
    };

    /// <summary>Same size/color/alignment as <see cref="MenuStyle.TextStatus"/>, but in Humaroid.</summary>
    private static readonly SKPaint _noteTextPaint = new()
    {
        Color = MenuStyle.ColorText,
        TextSize = MenuStyle.StatusFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = MenuStyle.TypefaceHumaroid
    };

    /// <summary>
    /// Open dropdown lists are flat dark-gray panels, deliberately not drawn with
    /// <see cref="ImageButton"/>'s nine-sliced button texture — a plain fill reads as a
    /// list, not as a stack of buttons.
    /// </summary>
    private static readonly SKPaint _dropdownBackgroundFill = new() { Color = new SKColor(0x2D, 0x2D, 0x2D), Style = SKPaintStyle.Fill };
    private static readonly SKPaint _dropdownBorder = new() { Color = new SKColor(0x50, 0x50, 0x50), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
    private static readonly SKPaint _dropdownHoverFill = new() { Color = new SKColor(0x45, 0x45, 0x45), Style = SKPaintStyle.Fill };
    private static readonly SKPaint _dropdownSelectedFill = new() { Color = new SKColor(0x58, 0x58, 0x58), Style = SKPaintStyle.Fill };

    private static readonly SKPaint _dropdownOptionText = new()
    {
        Color = MenuStyle.ColorText,
        TextSize = MenuStyle.ButtonFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = MenuStyle.TypefaceHumaroid
    };

    /// <summary>Draws the shared dark-gray background + border spanning every option row of an open dropdown.</summary>
    private static void DrawDropdownListBackground(SKCanvas canvas, SKRect listRect)
    {
        canvas.DrawRect(listRect, _dropdownBackgroundFill);
        canvas.DrawRect(listRect, _dropdownBorder);
    }

    /// <summary>Draws one dropdown row's text over the shared list background, with a flat highlight fill (no button texture) when selected or hovered.</summary>
    private static void DrawDropdownOption(SKCanvas canvas, SKRect rect, string text, bool isSelected, bool isHovered)
    {
        if (isSelected)
            canvas.DrawRect(rect, _dropdownSelectedFill);
        else if (isHovered)
            canvas.DrawRect(rect, _dropdownHoverFill);

        canvas.DrawText(text, rect.MidX, MenuStyle.VerticalCenterBaseline(rect, _dropdownOptionText), _dropdownOptionText);
    }

    /// <summary>
    /// Draws a combo box's closed toggle in the same flat dark-gray style as the dropdown
    /// list it opens (see <see cref="DrawDropdownListBackground"/>/<see cref="DrawDropdownOption"/>)
    /// — deliberately not <see cref="ImageButton"/>'s button texture, so the box and the
    /// list it expands into read as one control, not a button.
    /// </summary>
    private static void DrawComboBox(SKCanvas canvas, SKRect rect, string text, bool isHighlighted)
    {
        canvas.DrawRect(rect, isHighlighted ? _dropdownHoverFill : _dropdownBackgroundFill);
        canvas.DrawRect(rect, _dropdownBorder);
        canvas.DrawText(text, rect.MidX, MenuStyle.VerticalCenterBaseline(rect, _dropdownOptionText), _dropdownOptionText);
    }

    public SettingsScreen(
        IReadOnlyList<string> monitorNames, int selectedMonitorIndex, Action<int> onMonitorSelected,
        double selectedUiScale, Action<double> onUiScaleSelected,
        string selectedLanguage, Action<string> onLanguageSelected)
    {
        _monitorNames = monitorNames.Count > 0 ? monitorNames : new[] { "Monitor 1" };
        _selectedMonitorIndex = Math.Clamp(selectedMonitorIndex, 0, _monitorNames.Count - 1);
        _onMonitorSelected = onMonitorSelected;

        int scaleIndex = Array.FindIndex(UiScaleValues, v => Math.Abs(v - selectedUiScale) < 0.001);
        _selectedUiScaleIndex = scaleIndex >= 0 ? scaleIndex : Array.IndexOf(UiScaleValues, 1.0);
        _onUiScaleSelected = onUiScaleSelected;

        int languageIndex = Array.IndexOf(LanguageValues, selectedLanguage);
        _selectedLanguageIndex = languageIndex >= 0 ? languageIndex : 0;
        _onLanguageSelected = onLanguageSelected;
    }

    public void OnActivated()
    {
        _hoveredButton = SettingsButton.None;
        _pressedButton = SettingsButton.None;
        _isMonitorComboOpen = false;
        _hoveredMonitorOption = -1;
        _isUiScaleComboOpen = false;
        _hoveredUiScaleOption = -1;
        _isLanguageComboOpen = false;
        _hoveredLanguageOption = -1;
    }

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key)
    {
        if (key != Key.Escape)
            return ScreenEvent.None;

        if (_isMonitorComboOpen)
        {
            _isMonitorComboOpen = false;
            return ScreenEvent.None;
        }

        if (_isUiScaleComboOpen)
        {
            _isUiScaleComboOpen = false;
            return ScreenEvent.None;
        }

        if (_isLanguageComboOpen)
        {
            _isLanguageComboOpen = false;
            return ScreenEvent.None;
        }

        return ScreenEvent.CloseSettings;
    }

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        if (_isMonitorComboOpen)
        {
            int option = SettingsLayout.HitTestMonitorOption(x, y, _screenWidth, _screenHeight, _monitorNames.Count);
            _isMonitorComboOpen = false;

            if (option >= 0 && option != _selectedMonitorIndex)
            {
                _selectedMonitorIndex = option;
                _onMonitorSelected(option);
            }

            return ScreenEvent.None;
        }

        if (_isUiScaleComboOpen)
        {
            int option = SettingsLayout.HitTestUiScaleOption(
                x, y, _screenWidth, _screenHeight, UiScaleValues.Length);
            _isUiScaleComboOpen = false;

            if (option >= 0 && option != _selectedUiScaleIndex)
            {
                _selectedUiScaleIndex = option;
                _onUiScaleSelected(UiScaleValues[option]);
            }

            return ScreenEvent.None;
        }

        if (_isLanguageComboOpen)
        {
            int option = SettingsLayout.HitTestLanguageOption(
                x, y, _screenWidth, _screenHeight, LanguageValues.Length);
            _isLanguageComboOpen = false;

            if (option >= 0 && option != _selectedLanguageIndex)
            {
                _selectedLanguageIndex = option;
                _onLanguageSelected(LanguageValues[option]);
            }

            return ScreenEvent.None;
        }

        var hit = SettingsLayout.HitTest(x, y, _screenWidth, _screenHeight);

        if (hit == SettingsButton.MonitorCombo)
        {
            _isMonitorComboOpen = true;
            return ScreenEvent.None;
        }

        if (hit == SettingsButton.UiScaleCombo)
        {
            _isUiScaleComboOpen = true;
            return ScreenEvent.None;
        }

        if (hit == SettingsButton.LanguageCombo)
        {
            _isLanguageComboOpen = true;
            return ScreenEvent.None;
        }

        if (hit == SettingsButton.None)
            return ScreenEvent.None;

        _pressedButton = hit;

        return hit == SettingsButton.Exit ? ScreenEvent.CloseSettings : ScreenEvent.None;
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call sites/tests.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        if (_isMonitorComboOpen)
        {
            _hoveredMonitorOption = SettingsLayout.HitTestMonitorOption(x, y, _screenWidth, _screenHeight, _monitorNames.Count);
            return true;
        }

        if (_isUiScaleComboOpen)
        {
            _hoveredUiScaleOption = SettingsLayout.HitTestUiScaleOption(
                x, y, _screenWidth, _screenHeight, UiScaleValues.Length);
            return true;
        }

        if (_isLanguageComboOpen)
        {
            _hoveredLanguageOption = SettingsLayout.HitTestLanguageOption(
                x, y, _screenWidth, _screenHeight, LanguageValues.Length);
            return true;
        }

        var hit = SettingsLayout.HitTest(x, y, _screenWidth, _screenHeight);
        _hoveredButton = hit;
        return hit != SettingsButton.None;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;
        _pressedButton = SettingsButton.None;

        float pl = SettingsLayout.PanelLeft(width);
        float pt = SettingsLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + SettingsLayout.PanelWidth, pt + SettingsLayout.PanelHeight);
        if (BackgroundImage is not null)
            canvas.DrawBitmap(BackgroundImage, panelRect);
        else
            MenuStyle.DrawPanel(canvas, panelRect);

        float cx = pl + SettingsLayout.PanelWidth / 2f;

        canvas.DrawText(Localization.Get("Settings.Title"), cx, pt + SettingsLayout.TitleY, _titleTextPaint);

        DrawMonitorRow(canvas, pl, pt, cx);
        DrawLanguageRow(canvas, pl, pt);
        DrawUiScaleRow(canvas, pl, pt);
        DrawButton(canvas, pl, pt, SettingsLayout.ExitY, Localization.Get("Settings.Exit"), SettingsButton.Exit);

        // An open dropdown's option list is drawn last, as a final overlay pass, so it is
        // never covered by a row positioned below it (at most one combo is ever open at a
        // time — opening one always closes whichever other one was open, see OnMouseDown).
        if (_isMonitorComboOpen)
            DrawMonitorOptions(canvas, pl, pt);
        else if (_isUiScaleComboOpen)
            DrawUiScaleOptions(canvas, pl, pt);
        else if (_isLanguageComboOpen)
            DrawLanguageOptions(canvas, pl, pt);
    }

    private void DrawLanguageRow(SKCanvas canvas, float panelLeft, float panelTop)
    {
        float bx = panelLeft + SettingsLayout.RowRightX - SettingsLayout.LanguageComboWidth;
        float by = panelTop + SettingsLayout.LanguageRowY;
        var boxRect = new SKRect(bx, by, bx + SettingsLayout.LanguageComboWidth, by + SettingsLayout.LanguageComboHeight);

        canvas.DrawText(Localization.Get("Settings.Language"), panelLeft + SettingsLayout.RowLabelX,
            MenuStyle.VerticalCenterBaseline(boxRect, _labelPaint), _labelPaint);

        bool boxHighlighted = _isLanguageComboOpen || _hoveredButton == SettingsButton.LanguageCombo;
        DrawComboBox(canvas, boxRect, LanguageValues[_selectedLanguageIndex], boxHighlighted);
        DrawDropdownArrow(canvas, boxRect, _isLanguageComboOpen);
    }

    private void DrawLanguageOptions(SKCanvas canvas, float panelLeft, float panelTop)
    {
        float bx = panelLeft + SettingsLayout.RowRightX - SettingsLayout.LanguageComboWidth;
        float by = panelTop + SettingsLayout.LanguageRowY + SettingsLayout.LanguageComboHeight;
        float listHeight = LanguageValues.Length * SettingsLayout.LanguageOptionHeight;

        DrawDropdownListBackground(canvas, new SKRect(bx, by, bx + SettingsLayout.LanguageComboWidth, by + listHeight));

        for (int i = 0; i < LanguageValues.Length; i++)
        {
            float oy = by + i * SettingsLayout.LanguageOptionHeight;
            var optionRect = new SKRect(bx, oy, bx + SettingsLayout.LanguageComboWidth, oy + SettingsLayout.LanguageOptionHeight);

            DrawDropdownOption(canvas, optionRect, LanguageValues[i],
                isSelected: i == _selectedLanguageIndex, isHovered: i == _hoveredLanguageOption);
        }
    }

    private void DrawUiScaleRow(SKCanvas canvas, float panelLeft, float panelTop)
    {
        float bx = panelLeft + SettingsLayout.RowRightX - SettingsLayout.UiScaleComboWidth;
        float by = panelTop + SettingsLayout.UiScaleRowY;
        var boxRect = new SKRect(bx, by, bx + SettingsLayout.UiScaleComboWidth, by + SettingsLayout.UiScaleComboHeight);

        canvas.DrawText(Localization.Get("Settings.InterfaceScale"), panelLeft + SettingsLayout.RowLabelX,
            MenuStyle.VerticalCenterBaseline(boxRect, _labelPaint), _labelPaint);

        bool boxHighlighted = _isUiScaleComboOpen || _hoveredButton == SettingsButton.UiScaleCombo;
        DrawComboBox(canvas, boxRect, FormatScaleLabel(UiScaleValues[_selectedUiScaleIndex]), boxHighlighted);
        DrawDropdownArrow(canvas, boxRect, _isUiScaleComboOpen);
    }

    private void DrawUiScaleOptions(SKCanvas canvas, float panelLeft, float panelTop)
    {
        float bx = panelLeft + SettingsLayout.RowRightX - SettingsLayout.UiScaleComboWidth;
        float by = panelTop + SettingsLayout.UiScaleRowY + SettingsLayout.UiScaleComboHeight;
        float listHeight = UiScaleValues.Length * SettingsLayout.UiScaleOptionHeight;

        DrawDropdownListBackground(canvas, new SKRect(bx, by, bx + SettingsLayout.UiScaleComboWidth, by + listHeight));

        for (int i = 0; i < UiScaleValues.Length; i++)
        {
            float oy = by + i * SettingsLayout.UiScaleOptionHeight;
            var optionRect = new SKRect(bx, oy, bx + SettingsLayout.UiScaleComboWidth, oy + SettingsLayout.UiScaleOptionHeight);

            DrawDropdownOption(canvas, optionRect, FormatScaleLabel(UiScaleValues[i]),
                isSelected: i == _selectedUiScaleIndex, isHovered: i == _hoveredUiScaleOption);
        }
    }

    private static string FormatScaleLabel(double scale) => $"{(int)Math.Round(scale * 100)}%";

    private void DrawMonitorRow(SKCanvas canvas, float panelLeft, float panelTop, float centerX)
    {
        float bx = panelLeft + SettingsLayout.RowRightX - SettingsLayout.MonitorComboWidth;
        float by = panelTop + SettingsLayout.MonitorRowY;
        var boxRect = new SKRect(bx, by, bx + SettingsLayout.MonitorComboWidth, by + SettingsLayout.MonitorComboHeight);

        canvas.DrawText(Localization.Get("Settings.Monitor"), panelLeft + SettingsLayout.RowLabelX,
            MenuStyle.VerticalCenterBaseline(boxRect, _labelPaint), _labelPaint);

        bool boxHighlighted = _isMonitorComboOpen || _hoveredButton == SettingsButton.MonitorCombo;
        DrawComboBox(canvas, boxRect, _monitorNames[_selectedMonitorIndex], boxHighlighted);
        DrawDropdownArrow(canvas, boxRect, _isMonitorComboOpen);

        if (!_isMonitorComboOpen)
            canvas.DrawText(Localization.Get("Settings.RestartNote"), centerX, panelTop + SettingsLayout.MonitorNoteY, _noteTextPaint);
    }

    private void DrawMonitorOptions(SKCanvas canvas, float panelLeft, float panelTop)
    {
        float bx = panelLeft + SettingsLayout.RowRightX - SettingsLayout.MonitorComboWidth;
        float by = panelTop + SettingsLayout.MonitorRowY + SettingsLayout.MonitorComboHeight;
        float listHeight = _monitorNames.Count * SettingsLayout.MonitorOptionHeight;

        DrawDropdownListBackground(canvas, new SKRect(bx, by, bx + SettingsLayout.MonitorComboWidth, by + listHeight));

        for (int i = 0; i < _monitorNames.Count; i++)
        {
            float oy = by + i * SettingsLayout.MonitorOptionHeight;
            var optionRect = new SKRect(bx, oy, bx + SettingsLayout.MonitorComboWidth, oy + SettingsLayout.MonitorOptionHeight);

            DrawDropdownOption(canvas, optionRect, _monitorNames[i],
                isSelected: i == _selectedMonitorIndex, isHovered: i == _hoveredMonitorOption);
        }
    }

    /// <summary>
    /// Draws the dropdown chevron as a filled vector triangle rather than a Unicode
    /// arrow glyph — Verdana (this UI's font) has no glyph for ▼/▲, so text-based
    /// arrows render as nothing.
    /// </summary>
    private static void DrawDropdownArrow(SKCanvas canvas, SKRect boxRect, bool pointingUp)
    {
        const float halfWidth = 5f;
        const float height = 5f;
        float cx = boxRect.Right - 18f;
        float cy = boxRect.MidY;

        using var path = new SKPath();
        if (pointingUp)
        {
            path.MoveTo(cx - halfWidth, cy + height / 2f);
            path.LineTo(cx + halfWidth, cy + height / 2f);
            path.LineTo(cx, cy - height / 2f);
        }
        else
        {
            path.MoveTo(cx - halfWidth, cy - height / 2f);
            path.LineTo(cx + halfWidth, cy - height / 2f);
            path.LineTo(cx, cy + height / 2f);
        }
        path.Close();

        canvas.DrawPath(path, _arrowPaint);
    }

    private ButtonState GetState(SettingsButton id)
    {
        if (_pressedButton == id) return ButtonState.Pressed;
        if (_hoveredButton == id) return ButtonState.Hovered;
        return ButtonState.Normal;
    }

    private void DrawButton(SKCanvas canvas, float panelLeft, float panelTop,
        float buttonLocalY, string text, SettingsButton id)
    {
        float bx = panelLeft + (SettingsLayout.PanelWidth - SettingsLayout.ButtonWidth) / 2f;
        float by = panelTop + buttonLocalY;
        var rect = new SKRect(bx, by, bx + SettingsLayout.ButtonWidth, by + SettingsLayout.ButtonHeight);

        ImageButton.Draw(canvas, rect, text, GetState(id), MenuStyle.TypefaceHumaroid);
    }
}
