using System.Globalization;

namespace DeepSpaceSaga.Client.UI;

/// <summary>
/// Thread-safe helper that appends timestamped UI events to Interface.log
/// in the user data directory (or portable installation). Every call flushes to disk.
/// </summary>
public static class InterfaceLog
{
    public const string FileName = "Interface.log";

    /// <summary>Absolute evidence override; normal application logging uses the user data directory.</summary>
    public static string OutputPath => Environment.GetEnvironmentVariable("DSS_INTERFACE_LOG_PATH") is { } path
        && Path.IsPathFullyQualified(path) ? path : Path.Combine(AppPaths.Current.UserData, FileName);

    internal static string? DiagnosticDirectory => Environment.GetEnvironmentVariable("DSS_DIAGNOSTICS_DIRECTORY") is { } path
        && Path.IsPathFullyQualified(path) ? path : null;

    private static readonly object Sync = new();

    /// <summary>
    /// Append a single line: "[yyyy-MM-dd HH:mm:ss] {message}".
    /// </summary>
    public static void Write(string message)
    {
        string line = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}] {message}";
        lock (Sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(OutputPath))!);
            using var stream = new FileStream(
                OutputPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream);
            writer.WriteLine(line);
            writer.Flush();
        }
    }
}
