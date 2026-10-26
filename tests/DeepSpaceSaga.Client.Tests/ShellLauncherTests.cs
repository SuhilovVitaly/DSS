using System.ComponentModel;
using DeepSpaceSaga.Client.UI;
using Xunit;

namespace DeepSpaceSaga.Client.Tests;

public sealed class ShellLauncherTests
{
    [Theory]
    [InlineData("linux", "xdg-open")]
    [InlineData("macos", "open")]
    public void UnixDirectoryIsOneUnquotedArgument(string platform, string executable)
    {
        string path = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "folder with spaces;literal"));
        var command = ShellLauncher.DirectoryCommand(path, platform);
        Assert.Equal(executable, command.FileName);
        Assert.False(command.UseShellExecute);
        Assert.Equal(new[] { path }, command.ArgumentList);
    }

    [Fact]
    public void WindowsUsesDirectoryShellAssociation()
    {
        string path = Path.GetFullPath(Path.GetTempPath());
        var command = ShellLauncher.DirectoryCommand(path, "windows");
        Assert.True(command.UseShellExecute);
        Assert.Equal(path, command.FileName);
        Assert.Empty(command.ArgumentList);
    }

    [Fact]
    public async Task NoProcessHandleIsAcceptedOnlyForWindowsShell()
    {
        string? result = await ShellLauncher.OpenDirectory(Path.GetTempPath(), _ => null);
        if (OperatingSystem.IsWindows()) Assert.Null(result);
        else Assert.Contains("Folder launcher did not start", result);
    }

    [Fact]
    public async Task MissingLauncherReturnsFailureWithoutThrowing()
    {
        string? result = await ShellLauncher.OpenDirectory(Path.GetTempPath(), _ => throw new Win32Exception("missing xdg-open"));
        Assert.Contains("missing xdg-open", result);
    }
}
