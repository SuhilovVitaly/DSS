namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Metadata for one save slot, as listed in the client's save window.
/// Plain JSON-serializable DTO — no graphics dependency, same shape for
/// in-process and (future) network implementations.
/// DisplayName and SavedAtUtc are derived purely from the save file's name and
/// filesystem last-write time; they are never persisted as extra fields inside
/// the save's own JSON schema.
/// </summary>
public sealed record SaveSlotInfo(
    string SlotId,
    string DisplayName,
    DateTime SavedAtUtc);
