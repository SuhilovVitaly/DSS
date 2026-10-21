using System.Security.Cryptography;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.CombatAcceptance;

/// <summary>Fixed input policy v1. All simulation, AI, scatter, damage and outcomes belong to the production Engine.</summary>
internal sealed class ProductionCombatBalanceHarness
{
    internal const string PolicyVersion = "public-observation-v1";
    internal const long StepMs = 100, ReactionMs = 500, HorizonMs = 600000;
    private readonly GameDataRegistry _registry;
    private readonly ScenarioFile _source;
    internal string ScenarioHash { get; }
    internal string CatalogHash { get; }

    internal ProductionCombatBalanceHarness(string clientRoot)
    {
        string scenario = Path.Combine(clientRoot, "Scenarios/CombatMukha/scenario.json");
        _source = ScenarioLoader.LoadFromFile(scenario);
        _registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(clientRoot, "Settings.json"), out _, out _);
        if (_source.GameState.SpaceObjects.Count(o => o.ObjectType == SpaceObjectType.NpcShip) != 3 ||
            !_source.GameState.SpaceObjects.SelectMany(o => o.Crew ?? []).Any() ||
            _source.GameState.SpaceObjects.SelectMany(o => o.Crew ?? []).Any(c => c.AccuracySkill != 20 || c.ManeuverabilitySkill != 20))
            throw new InvalidOperationException("Balance requires shipped three-Mukha scenario and all operators20; no fixture substitution.");
        ScenarioHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(scenario)));
        CatalogHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            Modules = Enumerable.Range(0, _registry.ModuleTypes.Count).Select(_registry.ModuleTypes.GetDefinition).ToArray(),
            Classes = Enumerable.Range(0, _registry.ShipClasses.Count).Select(_registry.ShipClasses.GetDefinition).ToArray(),
            _registry.CombatRules
        })));
    }

    internal sealed record CommandTrace(long MotionTimeMs, PlayerCommand Command, string? Status, string? Reason);
    internal sealed record RunResult(ulong Seed, string Policy, string PolicyVersion, string ScenarioHash, string CatalogHash,
        string Outcome, double PhysicalDurationMs, double RealDurationMs, int PlayerTorpedoes, int Countermissiles,
        int HostileTorpedoes, int PlayerHits, int Evades, int LegacyChanceFacts, CommandTrace[] Commands);

    internal RunResult Run(ulong seed, string policy)
    {
        if (policy is not ("dodge" or "static")) throw new ArgumentOutOfRangeException(nameof(policy));
        long real = 0;
        using var engine = new SimulationEngine(_registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => real));
        engine.LoadScenario(_source with { GameState = _source.GameState with { MasterSeed = seed, CurrentSpeed = "Speed1" } });
        string player = _source.GameState.PlayerShipObjectId!;
        var enemies = _source.GameState.SpaceObjects.Where(o => o.ObjectType == SpaceObjectType.NpcShip)
            .Select(o => o.ObjectId).ToHashSet(StringComparer.Ordinal);
        var history = new Dictionary<string, Queue<Pose>>(StringComparer.Ordinal);
        var firstSeen = new Dictionary<string, long>(StringComparer.Ordinal);
        var evaded = new HashSet<string>(StringComparer.Ordinal);
        var defended = new HashSet<string>(StringComparer.Ordinal);
        var tape = new List<CommandTrace>();
        var traceIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        var journal = new Dictionary<long, CombatJournalEntry>();
        ulong sequence = 0;
        AuthoritativeSnapshot snapshot = engine.CaptureSnapshot(advanceClock: true);
        string outcome = "unfinished";
        for (long time = 0; time <= HorizonMs; time += StepMs)
        {
            real = time * 4;
            snapshot = engine.CaptureSnapshot(advanceClock: true);
            Observe(snapshot);
            if (snapshot.EffectiveOutcome.State == SessionOutcomeState.Defeated) { outcome = "defeat"; break; }
            if (!snapshot.Objects.Any(o => enemies.Contains(o.ObjectId))) { outcome = "victory"; break; }
            var own = snapshot.Objects.Single(o => o.ObjectId == player);
            foreach (var target in snapshot.Objects.Where(o => enemies.Contains(o.ObjectId)))
            {
                if (!history.TryGetValue(target.ObjectId, out var poses)) history.Add(target.ObjectId, poses = new());
                if (time % 1000 == 0 && (poses.Count == 0 || poses.Last().Time != time))
                {
                    poses.Enqueue(new(time, target.X, target.Y, target.Direction, target.SpeedKmS));
                    while (poses.Count > 3) poses.Dequeue();
                }
            }
            var threats = snapshot.Objects.Where(o => o.ObservedProjectile?.Kind == ProjectileKind.Torpedo)
                .Select(o => (Object: o, Flight: o.ObservedProjectile!)).ToArray();
            foreach (var threat in threats) firstSeen.TryAdd(threat.Object.ObjectId, time);
            var readyThreats = threats.Where(t => time >= firstSeen[t.Object.ObjectId] + ReactionMs)
                .Select(t => (t.Object, t.Flight, Danger: DangerTime(own, t.Flight)))
                .Where(t => double.IsFinite(t.Danger)).OrderBy(t => t.Danger)
                .ThenBy(t => t.Object.ObjectId, StringComparer.Ordinal).ToArray();
            foreach (var module in snapshot.InstalledModules.OrderBy(m => m.ModuleId, StringComparer.Ordinal))
            {
                if (!Available(module, time)) continue;
                if (module.Defense is { Profiles: { } profiles } defense && (defense.ReadyAtMotionTimeMs ?? 0) <= time)
                {
                    var profile = profiles.Single(p => p.ProfileId == "wide");
                    foreach (var threat in readyThreats.Where(t => !defended.Contains(t.Object.ObjectId)))
                    {
                        var aim = Intercept(own.X, own.Y, threat.Flight.X, threat.Flight.Y,
                            threat.Flight.DirectionX * threat.Flight.SpeedKmS * 10,
                            threat.Flight.DirectionY * threat.Flight.SpeedKmS * 10, profile.SpeedKmS * 10);
                        if (aim is null || Distance(own.X, own.Y, aim.Value.X, aim.Value.Y) > defense.RangeKm * 10) continue;
                        Send(module.ModuleId, DefenseCommandTypes.Launch, aim.Value.X, aim.Value.Y, profile.ProfileId);
                        defended.Add(threat.Object.ObjectId);
                        break;
                    }
                }
                if (module.LauncherCombat is { Profiles: { } torpedoProfiles } launcher && (launcher.ReadyAtMotionTimeMs ?? 0) <= time)
                {
                    var profile = torpedoProfiles.Single(p => p.ProfileId == "fast");
                    var target = snapshot.Objects.Where(o => enemies.Contains(o.ObjectId))
                        .OrderBy(o => Distance(own.X, own.Y, o.X, o.Y)).ThenBy(o => o.ObjectId, StringComparer.Ordinal).First();
                    var poses = history[target.ObjectId].ToArray();
                    double turn = ConfirmedTurn(poses, new(time, target.X, target.Y, target.Direction, target.SpeedKmS));
                    var aim = ArcIntercept(own.X, own.Y, new(time, target.X, target.Y, target.Direction, target.SpeedKmS),
                        turn, profile.SpeedKmS, launcher.RangeKm!.Value);
                    if (aim is not null) Send(module.ModuleId, CombatCommandTypes.Launch, aim.Value.X, aim.Value.Y, profile.ProfileId);
                }
            }
            if (policy == "dodge" && own.Evasion is null)
            {
                var threat = readyThreats.FirstOrDefault(t => t.Danger <= 2 && !evaded.Contains(t.Object.ObjectId));
                var module = snapshot.InstalledModules.FirstOrDefault(m => Available(m, time) && m.CommandTypeIds.Contains(ManeuverCommandTypes.Evade));
                if (threat.Object is not null && module is not null)
                {
                    Send(module.ModuleId, ManeuverCommandTypes.Evade, threat: threat.Object.ObjectId);
                    evaded.Add(threat.Object.ObjectId);
                }
            }
            snapshot = engine.CaptureSnapshot(advanceClock: false);
            Observe(snapshot);
            if (snapshot.EffectiveOutcome.State == SessionOutcomeState.Defeated) { outcome = "defeat"; break; }

            void Send(string module, string type, double? x = null, double? y = null, string? profile = null, string? threat = null)
            {
                string id = $"{policy}-{seed}-{++sequence}";
                var command = new PlayerCommand(id, sequence, player, module, type, TargetWorldX: x, TargetWorldY: y,
                    ProfileId: profile, ThreatProjectileId: threat, EvadeSide: threat is null ? null : EvadeSide.Left);
                traceIndex.Add(id, tape.Count); tape.Add(new(time, command, null, null));
                engine.ReceiveCommand(command);
            }
        }
        double physical = snapshot.EffectiveOutcome.DeathMotionTimeMs ?? snapshot.MotionTimeMs;
        var events = journal.Values.ToArray();
        return new(seed, policy, PolicyVersion, ScenarioHash, CatalogHash, outcome, physical, physical * 4,
            events.Count(e => e.Type == CombatEventType.Launch && e.ActorObjectId == player && e.ProjectileKind == ProjectileKind.Torpedo),
            events.Count(e => e.Type == CombatEventType.Launch && e.ActorObjectId == player && e.ProjectileKind == ProjectileKind.Countermissile),
            events.Count(e => e.Type == CombatEventType.Launch && e.ActorObjectId != player && e.ProjectileKind == ProjectileKind.Torpedo),
            events.Count(e => e.Type == CombatEventType.Hit && e.TargetObjectId == player),
            tape.Count(t => t.Command.CommandType == ManeuverCommandTypes.Evade && t.Status == nameof(CommandResultStatus.Executed)),
            events.Count(e => e.Roll is not null || e.ChanceTenths is not null), tape.ToArray());

        void Observe(AuthoritativeSnapshot s)
        {
            foreach (var e in s.CombatJournal) journal[e.EventId] = e;
            foreach (var r in s.CommandResults)
                if (traceIndex.TryGetValue(r.CommandId, out int i)) tape[i] = tape[i] with { Status = r.Status.ToString(), Reason = r.ReasonCode };
        }
    }

    private static bool Available(InstalledModuleSnapshot m, long time) => !m.IsDestroyed && m.StructureCurrent > 0 &&
        m.PowerState == "On" && m.OperationalState == "Ready" && (m.DisabledUntilMotionTimeMs ?? 0) <= time;
    private static double Distance(double ax, double ay, double bx, double by) => double.Hypot(bx - ax, by - ay);
    internal sealed record Pose(long Time, double X, double Y, double Direction, double Speed);
    private static double Delta(double a, double b) => ((b - a + 540) % 360) - 180;

    internal static double ConfirmedTurn(Pose[] poses, Pose current)
    {
        if (poses.Length != 3 || poses[1].Time - poses[0].Time != 1000 || poses[2].Time - poses[1].Time != 1000 ||
            poses.Any(p => Math.Abs(p.Speed - current.Speed) > 1e-6)) return 0;
        double first = Delta(poses[0].Direction, poses[1].Direction), second = Delta(poses[1].Direction, poses[2].Direction);
        if (Math.Abs(first - second) > 1e-6 || Math.Abs(Delta(poses[2].Direction + second * (current.Time - poses[2].Time) / 1000d, current.Direction)) > 1e-4) return 0;
        return second;
    }

    internal static (double X, double Y) ArcPosition(Pose pose, double turn, double seconds)
    {
        double h = pose.Direction * Math.PI / 180, rate = turn * Math.PI / 180, speed = pose.Speed * 10;
        if (Math.Abs(rate) < 1e-9) return (pose.X + Math.Sin(h) * speed * seconds, pose.Y - Math.Cos(h) * speed * seconds);
        return (pose.X + speed / rate * (Math.Cos(h) - Math.Cos(h + rate * seconds)),
            pose.Y - speed / rate * (Math.Sin(h + rate * seconds) - Math.Sin(h)));
    }

    private static (double X, double Y)? ArcIntercept(double x, double y, Pose pose, double turn, double speed, double range)
    {
        double previous = 0, maximum = range / speed;
        for (double t = Math.Min(.025, maximum); t <= maximum + 1e-9; t = Math.Min(t + .025, maximum))
        {
            var p = ArcPosition(pose, turn, t);
            if (Distance(x, y, p.X, p.Y) <= speed * 10 * t)
            {
                double lo = previous, hi = t;
                for (int i = 0; i < 32; i++)
                {
                    double mid = (lo + hi) / 2; p = ArcPosition(pose, turn, mid);
                    if (Distance(x, y, p.X, p.Y) <= speed * 10 * mid) hi = mid; else lo = mid;
                }
                return ArcPosition(pose, turn, hi);
            }
            if (t == maximum) break;
            previous = t;
        }
        return null;
    }

    private static (double X, double Y)? Intercept(double x, double y, double tx, double ty, double vx, double vy, double speed)
    {
        double dx = tx - x, dy = ty - y, a = vx * vx + vy * vy - speed * speed, b = 2 * (dx * vx + dy * vy), c = dx * dx + dy * dy;
        double time;
        if (Math.Abs(a) < 1e-9) time = b < 0 ? -c / b : double.PositiveInfinity;
        else
        {
            double d = b * b - 4 * a * c;
            if (d < 0) return null;
            double q = -.5 * (b + Math.CopySign(Math.Sqrt(d), b));
            double t1 = q / a, t2 = q == 0 ? double.PositiveInfinity : c / q;
            time = Math.Min(t1 > 0 ? t1 : double.PositiveInfinity, t2 > 0 ? t2 : double.PositiveInfinity);
        }
        return double.IsFinite(time) ? (tx + vx * time, ty + vy * time) : null;
    }

    private static double DangerTime(ObjectMotionSnapshot own, ObservedProjectileSnapshot p)
    {
        double h = own.Direction * Math.PI / 180, vx = p.DirectionX * p.SpeedKmS * 10 - Math.Sin(h) * own.SpeedKmS * 10,
            vy = p.DirectionY * p.SpeedKmS * 10 + Math.Cos(h) * own.SpeedKmS * 10, dx = p.X - own.X, dy = p.Y - own.Y;
        double vv = vx * vx + vy * vy;
        if (vv == 0) return double.PositiveInfinity;
        double time = -(dx * vx + dy * vy) / vv;
        return time >= 0 && Distance(0, 0, dx + vx * time, dy + vy * time) <= p.BlastRadiusKm * 10 ? time : double.PositiveInfinity;
    }
}
