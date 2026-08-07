using System.Globalization;

namespace DeepSpaceSaga.Client.UI;

/// <summary>
/// Thread-safe helper that appends timestamped UI events to Interface.log
/// in the application working directory. Every call flushes to disk.
/// </summary>
public static class InterfaceLog
{
    public const string FileName = "Interface.log";

    private static readonly object Sync = new();

    /// <summary>
    /// Append a single line: "[yyyy-MM-dd HH:mm:ss] {message}".
    /// </summary>
    public static void Write(string message)
    {
        string line = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}] {message}";
        lock (Sync)
        {
            using var stream = new FileStream(
                FileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream);
            writer.WriteLine(line);
            writer.Flush();
        }
    }
}
