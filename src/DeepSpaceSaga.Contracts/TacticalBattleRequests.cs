using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

public sealed record TacticalLoadoutChoice(string DroneId, string GunId, string PlateId, string? ShieldId, int ShieldCount);
public sealed record StartTacticalBattleRequest(string CommandId, long Seed, string? ScenarioId = null,
    string? OwnTacticId = null, string? EnemyTacticId = null,
    [property: JsonConverter(typeof(TacticalArrayJsonConverter<TacticalLoadoutChoice>))] ImmutableArray<TacticalLoadoutChoice> Loadout = default);
public sealed record MoveTacticalBattleRequest(string CommandId, string BattleId, ulong ExpectedRevision,
    [property: JsonConverter(typeof(TacticalArrayJsonConverter<TacticalDroneOrder>))] ImmutableArray<TacticalDroneOrder> Orders = default);
public sealed record TacticalFireTarget(string DroneId, string? TargetId);
public sealed record FireTacticalBattleRequest(string CommandId, string BattleId, ulong ExpectedRevision,
    [property: JsonConverter(typeof(TacticalArrayJsonConverter<TacticalFireTarget>))] ImmutableArray<TacticalFireTarget> Targets = default);
public sealed record ExitTacticalBattleRequest(string CommandId, string BattleId);
public enum TacticalBattleRejectionCode
{
    None = 0, Unavailable = 1, NoBattle = 2, AlreadyActive = 3, WrongPhase = 4, WrongBattle = 5,
    StaleRevision = 6, InvalidIdentity = 7, IdentityConflict = 8, UnknownScenario = 9, UnknownTactic = 10,
    InvalidDrone = 11, DuplicateDrone = 12, InvalidNumber = 13, UnknownWeapon = 14, UnknownArmor = 15,
    UnknownShield = 16, InvalidShieldCount = 17, InvalidTarget = 18, InvalidRequest = 19, Unsupported = 20
}
/// <summary>NewEvents is a delta for this command. State.Log, when present, is the full journal.</summary>
public sealed record TacticalBattleResponse(string CommandId, bool Accepted, TacticalBattleRejectionCode Code,
    TacticalBattlePhase? CurrentPhase, TacticalBattleSnapshot? State, ImmutableArray<TacticalBattleLogEvent> NewEvents,
    TacticalContentIssue? ContentError = null);

/// <summary>Default arrays write as empty; explicit malformed/null arrays and null elements fail instead of becoming commands.</summary>
public sealed class TacticalArrayJsonConverter<T> : JsonConverter<ImmutableArray<T>>
{
    public override ImmutableArray<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected tactical command array.");
        var values = JsonSerializer.Deserialize<List<T>>(ref reader, options)!;
        if (values.Any(value => value is null)) throw new JsonException("Tactical command array elements must not be null.");
        return values.ToImmutableArray();
    }
    public override void Write(Utf8JsonWriter writer, ImmutableArray<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        if (!value.IsDefault)
            foreach (var item in value) JsonSerializer.Serialize(writer, item, options);
        writer.WriteEndArray();
    }
}
