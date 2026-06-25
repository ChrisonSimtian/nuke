using System;
using System.Linq;
using Fallout.Common.Utilities;

namespace Fallout.Common.CI.GitHubActions.Configuration;

public class GitHubActionsCheckoutStep : GitHubActionsStep
{
    public GitHubActionsSubmodules? Submodules { get; set; }
    public bool? Lfs { get; set; }
    public uint? FetchDepth { get; set; }
    public bool? Progress { get; set; }
    public string Filter { get; set; }

    /// <summary>
    /// The git ref to check out. When unset, actions/checkout picks the default for the event
    /// (the merge SHA on pull_request triggers, which leaves HEAD detached). Set to
    /// <c>${{ github.head_ref }}</c> on PR workflows that read <c>.git/HEAD</c> directly
    /// (e.g. <see cref="Fallout.Common.Git.GitRepository.FromLocalDirectory"/>) so the branch
    /// resolves correctly. When set, the generator also emits a <c>repository:</c> line that
    /// resolves to the PR head's repo for fork PRs and falls back to the current repo for push
    /// events — without it, fork PRs fail with "branch or tag could not be found" because the
    /// branch only exists on the fork, not on origin.
    /// </summary>
    public string Ref { get; set; }

    public string[] CheckoutWith { get; set; } = new string[0];

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- uses: actions/checkout@v6");

        if (Submodules.HasValue || Lfs.HasValue || FetchDepth.HasValue || Progress.HasValue ||
            !Filter.IsNullOrWhiteSpace() || !Ref.IsNullOrWhiteSpace() || CheckoutWith.Length > 0)
        {
            using (writer.Indent())
            {
                writer.WriteLine("with:");
                using (writer.Indent())
                {
                    if (Submodules.HasValue)
                        writer.WriteLine($"submodules: {Submodules.ToString().ToLowerInvariant()}");
                    if(Lfs.HasValue)
                        writer.WriteLine($"lfs: {Lfs.ToString().ToLowerInvariant()}");
                    if (FetchDepth.HasValue)
                        writer.WriteLine($"fetch-depth: {FetchDepth}");
                    if (Progress.HasValue)
                        writer.WriteLine($"progress: {Progress.ToString().ToLowerInvariant()}");
                    if (!Filter.IsNullOrWhiteSpace())
                        writer.WriteLine($"filter: {Filter}");
                    if (!Ref.IsNullOrWhiteSpace())
                    {
                        // Pin checkout to the source repo of the PR head (or the current repo on
                        // push events). Required when `ref:` is set to `${{ github.head_ref }}` —
                        // for cross-repo PRs, that branch only exists on the fork, not on origin,
                        // and checkout's default `repository: ${{ github.repository }}` would fail
                        // to resolve it. The `||` fallback handles push events where there's no
                        // pull_request context.
                        writer.WriteLine("repository: ${{ github.event.pull_request.head.repo.full_name || github.repository }}");
                        writer.WriteLine($"ref: {Ref}");
                    }

                    foreach (var line in CheckoutWith)
                        writer.WriteLine(line);
                }
            }
        }
    }
}
