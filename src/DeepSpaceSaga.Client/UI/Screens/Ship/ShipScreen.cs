using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Ship;

/// <summary>
/// Ship overlay (ТЗ DB/Documentation/02-FirstRelease/Screens/Ship.md). Placeholder shell: the
/// TetrarchClass/CrewAndHabitation/CrewDialogues/IceMining mechanics it will report
/// on are not yet implemented in the Engine, so every section shows a "not
/// available yet" line instead of fabricated data. Opened via the bottom-center
/// Ship panel button or Ctrl+S; closes via the × button, Escape, or a click
/// outside the panel (on the dimmed background). Pause-on-open/resume-on-close
/// is handled generically by SkiaWindow's PushModalAsync/PopModalAsync — this
/// screen has no speed/pause logic of its own.
/// </summary>
public sealed class ShipScreen : IScreen
{
    private readonly ShipViewModel _model;
    private int _screenWidth;
    private int _screenHeight;
    private bool _isCloseHovered;

    /// <summary>Legacy open path: no carrier surface yet — renders the explicit unsupported state.</summary>
    public ShipScreen() : this(ShipViewModel.From(null)) { }

    public ShipScreen(ShipViewModel model) => _model = model;

    public void OnActivated() => _isCloseHovered = false;

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key) =>
        key == Key.Escape ? ScreenEvent.CloseShip : ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        var hit = ShipLayout.HitTest(x, y, _screenWidth, _screenHeight);
        if (hit == ShipButton.Close)
            return ScreenEvent.CloseShip;

        // Click on the dimmed background outside the panel also closes it.
        if (!ShipLayout.IsInsidePanel(x, y, _screenWidth, _screenHeight))
            return ScreenEvent.CloseShip;

        return ScreenEvent.None;
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call sites/tests.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        var hit = ShipLayout.HitTest(x, y, _screenWidth, _screenHeight);
        _isCloseHovered = hit == ShipButton.Close;
        return _isCloseHovered;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        float pl = ShipLayout.PanelLeft(width);
        float pt = ShipLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + ShipLayout.PanelWidth, pt + ShipLayout.PanelHeight);
        TabletopStyle.Panel(canvas, panelRect);

        float cx = pl + ShipLayout.PanelWidth / 2f;
        canvas.DrawText("SHIP", cx, pt + ShipLayout.TitleY, MenuStyle.TextTitle);

        float textY = pt + ShipLayout.BodyStartY;
        if (_model.Unsupported)
        {
            canvas.DrawText("Ship overview: not available for this ship", cx, textY, MenuStyle.TextStatus);
            textY += ShipLayout.BodyLineHeight;
        }
        else
        {
            canvas.DrawText($"{_model.Name} ({_model.ShipClassId}) — hull {_model.HullCurrent}/{_model.HullMax}",
                cx, pt + ShipLayout.BodyStartY, MenuStyle.TextStatus);

            // Wing panel (left half): one line per owned drone, destroyed/disabled never shown ready.
            var (wl, wt, _, _) = ShipLayout.WingPanelLocalRect();
            float wingY = pt + wt;
            foreach (var drone in _model.Drones)
            {
                string state = ShipWingPanel.DroneStateLabel(drone.Destroyed);
                canvas.DrawText($"{drone.Id} — ammo {ShipWingPanel.DroneAmmoText(drone.AmmoCurrent, drone.AmmoCapacity)} ({state})",
                    pl + wl, wingY, MenuStyle.TextStatus);
                wingY += ShipLayout.BodyLineHeight;
            }

            // Crew panel (right half): one line per crew member.
            var (cl, ct, _, _) = ShipLayout.CrewPanelLocalRect();
            float crewY = pt + ct;
            foreach (var member in _model.Crew)
            {
                canvas.DrawText($"{member.DisplayName} ({member.Role})", pl + cl, crewY, MenuStyle.TextStatus);
                crewY += ShipLayout.BodyLineHeight;
            }
        }

        DrawCloseButton(canvas, pl, pt);
    }

    private void DrawCloseButton(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var (left, top, right, bottom) = ShipLayout.CloseButtonLocalRect();
        var rect = new SKRect(panelLeft + left, panelTop + top, panelLeft + right, panelTop + bottom);

        MenuStyle.DrawButton(canvas, rect, "×", _isCloseHovered ? ButtonState.Hovered : ButtonState.Normal);
    }
}
