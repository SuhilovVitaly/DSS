using DeepSpaceSaga.Client.UI;
using Xunit;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatToneTests
{
    [Fact]
    public async Task NativeInitializationReportsDeviceOutcomeWithoutCrashing()
    {
        _ = CombatAlertAudio.CreateSession();
        bool available = await CombatAlertAudio.Initialization.WaitAsync(TimeSpan.FromSeconds(15));
        if (Environment.GetEnvironmentVariable("DSS_EXPECT_AUDIO_AVAILABLE") is { } expected)
            Assert.Equal(bool.Parse(expected), available);
    }

    [Fact]
    public void SineHasExactDurationSilentEndpointsAndFiveMillisecondEnvelope()
    {
        var tone = CombatTone.From(new(CombatAlertKind.Danger, 440, 100, 1, 0));
        short[] pcm = tone.Generate();
        Assert.Equal(4410, pcm.Length);
        Assert.Equal(0, pcm[0]);
        Assert.Equal(0, pcm[^1]);
        Assert.InRange(pcm.Max(v => Math.Abs((int)v)), 8000, 8192);
        Assert.True(pcm.Take(44).Max(v => Math.Abs((int)v)) < 1700);
        // 440 Hz crosses zero about 88 times over 100 ms.
        int crossings = Enumerable.Range(1, pcm.Length - 2).Count(i => pcm[i - 1] < 0 && pcm[i] >= 0 || pcm[i - 1] > 0 && pcm[i] <= 0);
        Assert.InRange(crossings, 86, 90);
    }

    [Theory]
    [InlineData(-1, 0, 37, 1, 44100)]
    [InlineData(99999, 999, 32767, 250, 96000)]
    [InlineData(22049, 100, 22049, 100, 44100)]
    [InlineData(22050, 100, 22050, 100, 96000)]
    public void LimitsRemainSupportedWithoutAliasing(int frequency, int duration, int expectedHz, int expectedMs, int rate)
    {
        var tone = CombatTone.From(new(CombatAlertKind.Danger, frequency, duration, 1, 0));
        Assert.Equal(expectedHz, tone.FrequencyHz);
        Assert.Equal(expectedMs, tone.DurationMs);
        Assert.Equal(rate, tone.SampleRate);
        Assert.True(tone.FrequencyHz < tone.SampleRate / 2);
        Assert.Equal(rate * expectedMs / 1000, tone.Generate().Length);
    }
}
