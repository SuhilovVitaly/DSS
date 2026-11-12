using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.Settings;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using Silk.NET.Windowing;
using SkiaSharp;

internal static class TacticalBattleNativeEvidence
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly MethodInfo Keyboard = typeof(SkiaWindow).GetMethod("HandleKeyboardEdge", Private)!;
    private static readonly MethodInfo Route = typeof(SkiaWindow).GetMethod("HandleScreenEvent", Private)!;

    internal static int Run(string[] args)
    {
        if (args.Length != 5) throw new ArgumentException("<DSS root> <report.json> --tactical-window <scale> <WxH>");
        string root = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]);
        string directory = Path.GetDirectoryName(output)!;
        Directory.CreateDirectory(directory);
        string oldDirectory = Directory.GetCurrentDirectory();
        string settings = Path.Combine(root, "src/DeepSpaceSaga.Client/Settings.json");
        string? skinSettings = Environment.GetEnvironmentVariable("DSS_TACTICAL_NATIVE_SETTINGS");
        string? expectedSkin = Environment.GetEnvironmentVariable("DSS_TACTICAL_NATIVE_EXPECT_SKIN");
        bool pickLastSkin = Environment.GetEnvironmentVariable("DSS_TACTICAL_NATIVE_PICK_LAST") == "1";
        string language = skinSettings is null ? "Russian" : Localization.ReadLanguageSetting(skinSettings);
        TacticalOutcomeSnapshot? outcome = null;
        string saves = Path.Combine(directory, Path.GetFileNameWithoutExtension(output) + "-slots");
        string save = Path.Combine(saves, SaveSlotNaming.ToFileName(SaveSlots.Quicksave));
        string[] size = args[4].Split('x');
        int width = int.Parse(size[0], CultureInfo.InvariantCulture), height = int.Parse(size[1], CultureInfo.InvariantCulture);
        float scale = float.Parse(args[3], CultureInfo.InvariantCulture);
        var checks = new Dictionary<string, bool>();
        var captures = new List<string>();
        var drawMs = new List<double>();
        int tacticalFrames = 0;
        bool measureCadence = Environment.GetEnvironmentVariable("DSS_TACTICAL_MEASURE_CADENCE") == "1";
        const int warmup = 120, samples = 600;
        string[] cases = ["steady", "pan", "zoom", "salvo"];
        var intervals = cases.Select(_ => new List<double>(samples)).ToArray();
        var submit = cases.Select(_ => new List<double>(samples)).ToArray();
        var swap = cases.Select(_ => new List<double>(samples)).ToArray();
        var flush = cases.Select(_ => new List<double>(samples)).ToArray();
        int benchmark = -1, benchmarkFrame = 0;
        long previousPresent = 0, salvoClock = 0;
        double? refreshHz = null;
        bool panObserved = false, zoomObserved = false;
        bool vsync = true;
        int positionX = 0, positionY = 0;
        var factory = new Factory(settings, saves);
        string? renderer = null, failure = null;
        long clock = 0;
        int step = 0, frames = 0, turns = 0;
        bool fireSeen = false, animationSeen = false, resultSeen = false, replaySeen = false;
        bool restarted = false;
        (double X, double Y, double Zoom) originalCamera = default;
        var watch = Stopwatch.StartNew();
        using var locale = Localization.UseLocaleForTests(language);
        try
        {
            Directory.SetCurrentDirectory(directory);
            var engine = SimulationEngine.CreateFromSettingsFile(settings);
            engine.SetSpeed(SimulationSpeed.Speed0);
            string world = ScenarioLoader.Serialize(engine.CaptureSaveState());
            var handle = new GameSessionHandle(new LocalGameSessionConnection(engine, saves), deferDebugTuningWrites: true);
            var game = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle, uiScale: scale);
            using var window = new SkiaWindow(game, factory, null, createNativeWindow: true, skinSettings);
            typeof(SkiaWindow).GetField("_session", Private)!.SetValue(window, handle);
            typeof(SkiaWindow).GetField("_gameSessionScreen", Private)!.SetValue(window, game);
            var native = Field<IWindow>(window, "_window");
            window.NativeFrameMeasured = (screen, elapsed) =>
            {
                if (screen is TacticalBattleScreen && ++tacticalFrames > 5) drawMs.Add(elapsed);
            };
            if (measureCadence)
                window.NativeFramePresented = (screen, presentedAt, cpuMs, swapMs, flushMs) =>
                {
                    if (benchmark < 0 || screen is not TacticalBattleScreen) return;
                    if (++benchmarkFrame > warmup && benchmarkFrame <= warmup + samples)
                    {
                        intervals[benchmark].Add(Stopwatch.GetElapsedTime(previousPresent, presentedAt).TotalMilliseconds);
                        submit[benchmark].Add(cpuMs);
                        swap[benchmark].Add(swapMs);
                        flush[benchmark].Add(flushMs);
                    }
                    previousPresent = presentedAt;
                };
            window.NativeFrameRendered = (surface, current) =>
            {
                try
                {
                    if (watch.Elapsed.TotalSeconds > 125) throw new TimeoutException($"Native tactical window stage {step}.");
                    frames++;
                    if (Field<Task?>(window, "_pendingKeyboardTransition") is { } pending)
                    {
                        if (!pending.IsCompleted) return;
                        pending.GetAwaiter().GetResult();
                    }
                    switch (step)
                    {
                        case 0:
                            if (Environment.GetEnvironmentVariable("DSS_VALIDATION_SMALL_MONITOR") == "1")
                            {
                                var monitor = Silk.NET.Windowing.Monitor.GetMonitors(native).Single(m => m.Bounds.Origin == native.Position);
                                var resolution = monitor.VideoMode.Resolution ?? monitor.Bounds.Size;
                                Check("window-fits-smaller-monitor", width <= resolution.X && height <= resolution.Y);
                            }
                            native.Size = new(width, height);
                            step++;
                            break;
                        case 1:
                            if (frames < 4 || handle.Buffer.Latest is null) return;
                            renderer = Field<string?>(window, "_graphicsRenderer");
                            Check("real-open-gl", Field<GRContext?>(window, "_grContext") is not null && !string.IsNullOrWhiteSpace(renderer));
                            Check("framebuffer-size", native.FramebufferSize.X == width && native.FramebufferSize.Y == height);
                            positionX = native.Position.X; positionY = native.Position.Y;
                            refreshHz = Field<double?>(window, "_monitorRefreshHz");
                            if (measureCadence && Environment.GetEnvironmentVariable("DSS_TACTICAL_BENCH_UNCAPPED") == "1")
                                native.VSync = false;
                            vsync = native.VSync;
                            if (skinSettings is not null)
                            {
                                using var configuration = JsonDocument.Parse(File.ReadAllText(skinSettings));
                                if (configuration.RootElement.GetProperty("gameSettings").TryGetProperty("selectedMonitorIndex", out var index))
                                {
                                    var monitors = Silk.NET.Windowing.Monitor.GetMonitors(native).ToArray();
                                    var selectedMonitor = monitors[index.GetInt32()];
                                    Check("selected-monitor-position", native.Position == selectedMonitor.Bounds.Origin);
                                }
                            }
                            Check(language.ToLowerInvariant(), BattleFormat.Localize("Phase.Move") ==
                                (language == "Russian" ? "Движение" : "Movement"));
                            originalCamera = (game.CameraFocusX, game.CameraFocusY, game.CameraPixelsPerWorldUnit);
                            if (skinSettings is not null)
                            {
                                RouteEvent(ScreenEvent.OpenSettings);
                                step = 21;
                            }
                            else
                            {
                                KeyDown(Key.F8);
                                step++;
                            }
                            break;
                        case 21:
                            if (current is not SettingsScreen preferences) return;
                            if (pickLastSkin)
                            {
                                var field = SettingsLayout.SkinField(width, height);
                                preferences.OnMouseDown(field.MidX, field.MidY, MouseButton.Left);
                                preferences.OnKeyDown(Key.End);
                                preferences.OnKeyDown(Key.Enter);
                                Check("skin-preference-written", !preferences.SkinSaveFailed &&
                                    TacticalBattleSkinPreference.Read(skinSettings!) == expectedSkin);
                            }
                            Check("settings-effective-skin", preferences.SelectedSkinId == expectedSkin);
                            step = 22;
                            break;
                        case 22:
                            if (current is not SettingsScreen) return;
                            Capture(surface, "settings");
                            RouteEvent(ScreenEvent.CloseSettings);
                            step = 23;
                            break;
                        case 23:
                            if (!ReferenceEquals(current, game)) return;
                            KeyDown(Key.F8);
                            step = 2;
                            break;
                        case 2:
                            if (current is not TacticalBattleSetupScreen setup || setup.Flow.Options is null) return;
                            Check("setup", setup.CanStart && setup.UiScale == scale && setup.ScenarioSummary.Contains("Начальный бой", StringComparison.Ordinal));
                            Capture(surface, "setup");
                            var layout = setup.CurrentLayout!;
                            setup.OnMouseWheel(layout.Content.MidX * scale, layout.Content.MidY * scale, 10000);
                            layout = setup.CurrentLayout!;
                            Check("setup-scroll", layout.Scroll == layout.MaximumScroll && layout.Content.Contains(layout.Seed.MidX, layout.Seed.MidY));
                            setup.OnMouseDown(layout.Seed.MidX * scale, layout.Seed.MidY * scale, MouseButton.Left);
                            KeyDown(Key.Backspace);
                            setup.OnTextInput('−');
                            setup.OnTextInput('7');
                            Check("exact-seed-input", setup.BuildStartRequest().Seed == -7);
                            KeyDown(Key.Backspace);
                            KeyDown(Key.Backspace);
                            setup.OnTextInput('1');
                            KeyDown(Key.Enter);
                            step++;
                            break;
                        case 3:
                            if (current is not TacticalBattleScreen battle) return;
                            clock = Environment.TickCount64;
                            battle.ClockMilliseconds = () => clock;
                            Check("battle", battle.UiScale == scale && battle.Flow.State is { Seed: 1, ScenarioId: "initial", Phase: TacticalBattlePhase.Move });
                            if (expectedSkin is not null)
                                Check(restarted ? "restarted-battle-skin" : "battle-skin", battle.SkinId == expectedSkin);
                            if (!restarted && battle.SkinId == "tabletop" && Environment.GetEnvironmentVariable("DSS_TACTICAL_CLOUD_TIMING") == "1")
                                TacticalBattleCloudEvidence.Measure(Field<GRContext>(window, "_grContext"), battle.CaptureFrame(1920, 1080)!,
                                    Path.Combine(directory, Path.GetFileNameWithoutExtension(output) + "-clouds.json"));
                            ulong revision = battle.Flow.State!.Revision;
                            foreach (var key in new[] { Key.F5, Key.F9, Key.F12, Key.F8, Key.Space, Key.I, Key.Number1, Key.Number2, Key.Number3, Key.Number4 })
                                KeyDown(key);
                            var ctrlF = battle.OnKeyDown(Key.F, KeyModifiers.Control, false);
                            var ctrlS = battle.OnKeyDown(Key.S, KeyModifiers.Control, false);
                            Check("world-keys-blocked", battle.BlockedNotice.Length > 0 && !File.Exists(save) &&
                                factory.Loads == 0 && engine.CurrentSpeed == SimulationSpeed.Speed0 && battle.Flow.State!.Revision == revision &&
                                ctrlF == ScreenEvent.None && ctrlS == ScreenEvent.None);
                            KeyDown(Key.Escape);
                            Check("exit-confirmation", battle.ConfirmExit);
                            KeyDown(Key.Escape);
                            Check("cancel-exit", !battle.ConfirmExit);
                            KeyDown(Key.F10);
                            string screenshots = Path.Combine(Path.GetDirectoryName(InterfaceLog.FileName) ?? "", "Screenshots");
                            Check("F10-screenshot", Directory.Exists(screenshots) && Directory.EnumerateFiles(screenshots, "*.png").Any());
                            var moveOrder = battle.Movement.SelectedOrder!.Order;
                            ClickCard(battle, "turn+");
                            if (battle.Movement.SelectedOrder!.Order == moveOrder) ClickCard(battle, "turn-");
                            Check("pilot-move-step", battle.Movement.SelectedOrder!.Order != moveOrder);
                            ClickCard(battle, "ai");
                            Check("pilot-move-restore", battle.Movement.SelectedOrder!.Order == battle.Movement.SelectedOrder.Suggestion);
                            Check("pilot-move-panel", battle.Layout.Rects["phase"].Bottom == 152 &&
                                battle.CardCommandBounds("slider:speed").Left < 202 * height / 720f);
                            var view = battle.View;
                            battle.OnMouseWheel(600 * height / 720f, 500 * height / 720f, 1);
                            Check("wheel-zoom", battle.View.PixelsPerUnit != view.PixelsPerUnit);
                            var beforePan = battle.View;
                            battle.OnMouseDown(600 * height / 720f, 500 * height / 720f, MouseButton.Right);
                            battle.OnMouseMove(640 * height / 720f, 520 * height / 720f);
                            battle.OnMouseUp(640 * height / 720f, 520 * height / 720f, MouseButton.Right);
                            Check("right-drag", battle.View.CenterX != beforePan.CenterX);
                            KeyDown(Key.Number0);
                            KeyDown(Key.Keypad0);
                            string? selected = battle.Movement.Selected;
                            KeyDown(Key.Tab);
                            Check("tab-cycle", battle.Movement.Selected is { } other && selected != other);
                            var original = battle.Movement.SelectedOrder!.Order;
                            KeyDown(Key.Left);
                            if (battle.Movement.SelectedOrder!.Order == original) KeyDown(Key.Right);
                            Check("arrows-edit", battle.Movement.SelectedOrder!.Order != original);
                            battle.OnKeyDown(Key.Tab, KeyModifiers.Shift, false);
                            Check("shift-tab-cycle", battle.Movement.Selected == selected);
                            Check("mouse-aim", Aim(battle, height / 720f));
                            Capture(surface, "move");
                            var overlay = battle.CaptureFrame(width, height)!;
                            Check("removed-overlays", !overlay.EnemyForecast && !overlay.EnemySectors && !overlay.AllFireSectors);
                            Check("compact-panels", battle.PanelRects.Count >= 5 && overlay.View.PixelWidth == (int)(width * 720f / height));
                            step++;
                            break;
                        case 20:
                            if (current is not TacticalBattleSetupScreen again || !again.CanStart) return;
                            Check("new-battle-retains-settings", !engine.IsTacticalBattleActive && again.BuildStartRequest().Seed == 1 &&
                                again.BuildStartRequest().ScenarioId == "initial");
                            restarted = true;
                            KeyDown(Key.Enter);
                            step = 3;
                            break;
                        case 4:
                            if (current is not TacticalBattleScreen active) return;
                            if (measureCadence && !restarted && intervals[2].Count < samples)
                            {
                                if (benchmark < 0) benchmark = 0;
                                else if (intervals[benchmark].Count == samples)
                                {
                                    benchmark++;
                                    benchmarkFrame = 0;
                                    if (benchmark == 1)
                                        active.OnMouseDown(width * .5f, height * .5f, MouseButton.Right);
                                    else
                                        active.OnMouseUp(width * .5f, height * .5f, MouseButton.Right);
                                }
                                if (benchmark == 1)
                                {
                                    int offset = 30 - Math.Abs(30 - benchmarkFrame % 60);
                                    var beforeInput = active.View;
                                    active.OnMouseMove(width * .5f + offset * 2, height * .5f);
                                    panObserved |= beforeInput.CenterX != active.View.CenterX || beforeInput.CenterY != active.View.CenterY;
                                }
                                if (benchmark == 2 && benchmarkFrame % 8 == 0)
                                {
                                    var beforeInput = active.View;
                                    active.OnMouseWheel(width * .5f, height * .5f, benchmarkFrame % 16 == 0 ? 1 : -1);
                                    zoomObserved |= beforeInput.PixelsPerUnit != active.View.PixelsPerUnit;
                                }
                                return;
                            }
                            if (benchmark is >= 0 and <= 2)
                            {
                                benchmark = -1;
                                KeyDown(Key.Number0);
                            }
                            if (active.TurnBusy) { clock += 1000; return; }
                            if (active.HasSalvoPresentation)
                            {
                                if (measureCadence && !restarted && intervals[3].Count < samples)
                                {
                                    if (benchmark < 0)
                                    {
                                        benchmark = 3;
                                        benchmarkFrame = 0;
                                        salvoClock = clock;
                                    }
                                    clock = salvoClock + benchmarkFrame * 16 % 4500;
                                    return;
                                }
                                if (benchmark == 3)
                                {
                                    benchmark = -1;
                                    clock = salvoClock;
                                }
                                var frame = active.CaptureFrame(width, height)!;
                                if (!animationSeen && frame.Presentation == TacticalBattlePresentation.SalvoFlight)
                                {
                                    animationSeen = true;
                                    Capture(surface, "salvo");
                                }
                                if (!resultSeen && frame.Presentation == TacticalBattlePresentation.SalvoResults)
                                {
                                    resultSeen = true;
                                    Capture(surface, "results");
                                    var replay = active.ReplaySalvoBounds;
                                    Check("replay-control", !replay.IsEmpty);
                                    Click(active, replay.MidX, replay.MidY);
                                    replaySeen = active.CaptureFrame(width, height)!.Presentation == TacticalBattlePresentation.SalvoFlight;
                                    return;
                                }
                                KeyDown(Key.Enter);
                                return;
                            }
                            if (active.Flow.State!.Phase == TacticalBattlePhase.Fire && !fireSeen)
                            {
                                fireSeen = true;
                                Capture(surface, "fire");
                                var shown = active.CaptureFrame(width, height)!.FirePlan!.Selected!;
                                var shownId = shown.Drone.Id;
                                Check("pilot-fire-panel", active.Layout.Rects["phase"].Bottom == 152 &&
                                    !active.Layout.Rects.ContainsKey("button:hold") && !active.CardCommandBounds("hold").IsEmpty);
                                if (shown.TargetId is { } planned && !active.CardCommandBounds("target:" + planned).IsEmpty)
                                {
                                    ClickCard(active, "hold");
                                    Check("pilot-fire-hold", active.BuildFireRequest().Targets.Single(t => t.DroneId == shownId).TargetId is null);
                                    ClickCard(active, "target:" + planned);
                                    Check("pilot-fire-target", active.BuildFireRequest().Targets.Single(t => t.DroneId == shownId).TargetId == planned);
                                }
                                if (shown.CanDecoy)
                                {
                                    ClickCard(active, "decoy");
                                    Check("pilot-fire-decoy", active.BuildFireRequest().Targets.Single(t => t.DroneId == shownId).Decoy);
                                    ClickCard(active, "decoy");
                                }
                                if (shown.CanEvade)
                                {
                                    ClickCard(active, "evade");
                                    Check("pilot-fire-evade", active.BuildFireRequest().Targets.Single(t => t.DroneId == shownId).Evade);
                                    ClickCard(active, "evade");
                                    if (shown.TargetId is { } restore && !active.CardCommandBounds("target:" + restore).IsEmpty)
                                        ClickCard(active, "target:" + restore);
                                }
                                KeyDown(Key.Tab);
                                var frame = active.CaptureFrame(width, height)!;
                                var target = frame.FirePlan?.Selected?.TargetId;
                                if (target is not null)
                                {
                                    var p = frame.View.Project(frame.Battle.Units.Single(u => u.Id == target).Pose);
                                    Click(active, p.X * height / 720f, p.Y * height / 720f); // clear then restore the valid default
                                    Click(active, p.X * height / 720f, p.Y * height / 720f);
                                }
                                Check("fire-plan", frame.FirePlan is { Rows.IsEmpty: false });
                                var selectedId = active.CaptureFrame(width, height)!.SelectedDroneId!;
                                KeyDown(Key.E);
                                Check("evade-route", active.BuildFireRequest().Targets.Single(t => t.DroneId == selectedId).Evade);
                                KeyDown(Key.D);
                                Check("decoy-blocked-during-evade", !active.BuildFireRequest().Targets.Single(t => t.DroneId == selectedId).Decoy);
                                KeyDown(Key.E); KeyDown(Key.D);
                                Check("decoy-route", active.BuildFireRequest().Targets.Single(t => t.DroneId == selectedId).Decoy);
                                KeyDown(Key.D);
                                Check("orders-roundtrip", !active.BuildFireRequest().Targets.Single(t => t.DroneId == selectedId).Evade &&
                                    !active.BuildFireRequest().Targets.Single(t => t.DroneId == selectedId).Decoy);
                                Check("build-card-model", BuildCardModel.From(active.CaptureFrame(width, height)! with { HoverUnitId = null, HoverPoint = null })?.Unit.Id == selectedId);
                            }
                            if (active.Flow.State!.Phase == TacticalBattlePhase.Over)
                            {
                                outcome = active.Flow.State.Outcome;
                                Check("complete-battle", outcome is not null && fireSeen && animationSeen && resultSeen && replaySeen);
                                Capture(surface, "outcome");
                                if (!restarted)
                                {
                                    var rect = active.NewBattleBounds;
                                    var action = Click(active, rect.MidX, rect.MidY);
                                    Check("new-battle-after-outcome", action == ScreenEvent.NewTacticalBattle);
                                    RouteEvent(action); step = 20; return;
                                }
                                KeyDown(Key.Escape);
                                step++;
                                return;
                            }
                            if (++turns > 180) throw new InvalidOperationException("Battle did not finish.");
                            KeyDown(turns == 1 ? Key.KeypadEnter : Key.Enter);
                            break;
                        case 5:
                            Check("return-to-world", ReferenceEquals(current, game) && !engine.IsTacticalBattleActive &&
                                string.Equals(world, ScenarioLoader.Serialize(engine.CaptureSaveState()), StringComparison.Ordinal));
                            Check("world-camera-restored", originalCamera == (game.CameraFocusX, game.CameraFocusY, game.CameraPixelsPerWorldUnit));
                            KeyDown(Key.F5);
                            step++;
                            break;
                        case 6:
                            Check("F5-after-exit", File.Exists(save) && string.Equals(world, File.ReadAllText(save), StringComparison.Ordinal));
                            KeyDown(Key.F9);
                            step++;
                            break;
                        case 7:
                            Check("F9-after-exit", factory.Loads == 1 && current is GameSessionScreen && !ReferenceEquals(current, game));
                            Capture(surface, "returned");
                            step++;
                            native.Close();
                            break;
                    }
                }
                catch (Exception ex) { failure = ex.ToString(); native.Close(); }

                void KeyDown(Key key) => Keyboard.Invoke(window, [key]);
                void RouteEvent(ScreenEvent evt) =>
                    typeof(SkiaWindow).GetField("_pendingKeyboardTransition", Private)!.SetValue(window, Route.Invoke(window, [evt, null]));
            };
            try { window.Run(); }
            finally
            {
                game.OnDeactivated();
                Field<GameSessionHandle?>(window, "_session")?.DisposeAsync().AsTask().GetAwaiter().GetResult();
                handle.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex) { failure = ex.ToString(); }
        finally { Directory.SetCurrentDirectory(oldDirectory); }
        if (measureCadence)
        {
            checks["cadence-sample-counts"] = intervals.All(x => x.Count == samples);
            checks["cadence-pan-input"] = panObserved;
            checks["cadence-zoom-input"] = zoomObserved;
        }
        bool passed = step == 8 && failure is null && checks.Values.All(v => v);
        var sorted = drawMs.Order().ToArray();
        var nativeDraw = sorted.Length == 0 ? null : new { count = sorted.Length, p95Ms = sorted[(int)Math.Ceiling(sorted.Length * .95) - 1], maxMs = sorted[^1] };
        File.WriteAllText(output, JsonSerializer.Serialize(new
        {
            passed,
            failure,
            stage = step,
            frames,
            scale,
            width,
            height,
            renderer,
            monitorPosition = new { positionX, positionY },
            language,
            skin = expectedSkin,
            outcome,
            input = "Managed keyboard edges through the native SkiaWindow router and mouse events through the live screen; no physical input claim.",
            checks,
            captures,
            nativeDraw,
            refreshHz,
            vsync,
            targetFrameMs = 12.5,
            cadenceWarmupFrames = measureCadence ? warmup : 0,
            cadence = !measureCadence || intervals.Any(x => x.Count != samples) ? null :
                cases.Select(Cadence).ToArray()
        }, new JsonSerializerOptions { WriteIndented = true }));
        return passed ? 0 : 1;

        object Cadence(string name, int index)
        {
            var frame = MapFrameEvidence.Statistics(intervals[index]);
            return new
            {
                name,
                frame,
                submit = MapFrameEvidence.Statistics(submit[index]),
                swap = MapFrameEvidence.Statistics(swap[index]),
                flush = MapFrameEvidence.Statistics(flush[index]),
                verdict = vsync && refreshHz is > 0 and < 80 ? "display-limited"
                    : frame.P99Ms <= 12.5 ? "passed" : "failed"
            };
        }

        void ClickCard(TacticalBattleScreen screen, string action)
        {
            var bounds = screen.CardCommandBounds(action);
            if (bounds.IsEmpty) throw new InvalidOperationException("Missing pilot card command: " + action);
            Click(screen, bounds.MidX, bounds.MidY);
        }

        void Check(string name, bool ok)
        {
            checks[name] = ok;
            if (!ok) throw new InvalidOperationException(name);
        }

        void Capture(SKSurface surface, string label)
        {
            string path = Path.ChangeExtension(output, "." + label + ".png");
            using var image = surface.Snapshot();
            using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            if (image.Width != width || image.Height != height || png is null)
                throw new InvalidOperationException("Invalid GPU capture.");
            File.WriteAllBytes(path, png.ToArray());
            captures.Add(path);
        }
    }

    private static bool Aim(TacticalBattleScreen screen, float scale)
    {
        var selected = screen.Movement.SelectedOrder!;
        var end = screen.View.Project(selected.Path[^1]);
        int x = screen.View.PixelWidth, y = screen.View.PixelHeight;
        if (end.X <= 0 || end.X >= x || end.Y <= 0 || end.Y >= y) return false;
        float tx = end.X < x / 2f ? end.X + 60 : end.X - 60, ty = end.Y < y / 2f ? end.Y + 55 : end.Y - 55;
        screen.OnMouseDown(end.X * scale, end.Y * scale, MouseButton.Left);
        bool captured = screen.OnMouseMove(tx * scale, ty * scale);
        screen.OnMouseUp(tx * scale, ty * scale, MouseButton.Left);
        return captured && screen.Movement.Orders.Any(o => o.Unit.Id == selected.Unit.Id && o.Touched);
    }

    private static ScreenEvent Click(TacticalBattleScreen screen, float x, float y)
    {
        var action = screen.OnMouseDown(x, y, MouseButton.Left);
        screen.OnMouseUp(x, y, MouseButton.Left);
        return action;
    }

    private static T Field<T>(SkiaWindow window, string field) => (T)typeof(SkiaWindow).GetField(field, Private)!.GetValue(window)!;

    private sealed class Factory(string settings, string saves) : IGameSessionFactory
    {
        public int Loads { get; private set; }
        public IGameSessionConnection CreateSession() => LocalGameSessionConnection.CreateFromSettingsFile(settings, saves);
        public IGameSessionConnection CreateSessionFromSave(string slotId)
        {
            Loads++;
            return LocalGameSessionConnection.CreateFromSaveFile(settings, Path.Combine(saves, SaveSlotNaming.ToFileName(slotId)),
                debugTuningPath: Path.Combine(saves, "debug-tuning-not-present.json"));
        }
        public IGameSessionConnection CreateSessionFromScenario(string path) => LocalGameSessionConnection.CreateFromScenarioFile(settings, path, saves);
        public bool HasQuickSave() => File.Exists(Path.Combine(saves, SaveSlotNaming.ToFileName(SaveSlots.Quicksave)));
        public SaveSlotInfo[] ListSaveSlots() => [];
        public ScenarioInfo[] ListScenarios() => [];
        public void DeleteSaveSlot(string id) => throw new NotSupportedException();
    }
}
