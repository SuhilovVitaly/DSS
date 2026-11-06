using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using DeepSpaceSaga.Engine.Rng;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Reads only discarded Save18/19 facts. No archived object can enter the world runtime through this reader.</summary>
internal static class LegacyCombatSaveReader
{
    private static readonly HashSet<string> Sections = new(StringComparer.Ordinal)
    {
        "combatState", "defenseState", "hostileCombatState", "combatReportsState", "combatReactionDelays", "pointCombatState"
    };

    internal static bool HasSections(JsonElement state) =>
        state.ValueKind == JsonValueKind.Object && state.EnumerateObject().Any(p => Sections.Contains(p.Name));

    internal static string ExtractWorld(JsonElement root, int version, out HashSet<string> actors)
    {
        Object(root);
        var world = Object(Field(root, "gameState"));
        long time = Int(world, "simulationTimeMs");
        ulong seed = UInt(world, "masterSeed");
        var objects = UniqueRows(world, "spaceObjects", "objectId");
        string player = Id(world, "playerShipObjectId");
        var combat = Object(Field(world, "combatState"));
        var defense = Object(Field(world, "defenseState"));
        var hostile = Object(Field(world, "hostileCombatState"));
        Require(Int(combat, "version") == 2 && Int(combat, "lastProcessedMotionTimeMs") == time &&
            Int(combat, "impactSequence") == 0 && Int(combat, "nextGuidanceMotionTimeMs") == long.MaxValue &&
            Int(combat, "wreckSequence") is >= 0 and < long.MaxValue, "combat envelope");
        _ = UInt(combat, "hitLocationRngState");
        Require(Int(defense, "Version") == 3 && Int(defense, "RngVersion") == 1 &&
            UInt(defense, "RngState") == 0 && UInt(defense, "RngCounter") == 0 && !Present(defense, "SelectedProjectileId"),
            "defense envelope");
        Require(Int(hostile, "Version") == 3 && Int(hostile, "NextDecisionMotionTimeMs") >= 0, "hostile envelope");
        Require(!Rows(combat, "projectiles").Any() && !Rows(defense, "Projectiles").Any() &&
            (!Present(combat, "processedModeCommandIds") || !Rows(combat, "processedModeCommandIds").Any()) &&
            (!Present(world, "combatReactionDelays") || !Rows(world, "combatReactionDelays").Any()), "retired guided/reaction continuation");
        foreach (string field in new[] { "torpedoHitRngState", "torpedoHitRngCounter", "launcherBreakdownRngState", "launcherBreakdownRngCounter" })
            Require(!Present(combat, field) || UInt(combat, field) == 0, "retired RNG: " + field);
        _ = Ids(combat, "processedLaunchCommandIds");
        var torpedoModules = Launchers(combat, "launchers", "ownerObjectId", "moduleId", "state", objects);
        var defenseModules = Launchers(defense, "Launchers", "OwnerObjectId", "ModuleId", "State", objects);

        actors = new(StringComparer.OrdinalIgnoreCase);
        foreach (var row in UniqueRows(hostile, "Actors", "ObjectId"))
        {
            Require(objects.TryGetValue(row.Key, out var owner) && TypeIs(owner, "NpcShip") &&
                string.Equals(Id(owner, "relationToPlayer"), "enemy", StringComparison.OrdinalIgnoreCase), "hostile owner");
            var state = Object(Field(row.Value, "State"));
            var segment = Object(Field(state, "Segment"));
            long epoch = Int(segment, "EpochMs"), elapsed = Int(segment, "ElapsedMs");
            Require(Int(state, "Version") == 1 && Int(state, "Phase") is >= 0 and <= 4 &&
                Same(Id(state, "TargetObjectId"), player) && Num(state, "DesiredRadiusKm") > 0 &&
                torpedoModules.Contains(Key(row.Key, Id(state, "WeaponModuleId"))) &&
                HasModule(owner, Id(state, "EngineModuleId")), "hostile steering");
            Require(Int(segment, "Version") == 1 && Int(segment, "DurationMs") == 100 &&
                epoch >= 0 && epoch <= time && elapsed is >= 0 and <= 100 && Num(segment, "StartSpeedKmS") >= 0,
                "hostile segment");
            Finite(segment, "StartX", "StartY", "StartDirectionDegrees", "AccelerationKmS2", "TurnRateDegPerSec");
            if (Bool(row.Value, "ActiveMotion"))
                Require(elapsed == time - epoch && epoch % 100 == 0 && epoch <= long.MaxValue - 100 &&
                    Int(hostile, "NextDecisionMotionTimeMs") == epoch + 100 && epoch + 100 > time, "hostile cursor");
            actors.Add(row.Key);
        }

        var launches = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        var destroyedActors = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        long previousId = 0;
        double previousTime = 0;
        foreach (var row in Rows(defense, "Journal"))
        {
            Object(row);
            long id = Int(row, "EventId"), kind = Int(row, "Type");
            double factTime = Num(row, "MotionTimeMs");
            Require(id > previousId && factTime >= previousTime && factTime <= time &&
                kind is 0 or 2 or 3 or 9 or 10 or 11 or 14 or 15 or 16 or 17 or 18, "journal order or type");
            Finite(row, "X", "Y");
            if (kind == 0)
                Require(Id(row, "ActorObjectId").Length > 0 && Id(row, "ProfileId").Length > 0 &&
                    Int(row, "ProjectileKind") is 0 or 1 && Num(row, "BlastRadiusKm") > 0 &&
                    launches.TryAdd(Id(row, "ProjectileObjectId"), row), "duplicate or invalid launch fact");
            if (kind == 11) destroyedActors[Id(row, "TargetObjectId")] = factTime;
            previousId = id;
            previousTime = factTime;
        }
        Require(previousId == Int(defense, "JournalSequence"), "journal sequence");
        long torpedoSequence = Int(combat, "projectileSequence"), counterSequence = Int(defense, "ProjectileSequence");
        Require(torpedoSequence >= 0 && counterSequence >= 0, "projectile sequences");

        if (version == 18)
            Require(!Present(world, "debugTuning"), "debug tuning/provenance requires Save19.");
        if (Present(world, "combatReportsState"))
        {
            var report = Object(Field(world, "combatReportsState"));
            Require(report.EnumerateObject().Count() == 3 && Int(report, "version") == 1 && Int(report, "sequence") >= 0,
                "report envelope");
            _ = Ids(report, "reportedRangeTargetIds");
        }

        if (!Present(world, "pointCombatState"))
        {
            Require(torpedoModules.Count == 0 && defenseModules.Count == 0 && launches.Count == 0 &&
                objects.Values.All(o => !TypeIs(o, "Missile") && !TypeIs(o, "Countermeasure") &&
                    (!Present(o, "modules") || Rows(o, "modules").All(m =>
                        !Id(m, "moduleTypeId").StartsWith("module.torpedo.", StringComparison.OrdinalIgnoreCase) &&
                        !Id(m, "moduleTypeId").StartsWith("module.countermeasure.", StringComparison.OrdinalIgnoreCase)))),
                "missing pointCombatState");
            return WriteWorld(root, world);
        }
        var point = Object(Field(world, "pointCombatState"));
        Require(Int(point, "Version") == 1 && Int(point, "PlayerEvasionEpisodes") >= 0, "point combat envelope");
        ulong scatter = Rng(point, "LaunchScatter"), dodge = Rng(point, "NpcDodge");
        Require(scatter == (ulong)launches.Count * 2 && UInt(Field(point, "LaunchScatter"), "State") ==
            unchecked(RngStreamSeedDerivation.DeriveStreamSeed(seed, "launch_scatter") + scatter * 0x9E3779B97F4A7C15UL),
            "launch scatter cursor");
        var flights = UniqueRows(point, "Projectiles", "ObjectId");
        foreach (var (id, row) in flights)
        {
            var flight = Object(Field(row, "Flight"));
            long kind = Int(flight, "Kind");
            string owner = Id(flight, "OwnerObjectId");
            Require(kind is 0 or 1 && !Same(owner, id) && objects.TryGetValue(id, out var obj) &&
                TypeIs(obj, kind == 0 ? "Missile" : "Countermeasure") &&
                Sequence(id, kind == 0 ? "torpedo-" : "countermeasure-", kind == 0 ? torpedoSequence : counterSequence),
                "projectile identity or sequence");
            var pose = objects[id];
            ValidateFlight(flight, time, pose);
            var original = flight;
            if (Present(row, "LauncherModuleTypeId") || Present(row, "OriginalFlight"))
                Require(version >= 19 && Id(row, "LauncherModuleTypeId").Length > 0, "debug tuning/provenance requires Save19.");
            if (Present(row, "OriginalFlight"))
            {
                original = Object(Field(row, "OriginalFlight"));
                ValidateFlight(original, time, null);
                foreach (string field in new[] { "Kind", "OwnerObjectId", "LauncherModuleId", "ProfileId", "DetonationX", "DetonationY",
                    "Damage", "ArmorPiercingPercent", "ArmorDamagePercent", "CoreFraction", "OwnerPlayerRelationAtLaunch" })
                    Require(EqualPrimitive(Field(original, field), Field(flight, field)), "flight provenance: " + field);
                Require(Int(original, "LaunchMotionTimeMs") <= Int(flight, "LaunchMotionTimeMs"), "flight provenance time");
            }
            Require(launches.TryGetValue(id, out var launch) && Same(owner, Id(launch, "ActorObjectId")) &&
                Int(launch, "ProjectileKind") == kind && Id(launch, "ProfileId") == Id(original, "ProfileId") &&
                Num(launch, "BlastRadiusKm") == Num(original, "BlastRadiusKm") &&
                Num(launch, "MotionTimeMs") == Int(original, "LaunchMotionTimeMs") &&
                Near(Num(launch, "X"), Num(original, "StartX")) && Near(Num(launch, "Y"), Num(original, "StartY")),
                "flight does not match launch fact");
            bool own = Same(owner, player);
            string relation = Id(flight, "OwnerPlayerRelationAtLaunch");
            Require(relation is "self" or "enemy" or "friend" or "neutral" && own == (relation == "self") &&
                (own ? Present(flight, "Reporter") && Int(Field(flight, "Reporter"), "Role") == (kind == 0 ? 1 : 2)
                     : !Present(flight, "Reporter")), "flight relation or reporter");
        }
        Require(objects.All(p => !TypeIs(p.Value, "Missile") && !TypeIs(p.Value, "Countermeasure") || flights.ContainsKey(p.Key)),
            "world projectile without flight");

        foreach (var (id, row) in UniqueRows(point, "Evasions", "ObjectId"))
        {
            Require(objects.TryGetValue(id, out var ship) && HasModule(ship, Id(row, "EngineModuleId")), "evasion owner or module");
            long started = Int(row, "StartedAt");
            var motion = Object(Field(row, "Motion"));
            var basis = Object(Field(motion, "BaseMotion"));
            double end = Num(motion, "EndsAtMotionTimeMs");
            var steering = Present(motion, "Steering") ? Object(Field(motion, "Steering")) : default;
            double returns = steering.ValueKind == JsonValueKind.Object ? Num(steering, "ReturnsAtMotionTimeMs") : end;
            Require(started >= 0 && started <= time && Num(motion, "SampleMotionTimeMs") >= started &&
                Num(motion, "SampleMotionTimeMs") <= time && returns >= end && Math.Ceiling(returns) > time &&
                Same(Id(basis, "ObjectId"), id) && Id(motion, "ThreatProjectileId").Length > 0, "evasion schedule or base course");
            Finite(basis, "X", "Y", "SpeedKmS", "Direction");
            if (Present(row, "IntegrationAnchor"))
            {
                var anchor = Object(Field(row, "IntegrationAnchor"));
                Require(Same(Id(anchor, "ObjectId"), id), "evasion anchor");
                Finite(anchor, "X", "Y", "SpeedKmS", "Direction");
            }
            _ = Field(row, "OrbitCycle");
            _ = Field(row, "LastTurnTime");
        }
        ulong rolls = 0;
        var threats = new HashSet<(string, string)>();
        foreach (var row in Rows(point, "DodgeThreats"))
        {
            string owner = Id(row, "OwnerId"), projectile = Id(row, "ProjectileId");
            long detected = Int(row, "DetectedAt"), due = Int(row, "DueAt");
            bool evaluated = Bool(row, "Evaluated"), evaded = Bool(row, "Evaded");
            var roll = Field(row, "RollSucceeded");
            Require(threats.Add(Key(owner, projectile)) && detected >= 0 && detected <= time && due >= detected &&
                (evaluated ? due <= time : due >= time) && (roll.ValueKind == JsonValueKind.Null ||
                evaluated && roll.ValueKind is JsonValueKind.True or JsonValueKind.False) && (!evaded || roll.ValueKind == JsonValueKind.True) &&
                launches.TryGetValue(projectile, out var launch) && Int(launch, "ProjectileKind") == 0 &&
                !Same(owner, Id(launch, "ActorObjectId")) && !Same(owner, player), "NPC dodge episode");
            if (objects.TryGetValue(owner, out var ship))
                Require(TypeIs(ship, "NpcShip") && HasModule(ship, Id(row, "EngineModuleId")), "NPC dodge engine");
            else Require(destroyedActors.TryGetValue(owner, out double destroyedAt) && destroyedAt >= detected, "unknown NPC dodge owner");
            if (roll.ValueKind != JsonValueKind.Null) rolls++;
        }
        Require(rolls == dodge && (dodge != 0 || UInt(Field(point, "NpcDodge"), "State") ==
            RngStreamSeedDerivation.DeriveStreamSeed(seed, "npc_dodge")), "NPC dodge cursor");
        var encounter = Object(Field(point, "Encounter"));
        long stateCode = Int(encounter, "State"), start = Int(encounter, "StartedAt"), quiet = Int(encounter, "QuietSince");
        var ended = Field(encounter, "EndedAt");
        var resume = Field(encounter, "ResumeSpeed");
        Require(stateCode is >= 0 and <= 3 && start >= 0 && start <= time && quiet >= 0 && quiet <= time &&
            (ended.ValueKind == JsonValueKind.Null ? stateCode is 0 or 1 :
                stateCode != 1 && ended.ValueKind == JsonValueKind.Number && ended.TryGetInt64(out long finish) && finish >= start && finish <= time) &&
            (resume.ValueKind == JsonValueKind.Null || stateCode == 1 && resume.ValueKind == JsonValueKind.Number && resume.TryGetInt32(out int speed) &&
                Enum.IsDefined((DeepSpaceSaga.Contracts.SimulationSpeed)speed)), "encounter timeline");
        Require(Ids(encounter, "EnemyIds").All(id => !Same(id, player) && (!objects.TryGetValue(id, out var owner) || TypeIs(owner, "NpcShip"))) &&
            Ids(encounter, "ProjectileIds").All(launches.ContainsKey), "encounter membership");
        var stats = Object(Field(encounter, "Stats"));
        Require(stats.EnumerateObject().All(p => p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetInt64(out long value) && value >= 0) &&
            Int(stats, "PlayerEvasions") <= Int(point, "PlayerEvasionEpisodes"), "encounter statistics");

        return WriteWorld(root, world);
    }

    // Preserve every unrelated JSON member and let the current typed world loader validate it.
    private static string WriteWorld(JsonElement root, JsonElement world)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name != "gameState") { property.WriteTo(writer); continue; }
                writer.WritePropertyName("gameState");
                writer.WriteStartObject();
                foreach (var item in world.EnumerateObject())
                {
                    if (Sections.Contains(item.Name)) continue;
                    if (item.Name == "pendingCommands" && item.Value.ValueKind == JsonValueKind.Array)
                        WriteCommands(writer, item);
                    else item.WriteTo(writer);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    // Save18/19 stored command DTOs inside the world payload. The current schema is strict,
    // so strip only known retired members here and continue to validate all other world fields.
    private static void WriteCommands(Utf8JsonWriter writer, JsonProperty list)
    {
        writer.WritePropertyName(list.Name);
        writer.WriteStartArray();
        foreach (var row in list.Value.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object) { row.WriteTo(writer); continue; }
            if (row.TryGetProperty("CommandType", out var type) && type.ValueKind == JsonValueKind.String &&
                SaveMigration20.CombatCommand(type.GetString())) { _ = Id(row, "CommandId"); continue; }
            writer.WriteStartObject();
            foreach (var property in row.EnumerateObject())
            {
                if (property.Name is "ModeId" or "ProfileId" or "ThreatProjectileId" or "EvadeSide")
                    continue;
                property.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void ValidateFlight(JsonElement flight, long time, JsonElement? pose)
    {
        long kind = Int(flight, "Kind"), launch = Int(flight, "LaunchMotionTimeMs");
        double x = Num(flight, "StartX"), y = Num(flight, "StartY");
        double dx = Num(flight, "DetonationX") - x, dy = Num(flight, "DetonationY") - y;
        double speed = Num(flight, "SpeedKmS"), fuse = Num(flight, "DetonationMotionTimeMs");
        long damage = Int(flight, "Damage");
        Require(kind is 0 or 1 && launch >= 0 && launch <= time && speed > 0 && fuse >= launch &&
            fuse <= long.MaxValue && Near(fuse, launch + double.Hypot(dx, dy) / 10 / speed * 1000) &&
            Id(flight, "ProfileId").Length > 0 && Id(flight, "LauncherModuleId").Length > 0 &&
            Num(flight, "BlastRadiusKm") > 0 && Num(flight, "CoreFraction") is > 0 and < 1 &&
            Num(flight, "ArmorPiercingPercent") is >= 0 and <= 100 && Num(flight, "ArmorDamagePercent") > 0 &&
            (kind == 0 ? damage > 0 : damage == 0), "invalid captured flight, speed or deadline");
        Finite(flight, "X", "Y");
        if (pose is not { } obj) return;
        Require(fuse >= time, "expired flight");
        double fraction = fuse == launch ? 0 : (time - launch) / (fuse - launch);
        Require(Near(Num(flight, "X"), x + fraction * dx) && Near(Num(flight, "Y"), y + fraction * dy) &&
            Near(Num(obj, "positionX"), Num(flight, "X")) && Near(Num(obj, "positionY"), Num(flight, "Y")) &&
            Near(Num(obj, "speedMps"), speed * 1000), "captured projectile pose");
    }

    private static HashSet<(string, string)> Launchers(JsonElement parent, string array, string ownerKey, string moduleKey,
        string stateKey, Dictionary<string, JsonElement> objects)
    {
        var result = new HashSet<(string, string)>();
        foreach (var row in Rows(parent, array))
        {
            Object(row);
            string owner = Id(row, ownerKey), module = Id(row, moduleKey);
            var state = Object(Field(row, stateKey));
            Require(objects.TryGetValue(owner, out var ship) && (TypeIs(ship, "PlayerShip") || TypeIs(ship, "NpcShip")) &&
                HasModule(ship, module) && result.Add(Key(owner, module)) &&
                Num(state, "RangeKm") > 0 && Num(state, "ReloadDurationMs") >= 0 && Num(state, "ScatterPercent") >= 0,
                "invalid or duplicate launcher");
            var profiles = UniqueRows(state, "Profiles", "ProfileId");
            Require(profiles.Count > 0 && profiles.Values.All(p => Num(p, "SpeedKmS") > 0 && Num(p, "BlastRadiusKm") > 0),
                "invalid launcher profiles");
        }
        return result;
    }

    private static ulong Rng(JsonElement parent, string key)
    {
        var value = Object(Field(parent, key));
        Require(Int(value, "AlgorithmVersion") == 1, "RNG algorithm: " + key);
        _ = UInt(value, "State");
        return UInt(value, "Counter");
    }

    private static bool Sequence(string id, string prefix, long maximum) =>
        id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
        long.TryParse(id.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out long sequence) &&
        sequence > 0 && sequence <= maximum;

    private static bool HasModule(JsonElement obj, string id) => Present(obj, "modules") &&
        Rows(obj, "modules").Any(m => Same(Id(m, "moduleId"), id));
    private static bool TypeIs(JsonElement obj, string name) => Same(Id(obj, "objectType"), name);
    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static (string, string) Key(string a, string b) => (a.ToUpperInvariant(), b.ToUpperInvariant());
    private static bool EqualPrimitive(JsonElement a, JsonElement b) =>
        a.ValueKind == b.ValueKind && (a.ValueKind == JsonValueKind.Number
            ? a.GetDouble() == b.GetDouble()
            : a.GetString() == b.GetString());

    private static bool Near(double a, double b) => double.IsFinite(a) && double.IsFinite(b) &&
        Math.Abs(a - b) <= 1e-7 + 16 * Math.Max(Math.Abs(Math.BitIncrement(a) - a), Math.Abs(Math.BitIncrement(b) - b));

    private static Dictionary<string, JsonElement> UniqueRows(JsonElement parent, string key, string idKey)
    {
        var result = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in Rows(parent, key)) Require(result.TryAdd(Id(Object(row), idKey), row), "duplicate identity in " + key);
        return result;
    }

    private static HashSet<string> Ids(JsonElement parent, string key)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in Rows(parent, key)) Require(result.Add(Text(row, key)), "duplicate identity in " + key);
        return result;
    }

    private static JsonElement Object(JsonElement value)
    {
        Require(value.ValueKind == JsonValueKind.Object, "expected an object");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject()) Require(keys.Add(property.Name), "duplicate field " + property.Name);
        return value;
    }

    private static JsonElement.ArrayEnumerator Rows(JsonElement parent, string key)
    {
        var value = Field(parent, key);
        Require(value.ValueKind == JsonValueKind.Array, "expected array: " + key);
        return value.EnumerateArray();
    }

    private static bool Present(JsonElement parent, string key) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null;

    private static JsonElement Field(JsonElement parent, string key) => parent.ValueKind == JsonValueKind.Object &&
        parent.TryGetProperty(key, out var value) ? value : throw Invalid("missing " + key);

    private static string Id(JsonElement parent, string key) => Text(Field(parent, key), key);
    private static string Text(JsonElement value, string key) =>
        value.ValueKind == JsonValueKind.String && value.GetString() is { } text &&
        !string.IsNullOrWhiteSpace(text) && text.Trim() == text ? text : throw Invalid("invalid identity: " + key);

    private static long Int(JsonElement parent, string key) => Field(parent, key) is { ValueKind: JsonValueKind.Number } v &&
        v.TryGetInt64(out long value) ? value : throw Invalid("invalid integer: " + key);

    private static ulong UInt(JsonElement parent, string key) => Field(parent, key) is { ValueKind: JsonValueKind.Number } v &&
        v.TryGetUInt64(out ulong value) ? value : throw Invalid("invalid unsigned integer: " + key);

    private static double Num(JsonElement parent, string key) => Field(parent, key) is { ValueKind: JsonValueKind.Number } v &&
        v.TryGetDouble(out double value) && double.IsFinite(value) ? value : throw Invalid("invalid number: " + key);

    private static bool Bool(JsonElement parent, string key) => Field(parent, key).ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => throw Invalid("invalid boolean: " + key)
    };

    private static void Finite(JsonElement parent, params string[] keys)
    {
        foreach (string key in keys) _ = Num(parent, key);
    }

    private static void Require([DoesNotReturnIf(false)] bool ok, string detail)
    {
        if (!ok) throw Invalid(detail);
    }

    private static ScenarioException Invalid(string detail) => new("Invalid legacy combat save: " + detail + ". Save was not modified.");
}
