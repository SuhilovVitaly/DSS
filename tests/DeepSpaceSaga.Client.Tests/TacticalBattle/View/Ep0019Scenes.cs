using System.Text.Json.Nodes;
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
    private Ep0019Scenes(BattleNavigationTests.Fixture fixture) => Fixture = fixture;
    internal static async Task<Ep0019Scenes> Create(string scene, bool withoutModules = false, bool withoutAnomalies = false)
    {
        string? directory = null;
        string? settings = withoutModules || withoutAnomalies ? LegacySettings(out directory, withoutModules, withoutModules || withoutAnomalies) : null;
        BattleNavigationTests.Fixture f;
        try { f = await BattleNavigationTests.Fixture.Create(engineSettingsPath: settings); }
        catch { if (directory is not null) DeleteFixture(directory); throw; }
        var result = new Ep0019Scenes(f) { _contentFixture = directory };
        try
        {
            await f.Route(ScreenEvent.OpenTacticalBattle);
            var setup = (TacticalBattleModalScreen)f.Screens.Current;
            await setup.Flow.OptionsTask!;
            await f.Route(setup.OnKeyDown(Key.Enter));
            Assert.Equal(1, result.Screen.Flow.State!.Seed);
            if (scene == "2-decoy-empty")
                f.Engine.TacticalBattleSessionForTests!.Battle.Drones.Single(d => d.Id == "Марта").DecoyCharges = 0;
            if (!scene.StartsWith("1-", StringComparison.Ordinal))
            {
                while (result.Screen.Flow.State!.Phase != TacticalBattlePhase.Fire)
                {
                    Assert.True(result.Screen.Flow.State.Turn <= 3);
                    await f.Route(ScreenEvent.MoveTacticalBattle);
                    f.FinishPresentation();
                }
                Assert.Equal(3, result.Screen.Flow.State.Turn);
                result.Screen.CaptureFrame(1920, 1080);
                // First own candidate in the canonical scenario is Marta.
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
            var hoverName = scene is "1-move-no-hover" or "5-outcome" ? null : scene == "2-fire-hover-own" ? "Олег" : "Таг-6";
            result.Screen.HoverUnitId = result.Screen.Flow.State!.Units.FirstOrDefault(u =>
                (u.Build?.PilotName ?? u.Id) == hoverName)?.Id;
            return result;
        }
        catch { await result.DisposeAsync(); throw; }
    }
    internal SKBitmap Render()
    {
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
