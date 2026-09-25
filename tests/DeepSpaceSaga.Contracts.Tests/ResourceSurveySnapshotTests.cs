using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class ResourceSurveySnapshotTests
{
    [Fact]
    public void Legacy_motion_snapshot_roundtrips_with_null_survey()
    {
        const string legacyJson = """
            {"ObjectId":"SPC-1000","X":100,"Y":200,"SpeedKmS":5,"Direction":90}
            """;
        var snapshot = JsonSerializer.Deserialize<ObjectMotionSnapshot>(legacyJson);
        var positional = new ObjectMotionSnapshot("SPC-1000", 100, 200, 5, 90);

        Assert.NotNull(snapshot);
        Assert.Equal(positional, snapshot);
        Assert.Null(snapshot.Survey);

        var json = JsonSerializer.Serialize(snapshot);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("Survey").ValueKind);
        Assert.Equal(snapshot, JsonSerializer.Deserialize<ObjectMotionSnapshot>(json));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unrevealed_survey_contains_mass_but_no_composition_or_resources(bool canStructuralScan)
    {
        // Producer fixture: identifying a permanent asteroid reveals mass, not its composition.
        var snapshot = new ObjectMotionSnapshot("SPC-1000", 100, 200, 0, 0,
            Survey: new AsteroidSurveySnapshot(1234567890123, false, canStructuralScan));

        var json = JsonSerializer.Serialize(snapshot);
        using var document = JsonDocument.Parse(json);
        var surveyJson = document.RootElement.GetProperty("Survey");
        Assert.Equal(1234567890123, surveyJson.GetProperty("MassKg").GetInt64());
        Assert.False(surveyJson.GetProperty("CompositionKnown").GetBoolean());
        Assert.Equal(canStructuralScan, surveyJson.GetProperty("CanStructuralScan").GetBoolean());
        Assert.Equal(JsonValueKind.Null, surveyJson.GetProperty("CompositionType").ValueKind);
        Assert.Equal(0, surveyJson.GetProperty("Resources").GetArrayLength());
        Assert.Equal(
            new[] { "CanStructuralScan", "CompositionKnown", "CompositionType", "MassKg", "Resources" },
            surveyJson.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));

        var restored = JsonSerializer.Deserialize<ObjectMotionSnapshot>(json);
        Assert.NotNull(restored);
        Assert.NotNull(restored.Survey);
        Assert.Equal(1234567890123, restored.Survey.MassKg);
        Assert.False(restored.Survey.CompositionKnown);
        Assert.Equal(canStructuralScan, restored.Survey.CanStructuralScan);
        Assert.Null(restored.Survey.CompositionType);
        Assert.False(restored.Survey.Resources.IsDefault);
        Assert.Empty(restored.Survey.Resources);
    }

    [Theory]
    [InlineData("Ice", "item.ice", "item.silicon")]
    [InlineData("Silicate", "item.carbon-ore", "item.silicon")]
    [InlineData("Iron", "item.iron-ore", "item.magnesium-ore")]
    public void Revealed_survey_roundtrips_exact_resource_fractions(
        string compositionType, string primaryItemTypeId, string secondaryItemTypeId)
    {
        var snapshot = new ObjectMotionSnapshot("SPC-1000", 100, 200, 0, 0,
            Survey: new AsteroidSurveySnapshot(long.MaxValue, true, false, compositionType,
                ImmutableArray.Create(
                    new ResourceFractionSnapshot(primaryItemTypeId, 999),
                    new ResourceFractionSnapshot(secondaryItemTypeId, 1))));

        var json = JsonSerializer.Serialize(snapshot);
        using var document = JsonDocument.Parse(json);
        var surveyJson = document.RootElement.GetProperty("Survey");
        Assert.Equal(long.MaxValue, surveyJson.GetProperty("MassKg").GetInt64());
        Assert.True(surveyJson.GetProperty("CompositionKnown").GetBoolean());
        Assert.False(surveyJson.GetProperty("CanStructuralScan").GetBoolean());
        Assert.Equal(compositionType, surveyJson.GetProperty("CompositionType").GetString());
        var resourcesJson = surveyJson.GetProperty("Resources");
        Assert.Equal(2, resourcesJson.GetArrayLength());
        Assert.Equal(primaryItemTypeId, resourcesJson[0].GetProperty("ItemTypeId").GetString());
        Assert.Equal(999, resourcesJson[0].GetProperty("Permille").GetInt32());
        Assert.Equal(secondaryItemTypeId, resourcesJson[1].GetProperty("ItemTypeId").GetString());
        Assert.Equal(1, resourcesJson[1].GetProperty("Permille").GetInt32());

        var restored = JsonSerializer.Deserialize<ObjectMotionSnapshot>(json);
        Assert.NotNull(restored);
        Assert.NotNull(restored.Survey);
        Assert.Equal(long.MaxValue, restored.Survey.MassKg);
        Assert.True(restored.Survey.CompositionKnown);
        Assert.False(restored.Survey.CanStructuralScan);
        Assert.Equal(compositionType, restored.Survey.CompositionType);
        Assert.False(restored.Survey.Resources.IsDefault);
        Assert.Collection(restored.Survey.Resources,
            resource => Assert.Equal(new ResourceFractionSnapshot(primaryItemTypeId, 999), resource),
            resource => Assert.Equal(new ResourceFractionSnapshot(secondaryItemTypeId, 1), resource));
        Assert.Equal(1000, restored.Survey.Resources.Sum(resource => resource.Permille));
    }

    [Fact]
    public void Default_resource_array_serializes_as_empty()
    {
        var survey = new AsteroidSurveySnapshot(1000, false, true);
        Assert.True(survey.Resources.IsDefault);

        var json = JsonSerializer.Serialize(survey);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("[]", document.RootElement.GetProperty("Resources").GetRawText());

        var restored = JsonSerializer.Deserialize<AsteroidSurveySnapshot>(json);
        Assert.NotNull(restored);
        Assert.False(restored.Resources.IsDefault);
        Assert.Empty(restored.Resources);

        const string omittedResourcesJson = """
            {"MassKg":1000,"CompositionKnown":false,"CanStructuralScan":true}
            """;
        var omittedResources = JsonSerializer.Deserialize<AsteroidSurveySnapshot>(omittedResourcesJson);
        Assert.NotNull(omittedResources);
        Assert.Null(omittedResources.CompositionType);
        Assert.True(omittedResources.Resources.IsDefaultOrEmpty);
        Assert.Equal(json, JsonSerializer.Serialize(omittedResources));
    }

    [Fact]
    public void Motion_with_copy_preserves_survey()
    {
        AsteroidSurveySnapshot?[] surveys =
        [
            null,
            new AsteroidSurveySnapshot(1000, false, true),
            new AsteroidSurveySnapshot(1000, true, false, "Ice",
                ImmutableArray.Create(new ResourceFractionSnapshot("item.ice", 1000)))
        ];

        foreach (var survey in surveys)
        {
            var original = new ObjectMotionSnapshot("SPC-1000", 100, 200, 0, 90, Survey: survey);
            var predicted = original with { X = 110, Y = 220, SpeedKmS = 5 };

            Assert.Same(survey, predicted.Survey);
            Assert.Equal(original.ObjectId, predicted.ObjectId);
            Assert.Equal(original.Direction, predicted.Direction);
            Assert.Equal(110, predicted.X);
            Assert.Equal(220, predicted.Y);
            Assert.Equal(5, predicted.SpeedKmS);
            Assert.Equal(100, original.X);
            Assert.Equal(200, original.Y);
            Assert.Equal(0, original.SpeedKmS);
        }
    }
}
