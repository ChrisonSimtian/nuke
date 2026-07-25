using System;
using System.Linq;
using Fallout.Common.Utilities;

namespace Fallout.Common.CI.GitHubActions.Configuration;

public class GitHubActionsArtifactStep : GitHubActionsStep
{
    /// <summary>
    /// The upload-artifact action to reference. Accepts a complete <c>owner/repo@ref</c> or a bare ref that
    /// gets appended to <c>actions/upload-artifact</c>. Defaults to the version the generator pins; setting
    /// null or whitespace restores it.
    /// </summary>
    public string Uses
    {
        get;
        set => field = GitHubActionsActionReference.Resolve(
            GitHubActionsDefaults.UploadArtifactAction, value, $"{nameof(GitHubActionsArtifactStep)}.{nameof(Uses)}");
    } = GitHubActionsDefaults.UploadArtifactAction;

    public string Name { get; set; }
    public string Path { get; set; }
    public string Condition { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- name: " + $"Publish: {Name}".SingleQuoteYaml());
        writer.WriteLine($"  uses: {Uses}");

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
