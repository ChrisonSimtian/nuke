using System.Linq;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.CI.GitHubActions.Configuration;

// https://github.com/actions/cache
public class GitHubActionsCacheStep : GitHubActionsStep
{
    /// <summary>
    /// The cache action to reference. Accepts a complete <c>owner/repo@ref</c> or a bare ref that gets
    /// appended to <c>actions/cache</c>. Defaults to the version the generator pins; setting null or
    /// whitespace restores it.
    /// </summary>
    public string Uses
    {
        get;
        set => field = GitHubActionsActionReference.Resolve(
            GitHubActionsDefaults.CacheAction, value, $"{nameof(GitHubActionsCacheStep)}.{nameof(Uses)}");
    } = GitHubActionsDefaults.CacheAction;

    public string[] IncludePatterns { get; set; }

    public string[] ExcludePatterns { get; set; }

    public string[] KeyFiles { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- name: " + $"Cache: {IncludePatterns.JoinCommaSpace()}".SingleQuoteYaml());
        using (writer.Indent())
        {
            writer.WriteLine($"uses: {Uses}");
            writer.WriteLine("with:");
            using (writer.Indent())
            {
                writer.WriteLine("path: |");
                IncludePatterns.ForEach(x => writer.WriteLine($"  {x}"));
                ExcludePatterns.ForEach(x => writer.WriteLine($"  !{x}"));
                writer.WriteLine(
                    $"key: ${{{{ runner.os }}}}-${{{{ hashFiles({KeyFiles.Select(x => x.SingleQuoteYaml()).JoinCommaSpace()}) }}}}");
            }
        }
    }
}
