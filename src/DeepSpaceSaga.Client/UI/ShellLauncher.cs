using System.Diagnostics;

namespace DeepSpaceSaga.Client.UI;

internal static class ShellLauncher
{
    internal static ProcessStartInfo DirectoryCommand(string path, string platform)
    {
        path = Path.GetFullPath(path);
        if (platform == "windows") return new(path) { UseShellExecute = true };
        var command = new ProcessStartInfo(platform == "macos" ? "open" : "xdg-open")
        { UseShellExecute = false, RedirectStandardError = true, CreateNoWindow = true };
        command.ArgumentList.Add(path);
        return command;
    }

    internal static async Task<string?> OpenDirectory(string path, Func<ProcessStartInfo, Process?>? start = null)
    {
        try
        {
            Directory.CreateDirectory(path);
            string platform = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "macos" : "linux";
            using var process = (start ?? Process.Start)(DirectoryCommand(path, platform));
            // Explorer may handle the shell request without starting a new process.
            if (platform == "windows") return null;
            if (process is null) throw new InvalidOperationException("Folder launcher did not start");
            Task<string> errors = process.StandardError.ReadToEndAsync();
            try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false); }
            catch (TimeoutException) { process.Kill(); throw; }
            string detail = await errors.ConfigureAwait(false);
            if (process.ExitCode != 0) throw new IOException($"Folder launcher exit {process.ExitCode}: {detail.Trim()}");
            return null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or InvalidOperationException or TimeoutException or ArgumentException or NotSupportedException)
        {
            try { InterfaceLog.Write($"Could not open folder '{path}': {error.Message}"); }
            catch (Exception logError) when (logError is IOException or UnauthorizedAccessException) { }
            return error.Message;
        }
    }
}
