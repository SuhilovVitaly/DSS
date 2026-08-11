using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Settings;

public sealed class SettingsScreen : IScreen
{
    private readonly IReadOnlyList<string> _monitorNames;
    private readonly Action<int> _onMonitorSelected;

    private int _screenWidth;
    private int _screenHeight;

    private int _selectedMonitorIndex;
    private bool _isMonitorComboOpen;
    private int _hoveredMonitorOption = -1;

    private SettingsButton _hoveredButton = SettingsButton.None;
    private SettingsButton _pressedButton = SettingsButton.None;

    private static readonly SKPaint _dimPaint = new()
    {
        Color = new SKColor(0, 0, 0, 160),
        Style = SKPaintStyle.Fill
    };

    private static readonly SKPaint _arrowPaint = new()
    {
        Color = MenuStyle.ColorText,
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    public SettingsScreen(IReadOnlyList<string> monitorNames, int selectedMonitorIndex, Action<int> onMonitorSelected)
    {
        _monitorNames = monitorNames.Count > 0 ? monitorNames : new[] { "Monitor 1" };
        _selectedMonitorIndex = Math.Clamp(selectedMonitorIndex, 0, _monitorNames.Count - 1);
        _onMonitorSelected = onMonitorSelected;
    }

    public void OnActivated()
    {
        _hoveredButton = SettingsButton.None;
        _pressedButton = SettingsButton.None;
        _isMonitorComboOpen = false;
        _hoveredMonitorOption = -1;
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

        return ScreenEvent.CloseSettings;
    }

    public ScreenEvent OnMouseDown(float x, float y)
    {
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

        var hit = SettingsLayout.HitTest(x, y, _screenWidth, _screenHeight);

        if (hit == SettingsButton.MonitorCombo)
        {
            _isMonitorComboOpen = true;
            return ScreenEvent.None;
        }

        if (hit == SettingsButton.None)
            return ScreenEvent.None;

        _pressedButton = hit;

        return hit == SettingsButton.Exit ? ScreenEvent.CloseSettings : ScreenEvent.None;
    }

    public bool OnMouseMove(float x, float y)
    {
        if (_isMonitorComboOpen)
        {
            _hoveredMonitorOption = SettingsLayout.HitTestMonitorOption(x, y, _screenWidth, _screenHeight, _monitorNames.Count);
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

        // Dim background (overlay effect — underlying screen renders behind this)
        canvas.DrawRect(0, 0, width, height, _dimPaint);

        float pl = SettingsLayout.PanelLeft(width);
        float pt = SettingsLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + SettingsLayout.PanelWidth, pt + SettingsLayout.PanelHeight);
        MenuStyle.DrawPanel(canvas, panelRect);

        float cx = pl + SettingsLayout.PanelWidth / 2f;

        canvas.DrawText("SETTINGS", cx, pt + SettingsLayout.TitleY, MenuStyle.TextTitle);

        DrawMonitorCombo(canvas, pl, pt, cx);
        DrawButton(canvas, pl, pt, SettingsLayout.ExitY, "EXIT", SettingsButton.Exit);
    }

    private void DrawMonitorCombo(SKCanvas canvas, float panelLeft, float panelTop, float centerX)
    {
        canvas.DrawText("MONITOR", centerX, panelTop + SettingsLayout.MonitorLabelY, MenuStyle.TextStatus);

        float bx = panelLeft + (SettingsLayout.PanelWidth - SettingsLayout.MonitorComboWidth) / 2f;
        float by = panelTop + SettingsLayout.MonitorComboY;
        var boxRect = new SKRect(bx, by, bx + SettingsLayout.MonitorComboWidth, by + SettingsLayout.MonitorComboHeight);

        bool boxHighlighted = _isMonitorComboOpen || _hoveredButton == SettingsButton.MonitorCombo;
        MenuStyle.DrawButton(canvas, boxRect, _monitorNames[_selectedMonitorIndex],
            boxHighlighted ? ButtonState.Hovered : ButtonState.Normal);
        DrawDropdownArrow(canvas, boxRect, _isMonitorComboOpen);

        if (_isMonitorComboOpen)
        {
            for (int i = 0; i < _monitorNames.Count; i++)
            {
                float oy = by + SettingsLayout.MonitorComboHeight + i * SettingsLayout.MonitorOptionHeight;
                var optionRect = new SKRect(bx, oy, bx + SettingsLayout.MonitorComboWidth, oy + SettingsLayout.MonitorOptionHeight);

                var optionState = i == _selectedMonitorIndex ? ButtonState.Pressed
                    : i == _hoveredMonitorOption ? ButtonState.Hovered
                    : ButtonState.Normal;

                MenuStyle.DrawButton(canvas, optionRect, _monitorNames[i], optionState);
            }
        }
        else
        {
            canvas.DrawText("Changes apply after game restart", centerX, panelTop + SettingsLayout.MonitorNoteY, MenuStyle.TextStatus);
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

        MenuStyle.DrawButton(canvas, rect, text, GetState(id));
    }
}
