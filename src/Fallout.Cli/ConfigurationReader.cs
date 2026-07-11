using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fallout.Common.IO;
using Fallout.Common.Utilities;

namespace Fallout.Cli;

/// <summary>Parses the <c># CONFIGURATION … # EXECUTION</c> block out of a build script.</summary>
internal interface IConfigurationReader
{
    Dictionary<string, string> Read(AbsolutePath buildScript, bool evaluate);
}

/// <inheritdoc />
internal sealed class ConfigurationReader : IConfigurationReader
{
    /// <summary>Configuration key holding the build project file path.</summary>
    public const string BuildProjectFileKey = "BUILD_PROJECT_FILE";

    public Dictionary<string, string> Read(AbsolutePath buildScript, bool evaluate)
    {
        string ReplaceScriptDirectory(string value)
            => evaluate
                ? value
                    .Replace("$SCRIPT_DIR", buildScript.Parent)
                    .Replace("$PSScriptRoot", buildScript.Parent)
                : value;

        return File.ReadAllLines(buildScript)
            .SkipWhile(x => !x.StartsWithOrdinalIgnoreCase("# CONFIGURATION"))
            .TakeWhile(x => !x.StartsWithOrdinalIgnoreCase("# EXECUTION"))
            .Where(x => !x.IsNullOrEmpty() && !x.StartsWithAny("#", "export ", "$env:"))
            .Select(ReplaceScriptDirectory)
            .Select(x => x.Split("="))
            .ToDictionary(
                x => x.ElementAt(0).TrimStart("$").Trim().SplitCamelHumpsWithKnownWords().JoinUnderscore().ToUpperInvariant(),
                x => x.ElementAt(1).Trim().TrimMatchingDoubleQuotes());
    }
}
