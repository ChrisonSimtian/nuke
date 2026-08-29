using System.Collections.Generic;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.CI.GitHubActions.Configuration;

public class GitHubActionsRunStep : GitHubActionsStep
{
    /// <summary>
    /// The SDK-setup action to reference — this step emits the setup, the tool restore, and the build run,
    /// so only the first of the three is configurable. Accepts a complete <c>owner/repo@ref</c> or a bare
    /// ref that gets appended to <c>actions/setup-dotnet</c>. Defaults to the version the generator pins;
    /// setting null or whitespace restores it.
    /// </summary>
    public string SetupDotNetAction
    {
        get;
        set => field = GitHubActionsActionReference.Resolve(
            GitHubActionsDefaults.SetupDotNetAction, value, $"{nameof(GitHubActionsRunStep)}.{nameof(SetupDotNetAction)}");
    } = GitHubActionsDefaults.SetupDotNetAction;

    public string[] InvokedTargets { get; set; }

    public Dictionary<string, string> Imports { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- name: 'Setup: .NET SDK'");
        using (writer.Indent())
        {
            writer.WriteLine($"uses: {SetupDotNetAction}");
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
