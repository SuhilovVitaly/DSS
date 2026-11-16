using System.Runtime.CompilerServices;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

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

    /// <summary>Temporary battle-metrics directory of this test run, removed when the test host exits.</summary>
    internal static string BattleMetricsDirectory { get; } =
        Path.Combine(Path.GetTempPath(), "DeepSpaceSaga.Client.Tests", "BattleMetrics-" + Guid.NewGuid().ToString("N"));

    [ModuleInitializer]
    internal static void RedirectBattleMetricsOutOfTheUserProfile()
    {
        // US-0022-TK-0013: every accepted battle response exports a report; without DSS_VALIDATION_ROOT (an explicit
        // validation artifact root) the reports would land in %LOCALAPPDATA%/DeepSpaceSaga/BattleMetrics.
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } root && Path.IsPathFullyQualified(root))
            return;
        TacticalBattleMetrics.DirectoryOverride = BattleMetricsDirectory;
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { Directory.Delete(BattleMetricsDirectory, recursive: true); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        };
    }
}
