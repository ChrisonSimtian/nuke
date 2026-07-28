namespace Fallout;

/// <summary>
/// Fallout's own fixed values: directory and file names, parameter names, environment variable
/// keys, package ids and project URLs. Shared between the libraries, the CLI, the source
/// generator and the IDE extensions.
/// </summary>
/// <remarks>
/// Deliberately in the root <c>Fallout</c> namespace rather than a layer's. Core is the innermost
/// ring, so it must not declare types under an outer layer's namespace such as
/// <c>Fallout.Common</c> — <c>ArchitectureFitnessSpecs</c> enforces that. Every call site sits
/// somewhere under <c>Fallout</c>, so unqualified <c>Constants.X</c> resolves without a using.
/// <para>
/// Values only. Anything that derives a path or reads the file system lives in <c>FalloutPaths</c>
/// (<c>Fallout.Build.Shared</c>), which cannot sit here because Core references nothing.
/// </para>
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
    // packs as Fallout.GlobalTool so the existing install base never has to migrate (#582).
    // Still hand-written: <PackageId> in Fallout.Cli.csproj is the source of truth, but the one
    // consumer here (UpdateNotificationAttribute) ships inside Fallout.Common and runs in someone
    // else's build, where no Fallout csproj exists to read it from. Flowing the id in at compile
    // time — one shared MSBuild property feeding both <PackageId> and a generated constant — is
    // the follow-up tracked on #584; until then keep these two in sync by hand.
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
