using System.Runtime.CompilerServices;

namespace DeepSpaceSaga.Client.Tests;

internal static class TestAssemblySetup
{
    [ModuleInitializer]
    internal static void DisablePauseResumeDiagnosticsFileOutput()
    {
        // PauseResumeDiagnostics defaults to on (for the live app) — tests would otherwise
        // spray a PauseResume.log into the test output directory on every run.
        Environment.SetEnvironmentVariable("DSS_TRACE_PAUSE_RESUME", "0");
    }
}
