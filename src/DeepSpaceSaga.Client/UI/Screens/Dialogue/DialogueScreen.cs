using System.Collections.Immutable;
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
    // Speech field scroll: first visible wrapped line, plus the line counts measured by the last render.
    private int _speechScroll, _speechLineCount, _speechVisibleLines = int.MaxValue;
    private ImmutableArray<string> _visibleSpeechLines = [];
    private SKBitmap? _speakerPortrait, _captainPortrait;
    private (string? Path, string Person)? _speakerKey, _captainKey;
    internal bool IsPending => _pendingCommandId is not null;
    internal DialogueState State => _state;
    internal string? Error => _error;
    internal int SpeechScroll => _speechScroll;
    internal int SpeechLineCount => _speechLineCount;
    internal ImmutableArray<string> VisibleSpeechLines => _visibleSpeechLines;
    internal bool SpeechHasMoreAbove => _speechScroll > 0;
    internal bool SpeechHasMoreBelow => _speechScroll + _speechVisibleLines < _speechLineCount;
    private int MaxSpeechScroll => Math.Max(0, _speechLineCount - _speechVisibleLines);
    internal static SKRect SpeechField(int width, int height) => SpeechField(DialogueLayout.Panel(width, height));
    private static SKRect SpeechField(SKRect panel) => new(panel.Left + 410, panel.Top + 155, panel.Left + 990, panel.Top + 435);
    public DialogueScreen(SnapshotBuffer buffer, GameSessionHandle? handle, DialogueState state)
    { _buffer = buffer; _handle = handle; _state = state; _instanceId = state.InstanceId; }
    public void OnActivated() { }
    public void OnDeactivated()
    {
        _speakerPortrait?.Dispose(); _captainPortrait?.Dispose();
        _speakerPortrait = null; _captainPortrait = null; _speakerKey = null; _captainKey = null;
    }

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
        if (snapshot.ActiveDialogue.Revision != _state.Revision) { _scroll = 0; _speechScroll = 0; _pendingCommandId = null; }
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
        else if (key is Key.Up or Key.Down or Key.PageUp or Key.PageDown or Key.Home or Key.End)
        {
            int page = _speechVisibleLines == int.MaxValue ? 1 : Math.Max(1, _speechVisibleLines - 1);
            int target = key switch
            {
                Key.Up => _speechScroll - 1, Key.Down => _speechScroll + 1,
                Key.PageUp => _speechScroll - page, Key.PageDown => _speechScroll + page,
                Key.Home => 0, _ => MaxSpeechScroll,
            };
            _speechScroll = Math.Clamp(target, 0, MaxSpeechScroll);
        }
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
        if (SpeechField(_width, _height).Contains(x, y))
        {
            _speechScroll = Math.Clamp(_speechScroll - Math.Sign(delta), 0, MaxSpeechScroll);
            return ScreenEvent.None;
        }
        _scroll = Math.Clamp(_scroll - Math.Sign(delta), 0, Math.Max(0, _state.Choices.Length - DialogueLayout.VisibleChoices));
        return ScreenEvent.None;
    }
    private string Text(string key)
    {
        if (key == "Demo.SensorReport.Intro" && _state.Parameters.TryGetValue("sensorBatchId", out var id) &&
            _buffer.Latest?.Snapshot.DemoCampaign?.PendingSensorReport is { } batch && batch.BatchId == id)
            return SensorReportText(batch);
        string text = Localization.Get(key);
        foreach (var (name, value) in _state.Parameters) text = text.Replace("{" + name + "}", value, StringComparison.Ordinal);
        return text;
    }
    internal static string SensorReportText(SensorContactReportBatch batch) =>
        Localization.Get("Demo.SensorReport.Intro") + "\n\n" + string.Join("\n\n", batch.Reports.Select(report =>
        {
            string signatures = string.Join(" + ", report.SignatureTypeIds.Select(type =>
                Localization.Get(type == PirateSignatureTypes.Lair ? "Demo.Pirate.Lair" : "Demo.Pirate.Tancred")));
            string eta = report.EtaRealSecondsAt1X is { } seconds ? string.Format(Localization.Get("Demo.SensorReport.Eta"), seconds)
                : Localization.Get("Demo.SensorReport.NoEta");
            return string.Format(Localization.Get("Demo.SensorReport.Contact"), signatures, report.BearingDegrees,
                report.RangeWorldUnits / 10, report.ApproximateDroneCount, eta);
        })) + "\n\n" + Localization.Get("Demo.SensorReport.Footer");
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
        TabletopStyle.Panel(canvas, panel);
        canvas.DrawText(Localization.Get("Dialogue.Title"), panel.MidX, panel.Top + 60, MenuStyle.TextTitle);
        var captain = _buffer.Latest?.Snapshot.Objects.FirstOrDefault(o => o.ObjectId == _buffer.Latest.Snapshot.PlayerShipObjectId);
        bool sensorScript = _state.DialogueDefinitionId == "dialogue.sensor-pirate-contact" && _state.ParticipantId == "sensor-script";
        string speakerPerson = _state.DialogueDefinitionId == "dialogue.sensor-pirate-contact" ? _state.ParticipantId :
            _state.SpeakerRole == "Captain" && captain is not null ? captain.ObjectId :
            $"{_state.StationObjectId}/{_state.ParticipantId}/{_state.SpeakerRole}";
        var speakerKey = (_state.SpeakerPortraitImage, speakerPerson);
        if (_speakerKey != speakerKey)
        {
            _speakerPortrait?.Dispose(); _speakerKey = speakerKey;
            _speakerPortrait = sensorScript ? null : DialoguePortraitComposer.Compose(_state.SpeakerPortraitImage,
                DialoguePortraitComposer.SexFor(_state.SpeakerPortraitImage, _state.SpeakerRole == "Captain" ? PersonSex.Male : PersonSex.Female), speakerPerson);
        }
        (string? Path, string Person)? captainKey = captain is null ? null : (captain.CaptainPortraitImage, captain.ObjectId);
        if (_captainKey != captainKey)
        {
            _captainPortrait?.Dispose(); _captainPortrait = null; _captainKey = captainKey;
            if (captain is not null)
                _captainPortrait = DialoguePortraitComposer.Compose(captain.CaptainPortraitImage, PersonSex.Male, captain.ObjectId);
        }
        DrawPortrait(canvas, DialogueLayout.Operator(width, height), _speakerPortrait,
            sensorScript ? Localization.Get("Demo.SensorReport.Script") : _state.SpeakerDisplayName);
        DrawPortrait(canvas, DialogueLayout.Captain(width, height), _captainPortrait, captain?.CaptainDisplayName);
        using var paint = new SKPaint { Color = MenuStyle.ColorText, TextSize = 18, Typeface = MenuStyle.TypefaceRegular, IsAntialias = true };
        var speech = SpeechField(panel);
        TabletopStyle.Field(canvas, speech);
        speech.Inflate(-18, -14);
        DrawSpeech(canvas, Text(_state.TextKey), speech, paint);
        for (int i = 0; i < DialogueLayout.VisibleChoices && i + _scroll < _state.Choices.Length; i++)
        {
            var choice = _state.Choices[i + _scroll];
            var rect = DialogueLayout.Choice(i, width, height);
            GenericButtonTypeA.Draw(canvas, rect, Text(choice.TextKey), !choice.Enabled || IsPending ? ButtonState.Disabled : _hover == i ? ButtonState.Hovered : ButtonState.Normal);
            if (choice.DisabledReasonKey is { } reason)
            {
                using var reasonPaint = MenuStyle.TextStatus.Clone();
                reasonPaint.Color = TabletopStyle.Danger;
                TabletopStyle.Label(canvas, new(rect.Left, rect.Bottom, rect.Right, rect.Bottom + 16),
                    ErrorText(reason), reasonPaint);
            }
        }
        if (_error is { } error)
        {
            using var errorPaint = MenuStyle.TextStatus.Clone();
            errorPaint.Color = TabletopStyle.Danger;
            TabletopStyle.Label(canvas, new(speech.Left, panel.Top + 697, speech.Right, panel.Top + 722),
                ErrorText(error), errorPaint);
        }
        if (_state.CanAbort) GenericButtonTypeA.Draw(canvas, DialogueLayout.Cancel(width, height), Localization.Get("Dialogue.Cancel"), IsPending ? ButtonState.Disabled : ButtonState.Normal);
    }
    private static void DrawPortrait(SKCanvas canvas, SKRect rect, SKBitmap? image, string? name)
    {
        TabletopStyle.Field(canvas, rect);
        if (image is not null)
        {
            var content = rect;
            content.Inflate(-4, -4);
            canvas.DrawBitmap(image, content);
        }
        if (name is not null)
            TabletopStyle.Label(canvas, new(rect.Left, rect.Bottom + 5, rect.Right, rect.Bottom + 35),
                name, MenuStyle.TextButton);
    }

    /// <summary>Draws only whole lines (descenders included) from the scroll offset and marks hidden text above/below.</summary>
    private void DrawSpeech(SKCanvas canvas, string text, SKRect bounds, SKPaint paint)
    {
        var lines = ControlText.Wrap(text, bounds.Width, paint);
        var metrics = paint.FontMetrics;
        float lineHeight = Math.Max(27, paint.FontSpacing);
        float firstLineHeight = metrics.Descent - metrics.Ascent;
        _speechLineCount = lines.Length;
        _speechVisibleLines = Math.Max(1, 1 + (int)Math.Floor((bounds.Height - firstLineHeight) / lineHeight));
        _speechScroll = Math.Clamp(_speechScroll, 0, MaxSpeechScroll);
        var visible = ImmutableArray.CreateBuilder<string>();
        float y = bounds.Top - metrics.Ascent;
        canvas.Save();
        canvas.ClipRect(bounds);
        for (int i = _speechScroll; i < lines.Length && visible.Count < _speechVisibleLines; i++)
        {
            canvas.DrawText(lines[i], bounds.Left, y, paint);
            visible.Add(lines[i]);
            y += lineHeight;
        }
        canvas.Restore();
        _visibleSpeechLines = visible.ToImmutable();
        // Overflow markers sit in the field padding, outside the text clip, at the right edge.
        if (SpeechHasMoreAbove) DrawMarker(canvas, bounds.Right - 6, bounds.Top - 11, bounds.Top - 3, paint.Color);
        if (SpeechHasMoreBelow) DrawMarker(canvas, bounds.Right - 6, bounds.Bottom + 11, bounds.Bottom + 3, paint.Color);
    }
    private static void DrawMarker(SKCanvas canvas, float midX, float tipY, float baseY, SKColor color)
    {
        using var path = new SKPath();
        path.MoveTo(midX - 6, baseY);
        path.LineTo(midX + 6, baseY);
        path.LineTo(midX, tipY);
        path.Close();
        using var fill = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawPath(path, fill);
    }
}
