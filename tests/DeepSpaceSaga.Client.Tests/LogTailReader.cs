using DeepSpaceSaga.Client.UI;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Shared helper for reading the tail of <see cref="InterfaceLog"/> added after a given position.
/// Isolates test assertions from parallel test-class writes to the shared log file.
/// </summary>
internal static class LogTailReader
{
    /// <summary>
    /// Returns the content appended to <see cref="InterfaceLog.FileName"/> after <paramref name="lengthBefore"/>.
    /// Returns <see cref="string.Empty"/> if the file does not exist or nothing was appended.
    /// </summary>
    internal static string ReadTail(long lengthBefore)
    {
        string logPath = Path.Combine(Environment.CurrentDirectory, InterfaceLog.FileName);
        long lengthAfter = File.Exists(logPath) ? new FileInfo(logPath).Length : 0;
        if (lengthAfter <= lengthBefore)
            return string.Empty;

        using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(lengthBefore, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
