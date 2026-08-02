using SkiaSharp;

namespace DeepSpaceSaga.Client;

public sealed class MainMenuScreen : IScreen
{
    private int _screenWidth;
    private int _screenHeight;

    private readonly SKPaint _backgroundPaint;
    private readonly SKPaint _panelFillPaint;
    private readonly SKPaint _panelBorderPaint;
    private readonly SKPaint _titlePaint;
    private readonly SKPaint _versionPaint;
    private readonly SKPaint _buttonActiveFillPaint;
    private readonly SKPaint _buttonActiveBorderPaint;
    private readonly SKPaint _buttonActiveTextPaint;
    private readonly SKPaint _buttonDisabledFillPaint;
    private readonly SKPaint _buttonDisabledBorderPaint;
    private readonly SKPaint _buttonDisabledTextPaint;
    private readonly SKPaint _statusTextPaint;

    public MainMenuScreen()
    {
        _backgroundPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill };

        _panelFillPaint = new SKPaint
        {
            Color = new SKColor(MenuLayout.PanelFillColor.R, MenuLayout.PanelFillColor.G, MenuLayout.PanelFillColor.B),
            Style = SKPaintStyle.Fill
        };
        _panelBorderPaint = new SKPaint
        {
            Color = new SKColor(MenuLayout.PanelBorderColor.R, MenuLayout.PanelBorderColor.G, MenuLayout.PanelBorderColor.B),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = MenuLayout.PanelBorderWidth
        };

        _titlePaint = new SKPaint
        {
            Color = new SKColor(MenuLayout.TitleColor.R, MenuLayout.TitleColor.G, MenuLayout.TitleColor.B),
            TextSize = MenuLayout.TitleFontSize,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.Default
        };

        _versionPaint = new SKPaint
        {
            Color = new SKColor(MenuLayout.VersionColor.R, MenuLayout.VersionColor.G, MenuLayout.VersionColor.B),
            TextSize = MenuLayout.VersionFontSize,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.Default
        };

        _buttonActiveFillPaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Fill
        };
        _buttonActiveBorderPaint = new SKPaint
        {
            Color = new SKColor(MenuLayout.ButtonBorderColor.R, MenuLayout.ButtonBorderColor.G, MenuLayout.ButtonBorderColor.B),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = MenuLayout.ButtonBorderWidth
        };
        _buttonActiveTextPaint = new SKPaint
        {
            Color = new SKColor(MenuLayout.ButtonTextColor.R, MenuLayout.ButtonTextColor.G, MenuLayout.ButtonTextColor.B),
            TextSize = MenuLayout.ButtonFontSize,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.Default
        };

        _buttonDisabledFillPaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Fill
        };
        _buttonDisabledBorderPaint = new SKPaint
        {
            Color = new SKColor(MenuLayout.ButtonDisabledBorderColor.R, MenuLayout.ButtonDisabledBorderColor.G, MenuLayout.ButtonDisabledBorderColor.B),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = MenuLayout.ButtonBorderWidth
        };
        _buttonDisabledTextPaint = new SKPaint
        {
            Color = new SKColor(MenuLayout.ButtonDisabledTextColor.R, MenuLayout.ButtonDisabledTextColor.G, MenuLayout.ButtonDisabledTextColor.B),
            TextSize = MenuLayout.ButtonFontSize,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.Default
        };

        _statusTextPaint = new SKPaint
        {
            Color = new SKColor(MenuLayout.StatusTextColor.R, MenuLayout.StatusTextColor.G, MenuLayout.StatusTextColor.B),
            TextSize = MenuLayout.StatusFontSize,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.Default
        };
    }

    public void OnActivated()
    {
    }

    public void OnDeactivated()
    {
    }

    public ScreenEvent OnMouseDown(float x, float y)
    {
        var hit = MenuLayout.HitTest(x, y, _screenWidth, _screenHeight);

        return hit switch
        {
            MenuButton.NewGame => ScreenEvent.NewGame,
            MenuButton.Exit => ScreenEvent.Exit,
            _ => ScreenEvent.None
        };
    }

    public void OnMouseMove(float x, float y)
    {
    }

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        // Black background
        canvas.DrawRect(0, 0, width, height, _backgroundPaint);

        float panelLeft = (width - MenuLayout.PanelWidth) / 2f;
        float panelTop = (height - MenuLayout.PanelHeight) / 2f;

        // Panel background
        var panelRect = new SKRect(panelLeft, panelTop, panelLeft + MenuLayout.PanelWidth, panelTop + MenuLayout.PanelHeight);
        canvas.DrawRect(panelRect, _panelFillPaint);
        canvas.DrawRect(panelRect, _panelBorderPaint);

        // Title
        float titleX = panelLeft + MenuLayout.PanelWidth / 2f;
        canvas.DrawText("Deep Space Saga", titleX, panelTop + MenuLayout.TitleY, _titlePaint);

        // Version
        canvas.DrawText("Version 1.0.0", titleX, panelTop + MenuLayout.VersionY, _versionPaint);

        // NEW GAME button (active)
        DrawButton(canvas, panelLeft, panelTop, MenuLayout.NewGameButtonY, "NEW GAME",
            _buttonActiveFillPaint, _buttonActiveBorderPaint, _buttonActiveTextPaint);

        // LOAD button (disabled)
        DrawButton(canvas, panelLeft, panelTop, MenuLayout.LoadButtonY, "LOAD",
            _buttonDisabledFillPaint, _buttonDisabledBorderPaint, _buttonDisabledTextPaint);

        // "No saved games available" between LOAD and EXIT
        canvas.DrawText("No saved games available", titleX, panelTop + MenuLayout.StatusTextY, _statusTextPaint);

        // EXIT button (active)
        DrawButton(canvas, panelLeft, panelTop, MenuLayout.ExitButtonY, "EXIT",
            _buttonActiveFillPaint, _buttonActiveBorderPaint, _buttonActiveTextPaint);
    }

    private static void DrawButton(SKCanvas canvas, float panelLeft, float panelTop,
        float buttonY, string text,
        SKPaint fillPaint, SKPaint borderPaint, SKPaint textPaint)
    {
        float bx = panelLeft + MenuLayout.ButtonLeft;
        float by = panelTop + buttonY;
        var rect = new SKRect(bx, by, bx + MenuLayout.ButtonWidth, by + MenuLayout.ButtonHeight);

        canvas.DrawRoundRect(rect, MenuLayout.ButtonCornerRadius, MenuLayout.ButtonCornerRadius, fillPaint);
        canvas.DrawRoundRect(rect, MenuLayout.ButtonCornerRadius, MenuLayout.ButtonCornerRadius, borderPaint);

        // Vertically center text in button
        float textY = by + MenuLayout.ButtonHeight / 2f + textPaint.TextSize / 3f;
        canvas.DrawText(text, bx + MenuLayout.ButtonWidth / 2f, textY, textPaint);
    }
}
