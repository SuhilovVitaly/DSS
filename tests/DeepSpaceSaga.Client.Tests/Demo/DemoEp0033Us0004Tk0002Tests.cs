using System.Text.Json;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0033Us0004Tk0002Tests
{
    [Fact]
    public void RecruitmentAssetsAndCalendarTermsAreValid()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Root() + "/Data/Demo/services.json"));
        var recruitment = doc.RootElement.GetProperty("recruitment");
        foreach (var portrait in recruitment.GetProperty("portraits").EnumerateArray())
            Assert.True(File.Exists(Path.Combine(Root(), portrait.GetString()!)));
        Assert.Equal(86400000, recruitment.GetProperty("refreshGameTimeMs").GetInt64());
        Assert.True(recruitment.GetProperty("dailyWage").GetInt64() > 0);
        Assert.True(doc.RootElement.GetProperty("stationIds").GetArrayLength() >= 2);
    }
    private static string Root([System.Runtime.CompilerServices.CallerFilePath] string file = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "../../../src/DeepSpaceSaga.Client"));
}
