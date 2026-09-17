using System.Diagnostics;
using System.Globalization;

namespace DeepSpaceSaga.Client.UI;

/// <summary>
/// Millisecond-precision trace of the pause/resume event sequence (input,
/// incoming snapshots, per-frame render state) to a dedicated log file under
/// Logs/. Opt in with DSS_TRACE_PAUSE_RESUME=1 when investigating timing issues.
/// Normal rendering performs no diagnostic file I/O.
/// The log file is truncated at the start of every run.
/// </summary>
public static class PauseResumeDiagnostics
{
    private const string LogDirectory = "Logs";
    private const string FileName = "PauseResume.log";

    public static readonly bool Enabled =
        Environment.GetEnvironmentVariable("DSS_TRACE_PAUSE_RESUME") == "1";

    public static readonly string FilePath = Path.Combine(LogDirectory, FileName);

    private static readonly Stopwatch Clock = Stopwatch.StartNew();
    private static readonly object Sync = new();

    static PauseResumeDiagnostics()
    {
        if (!Enabled)
            return;

        Directory.CreateDirectory(LogDirectory);

        // Fresh log every run — a stale log from a previous session is worse than none.
        try
        {
            File.Delete(FilePath);
        }
        catch (IOException)
        {
            // Locked by another instance/viewer — next Write() will just append to it.
        }
    }

    public static void Write(string message)
    {
        if (!Enabled)
            return;

        double ms = Clock.Elapsed.TotalMilliseconds;
        string line = $"[{ms.ToString("F2", CultureInfo.InvariantCulture),10}ms] {message}";

        lock (Sync)
        {
            using var stream = new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream);
            writer.WriteLine(line);
        }
    }
}
