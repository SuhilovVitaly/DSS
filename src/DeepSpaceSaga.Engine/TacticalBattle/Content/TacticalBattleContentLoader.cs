using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.TacticalBattle.Content;

internal sealed record TacticalBattleContentIssue(string File, string Field, string Message);
internal sealed class TacticalBattleContentException(TacticalBattleContentIssue issue)
    : ContentException($"{issue.File}: {issue.Field}: {issue.Message}")
{
    public TacticalBattleContentIssue Issue { get; } = issue;
}
internal sealed record TacticalBattleAvailability(TacticalBattleCatalog? Catalog, TacticalBattleContentIssue? Error)
{
    public bool IsAvailable => Catalog is not null;
    public static TacticalBattleAvailability Available(TacticalBattleCatalog catalog) => new(catalog, null);
    public static TacticalBattleAvailability Unavailable(string file, string field, string message) => new(null, new(file, field, message));
}

internal static class TacticalBattleContentLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static TacticalBattleCatalog Load(string folder)
    {
        var roots = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        var rules = Read<TacticalRules>(FileRoot("rules.json"), "rules.json", "$");
        var aiRoot = FileRoot("ai.json");
        Require(aiRoot.ValueKind == JsonValueKind.Object && aiRoot.EnumerateObject().All(p =>
            new[] { "tactics", "roles", "pursuit", "striker", "interceptor", "commander", "leash", "tail", "fighter" }.Contains(p.Name, StringComparer.Ordinal)),
            "ai.json", "$", "Unknown AI field or invalid object.");
        var ai = new BattleAi(
            Named<BattleTactic>(Property(aiRoot, "tactics", "ai.json", "$"), "ai.json", "$.tactics"),
            Named<BattleRole>(Property(aiRoot, "roles", "ai.json", "$"), "ai.json", "$.roles"),
            Ai<PursuitSettings>("pursuit"), Ai<StrikerSettings>("striker"), Ai<InterceptorSettings>("interceptor"),
            Ai<CommanderSettings>("commander"), Ai<LeashSettings>("leash"), Ai<TailSettings>("tail"), Ai<FighterSettings>("fighter"));
        var pilotsRoot = FileRoot("pilots.json");
        Require(pilotsRoot.ValueKind == JsonValueKind.Object, "pilots.json", "$", "Expected object.");
        var pilots = new OrderedBattleCatalog<BattlePilot>(pilotsRoot.EnumerateObject().Select(p =>
        {
            string field = "$." + p.Name;
            Require(!string.IsNullOrWhiteSpace(p.Name), "pilots.json", field, "Pilot ID must not be empty.");
            Require(p.Value.ValueKind == JsonValueKind.Object, "pilots.json", field, "Expected pilot object.");
            Require(p.Value.EnumerateObject().All(v => v.Name is "name" or "accuracy" or "maneuver"), "pilots.json", field, "Unknown pilot field.");
            string name = Property(p.Value, "name", "pilots.json", field).ValueKind == JsonValueKind.String
                ? p.Value.GetProperty("name").GetString()! : throw Error("pilots.json", field + ".name", "Expected name.");
            return (p.Name, new BattlePilot(name, Skill(Property(p.Value, "accuracy", "pilots.json", field), field + ".accuracy"),
                Skill(Property(p.Value, "maneuver", "pilots.json", field), field + ".maneuver")));
        }));
        var catalog = new TacticalBattleCatalog(rules, ai, DirectoryCatalog<BattleFrame>("Frames"),
            DirectoryCatalog<BattleWeapon>("Modules/Weapons"), DirectoryCatalog<BattleEngine>("Modules/Engines"),
            DirectoryCatalog<BattleShield>("Modules/Shields"), DirectoryCatalog<BattleArmor>("Modules/Armor"), pilots,
            Named<BattleLayout>(FileRoot("layouts.json"), "layouts.json", "$"),
            Named<BattleScenario>(FileRoot("scenarios.json"), "scenarios.json", "$"));
        Validate(catalog, aiRoot, name => FileRoot(name));
        return catalog;

        JsonElement FileRoot(string relative)
        {
            if (roots.TryGetValue(relative, out var cached)) return cached;
            string path = Path.Combine(folder, relative);
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                ValidateJson(document.RootElement, relative, "$");
                var copy = document.RootElement.Clone();
                roots.Add(relative, copy);
                return copy;
            }
            catch (JsonException ex) { throw Error(relative, ex.Path ?? "$", ex.Message); }
            catch (IOException ex) { throw Error(relative, "$", ex.Message); }
            catch (UnauthorizedAccessException ex) { throw Error(relative, "$", ex.Message); }
        }
        T Ai<T>(string key) => Read<T>(Property(aiRoot, key, "ai.json", "$"), "ai.json", "$." + key);
        OrderedBattleCatalog<T> DirectoryCatalog<T>(string relative)
        {
            string[] files;
            try { files = Directory.GetFiles(Path.Combine(folder, relative), "*.json"); }
            catch (IOException ex) { throw Error(relative, "$", ex.Message); }
            catch (UnauthorizedAccessException ex) { throw Error(relative, "$", ex.Message); }
            Require(files.Length > 0, relative, "$", "Catalog must not be empty.");
            return new(files.OrderBy(Path.GetFileName, StringComparer.Ordinal).Select(path =>
            {
                string id = Path.GetFileNameWithoutExtension(path);
                string file = relative + "/" + Path.GetFileName(path);
                Require(!string.IsNullOrWhiteSpace(id), file, "$.id", "Catalog ID must not be empty.");
                var element = FileRoot(file);
                Require(element.ValueKind == JsonValueKind.Object && element.TryGetProperty("id", out var found)
                    && found.ValueKind == JsonValueKind.String && found.GetString() == id, file, "$.id", "ID must equal filename.");
                return (id, Read<T>(element, file, "$"));
            }));
        }
    }

    public static TacticalBattleAvailability FromSettings(string settingsFile, string basePath, JsonElement path)
    {
        if (path.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(path.GetString()))
            return TacticalBattleAvailability.Unavailable(settingsFile, "typeData.tacticalBattle", "Battle content path is absent or invalid.");
        try
        {
            string folder = Path.GetFullPath(Path.Combine(basePath, path.GetString()!));
            if (!Directory.Exists(folder))
                return TacticalBattleAvailability.Unavailable(settingsFile, "typeData.tacticalBattle", "Battle content directory does not exist: " + folder);
            return TacticalBattleAvailability.Available(Load(folder));
        }
        catch (TacticalBattleContentException ex) { return new(null, ex.Issue); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { return TacticalBattleAvailability.Unavailable(settingsFile, "typeData.tacticalBattle", ex.Message); }
    }

    private static OrderedBattleCatalog<T> Named<T>(JsonElement element, string file, string field)
    {
        Require(element.ValueKind == JsonValueKind.Object, file, field, "Expected ordered object catalog.");
        Require(element.EnumerateObject().All(p => !string.IsNullOrWhiteSpace(p.Name)), file, field, "Catalog ID must not be empty.");
        return new(element.EnumerateObject().Select(p => (p.Name, Read<T>(p.Value, file, field + "." + p.Name))));
    }

    private static T Read<T>(JsonElement element, string file, string field)
    {
        ValidateShape(element, typeof(T), file, field);
        try { return JsonSerializer.Deserialize<T>(element, Options)!; }
        catch (JsonException ex) { throw Error(file, field + (ex.Path is { } p ? p.TrimStart('$') : ""), ex.Message); }
    }

    private static void ValidateShape(JsonElement element, Type type, string file, string field)
    {
        if (type == typeof(string))
        {
            Require(element.ValueKind == JsonValueKind.String, file, field, "Expected string.");
            return;
        }
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            if (element.ValueKind == JsonValueKind.Null) return;
            type = underlying;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
        {
            Require(element.ValueKind == JsonValueKind.Array, file, field, "Expected array.");
            int index = 0;
            foreach (var child in element.EnumerateArray()) ValidateShape(child, type.GetGenericArguments()[0], file, $"{field}[{index++}]");
            return;
        }
        if (type.Namespace != typeof(TacticalRules).Namespace || type.IsValueType) return;
        Require(element.ValueKind == JsonValueKind.Object, file, field, "Expected object.");
        var constructor = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        foreach (var parameter in constructor.GetParameters())
        {
            string key = JsonNamingPolicy.CamelCase.ConvertName(parameter.Name!);
            if (!element.TryGetProperty(key, out var value))
            {
                Require(parameter.HasDefaultValue, file, field + "." + key, "Required field is missing.");
                continue;
            }
            if (value.ValueKind == JsonValueKind.Null)
            {
                Require(new NullabilityInfoContext().Create(parameter).ReadState == NullabilityState.Nullable,
                    file, field + "." + key, "Field must not be null.");
                continue;
            }
            ValidateShape(value, parameter.ParameterType, file, field + "." + key);
        }
    }

    private static void ValidateJson(JsonElement element, string file, string field)
    {
        if (element.ValueKind == JsonValueKind.Number)
            Require(element.TryGetDouble(out double value) && double.IsFinite(value), file, field, "Number must be finite.");
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new List<string>();
            foreach (var property in element.EnumerateObject())
            {
                Require(!names.Contains(property.Name, StringComparer.Ordinal), file, field + "." + property.Name, "Duplicate field.");
                names.Add(property.Name);
                ValidateJson(property.Value, file, field + "." + property.Name);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (var child in element.EnumerateArray()) ValidateJson(child, file, $"{field}[{index++}]");
        }
    }

    private static PilotSkill Skill(JsonElement element, string field)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int fixedValue)) return PilotSkill.Fixed(fixedValue);
        Require(element.ValueKind == JsonValueKind.Array && element.GetArrayLength() == 2, "pilots.json", field, "Expected integer or [min,max].");
        Require(element[0].ValueKind == JsonValueKind.Number && element[0].TryGetInt32(out _)
            && element[1].ValueKind == JsonValueKind.Number && element[1].TryGetInt32(out _), "pilots.json", field, "Skill endpoints must be integers.");
        int lo = element[0].GetInt32(), hi = element[1].GetInt32();
        Require(lo <= hi, "pilots.json", field, "Skill minimum exceeds maximum.");
        return PilotSkill.Range(lo, hi);
    }

    private static void Validate(TacticalBattleCatalog c, JsonElement aiRoot, Func<string, JsonElement> root)
    {
        var r = c.Rules;
        Require(r.Version == "4.0", "rules.json", "$.version", "Expected V4.0.");
        Require(r.Substeps > 0 && r.SeriesTurnCap > 0, "rules.json", "$.substeps/seriesTurnCap", "Counts must be positive.");
        Require(Unit(r.TurnAtMaxSpeedFraction) && Unit(r.Disabled.HullFraction) && Unit(r.Systems.EngineDamagedFactor), "rules.json", "$", "Fractions must be in [0,1].");
        Require(r.DeployScatter.Xy >= 0 && r.DeployScatter.CourseDeg >= 0, "rules.json", "$.deployScatter", "Scatter must not be negative.");
        Require(r.Hit.MinChance >= 0 && r.Hit.MaxChance <= 100 && r.Hit.MinChance <= r.Hit.MaxChance, "rules.json", "$.hit", "Invalid chance limits.");
        Require(r.Rear.ArcHalfDeg is >= 0 and <= 180 && r.Hit.RearBonusArcDeg is >= 0 and <= 180, "rules.json", "$.rear/hit", "Invalid arc.");
        Require(r.Systems.MinHullLoss >= 0 && !r.Systems.Table.IsEmpty && r.Systems.Table.All(e => e.Weight >= 0 && !string.IsNullOrWhiteSpace(e.Effect))
            && r.Systems.Table.Any(e => e.Weight > 0), "rules.json", "$.systems", "Invalid system damage table.");
        Require(c.Frames.Contains(TacticalBattleConstants.CarrierFrame), "Frames", "$.carrier", "Carrier frame is required.");
        foreach (var (id, frame) in c.Frames.Entries)
        {
            string file = $"Frames/{id}.json";
            Require(frame.Hull > 0, file, "$.hull", "Hull must be positive.");
            if (id == TacticalBattleConstants.CarrierFrame)
            {
                Require(frame.Maneuver is not null, file, "$.maneuver", "Carrier maneuver integer is required.");
                continue;
            }
            var element = root(file);
            foreach (string key in new[] { "frameMassKg", "referenceMassKg", "minSpeed", "maxSpeedCap", "turnCapDeg", "engineSlots" }) Property(element, key, file, "$");
            Require(frame.FrameMassKg > 0 && frame.ReferenceMassKg > 0, file, "$.frameMassKg/referenceMassKg", "Masses must be positive.");
            Require(frame.MinSpeed >= 0 && frame.MaxSpeedCap >= frame.MinSpeed && frame.TurnCapDeg > 0, file, "$.minSpeed/maxSpeedCap/turnCapDeg", "Invalid movement limits.");
            Require(frame.EngineSlots > 0 && frame.ShieldSlots >= 0, file, "$.engineSlots/shieldSlots", "Invalid slots.");
        }
        foreach (var (id, w) in c.Weapons.Entries)
        {
            string file = $"Modules/Weapons/{id}.json";
            Require(w.DamageMin >= 0 && w.DamageMax >= w.DamageMin, file, "$.damageMin/damageMax", "Invalid damage range.");
            Require(w.Piercing >= 0 && w.MassKg >= 0 && w.Tier >= 0, file, "$.piercing/massKg/tier", "Negative equipment value.");
            Require(w.ArcDeg is > 0 and <= 180 && w.Range > 0 && Unit(w.RearKillChance), file, "$.arcDeg/range/rearKillChance", "Invalid weapon geometry or probability.");
        }
        foreach (var (id, e) in c.Engines.Entries)
            Require(e.SpeedBonus >= 0 && e.SpeedChange >= 0 && e.TurnDeg >= 0 && e.MassKg >= 0, $"Modules/Engines/{id}.json", "$", "Negative engine value.");
        foreach (var (id, s) in c.Shields.Entries)
            Require(s.Defense >= 0 && s.MassKg >= 0 && s.Tier >= 0, $"Modules/Shields/{id}.json", "$", "Negative shield value.");
        foreach (var (id, a) in c.Armor.Entries)
            Require(a.Armor >= 0 && a.MassKg >= 0 && a.Tier >= 0, $"Modules/Armor/{id}.json", "$", "Negative armor value.");
        Require(c.Layouts.Count > 0 && c.Pilots.Count > 0 && c.Scenarios.Count > 0 && c.Ai.Tactics.Count > 0, "layouts/pilots/scenarios/ai.json", "$", "Catalogs must not be empty.");
        foreach (var (id, layout) in c.Layouts.Entries)
        {
            string field = "$." + id;
            Require(c.Frames.Contains(layout.Class), "layouts.json", field + ".class", "Unknown frame.");
            Require(c.Armor.Contains(layout.Armor), "layouts.json", field + ".armor", "Unknown armor.");
            Require(layout.Hull is null or > 0, "layouts.json", field + ".hull", "Hull must be positive.");
            var frame = c.Frames[layout.Class];
            if (layout.Class == TacticalBattleConstants.CarrierFrame)
                Require(layout.Weapon is null && layout.Engines.IsEmpty && layout.Shield is null && (layout.ShieldCount ?? 0) == 0, "layouts.json", field, "Carrier must not mount weapons, engines or shields.");
            else
            {
                Require(layout.Weapon is not null && c.Weapons.Contains(layout.Weapon), "layouts.json", field + ".weapon", "Drone weapon is required.");
                Require(layout.Engines.Length >= 1 && layout.Engines.Length <= frame.EngineSlots, "layouts.json", field + ".engines", "Engine slot count exceeded.");
            }
            foreach (string engine in layout.Engines) Require(c.Engines.Contains(engine), "layouts.json", field + ".engines", "Unknown engine.");
            if (layout.Shield is { } shield)
            {
                Require(c.Shields.Contains(shield), "layouts.json", field + ".shield", "Unknown shield.");
                int count = layout.ShieldCount ?? TacticalBattleConstants.DefaultShieldCount;
                Require(count >= 0 && count <= frame.ShieldSlots, "layouts.json", field + ".shieldCount", "Shield slot count exceeded.");
            }
            else Require((layout.ShieldCount ?? 0) == 0, "layouts.json", field + ".shieldCount", "No shield requires zero count.");
        }
        var ai = c.Ai;
        Require(ai.Pursuit.SlowForTurnSteps > 0 && ai.Tail.SpeedSteps > 0 && ai.Tail.TurnSteps > 0
            && ai.Fighter.BreakAfterTurns > 0 && ai.Fighter.ExtendTurns > 0, "ai.json", "$", "AI step counts must be positive integers.");
        Require(Unit(ai.Tail.Chance) && ai.Tail.RangeFactor > 0 && ai.Tail.ConeFactor > 0 && ai.Pursuit.SpeedFactor > 0
            && ai.Pursuit.AimErrorScale >= 0 && ai.Leash.Radius > 0, "ai.json", "$", "Invalid AI tuning.");
        Require(ai.Striker.AttackSpeed >= 0 && ai.Striker.BreakDistance >= 0 && ai.Striker.ExtendDistance > ai.Striker.BreakDistance,
            "ai.json", "$.striker", "Invalid striker distances or speed.");
        Require(ai.Interceptor.PatrolRadius >= 0 && ai.Interceptor.PatrolOrbitRadius >= 0 && ai.Interceptor.PatrolOrbitStepDeg > 0
            && ai.Interceptor.PatrolSpeed >= 0 && ai.Interceptor.ScreenDistance >= 0 && ai.Interceptor.ScreenHold >= 0
            && ai.Interceptor.EngageFactor >= 0 && Unit(ai.Interceptor.OrbitTurnFraction), "ai.json", "$.interceptor", "Invalid interceptor tuning.");
        Require(ai.Commander.MaxInterceptors >= 0 && ai.Commander.ExtraInterceptors >= 0 && ai.Commander.MinRoleTurns >= 0
            && ai.Commander.ThreatTurns > 0 && ai.Commander.ThreatRadius >= 0 && ai.Commander.ThreatHeadingDeg is >= 0 and <= 180
            && Unit(ai.Commander.StrikeWhenEnemyCarrierBelow), "ai.json", "$.commander", "Invalid commander tuning.");
        Require(ai.Tactics.Contains(ai.Commander.StartTactic) && !ai.Tactics[ai.Commander.StartTactic].Adaptive, "ai.json", "$.commander.startTactic", "Starting tactic must be fixed.");
        foreach (string role in TacticalBattleConstants.RoleOrder) Require(ai.Roles.Contains(role), "ai.json", "$.roles." + role, "Missing role.");
        foreach (var (id, tactic) in ai.Tactics.Entries)
        {
            Require(tactic.Striker >= 0 && tactic.Interceptor >= 0 && tactic.Fighter >= 0, "ai.json", "$.tactics." + id, "Role counts must not be negative.");
            if (!tactic.Adaptive)
                foreach (string role in TacticalBattleConstants.RoleOrder) Property(aiRoot.GetProperty("tactics").GetProperty(id), role, "ai.json", "$.tactics." + id);
        }
        foreach (var (id, scenario) in c.Scenarios.Entries)
        {
            string field = "$." + id;
            Require(scenario.Sides.Length == 2 && scenario.Sides[0].Id == "A" && scenario.Sides[1].Id == "B", "scenarios.json", field + ".sides", "Expected sides A, B in order.");
            var ids = new List<string>();
            foreach (var side in scenario.Sides)
            {
                Require(ai.Tactics.Contains(side.Tactic), "scenarios.json", field + ".sides.tactic", "Unknown tactic.");
                Require(c.Layouts.Contains(side.Carrier.Layout) && c.Layouts[side.Carrier.Layout].Class == TacticalBattleConstants.CarrierFrame, "scenarios.json", field + ".carrier.layout", "Carrier layout required.");
                AddId(side.Carrier.Id);
                Require(side.Drones.Length >= 1, "scenarios.json", field + ".drones", "Each side requires at least one drone.");
                foreach (var drone in side.Drones)
                {
                    AddId(drone.Id);
                    Require(c.Pilots.Contains(drone.Pilot), "scenarios.json", field + ".drones.pilot", "Unknown pilot.");
                    Require(c.Layouts.Contains(drone.Layout) && c.Layouts[drone.Layout].Class != TacticalBattleConstants.CarrierFrame, "scenarios.json", field + ".drones.layout", "Drone layout required.");
                    Require(drone.Speed >= 0, "scenarios.json", field + ".drones.speed", "Speed must not be negative.");
                }
            }
            if (scenario.Goal is { } goal)
            {
                Require(TacticalBattleConstants.SideOrder.Contains(goal.PlayerSide, StringComparer.Ordinal), "scenarios.json", field + ".goal.playerSide", "Unknown player side.");
                Require(Unit(goal.WinMaxOneLoss) && Unit(goal.WinNoLoss) && Unit(goal.Tolerance), "scenarios.json", field + ".goal", "Invalid goal fractions.");
                Require(goal.Seeds.Length == 2 && goal.Seeds[0] >= 1 && goal.Seeds[1] >= goal.Seeds[0], "scenarios.json", field + ".goal.seeds", "Expected positive inclusive seed interval.");
            }
            void AddId(string unitId)
            {
                Require(!string.IsNullOrWhiteSpace(unitId) && !ids.Contains(unitId, StringComparer.Ordinal), "scenarios.json", field + ".id", "Duplicate or empty unit ID.");
                ids.Add(unitId);
            }
        }
    }

    private static JsonElement Property(JsonElement element, string key, string file, string field)
    {
        Require(element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out _), file, field + "." + key, "Required field is missing.");
        return element.GetProperty(key);
    }
    private static bool Unit(double value) => value is >= 0 and <= 1;
    private static TacticalBattleContentException Error(string file, string field, string message) => new(new(file, field, message));
    private static void Require(bool valid, string file, string field, string message)
    {
        if (!valid) throw Error(file, field, message);
    }
}
