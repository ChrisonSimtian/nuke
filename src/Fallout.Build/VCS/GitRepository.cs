using System;
using System.Collections.Generic;
using System.Linq;
using Fallout.Common.CI;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using LibGit2Sharp;

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
        var rootDirectory = directory.FindParentOrSelf(x => x.ContainsDirectory(".git")).NotNull($"No parent Git directory for '{directory}'");

        using var repository = new Repository(rootDirectory);

        var head = GetHead(repository);
        var branch = (GetBranchFromCI() ?? GetHeadBranch(repository))?.TrimStart("refs/heads/").TrimStart("origin/");
        var commit = GetCommitFromCI() ?? repository.Head.Tip?.Sha;
        var tags = GetTagsFromCommit(repository, commit);
        var (remoteName, remoteBranch) = GetRemoteNameAndBranch(repository, branch);
        var (protocol, endpoint, identifier) = GetRemoteConnectionFromConfig(repository, remoteName ?? FallbackRemoteName);

        return new GitRepository(
            protocol,
            endpoint,
            identifier,
            branch,
            rootDirectory,
            head,
            commit,
            tags,
            remoteName,
            remoteBranch);
    }

    private static string GetHead(Repository repository)
    {
        // Mirrors the value previously read from .git/HEAD: the symbolic ref for an
        // attached head (refs/heads/<branch>), or the commit sha for a detached head.
        var head = repository.Head;
        return head.Reference is SymbolicReference symbolic
            ? symbolic.TargetIdentifier
            : head.Tip?.Sha;
    }

    private static string GetHeadBranch(Repository repository)
    {
        var head = repository.Head;
        return head.Reference is SymbolicReference symbolic ? symbolic.TargetIdentifier : null;
    }

    private static (string Name, string Branch) GetRemoteNameAndBranch(Repository repository, string branch)
    {
        if (branch == null)
            return (null, null);

        var trackedBranch = repository.Branches[branch]?.TrackedBranch;
        if (trackedBranch == null || !trackedBranch.IsRemote)
            return (null, null);

        return (trackedBranch.RemoteName, trackedBranch.UpstreamBranchCanonicalName?.TrimStart("refs/heads/"));
    }

    internal static string GetBranchFromCI()
    {
        return (Host.Instance as IBuildServer)?.Branch;
    }

    internal static string GetCommitFromCI()
    {
        return (Host.Instance as IBuildServer)?.Commit;
    }

    private static IReadOnlyCollection<string> GetTagsFromCommit(Repository repository, string commit)
    {
        if (commit == null)
            return Array.Empty<string>();

        return repository.Tags
            .Where(x => x.Target.Sha == commit)
            .Select(x => x.FriendlyName)
            .ToList();
    }

    private static (GitProtocol? Protocol, string Endpoint, string Identifier) GetRemoteConnectionFromConfig(
        Repository repository,
        string remote)
    {
        var url = repository.Network.Remotes[remote]?.Url;
        return url == null
            ? (null, null, null)
            : GetRemoteConnectionFromUrl(url);
    }

    private static (GitProtocol Protocol, string Endpoint, string Identifier) GetRemoteConnectionFromUrl(string url)
    {
        url = url.NotNull().Trim();

        // Standard schemes (https://, ssh://, git://, http://) parse cleanly via Uri.
        // SCP-like syntax (git@host:path) is not a valid Uri, so it is handled separately.
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.HostNameType != UriHostNameType.Unknown)
        {
            var protocol = uri.Scheme.EqualsOrdinalIgnoreCase(Uri.UriSchemeHttps)
                ? GitProtocol.Https
                : GitProtocol.Ssh;
            return (protocol, uri.Host, NormalizeIdentifier(uri.AbsolutePath));
        }

        return ParseScpLikeUrl(url);
    }

    private static (GitProtocol Protocol, string Endpoint, string Identifier) ParseScpLikeUrl(string url)
    {
        // Forms: [user@]host:path or [user@]host/path (no scheme prefix).
        var withoutUser = url.Contains('@') ? url.Substring(url.IndexOf('@') + 1) : url;

        var separatorIndex = withoutUser.IndexOfAny(new[] { ':', '/' });
        Assert.True(separatorIndex > 0, $"Url '{url}' could not be parsed.");

        var endpoint = withoutUser.Substring(0, separatorIndex);
        var path = withoutUser.Substring(separatorIndex + 1);

        return (GitProtocol.Ssh, endpoint, NormalizeIdentifier(path));
    }

    private static string NormalizeIdentifier(string path)
    {
        return path
            .Trim('/')
            .TrimEnd(".git");
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
}
