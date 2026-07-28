namespace Fallout.Common;

/// <summary>
/// Fallout's own fixed values: directory and file names, parameter names, environment variable
/// keys, package ids and project URLs. Shared between the libraries, the CLI, the source
/// generator and the IDE extensions.
/// </summary>
/// <remarks>
/// Values only. Anything that derives a path or reads the file system lives in
/// <see cref="FalloutPaths"/>, which cannot sit here because Core references nothing.
/// </remarks>
internal static class Constants
{
    internal const string FalloutFileName = FalloutDirectoryName;
    internal const string FalloutDirectoryName = ".fallout";

    // Legacy directory name from the pre-Fallout era. Read-only: lets existing
    // consumer projects keep building until they migrate (manually or via the
    // Fallout.Migrate CLI). New setups always use .fallout/.
    internal const string LegacyNukeDirectoryName = ".nuke";

    internal const string FalloutCommonPackageId = "Fallout.Common";

    // The dotnet-tool package id. Deliberately not the project or assembly name — Fallout.Cli
    // packs as Fallout.GlobalTool so the existing install base never has to migrate. Keep in
    // sync with <PackageId> in Fallout.Cli.csproj.
    internal const string FalloutGlobalToolPackageId = "Fallout.GlobalTool";

    internal const string BuildSchemaFileName = "build.schema.json";
    internal const string VisualStudioDebugFileName = $"{VisualStudioDebugParameterName}.log";

    internal const string TargetsSeparator = "+";
    internal const string RootDirectoryParameterName = "Root";
    internal const string InvokedTargetsParameterName = "Target";
    internal const string SkippedTargetsParameterName = "Skip";
    internal const string LoadedLocalProfilesParameterName = "Profile";

    public const string VisualStudioDebugParameterName = "visual-studio-debug";
    internal const string CompletionParameterName = "shell-completion";
    internal const string ParametersFilePrefix = "parameters";
    internal const string DefaultProfileName = "$default";

    internal const string GlobalToolVersionEnvironmentKey = "FALLOUT_GLOBAL_TOOL_VERSION";
    internal const string GlobalToolStartTimeEnvironmentKey = "FALLOUT_GLOBAL_TOOL_START_TIME";
    internal const string InterceptorEnvironmentKey = "FALLOUT_INTERNAL_INTERCEPTOR";

    // Legacy NUKE_* env var names — readers fall back to these via LegacyEnvironment.Read.
    // Writers (e.g. global tool spawning the build) only emit the FALLOUT_* form above.
    internal const string LegacyGlobalToolVersionEnvironmentKey = "NUKE_GLOBAL_TOOL_VERSION";
    internal const string LegacyGlobalToolStartTimeEnvironmentKey = "NUKE_GLOBAL_TOOL_START_TIME";
    internal const string LegacyInterceptorEnvironmentKey = "NUKE_INTERNAL_INTERCEPTOR";

    // Canonical project URLs. Until P7 (domain registration) lands, these all point at the GitHub fork.
    // To migrate to fallout.<tld>, edit FalloutWebsite / FalloutRepository here — call sites already use the constants.
    internal const string FalloutOwner = "Fallout-build";
    internal const string FalloutRepoName = "Fallout";
    internal const string FalloutWebsite = $"https://github.com/{FalloutOwner}/{FalloutRepoName}";
    internal const string FalloutRepository = FalloutWebsite;
    internal const string FalloutRepositoryGit = $"{FalloutWebsite}.git";
    internal const string FalloutRawRepository = $"https://raw.githubusercontent.com/{FalloutOwner}/{FalloutRepoName}/main";
    internal const string FalloutDocsUrl = "https://docs.fallout.build/";
    internal const string FalloutNotificationsUrl = $"{FalloutRawRepository}/notifications.json";

    // Upstream NUKE references — only for attribution / fallback recognition of legacy project URLs.
    internal const string UpstreamNukeRepository = "https://github.com/nuke-build/nuke";
    internal const string UpstreamNukeRepositoryGit = $"{UpstreamNukeRepository}.git";
}
