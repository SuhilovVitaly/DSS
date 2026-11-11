using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

internal static class AnomalySceneSearch
{
    internal static async Task<TacticalBattleSnapshot> First(string name, Func<TacticalBattleSnapshot, bool> predicate)
    {
        await using var scene = await Ep0019Scenes.Create("1-move");
        var catalog = scene.Fixture.Engine.TacticalBattleSessionForTests!.Battle.Catalog;
        for (long seed = 1; seed <= 64; seed++)
        {
            var session = TacticalBattleSession.TryCreate(catalog, new("anomaly-" + seed, seed), out _)!;
            for (int command = 0; command < 100; command++)
            {
                var state = session.Capture();
                if (predicate(state))
                {
                    if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } root)
                        File.WriteAllText(Path.Combine(root, name + ".json"), JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
                    return state;
                }
                if (state.Phase == TacticalBattlePhase.Over) break;
                var result = state.Phase == TacticalBattlePhase.Move ? session.Move(new("m" + command, state.BattleId, state.Revision))
                    : session.Fire(new("f" + command, state.BattleId, state.Revision,
                        state.Targets.Select(g => new TacticalFireTarget(g.DroneId, g.DefaultTargetId)).ToImmutableArray()));
                Assert.True(result.Accepted, result.Code.ToString());
            }
        }
        throw new InvalidOperationException("No real anomaly situation found in seed1..64 / first100commands: " + name);
    }
    internal static TacticalBattleFrame Frame(TacticalBattleSnapshot state, string selected, bool movement = false)
    {
        var camera = new TacticalBattleCamera(); camera.Update(state, 1280, 720);
        if (movement)
        {
            var orders = new TacticalBattleOrders(); orders.Observe(state); Assert.True(orders.Select(selected));
            return new(state, camera.View, new(1040, 6, 1274, 152), 1, false, "", SelectedDroneId: selected, Movement: orders.Capture());
        }
        var fire = new TacticalBattleFireOrders(); fire.Observe(state, selected); Assert.True(fire.Select(selected));
        return new(state, camera.View, new(1040, 6, 1274, 152), 1, false, "", SelectedDroneId: selected, FirePlan: fire.Capture());
    }
    internal static (SKBitmap Bitmap, BattleLayout Layout) Render(TacticalBattleFrame frame, string name)
    {
        var bitmap = new SKBitmap(1920, 1080); var layout = new BattleLayout { Camera = frame.View };
        BattleDesign.Recording = layout;
        try
        {
            using var canvas = new SKCanvas(bitmap); canvas.Scale(1.5f);
            var column = new PilotCardColumn();
            column.Arrange(frame, PilotCardModel.Project(frame));
            TacticalBattleMapRenderer.Draw(canvas, frame, pilotColumnBottom: column.Bounds.Bottom);
            new PhasePanel().Draw(canvas, frame, layout);
            column.Draw(canvas, PilotPortraitCatalog.Default);
            new HoverCard().Draw(canvas, frame, PilotPortraitCatalog.Default);
        }
        finally { BattleDesign.Recording = null; }
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } root)
        { using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100); File.WriteAllBytes(Path.Combine(root, name + ".png"), encoded.ToArray()); }
        return (bitmap, layout);
    }
}
