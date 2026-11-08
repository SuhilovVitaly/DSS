using System.Collections.Immutable;

namespace DeepSpaceSaga.Engine.TacticalBattle;

internal sealed partial class TacticalBattle
{
    private ImmutableArray<TacticalDrone> _fireOrder;

    public void InitializeFireOrder()
    {
        if (!_fireOrder.IsDefault) return;
        var order = ImmutableArray.CreateBuilder<TacticalDrone>(Drones.Length);
        foreach (var group in Drones.GroupBy(d => d.Initiative).OrderByDescending(g => g.Key))
        {
            var tied = group.ToArray();
            for (int i = tied.Length - 1; i > 0; i--)
            {
                int j = InitiativeRandom.RandInt(0, i);
                (tied[i], tied[j]) = (tied[j], tied[i]);
            }
            order.AddRange(tied);
        }
        _fireOrder = order.MoveToImmutable();
    }

    public ImmutableArray<TacticalDrone> CaptureFireOrder()
    {
        if (_fireOrder.IsDefault) throw new InvalidOperationException("Fire order must be initialized during battle assembly.");
        return _fireOrder.Where(d => d.IsActive).ToImmutableArray();
    }
}
