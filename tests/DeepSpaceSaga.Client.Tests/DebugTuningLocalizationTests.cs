using System.Text.Json;
using System.Text.RegularExpressions;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class DebugTuningLocalizationTests
{
    private static string ClientRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));

    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void Every_static_and_catalog_derived_key_exists_and_localized_render_allocates_nothing(string language)
    {
        var catalog = LocalGameSessionConnection.LoadDebugTuningDefaults(Path.Combine(ClientRoot, "Settings.json"));
        var required = new HashSet<string>(DebugTuningPanel.SectionKeys, StringComparer.Ordinal);
        foreach (string file in new[] { "Controls/DebugTuningPanel.cs", "GameSessionScreen.DebugTuning.cs" })
        {
            string source = File.ReadAllText(Path.Combine(ClientRoot, "UI/Screens/GameSession", file));
            foreach (Match match in Regex.Matches(source, "\"(Debug\\.[A-Za-z_][A-Za-z0-9_]*)\"")) required.Add(match.Groups[1].Value);
        }
        foreach (var value in catalog.Defaults)
        {
            required.Add("Debug." + value.Field);
            if (value.ProfileId is not null) required.Add("Debug." + value.ProfileId);
        }
        foreach (string reason in new[] { DebugTuningReasonCodes.UnknownParameter, DebugTuningReasonCodes.ValueOutOfRange, DebugTuningReasonCodes.SessionNotRunning }) required.Add("Debug." + reason);
        var strings = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(ClientRoot, "Data/Locale", language + ".json")))!;
        Validate(strings, required);
        var incomplete = new Dictionary<string, string>(strings); incomplete.Remove("Debug.Title");
        Assert.Throws<InvalidDataException>(() => Validate(incomplete, required));
        using var locale = Localization.UseLocaleForTests(language);
        using var panel = new DebugTuningPanel() { Visible = true };
        using var surface = SKSurface.Create(new SKImageInfo(800, 1100));
        var snapshot = catalog with { Revision = 1, Values = catalog.Values.SetItem(0, catalog.Values[0] with { Value = 5.5 }) };
        var status = new DebugTuningFileStatus(1, new DateTimeOffset(2026, 10, 22, 12, 0, 5, TimeSpan.Zero), null);
        var canvas = surface.Canvas;
        for (int i = 0; i < 100; i++) { panel.Prepare(800, 1100, snapshot); panel.SetFeedback(null, null, false, false, status, 1); panel.Render(canvas); }
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) { panel.Prepare(800, 1100, snapshot); panel.SetFeedback(null, null, false, false, status, 1); panel.Render(canvas); }
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        const string directory = "D:/DeepSpaceSaga/artifacts/validation/ep0013/panel-locales-20261022";
        Directory.CreateDirectory(directory);
        Save("top");
        panel.ToggleHelp(panel.Bounds.Right - 20, panel.Bounds.Top + 15);
        canvas.Clear(SKColors.Black); panel.Render(canvas); Save("help");
        void Save(string suffix)
        {
            using var image = surface.Snapshot(); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var output = File.Create(Path.Combine(directory, language + "-" + suffix + ".png")); data.SaveTo(output);
        }
    }

    private static void Validate(Dictionary<string, string> strings, IEnumerable<string> required)
    {
        foreach (string key in required)
            if (!strings.TryGetValue(key, out string? text) || string.IsNullOrWhiteSpace(text) || text == key)
                throw new InvalidDataException("Missing debug locale key: " + key);
    }
}
