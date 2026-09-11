using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Portraits;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Dialogue;

/// <summary>Snapshot-only dialogue presentation. Commands remain pending until an authoritative acknowledgement.</summary>
internal sealed class DialogueScreen : IScreen
{
    private readonly SnapshotBuffer _buffer;
    private readonly GameSessionHandle? _handle;
    private readonly string _instanceId;
    private DialogueState _state;
    private string? _pendingCommandId;
    private Task? _sendTask;
    private string? _error;
    private int _width, _height, _hover = -1, _scroll;
    private SKBitmap? _speakerPortrait, _captainPortrait;
    private string? _portraitPath;
    private bool _captainLoaded;
    private static readonly SKBitmap? Background = LoadBackground();
    private static SKBitmap? LoadBackground()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Images/UI/mechanics-window-background-titlebar-1400x900.png");
        return File.Exists(path) ? SKBitmap.Decode(path) : null;
    }
    internal bool IsPending => _pendingCommandId is not null;
    internal DialogueState State => _state;
    internal string? Error => _error;
    public DialogueScreen(SnapshotBuffer buffer, GameSessionHandle? handle, DialogueState state)
    { _buffer = buffer; _handle = handle; _state = state; _instanceId = state.InstanceId; }
    public void OnActivated() { }
    public void OnDeactivated()
    { _speakerPortrait?.Dispose(); _captainPortrait?.Dispose(); _speakerPortrait = null; _captainPortrait = null; }

    internal ScreenEvent Poll()
    {
        var snapshot = _buffer.Latest?.Snapshot;
        if (snapshot is null) return ScreenEvent.None;
        if (_sendTask is { IsFaulted: true } or { IsCanceled: true })
        { _ = _sendTask.Exception; _error = "send_failed"; _pendingCommandId = null; _sendTask = null; }
        if (_pendingCommandId is { } pending && !snapshot.DialogueEvents.IsDefaultOrEmpty)
        {
            var ack = snapshot.DialogueEvents.LastOrDefault(e => e.CommandId == pending);
            if (ack is not null)
            {
                _pendingCommandId = null;
                _sendTask = null;
                _error = ack.EventCode is "dialogue_advanced" or "dialogue_completed" or "dialogue_aborted" or "dialogue_duplicate"
                    ? null : ack.EventCode;
            }
        }
        if (snapshot.ActiveDialogue?.InstanceId != _instanceId) return ScreenEvent.CloseDialogue;
        if (snapshot.ActiveDialogue.Revision != _state.Revision) { _scroll = 0; _pendingCommandId = null; }
        _state = snapshot.ActiveDialogue;
        return ScreenEvent.None;
    }

    private void Send(DialogueAction action, string? choiceId = null)
    {
        if (IsPending || _handle is null) return;
        _error = null;
        _pendingCommandId = $"dialogue-cmd-{Guid.NewGuid():N}";
        var command = new DialogueCommand(_pendingCommandId, action, _instanceId, _state.Revision, choiceId);
        try { _sendTask = _handle.SendDialogueCommandAsync(command).AsTask(); }
        catch (Exception) { _error = "send_failed"; _pendingCommandId = null; }
    }
    public ScreenEvent OnKeyDown(Key key)
    {
        if (Poll() == ScreenEvent.CloseDialogue) return ScreenEvent.CloseDialogue;
        if (key == Key.Escape && _state.CanAbort) Send(DialogueAction.Abort);
        return ScreenEvent.None;
    }
    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left) return ScreenEvent.None;
        if (Poll() == ScreenEvent.CloseDialogue) return ScreenEvent.CloseDialogue;
        if (_state.CanAbort && (DialogueLayout.Cancel(_width, _height).Contains(x, y) || !DialogueLayout.Panel(_width, _height).Contains(x, y)))
            Send(DialogueAction.Abort);
        for (int i = 0; i < DialogueLayout.VisibleChoices && i + _scroll < _state.Choices.Length; i++)
            if (DialogueLayout.Choice(i, _width, _height).Contains(x, y) && _state.Choices[i + _scroll].Enabled)
                Send(DialogueAction.Choose, _state.Choices[i + _scroll].ChoiceId);
        return ScreenEvent.None;
    }
    public bool OnMouseMove(float x, float y)
    {
        _hover = -1;
        for (int i = 0; i < DialogueLayout.VisibleChoices && i + _scroll < _state.Choices.Length; i++)
            if (DialogueLayout.Choice(i, _width, _height).Contains(x, y)) _hover = i;
        return !IsPending && (_hover >= 0 || _state.CanAbort && DialogueLayout.Cancel(_width, _height).Contains(x, y));
    }
    public ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        _scroll = Math.Clamp(_scroll - Math.Sign(delta), 0, Math.Max(0, _state.Choices.Length - DialogueLayout.VisibleChoices));
        return ScreenEvent.None;
    }
    private string Text(string key)
    {
        string text = Localization.Get(key);
        foreach (var (name, value) in _state.Parameters) text = text.Replace("{" + name + "}", value, StringComparison.Ordinal);
        return text;
    }
    private static string ErrorText(string error)
    {
        string key = "Dialogue.Error." + error, text = Localization.Get(key);
        return text == key ? Localization.Get("Dialogue.Error.generic") : text;
    }
    public void Render(SKCanvas canvas, int width, int height)
    {
        _width = width; _height = height;
        Poll();
        var panel = DialogueLayout.Panel(width, height);
        if (Background is not null) canvas.DrawBitmap(Background, panel); else MenuStyle.DrawPanel(canvas, panel);
        canvas.DrawText(Localization.Get("Dialogue.Title"), panel.MidX, panel.Top + 60, MenuStyle.TextTitle);
        if (_portraitPath != _state.SpeakerPortraitImage)
        {
            _speakerPortrait?.Dispose(); _portraitPath = _state.SpeakerPortraitImage;
            _speakerPortrait = _portraitPath is null ? null : PortraitComposer.ComposeBodyAndHeadPortrait(_portraitPath,
                _portraitPath.Replace('\\', '/').Contains("/M/", StringComparison.Ordinal) ? PersonSex.Male : PersonSex.Female, _state.ParticipantId);
        }
        var captain = _buffer.Latest?.Snapshot.Objects.FirstOrDefault(o => o.ObjectId == _buffer.Latest.Snapshot.PlayerShipObjectId);
        if (!_captainLoaded && captain is not null)
        {
            _captainLoaded = true;
            if (captain.CaptainPortraitImage is { } path)
                _captainPortrait = PortraitComposer.ComposeBodyAndHeadPortrait(path, PersonSex.Male, captain.ObjectId);
        }
        DrawPortrait(canvas, DialogueLayout.Operator(width, height), _speakerPortrait, _state.SpeakerDisplayName);
        DrawPortrait(canvas, DialogueLayout.Captain(width, height), _captainPortrait, captain?.CaptainDisplayName);
        using var paint = new SKPaint { Color = MenuStyle.ColorText, TextSize = 18, Typeface = MenuStyle.TypefaceRegular, IsAntialias = true };
        DrawWrapped(canvas, Text(_state.TextKey), new(panel.Left + 410, panel.Top + 155, panel.Left + 990, panel.Top + 435), paint);
        for (int i = 0; i < DialogueLayout.VisibleChoices && i + _scroll < _state.Choices.Length; i++)
        {
            var choice = _state.Choices[i + _scroll];
            var rect = DialogueLayout.Choice(i, width, height);
            GenericButtonTypeA.Draw(canvas, rect, Text(choice.TextKey), !choice.Enabled || IsPending ? ButtonState.Disabled : _hover == i ? ButtonState.Hovered : ButtonState.Normal);
            if (choice.DisabledReasonKey is { } reason)
                canvas.DrawText(ErrorText(reason), rect.MidX, rect.Bottom + 12, MenuStyle.TextStatus);
        }
        if (_error is { } error) canvas.DrawText(ErrorText(error), panel.MidX, panel.Top + 705, MenuStyle.TextStatus);
        if (_state.CanAbort) GenericButtonTypeA.Draw(canvas, DialogueLayout.Cancel(width, height), Localization.Get("Dialogue.Cancel"), IsPending ? ButtonState.Disabled : ButtonState.Normal);
    }
    private static void DrawPortrait(SKCanvas canvas, SKRect rect, SKBitmap? image, string? name)
    {
        if (image is not null) canvas.DrawBitmap(image, rect);
        if (name is not null) canvas.DrawText(name, rect.MidX, rect.Bottom + 24, MenuStyle.TextStatus);
    }
    private static void DrawWrapped(SKCanvas canvas, string text, SKRect bounds, SKPaint paint)
    {
        float y = bounds.Top + paint.TextSize;
        string line = "";
        foreach (string word in text.Split(' '))
        {
            string next = line.Length == 0 ? word : line + " " + word;
            if (paint.MeasureText(next) > bounds.Width && line.Length > 0)
            { canvas.DrawText(line, bounds.Left, y, paint); y += 27; line = word; }
            else line = next;
        }
        canvas.DrawText(line, bounds.Left, y, paint);
    }
}
