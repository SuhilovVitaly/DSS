namespace DeepSpaceSaga.Engine.LocalClient;

/// <summary>
/// Shared slot-id sanitization for save files. A slot id becomes a file name
/// ("&lt;sanitized-slot-id&gt;.json") inside the save directory, so it must be
/// constrained to a safe, portable character set — never pass a raw, possibly
/// user-typed slot id straight through to Path.Combine.
/// Used by both LocalGameSessionConnection.SaveAsync (write) and
/// SaveSlotRepository (list/delete) so the mapping is identical on every path.
/// </summary>
public static class SaveSlotNaming
{
    private const int MaxSanitizedLength = 64;

    /// <summary>
    /// Reduce an arbitrary slot id to a safe file-name stem: keeps letters, digits,
    /// spaces, '-' and '_'; drops everything else; trims and caps length. If nothing
    /// survives (e.g. an empty or fully-illegal input), falls back to a UTC-timestamp
    /// stem so the caller always gets a usable name instead of an empty/invalid one.
    /// </summary>
    public static string Sanitize(string slotId)
    {
        if (string.IsNullOrEmpty(slotId))
            return TimestampFallback();

        var chars = slotId.Where(IsAllowed).ToArray();
        string sanitized = new string(chars).Trim();

        if (sanitized.Length > MaxSanitizedLength)
            sanitized = sanitized[..MaxSanitizedLength].TrimEnd();

        return sanitized.Length == 0 ? TimestampFallback() : sanitized;
    }

    /// <summary>The file name (with the ".json" extension) a given slot id maps to.</summary>
    public static string ToFileName(string slotId) => Sanitize(slotId) + ".json";

    private static bool IsAllowed(char c) =>
        char.IsLetterOrDigit(c) || c is ' ' or '-' or '_';

    private static string TimestampFallback() =>
        $"save-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}";
}
