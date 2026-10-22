using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using Harness = DeepSpaceSaga.CombatAcceptance.ProductionCombatBalanceHarness;

internal static class Program
{
    private const string Header = "seed,policy,outcome,physicalDurationMs,realDurationMs,realDurationMinutes,playerTorpedoes,countermissiles,hostileTorpedoes,playerHits,evades,commandCount,rejectedCommands,policyVersion,scenarioSha256,catalogSha256";
    private sealed record Summary(int Count, int Wins, int Defeats, int Unfinished, double WinPercent,
        double MedianRealMinutes, double P90RealMinutes, string Acceptance);

    private static int Main(string[] args)
    {
        try
        {
            if (args.SequenceEqual(new[] { "--self-test" })) { SelfTest(); return 0; }
            if (args.Length == 0 || args.SequenceEqual(new[] { "--help" }))
            {
                Console.WriteLine("CombatAnalyzer --seeds 3001:4000 --policy dodge|static --output <csv> [--root <repo>] [--input <balance.jsonl.gz>] | --self-test");
                return args.Length == 0 ? 1 : 0;
            }
            var options = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < args.Length; i += 2)
            {
                if (i + 1 == args.Length || args[i] is not ("--seeds" or "--policy" or "--output" or "--root" or "--input") ||
                    !options.TryAdd(args[i], args[i + 1])) throw new ArgumentException("Unknown, missing or duplicate option.");
            }
            var (first, last) = Seeds(options.GetValueOrDefault("--seeds", "3001:4000"));
            string policy = options.GetValueOrDefault("--policy") ?? throw new ArgumentException("--policy is required.");
            if (policy is not ("dodge" or "static")) throw new ArgumentException("Policy must be dodge or static.");
            string output = Path.GetFullPath(options.GetValueOrDefault("--output") ?? throw new ArgumentException("--output is required."));
            string raw = Path.ChangeExtension(output, ".jsonl.gz"), report = Path.ChangeExtension(output, ".summary.json");
            if (options.TryGetValue("--input", out string? source) &&
                new[] { output, raw, report }.Contains(Path.GetFullPath(source), StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Output paths must preserve the input artifact.");
            Harness.RunResult[] rows;
            if (options.TryGetValue("--input", out string? input))
                rows = Read(input).Where(r => r.Policy == policy && r.Seed >= (ulong)first && r.Seed <= (ulong)last).OrderBy(r => r.Seed).ToArray();
            else
            {
                string client = options.TryGetValue("--root", out string? root) ? Path.Combine(Path.GetFullPath(root), "src/DeepSpaceSaga.Client") : FindClient();
                var harness = new Harness(client);
                rows = Enumerable.Range(first, last - first + 1).Select(seed => harness.Run((ulong)seed, policy)).ToArray();
            }
            Validate(rows, policy, first, last);
            var summary = Summarize(rows, policy, first == 3001 && last == 4000);
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.WriteAllLines(output, new[] { Header }.Concat(rows.Select(Csv)), new UTF8Encoding(false));
            using (var file = File.Create(raw))
            using (var gzip = new GZipStream(file, CompressionLevel.Optimal))
            using (var writer = new StreamWriter(gzip, new UTF8Encoding(false)))
                foreach (var row in rows) writer.WriteLine(JsonSerializer.Serialize(row));
            var document = new
            {
                Policy = policy,
                FirstSeed = first,
                LastSeed = last,
                Harness.PolicyVersion,
                Summary = summary,
                Source = options.ContainsKey("--input") ? "validated recorded production rows; battles not rerun" : "live production Engine; shared public-command input policy",
                rows[0].ScenarioHash,
                rows[0].CatalogHash,
                Raw = Path.GetFileName(raw),
                CsvColumns = Header.Split(','),
                Duration = "x1 real=physical*4; nearest-rank P90 over all outcomes;100ms physical victory sampling",
                ReferenceTape = "not supplied; no claim of bitwise reference simulator equivalence"
            };
            string json = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(report, json, new UTF8Encoding(false)); Console.WriteLine(json);
            return Exit(summary);
        }
        catch (Exception e) when (e is ArgumentException or IOException or InvalidDataException or JsonException or InvalidOperationException or FormatException or OverflowException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine("error: " + e.Message); return 1;
        }
    }

    private static (int First, int Last) Seeds(string text)
    {
        string[] parts = text.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int first) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int last) || first < 1 || last < first || last - first >= 1000)
            throw new ArgumentException("Seed range must be positive inclusive first:last, with at most1000 seeds.");
        return (first, last);
    }

    private static string FindClient()
    {
        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                string client = Path.Combine(dir.FullName, "src/DeepSpaceSaga.Client");
                if (File.Exists(Path.Combine(client, "Settings.json"))) return client;
            }
        throw new DirectoryNotFoundException("Checkout not found; provide --root <repo> for live runs.");
    }

    private static IEnumerable<Harness.RunResult> Read(string path)
    {
        using var file = File.OpenRead(path);
        using Stream stream = path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ? new GZipStream(file, CompressionMode.Decompress) : file;
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var row = JsonSerializer.Deserialize<Harness.RunResult>(line) ?? throw new InvalidDataException("Null run record.");
            CheckRow(row); yield return row;
        }
    }

    private static void Validate(Harness.RunResult[] rows, string policy, int first, int last)
    {
        if (rows.Length != last - first + 1 || !rows.Select(r => r.Seed).SequenceEqual(Enumerable.Range(first, last - first + 1).Select(i => (ulong)i)))
            throw new InvalidDataException("Missing, duplicate, unordered or unexpected seed rows.");
        if (rows.Any(r => r.Policy != policy) || rows.Select(r => r.CatalogHash).Distinct().Count() != 1 || rows.Select(r => r.ScenarioHash).Distinct().Count() != 1)
            throw new InvalidDataException("Mixed policy, catalog or scenario provenance.");
        foreach (var row in rows) CheckRow(row);
    }

    private static void CheckRow(Harness.RunResult r)
    {
        if (r.Policy is not ("dodge" or "static") || r.PolicyVersion != Harness.PolicyVersion || r.Outcome is not ("victory" or "defeat" or "unfinished") ||
            !double.IsFinite(r.PhysicalDurationMs) || r.PhysicalDurationMs < 0 || r.PhysicalDurationMs > Harness.HorizonMs ||
            r.RealDurationMs != r.PhysicalDurationMs * 4 || r.LegacyChanceFacts != 0 || r.Policy == "static" && r.Evades != 0 ||
            r.Commands is null || r.ScenarioHash?.Length != 64 || r.CatalogHash?.Length != 64 ||
            new[] { r.PlayerTorpedoes, r.Countermissiles, r.HostileTorpedoes, r.PlayerHits, r.Evades }.Any(n => n < 0))
            throw new InvalidDataException("Invalid current production run record.");
        Convert.FromHexString(r.ScenarioHash); Convert.FromHexString(r.CatalogHash);
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var trace in r.Commands)
        {
            if (trace is null || trace.Command is not { } c || string.IsNullOrWhiteSpace(c.CommandId) || !identities.Add(c.CommandId) ||
                !Enum.TryParse<CommandResultStatus>(trace.Status, out var status) || !Enum.IsDefined(status) || status.ToString() != trace.Status || trace.MotionTimeMs < 0 || trace.MotionTimeMs > Harness.HorizonMs ||
                c.CommandType is not (CombatCommandTypes.Launch or DefenseCommandTypes.Launch or ManeuverCommandTypes.Evade) || c.TargetObjectId is not null)
                throw new InvalidDataException("Invalid, duplicate or obsolete command trace.");
            if (c.CommandType is CombatCommandTypes.Launch or DefenseCommandTypes.Launch &&
                (c.TargetWorldX is not { } x || !double.IsFinite(x) || c.TargetWorldY is not { } y || !double.IsFinite(y) || string.IsNullOrWhiteSpace(c.ProfileId)))
                throw new InvalidDataException("Point launch requires finite coordinates and a profile.");
            if (c.CommandType == ManeuverCommandTypes.Evade && (string.IsNullOrWhiteSpace(c.ThreatProjectileId) || c.EvadeSide is null || !Enum.IsDefined(c.EvadeSide.Value)))
                throw new InvalidDataException("Evade requires a visible threat identity and side.");
        }
    }

    private static Summary Summarize(Harness.RunResult[] rows, string policy, bool full)
    {
        if (rows.Length == 0) throw new InvalidDataException("Empty cohort.");
        double[] times = rows.Select(r => r.RealDurationMs / 60000).Order().ToArray();
        int wins = rows.Count(r => r.Outcome == "victory"), unfinished = rows.Count(r => r.Outcome == "unfinished");
        double percent = 100d * wins / rows.Length, p90 = times[(int)Math.Ceiling(.9 * times.Length) - 1];
        bool pass = rows.Length == 1000 && unfinished == 0 && p90 <= 5.5 && (policy == "dodge" ? percent is >= 72 and <= 88 : percent <= 10);
        return new(rows.Length, wins, rows.Count(r => r.Outcome == "defeat"), unfinished, percent,
            (times[(times.Length - 1) / 2] + times[times.Length / 2]) / 2, p90, full ? pass ? "PASS" : "OUTSIDE_CORRIDOR" : "NOT_EVALUATED_PARTIAL_CORPUS");
    }

    private static int Exit(Summary summary) => summary.Acceptance == "OUTSIDE_CORRIDOR" ? 2 : 0;
    private static string Cell(object value) => Escape(value is double number ? number.ToString("R", CultureInfo.InvariantCulture) : Convert.ToString(value, CultureInfo.InvariantCulture)!);
    private static string Escape(string value) => value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? '"' + value.Replace("\"", "\"\"") + '"' : value;
    private static string Csv(Harness.RunResult r) => string.Join(',', new object[] { r.Seed, r.Policy, r.Outcome, r.PhysicalDurationMs,
        r.RealDurationMs, r.RealDurationMs / 60000, r.PlayerTorpedoes, r.Countermissiles, r.HostileTorpedoes, r.PlayerHits, r.Evades,
        r.Commands.Length, r.Commands.Count(c => c.Status == nameof(CommandResultStatus.Rejected)), r.PolicyVersion, r.ScenarioHash, r.CatalogHash }.Select(Cell));

    private static void SelfTest()
    {
        int checks = 0;
        void Check(bool ok) { if (!ok) throw new InvalidOperationException("Self-test check failed: " + (checks + 1)); checks++; }
        Harness.RunResult[] Fake(string policy, int wins, double real = 180000) => Enumerable.Range(3001, 1000)
            .Select((seed, i) => new Harness.RunResult((ulong)seed, policy, Harness.PolicyVersion, new('A', 64), new('B', 64),
                i < wins ? "victory" : "defeat", real / 4, real, 0, 0, 0, 0, 0, 0, [])).ToArray();
        Check(Seeds("3001:4000") == (3001, 4000));
        Check(Header.Split(',').Length == 16 && Csv(Fake("static", 0)[0]).Split(',').Length == 16);
        Check(Escape("a,b\"c") == "\"a,b\"\"c\"");
        Check(Exit(Summarize(Fake("dodge", 720), "dodge", true)) == 0);
        Check(Exit(Summarize(Fake("dodge", 880, 330000), "dodge", true)) == 0);
        Check(Exit(Summarize(Fake("dodge", 719), "dodge", true)) == 2);
        Check(Exit(Summarize(Fake("dodge", 881), "dodge", true)) == 2);
        Check(Exit(Summarize(Fake("static", 100), "static", true)) == 0);
        Check(Exit(Summarize(Fake("static", 101), "static", true)) == 2);
        Check(Exit(Summarize(Fake("dodge", 800, 330001), "dodge", true)) == 2);
        Check(Summarize(Fake("dodge", 800)[..10], "dodge", false).Acceptance == "NOT_EVALUATED_PARTIAL_CORPUS");
        bool refused = false;
        try { var bad = Fake("static", 0); bad[1] = bad[0]; Validate(bad, "static", 3001, 4000); } catch (InvalidDataException) { refused = true; }
        Check(refused);
        refused = false;
        try { CheckRow(Fake("static", 0)[0] with { Commands = [null!] }); } catch (InvalidDataException) { refused = true; }
        Check(refused);
        refused = false;
        try
        {
            CheckRow(Fake("static", 0)[0] with { Commands = [new(0, new("obsolete", 1, "SPC-0001", "launcher", CombatCommandTypes.Fire, TargetObjectId: "npc"), "Executed", null)] });
        }
        catch (InvalidDataException) { refused = true; }
        Check(refused);
        refused = false;
        try
        {
            CheckRow(Fake("static", 0)[0] with { Commands = [new(0, new("status", 1, "SPC-0001", "launcher", CombatCommandTypes.Launch, TargetWorldX: 100, TargetWorldY: 0, ProfileId: "fast"), "999", null)] });
        }
        catch (InvalidDataException) { refused = true; }
        Check(refused);
        var live = new Harness(FindClient()).Run(1, "static"); CheckRow(live);
        Check(live.PlayerTorpedoes > 0 && live.HostileTorpedoes > 0 && live.Commands.All(c => c.Command.TargetObjectId is null));
        Console.WriteLine($"Analyzer_self_test_checks_seed_count_columns_and_exit_status PASS: {checks} checks; synthetic statistics boundaries + real production seed1 integrity probe; balance acceptance NOT EVALUATED.");
    }
}
