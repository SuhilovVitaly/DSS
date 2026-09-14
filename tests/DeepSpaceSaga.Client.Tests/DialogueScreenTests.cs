using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Dialogue;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class DialogueScreenTests
{
    private static DialogueState State(bool canAbort = true) => new("instance", "dialogue.station-docking", "station", "operator", "request_ship_id", 0, canAbort,
        [new("truthful_id", "Dialogue.Docking.AnswerTruthfulId", true), new("false_id", "Dialogue.Docking.AnswerFalseId", true)],
        "Dialogue.Docking.RequestShipId", "Dock Operator", "Operator", null, ImmutableDictionary<string, string>.Empty);
    private static AuthoritativeSnapshot Snapshot(DialogueState? state, ulong sequence = 1) =>
        new(sequence, 0, SimulationSpeed.Speed0, [new("player", 0, 0, 0, 0)], "player", ActiveDialogue: state);
    private static void Render(DialogueScreen screen)
    {
        using var bitmap = new SKBitmap(1600, 900);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1600, 900);
    }
    [Fact]
    public async Task Double_click_waits_for_acknowledgement_and_rejection_unlocks_choices()
    {
        var connection = new RecordingConnection();
        await using var handle = new GameSessionHandle(connection);
        var state = State(); handle.Buffer.Update(Snapshot(state));
        var screen = new DialogueScreen(handle.Buffer, handle, state); Render(screen);
        var rect = DialogueLayout.Choice(0, 1600, 900);
        screen.OnMouseDown(rect.MidX, rect.MidY, MouseButton.Left);
        screen.OnMouseDown(rect.MidX, rect.MidY, MouseButton.Left);
        var sent = Assert.Single(connection.Commands);
        Assert.True(screen.IsPending);
        // An unrelated snapshot does not count as an acknowledgement.
        handle.Buffer.Update(Snapshot(state, 2)); screen.Poll(); Assert.True(screen.IsPending);
        handle.Buffer.Update(Snapshot(state, 3) with { DialogueEvents = [new("event", state.InstanceId,
            "dialogue_condition_not_met", 0, state.Parameters, sent.CommandId)] });
        screen.Poll(); Assert.False(screen.IsPending);
        Assert.Equal("dialogue_condition_not_met", screen.Error);
        screen.OnDeactivated();
    }
    [Fact]
    public async Task Disabled_choice_is_not_sent_and_escape_obeys_can_abort()
    {
        var connection = new RecordingConnection();
        await using var handle = new GameSessionHandle(connection);
        var state = State(false) with { Choices = [new("pay", "Dialogue.Docking.AcceptFee", false, "insufficient_player_credits")] };
        handle.Buffer.Update(Snapshot(state));
        var screen = new DialogueScreen(handle.Buffer, handle, state); Render(screen);
        var rect = DialogueLayout.Choice(0, 1600, 900);
        screen.OnMouseDown(rect.MidX, rect.MidY, MouseButton.Left);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape));
        Assert.Empty(connection.Commands);
        state = state with { CanAbort = true }; handle.Buffer.Update(Snapshot(state, 2));
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape));
        Assert.Equal(DialogueAction.Abort, Assert.Single(connection.Commands).Action);
        Assert.True(screen.IsPending);
        handle.Buffer.Update(Snapshot(null, 3));
        Assert.Equal(ScreenEvent.CloseDialogue, screen.Poll());
        screen.OnDeactivated();
    }
    [Fact]
    public void Dialogue_precedes_station_transition_and_executed_dock_alone_cannot_open_station()
    {
        var buffer = new SnapshotBuffer(); var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        buffer.Update(Snapshot(null) with { CommandResults = [new("dock", "player", "nav", NavigationComputerCommandTypes.Dock, CommandResultStatus.Executed, 0)] });
        Assert.Equal(ScreenEvent.None, screen.ConsumePendingAutoTransition());
        buffer.Update(Snapshot(State(), 2) with { Objects = [new("player", 0, 0, 0, 0, IsDocked: true)] });
        Assert.Equal(ScreenEvent.OpenDialogue, screen.ConsumePendingAutoTransition());
        buffer.Update(Snapshot(null, 3) with { Objects = [new("player", 0, 0, 0, 0, IsDocked: true)] });
        Assert.Equal(ScreenEvent.OpenStation, screen.ConsumePendingAutoTransition());
    }
    [Fact]
    public void Final_line_remains_visible_until_acknowledged()
    {
        var buffer = new SnapshotBuffer(); var state = State() with { CurrentNodeId = "pirate_warning", TextKey = "Dialogue.Docking.PirateWarning",
            Choices = [new("continue", "Dialogue.Continue", true)] };
        buffer.Update(Snapshot(state)); var screen = new DialogueScreen(buffer, null, state);
        Render(screen);
        Assert.Equal(ScreenEvent.None, screen.Poll());
        Assert.Equal("Dialogue.Docking.PirateWarning", screen.State.TextKey);
        screen.OnDeactivated();
    }
    [Fact]
    public void Renders_operator_and_captain_with_the_fee_offer()
    {
        var state = State() with { SpeakerDisplayName = "Mari Lefeber",
            SpeakerPortraitImage = "Images/Persons/W/CHR-20260901-232751-JUKCIQ.png",
            TextKey = "Dialogue.Docking.OfferPortFee", CurrentNodeId = "offer_port_fee",
            Parameters = ImmutableDictionary<string, string>.Empty.Add("portFeeCreditsPerDay", "100"),
            Choices = [new("accept_fee", "Dialogue.Docking.AcceptFee", true), new("refuse_fee", "Dialogue.Docking.RefuseFee", true)] };
        var buffer = new SnapshotBuffer();
        buffer.Update(Snapshot(state) with { Objects = [new("player", 0, 0, 0, 0,
            CaptainDisplayName: "Captain", CaptainPortraitImage: "Images/Persons/M/CHR-20260906-150900-IYUL3A.png")] });
        var screen = new DialogueScreen(buffer, null, state);
        using var bitmap = new SKBitmap(1600, 900); using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black); screen.Render(canvas, 1600, 900);
        Assert.Equal(2, screen.State.Choices.Length);
        if (Environment.GetEnvironmentVariable("DSS_DIALOGUE_PREVIEW") is { } path)
        {
            using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.Create(path); data.SaveTo(stream);
        }
        screen.OnDeactivated();
    }
    private sealed class RecordingConnection : IGameSessionConnection
    {
        public List<DialogueCommand> Commands { get; } = [];
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default)
        { Commands.Add(command); return ValueTask.CompletedTask; }
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await Task.CompletedTask; yield break; }
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
