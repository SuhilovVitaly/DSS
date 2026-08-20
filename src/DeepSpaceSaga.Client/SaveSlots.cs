using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

/// <summary>
/// Reserved save-slot ids with special meaning to the client, as opposed to
/// free-named slots the player creates via the save window.
/// </summary>
public static class SaveSlots
{
    /// <summary>The slot F5 (save) / F9 (load) always target.</summary>
    public const string Quicksave = "quicksave";

    /// <summary>
    /// Filters the reserved quicksave slot out of a slot list. Used by the Save window
    /// (<c>SkiaWindow.OpenSaveWindowAsync</c>) so the player can never see/overwrite/delete
    /// it there and silently break F9 quickload.
    /// </summary>
    public static IReadOnlyList<SaveSlotInfo> ExcludeReserved(IReadOnlyList<SaveSlotInfo> slots) =>
        slots.Where(s => s.SlotId != Quicksave).ToArray();
}
