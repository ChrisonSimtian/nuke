using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common;

/// <summary>
/// Set of constants shared between libraries and IDE extensions.
/// </summary>
internal static class Constants
{
    internal const string FalloutFileName = FalloutDirectoryName;
    internal const string FalloutDirectoryName = ".fallout";
    // Legacy directory name from the pre-Fallout era. Read-only: lets existing
    // consumer projects keep building until they migrate (manually or via the
    // forthcoming Fallout.Migrate CLI). New setups always use .fallout/.
    internal const string LegacyNukeDirectoryName = ".nuke";
    internal const string FalloutCommonPackageId = "Fallout.Common";
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
    internal const string FalloutTelemetryDocsUrl = $"{FalloutWebsite}#telemetry";
    internal const string FalloutNotificationsUrl = $"{FalloutRawRepository}/notifications.json";

    // Upstream NUKE references — only for attribution / fallback recognition of legacy project URLs.
    internal const string UpstreamNukeRepository = "https://github.com/nuke-build/nuke";
    internal const string UpstreamNukeRepositoryGit = $"{UpstreamNukeRepository}.git";

    internal static AbsolutePath GlobalTemporaryDirectory => Path.GetTempPath();
    internal static AbsolutePath GlobalFalloutDirectory =>  EnvironmentInfo.SpecialFolder(SpecialFolders.UserProfile) / ".fallout";

    internal static AbsolutePath TryGetRootDirectoryFrom(AbsolutePath startDirectory, bool includeLegacy = true)
    {
        var rootDirectory = new DirectoryInfo(startDirectory)
            .DescendantsAndSelf(x => x.Parent)
            .FirstOrDefault(x => x.GetDirectories(FalloutDirectoryName).Any() ||
                                 x.GetDirectories(LegacyNukeDirectoryName).Any() ||
                                 includeLegacy && x.GetFiles(FalloutFileName).Any())
            ?.FullName;
        return rootDirectory != GlobalFalloutDirectory.Parent ? (AbsolutePath) rootDirectory : null;
    }

    internal static bool IsLegacy(AbsolutePath rootDirectory)
    {
        return File.Exists(rootDirectory / FalloutFileName);
    }

    internal static AbsolutePath GetFalloutDirectory(AbsolutePath rootDirectory)
    {
        var newDir = rootDirectory / FalloutDirectoryName;
        if (Directory.Exists(newDir))
            return newDir;
        var legacyDir = rootDirectory / LegacyNukeDirectoryName;
        return Directory.Exists(legacyDir) ? legacyDir : newDir;
    }

    internal static AbsolutePath GetTemporaryDirectory(AbsolutePath rootDirectory)
    {
        return !IsLegacy(rootDirectory)
            ? GetFalloutDirectory(rootDirectory) / "temp"
            : rootDirectory / ".tmp";
    }

    internal static AbsolutePath GetCompletionFile(AbsolutePath rootDirectory)
    {
        var completionFileName = CompletionParameterName + ".yml";
        return File.Exists(rootDirectory / completionFileName)
            ? rootDirectory / completionFileName
            : GetTemporaryDirectory(rootDirectory) / completionFileName;
    }

    internal static AbsolutePath GetBuildAttemptFile(AbsolutePath rootDirectory)
    {
        return GetTemporaryDirectory(rootDirectory) / "build-attempt.log";
    }

    public static AbsolutePath GetVisualStudioDebugFile(AbsolutePath rootDirectory)
    {
        return GetTemporaryDirectory(rootDirectory) / $"{VisualStudioDebugParameterName}.log";
    }

    public static AbsolutePath GetReSharperSurrogateFile(AbsolutePath rootDirectory)
    {
        return GetTemporaryDirectory(rootDirectory) / "resharper-surrogate.log";
    }

    internal static AbsolutePath GetBuildSchemaFile(AbsolutePath rootDirectory)
    {
        return GetFalloutDirectory(rootDirectory) / BuildSchemaFileName;
    }

    internal static AbsolutePath GetDefaultParametersFile(AbsolutePath rootDirectory)
    {
        return GetFalloutDirectory(rootDirectory) / GetParametersFileName(DefaultProfileName);
    }

    internal static IEnumerable<AbsolutePath> GetParametersProfileFiles(AbsolutePath rootDirectory)
    {
        return new DirectoryInfo(GetFalloutDirectory(rootDirectory)).GetFiles($"{ParametersFilePrefix}.*.json", SearchOption.TopDirectoryOnly)
            .Select(x => (AbsolutePath)x.FullName);
    }

    internal static AbsolutePath GetParametersProfileFile(AbsolutePath rootDirectory, string profile)
    {
        return GetFalloutDirectory(rootDirectory) / GetParametersFileName(profile);
    }

    internal static string GetParametersFileName(string profile)
    {
        return profile == DefaultProfileName ? $"{ParametersFilePrefix}.json" : $"{ParametersFilePrefix}.{profile}.json";
    }

    public static IEnumerable<string> GetProfileNames(AbsolutePath rootDirectory)
    {
        return GetParametersProfileFiles(rootDirectory)
            .Select(x => x.ToString())
            .Select(Path.GetFileNameWithoutExtension)
            .Select(x => x.TrimStart(ParametersFilePrefix).TrimStart("."));
    }

    internal static string GetCredentialStoreName(AbsolutePath rootDirectory, string profile)
    {
        return $"Fallout: {rootDirectory} ({profile ?? DefaultProfileName})";
    }

    // Pre-rename name. Readers fall back to this when the canonical entry is missing.
    // Writers (SavePassword / Secrets command) only emit the canonical form above.
    internal static string GetLegacyCredentialStoreName(AbsolutePath rootDirectory, string profile)
    {
        return $"NUKE: {rootDirectory} ({profile ?? DefaultProfileName})";
    }

    internal static string GetProfilePasswordParameterName(string profile)
    {
        return $"PARAMS_{profile.TrimStart(DefaultProfileName).ToUpperInvariant().Replace(".", "_")}_KEY".Replace("_", string.Empty);
    }
}
