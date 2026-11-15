using DeepSpaceSaga.Contracts;
using System.Text.Json;

namespace DeepSpaceSaga.Engine.LocalClient;

/// <summary>
/// Lists and deletes save slots. Reads only gameTimeMs for display, without loading
/// or migrating the world. Corrupt metadata gets a safe placeholder.
/// </summary>
public static class SaveSlotRepository
{
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
