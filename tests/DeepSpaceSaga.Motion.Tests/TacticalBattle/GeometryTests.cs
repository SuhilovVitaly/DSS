using System.Buffers.Binary;
using System.Text.Json;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Motion.Tests.TacticalBattle;

public sealed class GeometryTests
{
    public static IEnumerable<object[]> Vectors()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(System.IO.Path.Combine(dir.FullName, "DeepSpaceSaga.sln"))) dir = dir.Parent;
        if (dir is null) throw new DirectoryNotFoundException("DSS repository not found.");
        using var doc = JsonDocument.Parse(File.ReadAllText(System.IO.Path.Combine(dir.FullName, "VData/Planning/V4.0/Reference/geometry.json")));
        Assert.Equal(TacticalGeometry.HypotCPythonVersion, doc.RootElement.GetProperty("header").GetProperty("exporterPython").GetString());
        int index = 0;
        foreach (var vector in doc.RootElement.GetProperty("data").EnumerateArray())
            yield return [index++, vector.GetProperty("operation").GetString()!,
                vector.GetProperty("arguments")[0].GetString()!, vector.GetProperty("arguments").GetArrayLength() > 1 ? vector.GetProperty("arguments")[1].GetString()! : "0000000000000000",
                vector.GetProperty("expected").GetString()!];
    }

    [Theory]
    [MemberData(nameof(Vectors))]
    public void IndependentGlibcVectorsMatchExactIeeeImages(int index, string operation, string first, string second, string expected)
    {
        _ = index;
        double a = Decode(first), b = Decode(second);
        double actual = operation switch
        {
            "norm" => TacticalGeometry.Norm(a),
            "sin" => TacticalGeometry.Sin(a),
            "cos" => TacticalGeometry.Cos(a),
            "atan2" => TacticalGeometry.Atan2(a, b),
            "hypot" => TacticalGeometry.Hypot(a, b),
            _ => throw new InvalidOperationException(operation)
        };
        Assert.Equal(BitConverter.DoubleToInt64Bits(Decode(expected)), BitConverter.DoubleToInt64Bits(actual));
    }

    [Fact]
    public void PathIncludesStartAndPreservesSubstepCourseUntilFinalNormalization()
    {
        var start = new TacticalPose(10, 20, 170);
        var path = TacticalGeometry.Path(start, 80, 40, 4);
        Assert.Equal(5, path.Length);
        Assert.Equal(start, path[0]);
        Assert.Equal(180, path[1].Course);
        Assert.Equal(190, path[2].Course);
        Assert.Equal(200, path[3].Course);
        Assert.Equal(-150, path[4].Course);
        double x = start.X, y = start.Y;
        for (int i = 1; i <= 4; i++)
        {
            double radians = (170 + i * 10) * 0.017453292519943295;
            x += Math.Cos(radians) * 20;
            y += Math.Sin(radians) * 20;
            Assert.Equal(x, path[i].X);
            Assert.Equal(y, path[i].Y);
        }
        Assert.Equal(path[^1], TacticalGeometry.Integrate(start, 80, 40, 4));
    }

    [Fact]
    public void AxesDistanceAndNormalizationUseSpecifiedConventions()
    {
        Assert.Equal(90, TacticalGeometry.Bearing(0, 0, 0, 1));
        Assert.Equal(-90, TacticalGeometry.Bearing(0, 0, 0, -1));
        Assert.Equal(180, TacticalGeometry.Bearing(0, 0, -1, 0));
        Assert.Equal(5, TacticalGeometry.Dist(1, 2, 4, 6));
        Assert.Equal(0, TacticalGeometry.Dist(1, 2, 1, 2));
        Assert.Equal(-180, TacticalGeometry.Norm(180));
        Assert.Equal(0, TacticalGeometry.Norm(-0.0));
        Assert.Equal(0, TacticalGeometry.Clamp(10, 10, 0));
        var vector = TacticalGeometry.Vec(90, 3);
        Assert.Equal(Math.Cos(90 * 0.017453292519943295) * 3, vector.X);
        Assert.Equal(3, vector.Y);
    }

    [Fact]
    public void HypotScalesExtremesAndInfinityPrecedesNaNLikeCPython()
    {
        Assert.Equal(0L, BitConverter.DoubleToInt64Bits(TacticalGeometry.Hypot(-0.0, -0.0)));
        Assert.Equal(double.PositiveInfinity, TacticalGeometry.Hypot(double.NaN, double.NegativeInfinity));
        Assert.True(double.IsNaN(TacticalGeometry.Hypot(double.NaN, 1)));
        Assert.Equal(double.PositiveInfinity, TacticalGeometry.Hypot(double.MaxValue, double.MaxValue));
        Assert.Equal(double.Epsilon, TacticalGeometry.Hypot(double.Epsilon, double.Epsilon));
        Assert.Equal(1e300, TacticalGeometry.Hypot(1e300, 1e-300));
    }

    [Fact]
    public void InvalidMovementInputsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TacticalGeometry.Integrate(new(0, 0, 0), 1, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => TacticalGeometry.Path(new(0, 0, 0), -1, 1, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => TacticalGeometry.Integrate(new(double.NaN, 0, 0), 1, 1, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => TacticalGeometry.Integrate(new(0, 0, 0), 1, double.PositiveInfinity, 4));
    }

    private static double Decode(string text) => BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(Convert.FromHexString(text)));
}
