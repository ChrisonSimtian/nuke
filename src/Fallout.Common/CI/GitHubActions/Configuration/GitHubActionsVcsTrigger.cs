using System.Linq;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.CI.GitHubActions.Configuration;

public class GitHubActionsVcsTrigger : GitHubActionsDetailedTrigger
{
    public GitHubActionsTrigger Kind { get; set; }

    public string[] Branches { get; set; }

    public string[] BranchesIgnore { get; set; }

    public string[] Tags { get; set; }

    public string[] TagsIgnore { get; set; }

    public string[] IncludePaths { get; set; }

    public string[] ExcludePaths { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine($"{Kind.GetValue()}:");

        void WriteValue(string value)
            => writer.WriteLine($"- {value.SingleQuoteIfNeeded('.', '*', '!', '?', '+', '[', ']', '(', ')')}");

        using (writer.Indent())
        {
            if (Branches.Length > 0)
            {
                writer.WriteLine("branches:");
                using (writer.Indent())
                {
                    Branches.ForEach(WriteValue);
                }
            }

            if (BranchesIgnore.Length > 0)
            {
                writer.WriteLine("branches-ignore:");
                using (writer.Indent())
                {
                    BranchesIgnore.ForEach(WriteValue);
                }
            }

            if (Tags.Length > 0)
            {
                writer.WriteLine("tags:");
                using (writer.Indent())
                {
                    Tags.ForEach(WriteValue);
                }
            }

            if (TagsIgnore.Length > 0)
            {
                writer.WriteLine("tags-ignore:");
                using (writer.Indent())
                {
                    TagsIgnore.ForEach(WriteValue);
                }
            }

            // Exclusive-only patterns must use `paths-ignore`; a `paths:` block of only
            // `!`-prefixed entries is a documented GitHub Actions footgun — the workflow
            // never fires because no positive pattern matches.
            if (IncludePaths.Length > 0)
            {
                writer.WriteLine("paths:");
                using (writer.Indent())
                {
                    IncludePaths.ForEach(WriteValue);
                    ExcludePaths.Select(x => $"!{x}").ForEach(WriteValue);
                }
            }
            else if (ExcludePaths.Length > 0)
            {
                writer.WriteLine("paths-ignore:");
                using (writer.Indent())
                {
                    ExcludePaths.ForEach(WriteValue);
                }
            }
        }
    }
}
