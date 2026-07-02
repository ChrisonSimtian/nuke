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
}
