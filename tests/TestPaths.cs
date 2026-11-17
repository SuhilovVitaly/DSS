using System.Runtime.CompilerServices;

// Source locations are independent of --artifacts-path and test process cwd.
internal static class TestPaths
{
    public static string Repository { get; } = FindRepository();
    public static string ClientOutput => Environment.GetEnvironmentVariable("DSS_CLIENT_OUTPUT")
        ?? Path.Combine(Repository, "src", "DeepSpaceSaga.Client", "bin",
            typeof(TestPaths).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyConfigurationAttribute), false)
                .Cast<System.Reflection.AssemblyConfigurationAttribute>().Single().Configuration, "net8.0");

    private static string FindRepository([CallerFilePath] string source = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(source)!);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DeepSpaceSaga.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("DSS source repository not found.");
    }

    public static string Project([CallerFilePath] string source = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(source)!);
        while (directory is not null && !directory.EnumerateFiles("*.csproj").Any())
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("DSS test project not found.");
    }
}
