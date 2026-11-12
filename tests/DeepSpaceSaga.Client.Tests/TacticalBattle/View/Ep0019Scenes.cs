using System.Text.Json.Nodes;
using System.Collections.Immutable;
using DeepSpaceSaga.Motion.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle.View;

internal sealed class Ep0019Scenes : IAsyncDisposable
{
    internal BattleNavigationTests.Fixture Fixture { get; }
    internal TacticalBattleScreen Screen => (TacticalBattleScreen)Fixture.Screens.Current;
    private string? _contentFixture;
    private bool _manualDamage;
    internal TacticalBattleFrame Frame
    {
        get
        {
            var f = Screen.CaptureFrame(1920, 1080)!;
            if (!_manualDamage) return f;
            var u = f.Battle.Units.Single(u => u.Id == "Марта");
            var changed = u with
            {
                Jammed = true,
                Engines = u.Engines.SetItem(0, u.Engines[0] with { Factor = .5 }),
                Slots = u.Slots.Select(slot => slot.Kind == TacticalSlotKind.Weapon ? slot with { State = TacticalSlotState.Jammed }
                    : slot.Kind == TacticalSlotKind.Engine && slot.Index == 0 ? slot with { State = TacticalSlotState.Damaged, Factor = .5 } : slot).ToImmutableArray()
            };
            return f with { Battle = f.Battle with { Units = f.Battle.Units.Replace(u, changed) } };
        }
    }
    private Ep0019Scenes(BattleNavigationTests.Fixture fixture) => Fixture = fixture;
    internal static async Task<Ep0019Scenes> Create(string scene, bool withoutModules = false, bool withoutAnomalies = false, string? settingsPath = null)
    {
        long seed = scene == "2-anomaly-dust" ? 6 : 1;
        string? directory = null;
        string? settings = withoutModules || withoutAnomalies ? LegacySettings(out directory, withoutModules, withoutModules || withoutAnomalies) : null;
        BattleNavigationTests.Fixture f;
        try { f = await BattleNavigationTests.Fixture.Create(engineSettingsPath: settings, settingsPath: settingsPath); }
        catch { if (directory is not null) DeleteFixture(directory); throw; }
        var result = new Ep0019Scenes(f) { _contentFixture = directory };
        try
        {
            await f.Route(ScreenEvent.OpenTacticalBattle);
            var setup = (TacticalBattleModalScreen)f.Screens.Current;
            await setup.Flow.OptionsTask!;
            if (seed != 1)
            {
                var form = (TacticalBattleSetupScreen)setup; var original = form.BuildStartRequest().Loadout;
                for (int i = 0; form.SeedText.Length > 0 && i < 128; i++) { form.OnKeyDown(Key.Tab); form.OnKeyDown(Key.Backspace); }
                Assert.Empty(form.SeedText); foreach (char c in seed.ToString(System.Globalization.CultureInfo.InvariantCulture)) form.OnTextInput(c);
                Assert.Equal(seed, form.BuildStartRequest().Seed); Assert.Equal(original.ToArray(), form.BuildStartRequest().Loadout.ToArray());
            }
            await f.Route(setup.OnKeyDown(Key.Enter));
            Assert.Equal(seed, result.Screen.Flow.State!.Seed);
            if (scene == "2-decoy-empty")
                f.Engine.TacticalBattleSessionForTests!.Battle.Drones.Single(d => d.Id == (withoutModules || withoutAnomalies ? "Марта" : "Юн")).DecoyCharges = 0;
            if (!scene.StartsWith("1-", StringComparison.Ordinal))
            {
                while (result.Screen.Flow.State!.Phase != TacticalBattlePhase.Fire)
                {
                    Assert.True(result.Screen.Flow.State.Turn <= (seed != 1 ? 10 : withoutModules || withoutAnomalies ? 3 : 4));
                    await f.Route(ScreenEvent.MoveTacticalBattle);
                    f.FinishPresentation();
                }
                if (seed == 1) Assert.Equal(withoutModules || withoutAnomalies ? 3 : 4, result.Screen.Flow.State.Turn);
                result.Screen.CaptureFrame(1920, 1080);
                // Follow the first legal own candidate from the current Engine snapshot.
                while (result.Screen.CaptureFrame(1920, 1080)!.FirePlan!.Selected?.Target is null)
                    result.Screen.OnKeyDown(Key.Tab);
                if (scene == "2-evasion-plan") result.Screen.OnKeyDown(Key.E);
                if (scene == "2-no-target")
                    while (result.Screen.CaptureFrame(1920, 1080)!.FirePlan!.Selected!.Drone.Id != "Вера")
                        result.Screen.OnKeyDown(Key.Tab);
                if (scene is "2-decoy-plan" or "3-decoy-flight" or "4-decoy-results") result.Screen.OnKeyDown(Key.D);
                if (scene is "3-salvo-flight" or "4-salvo-results" or "3-decoy-flight" or "4-decoy-results")
                {
                    f.ManualAnimation = true;
                    await f.Route(ScreenEvent.FireTacticalBattle);
                    f.AdvanceAnimation(300 + (long)Math.Round(975 * 0.62));
                    if (scene is "4-salvo-results" or "4-decoy-results") result.Screen.OnKeyDown(Key.Enter);
                }
                if (scene == "5-outcome")
                {
                    for (int i = 0; result.Screen.Flow.State!.Phase != TacticalBattlePhase.Over && i < 100; i++)
                    {
                        f.FinishPresentation();
                        await f.Route(result.Screen.OnKeyDown(Key.Enter));
                    }
                    f.FinishPresentation();
                    Assert.Equal(TacticalBattlePhase.Over, result.Screen.Flow.State!.Phase);
                }
            }
            if (scene == "2-anomaly-absorption")
            {
                SelectDrone("Олег"); using (var initial = result.Render()) { }
                var frame = result.Screen.CaptureFrame(1920, 1080)!; var point = frame.View.Project(frame.Battle.Units.Single(u => u.Id == "Таг-1").Pose);
                result.Screen.OnMouseDown(point.X * 1.5f, point.Y * 1.5f, MouseButton.Left);
                Assert.Equal(.355, result.Screen.CaptureFrame(1920, 1080)!.FirePlan!.Selected!.Target!.Absorption, 12);
            }
            if (scene == "2-anomaly-dust")
            {
                await Until(s => s.Phase == TacticalBattlePhase.Fire && s.Units.Any(u => u.Side == s.PlayerSide && u.Kind == TacticalUnitKind.Drone && u.State == TacticalUnitState.Air && u.InDust));
                SelectDrone(result.Screen.Flow.State!.Units.First(u => u.Side == result.Screen.Flow.State.PlayerSide && u.Kind == TacticalUnitKind.Drone && u.State == TacticalUnitState.Air && u.InDust).Id);
            }
            if (scene == "1-anomaly-move-dust")
            {
                await Until(s => s.Phase == TacticalBattlePhase.Move && EndInDust(s) is not null);
                SelectDrone(EndInDust(result.Screen.Flow.State!)!);
            }
            if (scene == "4-anomaly-absorbed")
            {
                await Until(s => s.Shots.FirstOrDefault(shot => s.Units.Any(u => u.Id == shot.ShooterId && u.Side == s.PlayerSide))?.Absorbed == true);
                Assert.True(result.Screen.HasSalvoPresentation);
                if (result.Screen.CaptureFrame(1920, 1080)!.Salvo!.InFlight) result.Screen.OnKeyDown(Key.Enter);
            }
            var hoverName = scene.Contains("anomaly", StringComparison.Ordinal) || scene is "1-move-no-hover" or "5-outcome" ? null : scene == "2-fire-hover-own" ? "Олег" : "Таг-6";
            result.Screen.HoverUnitId = result.Screen.Flow.State!.Units.FirstOrDefault(u =>
                (u.Build?.PilotName ?? u.Id) == hoverName)?.Id;
            if (scene == "1-anomaly-hover")
            {
                using (var initial = result.Render()) { }
                var frame = result.Screen.CaptureFrame(1920, 1080)!;
                var point = frame.Battle.Anomalies.Where(a => a.Kind == TacticalAnomalyKindCode.Heat).SelectMany(a => a.Clumps)
                    .Select(c => frame.View.Project(c.X, c.Y)).First(p => p.X > 202 && p.X < 1034 && p.Y > 6 && p.Y < 714
                        && frame.Battle.Units.All(u => SKPoint.Distance(p, TacticalBattleMapRenderer.HoverPoint(frame, u)) > 25)
                        && AnomalyPresentation.HoverKind(frame with { HoverPoint = p }) == TacticalAnomalyKindCode.Heat);
                result.Screen.OnMouseMove(point.X * 1.5f, point.Y * 1.5f);
            }
            if (scene.StartsWith("2-build-", StringComparison.Ordinal))
            {
                SelectDrone("Марта"); result.Screen.HoverUnitId = null;
                if (scene == "2-build-hover")
                {
                    using (var first = result.Render()) { }
                    var f2 = result.Screen.CaptureFrame(1920, 1080)!; var point = f2.View.Project(f2.Battle.Units.Single(u => u.Id == "Вера").Pose);
                    result.Screen.OnMouseMove(point.X * 1.5f, point.Y * 1.5f);
                }
                result._manualDamage = scene == "2-build-damaged";
            }
            return result;

            void SelectDrone(string id)
            {
                for (int i = 0; result.Screen.CaptureFrame(1920, 1080)!.SelectedDroneId != id && i < 16; i++) result.Screen.OnKeyDown(Key.Tab);
                Assert.Equal(id, result.Screen.CaptureFrame(1920, 1080)!.SelectedDroneId);
            }
            async Task Until(Func<TacticalBattleSnapshot, bool> predicate)
            {
                f.ManualAnimation = true;
                for (int i = 0; !predicate(result.Screen.Flow.State!) && i < 100; i++)
                { Assert.NotEqual(TacticalBattlePhase.Over, result.Screen.Flow.State!.Phase); f.FinishPresentation(); await f.Route(result.Screen.OnKeyDown(Key.Enter)); }
                Assert.True(predicate(result.Screen.Flow.State!), scene);
                if (scene != "4-anomaly-absorbed") f.FinishPresentation();
            }
            static string? EndInDust(TacticalBattleSnapshot state)
            {
                var orders = new TacticalBattleOrders(); orders.Observe(state);
                return orders.Capture().Orders.FirstOrDefault(o => AnomalyPresentation.Inside(state, TacticalAnomalyKind.Dust, o.Path[^1]))?.Unit.Id;
            }
        }
        catch { await result.DisposeAsync(); throw; }
    }
    internal SKBitmap Render()
    {
        if (_manualDamage) return AnomalySceneSearch.Render(Frame, "manual-build-damage").Bitmap;
        var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        Screen.Render(canvas, bitmap.Width, bitmap.Height);
        return bitmap;
    }
    private static string LegacySettings(out string? directory, bool withoutModules, bool withoutAnomalies)
    {
        string validation = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT")!;
        directory = Path.Combine(validation, "content-fixture-" + Guid.NewGuid().ToString("N"));
        string content = Path.Combine(directory, "TacticalBattle");
        Directory.CreateDirectory(content);
        string source = Path.Combine(AppContext.BaseDirectory, "Data", "TacticalBattle");
        foreach (string file in Directory.GetFiles(source, "*.json", SearchOption.AllDirectories))
        {
            string target = Path.Combine(content, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
        if (withoutModules)
        {
            string layoutsPath = Path.Combine(content, "layouts.json");
            var layouts = JsonNode.Parse(File.ReadAllText(layoutsPath))!.AsObject();
            foreach (var entry in layouts) entry.Value!["module"] = null;
            File.WriteAllText(layoutsPath, layouts.ToJsonString());
        }
        if (withoutAnomalies)
        {
            string rulesPath = Path.Combine(content, "rules.json"); var rules = JsonNode.Parse(File.ReadAllText(rulesPath))!;
            rules["anomalies"]!["heatCount"] = new JsonArray(0, 0); rules["anomalies"]!["dustCount"] = new JsonArray(0, 0);
            File.WriteAllText(rulesPath, rules.ToJsonString());
        }
        var settings = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Settings.json")))!;
        foreach (var entry in settings["typeData"]!.AsObject().ToArray())
            if (entry.Value is JsonValue value && value.TryGetValue<string>(out var path))
                settings["typeData"]![entry.Key] = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
        settings["typeData"]!["tacticalBattle"] = content;
        settings["defaultScenario"] = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, settings["defaultScenario"]!.GetValue<string>()));
        string destination = Path.Combine(directory, "Settings.json");
        File.WriteAllText(destination, settings.ToJsonString());
        return destination;
    }
    private static void DeleteFixture(string directory)
    {
        string validation = Path.GetFullPath(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT")!);
        string target = Path.GetFullPath(directory);
        if (!target.StartsWith(validation + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Content fixture escaped validation root.");
        Directory.Delete(target, true);
    }
    public async ValueTask DisposeAsync()
    {
        await Fixture.DisposeAsync();
        if (_contentFixture is { } directory) { DeleteFixture(directory); _contentFixture = null; }
    }
}
