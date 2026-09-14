using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Dialogue;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public class DialogueTests
{
    internal static string ContentPath
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DeepSpaceSaga.sln"))) dir = dir.Parent;
            return Path.Combine(dir!.FullName, "src/DeepSpaceSaga.Client/Data/Dialogues");
        }
    }
    internal static AuthoritativeSnapshot Choose(SimulationEngine engine, string choice, string? id = null, long time = 0)
    {
        var active = engine.CaptureSnapshotForTests(time).ActiveDialogue!;
        engine.ReceiveDialogueCommand(new(id ?? Guid.NewGuid().ToString(), DialogueAction.Choose, active.InstanceId, active.Revision, choice));
        return engine.CaptureSnapshotForTests(time);
    }
    internal static AuthoritativeSnapshot PayAndFinish(SimulationEngine engine)
    {
        Choose(engine, "truthful_id");
        Choose(engine, "accept_fee");
        return Choose(engine, "continue");
    }
    internal static AuthoritativeSnapshot Start(SimulationEngine engine, string id = "dock")
    {
        engine.ReceiveCommand(new(id, 1, "SPC-0001", "MOD-NAV-01", NavigationComputerCommandTypes.Dock, TargetObjectId: "STATION-01"));
        return engine.CaptureSnapshotForTests();
    }
    [Fact]
    public void Request_starts_two_choices_with_operator_and_pauses_without_docking()
    {
        using var engine = DockCommandTests.CreateEngine();
        engine.SetSpeed(SimulationSpeed.Speed3);
        var snapshot = Start(engine);
        var active = Assert.IsType<DialogueState>(snapshot.ActiveDialogue);
        Assert.Equal(2, active.Choices.Length);
        Assert.Equal("Test Operator", active.SpeakerDisplayName);
        Assert.Equal("operator.png", active.SpeakerPortraitImage);
        Assert.Equal(SimulationSpeed.Speed0, engine.CurrentSpeed);
        Assert.False(snapshot.Objects.Single(o => o.ObjectId == "SPC-0001").IsDocked);
        engine.ReceiveDialogueCommand(new("abort", DialogueAction.Abort, active.InstanceId, active.Revision));
        snapshot = engine.CaptureSnapshotForTests();
        Assert.Null(snapshot.ActiveDialogue);
        Assert.Equal(SimulationSpeed.Speed3, snapshot.CurrentSpeed);
        Assert.NotNull(Start(engine, "dock-again").ActiveDialogue);
    }
    [Fact]
    public void Payment_is_once_even_after_duplicate_delivery_and_save_load()
    {
        using var engine = DockCommandTests.CreateEngine();
        Start(engine); Choose(engine, "truthful_id");
        var active = engine.CaptureSnapshotForTests().ActiveDialogue!;
        var pay = new DialogueCommand("pay-once", DialogueAction.Choose, active.InstanceId, active.Revision, "accept_fee");
        engine.ReceiveDialogueCommand(pay); engine.ReceiveDialogueCommand(pay);
        var snapshot = engine.CaptureSnapshotForTests();
        Assert.Equal(900, snapshot.PlayerCredits);
        Assert.True(snapshot.Objects.Single(o => o.ObjectId == "SPC-0001").IsDocked);
        Assert.Equal(10100, engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "STATION-01").Credits);
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()));
        using var loaded = new SimulationEngine(DockCommandTests.CreateRegistry(200));
        loaded.LoadScenario(save);
        loaded.ReceiveDialogueCommand(pay);
        Assert.Equal(900, loaded.CaptureSnapshotForTests().PlayerCredits);
        Assert.Null(Choose(loaded, "continue").ActiveDialogue);
    }
    [Theory]
    [InlineData("refuse_fee", 1000)]
    [InlineData("accept_fee", 99)]
    public void Refusal_or_insufficient_funds_cannot_charge_or_dock(string choice, long credits)
    {
        using var engine = DockCommandTests.CreateEngine(playerCredits: credits);
        Start(engine); Choose(engine, "truthful_id");
        var snapshot = Choose(engine, choice);
        Assert.Equal(credits, snapshot.PlayerCredits);
        Assert.False(snapshot.Objects.Single(o => o.ObjectId == "SPC-0001").IsDocked);
        if (choice == "accept_fee")
        {
            Assert.Equal("offer_port_fee", snapshot.ActiveDialogue!.CurrentNodeId);
            Assert.Contains(snapshot.DialogueEvents, e => e.EventCode == CommandReasonCodes.InsufficientPlayerCredits);
            Assert.False(snapshot.ActiveDialogue.Choices.Single(c => c.ChoiceId == "accept_fee").Enabled);
        }
        else Assert.Null(Choose(engine, "continue").ActiveDialogue);
    }
    [Fact]
    public void Late_effect_failure_rolls_back_credits_and_docking()
    {
        using var engine = DockCommandTests.CreateEngine(stationCredits: long.MaxValue);
        Start(engine); Choose(engine, "truthful_id");
        var snapshot = Choose(engine, "accept_fee");
        Assert.Equal(1000, snapshot.PlayerCredits);
        Assert.False(snapshot.Objects.Single(o => o.ObjectId == "SPC-0001").IsDocked);
        Assert.Contains(snapshot.DialogueEvents, e => e.EventCode == "dialogue_value_overflow");
    }
    [Fact]
    public void Stale_revision_and_wrong_instance_do_not_apply_effects()
    {
        using var engine = DockCommandTests.CreateEngine();
        var active = Start(engine).ActiveDialogue!;
        Choose(engine, "truthful_id");
        engine.ReceiveDialogueCommand(new("stale", DialogueAction.Choose, active.InstanceId, active.Revision, "accept_fee"));
        engine.ReceiveDialogueCommand(new("wrong", DialogueAction.Abort, "wrong-instance", 1));
        var snapshot = engine.CaptureSnapshotForTests();
        Assert.Equal(1000, snapshot.PlayerCredits);
        Assert.Equal("offer_port_fee", snapshot.ActiveDialogue!.CurrentNodeId);
        Assert.Contains(snapshot.DialogueEvents, e => e.EventCode == "dialogue_revision_mismatch");
        Assert.Contains(snapshot.DialogueEvents, e => e.EventCode == "dialogue_instance_mismatch");
    }
    [Fact]
    public void Active_dialogue_round_trips_and_resumes_original_speed()
    {
        using var engine = DockCommandTests.CreateEngine();
        engine.SetSpeed(SimulationSpeed.Speed2);
        Start(engine); Choose(engine, "truthful_id");
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()));
        using var loaded = new SimulationEngine(DockCommandTests.CreateRegistry(200));
        loaded.LoadScenario(save);
        Assert.Equal("offer_port_fee", loaded.CaptureSnapshotForTests().ActiveDialogue!.CurrentNodeId);
        Assert.Equal(SimulationSpeed.Speed0, loaded.CurrentSpeed);
        Choose(loaded, "refuse_fee");
        Assert.Equal(SimulationSpeed.Speed2, Choose(loaded, "continue").CurrentSpeed);
    }
    [Fact]
    public void False_id_denies_access_and_destroys_ship_at_deadline_after_save_load()
    {
        using var engine = DockCommandTests.CreateEngine();
        Start(engine); var warning = Choose(engine, "false_id");
        Assert.False(warning.Objects.Single(o => o.ObjectId == "SPC-0001").IsDocked);
        Choose(engine, "continue");
        var save = engine.CaptureSaveState();
        Assert.True(save.GameState.DialogueState!.Progress.StationAccessStates["STATION-01"].AccessDenied);
        using var loaded = new SimulationEngine(DockCommandTests.CreateRegistry(200));
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save)));
        Assert.Equal("station_access_denied", Assert.Single(Start(loaded, "retry").CommandResults).ReasonCode);
        Assert.False(loaded.CaptureSnapshotForTests(59999).Objects.Single(o => o.ObjectId == "SPC-0001").IsDestroyed);
        Assert.True(loaded.CaptureSnapshotForTests(60000).Objects.Single(o => o.ObjectId == "SPC-0001").IsDestroyed);
        var destroyed = loaded.CaptureSaveStateForTests(60000, SimulationSpeed.Speed0);
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(destroyed), allowNonZeroGameTime: true));
        Assert.True(loaded.CaptureSnapshotForTests(60000).Objects.Single(o => o.ObjectId == "SPC-0001").IsDestroyed);
    }
    [Fact]
    public void Leaving_zone_completes_incident_but_access_stays_denied()
    {
        using var engine = DockCommandTests.CreateEngine();
        Start(engine); Choose(engine, "false_id"); Choose(engine, "continue");
        var save = engine.CaptureSaveState();
        // Flight state after leaving; persistence must not reactivate the warning on re-entry.
        save = save with { GameState = save.GameState with { SpaceObjects = save.GameState.SpaceObjects.Select(o =>
            o.ObjectId == "SPC-0001" ? o with { PositionX = 20000 } : o).ToArray() } };
        engine.LoadScenario(save);
        Assert.False(engine.CaptureSnapshotForTests(1000).Objects.Single(o => o.ObjectId == "SPC-0001").IsDestroyed);
        var escaped = engine.CaptureSaveStateForTests(1000, SimulationSpeed.Speed0);
        Assert.True(Assert.Single(escaped.GameState.DialogueState!.Progress.SecurityIncidents).Completed);
        Assert.True(escaped.GameState.DialogueState.Progress.StationAccessStates["STATION-01"].AccessDenied);
        Assert.False(engine.CaptureSnapshotForTests(70000).Objects.Single(o => o.ObjectId == "SPC-0001").IsDestroyed);
    }
    [Fact]
    public void Docking_cannot_be_started_through_unvalidated_dialogue_start()
    {
        using var engine = DockCommandTests.CreateEngine();
        engine.ReceiveDialogueCommand(new("start", DialogueAction.Start, "", 0,
            DialogueDefinitionId: "dialogue.station-docking", ParticipantId: "operator", StationObjectId: "STATION-01"));
        Assert.Null(engine.CaptureSnapshotForTests().ActiveDialogue);
    }
    [Fact]
    public void Dock_effect_revalidates_loaded_context_and_rolls_back_the_entire_fee_transfer()
    {
        using var engine = DockCommandTests.CreateEngine(); Start(engine); Choose(engine, "truthful_id");
        var save = engine.CaptureSaveState();
        save = save with { GameState = save.GameState with { SpaceObjects = save.GameState.SpaceObjects.Select(o =>
            o.ObjectId == "SPC-0001" ? o with { PositionX = 50000 } : o).ToArray() } };
        engine.LoadScenario(save);
        var snapshot = Choose(engine, "accept_fee");
        Assert.Equal(1000, snapshot.PlayerCredits);
        Assert.Equal(10000, engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "STATION-01").Credits);
        Assert.False(snapshot.Objects.Single(o => o.ObjectId == "SPC-0001").IsDocked);
        Assert.Contains(snapshot.DialogueEvents, e => e.EventCode == CommandReasonCodes.DockOutOfRange);
    }
    [Fact]
    public void Saving_pending_payment_and_completion_applies_each_once_and_persists_resume_speed()
    {
        using var engine = DockCommandTests.CreateEngine(); engine.SetSpeed(SimulationSpeed.Speed2);
        Start(engine); Choose(engine, "truthful_id");
        var active = engine.CaptureSnapshotForTests().ActiveDialogue!;
        var payment = new DialogueCommand("pending-pay", DialogueAction.Choose, active.InstanceId, active.Revision, "accept_fee");
        engine.ReceiveDialogueCommand(payment);
        var paidSave = engine.CaptureSaveState();
        Assert.Equal(900, paidSave.GameState.PlayerTokens);
        using var loaded = new SimulationEngine(DockCommandTests.CreateRegistry(200)); loaded.LoadScenario(paidSave);
        loaded.ReceiveDialogueCommand(payment);
        var paid = loaded.CaptureSnapshotForTests().ActiveDialogue!;
        loaded.ReceiveDialogueCommand(new("pending-finish", DialogueAction.Choose, paid.InstanceId, paid.Revision, "continue"));
        var finishedSave = loaded.CaptureSaveState();
        Assert.Null(finishedSave.GameState.DialogueState!.ActiveDialogue);
        Assert.Equal("Speed2", finishedSave.GameState.CurrentSpeed);
        Assert.Equal(900, finishedSave.GameState.PlayerTokens);
    }
    [Fact]
    public void Escaping_after_deadline_does_not_evade_destruction_between_snapshots()
    {
        using var engine = DockCommandTests.CreateEngine(); Start(engine); Choose(engine, "false_id"); Choose(engine, "continue");
        var save = engine.CaptureSaveState();
        save = save with { GameState = save.GameState with { SpaceObjects = save.GameState.SpaceObjects.Select(o =>
            o.ObjectId == "SPC-0001" ? o with { SpeedMps = 3000, DirectionDegrees = 90, MovementType = "Linear" } : o).ToArray() } };
        engine.LoadScenario(save);
        // The ship travels 180 km at the deadline, 210 km by the next snapshot.
        Assert.True(engine.CaptureSnapshotForTests(70000).Objects.Single(o => o.ObjectId == "SPC-0001").IsDestroyed);
    }
}
