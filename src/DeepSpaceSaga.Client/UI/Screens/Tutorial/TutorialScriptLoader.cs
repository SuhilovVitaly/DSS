using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DeepSpaceSaga.Client.UI.Screens.Tutorial;

/// <summary>
/// Loads and fully validates the battle tutorial script. A broken or missing file disables the tutorial
/// (empty script plus one warning), never the battle.
/// </summary>
internal static partial class TutorialScriptLoader
{
    internal const string RelativePath = "Data/Tutorial/battle-tutorial.json";
    private const string LinePrefix = "Tutorial.Line.";
    private static readonly string[] Phases = ["Move", "Fire", "Salvo", "Results", "Outcome"];
    private static readonly Dictionary<string, TutorialCompletion> Completions = KebabNames<TutorialCompletion>(_ => true);
    private static readonly Dictionary<string, TutorialSignal> Signals = KebabNames<TutorialSignal>(s => s != TutorialSignal.None);

    [GeneratedRegex("^[a-z0-9]+([.-][a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex IdPattern();
    [GeneratedRegex(@"^[a-z][a-z-]*(:(\{(own|selected|any|signal)\}|[^\s{}]+))?$", RegexOptions.CultureInvariant)]
    private static partial Regex AnchorPattern();
    [GeneratedRegex("^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SegmentPattern();

    internal static TutorialScript Load(string baseDir, Action<string>? warning = null)
    {
        warning ??= InterfaceLog.Write;
        string path = Path.Combine(new[] { baseDir }.Concat(RelativePath.Split('/')).ToArray());
        try
        {
            if (!File.Exists(path))
            {
                warning($"TUTORIAL: {path} not found");
                return TutorialScript.Empty;
            }
            return Parse(File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or FormatException)
        {
            warning($"TUTORIAL: {path}: {ex.Message}");
            return TutorialScript.Empty;
        }
    }

    /// <summary>Parses a script; violations throw <see cref="FormatException"/> whose message starts with the field path.</summary>
    internal static TutorialScript Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        RequireObject(root, "$", ["schemaVersion", "officer", "limits", "steps"]);
        if (!root.TryGetProperty("schemaVersion", out var version) || ReadInt(version, "schemaVersion") != 1)
            throw Error("schemaVersion", "must be 1");
        var officer = root.TryGetProperty("officer", out var officerElement) ? ReadOfficer(officerElement) : null;
        var limits = root.TryGetProperty("limits", out var limitsElement) ? ReadLimits(limitsElement) : new TutorialLimits();
        if (!root.TryGetProperty("steps", out var stepsElement) || stepsElement.ValueKind != JsonValueKind.Array)
            throw Error("steps", "must be an array");
        var steps = ImmutableArray.CreateBuilder<TutorialStep>();
        var ids = new Dictionary<string, int>(StringComparer.Ordinal);
        int index = 0;
        foreach (var element in stepsElement.EnumerateArray())
        {
            var step = ReadStep(element, $"steps[{index}]", ids);
            ids.Add(step.Id, index);
            steps.Add(step);
            index++;
        }
        return new TutorialScript(1, officer, limits, steps.ToImmutable());
    }

    private static TutorialOfficer ReadOfficer(JsonElement element)
    {
        RequireObject(element, "officer", ["nameKey", "roleKey", "portrait", "portraitKey"]);
        string name = RequireText(element, "nameKey", "officer.nameKey");
        string role = RequireText(element, "roleKey", "officer.roleKey");
        string portrait = RequireText(element, "portrait", "officer.portrait");
        string key = RequireText(element, "portraitKey", "officer.portraitKey");
        var segments = portrait.Split('/');
        if (!portrait.EndsWith(".png", StringComparison.Ordinal) ||
            segments.Any(s => s is "." or ".." || !SegmentPattern().IsMatch(s)))
            throw Error("officer.portrait", "must be a relative .png path of [A-Za-z0-9._-] segments without '..'");
        return new TutorialOfficer(name, role, portrait, key);
    }

    private static TutorialLimits ReadLimits(JsonElement element)
    {
        RequireObject(element, "limits", ["minGapMs", "autoCollapseMs", "maxLinesPerPhase"]);
        var defaults = new TutorialLimits();
        return new TutorialLimits(
            OptionalInt(element, "minGapMs", "limits.minGapMs", 0, 60000) ?? defaults.MinGapMs,
            OptionalInt(element, "autoCollapseMs", "limits.autoCollapseMs", 1000, 120000) ?? defaults.AutoCollapseMs,
            OptionalInt(element, "maxLinesPerPhase", "limits.maxLinesPerPhase", 1, 5) ?? defaults.MaxLinesPerPhase);
    }

    private static TutorialStep ReadStep(JsonElement element, string path, IReadOnlyDictionary<string, int> earlier)
    {
        RequireObject(element, path, ["id", "lineKey", "when", "complete", "anchor", "priority"]);
        string id = RequireText(element, "id", path + ".id");
        if (!IdPattern().IsMatch(id)) throw Error(path + ".id", "must match " + IdPattern());
        if (earlier.ContainsKey(id)) throw Error(path + ".id", $"duplicate step id '{id}'");
        string lineKey = RequireText(element, "lineKey", path + ".lineKey");
        if (!lineKey.StartsWith(LinePrefix, StringComparison.Ordinal) || lineKey.Length == LinePrefix.Length)
            throw Error(path + ".lineKey", $"must start with '{LinePrefix}'");
        if (!element.TryGetProperty("when", out var when)) throw Error(path + ".when", "is required");
        var condition = ReadCondition(when, path + ".when", earlier);
        string complete = RequireText(element, "complete", path + ".complete");
        if (!Completions.TryGetValue(complete, out var completion))
            throw Error(path + ".complete", "must be one of " + string.Join(", ", Completions.Keys));
        string? anchor = null;
        if (element.TryGetProperty("anchor", out var anchorElement))
        {
            anchor = anchorElement.ValueKind == JsonValueKind.String ? anchorElement.GetString() : null;
            if (anchor is null || !AnchorPattern().IsMatch(anchor)) throw Error(path + ".anchor", "must match " + AnchorPattern());
        }
        int priority = OptionalInt(element, "priority", path + ".priority", 0, 100) ?? 0;
        return new TutorialStep(id, lineKey, condition, completion, anchor, priority);
    }

    private static TutorialCondition ReadCondition(JsonElement element, string path, IReadOnlyDictionary<string, int> earlier)
    {
        RequireObject(element, path, ["phase", "turn", "minTurn", "after", "signal"]);
        string phase = element.TryGetProperty("phase", out var phaseElement) && phaseElement.ValueKind == JsonValueKind.String
            ? phaseElement.GetString()! : "";
        if (!Phases.Contains(phase, StringComparer.Ordinal)) throw Error(path + ".phase", "must be one of " + string.Join(", ", Phases));
        int? turn = OptionalInt(element, "turn", path + ".turn", 1, int.MaxValue);
        int? minTurn = OptionalInt(element, "minTurn", path + ".minTurn", 1, int.MaxValue);
        if (turn is not null && minTurn is not null) throw Error(path + ".minTurn", "cannot be combined with turn");
        string? after = null;
        if (element.TryGetProperty("after", out var afterElement))
        {
            after = afterElement.ValueKind == JsonValueKind.String ? afterElement.GetString() : null;
            if (after is null || !earlier.ContainsKey(after)) throw Error(path + ".after", "must name a step listed earlier");
        }
        var signal = TutorialSignal.None;
        if (element.TryGetProperty("signal", out var signalElement) &&
            (signalElement.ValueKind != JsonValueKind.String || !Signals.TryGetValue(signalElement.GetString()!, out signal)))
            throw Error(path + ".signal", "must be one of " + string.Join(", ", Signals.Keys));
        return new TutorialCondition(phase, turn, minTurn, after, signal);
    }

    private static void RequireObject(JsonElement element, string path, string[] allowed)
    {
        if (element.ValueKind != JsonValueKind.Object) throw Error(path, "must be an object");
        foreach (var property in element.EnumerateObject())
            if (!allowed.Contains(property.Name, StringComparer.Ordinal))
                throw Error(path == "$" ? property.Name : path + "." + property.Name, "unknown property");
    }

    private static string RequireText(JsonElement element, string name, string path)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
            throw Error(path, "must be a non-empty string");
        return value.GetString()!;
    }

    private static int? OptionalInt(JsonElement element, string name, string path, int min, int max)
    {
        if (!element.TryGetProperty(name, out var value)) return null;
        int number = ReadInt(value, path);
        if (number < min || number > max) throw Error(path, $"must be in {min}..{max}");
        return number;
    }

    private static int ReadInt(JsonElement value, string path) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number) ? number : throw Error(path, "must be an integer");

    private static FormatException Error(string path, string reason) => new($"{path}: {reason}");

    private static Dictionary<string, T> KebabNames<T>(Func<T, bool> include) where T : struct, Enum
    {
        var names = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var value in Enum.GetValues<T>())
        {
            if (!include(value)) continue;
            var text = new StringBuilder();
            foreach (char c in value.ToString())
            {
                if (char.IsUpper(c) && text.Length > 0) text.Append('-');
                text.Append(char.ToLowerInvariant(c));
            }
            names.Add(text.ToString(), value);
        }
        return names;
    }
}
