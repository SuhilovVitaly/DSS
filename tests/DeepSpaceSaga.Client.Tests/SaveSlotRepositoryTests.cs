using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Filesystem-level tests for SaveSlotNaming (Engine.LocalClient) and
/// SaveSlotRepository (Engine.LocalClient) — the helpers Program.cs's
/// IGameSessionFactory implementation wires ListSaveSlots()/DeleteSaveSlot(string)
/// through to.
/// </summary>
public class SaveSlotRepositoryTests
{
    [Fact]
    public void Sanitize_keeps_alnum_space_dash_underscore_and_strips_the_rest()
    {
        Assert.Equal("My Save 1", SaveSlotNaming.Sanitize("My Save #1!"));
        Assert.Equal("save-name_2", SaveSlotNaming.Sanitize("save-name_2"));
    }

    [Fact]
    public void Sanitize_falls_back_to_a_timestamp_when_nothing_survives()
    {
        string result = SaveSlotNaming.Sanitize("###???");

        Assert.StartsWith("save-", result);
        Assert.NotEmpty(SaveSlotNaming.Sanitize(""));
    }

    [Fact]
    public void Sanitize_caps_length()
    {
        string longId = new string('a', 200);

        string result = SaveSlotNaming.Sanitize(longId);

        Assert.True(result.Length <= 64);
    }

    [Fact]
    public void ToFileName_appends_json_extension_to_the_sanitized_id()
    {
        Assert.Equal("quicksave.json", SaveSlotNaming.ToFileName("quicksave"));
        Assert.Equal("My Save 1.json", SaveSlotNaming.ToFileName("My Save #1!"));
    }

    [Fact]
    public void ListSlots_returns_empty_array_when_the_directory_does_not_exist()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"dss-slots-missing-{Guid.NewGuid():N}");

        SaveSlotInfo[] slots = SaveSlotRepository.ListSlots(dir);

        Assert.Empty(slots);
    }

    [Fact]
    public void ListSlots_derives_SlotId_and_DisplayName_from_the_file_name_and_SavedAtUtc_from_last_write_time()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"dss-slots-list-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "alpha.json"), "{}");
            File.WriteAllText(Path.Combine(dir, "beta.json"), "{}");

            SaveSlotInfo[] slots = SaveSlotRepository.ListSlots(dir);

            Assert.Equal(2, slots.Length);
            Assert.Contains(slots, s => s.SlotId == "alpha" && s.DisplayName == "alpha");
            Assert.Contains(slots, s => s.SlotId == "beta" && s.DisplayName == "beta");
            Assert.All(slots, s => Assert.True(s.SavedAtUtc > DateTime.MinValue));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void DeleteSlot_removes_the_slots_file()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"dss-slots-delete-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            string path = Path.Combine(dir, "gamma.json");
            File.WriteAllText(path, "{}");

            SaveSlotRepository.DeleteSlot(dir, "gamma");

            Assert.False(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void DeleteSlot_is_a_no_op_when_the_slot_does_not_exist()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"dss-slots-delete-missing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var exception = Record.Exception(() => SaveSlotRepository.DeleteSlot(dir, "does-not-exist"));

            Assert.Null(exception);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
