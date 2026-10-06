using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Rng;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ResourceSurveyTests
{
    private const string Player = "SPC-0001";
    private const string Scanner = "MOD-PLAYER-SCANNER-01";
    private const string EngineModule = "MOD-PLAYER-ENGINE-01";
    private const string Stream = "ResourceSurvey:" + Player + ":" + Scanner;
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    private static readonly Lazy<GameDataRegistry> Registry = new(() =>
        EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(ClientRoot, "Settings.json"), out _, out _));

    [Theory]
    [InlineData("actor", CommandReasonCodes.UnknownObject)]
    [InlineData("destroyedActor", CommandReasonCodes.UnknownObject)]
    [InlineData("module", CommandReasonCodes.UnknownModule)]
    [InlineData("command", CommandReasonCodes.UnknownCommandType)]
    [InlineData("off", CommandReasonCodes.ModuleUnavailable)]
    [InlineData("disabled", CommandReasonCodes.ModuleUnavailable)]
    [InlineData("broken", CommandReasonCodes.ModuleUnavailable)]
    [InlineData("missing", CommandReasonCodes.MissingTarget)]
    [InlineData("unknown", CommandReasonCodes.UnknownTarget)]
    [InlineData("unsupported", ResourceSurveyReasonCodes.UnsupportedTarget)]
    [InlineData("unidentified", ResourceSurveyReasonCodes.TargetNotIdentified)]
    [InlineData("range", ResourceSurveyReasonCodes.OutOfRange)]
    [InlineData("known", ResourceSurveyReasonCodes.AlreadyKnown)]
    [InlineData("equality", null)]
    public void Scan_requires_owned_ready_scanner_identified_supported_target_and_range(string kind, string? reason)
    {
        using var engine = Create();
        string target = Target(engine);
        var command = Command(target);
        switch (kind)
        {
            case "actor": command = command with { ObjectId = target }; break;
            case "destroyedActor": Change(engine, Player, o => o with { IsDestroyed = true }); break;
            case "module": command = command with { ModuleId = "absent" }; break;
            case "command": command = command with { ModuleId = EngineModule }; break;
            case "off": ChangeScanner(engine, m => m with { PowerState = "Off" }); break;
            case "disabled": ChangeScanner(engine, m => m with { OperationalState = "Disabled" }); break;
            case "broken": ChangeScanner(engine, m => m with { StructurePoints = 0 }); break;
            case "missing": command = command with { TargetObjectId = null }; break;
            case "unknown": command = command with { TargetObjectId = "absent" }; break;
            case "unsupported": command = command with { TargetObjectId = "SPC-0002" }; break;
            case "unidentified": Change(engine, target, o => o with { IsKnown = false }); break;
            case "range": MovePlayer(engine, target, 1200.0001); break;
            case "equality": MovePlayer(engine, target, 1200); break;
            case "known":
                Start(engine, target);
                engine.CaptureSnapshotForTests(60000);
                command = command with { CommandId = "repeat" };
                break;
        }
        long at = kind == "known" ? 60000 : 0;
        var before = Save(engine, at);
        engine.ReceiveCommand(command);
        var snapshot = engine.CaptureSnapshotForTests(at);
        var after = Save(engine, at);
        if (reason is null)
        {
            Assert.Empty(snapshot.CommandResults);
            Assert.Single(after.GameState.StationResourceFields!.Surveys);
        }
        else
        {
            var rejected = Assert.Single(snapshot.CommandResults);
            Assert.Equal(CommandResultStatus.Rejected, rejected.Status);
            Assert.Equal(reason, rejected.ReasonCode);
            Assert.Equal(Json(before.GameState.StationResourceFields), Json(after.GameState.StationResourceFields));
        }
    }

    [Fact]
    public void Busy_module_target_and_duplicate_command_do_not_start_second_job()
    {
        using var engine = Create();
        string target = Target(engine);
        // A second scanner is a runtime fixture, with the same command capability.
        Change(engine, Player, o => o with { Modules = o.Modules.Add(o.Modules.Single(m => m.ModuleId == Scanner) with { ModuleId = "second" }) });
        Start(engine, target);
        engine.ReceiveCommand(Command(target));
        engine.ReceiveCommand(Command(target, "busy-target") with { ModuleId = "second" });
        string another = Save(engine).GameState.StationResourceFields!.Asteroids.First(a => a.ObjectId != target && a.StationObjectId ==
            Save(engine).GameState.StationResourceFields!.Asteroids.Single(a => a.ObjectId == target).StationObjectId).ObjectId;
        engine.ReceiveCommand(Command(another, "busy-module"));
        var snapshot = engine.CaptureSnapshotForTests();
        Assert.Equal(2, snapshot.CommandResults.Length);
        Assert.All(snapshot.CommandResults, r => { Assert.Equal(CommandResultStatus.Rejected, r.Status); Assert.Equal(CommandReasonCodes.Busy, r.ReasonCode); });
        Assert.Single(Save(engine).GameState.StationResourceFields!.Surveys);
        Assert.Equal(ScannerCommandTypes.StructuralScan, snapshot.InstalledModules.Single(m => m.ModuleId == Scanner).ActiveCommandType);
        Assert.False(snapshot.Objects.Single(o => o.ObjectId == target).Survey!.CanStructuralScan);
        Assert.Empty(SurveyStreams(Save(engine)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(600)]
    public void Scan_finishes_at_60000_calendar_ms_without_accelerating_motion(int multiplier)
    {
        using var engine = Create();
        using var control = Create();
        string target = Target(engine);
        Change(engine, Player, o => o with { InitialMotion = o.InitialMotion with { SpeedKmS = .7 } });
        Change(control, Player, o => o with { InitialMotion = o.InitialMotion with { SpeedKmS = .7 } });
        Start(engine, target);
        Assert.Empty(engine.CaptureSnapshotForTests(59999, simulationTimeMs: 59999 / multiplier).CommandResults);
        var done = engine.CaptureSnapshotForTests(60000, simulationTimeMs: 60000 / multiplier);
        var comparison = control.CaptureSnapshotForTests(60000, simulationTimeMs: 60000 / multiplier);
        var result = Assert.Single(done.CommandResults);
        Assert.Equal(CommandResultStatus.Executed, result.Status);
        Assert.Equal(60000, result.EffectiveGameTimeMs);
        Assert.Equal(comparison.Objects.Single(o => o.ObjectId == Player), done.Objects.Single(o => o.ObjectId == Player));
        Assert.Empty(Save(engine, 60000, 60000 / multiplier).GameState.StationResourceFields!.Surveys);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 100)]
    [InlineData(0, 85)]
    [InlineData(1, 85)]
    [InlineData(2, 85)]
    [InlineData(14, 85)]
    [InlineData(123, 85)]
    public void Seeded_scan_success_and_failure_use_one_saved_draw(int masterSeed, int chance)
    {
        using var engine = Create((ulong)masterSeed, chance);
        string target = Target(engine);
        ulong seed = RngStreamSeedDerivation.DeriveStreamSeed((ulong)masterSeed, Stream);
        var expected = RngStreamNames.CreateDeterministicRandom(seed);
        for (int i = 0; i < 1000; i++) expected.NextDouble();
        int draw = (int)Math.Floor(expected.NextDouble() * 100);
        Start(engine, target);
        Assert.Empty(SurveyStreams(Save(engine)));
        var result = Assert.Single(engine.CaptureSnapshotForTests(60000).CommandResults);
        Assert.Equal(draw < chance ? CommandResultStatus.Executed : CommandResultStatus.Failed, result.Status);
        Assert.Equal(draw < chance ? null : ResourceSurveyReasonCodes.ScanFailed, result.ReasonCode);
        var stream = Assert.Single(SurveyStreams(Save(engine, 60000)));
        Assert.Equal(seed, stream.Seed);
        Assert.Equal(10010UL, stream.Counter);
        engine.ReceiveCommand(Command(target, "retry"));
        var retry = engine.CaptureSnapshotForTests(60000);
        if (draw < chance) Assert.Equal(ResourceSurveyReasonCodes.AlreadyKnown, Assert.Single(retry.CommandResults).ReasonCode);
        else
        {
            Assert.Empty(retry.CommandResults);
            var next = Assert.Single(engine.CaptureSnapshotForTests(120000).CommandResults);
            Assert.Equal(expected.NextDouble() * 100 < chance ? CommandResultStatus.Executed : CommandResultStatus.Failed, next.Status);
            Assert.Equal(10020UL, Assert.Single(SurveyStreams(Save(engine, 120000))).Counter);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Leaving_range_fails_before_due_without_rng_even_after_reentry(bool curved)
    {
        using var coarse = Create();
        using var fine = Create();
        string target = Target(coarse);
        foreach (var engine in new[] { coarse, fine })
        {
            if (curved) Circle(engine, target, 700, 'R');
            else MovePlayer(engine, target, 1000, speed: 1, heading: 90);
            Start(engine, target);
        }
        var once = Assert.Single(coarse.CaptureSnapshotForTests(60000).CommandResults);
        var results = new List<CommandResult>();
        for (int t = 137; t < 60000; t += 137) results.AddRange(fine.CaptureSnapshotForTests(t).CommandResults);
        results.AddRange(fine.CaptureSnapshotForTests(60000).CommandResults);
        var frequent = Assert.Single(results);
        Assert.Equal(CommandResultStatus.Failed, once.Status);
        Assert.Equal(ResourceSurveyReasonCodes.OutOfRange, once.ReasonCode);
        Assert.InRange(once.EffectiveGameTimeMs, 1, 59999);
        Assert.InRange(Math.Abs(once.EffectiveGameTimeMs - frequent.EffectiveGameTimeMs), 0, 1);
        Assert.Empty(SurveyStreams(Save(coarse, 60000)));
        if (curved)
        {
            var end = coarse.CaptureSnapshotForTests(60000);
            Assert.True(end.Objects.Single(o => o.ObjectId == target).Survey!.CanStructuralScan);
        }
    }

    [Theory]
    [InlineData('L', 600, false)]
    [InlineData('R', 600, false)]
    [InlineData('L', 700, true)]
    [InlineData('R', 700, true)]
    public void Range_tangency_and_arc_crossing_are_distinguished(char direction, double center, bool exits)
    {
        using var engine = Create();
        string target = Target(engine);
        Circle(engine, target, center, direction);
        Start(engine, target);
        var result = Assert.Single(engine.CaptureSnapshotForTests(60000).CommandResults);
        Assert.Equal(exits ? CommandResultStatus.Failed : CommandResultStatus.Executed, result.Status);
        if (exits)
        {
            // Circle x = center - 600 cos(theta), y = +/-600 sin(theta).
            double angle = Math.Acos((center * center + 600 * 600 - 1200 * 1200) / (2 * center * 600));
            long crossing = (long)Math.Ceiling(angle / (6 * Math.PI / 180) * 1000);
            Assert.Equal(crossing, result.EffectiveGameTimeMs);
        }
        else Assert.Equal(60000, result.EffectiveGameTimeMs);
    }

    [Theory]
    [InlineData("target")]
    [InlineData("remove")]
    [InlineData("off")]
    [InlineData("actor")]
    public void Target_loss_and_module_unavailability_finish_once(string failure)
    {
        using var engine = Create();
        string target = Target(engine);
        Start(engine, target);
        engine.CaptureSnapshotForTests(10000);
        if (failure == "target") Change(engine, target, o => o with { IsDestroyed = true });
        if (failure == "remove") Objects(engine).RemoveAll(o => o.InitialMotion.ObjectId == target);
        if (failure == "off") ChangeScanner(engine, m => m with { PowerState = "Off" });
        if (failure == "actor") Change(engine, Player, o => o with { IsDestroyed = true });
        var snapshot = engine.CaptureSnapshotForTests(11000);
        var result = Assert.Single(snapshot.CommandResults);
        bool lost = failure is "target" or "remove";
        Assert.Equal(lost ? CommandResultStatus.Failed : CommandResultStatus.Cancelled, result.Status);
        Assert.Equal(lost ? ResourceSurveyReasonCodes.TargetLost : CommandReasonCodes.ModuleUnavailable, result.ReasonCode);
        Assert.Null(snapshot.InstalledModules.Single(m => m.ModuleId == Scanner).ActiveCommandType);
        Assert.Empty(engine.CaptureSnapshotForTests(60000).CommandResults);
        Assert.Empty(SurveyStreams(Save(engine, 60000)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(85)]
    public void Save_mid_scan_resumes_same_deadline_outcome_counter_and_command_id(int chance)
    {
        using var engine = Create(chance: chance);
        string target = Target(engine);
        Start(engine, target);
        engine.CaptureSnapshotForTests(30000, simulationTimeMs: 500);
        var save = Save(engine, 30000, 500);
        var job = Assert.Single(save.GameState.StationResourceFields!.Surveys);
        Assert.Equal(60000, job.DueGameTimeMs);
        Assert.Equal(500, job.LastValidatedSimulationTimeMs);
        using var restored = Restore(RoundTrip(save));
        // A changed current config cannot override the saved survey rules.
        restored.ConfigureStationResourceFields(Config(1 - chance / 100));
        restored.ReceiveCommand(Command(target));
        Assert.Empty(restored.CaptureSnapshotForTests(30000, simulationTimeMs: 500).CommandResults);
        Assert.Equal(Json(save.GameState.StationResourceFields), Json(Save(restored, 30000, 500).GameState.StationResourceFields));
        Assert.Empty(restored.CaptureSnapshotForTests(59999, simulationTimeMs: 999).CommandResults);
        var continuous = engine.CaptureSnapshotForTests(60000, simulationTimeMs: 1000);
        var resumed = restored.CaptureSnapshotForTests(60000, simulationTimeMs: 1000);
        Assert.Equal(Assert.Single(continuous.CommandResults), Assert.Single(resumed.CommandResults));
        Assert.Equal("scan", resumed.CommandResults[0].CommandId);
        Assert.Equal(Json(Save(engine, 60000, 1000).GameState.StationResourceFields), Json(Save(restored, 60000, 1000).GameState.StationResourceFields));
    }

    [Theory]
    [InlineData("overdue")]
    [InlineData("future")]
    [InlineData("negative")]
    [InlineData("duration")]
    [InlineData("cursor")]
    [InlineData("duplicate")]
    [InlineData("pending")]
    [InlineData("receipt")]
    [InlineData("off")]
    [InlineData("range")]
    [InlineData("cycle")]
    public void Invalid_saved_job_leaves_previous_world_unchanged(string corruption)
    {
        using var engine = Create();
        string target = Target(engine);
        Start(engine, target);
        var save = Save(engine, 1000);
        var gs = save.GameState;
        var fields = gs.StationResourceFields!;
        var job = Assert.Single(fields.Surveys);
        job = corruption switch
        {
            "overdue" => job with { DueGameTimeMs = 1000 },
            "future" => job with { StartedGameTimeMs = 2000, DueGameTimeMs = 62000 },
            "negative" => job with { StartedGameTimeMs = -1 },
            "duration" => job with { DueGameTimeMs = long.MaxValue },
            "cursor" => job with { LastValidatedSimulationTimeMs = 999 },
            _ => job
        };
        gs = gs with { StationResourceFields = fields with { Surveys = corruption == "duplicate" ? [job, job] : [job] } };
        if (corruption == "pending") gs = gs with { PendingCommands = [Command(target)] };
        if (corruption == "receipt") gs = gs with { CommandReceipts = [new("scan", Player, Scanner, ScannerCommandTypes.StructuralScan, CommandResultStatus.Failed, 0)] };
        if (corruption is "off" or "range" or "cycle") gs = gs with
        {
            SpaceObjects = gs.SpaceObjects.Select(o => o.ObjectId != Player ? o : corruption == "range"
                ? o with { PositionX = o.PositionX + 2000 }
                : o with
                {
                    Modules = o.Modules!.Select(m => m.ModuleId != Scanner ? m : corruption == "off"
                    ? m with { PowerState = "Off" } : m with { ActiveCycle = new("bad", 0, 100000, ShipEngineCommandTypes.Accelerate, false) }).ToArray()
                }).ToArray()
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with { GameState = gs }, isSave: true));
        Assert.Equal(Json(save), Json(Save(engine, 1000)));
    }

    [Fact]
    public void Unknown_snapshot_never_leaks_resources_or_ice_image()
    {
        using var engine = Create();
        string target = Target(engine);
        var snapshot = engine.CaptureSnapshotForTests();
        var unknown = snapshot.Objects.Single(o => o.ObjectId == target);
        var actual = Save(engine).GameState.SpaceObjects.Single(o => o.ObjectId == target);
        Assert.Equal("Ice", actual.CompositionType);
        Assert.NotEqual(actual.Image, unknown.Image);
        Assert.Null(unknown.DisplayName);
        Assert.Null(unknown.Survey!.CompositionType);
        Assert.Empty(unknown.Survey.Resources);
        Assert.Equal(actual.MassKg, unknown.Survey.MassKg);
        Assert.True(unknown.Survey.CanStructuralScan);
        Assert.All(snapshot.Objects.Where(o => o.Survey is not null), o => Assert.False(o.Survey!.CompositionKnown));
        Assert.Null(snapshot.Objects.Single(o => o.ObjectId == Player).Survey);
    }

    [Fact]
    public void Success_reveals_only_target_and_survives_reload()
    {
        using var engine = Create();
        string target = Target(engine);
        Start(engine, target);
        var snapshot = engine.CaptureSnapshotForTests(60000);
        var revealed = snapshot.Objects.Single(o => o.ObjectId == target);
        Assert.True(revealed.Survey!.CompositionKnown);
        Assert.False(revealed.Survey.CanStructuralScan);
        Assert.Equal("Ice", revealed.Survey.CompositionType);
        Assert.Equal(1000, revealed.Survey.Resources.Sum(r => r.Permille));
        Assert.Single(snapshot.Objects.Where(o => o.Survey?.CompositionKnown == true));
        var save = RoundTrip(Save(engine, 60000));
        Assert.Equal(save.GameState.SpaceObjects.Single(o => o.ObjectId == target).Image, revealed.Image);
        using var restored = Restore(save);
        var after = restored.CaptureSnapshotForTests(60000).Objects.Single(o => o.ObjectId == target);
        Assert.Equal(Json(revealed), Json(after));
        Assert.Equal(Json(save.GameState.StationResourceFields), Json(Save(restored, 60000).GameState.StationResourceFields));
    }

    [Fact]
    public void Real_content_scan_changes_no_market_budget_credits_or_cargo()
    {
        using var engine = Create(chance: 85);
        using var control = Create(chance: 85);
        Start(engine, Target(engine));
        var scanned = Save(engine, 60000);
        var untouched = Save(control, 60000);
        Assert.Equal(Json(untouched.GameState.SpaceObjects), Json(scanned.GameState.SpaceObjects));
        Assert.Equal(untouched.GameState.PlayerTokens, scanned.GameState.PlayerTokens);
        Assert.Equal(Json(untouched.GameState.EconomyTime), Json(scanned.GameState.EconomyTime));
        Assert.Equal(Json(untouched.GameState.TradingMap), Json(scanned.GameState.TradingMap));
        Assert.Equal(Json(untouched.GameState.StationResourceFields!.RngStreams),
            Json(scanned.GameState.StationResourceFields!.RngStreams.Where(s => !s.Name.StartsWith("ResourceSurvey:")).ToArray()));
    }

    [Theory]
    [InlineData(2UL, 682517968136577984UL, 0.8452141274908623)]
    [InlineData(14UL, 9168840623717708332UL, 0.8511006305232182)]
    public void Named_survey_stream_matches_golden_draws_on_both_sides_of_85_percent(ulong master, ulong seed, double draw)
    {
        Assert.Equal(seed, RngStreamSeedDerivation.DeriveStreamSeed(master, Stream));
        var random = new ResourceFieldRandom(new(Stream, seed, 10000));
        Assert.Equal(draw, random.NextDouble());
        Assert.Equal(10010UL, random.Capture().Counter);
        using var engine = Create(master, 85);
        Start(engine, Target(engine));
        Assert.Equal(draw < .85 ? CommandResultStatus.Executed : CommandResultStatus.Failed,
            Assert.Single(engine.CaptureSnapshotForTests(60000).CommandResults).Status);
    }

    [Fact]
    public void Start_and_rejection_use_calendar_cursor_and_overflow_leaves_no_job()
    {
        using var engine = Create();
        string target = Target(engine);
        engine.CaptureSnapshotForTests(30000, simulationTimeMs: 500);
        engine.ReceiveCommand(Command(target));
        Assert.Empty(engine.CaptureSnapshotForTests(30000, simulationTimeMs: 500).CommandResults);
        var job = Assert.Single(Save(engine, 30000, 500).GameState.StationResourceFields!.Surveys);
        Assert.Equal(30000, job.StartedGameTimeMs);
        Assert.Equal(90000, job.DueGameTimeMs);
        engine.ReceiveCommand(Command(target, "busy"));
        Assert.Equal(30000, Assert.Single(engine.CaptureSnapshotForTests(30000, simulationTimeMs: 500).CommandResults).EffectiveGameTimeMs);
        Assert.Empty(engine.CaptureSnapshotForTests(89999, simulationTimeMs: 1000).CommandResults);
        Assert.Equal(90000, Assert.Single(engine.CaptureSnapshotForTests(90000, simulationTimeMs: 1001).CommandResults).EffectiveGameTimeMs);

        using var overflow = Create();
        typeof(SimulationEngine).GetField("_processedWorldTimeMs", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(overflow, long.MaxValue - 1);
        overflow.ReceiveCommand(Command(Target(overflow)));
        var result = Assert.Single(overflow.CaptureSnapshotForTests(long.MaxValue - 1, simulationTimeMs: 0).CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(ResourceSurveyReasonCodes.InvalidTime, result.ReasonCode);
        Assert.Equal(long.MaxValue - 1, result.EffectiveGameTimeMs);
        var state = Save(overflow, long.MaxValue - 1, 0);
        Assert.Empty(state.GameState.StationResourceFields!.Surveys);
        Assert.Empty(SurveyStreams(state));
    }

    [Fact]
    public void Straight_out_and_back_is_checked_before_the_direction_cycle_changes_motion()
    {
        using var engine = Create();
        string target = Target(engine);
        MovePlayer(engine, target, 1000, speed: 1, heading: 90);
        Change(engine, Player, o => o with
        {
            Modules = o.Modules.Select(m => m.ModuleId != EngineModule ? m : m with
            {
                ActiveCycle = new("return", 0, 30000, ShipEngineCommandTypes.DirectionSynchronization, false,
                TargetObjectId: target, CapturedTargetCourseDegrees: 270)
            }).ToImmutableArray()
        });
        Start(engine, target);
        var snapshot = engine.CaptureSnapshotForTests(60000);
        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(ResourceSurveyReasonCodes.OutOfRange, result.ReasonCode);
        Assert.InRange(result.EffectiveGameTimeMs, 20000, 20001);
        Assert.True(snapshot.Objects.Single(o => o.ObjectId == target).Survey!.CanStructuralScan);
        Assert.Empty(SurveyStreams(Save(engine, 60000)));
    }

    [Fact]
    public void Real_approach_and_active_scan_resume_together_without_changing_motion()
    {
        using var engine = Create();
        using var control = Create();
        string target = Target(engine);
        foreach (var world in new[] { engine, control })
        {
            MovePlayer(world, target, 600, speed: .7, heading: 90);
            world.ReceiveCommand(new("approach", 0, Player, EngineModule, NavigationComputerCommandTypes.Approach, target));
            Assert.Empty(world.CaptureSnapshotForTests().CommandResults);
        }
        Start(engine, target);
        var save = RoundTrip(Save(engine, 30000, 500));
        Assert.NotNull(save.GameState.SpaceObjects.Single(o => o.ObjectId == Player).Modules!.Single(m => m.ModuleId == EngineModule).ActiveCycle!.ApproachRoute);
        using var restored = Restore(save);
        var resumed = restored.CaptureSnapshotForTests(60000, simulationTimeMs: 1000);
        var continuous = engine.CaptureSnapshotForTests(60000, simulationTimeMs: 1000);
        var comparison = control.CaptureSnapshotForTests(60000, simulationTimeMs: 1000);
        Assert.Equal(Json(continuous.Objects.Single(o => o.ObjectId == Player)), Json(resumed.Objects.Single(o => o.ObjectId == Player)));
        Assert.Equal(Json(comparison.Objects.Single(o => o.ObjectId == Player)), Json(resumed.Objects.Single(o => o.ObjectId == Player)));
        Assert.Equal(CommandResultStatus.Executed, resumed.CommandResults.Single(r => r.CommandId == "scan").Status);
    }

    [Theory]
    [InlineData(10010UL)]
    [InlineData(ulong.MaxValue - 15)]
    public void Saved_survey_counter_cannot_exceed_completions_allowed_by_elapsed_time(ulong counter)
    {
        using var engine = Create();
        Start(engine, Target(engine));
        var original = Save(engine, 1000);
        var fields = original.GameState.StationResourceFields!;
        var impossible = new ResourceFieldRngData(Stream,
            RngStreamSeedDerivation.DeriveStreamSeed(original.GameState.MasterSeed!.Value, Stream), counter);
        var malformed = RoundTrip(original with
        {
            GameState = original.GameState with
            {
                StationResourceFields = fields with { RngStreams = fields.RngStreams.Append(impossible).ToImmutableArray() }
            }
        });

        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(malformed, isSave: true));
        Assert.Contains("survey RNG counter", error.Message, StringComparison.Ordinal);
        Assert.Equal(Json(original), Json(Save(engine, 1000)));
    }

    [Fact]
    public void Exhausted_saved_survey_stream_is_rejected_before_replacing_world()
    {
        using var engine = Create();
        Start(engine, Target(engine));
        var original = Save(engine, 1000);
        var fields = original.GameState.StationResourceFields!;
        var exhausted = new ResourceFieldRngData(Stream,
            RngStreamSeedDerivation.DeriveStreamSeed(original.GameState.MasterSeed!.Value, Stream),
            ulong.MaxValue - 5);
        var malformed = RoundTrip(original with
        {
            GameState = original.GameState with
            {
                StationResourceFields = fields with { RngStreams = fields.RngStreams.Append(exhausted).ToImmutableArray() }
            }
        });

        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(malformed, isSave: true));
        Assert.Contains("survey RNG counter", error.Message, StringComparison.Ordinal);
        Assert.Equal(Json(original), Json(Save(engine, 1000)));
    }

    private static StationResourceFieldConfig Config(int chance) =>
        JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(Path.Combine(ClientRoot, "Data", "World", "station-resource-fields.json")))! is { } config
            ? config with { StructuralScan = config.StructuralScan with { SuccessChancePercent = chance } } : throw new InvalidOperationException();

    private static SimulationEngine Create(ulong seed = 123, int chance = 100)
    {
        var engine = new SimulationEngine(Registry.Value);
        engine.ConfigureStationResourceFields(Config(chance));
        var source = ScenarioLoader.LoadFromFile(Path.Combine(ClientRoot, "Scenarios", "Undocked", "scenario.json"));
        engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed } });
        string target = Target(engine);
        MovePlayer(engine, target, 0);
        return engine;
    }

    private static SimulationEngine Restore(ScenarioFile save)
    {
        var engine = new SimulationEngine(Registry.Value);
        engine.LoadScenario(save, isSave: true);
        return engine;
    }

    private static string Target(SimulationEngine engine) => Objects(engine).First(o =>
        o.ObjectType == SpaceObjectType.Asteroid && o.PersistenceType == "Permanent" && o.CompositionType == "Ice").InitialMotion.ObjectId;
    private static PlayerCommand Command(string target, string id = "scan") => new(id, 1, Player, Scanner, ScannerCommandTypes.StructuralScan, target);
    private static void Start(SimulationEngine engine, string target)
    {
        engine.ReceiveCommand(Command(target));
        Assert.Empty(engine.CaptureSnapshotForTests().CommandResults);
        Assert.Single(Save(engine).GameState.StationResourceFields!.Surveys);
    }
    private static List<SpaceObjectRuntime> Objects(SimulationEngine engine) =>
        (List<SpaceObjectRuntime>)typeof(SimulationEngine).GetField("_objects", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(engine)!;
    private static void Change(SimulationEngine engine, string id, Func<SpaceObjectRuntime, SpaceObjectRuntime> change)
    {
        var objects = Objects(engine);
        int index = objects.FindIndex(o => o.InitialMotion.ObjectId == id);
        objects[index] = change(objects[index]);
    }
    private static void ChangeScanner(SimulationEngine engine, Func<InstalledModuleRuntime, InstalledModuleRuntime> change) =>
        Change(engine, Player, o => o with { Modules = o.Modules.Select(m => m.ModuleId == Scanner ? change(m) : m).ToImmutableArray() });
    private static void MovePlayer(SimulationEngine engine, string target, double dx, double speed = 0, double heading = 0)
    {
        var point = Objects(engine).Single(o => o.InitialMotion.ObjectId == target).InitialMotion;
        Change(engine, Player, o => o with { InitialMotion = o.InitialMotion with { X = point.X + dx, Y = point.Y, SpeedKmS = speed, Direction = heading } });
    }
    private static void Circle(SimulationEngine engine, string target, double center, char direction)
    {
        MovePlayer(engine, target, center - 600, 600 * (6 * Math.PI / 180) / 10, direction == 'R' ? 0 : 180);
        Change(engine, Player, o =>
        {
            var p = o.InitialMotion;
            var route = new ApproachRoute(p.X, p.Y, p.Direction, p.SpeedKmS, 6, $"{direction}SS", 2 * Math.PI * 600, 0, 0,
                p.X, p.Y, p.Direction, 0, 1);
            return o with
            {
                Modules = o.Modules.Select(m => m.ModuleId != EngineModule ? m : m with
                {
                    ActiveCycle = new("circle", 0, 100000, NavigationComputerCommandTypes.Approach, false, TargetObjectId: target, ApproachRoute: route)
                }).ToImmutableArray()
            };
        });
    }
    private static ScenarioFile Save(SimulationEngine engine, long calendar = 0, long? physical = null) =>
        (ScenarioFile)typeof(SimulationEngine).GetMethod("CaptureSaveStateCore", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(engine, [new SimulationClockState(calendar, SimulationSpeed.Speed0, physical ?? calendar)])!;
    private static IEnumerable<ResourceFieldRngData> SurveyStreams(ScenarioFile save) =>
        save.GameState.StationResourceFields!.RngStreams.Where(s => s.Name.StartsWith("ResourceSurvey:", StringComparison.Ordinal));
    private static ScenarioFile RoundTrip(ScenarioFile save) => ScenarioLoader.LoadFromJson(Json(save), allowNonZeroGameTime: true);
    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
}
