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

    /// <summary>Restart note in the subdued cyan Xenon footer style.</summary>
    private static readonly SKPaint _noteTextPaint = new()
    {
        Color = XenonStyle.CyanDim,
        TextSize = XenonStyle.FooterFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = XenonStyle.TypefaceRegular
    };

    public SettingsScreen(
        IReadOnlyList<string> monitorNames, int selectedMonitorIndex, Action<int> onMonitorSelected,
        double selectedUiScale, Action<double> onUiScaleSelected,
        string selectedLanguage, Action<string> onLanguageSelected)
    {
        GenericWindowTypeA.Preload();
        GenericButtonTypeA.Preload();
        XenonComboBox.Preload();

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
        var panelRect = SettingsLayout.PanelRect(width, height);
        GenericWindowTypeA.Draw(canvas, panelRect);

        float cx = pl + SettingsLayout.PanelWidth / 2f;

        GenericWindowTypeA.DrawTitle(canvas, panelRect, Localization.Get("Settings.Title"), _titleTextPaint);

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

        bool boxHighlighted = _isLanguageComboOpen || _hoveredButton == SettingsButton.LanguageCombo;
        XenonComboBox.DrawClosed(canvas, boxRect, Localization.Get("Settings.Language"),
            LanguageValues[_selectedLanguageIndex], boxHighlighted);
    }

    private void DrawLanguageOptions(SKCanvas canvas, float panelLeft, float panelTop)
    {
        float bx = panelLeft + SettingsLayout.RowRightX - SettingsLayout.LanguageComboWidth;
        float by = panelTop + SettingsLayout.LanguageRowY + SettingsLayout.LanguageComboHeight;
        float listHeight = LanguageValues.Length * SettingsLayout.LanguageOptionHeight;

        XenonComboBox.DrawListBackground(canvas,
            new SKRect(bx, by, bx + SettingsLayout.LanguageComboWidth, by + listHeight));

        for (int i = 0; i < LanguageValues.Length; i++)
        {
            float oy = by + i * SettingsLayout.LanguageOptionHeight;
            var optionRect = new SKRect(bx, oy, bx + SettingsLayout.LanguageComboWidth, oy + SettingsLayout.LanguageOptionHeight);

            XenonComboBox.DrawOption(canvas, optionRect, LanguageValues[i],
                highlighted: i == _selectedLanguageIndex || i == _hoveredLanguageOption);
        }
    }

    private void DrawUiScaleRow(SKCanvas canvas, float panelLeft, float panelTop)
    {
        float bx = panelLeft + SettingsLayout.RowRightX - SettingsLayout.UiScaleComboWidth;
        float by = panelTop + SettingsLayout.UiScaleRowY;
        var boxRect = new SKRect(bx, by, bx + SettingsLayout.UiScaleComboWidth, by + SettingsLayout.UiScaleComboHeight);

        bool boxHighlighted = _isUiScaleComboOpen || _hoveredButton == SettingsButton.UiScaleCombo;
        XenonComboBox.DrawClosed(canvas, boxRect, Localization.Get("Settings.InterfaceScale"),
            FormatScaleLabel(UiScaleValues[_selectedUiScaleIndex]), boxHighlighted);
    }

    private void DrawUiScaleOptions(SKCanvas canvas, float panelLeft, float panelTop)
    {
        float bx = panelLeft + SettingsLayout.RowRightX - SettingsLayout.UiScaleComboWidth;
        float by = panelTop + SettingsLayout.UiScaleRowY + SettingsLayout.UiScaleComboHeight;
        float listHeight = UiScaleValues.Length * SettingsLayout.UiScaleOptionHeight;

        XenonComboBox.DrawListBackground(canvas,
            new SKRect(bx, by, bx + SettingsLayout.UiScaleComboWidth, by + listHeight));

        for (int i = 0; i < UiScaleValues.Length; i++)
        {
            float oy = by + i * SettingsLayout.UiScaleOptionHeight;
            var optionRect = new SKRect(bx, oy, bx + SettingsLayout.UiScaleComboWidth, oy + SettingsLayout.UiScaleOptionHeight);

            XenonComboBox.DrawOption(canvas, optionRect, FormatScaleLabel(UiScaleValues[i]),
                highlighted: i == _selectedUiScaleIndex || i == _hoveredUiScaleOption);
        }
    }

    private static string FormatScaleLabel(double scale) => $"{(int)Math.Round(scale * 100)}%";

    private void DrawMonitorRow(SKCanvas canvas, float panelLeft, float panelTop, float centerX)
    {
        float bx = panelLeft + SettingsLayout.RowRightX - SettingsLayout.MonitorComboWidth;
        float by = panelTop + SettingsLayout.MonitorRowY;
        var boxRect = new SKRect(bx, by, bx + SettingsLayout.MonitorComboWidth, by + SettingsLayout.MonitorComboHeight);

        bool boxHighlighted = _isMonitorComboOpen || _hoveredButton == SettingsButton.MonitorCombo;
        XenonComboBox.DrawClosed(canvas, boxRect, Localization.Get("Settings.Monitor"),
            _monitorNames[_selectedMonitorIndex], boxHighlighted);

        if (!_isMonitorComboOpen)
            canvas.DrawText(Localization.Get("Settings.RestartNote"), centerX, panelTop + SettingsLayout.MonitorNoteY, _noteTextPaint);
    }

    private void DrawMonitorOptions(SKCanvas canvas, float panelLeft, float panelTop)
    {
        float bx = panelLeft + SettingsLayout.RowRightX - SettingsLayout.MonitorComboWidth;
        float by = panelTop + SettingsLayout.MonitorRowY + SettingsLayout.MonitorComboHeight;
        float listHeight = _monitorNames.Count * SettingsLayout.MonitorOptionHeight;

        XenonComboBox.DrawListBackground(canvas,
            new SKRect(bx, by, bx + SettingsLayout.MonitorComboWidth, by + listHeight));

        for (int i = 0; i < _monitorNames.Count; i++)
        {
            float oy = by + i * SettingsLayout.MonitorOptionHeight;
            var optionRect = new SKRect(bx, oy, bx + SettingsLayout.MonitorComboWidth, oy + SettingsLayout.MonitorOptionHeight);

            XenonComboBox.DrawOption(canvas, optionRect, _monitorNames[i],
                highlighted: i == _selectedMonitorIndex || i == _hoveredMonitorOption);
        }
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

        GenericButtonTypeA.Draw(canvas, rect, text, GetState(id));
    }
}
