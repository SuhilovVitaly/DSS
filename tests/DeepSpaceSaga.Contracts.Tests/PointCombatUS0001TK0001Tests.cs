using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class PointCombatUS0001TK0001Tests
{
    [Fact]
    public void Retired_roll_and_auto_fields_cannot_be_serialized_even_when_nonnull()
    {
        var entry = new CombatJournalEntry(1, 0, CombatEventType.Hit, "a", "b", "p", 0, 0,
            ChanceTenths: 999, Roll: 1, LaunchMode: LaunchMode.Auto, Accuracy: 50, Attack: 50, Defense: 50);
        var json = JsonSerializer.SerializeToElement(entry);
        foreach (string field in new[] { "ChanceTenths", "Roll", "LaunchMode", "Accuracy", "Attack", "Defense" })
            Assert.False(json.TryGetProperty(field, out _));
        var defense = JsonSerializer.SerializeToElement(new DefenseSnapshot(null, DefenseState.Ready));
        Assert.False(defense.TryGetProperty("AutoEnabled", out _));
    }
    [Theory]
    [InlineData(ProjectileKind.Torpedo)]
    [InlineData(ProjectileKind.Countermissile)]
    public void Own_projectile_roundtrip_retains_captured_values(ProjectileKind kind)
    {
        var flight = new OwnProjectileSnapshot(kind, "player", "launcher-2",
            -12.25, 48.5, -100, 200, 550.125, -730.5, 1000, 12345.625,
            6, "slow", 9, 200, 20, 100);
        var motion = new ObjectMotionSnapshot("projectile-1", flight.X, flight.Y, 6, 45,
            OwnProjectile: flight);
        var snapshot = new AuthoritativeSnapshot(7, 987654321, SimulationSpeed.Speed1,
            [motion], PlayerShipObjectId: "player", SimulationTimeMs: 1250);

        var restored = JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!;

        Assert.Equal(1250, restored.MotionTimeMs);
        Assert.Equal(987654321, restored.GameTimeMs);
        var projectile = Assert.Single(restored.Objects);
        Assert.Equal(flight, projectile.OwnProjectile);
        Assert.Null(projectile.ObservedProjectile);
        Assert.Null(projectile.Torpedo);
        Assert.Null(projectile.Countermeasure);
        Assert.Equal(12345.625, projectile.OwnProjectile!.DetonationMotionTimeMs);
    }

    [Theory]
    [InlineData(ProjectileKind.Torpedo)]
    [InlineData(ProjectileKind.Countermissile)]
    public void Observed_projectile_schema_has_no_hidden_members(ProjectileKind kind)
    {
        string[] expected = ["Kind", "X", "Y", "DirectionX", "DirectionY", "SpeedKmS", "ProfileId", "BlastRadiusKm"];
        var observed = new ObservedProjectileSnapshot(kind, -100.5, 50.25, 0.6, -0.8, 16, "fast", 2.5);
        var motion = new ObjectMotionSnapshot("foreign-1", observed.X, observed.Y, 16, 36.87,
            ObservedProjectile: observed);
        var json = JsonSerializer.SerializeToNode(motion)!.AsObject();

        Assert.Equal(expected.Order(), typeof(ObservedProjectileSnapshot).GetProperties().Select(p => p.Name).Order());
        Assert.Equal(expected.Order(), json["ObservedProjectile"]!.AsObject().Select(p => p.Key).Order());
        Assert.False(json.ContainsKey("OwnProjectile"));
        var restored = JsonSerializer.Deserialize<ObjectMotionSnapshot>(json.ToJsonString())!;
        Assert.Equal(observed, restored.ObservedProjectile);
        Assert.Null(restored.OwnProjectile);
    }

    [Theory]
    [InlineData("DetonationX")]
    [InlineData("DetonationMotionTimeMs")]
    [InlineData("StartX")]
    [InlineData("LauncherModuleId")]
    [InlineData("TargetObjectId")]
    [InlineData("Route")]
    [InlineData("CapturedAttack")]
    [InlineData("HitChancePercent")]
    public void Observed_projectile_rejects_unknown_fields_even_with_permissive_serializer(string hiddenField)
    {
        var json = JsonSerializer.SerializeToNode(new ObservedProjectileSnapshot(
            ProjectileKind.Torpedo, 1, 2, 0, -1, 10, "medium", 5))!.AsObject();
        json[hiddenField] = JsonValue.Create(123);
        var permissive = new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip };

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ObservedProjectileSnapshot>(json.ToJsonString(), permissive));
        var motion = new JsonObject
        {
            ["ObjectId"] = "foreign-1",
            ["X"] = 1,
            ["Y"] = 2,
            ["SpeedKmS"] = 10,
            ["Direction"] = 0,
            ["ObservedProjectile"] = json.DeepClone()
        };
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ObjectMotionSnapshot>(motion.ToJsonString(), permissive));
    }
}
