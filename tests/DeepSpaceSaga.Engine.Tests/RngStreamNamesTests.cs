using DeepSpaceSaga.Engine.Rng;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Batch 2 (Trade economy generation, story-20260822-193700.md): RngStreamNames' named
/// stream conventions and CreateDeterministicRandom's ulong -&gt; System.Random seed fold.
/// </summary>
public class RngStreamNamesTests
{
    [Fact]
    public void Station_stream_names_differ_by_objectId()
    {
        Assert.NotEqual(
            RngStreamNames.StationCredits("STATION-1"),
            RngStreamNames.StationCredits("STATION-2"));
    }

    [Fact]
    public void Station_stream_names_differ_by_fact()
    {
        Assert.NotEqual(
            RngStreamNames.StationCredits("STATION-1"),
            RngStreamNames.StationPriceCoefficient("STATION-1"));
    }

    [Fact]
    public void Station_inventory_stream_names_differ_by_itemTypeId()
    {
        Assert.NotEqual(
            RngStreamNames.StationInventory("STATION-1", "item.fuel"),
            RngStreamNames.StationInventory("STATION-1", "item.ice"));
    }

    [Fact]
    public void Station_inventory_stream_names_differ_by_objectId()
    {
        Assert.NotEqual(
            RngStreamNames.StationInventory("STATION-1", "item.fuel"),
            RngStreamNames.StationInventory("STATION-2", "item.fuel"));
    }

    [Fact]
    public void CreateDeterministicRandom_with_same_streamSeed_produces_the_same_sequence_twice()
    {
        var random1 = RngStreamNames.CreateDeterministicRandom(123456789UL);
        var random2 = RngStreamNames.CreateDeterministicRandom(123456789UL);

        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(random1.Next(), random2.Next());
        }
    }

    [Fact]
    public void CreateDeterministicRandom_folds_the_full_64_bits_not_just_the_low_32()
    {
        // Two seeds that share the exact same low 32 bits but differ in the high 32 bits
        // must fold to different System.Random seeds (a naive truncating cast would collide).
        var random1 = RngStreamNames.CreateDeterministicRandom(0x0000_0001_ABCD_EF01UL);
        var random2 = RngStreamNames.CreateDeterministicRandom(0x0000_0002_ABCD_EF01UL);

        Assert.NotEqual(random1.Next(), random2.Next());
    }
}
