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
            window.NativeFrameRendered = (surface, current) =>
            {
                try
                {
                    if (watch.Elapsed.TotalSeconds > 90) throw new TimeoutException($"Native tactical window stage {step}.");
                    frames++;
                    if (Field<Task?>(window, "_pendingKeyboardTransition") is { } pending)
                    {
                        if (!pending.IsCompleted) return;
                        pending.GetAwaiter().GetResult();
                    }
                    switch (step)
                    {
                        case 0:
                            native.Size = new(width, height);
                            step++;
                            break;
                        case 1:
                            if (frames < 4 || handle.Buffer.Latest is null) return;
                            renderer = Field<string?>(window, "_graphicsRenderer");
                            Check("real-open-gl", Field<GRContext?>(window, "_grContext") is not null && !string.IsNullOrWhiteSpace(renderer));
                            Check("framebuffer-size", native.FramebufferSize.X == width && native.FramebufferSize.Y == height);
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
                            if (!restarted)
                            {
                                var bounds = battle.NewBattleBounds;
                                var action = Click(battle, bounds.MidX, bounds.MidY);
                                Check("new-battle-action", action == ScreenEvent.NewTacticalBattle);
                                typeof(SkiaWindow).GetField("_pendingKeyboardTransition", Private)!.SetValue(window, Route.Invoke(window, [action, null]));
                                step = 20;
                                return;
                            }
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
                            Check("F10-screenshot", Directory.Exists("Screenshots") && Directory.EnumerateFiles("Screenshots", "*.png").Any());
                            var view = battle.View;
                            battle.OnMouseWheel(view.PixelWidth / 2f, view.PixelHeight / 2f, 1);
                            Check("wheel-zoom", battle.View.PixelsPerUnit != view.PixelsPerUnit);
                            var beforePan = battle.View;
                            battle.OnMouseDown(140, 160, MouseButton.Right);
                            battle.OnMouseMove(196, 189);
                            battle.OnMouseUp(196, 189, MouseButton.Right);
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
                            Check("mouse-aim", Aim(battle));
                            foreach (var (index, id) in new[] { (1, "sides"), (2, "log"), (3, "forecast"), (0, "phase") })
                            {
                                var tabs = battle.PanelTabs.Bounds;
                                Click(battle, tabs.Left + (index + .5f) * tabs.Width / 4, tabs.MidY);
                                Check("tab-" + id, battle.PanelTabs.SelectedId == id);
                            }
                            Capture(surface, "move");
                            var overlay = battle.OverlayControls;
                            var initialOverlays = battle.CaptureFrame(width, height)!;
                            Check("overlay-defaults", initialOverlays.EnemyForecast && initialOverlays.EnemySectors && initialOverlays.AllFireSectors);
                            Click(battle, overlay.Forecast.MidX, overlay.Forecast.MidY);
                            var forecastOff = battle.CaptureFrame(width, height)!;
                            Check("forecast-toggle", !forecastOff.EnemyForecast && forecastOff.EnemySectors && forecastOff.AllFireSectors);
                            Click(battle, overlay.Sectors.MidX, overlay.Sectors.MidY);
                            var sectorsOff = battle.CaptureFrame(width, height)!;
                            Check("rear-sectors-toggle", !sectorsOff.EnemyForecast && !sectorsOff.EnemySectors && sectorsOff.AllFireSectors);
                            Click(battle, overlay.AllFire.MidX, overlay.AllFire.MidY);
                            var overlays = battle.CaptureFrame(width, height)!;
                            Check("overlay-toggles", !overlays.EnemyForecast && !overlays.EnemySectors && !overlays.AllFireSectors);
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
                            if (active.TurnBusy) { clock += 1000; return; }
                            if (active.HasSalvoPresentation)
                            {
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
                                KeyDown(Key.Tab);
                                var frame = active.CaptureFrame(width, height)!;
                                var target = frame.FirePlan?.Selected?.TargetId;
                                if (target is not null)
                                {
                                    var p = frame.View.Project(frame.Battle.Units.Single(u => u.Id == target).Pose);
                                    Click(active, p.X, p.Y); // clear then restore the valid default
                                    Click(active, p.X, p.Y);
                                }
                                Check("fire-plan", frame.FirePlan is { Rows.IsEmpty: false });
                            }
                            if (active.Flow.State!.Phase == TacticalBattlePhase.Over)
                            {
                                outcome = active.Flow.State.Outcome;
                                Check("complete-battle", outcome is not null && fireSeen && animationSeen && resultSeen && replaySeen);
                                Capture(surface, "outcome");
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
            language,
            skin = expectedSkin,
            outcome,
            input = "Managed keyboard edges through the native SkiaWindow router and mouse events through the live screen; no physical input claim.",
            checks,
            captures,
            nativeDraw
        }, new JsonSerializerOptions { WriteIndented = true }));
        return passed ? 0 : 1;

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

    private static bool Aim(TacticalBattleScreen screen)
    {
        var selected = screen.Movement.SelectedOrder!;
        var end = screen.View.Project(selected.Path[^1]);
        int x = screen.View.PixelWidth, y = screen.View.PixelHeight;
        if (end.X <= 0 || end.X >= x || end.Y <= 0 || end.Y >= y) return false;
        float tx = end.X < x / 2f ? end.X + 60 : end.X - 60, ty = end.Y < y / 2f ? end.Y + 55 : end.Y - 55;
        screen.OnMouseDown(end.X, end.Y, MouseButton.Left);
        bool captured = screen.OnMouseMove(tx, ty);
        screen.OnMouseUp(tx, ty, MouseButton.Left);
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
