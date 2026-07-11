using Fallout.Common;
using Fallout.Common.Utilities;

namespace Fallout.Cli;

/// <summary>Small CLI-wide conventions shared by the entry point and commands.</summary>
internal static class CliConventions
{
    /// <summary>The build-script file name for the current platform.</summary>
    public static string CurrentBuildScriptName => EnvironmentInfo.IsWin ? "build.ps1" : "build.sh";
}

/// <summary>Prints the global-tool banner.</summary>
internal static class ToolBanner
{
    public static void Print()
        => Host.Information($"Fallout Global Tool 🌐 {typeof(ToolBanner).Assembly.GetInformationalText()}");
}
