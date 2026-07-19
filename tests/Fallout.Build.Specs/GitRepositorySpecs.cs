using System;
using System.IO;
using FluentAssertions;
using Fallout.Common.Git;
using Xunit;

namespace Fallout.Common.Specs;

public class GitRepositorySpecs
{
    [Theory]
    [InlineData("https://github.com/fallout-build", "github.com", "fallout-build")]
    [InlineData("https://github.com/fallout-build/", "github.com", "fallout-build")]
    [InlineData("https://github.com/fallout-build/fallout", "github.com", "fallout-build/fallout")]
    [InlineData("https://github.com/fallout-build/fallout.git", "github.com", "fallout-build/fallout")]
    [InlineData("https://user:pass@github.com/fallout-build/fallout.git", "github.com", "fallout-build/fallout")]
    [InlineData(" https://github.com/TdMxm/fallout.git", "github.com", "TdMxm/fallout")]
    [InlineData("git@git.test.org:test", "git.test.org", "test")]
    [InlineData("git@git.test.org/test", "git.test.org", "test")]
    [InlineData("git@git.test.org/test/", "git.test.org", "test")]
    [InlineData("git@git.test.org/test.git", "git.test.org", "test")]
    [InlineData("ssh://git@git.test.org/test.git", "git.test.org", "test")]
    [InlineData("ssh://git@git.test.org:1234/test.git", "git.test.org", "test")]
    [InlineData("ssh://git.test.org/test/test", "git.test.org", "test/test")]
    [InlineData("ssh://git.test.org:1234/test/test", "git.test.org", "test/test")]
    [InlineData("https://git.test.org:1234/test/test", "git.test.org", "test/test")]
    [InlineData("git://git.test.org:1234/test/test", "git.test.org", "test/test")]
    [InlineData("git://git.test.org/test/test", "git.test.org", "test/test")]
    public void Parsed_from_url(string url, string endpoint, string identifier)
    {
        var repository = GitRepository.FromUrl(url);
        repository.Endpoint.Should().Be(endpoint);
        repository.Identifier.Should().Be(identifier);
    }

    [Theory]
    [InlineData("https://github.com/fallout-build", GitProtocol.Https)]
    [InlineData("git@git.test.org:test", GitProtocol.Ssh)]
    [InlineData("ssh://git.test.org:1234/test/test", GitProtocol.Ssh)]
    [InlineData("git://git.test.org:1234/test/test", GitProtocol.Ssh)]
    public void Parsed_from_url_with_protocol(string url, GitProtocol protocol)
    {
        var repository = GitRepository.FromUrl(url);
        repository.Protocol.Should().Be(protocol);
    }

    [Fact]
    public void Parsed_from_directory()
    {
        var repository = GitRepository.FromLocalDirectory(Directory.GetCurrentDirectory()).NotNull();
        repository.Endpoint.Should().NotBeNullOrEmpty();
        repository.Identifier.Should().NotBeNullOrEmpty();
        repository.LocalDirectory.Should().NotBeNull();
        repository.Head.Should().NotBeNullOrEmpty();
        repository.Commit.Should().NotBeNullOrEmpty();
        repository.Tags.Should().NotBeNull();
    }

    [Fact]
    public void Fails_for_non_git_directory()
    {
        var tempDir = $"{Path.GetTempPath()}fallout-test-{Guid.NewGuid().ToString("N")[..8]}";
        Directory.CreateDirectory(tempDir);

        try
        {
            var act = () => GitRepository.FromLocalDirectory(tempDir);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Failed to retrieve Git repository information");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// VS Code's Git extension is known to append a duplicate `vscode-merge-base` line to the
    /// `[branch "name"]` section instead of updating it in place, which used to crash
    /// GetRemoteNameAndBranch's ToDictionary call.
    /// </summary>
    [Fact]
    public void Handles_duplicate_config_keys_caused_by_vs_code()
    {
        // Arrange
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            RunGit(directory, "init --quiet --initial-branch=main");
            RunGit(directory, "config user.email test@example.com");
            RunGit(directory, "config user.name Test");
            File.WriteAllText(Path.Combine(directory, "file.txt"), "content");
            RunGit(directory, "add file.txt");
            RunGit(directory, "commit --quiet -m initial");
            // Deliberately not a real Fallout-build/Fallout URL: some CI environments rewrite
            // that org's URLs (e.g. to a canary mirror) via a global git `insteadOf` config,
            // which would silently change the identifier this test asserts on.
            RunGit(directory, "remote add origin https://github.com/octocat/hello-world.git");
            RunGit(directory, "config branch.main.remote origin");
            RunGit(directory, "config branch.main.merge refs/heads/main");

            var configPath = Path.Combine(directory, ".git", "config");
            File.AppendAllText(
                configPath,
                "\tvscode-merge-base = origin/main" + Environment.NewLine +
                "\tvscode-merge-base = origin/main" + Environment.NewLine);

            // Act
            var repository = GitRepository.FromLocalDirectory(directory);

            // Assert
            repository.Branch.Should().Be("main");
            repository.Identifier.Should().Be("octocat/hello-world");
        }
        finally
        {
            // Git marks object files read-only, which trips up Directory.Delete on Windows.
            foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(directory, true);
        }
    }

    private static void RunGit(string workingDirectory, string arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = System.Diagnostics.Process.Start(startInfo).NotNull();
        process.WaitForExit();
        process.ExitCode.Should().Be(0, process.StandardError.ReadToEnd());
    }
}
