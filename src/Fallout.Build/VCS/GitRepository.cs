using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Fallout.Common.CI;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;

namespace Fallout.Common.Git;

public enum GitProtocol
{
    Https,
    Ssh
}

public class GitRepository
{
    private const string FallbackRemoteName = "origin";

    public static GitRepository FromUrl(string url, string branch = null)
    {
        var (protocol, endpoint, identifier) = GetRemoteConnectionFromUrl(url);
        return new GitRepository(
            protocol,
            endpoint,
            identifier,
            branch,
            localDirectory: null,
            head: null,
            commit: null,
            tags: null,
            remoteName: null,
            remoteBranch: null);
    }

    /// <summary>
    /// Obtains information from a local git repository.
    /// </summary>
    public static GitRepository FromLocalDirectory(AbsolutePath directory)
    {
        var metadata = GetGitMetadata(directory);

        var head = metadata.Head;
        var branch = (GetBranchFromCI() ?? GetHeadIfAttached(head))?.TrimStart("refs/heads/").TrimStart("origin/");
        var commit = GetCommitFromCI() ?? GetCommitFromHead(metadata.GitDirectory, head);
        var tags = GetTagsFromCommit(metadata.GitDirectory, commit);
        var (remoteName, remoteBranch) = GetRemoteNameAndBranch(metadata.GitDirectory, branch);
        var (protocol, endpoint, identifier) = GetRemoteConnectionFromConfig(metadata.GitDirectory, remoteName ?? FallbackRemoteName);

        return new GitRepository(
            protocol,
            endpoint,
            identifier,
            branch,
            metadata.RootDirectory,
            head,
            commit,
            tags,
            remoteName,
            remoteBranch);
    }

    /// <summary>
    /// Obtains information from a local git repository.
    /// </summary>
    private static GitMetadata GetGitMetadata(AbsolutePath directory)
    {
        var rootDirectory = directory.FindParentOrSelf(x => x.ContainsDirectory(".git") || x.ContainsFile(".git"));

        if (rootDirectory is not null)
        {
            var gitDirectory = rootDirectory / ".git";
            if (File.Exists(gitDirectory))
            {
                var content = File.ReadAllText(gitDirectory).Trim();
                if (content.StartsWith("gitdir:"))
                {
                    var path = content.Substring("gitdir:".Length).Trim();
                    gitDirectory = Path.IsPathRooted(path) ? path : rootDirectory / path;
                }
            }

            if (Directory.Exists(gitDirectory))
            {
                var head = GetHead(gitDirectory);
                return new GitMetadata(rootDirectory, gitDirectory, head);
            }
        }

        var worktreeInfo = GetWorktreeInfoFromGit(directory);
        return worktreeInfo ?? throw new InvalidOperationException("No Git repository found");
    }

    private static GitMetadata GetWorktreeInfoFromGit(AbsolutePath directory)
    {
        try
        {
            // Get all information in one call
            var process = ProcessTasks.StartProcess("git", "rev-parse --show-toplevel --git-common-dir --symbolic-full-name HEAD", workingDirectory: directory, logOutput: false);
            process.AssertZeroExitCode();

            var lines = process.Output
                .Where(o => o.Type == OutputType.Std)
                .Select(o => o.Text.Trim())
                .ToArray();

            if (lines.Length < 3)
            {
                throw new InvalidOperationException($"Expected 3 lines from 'git rev-parse --show-toplevel --git-common-dir --symbolic-full-name HEAD' but got {lines.Length} lines: [{string.Join(", ", lines.Select(l => $"'{l}'"))}]");
            }

            var rootDirectory = lines[0].Trim();
            var gitDirectory = lines[1].Trim();
            var head = lines[2].Trim();

            // For detached HEAD, --symbolic-full-name HEAD returns "HEAD"
            // In this case, get the actual commit SHA
            if (head == "HEAD")
            {
                var commitProcess = ProcessTasks.StartProcess("git", "rev-parse HEAD", workingDirectory: directory, logOutput: false);
                commitProcess.AssertZeroExitCode();

                head = commitProcess.Output
                    .Where(o => o.Type == OutputType.Std)
                    .Select(o => o.Text.Trim())
                    .FirstOrDefault();
            }

            return new GitMetadata(rootDirectory, gitDirectory, head);
        }
        catch (ProcessException ex)
        {
            throw new InvalidOperationException("Failed to retrieve Git repository information", ex);
        }
    }

    private static (string Name, string Branch) GetRemoteNameAndBranch(AbsolutePath gitDirectory, string branch)
    {
        if (branch == null)
            return (null, null);

        var configFile = gitDirectory / "config";
        if (!configFile.Exists())
        {
            var commonDir = GetGitCommonDirectory(gitDirectory);
            if (commonDir != null)
                configFile = commonDir / "config";
        }

        if (!configFile.Exists())
            return (null, null);

        var configFileContent = configFile.ReadAllLines();

        // Git config files are INI-style: sections are denoted by a [header] line, with
        // indented key = value pairs beneath them. To find the remote and merge ref for
        // the given branch, we locate the matching [branch "..."] section, read all
        // key-value lines until the next section begins, and project them into a dictionary.
        // GroupBy + Last() handles the (uncommon) case of a duplicated key, keeping the
        // final value as git itself would.
        Dictionary<string, string> data = configFileContent
            .Select(x => x.Trim())
            .SkipWhile(x => x != $"[branch {branch.DoubleQuote()}]")
            .Skip(1)
            .TakeWhile(x => !x.StartsWith("["))
            .Where(x => x.Contains('='))
            .Select(x => x.Split('=', 2))
            .GroupBy(x => x[0].Trim(), x => x[1].Trim())
            .ToDictionary(x => x.Key, x => x.Last());

        return data.TryGetValue("remote", out var remote) && data.TryGetValue("merge", out var merge)
            ? (remote, merge.TrimStart("refs/heads/"))
            : (null, null);
    }

    internal static string GetHeadIfAttached(string head)
    {
        return head.StartsWith("refs/heads/") ? head : null;
    }

    internal static string GetCommitFromHead(AbsolutePath gitDirectory, string head)
    {
        if (head == null)
            return null;

        if (!head.StartsWith("refs/heads/"))
            return head;

        var headRefFile = gitDirectory / head;
        if (headRefFile.Exists())
            return headRefFile.ReadAllLines().First();

        var commonDir = GetGitCommonDirectory(gitDirectory);
        if (commonDir != null)
        {
            headRefFile = commonDir / head;
            if (headRefFile.Exists())
                return headRefFile.ReadAllLines().First();
        }

        var commit = GetPackedRefs(gitDirectory)
            .Concat(commonDir != null ? GetPackedRefs(commonDir) : Array.Empty<(string, string)>())
            .Where(x => x.Reference == head)
            .Select(x => x.Commit)
            .FirstOrDefault();

        commit.NotNull("Could not find commit information");

        return commit;
    }

    private static string GetHead(AbsolutePath gitDirectory)
    {
        var headFile = gitDirectory / "HEAD";
        Assert.FileExists(headFile);
        return headFile.ReadAllText().TrimStart("ref: ").Trim();
    }

    internal static string GetBranchFromCI()
    {
        return (Host.Instance as IBuildServer)?.Branch;
    }

    internal static string GetCommitFromCI()
    {
        return (Host.Instance as IBuildServer)?.Commit;
    }

    private static IReadOnlyCollection<string> GetTagsFromCommit(AbsolutePath gitDirectory, string commit)
    {
        if (commit == null)
            return Array.Empty<string>();

        var commonDir = GetGitCommonDirectory(gitDirectory);

        var packedTags = GetPackedRefs(gitDirectory)
            .Concat(commonDir != null ? GetPackedRefs(commonDir) : Array.Empty<(string, string)>())
            .Where(x => x.Commit == commit && x.Reference.StartsWithOrdinalIgnoreCase("refs/tags"))
            .Select(x => x.Reference.TrimStart("refs/tags/"));

        var tagsDirectory = gitDirectory / "refs" / "tags";
        var localTags = tagsDirectory.Exists()
            ? tagsDirectory
                .GlobFiles("**/*")
                .Where(x => x.ReadAllText().Trim() == commit)
                .Select(x => tagsDirectory.GetUnixRelativePathTo(x).ToString())
            : Array.Empty<string>();

        if (commonDir != null)
        {
            var commonTagsDirectory = commonDir / "refs" / "tags";
            if (commonTagsDirectory.Exists())
            {
                localTags = localTags.Concat(commonTagsDirectory
                    .GlobFiles("**/*")
                    .Where(x => x.ReadAllText().Trim() == commit)
                    .Select(x => commonTagsDirectory.GetUnixRelativePathTo(x).ToString()));
            }
        }

        return localTags.Concat(packedTags).Distinct().ToList();
    }

    private static IEnumerable<(string Commit, string Reference)> GetPackedRefs(AbsolutePath gitDirectory)
    {
        var packedRefsFile = gitDirectory / "packed-refs";
        if (!packedRefsFile.Exists())
            return Array.Empty<(string Commit, string Reference)>();

        return packedRefsFile.ReadAllLines()
            .Where(x => !x.StartsWith("#") && !x.StartsWith("^"))
            .Select(x => x.Split(' '))
            .Select(x => (Commit: x[0], Reference: x[1]));
    }

    private static (GitProtocol Protocol, string Endpoint, string Identifier) GetRemoteConnectionFromUrl(string url)
    {
        var regex = new Regex(
            @"^(?'protocol'\w+)?(\:\/\/)?(?>(?'user'.*)@)?(?'endpoint'[^\/:]+)(?>\:(?'port'\d+))?[\/:](?'identifier'.*?)\/?(?>\.git)?$");
        var match = regex.Match(url.NotNull().Trim());

        Assert.True(match.Success, $"Url '{url}' could not be parsed.");
        var protocol = match.Groups["protocol"].Value.EqualsOrdinalIgnoreCase(GitProtocol.Https.ToString())
            ? GitProtocol.Https
            : GitProtocol.Ssh;
        return (protocol, match.Groups["endpoint"].Value, match.Groups["identifier"].Value);
    }

    private static (GitProtocol? Protocol, string Endpoint, string Identifier) GetRemoteConnectionFromConfig(
        AbsolutePath gitDirectory,
        string remote)
    {
        var configFile = gitDirectory / "config";
        if (!configFile.Exists())
        {
            var commonDir = GetGitCommonDirectory(gitDirectory);
            if (commonDir != null)
                configFile = commonDir / "config";
        }

        if (!configFile.Exists())
            return (null, null, null);

        var configFileContent = configFile.ReadAllLines();
        var url = configFileContent
            .Select(x => x.Trim())
            .SkipWhile(x => x != $"[remote {remote.DoubleQuote()}]")
            .Skip(1)
            .TakeWhile(x => !x.StartsWith("["))
            .SingleOrDefault(x => x.StartsWithOrdinalIgnoreCase("url = "))
            ?.Split('=').ElementAt(1)
            .Trim();

        if (url == null)
            return (null, null, null);

        return GetRemoteConnectionFromUrl(url);
    }

    private static AbsolutePath GetGitCommonDirectory(AbsolutePath gitDirectory)
    {
        var commondirFile = gitDirectory / "commondir";
        if (!commondirFile.Exists())
            return null;

        var path = commondirFile.ReadAllText().Trim();
        return Path.IsPathRooted(path) ? path : gitDirectory / path;
    }

    public GitRepository(
        GitProtocol? protocol,
        string endpoint,
        string identifier,
        string branch,
        AbsolutePath localDirectory,
        string head,
        string commit,
        IReadOnlyCollection<string> tags,
        string remoteName,
        string remoteBranch)
    {
        Protocol = protocol;
        Endpoint = endpoint;
        Identifier = identifier;
        Branch = branch;
        LocalDirectory = localDirectory;
        Head = head;
        Commit = commit;
        Tags = tags;
        RemoteName = remoteName;
        RemoteBranch = remoteBranch;
    }

    /// <summary>Default protocol for the repository.</summary>
    public GitProtocol? Protocol { get; private set; }

    /// <summary>Endpoint for the repository. For instance <em>github.com</em>.</summary>
    public string Endpoint { get; private set; }

    /// <summary>Identifier of the repository.</summary>
    public string Identifier { get; private set; }

    /// <summary>Local path from which the repository was parsed.</summary>
    public AbsolutePath LocalDirectory { get; private set; }

    /// <summary>Current head; <c>null</c> if parsed from URL.</summary>
    public string Head { get; private set; }

    /// <summary>Current commit; <c>null</c> if parsed from URL.</summary>
    public string Commit { get; }

    /// <summary>List of tags; <c>null</c> if parsed from URL.</summary>
    public IReadOnlyCollection<string> Tags { get; }

    /// <summary>Name of the remote.</summary>
    public string RemoteName { get; }

    /// <summary>Name of the remote branch.</summary>
    public string RemoteBranch { get; }

    /// <summary>Current branch; <c>null</c> if head is detached.</summary>
    public string Branch { get; private set; }

    /// <summary>Url in the form of <c>https://endpoint/identifier.git</c></summary>
    public string HttpsUrl => Endpoint != null ? $"https://{Endpoint}/{Identifier}.git" : null;

    /// <summary>Url in the form of <c>git@endpoint:identifier.git</c></summary>
    public string SshUrl => Endpoint != null ? $"git@{Endpoint}:{Identifier}.git" : null;

    public GitRepository SetBranch(string branch)
    {
        return new GitRepository(
            Protocol,
            Endpoint,
            Identifier,
            branch,
            LocalDirectory,
            Head,
            Commit,
            Tags,
            RemoteName,
            RemoteBranch);
    }

    public override string ToString()
    {
        return (Protocol == GitProtocol.Https ? HttpsUrl : SshUrl).TrimEnd(".git");
    }

    private record GitMetadata(AbsolutePath RootDirectory, AbsolutePath GitDirectory, string Head);
}
