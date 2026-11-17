namespace DeepSpaceSaga.Client.Tests.Demo;

// EP-0034-US-0006-TK-0003: content-backed demo tests read this checkout's client project
// instead of the fixed sibling path D:/DeepSpaceSaga/DSS, which may hold another branch
// during parallel epics.
internal static class DemoCheckoutPaths
{
    internal static readonly string ClientRoot = Resolve();

    private static string Resolve([System.Runtime.CompilerServices.CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", "..", "src", "DeepSpaceSaga.Client"))
            .Replace('\\', '/');
}
