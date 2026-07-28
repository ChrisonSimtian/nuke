using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common;

/// <summary>
/// Paths and names derived from a repository root or a parameters profile.
/// </summary>
/// <remarks>
/// Split from <see cref="Constants"/>, which holds the fixed values and now lives in
/// <c>Fallout.Core</c>. These helpers cannot follow it: they need <see cref="AbsolutePath"/>,
/// <see cref="EnvironmentInfo"/> and the file system, and Core references nothing.
/// </remarks>
internal static class FalloutPaths
{
    internal static AbsolutePath GlobalTemporaryDirectory => Path.GetTempPath();
    internal static AbsolutePath GlobalFalloutDirectory => EnvironmentInfo.SpecialFolder(SpecialFolders.UserProfile) / Constants.FalloutDirectoryName;

    internal static AbsolutePath TryGetRootDirectoryFrom(AbsolutePath startDirectory, bool includeLegacy = true)
    {
        var rootDirectory = new DirectoryInfo(startDirectory)
            .DescendantsAndSelf(x => x.Parent)
            .FirstOrDefault(x => x.GetDirectories(Constants.FalloutDirectoryName).Any() ||
                                 x.GetDirectories(Constants.LegacyNukeDirectoryName).Any() ||
                                 includeLegacy && x.GetFiles(Constants.FalloutFileName).Any())
            ?.FullName;
        return rootDirectory != GlobalFalloutDirectory.Parent ? (AbsolutePath) rootDirectory : null;
    }

    internal static bool IsLegacy(AbsolutePath rootDirectory)
    {
        return File.Exists(rootDirectory / Constants.FalloutFileName);
    }

    internal static AbsolutePath GetFalloutDirectory(AbsolutePath rootDirectory)
    {
        var newDir = rootDirectory / Constants.FalloutDirectoryName;
        if (Directory.Exists(newDir))
            return newDir;
        var legacyDir = rootDirectory / Constants.LegacyNukeDirectoryName;
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
        var completionFileName = Constants.CompletionParameterName + ".yml";
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
        return GetTemporaryDirectory(rootDirectory) / $"{Constants.VisualStudioDebugParameterName}.log";
    }

    public static AbsolutePath GetReSharperSurrogateFile(AbsolutePath rootDirectory)
    {
        return GetTemporaryDirectory(rootDirectory) / "resharper-surrogate.log";
    }

    internal static AbsolutePath GetBuildSchemaFile(AbsolutePath rootDirectory)
    {
        return GetFalloutDirectory(rootDirectory) / Constants.BuildSchemaFileName;
    }

    internal static AbsolutePath GetDefaultParametersFile(AbsolutePath rootDirectory)
    {
        return GetFalloutDirectory(rootDirectory) / GetParametersFileName(Constants.DefaultProfileName);
    }

    internal static IEnumerable<AbsolutePath> GetParametersProfileFiles(AbsolutePath rootDirectory)
    {
        return new DirectoryInfo(GetFalloutDirectory(rootDirectory)).GetFiles($"{Constants.ParametersFilePrefix}.*.json", SearchOption.TopDirectoryOnly)
            .Select(x => (AbsolutePath)x.FullName);
    }

    internal static AbsolutePath GetParametersProfileFile(AbsolutePath rootDirectory, string profile)
    {
        return GetFalloutDirectory(rootDirectory) / GetParametersFileName(profile);
    }

    internal static string GetParametersFileName(string profile)
    {
        return profile == Constants.DefaultProfileName ? $"{Constants.ParametersFilePrefix}.json" : $"{Constants.ParametersFilePrefix}.{profile}.json";
    }

    public static IEnumerable<string> GetProfileNames(AbsolutePath rootDirectory)
    {
        return GetParametersProfileFiles(rootDirectory)
            .Select(x => x.ToString())
            .Select(Path.GetFileNameWithoutExtension)
            .Select(x => x.TrimStart(Constants.ParametersFilePrefix).TrimStart("."));
    }

    internal static string GetCredentialStoreName(AbsolutePath rootDirectory, string profile)
    {
        return $"Fallout: {rootDirectory} ({profile ?? Constants.DefaultProfileName})";
    }

    // Pre-rename name. Readers fall back to this when the canonical entry is missing.
    // Writers (SavePassword / Secrets command) only emit the canonical form above.
    internal static string GetLegacyCredentialStoreName(AbsolutePath rootDirectory, string profile)
    {
        return $"NUKE: {rootDirectory} ({profile ?? Constants.DefaultProfileName})";
    }

    internal static string GetProfilePasswordParameterName(string profile)
    {
        return $"PARAMS_{profile.TrimStart(Constants.DefaultProfileName).ToUpperInvariant().Replace(".", "_")}_KEY".Replace("_", string.Empty);
    }
}
