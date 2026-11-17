using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0010Tk0003Tests
{
    [Fact]
    public void ReportReusesCrewPortraitAndDialogue_AcknowledgementStaysPausedAcrossLoad()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var scenario = DemoEp0035Us0010Tk0002Tests.ContactScenario(engine);
        var crew = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId).Crew!
            .Single(c => c.CrewId == "CHR-PLAYER-DEFENSE");
        engine.LoadScenario(scenario, true);
        engine.SetSpeed(SimulationSpeed.Speed3);
        var snapshot = engine.CaptureSnapshot();
        var dialogue = Assert.IsType<DialogueState>(snapshot.ActiveDialogue);
        Assert.Equal("dialogue.sensor-pirate-contact", dialogue.DialogueDefinitionId);
        Assert.Equal(crew.DisplayName, dialogue.SpeakerDisplayName);
        Assert.Equal(crew.PortraitAssetId, dialogue.SpeakerPortraitImage);
        Assert.Equal(SimulationSpeed.Speed0, snapshot.CurrentSpeed);
        Assert.Equal("sensor_report_pending", engine.TrySetSpeed(SimulationSpeed.Speed1));
        var save = engine.CaptureSaveState();
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
        Assert.Equal(dialogue.InstanceId, engine.CaptureSnapshot().ActiveDialogue!.InstanceId);
        engine.ReceiveDialogueCommand(new("ack-sensor", DialogueAction.Choose, dialogue.InstanceId, dialogue.Revision, "acknowledge"));
        snapshot = engine.CaptureSnapshot();
        Assert.Null(snapshot.ActiveDialogue);
        Assert.Null(snapshot.DemoCampaign!.PendingSensorReport);
        Assert.Equal(SimulationSpeed.Speed0, snapshot.CurrentSpeed);
        Assert.Null(engine.CaptureSnapshot().ActiveDialogue);
        Assert.Null(engine.TrySetSpeed(SimulationSpeed.Speed1));
    }

    [Fact]
    public void MissingAssignmentUsesScriptWithoutPortrait_AndExistingDialogueIsNotOverwritten()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        engine.LoadScenario(DemoEp0035Us0010Tk0002Tests.ContactScenario(engine, script: true), true);
        var snapshot = engine.CaptureSnapshot();
        Assert.Equal("sensor-script", snapshot.ActiveDialogue!.ParticipantId);
        Assert.Null(snapshot.ActiveDialogue.SpeakerPortraitImage);
        Assert.Null(snapshot.DemoCampaign!.PendingSensorReport!.Reports[0].OperatorCrewId);
    }
}
