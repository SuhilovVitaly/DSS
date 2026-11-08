using System.Collections.Immutable;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using DeepSpaceSaga.Engine.TacticalBattle.Ai;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.TacticalBattle;

internal enum TacticalDroneStatus { Air, Disabled, Destroyed }
internal readonly record struct TacticalOrder(double Speed, double Turn);
internal sealed class TacticalEngineState(BattleEngine definition)
{
    public BattleEngine Definition { get; } = definition;
    public double Factor { get; set; } = TacticalBattleConstants.IntactEngineFactor;
    public TacticalDrive Motion => new(Definition.MassKg, Definition.SpeedBonus, Definition.SpeedChange, Definition.TurnDeg, Factor);
}

internal abstract class TacticalUnit(string id, string side, TacticalPose pose, int hull, int armor, int maneuver)
{
    public string Id { get; } = id;
    public string Side { get; } = side;
    public TacticalPose Pose { get; set; } = pose;
    public int MaxHull { get; } = hull;
    public int Hull { get; set; } = hull;
    public int MaxArmor { get; } = armor;
    public int Armor { get; set; } = armor;
    public int Maneuver { get; } = maneuver;
    public abstract bool IsActive { get; }
}

internal sealed class TacticalCarrier(string id, string side, TacticalPose pose, int hull, int armor, int maneuver)
    : TacticalUnit(id, side, pose, hull, armor, maneuver)
{
    public override bool IsActive => Hull > 0;
}

internal sealed class TacticalDrone(string id, string side, TacticalPose pose, int hull, int armor, int maneuver,
    int accuracy, string pilotId, BattleLayout layout, BattleFrame frame, BattleWeapon weapon, BattleArmor plates,
    BattleShield? shield, int shieldCount, ImmutableArray<TacticalEngineState> engines, double speed)
    : TacticalUnit(id, side, pose, hull, armor, maneuver)
{
    public string PilotId { get; } = pilotId;
    public BattleLayout Layout { get; } = layout;
    public BattleFrame Frame { get; } = frame;
    public BattleWeapon Weapon { get; } = weapon;
    public BattleArmor Plates { get; } = plates;
    public BattleShield? Shield { get; } = shield;
    public int ShieldCount { get; } = shieldCount;
    public ImmutableArray<TacticalEngineState> Engines { get; } = engines;
    public int Accuracy { get; } = accuracy;
    public int Initiative { get; internal set; } = 50;
    public int Consciousness { get; internal set; }
    public double Speed { get; set; } = speed;
    public double PreviousTurn { get; set; }
    public TacticalDroneStatus Status { get; set; } = TacticalDroneStatus.Air;
    public bool Jammed { get; set; }
    public string Role { get; set; } = TacticalBattleConstants.InitialRole;
    public int RoleSince { get; set; }
    public string StrikeMode { get; set; } = TacticalBattleConstants.InitialStrikeMode;
    public string? Hunt { get; set; }
    public int Dry { get; set; }
    public int Extend { get; set; }
    public TacticalOrder Order { get; set; } = new(speed, 0);
    public override bool IsActive => Status == TacticalDroneStatus.Air;
    public TacticalLimits Limits => TacticalLimits.Of(new(Frame.FrameMassKg, Frame.ReferenceMassKg, Frame.MinSpeed,
        Frame.MaxSpeedCap, Frame.TurnCapDeg), Plates.MassKg, Weapon.MassKg, Shield?.MassKg ?? 0, ShieldCount,
        Engines.Select(e => e.Motion).ToArray());
}

internal sealed partial class TacticalBattle(TacticalBattleCatalog catalog, string scenarioId, long seed,
    ImmutableArray<TacticalDrone> drones, ImmutableArray<TacticalCarrier> carriers, ImmutableArray<string> tactics, Mulberry32 random)
{
    public TacticalBattleCatalog Catalog { get; } = catalog;
    public string ScenarioId { get; } = scenarioId;
    public long Seed { get; } = seed;
    public ImmutableArray<TacticalDrone> Drones { get; } = drones;
    public ImmutableArray<TacticalCarrier> Carriers { get; } = carriers;
    public ImmutableArray<string> Tactics { get; } = tactics;
    public Mulberry32 Random { get; } = random;
    public Mulberry32 InitiativeRandom { get; } = new(unchecked((uint)seed ^ 0x494E4954u));
    public int Turn { get; set; }
    public int RoleChanges { get; set; }
    public List<TacticalAiEvent> AiEvents { get; } = [];
    public (double X, double Y) Midpoint => ((Carriers[0].Pose.X + Carriers[1].Pose.X) / 2.0, (Carriers[0].Pose.Y + Carriers[1].Pose.Y) / 2.0);
    public IEnumerable<TacticalDrone> ActiveDrones(string side) => Drones.Where(d => d.Side == side && d.IsActive);
    public TacticalCarrier Carrier(string side) => Carriers.First(c => c.Side == side);
    public string EnemySide(string side) => side == TacticalBattleConstants.SideOrder[0] ? TacticalBattleConstants.SideOrder[1] : TacticalBattleConstants.SideOrder[0];
}
