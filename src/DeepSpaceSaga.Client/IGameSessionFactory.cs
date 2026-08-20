using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

public interface IGameSessionFactory
{
    IGameSessionConnection CreateSession();

    /// <summary>Bootstrap a new session from the given save slot's file (F9 uses <see cref="SaveSlots.Quicksave"/>). Throws if the file is missing or invalid.</summary>
    IGameSessionConnection CreateSessionFromSave(string slotId);

    /// <summary>Whether a quicksave file currently exists on disk.</summary>
    bool HasQuickSave();

    /// <summary>List every save slot currently on disk, most recently saved first.</summary>
    SaveSlotInfo[] ListSaveSlots();

    /// <summary>Delete a save slot's file, if present. No-op if the slot doesn't exist.</summary>
    void DeleteSaveSlot(string slotId);
}
