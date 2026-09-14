using System.Collections.Immutable;
using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests;

public class DialogueContractTests
{
    [Fact]
    public void Dialogue_snapshot_and_commands_round_trip_through_json()
    {
        var state = new DialogueState("instance", "definition", "station", "operator", "node", 7, true,
            [new("pay", "Dialogue.Pay", false, "insufficient_player_credits")], "Dialogue.Fee", "Dock Operator",
            "Operator", "portrait.png", ImmutableDictionary<string, string>.Empty.Add("fee", "100"));
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [], ActiveDialogue: state,
            DialogueEvents: [new("event", "instance", "dialogue_started", 0, state.Parameters, "command")],
            PlayerCharacter: new(ImmutableDictionary<string, int>.Empty.Add("engineering", 4)),
            Quests: [new("quest", "active", ImmutableDictionary<string, string>.Empty.Add("objective", "active"), ImmutableHashSet<string>.Empty)]);
        var loaded = JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal(7, loaded.ActiveDialogue!.Revision);
        Assert.Equal(state.Choices[0], Assert.Single(loaded.ActiveDialogue.Choices));
        Assert.Equal("command", Assert.Single(loaded.DialogueEvents).CommandId);
        Assert.Equal(4, loaded.PlayerCharacter!.Attributes["engineering"]);
        Assert.Equal("active", Assert.Single(loaded.Quests).ObjectiveStates["objective"]);
        var command = new DialogueCommand("command", DialogueAction.Choose, "instance", 7, "pay");
        Assert.Equal(command, JsonSerializer.Deserialize<DialogueCommand>(JsonSerializer.Serialize(command)));
        Assert.NotNull(typeof(IGameSessionConnection).GetMethod(nameof(IGameSessionConnection.SendDialogueCommandAsync)));
    }
}
