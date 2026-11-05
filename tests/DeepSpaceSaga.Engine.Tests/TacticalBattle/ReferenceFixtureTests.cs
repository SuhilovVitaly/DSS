using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class ReferenceFixtureTests
{
    internal static string Root
    {
        get
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "DeepSpaceSaga.sln"))) return Path.Combine(dir.FullName, "VData/Planning/V4.0/Reference");
            throw new DirectoryNotFoundException("DSS repository not found.");
        }
    }
    internal static JsonDocument Open(string name) => JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, name + ".json")));
    internal static double ReadDoubleHex(string text)
    {
        if (text.Length != 16) throw new FormatException("Expected 8 little-endian IEEE bytes.");
        return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(Convert.FromHexString(text)));
    }

    [Theory]
    [InlineData("0000000000000000", 0L)]
    [InlineData("0000000000000080", long.MinValue)]
    [InlineData("000000000000f03f", 0x3ff0000000000000L)]
    [InlineData("0100000000000000", 1L)]
    [InlineData("000000000000f07f", 0x7ff0000000000000L)]
    public void HexReaderPreservesExactIeeeBits(string hex, long expected) => Assert.Equal(expected, BitConverter.DoubleToInt64Bits(ReadDoubleHex(hex)));

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("000000000000000000")]
    [InlineData("zz00000000000000")]
    public void HexReaderRejectsMalformedImages(string text) => Assert.Throws<FormatException>(() => ReadDoubleHex(text));

    [Theory]
    [InlineData("battles")]
    [InlineData("trace-seed-1")]
    [InlineData("geometry")]
    [InlineData("series")]
    public void IndependentSourceFingerprintAndActualExportEnvironmentAreRecorded(string name)
    {
        using var doc = Open(name);
        var header = doc.RootElement.GetProperty("header");
        Assert.Equal("independent-markdown", header.GetProperty("source").GetString());
        Assert.StartsWith("glibc ", header.GetProperty("exporterLibc").GetString());
        Assert.StartsWith("Linux-", header.GetProperty("exporterOs").GetString());
        Assert.NotEmpty(header.GetProperty("exporterPython").GetString()!);
        Assert.Equal("decimal-6", header.GetProperty("tracePrecision").GetString());
        Assert.Contains("unrounded trace IEEE bits", header.GetProperty("missingCoverage").EnumerateArray().Select(e => e.GetString()));
        foreach (var hash in header.GetProperty("sourceSha256").EnumerateObject())
        {
            string source = File.ReadAllText(Path.Combine(Root, hash.Name)).Replace("\r\n", "\n");
            Assert.Equal(hash.Value.GetString(), Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant());
        }
    }

    [Fact]
    public void AllNinetyBattleRowsAndPublishedCountersMatchMarkdown()
    {
        using var doc = Open("battles");
        var rows = File.ReadLines(Path.Combine(Root, "battles.md")).Where(l => l.StartsWith('|')).Skip(2)
            .Select(l => l.Trim('|').Split('|').Select(c => c.Trim()).ToArray()).ToArray();
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal(90, data.GetArrayLength());
        Assert.Equal(rows.Length, data.GetArrayLength());
        for (int i = 0; i < rows.Length; i++)
        {
            var r = rows[i];
            var actual = data[i];
            Assert.Equal(int.Parse(r[0]), actual.GetProperty("seed").GetInt32());
            Assert.Equal(r[1], actual.GetProperty("tactic").GetString());
            Assert.Equal(r[2], actual.GetProperty("outcome").GetString());
            Assert.Equal(int.Parse(r[3]), actual.GetProperty("turns").GetInt32());
            Assert.Equal(r[4].Split('/').Select(int.Parse), actual.GetProperty("losses").EnumerateArray().Select(e => e.GetInt32()));
            Assert.Equal(r[5].Split('/').Select(int.Parse), actual.GetProperty("carrierHull").EnumerateArray().Select(e => e.GetInt32()));
            string[] counters = ["shots", "hits", "rearKills", "systems", "disabled"];
            for (int j = 0; j < counters.Length; j++) Assert.Equal(int.Parse(r[6 + j]), actual.GetProperty(counters[j]).GetInt32());
            int[] calls = r[11].Split('+').Select(int.Parse).ToArray();
            Assert.Equal(calls[0], actual.GetProperty("assemblyCalls").GetInt32());
            Assert.Equal(calls[1], actual.GetProperty("turnCalls").GetInt32());
            Assert.Equal(calls.Sum(), actual.GetProperty("totalCalls").GetInt32());
            Assert.Equal(r[12], actual.GetProperty("reason").GetProperty("text").GetString());
        }
        Assert.Equal(new[] { "dogfight", "raid", "defence", "balanced", "commander" }, data.EnumerateArray().Select(e => e.GetProperty("tactic").GetString()).Distinct());
    }

    [Fact]
    public void CompleteTraceHasAllStagesAndSourcePrecisionRatherThanInventedBits()
    {
        using var doc = Open("trace-seed-1");
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal(6, data.GetProperty("decimalPlaces").GetInt32());
        Assert.Equal(42, data.GetProperty("assembly").GetProperty("calls").GetInt32());
        Assert.Equal(10, data.GetProperty("assembly").GetProperty("units").GetArrayLength());
        var turns = data.GetProperty("turns");
        Assert.Equal(19, turns.GetArrayLength());
        int previous = 42;
        for (int i = 0; i < turns.GetArrayLength(); i++)
        {
            var turn = turns[i];
            Assert.Equal(i + 1, turn.GetProperty("number").GetInt32());
            int planning = turn.GetProperty("planningCalls").GetInt32();
            Assert.True(planning >= previous);
            Assert.Equal(planning, turn.GetProperty("salvoCallsBefore").GetInt32());
            previous = turn.GetProperty("salvoCallsAfter").GetInt32();
            Assert.True(previous >= planning);
            Assert.Equal(turn.GetProperty("orders").GetArrayLength(), turn.GetProperty("positions").GetArrayLength());
            Assert.Equal(12, turn.GetProperty("states").GetArrayLength());
        }
        Assert.Equal(308, previous);
        Assert.Equal(136.334406, data.GetProperty("assembly").GetProperty("units")[0].GetProperty("x").GetDouble());
        Assert.Equal(349, turns[18].GetProperty("states")[11].GetProperty("hull").GetInt32());
    }

    [Fact]
    public void SeriesRetainsRoundedPublishedMeasurementsAndGeometryImagesDecode()
    {
        using var series = Open("series");
        var data = series.RootElement.GetProperty("data");
        Assert.Equal(5, data.GetArrayLength());
        Assert.All(data.EnumerateArray(), r => Assert.Equal(1000, r.GetProperty("battles").GetInt32()));
        Assert.Equal(80.9, data[0].GetProperty("winMaxOneLossPercent").GetDouble());
        Assert.Equal(57.2, data[0].GetProperty("winNoLossPercent").GetDouble());
        Assert.Equal(19.7, data[0].GetProperty("averageTurns").GetDouble());
        using var geometry = Open("geometry");
        var vectors = geometry.RootElement.GetProperty("data");
        Assert.Equal(1088, vectors.GetArrayLength());
        foreach (var vector in vectors.EnumerateArray())
        {
            Assert.True(double.IsFinite(ReadDoubleHex(vector.GetProperty("expected").GetString()!)));
            foreach (var argument in vector.GetProperty("arguments").EnumerateArray()) Assert.True(double.IsFinite(ReadDoubleHex(argument.GetString()!)));
        }
    }
}
