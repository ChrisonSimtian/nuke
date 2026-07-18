using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fallout.Common;
using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Common.Tools.GitHub;
using Fallout.Utilities.Text.Yaml;
using static Fallout.Common.ControlFlow;
using static Fallout.Common.Tools.Git.GitTasks;

// Step: check out the companion repositories declared in external/repositories.yml and
// expose their solutions. Depended on by IUpdateContributors, which mines their git log.
interface IHandleExternalRepositories : IFalloutBuild
{
    [Parameter] bool UseHttps => TryGetValue<bool?>(() => UseHttps) ?? false;

    AbsolutePath ExternalRepositoriesDirectory => RootDirectory / "external";
    AbsolutePath ExternalRepositoriesFile => ExternalRepositoriesDirectory / "repositories.yml";

    IEnumerable<GitRepository> ExternalRepositories
        => ExternalRepositoriesFile.ReadYaml<string[]>().Select(x => GitRepository.FromUrl(x));

    Target CheckoutExternalRepositories => _ => _
        .Executes(() =>
        {
            foreach (var repository in ExternalRepositories)
            {
                var repositoryDirectory = ExternalRepositoriesDirectory / repository.GetGitHubName();
                var origin = UseHttps ? repository.HttpsUrl : repository.SshUrl;

                if (!Directory.Exists(repositoryDirectory))
                    Git($"clone {origin} {repositoryDirectory} --progress");
                else
                {
                    SuppressErrors(() => Git($"remote add origin {origin}", repositoryDirectory));
                    Git($"remote set-url origin {origin}", repositoryDirectory);
                }
            }
        });
}
