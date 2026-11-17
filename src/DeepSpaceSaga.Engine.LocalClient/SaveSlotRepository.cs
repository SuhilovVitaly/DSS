using DeepSpaceSaga.Contracts;
using System.Text.Json;

namespace DeepSpaceSaga.Engine.LocalClient;

/// <summary>
/// Lists and deletes save slots. Reads only gameTimeMs for display, without loading
/// or migrating the world. Corrupt metadata gets a safe placeholder.
/// </summary>
public static class SaveSlotRepository
{
    /// <summary>Newest readable compatible world; a corrupt newer slot never hides an older valid slot.</summary>
    public static SaveSlotInfo? GetLatestValidSlot(string saveDirectory, string? settingsPath = null)
    {
        foreach (var slot in ListSlots(saveDirectory).OrderByDescending(s => s.SavedAtUtc).ThenBy(s => s.SlotId, StringComparer.Ordinal))
        {
            string path = Path.Combine(saveDirectory, SaveSlotNaming.ToFileName(slot.SlotId));
            try
            {
                if (settingsPath is null)
                {
                    var world = Scenario.ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true, isSave: true);
                    if (world.GameState.SpaceObjects.Any(o => o.ObjectId == world.GameState.PlayerShipObjectId && o.IsDestroyed)) continue;
                }
                else
                {
                    using var candidate = Content.EngineContentLoader.CreateEngineFromSaveFile(settingsPath, path);
                    var snapshot = candidate.CaptureSnapshot();
                    if (snapshot.Objects.Any(o => o.ObjectId == snapshot.PlayerShipObjectId && o.IsDestroyed)) continue;
                }
                return slot;
            }
            catch (Exception error) when (error is not (OutOfMemoryException or OperationCanceledException))
            { /* Candidate is never adopted; continue to the previous durable slot. */ }
        }
        return null;
    }

    /// <summary>
    /// List every save slot currently on disk in saveDirectory, most recently saved
    /// first. Returns an empty array if the directory doesn't exist yet (e.g. no save
    /// has ever been made).
    /// </summary>
    public static SaveSlotInfo[] ListSlots(string saveDirectory)
    {
        if (!Directory.Exists(saveDirectory))
            return Array.Empty<SaveSlotInfo>();

        return Directory.EnumerateFiles(saveDirectory, "*.json")
            .Select(path =>
            {
                string slotId = Path.GetFileNameWithoutExtension(path);
                return new SaveSlotInfo(
                    SlotId: slotId,
                    DisplayName: DisplayName(slotId),
                    SavedAtUtc: File.GetLastWriteTimeUtc(path), GameTimeMs: ReadGameTime(path));
            })
            .OrderByDescending(slot => slot.SavedAtUtc)
            .ToArray();
    }

    /// <summary>
    /// Whether at least one save slot file exists in saveDirectory. Answered from the file list alone — no slot file is
    /// opened or parsed — so it stays cheap however large the saves are. False when the directory doesn't exist yet.
    /// File-system errors (IOException, UnauthorizedAccessException) propagate to the caller.
    /// </summary>
    public static bool HasAnySlot(string saveDirectory) =>
        Directory.Exists(saveDirectory) && Directory.EnumerateFiles(saveDirectory, "*.json").Any();

    /// <summary>
    /// Display name for a slot id: ordinary slots show their id, while the reserved automatic
    /// continuation checkpoint gets its fixed, localized-neutral campaign label.
    /// </summary>
    private static string DisplayName(string slotId) =>
        slotId == SaveSlotNaming.ContinuationSlotId ? SaveSlotNaming.ContinuationDisplayName : slotId;

    private static long? ReadGameTime(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("gameState", out var state)
                && state.ValueKind == JsonValueKind.Object
                && state.TryGetProperty("gameTimeMs", out var time)
                && time.ValueKind == JsonValueKind.Number && time.TryGetInt64(out long ms) && ms >= 0 ? ms : null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        { return null; }
    }

    public static void DeleteSlot(string saveDirectory, string slotId)
    {
        string path = Path.Combine(saveDirectory, SaveSlotNaming.ToFileName(slotId));
        if (File.Exists(path))
            File.Delete(path);
    }
}
