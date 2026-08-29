using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using GlobExpressions;

namespace Fallout.Common.IO;

public static class Globbing
{
    public static GlobbingCaseSensitivity GlobbingCaseSensitivity;

    private static GlobOptions GlobOptions
        => GlobbingCaseSensitivity switch
        {
            GlobbingCaseSensitivity.CaseSensitive => GlobOptions.None,
            GlobbingCaseSensitivity.CaseInsensitive => GlobOptions.CaseInsensitive,
            _ => EnvironmentInfo.IsWin ? GlobOptions.CaseInsensitive : GlobOptions.None
        };

    public static IReadOnlyCollection<string> GlobFiles(string directory, params string[] patterns)
    {
        if (patterns.Length == 0)
        {
            (directory, patterns) = GetGlobFromSingleDefinition(directory);
        }

        var directoryInfo = new DirectoryInfo(directory);
        return patterns.SelectMany(x => Glob.Files(directoryInfo, x, GlobOptions)).Select(x => x.FullName).ToList();
    }

    public static IReadOnlyCollection<AbsolutePath> GlobFiles(this AbsolutePath directory, params string[] patterns)
    {
        return GlobFiles((string)directory, patterns).Select(x => (AbsolutePath)x).ToList();
    }

    public static IReadOnlyCollection<string> GlobDirectories(string directory, params string[] patterns)
    {
        if (patterns.Length == 0)
        {
            (directory, patterns) = GetGlobFromSingleDefinition(directory);
        }

        var directoryInfo = new DirectoryInfo(directory);
        return patterns.SelectMany(x => Glob.Directories(directoryInfo, x, GlobOptions)).Select(x => x.FullName).ToList();
    }

    public static IReadOnlyCollection<AbsolutePath> GlobDirectories(this AbsolutePath directory, params string[] patterns)
    {
        return GlobDirectories((string)directory, patterns).Select(x => (AbsolutePath)x).ToList();
    }

    private static (string Directory, string[] Patterns) GetGlobFromSingleDefinition(AbsolutePath definition)
    {
        var directory = definition.DescendantsAndSelf(x => x.Parent)
            .FirstOrDefault(x => !x.ToString().ContainsOrdinalIgnoreCase("*"));

        var pattern = definition.ToString().TrimStart(directory).TrimStart(PathConstruction.AllSeparators);
        return (directory, new[]
        {
            pattern
        });
    }
}
