using System.Diagnostics;
using System.Text.Json;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI;

/// <summary>Opt-in package acceptance: actual GPU frame and production save/load, then exit.</summary>
internal static class NativeStartupEvidence
{
    internal static void Attach(SkiaWindow window, IGameSessionFactory factory)
    {
        if (Environment.GetEnvironmentVariable("DSS_STARTUP_EVIDENCE_PATH") is not { } output) return;
        if (!Path.IsPathFullyQualified(output)) throw new ArgumentException("DSS_STARTUP_EVIDENCE_PATH must be absolute");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        int frames = 0;
        Task? roundTrip = null;
        var timer = Stopwatch.StartNew();
        window.NativeFrameRendered = (surface, screen) =>
        {
            try
            {
                if (++frames == 8)
                {
                    if (!window.HasNativeGpu || screen?.GetType().Name != "MainMenuScreen") throw new InvalidOperationException("Expected actual GPU main menu");
                    using var image = surface.Snapshot();
                    using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
                    File.WriteAllBytes(Path.ChangeExtension(output, ".png"), encoded.ToArray());
                    roundTrip = RoundTrip(factory);
                }
                if (timer.Elapsed > TimeSpan.FromSeconds(45)) throw new TimeoutException("Package acceptance exceeded 45 seconds");
                if (roundTrip?.IsCompleted != true) return;
                roundTrip.GetAwaiter().GetResult();
                Finish(null);
            }
            catch (Exception error) { Finish(error.ToString()); }
        };
        void Finish(string? failure)
        {
            File.WriteAllText(output, JsonSerializer.Serialize(new
            {
                passed = failure is null,
                failure,
                frames,
                nativeGpu = window.HasNativeGpu,
                content = AppPaths.Current.Content,
                data = AppPaths.Current.UserData,
                config = AppPaths.Current.UserConfig,
                portable = AppPaths.Current.Portable,
                notRun = new[] { "physical input", "audible audio", "human acceptance", "native Wayland" }
            }, new JsonSerializerOptions { WriteIndented = true }));
            window.NativeFrameRendered = null;
            Environment.ExitCode = failure is null ? 0 : 1;
            window.CloseEvidenceWindow();
        }
    }

    private static async Task RoundTrip(IGameSessionFactory factory)
    {
        string slot = "package-smoke-" + Guid.NewGuid().ToString("N");
        if (factory.ListScenarios().Length == 0) throw new InvalidOperationException("No packaged scenarios");
        await using (var session = factory.CreateSession()) await session.SaveAsync(slot);
        if (!factory.ListSaveSlots().Any(s => s.SlotId == slot)) throw new InvalidOperationException("Saved slot not listed");
        await using var restored = factory.CreateSessionFromSave(slot);
        await restored.SaveAsync(slot + "-roundtrip");
    }
}
