using System.Text.Json;
using System.Text.RegularExpressions;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public sealed class ModulePanelLocalizationTests
{
    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public async Task Panel_dictionary_is_complete_and_actual_rows_render_in_the_selected_language(string language)
    {
        string root = PointCombatUiFixture.Root;
        Dictionary<string, string> Read(string name) => JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(root, "Data/Locale", name + ".json")))!;
        var strings = Read(language);
        var required = Read("English").Keys.Concat(Read("Russian").Keys)
            .Where(k => k.StartsWith("Combat.Panel.", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "UI/Screens/GameSession"), "*.cs", SearchOption.AllDirectories))
            foreach (Match match in Regex.Matches(File.ReadAllText(file), "\"(Panel\\.[A-Za-z_][A-Za-z0-9_.]*)\""))
                if (!match.Groups[1].Value.EndsWith('.')) required.Add("Combat." + match.Groups[1].Value);
        Validate(strings, required);
        var incomplete = new Dictionary<string, string>(strings);
        incomplete.Remove("Combat.Panel.Reload");
        Assert.Throws<InvalidDataException>(() => Validate(incomplete, required));

        await using var fixture = await PointCombatUiFixture.Create(accuracySkill: 20);
        using (Localization.UseLocaleForTests(language))
        {
            var buffer = new SnapshotBuffer(() => 0); buffer.Update(fixture.Presented);
            var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
            using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, 1920, 1080);
            var rows = screen.CommandsPanel.CommandPanelRows;
            var torpedo = Assert.Single(rows, r => r.Weapon?.ModuleId == PointCombatUiFixture.Torpedo);
            Assert.Contains(torpedo.DetailLines, l => l.Contains(strings["Combat.Panel.Reload"], StringComparison.Ordinal));
            Assert.Contains(torpedo.DetailLines, l => l.Contains(strings["Combat.Panel.Damage"], StringComparison.Ordinal));
            Assert.DoesNotContain(rows.SelectMany(r => r.DetailLines), l => l.Contains("Combat.Panel.", StringComparison.Ordinal));
            Assert.Equal(strings["Combat.Panel.Engine"], Localization.CombatPanelCaption("Engine"));
            Assert.Equal(strings["Combat.Panel.NoOperator"], Localization.Combat("Panel.NoOperator"));
            screen.OnDeactivated();
        }
    }

    private static void Validate(Dictionary<string, string> strings, IEnumerable<string> required)
    {
        foreach (string key in required)
            if (!strings.TryGetValue(key, out string? text) || string.IsNullOrWhiteSpace(text) || text == key)
                throw new InvalidDataException("Missing module panel locale key: " + key);
    }
}
