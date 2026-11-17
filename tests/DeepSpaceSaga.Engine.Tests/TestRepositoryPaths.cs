using System.Runtime.CompilerServices;

namespace DeepSpaceSaga.Engine.Tests;

internal static class TestRepositoryPaths
{
    internal static string Root => FindRoot();
    internal static string Client => Path.Combine(Root, "src", "DeepSpaceSaga.Client");
    private static string FindRoot([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", ".."));
}
