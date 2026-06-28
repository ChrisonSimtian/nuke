using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fallout.Common;
using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Solutions;
using Fallout.Common.Tools.GitHub;
using Fallout.Common.Utilities;
using Fallout.Utilities.Text.Yaml;
using static Fallout.Common.ControlFlow;
using static Fallout.Common.Tools.Git.GitTasks;

partial class Build
{
    [Parameter] readonly bool UseHttps;

    AbsolutePath GlobalSolution => RootDirectory / "fallout-global.sln";
    AbsolutePath ExternalRepositoriesDirectory => RootDirectory / "external";
    AbsolutePath ExternalRepositoriesFile => ExternalRepositoriesDirectory / "repositories.yml";

    IEnumerable<Fallout.Solutions.Solution> ExternalSolutions
        => ExternalRepositories
            .Select(x => ExternalRepositoriesDirectory / x.GetGitHubName())
            .Select(x => x.GlobFiles("*.sln").Single())
            .Select(x => x.ReadSolution());

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
