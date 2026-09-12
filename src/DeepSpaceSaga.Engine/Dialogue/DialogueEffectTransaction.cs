using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Dialogue;

internal sealed record DialogueEffectResult(ImmutableArray<SpaceObjectRuntime> Objects,
    long Credits, DialogueProgressState Progress, bool EndDialogue);

/// <summary>Builds a detached candidate world. Failure discards it; the engine commits it under its world lock.</summary>
internal static class DialogueEffectTransaction
{
    public static string? Prepare(GameDataRegistry registry, ImmutableArray<SpaceObjectRuntime> objects,
        string playerId, DialogueState dialogue, DialogueProgressState progress, long credits, long time,
        ImmutableArray<DialogueEffect> effects, Func<string?> validateDock, out DialogueEffectResult? result)
    {
        result = null;
        var candidate = objects.ToBuilder();
        int shipIndex = Array.FindIndex(objects.ToArray(), o => o.InitialMotion.ObjectId == playerId);
        int stationIndex = Array.FindIndex(objects.ToArray(), o => o.InitialMotion.ObjectId == dialogue.StationObjectId && o.ObjectType == SpaceObjectType.Station);
        if (shipIndex < 0 || objects[shipIndex].IsDestroyed) return "player_destroyed";
        bool end = false;
        try
        {
            checked
            {
                foreach (var effect in effects.IsDefault ? [] : effects)
                {
                    long amount = effect.Amount;
                    if (effect.AmountSource == "station.portFeeCreditsPerDay")
                    {
                        if (stationIndex < 0 || candidate[stationIndex].PortFeeCreditsPerDay is not { } fee) return "station_fee_unavailable";
                        amount = fee;
                    }
                    if ((effect.Type.Contains("Station", StringComparison.Ordinal) || effect.Type == "ArmSecurityIncident") && stationIndex < 0)
                        return "unknown_station";
                    switch (effect.Type)
                    {
                        case "AddCredits": credits += amount; break;
                        case "RemoveCredits":
                            if (credits < amount) return CommandReasonCodes.InsufficientPlayerCredits;
                            credits -= amount; break;
                        case "AddStationCredits":
                            candidate[stationIndex] = candidate[stationIndex] with { Credits = candidate[stationIndex].Credits + amount }; break;
                        case "ModifyCharacterAttribute":
                            var attrs = progress.PlayerCharacter.Attributes;
                            progress = progress with { PlayerCharacter = new(attrs.SetItem(effect.Attribute!, (int)(attrs.GetValueOrDefault(effect.Attribute!) + amount))) }; break;
                        case "SetFlag": progress = progress with { Flags = progress.Flags.Add(effect.Flag!) }; break;
                        case "ClearFlag": progress = progress with { Flags = progress.Flags.Remove(effect.Flag!) }; break;
                        case "StartQuest":
                            if (!registry.Quests.Contains(effect.QuestId!)) return "unknown_quest";
                            if (progress.Quests.ContainsKey(effect.QuestId!)) return "quest_already_started";
                            var definition = registry.Quests.GetDefinition(registry.Quests.GetIndex(effect.QuestId!));
                            progress = progress with { Quests = progress.Quests.Add(definition.QuestId, QuestStateStore.Start(definition)) }; break;
                        case "CompleteQuestObjective":
                        case "FailQuestObjective":
                            if (!progress.Quests.TryGetValue(effect.QuestId!, out var quest) || !quest.ObjectiveStates.ContainsKey(effect.ObjectiveId!)) return "unknown_quest_objective";
                            progress = progress with { Quests = progress.Quests.SetItem(quest.QuestId,
                                QuestStateStore.SetObjective(quest, effect.ObjectiveId!, effect.Type == "CompleteQuestObjective" ? "completed" : "failed")) }; break;
                        case "GrantStationAccess":
                        case "DenyStationAccess":
                            progress = progress with { StationAccessStates = progress.StationAccessStates.SetItem(dialogue.StationObjectId!,
                                new(dialogue.StationObjectId!, effect.Type == "DenyStationAccess")) }; break;
                        case "ArmSecurityIncident":
                            var station = candidate[stationIndex];
                            if (station.SecurityZoneRadiusKm is null || station.PiracyWarningGracePeriodMs is not { } grace) return "station_security_unavailable";
                            progress = progress with { SecurityIncidents = progress.SecurityIncidents.Add(
                                new($"{dialogue.InstanceId}-{dialogue.Revision}-{progress.SecurityIncidents.Length}", dialogue.StationObjectId!,
                                    effect.IncidentType!, time, time + grace)) }; break;
                        case "DockPlayerToStation":
                            if (progress.StationAccessStates.TryGetValue(dialogue.StationObjectId!, out var access) && access.AccessDenied) return "station_access_denied";
                            var dockError = validateDock();
                            if (dockError is not null) return dockError;
                            var target = candidate[stationIndex];
                            var motion = RuntimeMotion.At(target, time);
                            var ship = candidate[shipIndex];
                            candidate[shipIndex] = ship with
                            {
                                InitialMotion = ship.InitialMotion with { X = motion.X + 1, Y = motion.Y + 1, SpeedKmS = motion.SpeedKmS, Direction = motion.Direction },
                                StartGameTimeMs = time, IsDocked = true, DockedStationObjectId = dialogue.StationObjectId,
                                Modules = ship.Modules.Select(m => m with { ActiveCycle = null }).ToImmutableArray()
                            };
                            progress = progress with { StationAccessStates = progress.StationAccessStates.SetItem(dialogue.StationObjectId!, new(dialogue.StationObjectId!, false)) };
                            break;
                        case "AddCargoItem":
                        case "RemoveCargoItem":
                            if (!registry.ItemTypes.Contains(effect.ItemTypeId!)) return CommandReasonCodes.UnknownItemType;
                            int itemIndex = registry.ItemTypes.GetIndex(effect.ItemTypeId!);
                            var cargoShip = candidate[shipIndex];
                            var modules = cargoShip.Modules.ToBuilder();
                            long remaining = effect.Quantity;
                            for (int i = 0; i < modules.Count && remaining > 0; i++)
                            {
                                var module = modules[i];
                                var type = registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
                                if (type.CargoCapacityKg is not { } capacity) continue;
                                var cargo = module.Cargo.IsDefault ? ImmutableArray<CargoStackRuntime>.Empty : module.Cargo;
                                int stackIndex = Array.FindIndex(cargo.ToArray(), s => s.ItemTypeIndex == itemIndex);
                                long oldQuantity = stackIndex < 0 ? 0 : cargo[stackIndex].Quantity;
                                long mass = 0;
                                foreach (var stack in cargo) mass += stack.Quantity * registry.ItemTypes.GetDefinition(stack.ItemTypeIndex).UnitMassKg;
                                long unitMass = registry.ItemTypes.GetDefinition(itemIndex).UnitMassKg;
                                long changed = effect.Type == "RemoveCargoItem" ? Math.Min(oldQuantity, remaining)
                                    : Math.Min(remaining, unitMass == 0 ? remaining : Math.Max(0, capacity - mass) / unitMass);
                                long quantity = effect.Type == "RemoveCargoItem" ? oldQuantity - changed : oldQuantity + changed;
                                if (changed == 0) continue;
                                cargo = stackIndex < 0 ? cargo.Add(new(itemIndex, quantity)) : quantity == 0 ? cargo.RemoveAt(stackIndex) : cargo.SetItem(stackIndex, new(itemIndex, quantity));
                                long newMass = effect.Type == "RemoveCargoItem" ? mass - changed * unitMass : mass + changed * unitMass;
                                modules[i] = module with { Cargo = cargo, AvailableCapacityKg = capacity - newMass };
                                remaining -= changed;
                            }
                            if (remaining > 0) return effect.Type == "AddCargoItem" ? CommandReasonCodes.CargoCapacityExceeded : CommandReasonCodes.InsufficientCargoQuantity;
                            candidate[shipIndex] = cargoShip with { Modules = modules.ToImmutable() }; break;
                        case "EndDialogue": end = true; break;
                        default: return "unknown_dialogue_effect";
                    }
                }
            }
        }
        catch (OverflowException) { return "dialogue_value_overflow"; }
        result = new(candidate.ToImmutable(), credits, progress, end);
        return null;
    }
}
