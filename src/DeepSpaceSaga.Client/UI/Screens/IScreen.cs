using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens;

[Flags]
public enum KeyModifiers { None = 0, Shift = 1, Control = 2, Alt = 4 }

public interface IScreen
{
    void Render(SKCanvas canvas, int width, int height);
    ScreenEvent OnMouseDown(float x, float y, MouseButton button);
    void OnMouseUp(float x, float y) { }
    void OnMouseUp(float x, float y, MouseButton button) { if (button == MouseButton.Left) OnMouseUp(x, y); }
    bool OnMouseMove(float x, float y);
    ScreenEvent OnMouseWheel(float x, float y, float delta);
    /// <summary>Opt in to browser delta units (positive down); legacy screens receive Silk steps (positive up).</summary>
    bool UsesBrowserWheelDelta => false;
    ScreenEvent OnKeyDown(Key key);
    ScreenEvent OnKeyDown(Key key, KeyModifiers modifiers, bool repeat) => OnKeyDown(key);
    bool WantsArrowRepeat => false;
    /// <summary>Focus loss cancels transient input such as dragging and panning.</summary>
    void OnInputReset() { }
    void OnKeyUp(Key key) { }
    /// <summary>Printable-character input, forwarded from Silk.NET's <c>IKeyboard.KeyChar</c>.</summary>
    void OnTextInput(char c) { }
    void OnActivated();
    void OnDeactivated();
}

public static class MouseWheelInput
{
    public const float BrowserUnitsPerStep = 100;
    public const double ZoomSensitivity = 0.0015;
    public static float NormalizeSilk(float steps) => float.IsFinite(steps) ? -steps * BrowserUnitsPerStep : 0;
    public static double ZoomFactor(float browserDelta) => float.IsFinite(browserDelta) ? Math.Exp(-browserDelta * ZoomSensitivity) : 1;
}

public enum ScreenEvent
{
    None,
    NewGame,
    ScenarioSelected,
    Exit,
    OpenGameMenu,
    Resume,
    MainMenu,
    QuickSave,
    QuickLoad,
    OpenSettings,
    CloseSettings,
    OpenSaveWindow,
    CloseSaveWindow,
    OpenLoadWindow,
    CloseLoadWindow,
    LoadSlotRequested,
    OpenFinance,
    CloseFinance,
    OpenShip,
    CloseShip,
    OpenStation,
    TravelDock,
    TravelMarket,
    TravelHabitation,
    TravelAdministration,
    CloseStation,
    Undock,
    OpenDialogue,
    CloseDialogue,
    OpenTrade,
    CloseTrade,
    OpenHire,
    CloseHire,
    OpenContracts,
    CloseContracts,
    /// <summary>
    /// Return to the Station hub from one of its nested windows (Trade/Hire/Contracts/
    /// Finance) — raised by clicking the station-name label in StationToolbar. Closes the
    /// current window and ensures Station ends up open, whether or not Station was
    /// already directly beneath it (see SkiaWindow.NavigateToStationAsync).
    /// </summary>
    NavigateToStation,
    OpenTempCharacterImage,
    CloseTempCharacterImage
}
