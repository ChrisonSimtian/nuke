using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Xunit;

namespace Fallout.Common.Specs;

public class GitRepositoryWorktreeSpecs
{
    [Fact]
    public void Parsed_from_worktree()
    {
        var tempDir = GetTemporaryDirectory();
        const string worktreeName = "test-worktree";
        var branchName = GitRepository.FromLocalDirectory(Directory.GetCurrentDirectory()).Branch;
        var worktreePath = CreateWorktree(tempDir, worktreeName, branchName);

        try
        {
            var repository = GitRepository.FromLocalDirectory(worktreePath);
            var mainRepository = GitRepository.FromLocalDirectory(Directory.GetCurrentDirectory());

            AssertWorktreeRepository(repository, mainRepository, worktreePath);
            repository.Branch.Should().StartWith($"{branchName}-test-"); // Verify branch name resolution
        }
        finally
        {
            CleanupWorktree(worktreePath);
            CleanupTemporaryDirectory(tempDir);
        }
    }

    [Fact]
    public void Parsed_from_nested_worktree_directory()
    {
        var tempDir = GetTemporaryDirectory();
        const string worktreeName = "nested-worktree";
        var branchName = GitRepository.FromLocalDirectory(Directory.GetCurrentDirectory()).Branch;
        var worktreePath = CreateWorktree(tempDir, worktreeName, branchName);
        var nestedPath = worktreePath / "nested" / "deeply" / "nested";
        nestedPath.CreateDirectory();

        try
        {
            var repository = GitRepository.FromLocalDirectory(nestedPath);
            var mainRepository = GitRepository.FromLocalDirectory(Directory.GetCurrentDirectory());

            AssertWorktreeRepository(repository, mainRepository, worktreePath);
        }
        finally
        {
            CleanupWorktree(worktreePath);
            CleanupTemporaryDirectory(tempDir);
        }
    }

    [Fact]
    public void Parsed_from_worktree_with_detached_head()
    {
        var tempDir = GetTemporaryDirectory();
        const string worktreeName = "detached-head-worktree";
        var branchName = GitRepository.FromLocalDirectory(Directory.GetCurrentDirectory()).Branch;
        var worktreePath = CreateWorktree(tempDir, worktreeName, branchName);

        try
        {
            // Get a commit hash to checkout
            var commitHash = ProcessTasks.StartProcess("git", "rev-parse HEAD~1", workingDirectory: worktreePath)
                .AssertZeroExitCode()
                .Output
                .First()
                .Text
                .Trim();

            // Checkout the commit to create detached HEAD
            ProcessTasks.StartProcess("git", $"checkout {commitHash}", workingDirectory: worktreePath)
                .AssertZeroExitCode();

            var repository = GitRepository.FromLocalDirectory(worktreePath);
            var mainRepository = GitRepository.FromLocalDirectory(Directory.GetCurrentDirectory());

            repository.Endpoint.Should().Be(mainRepository.Endpoint);
            repository.Identifier.Should().Be(mainRepository.Identifier);
            // Use string comparison to handle symlink resolution differences (e.g., /var vs /private/var)
            repository.LocalDirectory.ToString().Should().EndWith(worktreePath.ToString().Split('/').Last());
            repository.Branch.Should().BeNull(); // Detached HEAD should have no branch
            repository.Head.Should().Be(commitHash);
            repository.Commit.Should().Be(commitHash);
        }
        finally
        {
            CleanupWorktree(worktreePath);
            CleanupTemporaryDirectory(tempDir);
        }
    }

    [Fact]
    public void Fails_for_worktree_with_invalid_git_file()
    {
        var tempDir = GetTemporaryDirectory();
        var invalidGitDir = tempDir / "invalid-git";
        invalidGitDir.CreateDirectory();

        try
        {
            var gitFile = invalidGitDir / ".git";
            gitFile.WriteAllText("invalid content without gitdir prefix");

            var act = () => GitRepository.FromLocalDirectory(invalidGitDir);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Failed to retrieve Git repository information");
        }
        finally
        {
            CleanupTemporaryDirectory(tempDir);
        }
    }

    [Fact]
    public void Fails_for_worktree_with_non_existent_path()
    {
        var tempDir = GetTemporaryDirectory();
        var invalidGitDir = tempDir / "non-existent";
        invalidGitDir.CreateDirectory();

        try
        {
            var gitFile = invalidGitDir / ".git";
            var nonExistentPath = tempDir / "does-not-exist" / ".git";
            gitFile.WriteAllText($"gitdir: {nonExistentPath}");

            var act = () => GitRepository.FromLocalDirectory(invalidGitDir);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Failed to retrieve Git repository information");
        }
        finally
        {
            CleanupTemporaryDirectory(tempDir);
        }
    }

    // Helper methods
    private static AbsolutePath GetTemporaryDirectory()
    {
        var tempDir = AbsolutePath.Create(Path.GetTempPath()) / $"fallout-test-{Guid.NewGuid().ToString("N")[..8]}";
        tempDir.CreateDirectory();
        return tempDir;
    }

    private static AbsolutePath CreateWorktree(AbsolutePath tempDir, string worktreeName, string branchName)
    {
        var worktreePath = tempDir / worktreeName;
        var uniqueBranchName = $"{branchName}-test-{Guid.NewGuid().ToString("N")[..8]}";

        // Create a new branch from the specified branch and checkout in worktree
        ProcessTasks.StartProcess("git", $"worktree add -b {uniqueBranchName} {worktreePath} {branchName}", workingDirectory: Directory.GetCurrentDirectory())
            .AssertZeroExitCode();

        return worktreePath;
    }

    private static void CleanupWorktree(AbsolutePath worktreePath)
    {
        if (!worktreePath.DirectoryExists())
            return;

        try
        {
            ProcessTasks.StartProcess("git", $"worktree remove {worktreePath}", workingDirectory: Directory.GetCurrentDirectory())
                .AssertZeroExitCode();
        }
        catch
        {
            // If git worktree remove fails, force delete the directory
            worktreePath.DeleteDirectory();
        }
    }

    private static void CleanupTemporaryDirectory(AbsolutePath tempDir)
    {
        if (!tempDir.DirectoryExists())
            return;

        try
        {
            tempDir.DeleteDirectory();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private static void AssertWorktreeRepository(GitRepository worktreeRepo, GitRepository mainRepo, AbsolutePath expectedWorktreePath)
    {
        worktreeRepo.Endpoint.Should().Be(mainRepo.Endpoint);
        worktreeRepo.Identifier.Should().Be(mainRepo.Identifier);
        // Use string comparison to handle symlink resolution differences (e.g., /var vs /private/var)
        worktreeRepo.LocalDirectory.ToString().Should().EndWith(expectedWorktreePath.ToString().Split('/').Last());
        worktreeRepo.Head.Should().NotBeNullOrEmpty();
        worktreeRepo.Commit.Should().NotBeNullOrEmpty();
        worktreeRepo.Tags.Should().NotBeNull();
    }
}
