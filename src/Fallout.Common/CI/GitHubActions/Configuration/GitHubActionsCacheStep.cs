using System;
using System.Linq;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.CI.GitHubActions.Configuration;

// https://github.com/actions/cache
public class GitHubActionsCacheStep : GitHubActionsStep
{
    public string[] IncludePatterns { get; set; }
    public string[] ExcludePatterns { get; set; }
    public string[] KeyFiles { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- name: " + $"Cache: {IncludePatterns.JoinCommaSpace()}".SingleQuoteYaml());
        using (writer.Indent())
        {
            writer.WriteLine("uses: actions/cache@v6");
            writer.WriteLine("with:");
            using (writer.Indent())
            {
                writer.WriteLine("path: |");
                IncludePatterns.ForEach(x => writer.WriteLine($"  {x}"));
                ExcludePatterns.ForEach(x => writer.WriteLine($"  !{x}"));
                writer.WriteLine($"key: ${{{{ runner.os }}}}-${{{{ hashFiles({KeyFiles.Select(x => x.SingleQuoteYaml()).JoinCommaSpace()}) }}}}");
            }
        }
    }
}
