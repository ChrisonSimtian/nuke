using System;
using System.Linq;
using Fallout.Common.Utilities;

namespace Fallout.Common.CI.GitHubActions.Configuration;

public class GitHubActionsArtifactStep : GitHubActionsStep
{
    public string Name { get; set; }
    public string Path { get; set; }
    public string Condition { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- name: " + $"Publish: {Name}".SingleQuoteYaml());
        writer.WriteLine("  uses: actions/upload-artifact@v7");

        using (writer.Indent())
        {
            if (!Condition.IsNullOrWhiteSpace())
            {
                writer.WriteLine($"if: {Condition}");
            }

            writer.WriteLine("with:");
            using (writer.Indent())
            {
                writer.WriteLine($"name: {Name}");
                writer.WriteLine($"path: {Path}");
            }
        }
    }
}
