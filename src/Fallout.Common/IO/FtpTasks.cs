using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Serilog;

namespace Fallout.Common.IO;

public static class FtpTasks
{
    public static NetworkCredential FtpCredentials { get; set; }

    public static void FtpUploadDirectoryRecursively(string host, string directory, string serverRoot)
    {
        FtpUploadDirectoryRecursivelyAsync(host, directory, serverRoot).GetAwaiter().GetResult();
    }

    public static async Task FtpUploadDirectoryRecursivelyAsync(
        string host,
        string directory,
        string serverRoot,
        CancellationToken cancellationToken = default)
    {
        Log.Information("Uploading directory {Directory} to {ServerRoot} ...", directory, serverRoot);

        var files = Globbing.GlobFiles(directory, "**/*").ToList();

        await using var client = CreateClient(host);
        await client.Connect(cancellationToken);

        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];
            var relativePath = PathConstruction.GetRelativePath(directory, file);
            var serverPath = CombineServerPath(serverRoot, relativePath);

            Log.Debug("[{Index}/{Count}] Uploading to {ServerPath} ...", index + 1, files.Count, serverPath);
            await client.UploadFile(
                file,
                serverPath,
                createRemoteDir: true,
                token: cancellationToken);
        }
    }

    public static void FtpUploadFile(string host, string file, string serverDestination)
    {
        FtpUploadFileAsync(host, file, serverDestination).GetAwaiter().GetResult();
    }

    public static async Task FtpUploadFileAsync(
        string host,
        string file,
        string serverDestination,
        CancellationToken cancellationToken = default)
    {
        Log.Debug("Uploading to {ServerDestination} ...", serverDestination);

        await using var client = CreateClient(host);
        await client.Connect(cancellationToken);
        await client.UploadFile(
            file,
            serverDestination,
            createRemoteDir: true,
            token: cancellationToken);
    }

    public static void FtpMakeDirectory(string host, string path)
    {
        FtpMakeDirectoryAsync(host, path).GetAwaiter().GetResult();
    }

    public static async Task FtpMakeDirectoryAsync(
        string host,
        string path,
        CancellationToken cancellationToken = default)
    {
        await using var client = CreateClient(host);
        await client.Connect(cancellationToken);
        await client.CreateDirectory(path, force: true, token: cancellationToken);
    }

    private static AsyncFtpClient CreateClient(string host)
    {
        var credentials = FtpCredentials ?? new NetworkCredential();
        return new AsyncFtpClient(host, credentials);
    }

    private static string CombineServerPath(string serverRoot, string relativePath)
    {
        var normalizedRelative = relativePath.Replace('\\', '/');
        return $"{serverRoot.TrimEnd('/')}/{normalizedRelative.TrimStart('/')}";
    }
}
