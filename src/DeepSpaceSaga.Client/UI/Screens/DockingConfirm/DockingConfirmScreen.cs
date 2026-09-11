using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Portraits;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.DockingConfirm;

/// <summary>
/// Docking confirmation modal — shown before a navigation.dock command is actually sent to
/// the engine. Opened from GameSessionScreen (ScreenEvent.OpenDockingConfirm, produced by a
/// Commands Panel click on the Dock button — see GameSessionScreen.SendCommandFromPanel /
/// DockingConfirmRequest); the "Стыковка" button sends the deferred command and closes back
/// to GameSessionScreen (ScreenEvent.CloseDockingConfirm). Escape / a click outside the panel
/// close WITHOUT sending anything — the ship stays undocked exactly as if the player had
/// never clicked Dock. Pause-on-open/resume-on-close is handled generically by SkiaWindow's
/// PushModalAsync/PopModalAsync — this screen has no speed/pause logic of its own. Once a
/// successful Dock is confirmed and the engine executes it, GameSessionScreen's existing
/// ConsumePendingAutoTransition polling opens the Station screen exactly as before this
/// batch — untouched here.
/// </summary>
internal sealed class DockingConfirmScreen : IScreen
{
    private readonly SnapshotBuffer? _buffer;
    private readonly GameSessionHandle? _handle;
    private readonly DockingConfirmRequest _request;

    private int _screenWidth;
    private int _screenHeight;
    private bool _isConfirmHovered;
    private bool _isConfirmPressed;

    /// <summary>
    /// Composed at most once per screen instance (see <see cref="EnsurePortraitsComposed"/>)
    /// — the snapshot doesn't change while this modal is open (the game is paused for its
    /// whole lifetime, generically by SkiaWindow), so recomposing every frame would just
    /// repeat the same file I/O/canvas work for no visual difference.
    /// </summary>
    private bool _portraitsComposed;
    private SKBitmap? _dockOperatorPortrait;
    private SKBitmap? _captainPortrait;

    private static readonly SKBitmap? BackgroundImage =
        LoadImage("Images/UI/mechanics-window-background-titlebar-1400x900.png");

    /// <summary>
    /// Test-only: every female face portrait file, for <see cref="RerollDockOperatorFace"/>'s
    /// click-to-randomize. Not a gameplay data source — just a dev tool to preview the
    /// corporate suit against many faces without restarting the game.
    /// </summary>
    private static readonly string[] FemaleFacePool = LoadFemaleFacePool();

    private static readonly Random RerollRandom = new();

    private static string[] LoadFemaleFacePool()
    {
        try { return Directory.Exists("Images/Persons/W") ? Directory.GetFiles("Images/Persons/W", "*.png") : []; }
        catch { return []; }
    }

    private static SKBitmap? LoadImage(string path)
    {
        try { return File.Exists(path) ? SKBitmap.Decode(path) : null; }
        catch { return null; }
    }

    public DockingConfirmScreen(SnapshotBuffer? buffer, GameSessionHandle? handle, DockingConfirmRequest request)
    {
        _buffer = buffer;
        _handle = handle;
        _request = request;
    }

    public void OnActivated()
    {
        _isConfirmHovered = false;
        _isConfirmPressed = false;
    }

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key) =>
        key == Key.Escape ? ScreenEvent.CloseDockingConfirm : ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        var hit = DockingConfirmLayout.HitTest(x, y, _screenWidth, _screenHeight);
        if (hit == DockingConfirmButton.Confirm)
        {
            _isConfirmPressed = true;
            ConfirmDocking();
            return ScreenEvent.CloseDockingConfirm;
        }

        // Test-only: clicking the dock operator's portrait reshuffles her face — see
        // RerollDockOperatorFace. Checked before the "outside panel closes" fallback since
        // the portrait sits inside the panel.
        if (DockOperatorPortraitScreenRect().Contains(x, y))
        {
            RerollDockOperatorFace();
            return ScreenEvent.None;
        }

        // Click on the dimmed background outside the panel also closes it — no command sent.
        if (!DockingConfirmLayout.IsInsidePanel(x, y, _screenWidth, _screenHeight))
            return ScreenEvent.CloseDockingConfirm;

        return ScreenEvent.None;
    }

    private SKRect DockOperatorPortraitScreenRect()
    {
        var local = DockingConfirmLayout.DockOperatorPortraitLocalRect();
        float pl = DockingConfirmLayout.PanelLeft(_screenWidth);
        float pt = DockingConfirmLayout.PanelTop(_screenHeight);
        return new SKRect(pl + local.Left, pt + local.Top, pl + local.Right, pt + local.Bottom);
    }

    /// <summary>
    /// Test-only: recomposes the dock operator's portrait with a random face from
    /// <see cref="FemaleFacePool"/> on the same corporate suit — lets a dev eyeball the
    /// suit/docking-point calibration against many faces without restarting the game. Not
    /// wired to any snapshot data; the reroll is purely local render state.
    /// </summary>
    private void RerollDockOperatorFace()
    {
        if (FemaleFacePool.Length == 0)
            return;

        string facePath = FemaleFacePool[RerollRandom.Next(FemaleFacePool.Length)];
        var recomposed = PortraitComposer.ComposeBodyAndHeadPortrait(facePath, PersonSex.Female, _request.TargetObjectId);
        if (recomposed is null)
            return;

        _dockOperatorPortrait?.Dispose();
        _dockOperatorPortrait = recomposed;
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call sites/tests.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        var hit = DockingConfirmLayout.HitTest(x, y, _screenWidth, _screenHeight);
        _isConfirmHovered = hit == DockingConfirmButton.Confirm;
        return _isConfirmHovered;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;

    /// <summary>
    /// Sends the deferred navigation.dock command — exactly the call
    /// GameSessionScreen.SendCommandFromPanel used to make directly before this
    /// confirmation step existed. A null handle (tests constructing the screen directly) is
    /// a no-op, matching every other screen's _handle-less fallback.
    /// </summary>
    private void ConfirmDocking()
    {
        _ = _handle?.SendCommandAsync(
            _request.PlayerShipObjectId, _request.ModuleId, NavigationComputerCommandTypes.Dock,
            _request.TargetObjectId);
    }

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        float pl = DockingConfirmLayout.PanelLeft(width);
        float pt = DockingConfirmLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + DockingConfirmLayout.PanelWidth, pt + DockingConfirmLayout.PanelHeight);
        if (BackgroundImage is not null)
            canvas.DrawBitmap(BackgroundImage, panelRect);
        else
            MenuStyle.DrawPanel(canvas, panelRect);

        float cx = pl + DockingConfirmLayout.PanelWidth / 2f;
        canvas.DrawText("CONFIRM DOCKING", cx, pt + DockingConfirmLayout.TitleY, MenuStyle.TextTitle);

        EnsurePortraitsComposed();
        var (captainName, dockOperatorName) = ResolveNames();

        DrawPortrait(canvas, pl, pt, DockingConfirmLayout.DockOperatorPortraitLocalRect(),
            _dockOperatorPortrait, dockOperatorName);
        DrawPortrait(canvas, pl, pt, DockingConfirmLayout.CaptainPortraitLocalRect(),
            _captainPortrait, captainName);

        DrawConfirmButton(canvas, pl, pt);
    }

    private void DrawPortrait(
        SKCanvas canvas, float panelLeft, float panelTop, SKRect localRect, SKBitmap? portrait, string? name)
    {
        var rect = new SKRect(
            panelLeft + localRect.Left, panelTop + localRect.Top,
            panelLeft + localRect.Right, panelTop + localRect.Bottom);

        // No portrait path resolved yet (e.g. a legacy save without the sender data) —
        // draw nothing rather than an empty/placeholder box or throwing.
        if (portrait is not null)
            canvas.DrawBitmap(portrait, rect);

        if (!string.IsNullOrWhiteSpace(name))
        {
            float textY = rect.Bottom + DockingConfirmLayout.PortraitNameGap;
            canvas.DrawText(name, rect.MidX, textY, MenuStyle.TextStatus);
        }
    }

    private void DrawConfirmButton(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var local = DockingConfirmLayout.ConfirmButtonLocalRect();
        var rect = new SKRect(
            panelLeft + local.Left, panelTop + local.Top,
            panelLeft + local.Right, panelTop + local.Bottom);

        var state = _isConfirmPressed
            ? ButtonState.Pressed
            : _isConfirmHovered ? ButtonState.Hovered : ButtonState.Normal;
        GenericButtonTypeA.Draw(canvas, rect, "Стыковка", state);
    }

    /// <summary>
    /// Composes the dock-operator (left, <see cref="PersonSex.Female"/>) and captain (right,
    /// <see cref="PersonSex.Male"/>) portraits via <see cref="PortraitComposer.Compose"/>, once.
    /// A null/missing path (no live buffer yet, unknown station, or a legacy snapshot without
    /// the resolved sender data) leaves the corresponding bitmap null — <see cref="DrawPortrait"/>
    /// then simply skips drawing it instead of throwing or showing a broken image.
    /// </summary>
    private void EnsurePortraitsComposed()
    {
        if (_portraitsComposed)
            return;

        _portraitsComposed = true;

        var objects = _buffer?.Latest?.Snapshot.Objects;
        if (objects is null)
            return;

        // Background-less pipeline: PortraitComposer.ComposeBodyAndHeadPortrait (body + head,
        // no background layer yet, aligned on the docking point, cropped to 330x330) instead
        // of the production Compose() crop pipeline — see PortraitComposer.DockingPointX/Y doc comment.
        var captain = objects.Value.FirstOrDefault(o => o.ObjectId == _request.PlayerShipObjectId);
        if (captain is { CaptainPortraitImage: { } captainPortraitPath })
            _captainPortrait = PortraitComposer.ComposeBodyAndHeadPortrait(captainPortraitPath, PersonSex.Male, _request.PlayerShipObjectId);

        var dockOperator = objects.Value.FirstOrDefault(o => o.ObjectId == _request.TargetObjectId);
        if (dockOperator is { DockOperatorPortraitImage: { } operatorPortraitPath })
            _dockOperatorPortrait = PortraitComposer.ComposeBodyAndHeadPortrait(operatorPortraitPath, PersonSex.Female, _request.TargetObjectId);
    }

    /// <summary>Names are read live every frame (cheap, unlike the portrait composition) — see the class doc comment.</summary>
    private (string? CaptainName, string? DockOperatorName) ResolveNames()
    {
        var objects = _buffer?.Latest?.Snapshot.Objects;
        if (objects is null)
            return (null, null);

        var captain = objects.Value.FirstOrDefault(o => o.ObjectId == _request.PlayerShipObjectId);
        var dockOperator = objects.Value.FirstOrDefault(o => o.ObjectId == _request.TargetObjectId);

        return (captain?.CaptainDisplayName, dockOperator?.DockOperatorDisplayName);
    }
}
