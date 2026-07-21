using System;
using System.Linq;
using FluentAssertions;
using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Common.Tools.GitHub;
using Xunit;

namespace Fallout.Common.Specs;

public class GitHubTasksSpecs
{
    private static AbsolutePath RootDirectory => Constants.TryGetRootDirectoryFrom(EnvironmentInfo.WorkingDirectory).NotNull();

    [Fact]
    public void GitHubRepositoryFromLocalDirectorySpec()
    {
        var repository = GitRepository.FromLocalDirectory(RootDirectory).NotNull();
        if (!repository.IsGitHubRepository())
            return;

        // The URL helpers below are branch-based (GetGitHubDownloadUrl asserts Branch != null).
        // A tag-triggered release build checks out a detached HEAD, which has no branch, so
        // there is nothing meaningful to assert here.
        if (repository.Branch is null)
            return;

        var rawUrl = $"https://raw.githubusercontent.com/{repository.Identifier}/{repository.Branch}";
        var blobUrl = $"https://github.com/{repository.Identifier}/blob/{repository.Branch}";
        var treeUrl = $"https://github.com/{repository.Identifier}/tree/{repository.Branch}";

        repository.GetGitHubDownloadUrl(RootDirectory / "LICENSE").Should().Be($"{rawUrl}/LICENSE");

        repository.GetGitHubBrowseUrl("LICENSE").Should().Be($"{blobUrl}/LICENSE");
        repository.GetGitHubBrowseUrl("src").Should().Be($"{treeUrl}/src");

        repository.GetGitHubBrowseUrl(RootDirectory / "LICENSE").Should().Be($"{blobUrl}/LICENSE");
        repository.GetGitHubBrowseUrl(RootDirectory / "src").Should().Be($"{treeUrl}/src");
        repository.GetGitHubBrowseUrl(RootDirectory / "Directory.Build.props").Should().Be($"{blobUrl}/Directory.Build.props");

        repository.GetGitHubBrowseUrl("directory", itemType: GitHubItemType.Directory).Should().Be($"{treeUrl}/directory");
        repository.GetGitHubBrowseUrl("dir/file", itemType: GitHubItemType.File).Should().Be($"{blobUrl}/dir/file");

        repository.GetGitHubBrowseUrl(branch: repository.Branch).Should().Be(treeUrl);
    }

    [Fact]
    public void GitHubRepositoryFromUrlSpec()
    {
        var repository = GitRepository.FromUrl("https://github.com/nuke-build/nuke", "dev");

        repository.GetGitHubBrowseUrl("LICENSE", itemType: GitHubItemType.File).Should().Be($"{repository}/blob/dev/LICENSE");
        repository.GetGitHubBrowseUrl("source", itemType: GitHubItemType.Directory).Should().Be($"{repository}/tree/dev/source");
    }
}
