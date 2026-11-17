using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

public sealed record PurchaseDroneCommand(string CommandId, long ExpectedRevision, string OfferId, string HangarModuleId, int SlotIndex)
    : DemoCampaignCommand(CommandId, ExpectedRevision);
public sealed record EquipDroneCommand(string CommandId, long ExpectedRevision, string DroneId, string OfferId)
    : DemoCampaignCommand(CommandId, ExpectedRevision);
public sealed record ReplacementSlot(string HangarModuleId, int SlotIndex);
public sealed record ReplacementOffer(string Id, string CatalogId, bool Equipment, long Total, int Stock,
    ImmutableArray<string> Items, ImmutableArray<ReplacementSlot> CompatibleSlots, string? Reason = null);
public sealed record ReplacementSnapshot(ImmutableArray<ReplacementOffer> Offers, string? LastDroneId = null, string? Reason = null);
