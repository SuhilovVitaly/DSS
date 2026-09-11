using System.Collections.Immutable;
using System.Globalization;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Dialogue;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private readonly DialogueRuntime _dialogue = new();
    private readonly Queue<DialogueCommand> _pendingDialogueCommands = new();

    public void ReceiveDialogueCommand(DialogueCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        lock (_worldStateLock) _pendingDialogueCommands.Enqueue(command);
    }

    private void LoadDialogueState(DialogueSaveState? save, long time)
    {
        _pendingDialogueCommands.Clear();
        _dialogue.Load(save);
        foreach (var station in _objects)
            if (station.PortFeeCreditsPerDay < 0 || station.SecurityZoneRadiusKm <= 0 || station.PiracyWarningGracePeriodMs < 0)
                throw new ScenarioException("Invalid station docking/security configuration.");
        if (_dialogue.Active is { } active)
        {
            if (!_registry.Dialogues.Contains(active.DialogueDefinitionId))
                throw new ScenarioException("Unknown saved dialogue definition.");
            var definition = _registry.Dialogues.GetDefinition(_registry.Dialogues.GetIndex(active.DialogueDefinitionId));
            if (!definition.Nodes.Any(n => n.NodeId == active.CurrentNodeId) || active.Revision < 0 ||
                active.StationObjectId is not null && !_objects.Any(o => o.InitialMotion.ObjectId == active.StationObjectId))
                throw new ScenarioException("Invalid saved dialogue context.");
            _dialogue.ResumeSpeed ??= _clock.Speed;
            _clock.Reset(time, SimulationSpeed.Speed0);
        }
    }

    private string? StartDialogue(string definitionId, string? stationId, string participantId, string commandId, long time)
    {
        if (_dialogue.Active is not null) return "dialogue_active";
        if (!_registry.Dialogues.Contains(definitionId)) return "unknown_dialogue";
        var definition = _registry.Dialogues.GetDefinition(_registry.Dialogues.GetIndex(definitionId));
        var error = DialogueChoiceValidator.Validate(definition.Conditions, _dialogue.Progress);
        if (error is not null) return error;
        var node = definition.Node(definition.EntryNodeId);
        var station = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == stationId);
        var crew = station?.StationCrew.FirstOrDefault(c => c.Id == participantId);
        string instance = $"dialogue-{++_dialogue.NextInstanceId}";
        var parameters = ImmutableDictionary<string, string>.Empty;
        if (station?.PortFeeCreditsPerDay is { } fee)
            parameters = parameters.Add("portFeeCreditsPerDay", fee.ToString(CultureInfo.InvariantCulture));
        _dialogue.Active = new(instance, definitionId, stationId, participantId, node.NodeId, 0,
            definition.CanAbort, [], node.TextKey, node.SpeakerRole, crew?.DisplayName ?? participantId,
            crew?.PortraitImage, parameters);
        _dialogue.ResumeSpeed = _clock.Speed;
        // Freeze exactly at the command's authoritative time, including on save-before-snapshot.
        _clock.Reset(time, SimulationSpeed.Speed0);
        _dialogue.Emit(instance, "dialogue_started", time, commandId);
        return null;
    }

    private string? ValidateDialogueDock(DialogueState active, long time)
    {
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        var nav = ship?.Modules.FirstOrDefault(m => IsNavigationCommandType(
            _registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex), NavigationComputerCommandTypes.Dock));
        if (ship is null || nav is null) return CommandReasonCodes.ModuleUnavailable;
        var outcome = TryStartNavigationCommand(new("dialogue-validation", 0, PlayerShipObjectId!, nav.ModuleId,
            NavigationComputerCommandTypes.Dock, TargetObjectId: active.StationObjectId), time, validateOnly: true);
        return outcome.Disposition == CommandStartDisposition.Rejected ? outcome.ReasonCode : null;
    }

    private string? PrepareChoice(DialogueState active, DialogueChoice choice, long time, out DialogueEffectResult? result)
    {
        result = null;
        var error = DialogueChoiceValidator.Validate(choice.Conditions, _dialogue.Progress);
        return error ?? DialogueEffectTransaction.Prepare(_registry, _objects.ToImmutableArray(), PlayerShipObjectId!,
            active, _dialogue.Progress, PlayerCredits, time, choice.Effects,
            () => ValidateDialogueDock(active, time), out result);
    }

    private DialogueState? BuildDialogueSnapshot(long time)
    {
        if (_dialogue.Active is not { } active) return null;
        var definition = _registry.Dialogues.GetDefinition(_registry.Dialogues.GetIndex(active.DialogueDefinitionId));
        var node = definition.Node(active.CurrentNodeId);
        var station = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == active.StationObjectId);
        var speaker = station?.StationCrew.FirstOrDefault(c => c.Role == node.SpeakerRole);
        var captain = node.SpeakerRole == "Captain" ? _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId) : null;
        return active with {
            SpeakerDisplayName = captain?.CaptainDisplayName ?? speaker?.DisplayName ?? active.SpeakerDisplayName,
            SpeakerPortraitImage = captain?.CaptainPortraitImage ?? speaker?.PortraitImage ?? active.SpeakerPortraitImage,
            Choices = node.Choices.IsEmpty
                ? [new DialogueChoiceSnapshot("continue", "Dialogue.Continue", true)]
                : node.Choices.Select(choice =>
        {
            var error = PrepareChoice(active, choice, time, out _);
            return new DialogueChoiceSnapshot(choice.ChoiceId, choice.TextKey, error is null, error);
        }).ToImmutableArray() };
    }

    private void ApplyPendingDialogueCommands(long time)
    {
        while (_pendingDialogueCommands.TryDequeue(out var command))
        {
            if (string.IsNullOrWhiteSpace(command.CommandId)) continue;
            if (_dialogue.ProcessedCommandIds.Contains(command.CommandId))
            {
                _dialogue.Emit(command.DialogueInstanceId, "dialogue_duplicate", time, command.CommandId);
                continue;
            }
            _dialogue.ProcessedCommandIds = _dialogue.ProcessedCommandIds.Add(command.CommandId);
            var error = ApplyDialogueCommand(command, time);
            if (error is not null) _dialogue.Emit(command.DialogueInstanceId, error, time, command.CommandId);
        }
    }

    private string? ApplyDialogueCommand(DialogueCommand command, long time)
    {
        if (command.Action == DialogueAction.Start)
        {
            if (command.DialogueDefinitionId is not { } id || !_registry.Dialogues.Contains(id)) return "unknown_dialogue";
            var definition = _registry.Dialogues.GetDefinition(_registry.Dialogues.GetIndex(id));
            // Docking can only start through the fully validated navigation command.
            if (!definition.AllowManualStart || id == "dialogue.station-docking") return "dialogue_start_not_allowed";
            if (string.IsNullOrWhiteSpace(command.ParticipantId) || !_objects.Any(o =>
                o.InitialMotion.ObjectId == command.ParticipantId ||
                !o.StationCrew.IsDefaultOrEmpty && o.StationCrew.Any(c => c.Id == command.ParticipantId) ||
                !o.Crew.IsDefaultOrEmpty && o.Crew.Any(c => c.Id == command.ParticipantId))) return "unknown_participant";
            var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
            if (ship is null || ship.IsDestroyed) return "player_destroyed";
            if (command.StationObjectId is { } stationId && !_objects.Any(o => o.InitialMotion.ObjectId == stationId && o.ObjectType == SpaceObjectType.Station)) return "unknown_station";
            return StartDialogue(id, command.StationObjectId, command.ParticipantId, command.CommandId, time);
        }
        if (_dialogue.Active is not { } active || command.DialogueInstanceId != active.InstanceId) return "dialogue_instance_mismatch";
        if (command.ExpectedRevision != active.Revision) return "dialogue_revision_mismatch";
        if (command.Action == DialogueAction.Abort)
        {
            if (!active.CanAbort) return "dialogue_abort_not_allowed";
            EndDialogue(time, command.CommandId, "dialogue_aborted");
            return null;
        }
        if (command.Action != DialogueAction.Choose) return "unknown_dialogue_action";
        var definitionForChoice = _registry.Dialogues.GetDefinition(_registry.Dialogues.GetIndex(active.DialogueDefinitionId));
        var currentNode = definitionForChoice.Node(active.CurrentNodeId);
        if (currentNode.Choices.IsEmpty && command.ChoiceId == "continue")
        {
            EndDialogue(time, command.CommandId, "dialogue_completed");
            return null;
        }
        var choice = currentNode.Choices.FirstOrDefault(c => c.ChoiceId == command.ChoiceId);
        if (choice is null) return "unknown_dialogue_choice";
        if (active.Revision == long.MaxValue) return "dialogue_value_overflow";
        var error = PrepareChoice(active, choice, time, out var result);
        if (error is not null) return error;
        _objects.Clear();
        _objects.AddRange(result!.Objects);
        PlayerCredits = result.Credits;
        _dialogue.Progress = result.Progress;
        if (choice.NextNodeId is null || result.EndDialogue)
        {
            EndDialogue(time, command.CommandId, "dialogue_completed");
            return null;
        }
        var node = definitionForChoice.Node(choice.NextNodeId);
        _dialogue.Active = active with { CurrentNodeId = node.NodeId, TextKey = node.TextKey,
            SpeakerRole = node.SpeakerRole, Revision = active.Revision + 1 };
        // Terminal nodes expose an acknowledgement so the final line stays visible while time remains paused.
        _dialogue.Emit(active.InstanceId, "dialogue_advanced", time, command.CommandId, node.TextKey, active.Parameters);

        return null;
    }

    private void EndDialogue(long time, string? commandId, string code, string? textKey = null)
    {
        var active = _dialogue.Active!;
        _dialogue.Emit(active.InstanceId, code, time, commandId, textKey, active.Parameters);
        _dialogue.Active = null;
        var speed = _dialogue.ResumeSpeed ?? SimulationSpeed.Speed0;
        _dialogue.ResumeSpeed = null;
        _clock.Reset(time, speed);
    }

    private void UpdateStationSecurity(long time)
    {
        _dialogue.Progress = StationSecuritySystem.Update(_objects, PlayerShipObjectId, _dialogue.Progress, time);
    }
}
