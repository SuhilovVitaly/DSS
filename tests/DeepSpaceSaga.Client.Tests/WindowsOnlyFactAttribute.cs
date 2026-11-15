namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// A <see cref="FactAttribute"/> that runs only on Windows; on other platforms the test is reported as skipped
/// (visible in the test report) instead of returning early and passing silently.
/// </summary>
public sealed class WindowsOnlyFactAttribute : FactAttribute
{
    public WindowsOnlyFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
            Skip = "Windows-only: relies on Windows file-sharing semantics.";
    }
}
