using System.Collections.Generic;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.CI.GitHubActions.Configuration;

public class GitHubActionsRunStep : GitHubActionsStep
{
    public string[] InvokedTargets { get; set; }
    public Dictionary<string, string> Imports { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- name: 'Setup: .NET SDK'");
        using (writer.Indent())
        {
            writer.WriteLine("uses: actions/setup-dotnet@v5");
            writer.WriteLine("with:");
            using (writer.Indent())
            {
                writer.WriteLine("global-json-file: global.json");
            }
        }

        writer.WriteLine("- name: 'Restore: dotnet tools'");
        using (writer.Indent())
        {
            writer.WriteLine("run: dotnet tool restore");
        }

        writer.WriteLine("- name: " + $"Run: {InvokedTargets.JoinCommaSpace()}".SingleQuoteYaml());
        using (writer.Indent())
        {
            writer.WriteLine($"run: dotnet fallout {InvokedTargets.JoinSpace()}");

            if (Imports.Count > 0)
            {
                writer.WriteLine("env:");
                using (writer.Indent())
                {
                    Imports.ForEach(x => writer.WriteLine($"{x.Key}: {x.Value}"));
                }
            }
        }
    }
}
