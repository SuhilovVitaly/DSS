using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.LocalClient;

/// <summary>
/// Enumerates and deletes save-slot files on disk. Purely filesystem-level — it
/// never touches the engine or the save JSON schema. Slot metadata (DisplayName,
/// SavedAtUtc) is derived only from the file name and filesystem last-write time,
/// per the story's Protect note that no new fields are added to the save schema.
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
            .Select(path => new SaveSlotInfo(
                SlotId: Path.GetFileNameWithoutExtension(path),
                DisplayName: Path.GetFileNameWithoutExtension(path),
                SavedAtUtc: File.GetLastWriteTimeUtc(path)))
            .OrderByDescending(slot => slot.SavedAtUtc)
            .ToArray();
    }

    /// <summary>Delete a save slot's file, if present. No-op if the slot doesn't exist.</summary>
    public static void DeleteSlot(string saveDirectory, string slotId)
    {
        string path = Path.Combine(saveDirectory, SaveSlotNaming.ToFileName(slotId));
        if (File.Exists(path))
            File.Delete(path);
    }
}
