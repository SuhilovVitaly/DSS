using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using Silk.NET.Windowing;
using SkiaSharp;
using Harness = DeepSpaceSaga.CombatAcceptance.ProductionCombatBalanceHarness;

internal static class PointCombatNativeEvidence
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    internal static int Run(string[] args)
    {
        if (args.Length < 6) throw new ArgumentException("<repo> <report.json> --combat-window <scale> <WxH> <ru|en> [input|win|defeat] [--audio]");
        string settings = Path.Combine(AppContext.BaseDirectory, "Settings.json");
        byte[] original = File.ReadAllBytes(settings);
        var config = JsonNode.Parse(original)!;
        config["gameSettings"]!["uiScale"] = float.Parse(args[3], CultureInfo.InvariantCulture);
        config["gameSettings"]!["language"] = args[5] == "ru" ? "Russian" : "English";
        try { File.WriteAllText(settings, config.ToJsonString()); return RunCore(args); }
        finally { File.WriteAllBytes(settings, original); }
    }

    private static int RunCore(string[] args)
    {
        string root = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]);
        string client = Path.Combine(root, "src/DeepSpaceSaga.Client"), mode = args.Length > 6 ? args[6] : "input";
        if (mode is not ("input" or "win" or "defeat")) throw new ArgumentException("Unknown native evidence mode.");
        float scale = float.Parse(args[3], CultureInfo.InvariantCulture);
        var size = args[4].Split('x'); int width = int.Parse(size[0]), height = int.Parse(size[1]);
        Directory.SetCurrentDirectory(client); Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(client, "Settings.json"), out _, out _);
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios/CombatMukha/scenario.json"));
        long real = 0;
        long Ticks() => real * Stopwatch.Frequency / 1000;
        using var engine = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => real));
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { MasterSeed = 3001, CurrentSpeed = "Speed1" } });
        var world = engine.CaptureSnapshot(advanceClock: true);
        if (mode == "input")
            while (!world.Objects.Any(o => o.ObservedProjectile is not null) && world.MotionTimeMs < 30000)
            { real += 400; world = engine.CaptureSnapshot(advanceClock: true); }
        Harness.RunResult? tape = mode == "input" ? null : new Harness(client).Run(3001, mode == "win" ? "dodge" : "static");
        var buffer = new SnapshotBuffer(Ticks); buffer.Update(world);
        var connection = new DriverConnection(engine);
        var handle = new GameSessionHandle(connection, buffer);
        bool audioEnabled = mode == "input" && args.Contains("--audio");
        var audio = CombatAlertAudio.CreateSession();
        var audioCues = new List<CombatAlertKind>();
        if (audioEnabled) audio.Activate();
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), handle, timestampProvider: Ticks, uiScale: scale,
            mapSettings: TacticalMapSettings.Load(Path.Combine(client, "Settings.json")),
            combatSettings: CombatVisualSettings.Load(Path.Combine(client, CombatVisualSettings.RelativePath)),
            combatInputs: CombatInputSettings.Load(Path.Combine(client, CombatInputSettings.RelativePath)),
            alertAudio: audioEnabled ? cue => { audioCues.Add(cue.Kind); audio.Play(cue); }
        : null,
            activateAudio: audioEnabled ? audio.Activate : null, deactivateAudio: audioEnabled ? audio.Deactivate : null,
            playInitialCombatStart: true);
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_REPORT", Path.ChangeExtension(output, ".frame.json"));
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_WINDOW", args[4]);
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_EXIT", "0");
        Environment.SetEnvironmentVariable("DSS_MAP_FRAME_CASE", "EP0012 current point combat; actual production Engine; controlled clock and managed input");
        using var window = new SkiaWindow(screen, new Factory());
        typeof(SkiaWindow).GetField("_session", Private)!.SetValue(window, handle);
        typeof(SkiaWindow).GetField("_gameSessionScreen", Private)!.SetValue(window, screen);
        var native = (IWindow)typeof(SkiaWindow).GetField("_window", Private)!.GetValue(window)!;
        var checks = new Dictionary<string, bool>(); var notRun = new Dictionary<string, string>(); var captures = new List<string>(); var captureStates = new List<object>();
        var dispatchMs = new List<double>(); var elapsed = Stopwatch.StartNew();
        int frame = 0, stage = 0, tapeIndex = 0, terminalFrame = 0;
        int actualWidth = width, actualHeight = height, framebufferWidth = width, framebufferHeight = height;
        long replayTime = 0, pausedMotion = 0; double pausedReload = 0;
        (double X, double Y) aim = default; string? ownId = null, counterId = null;
        string? failure = null; bool completed = false, damageFramePending = false, damageFrameCaptured = false;
        window.NativeFrameRendered = (surface, current) =>
        {
            try
            {
                frame++;
                actualWidth = native.Size.X; actualHeight = native.Size.Y; framebufferWidth = native.FramebufferSize.X; framebufferHeight = native.FramebufferSize.Y;
                if (elapsed.Elapsed > TimeSpan.FromMinutes(3)) throw new TimeoutException("Native evidence internal3min cap.");
                checks["actualUiScaleMatchesRequest"] = Math.Abs((float)typeof(GameSessionScreen).GetField("_uiScale", Private)!.GetValue(screen)! - scale) < .001;
                checks["localeBattleLabel"] = GameSessionScreen.CombatText("Battle", "") == (args[5] == "ru" ? "БОЙ" : "COMBAT");
                checks["nativeOpenGlContext"] = typeof(SkiaWindow).GetField("_grContext", Private)!.GetValue(window) is not null;
                if (frame == 3)
                {
                    if (screen.IsPanelVisible) Click(screen.LastCloseRect);
                    var navigation = screen.CommandsPanel.CommandPanelRows.FirstOrDefault(r => r.Weapon is null && r.Opened);
                    if (navigation.Opened) Click(navigation.CaptionRect);
                    Tap(Key.F);
                }
                if (frame == 4 && mode == "input")
                {
                    var weapon = screen.CommandsPanel.CommandPanelRows.FirstOrDefault(r => r.Weapon?.ModuleId == world.InstalledModules.First(m => m.LauncherCombat is not null).ModuleId);
                    if (!weapon.Opened && Contains(weapon.CaptionRect)) Click(weapon.CaptionRect);
                }
                if (mode != "input") { Replay(surface, current); return; }
                if (frame < 6 || frame % 3 != 0) return;
                switch (stage++)
                {
                    case 0:
                        Tap(Key.F);
                        checks["activeBannerAndAllowedSpeeds"] = screen.CombatBorderVisible && !screen.CombatBannerRect.IsEmpty && screen.IsCombatSpeedEnabled(SimulationSpeed.SpeedDouble) && screen.IsCombatSpeedEnabled(SimulationSpeed.Speed2) && screen.IsCombatSpeedEnabled(SimulationSpeed.SpeedTen) && !screen.IsCombatSpeedEnabled(SimulationSpeed.Speed3) && !screen.IsCombatSpeedEnabled(SimulationSpeed.Speed4);
                        checks["shipCardAndLeftModules"] = screen.ObjectInfoPanel.OwnModuleDiagram is null && screen.CommandsPanel.OwnModuleDiagram is not null;
                        checks["ranges70And30"] = screen.WeaponRangeRings.Any(r => r.RangeKm == 70) && screen.WeaponRangeRings.Count(r => r.RangeKm == 30) == 2;
                        Tap(Key.Q); Tap(Key.Number1); aim = ChooseAim(40, 65); Move(aim); break;
                    case 1:
                        checks["torpedoAimSlowAndRealEta"] = screen.PointAim?.ProfileId == "slow" && screen.CurrentTorpedoAimPreview is { RadiusKm: 9 } p && Math.Abs(p.RealSeconds - p.PhysicalSeconds * 4) < 1e-9;
                        checks["torpedoAimLabelContained"] = screen.AvailableMapRect().Contains(screen.TorpedoAimLabelBounds);
                        Capture(surface, "torpedo-aim"); Tap(Key.Escape);
                        var free = screen.AvailableMapRect(); screen.OnMouseWheel(free.MidX, free.MidY, -8); break;
                    case 2: Tap(Key.Q); Tap(Key.Number1); aim = ChooseAim(80, 180); Move(aim); break;
                    case 3:
                        checks["outOfRangePreview"] = screen.CurrentTorpedoAimPreview?.OutOfRange == true;
                        int before = connection.Commands.Count; MapClick(aim); checks["outOfRangeDoesNotSend"] = before == connection.Commands.Count;
                        Capture(surface, "out-of-range"); Tap(Key.Escape); Tap(Key.F); break;
                    case 4: Tap(Key.Q); Tap(Key.Number1); aim = ChooseAim(40, 65); Move(aim); break;
                    case 5:
                        before = connection.Commands.Count; MapClick(aim);
                        checks["pointCommandSent"] = connection.Commands.Count == before + 1 && connection.Commands.Last().CommandType == CombatCommandTypes.Launch;
                        checks["torpedoClickExitsAiming"] = screen.PointAim is null && screen.CurrentTorpedoAimPreview is null;
                        checks["noProjectileBeforeAuthoritativeSnapshot"] = !buffer.Presented!.Snapshot.Objects.Any(o => o.OwnProjectile is not null);
                        Publish(); if (args.Contains("--clean-flight")) Tap(Key.Escape); break;
                    case 6:
                        var own = world.Objects.First(o => o.OwnProjectile?.Kind == ProjectileKind.Torpedo); ownId = own.ObjectId;
                        var visual = screen.PointProjectileVisuals[ownId];
                        checks["ownPhysicalRadiusAndCore"] = Math.Abs(visual.PhysicalRadiusPx!.Value - own.OwnProjectile!.BlastRadiusKm * 10 * screen.CameraPixelsPerWorldUnit) <= 1 &&
                            Math.Abs(visual.CoreRadiusPx!.Value - visual.PhysicalRadiusPx.Value * .3) <= 1;
                        checks["foreignHasNoEndpointOrDeadlineVisual"] = world.Objects.Where(o => o.ObservedProjectile is not null).All(o =>
                            screen.PointProjectileVisuals.TryGetValue(o.ObjectId, out var v) && v.DetonationCenter is null && v.RemainingRealSeconds is null && v.PhysicalRadiusPx is null);
                        checks["foreignHasNoForwardGeometry"] = screen.CombatTrajectories.Where(p => p.Value.IsObserved).All(p => p.Value.Prediction.Count == 0 && p.Value.Target.Count == 0);
                        checks["weaponRowsCurrentOnly"] = screen.CommandsPanel.CommandPanelRows.Where(r => r.Weapon is not null).All(r =>
                            !r.ReloadRingRect.IsEmpty && r.ReloadBarRect.IsEmpty && r.OperatorText is not null &&
                            r.Buttons.All(b => b.CommandTypeId is not (CombatCommandTypes.Fire or CombatCommandTypes.SelfDestruct or DefenseCommandTypes.Fire or DefenseCommandTypes.Enable or DefenseCommandTypes.Disable)));
                        checks["visibleProjectileLabelsContained"] = screen.PointProjectileVisuals.Values.Where(v => !v.LabelBounds.IsEmpty).All(v =>
                            new SKRect(0, 0, native.Size.X, native.Size.Y).Contains(v.LabelBounds));
                        Capture(surface, "confirmed-flights"); Tap(Key.Q); Move(aim); break;
                    case 7:
                        before = connection.Commands.Count; MapClick(aim); checks["reloadAimDoesNotSend"] = before == connection.Commands.Count && screen.PointAim is not null;
                        double zoom = screen.CameraPixelsPerWorldUnit; var profile = screen.PointAim!.ProfileId;
                        screen.OnMouseWheel(native.Size.X / 2, native.Size.Y / 2, -1);
                        checks["wheelChangesProfileNotZoom"] = screen.PointAim.ProfileId != profile && screen.CameraPixelsPerWorldUnit == zoom;
                        checks["profileKeysDidNotChangeSpeed"] = engine.CurrentSpeed == SimulationSpeed.Speed1;
                        Tap(Key.Escape); break;
                    case 8:
                        var row = screen.CommandsPanel.CommandPanelRows.FirstOrDefault(r => r.Weapon is not null && Contains(r.RangeToggleRect));
                        if (row.Weapon is not null)
                        {
                            bool visible = row.RangeVisible; string module = row.Weapon!.ModuleId;
                            Click(row.RangeToggleRect);
                            checks["rangeToggleSessionState"] = screen.CommandsPanel.IsWeaponRangeVisible(module) != visible;
                            Click(row.RangeToggleRect);
                        }
                        else notRun["rangeToggleNativeClick"] = "No weapon range toggle currently visible without scrolling in this viewport; unit scroll/range coverage remains separate.";
                        Tap(Key.W);
                        var observedPose = screen.RenderStates.First(o => o.Source.ObservedProjectile is not null).Pose.ToSnapshot();
                        var observed = observedPose.ObservedProjectile! with { X = observedPose.X, Y = observedPose.Y };
                        var player = screen.RenderStates.Single(o => o.IsPlayerShip).Pose.ToSnapshot();
                        double along = Math.Max(0, (player.X - observed.X) * observed.DirectionX + (player.Y - observed.Y) * observed.DirectionY - 50);
                        aim = (observed.X + observed.DirectionX * along, observed.Y + observed.DirectionY * along);
                        var defenseProfile = world.InstalledModules.First(m => m.Defense is not null).Defense!.Profiles!.Value[0];
                        for (double seconds = .01; seconds < 20; seconds += .01)
                        {
                            var candidate = (X: observed.X + observed.DirectionX * observed.SpeedKmS * 10 * seconds,
                                Y: observed.Y + observed.DirectionY * observed.SpeedKmS * 10 * seconds);
                            var preview = GameSessionScreen.ProjectCounterAim(player, screen.RenderStates.Select(s => s.Pose.ToSnapshot()).ToArray(), defenseProfile, 30,
                                candidate.X, candidate.Y, screen.CameraPixelsPerWorldUnit, false, world.MotionTimeMs, .25);
                            if (!preview.Unreachable && !preview.Late && preview.SnappedObjectId is not null)
                            { aim = candidate; break; }
                        }
                        Move(aim); break;
                    case 9:
                        checks["counterPreviewAndTraceSnap"] = screen.CurrentCounterAimPreview?.SnappedObjectId is not null;
                        checks["counterAimLabelContained"] = screen.AvailableMapRect().Contains(screen.CounterAimLabelBounds);
                        Capture(surface, "counter-aim"); screen.SynchronizeModifiers(false, false, true, false); Move(aim); break;
                    case 10:
                        notRun["physicalAltFreeAim"] = "Native window polls hardware modifier state each frame; single managed modifier injection does not prove held physical Alt. Native OS keyboard control is unavailable; headless Alt coverage is separate.";
                        screen.SynchronizeModifiers(false, false); Move(aim); MapClick(aim); Publish(); break;
                    case 11:
                        var counter = world.Objects.FirstOrDefault(o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile);
                        checks["actualCountermissileConfirmed"] = counter is not null; counterId = counter?.ObjectId;
                        Capture(surface, "counter-flight"); Tap(Key.Escape); Tap(Key.Tab); screen.OnKeyDown(Key.A); break;
                    case 12:
                        checks["heldEvadePreview"] = screen.CurrentEvadePreview is not null;
                        Capture(surface, "evade-preview"); screen.OnKeyUp(Key.A); Publish(); break;
                    case 13:
                        checks["evadeConfirmedAndBaseCourse"] = world.Objects.Single(o => o.ObjectId == world.PlayerShipObjectId).Evasion is not null && screen.DisplayedBaseCourse is not null;
                        break;
                    case 14:

                        handle.SetSpeedAsync(SimulationSpeed.Speed0).AsTask().GetAwaiter().GetResult(); Publish();
                        pausedMotion = world.MotionTimeMs; pausedReload = Reload(); break;
                    case 15:
                        real += 1000; world = engine.CaptureSnapshot(advanceClock: true); buffer.Update(world); break;
                    case 16:
                        checks["authoritativePauseFreezesMotionAndReload"] = world.MotionTimeMs == pausedMotion && Math.Abs(Reload() - pausedReload) < .1;
                        Capture(surface, "paused"); handle.SetSpeedAsync(SimulationSpeed.Speed1).AsTask().GetAwaiter().GetResult(); Publish();
                        Tap(Key.Escape); free = screen.AvailableMapRect(); screen.OnMouseWheel(free.MidX, free.MidY, -24); real += 1000; break;
                    case 17:
                        checks["minimumMarkerSeparateFromPhysicalRadius"] = screen.PointProjectileVisuals.Values.Any(v => v.HasMinimumMarker && v.PhysicalRadiusPx < 6);
                        Capture(surface, "minimum-markers"); Tap(Key.F);
                        if (counterId is not null)
                            for (int i = 0; i < 80 && world.Objects.Any(o => o.ObjectId == counterId) && !handle.IsDefeated; i++) Advance(100);
                        break;
                    case 18:
                        checks["counterDetonationActualJournal"] = world.CombatJournal.Any(e => e.Type == CombatEventType.CountermissileDetonated);
                        Capture(surface, "counter-blast");
                        if (ownId is not null && world.Objects.FirstOrDefault(o => o.ObjectId == ownId)?.OwnProjectile is { } flight)
                        {
                            long target = (long)Math.Min(flight.DetonationMotionTimeMs - 100, flight.LaunchMotionTimeMs + 3500);
                            while (world.MotionTimeMs < target && !handle.IsDefeated) Advance(Math.Min(100, target - world.MotionTimeMs));
                            if (!handle.IsDefeated && world.Objects.Any(o => o.ObjectId == ownId))
                            {
                                engine.ReceiveCommand(new("native-manual", 999999, scenario.GameState.PlayerShipObjectId!, flight.LauncherModuleId, CombatCommandTypes.Detonate, ownId)); Publish();
                            }
                        }
                        while (world.Objects.Single(o => o.ObjectId == world.PlayerShipObjectId).Evasion is not null && !handle.IsDefeated) Advance(100);
                        double heading = world.Objects.Single(o => o.ObjectId == world.PlayerShipObjectId).Direction;
                        screen.OnKeyDown(Key.Right); Publish(); Advance(500);
                        checks["heldTurnChangesAuthoritativeHeading"] = Math.Abs(world.Objects.Single(o => o.ObjectId == world.PlayerShipObjectId).Direction - heading) > .01;
                        screen.OnKeyUp(Key.Right); Publish(); Advance(100);
                        screen.OnKeyDown(Key.ShiftLeft); Publish(); screen.OnKeyUp(Key.ShiftLeft);
                        checks["shiftSendsFullSpeedCommand"] = connection.Commands.Any(c => c.CommandType == ShipEngineCommandTypes.Accelerate);
                        break;
                    case 19:
                        checks["manualDetonationActualJournal"] = world.CombatJournal.Any(e => e.Type == CombatEventType.ManuallyDetonated);
                        Capture(surface, "manual-blast"); completed = true; native.Close(); break;
                }
            }
            catch (Exception e) { failure = e.ToString(); native.Close(); }
        };
        try { window.Run(); }
        finally { audio.Deactivate(); handle.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
        bool passed = failure is null && completed && checks.Values.All(v => v);
        var report = new
        {
            schemaVersion = 1,
            cleanFlightCapture = args.Contains("--clean-flight"),
            localizedBattle = GameSessionScreen.CombatText("Battle", ""),
            localizedSlowProfile = GameSessionScreen.CombatText("Profile.slow", ""),
            passed,
            failure,
            completed,
            mode,
            seed = 3001,
            frame,
            actualWindowSeconds = elapsed.Elapsed.TotalSeconds,
            requested = new { width, height, scale, language = args[5] },
            actual = new { actualWidth, actualHeight, framebufferWidth, framebufferHeight },
            graphicsRenderer = typeof(SkiaWindow).GetField("_graphicsRenderer", Private)!.GetValue(window),
            graphicsVersion = typeof(SkiaWindow).GetField("_graphicsVersion", Private)!.GetValue(window),
            checks,
            notRun,
            captures,
            captureStates,
            authoritativeMotionMs = world.MotionTimeMs,
            outcome = world.EffectiveOutcome.State.ToString(),
            encounter = world.Encounter,
            managedDispatchMs = dispatchMs,
            commands = connection.Commands,
            audio = new { enabled = audioEnabled, requestedProductionCues = audioCues, auditoryAcceptance = "NOT RUN: no human hearing verification" },
            clock = "Controlled production Engine clock and matching renderer timestamp; terminal modes replay actual public-command policy tapes. No fabricated combat snapshots/outcomes.",
            input = "Managed screen callbacks; physical OS keyboard/mouse and end-to-end input-to-photon latency NOT RUN because native CUA surfaces are unavailable.",
            humanAcceptance = "OPEN; native rendering and managed callbacks do not establish human, auditory or deuteranopia acceptance"
        };
        File.WriteAllText(output, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Point native {mode} {args[4]} scale={scale} {args[5]} passed={passed};frame={frame};failure={failure}");
        return passed ? 0 : 1;

        void Publish() { world = engine.CaptureSnapshot(advanceClock: false); buffer.Update(world); }
        void Advance(long physicalMs) { real += physicalMs * 4; world = engine.CaptureSnapshot(advanceClock: true); buffer.Update(world); }
        void Tap(Key key) { screen.OnKeyDown(key); screen.OnKeyUp(key); }
        void Click(SKRect r) { screen.OnMouseDown(r.MidX * scale, r.MidY * scale); screen.OnMouseUp(r.MidX * scale, r.MidY * scale); }
        bool Contains(SKRect r) => !r.IsEmpty && r.Left >= 0 && r.Top >= 0 && r.Right * scale <= native.Size.X + 1 && r.Bottom * scale <= native.Size.Y + 1;
        void Move((double X, double Y) p) => screen.OnMouseMove((float)(native.Size.X / 2d + (p.X - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit),
            (float)(native.Size.Y / 2d + (p.Y - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit));
        void MapClick((double X, double Y) p)
        {
            Move(p); long start = Stopwatch.GetTimestamp();
            float x = (float)(native.Size.X / 2d + (p.X - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit),
                y = (float)(native.Size.Y / 2d + (p.Y - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit);
            screen.OnMouseDown(x, y); screen.OnMouseUp(x, y); dispatchMs.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        (double X, double Y) ChooseAim(double minKm, double maxKm)
        {
            var free = screen.AvailableMapRect(); var own = world.Objects.Single(o => o.ObjectId == world.PlayerShipObjectId);
            var choices = new List<(double X, double Y, double Distance)>();
            for (int x = 1; x < 10; x++) for (int y = 1; y < 10; y++)
            {
                double wx = screen.CameraFocusX + (free.Left + free.Width * x / 10 - native.Size.X / 2d) / screen.CameraPixelsPerWorldUnit,
                    wy = screen.CameraFocusY + (free.Top + free.Height * y / 10 - native.Size.Y / 2d) / screen.CameraPixelsPerWorldUnit;
                double km = double.Hypot(wx - own.X, wy - own.Y) / 10;
                if (km >= minKm && km <= maxKm) choices.Add((wx, wy, Math.Abs(km - (minKm + maxKm) / 2)));
            }
            if (choices.Count == 0) throw new InvalidOperationException($"No free-map input point within{minKm}..{maxKm}km;free={free}.");
            var chosen = choices.OrderBy(c => c.Distance).First(); return (chosen.X, chosen.Y);
        }
        double Reload() => screen.CommandsPanel.CommandPanelRows.FirstOrDefault(r => r.Weapon?.ModuleId == world.InstalledModules.First(m => m.LauncherCombat is not null).ModuleId).RemainingReloadRealSeconds;
        void Capture(SKSurface surface, string label)
        {
            string path = Path.ChangeExtension(output, "." + label + ".png");
            using var image = surface.Snapshot(); using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(path, png.ToArray()); captures.Add(Path.GetFileName(path));
            captureStates.Add(new
            {
                label,
                file = Path.GetFileName(path),
                world.MotionTimeMs,
                freeMap = new { screen.AvailableMapRect().Left, screen.AvailableMapRect().Top, screen.AvailableMapRect().Right, screen.AvailableMapRect().Bottom },
                screen.CameraFocusX,
                screen.CameraFocusY,
                screen.CameraPixelsPerWorldUnit,
                points = screen.PointProjectileVisuals.Select(pair => new
                {
                    id = pair.Key,
                    pair.Value.Profile,
                    pair.Value.Label,
                    labelBounds = new { pair.Value.LabelBounds.Left, pair.Value.LabelBounds.Top, pair.Value.LabelBounds.Right, pair.Value.LabelBounds.Bottom },
                    pair.Value.PhysicalRadiusPx,
                    pair.Value.CoreRadiusPx,
                    pair.Value.HasMinimumMarker,
                    centerX = pair.Value.DetonationCenter?.X,
                    centerY = pair.Value.DetonationCenter?.Y,
                    sourceOwn = screen.FrameEvidenceSnapshot?.Objects.FirstOrDefault(o => o.ObjectId == pair.Key)?.OwnProjectile
                }).ToArray()
            });
        }
        void Replay(SKSurface surface, IScreen? current)
        {
            if (frame < 4) return;
            if (damageFramePending)
            {
                Capture(surface, "player-hit"); damageFramePending = false; damageFrameCaptured = true;
                checks["actualPlayerHitFrameCaptured"] = world.CombatJournal.Any(e => e.Type == CombatEventType.Hit && e.TargetObjectId == world.PlayerShipObjectId);
            }
            if (terminalFrame == 0)
            {
                for (int step = 0; step < 10; step++)
                {
                    real = replayTime * 4; world = engine.CaptureSnapshot(advanceClock: true);
                    while (tapeIndex < tape!.Commands.Length && tape.Commands[tapeIndex].MotionTimeMs == replayTime)
                        connection.SendCommandAsync(tape.Commands[tapeIndex++].Command).GetAwaiter().GetResult();
                    Publish(); replayTime += 100;
                    if (world.EffectiveOutcome.State == SessionOutcomeState.Defeated || world.Encounter?.State == CombatEncounterState.Won)
                    { terminalFrame = frame; break; }
                    if (!damageFrameCaptured && world.CombatJournal.Any(e => e.Type == CombatEventType.Hit && e.TargetObjectId == world.PlayerShipObjectId))
                    { damageFramePending = true; break; }
                    if (replayTime > Harness.HorizonMs) throw new InvalidOperationException("Terminal replay exceeded fixed horizon.");
                }
                return;
            }
            if (frame == terminalFrame + 3)
            {
                checks["actualOutcomeMatchesProductionTape"] = mode == "win" ? world.Encounter?.State == CombatEncounterState.Won && tape!.Outcome == "victory" :
                    world.EffectiveOutcome.State == SessionOutcomeState.Defeated && tape!.Outcome == "defeat";
                if (mode == "win")
                {
                    checks["nonmodalSummaryAndContinue"] = screen.CombatSummary.CanContinue && Contains(screen.CombatSummary.ContinueRect);
                    Capture(surface, "won"); Click(screen.CombatSummary.ContinueRect); Publish();
                }
                else
                {
                    checks["realWindowDefeatTransition"] = current?.GetType().Name == "DefeatScreen";
                    Capture(surface, "defeat");
                }
            }
            if (frame >= terminalFrame + 6)
            {
                if (mode == "win") checks["continueAcknowledgedByEngine"] = connection.Commands.Any(c => c.CommandType == CombatEncounterCommandTypes.AcknowledgeResults) && world.Encounter?.State != CombatEncounterState.Won;
                completed = true; native.Close();
            }
        }
    }

    private sealed class DriverConnection(SimulationEngine engine) : IGameSessionConnection
    {
        private readonly Channel<AuthoritativeSnapshot> _channel = Channel.CreateUnbounded<AuthoritativeSnapshot>();
        internal readonly List<PlayerCommand> Commands = new();
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) { Commands.Add(command); engine.ReceiveCommand(command); return ValueTask.CompletedTask; }
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) { engine.SetSpeed(speed); return ValueTask.CompletedTask; }
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) { engine.SetObjectInteractionState(activeObjectId, selectedObjectId); return ValueTask.CompletedTask; }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.FromException(new NotSupportedException("Native combat harness does not drive dialogue."));
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.FromException(new NotSupportedException("Native combat harness does not persist saves."));
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await foreach (var snapshot in _channel.Reader.ReadAllAsync(cancellationToken)) yield return snapshot; }
        public ValueTask DisposeAsync() { _channel.Writer.TryComplete(); return ValueTask.CompletedTask; }
    }
    private sealed class Factory : IGameSessionFactory
    {
        public IGameSessionConnection CreateSession() => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromScenario(string path) => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromSave(string slotId) => throw new NotSupportedException();
        public ScenarioInfo[] ListScenarios() => [];
        public bool HasQuickSave() => false;
        public SaveSlotInfo[] ListSaveSlots() => [];
        public void DeleteSaveSlot(string slotId) => throw new NotSupportedException();
    }
}
