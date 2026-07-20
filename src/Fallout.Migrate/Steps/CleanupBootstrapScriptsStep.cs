using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fallout.Common.IO;
using Fallout.Migrate.Common;

namespace Fallout.Migrate.Steps;

/// <summary>
/// This migration step cleans up bootstrap scripts by removing unnecessary code.
/// The unnecessary code includes checks for a specific environment variable and related logic.
/// </summary>
internal partial class CleanupBootstrapScriptsStep : IMigrationStep
{
    public Task ExecuteAsync(MigrationContext context, Summary summary)
    {
        foreach (var file in new[]
                 {
                     "build.sh",
                     "build.ps1"
                 })
        {
            var path = context.RootDirectory / file;
            if (path.FileExists())
            {
                MigrationFileOperations.ApplyRewrite(context, path, Cleanup, summary);
            }
        }

        return Task.CompletedTask;
    }

    private static RewriteResult Cleanup(string content)
    {
        var envVarToCheck = "NUKE_ENTERPRISE_TOKEN";
        if (!content.Contains(envVarToCheck))
        {
            return new(content, 0);
        }

        // This is just to preserve the current line ending the user has for this files
        string newline =
            GetLineFeedRegex().Match(content).Value is { Length: > 0 } value
                ? value
                : Environment.NewLine;

        var lines = GetLineFeedRegex().Split(content).ToList();

        // here, we get the index of the line that contains the environment variable check
        var indexOfEnterpriseEnvVarCheck = lines.FindIndex(line => line.Contains(envVarToCheck));
        
        // here, we get the index of the line that ends the if block
        // which is fi on bash and a simple "}" in powershell
        var endOfIfBlock = lines.FindIndex(indexOfEnterpriseEnvVarCheck,
            line => line.Trim() == "}" || line.Trim() == "fi");

        // this is "just" to remove the empty line after the if block
        if (lines.Count > endOfIfBlock + 1 && lines[endOfIfBlock + 1].Trim() == "")
        {
            endOfIfBlock++;
        }

        lines.RemoveRange(indexOfEnterpriseEnvVarCheck, endOfIfBlock - indexOfEnterpriseEnvVarCheck + 1);

        return new(string.Join(newline, lines), 1);
    }

    [GeneratedRegex(@"\r\n|\n|\r")]
    private static partial Regex GetLineFeedRegex();
}
