using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Common.Tools.GitHub;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using Fallout.Components;
using Fallout.Solutions;
using Fallout.Utilities.Text.Yaml;
using Serilog;
using static Fallout.CodeGeneration.CodeGenerator;
using static Fallout.CodeGeneration.ReferenceUpdater;
using static Fallout.Common.ControlFlow;
using static Fallout.Common.IO.HttpTasks;
using static Fallout.Common.Tools.Git.GitTasks;

// Out-of-band maintenance/generation targets: not part of the default
// Restore -> Compile -> Test -> Pack -> Publish pipeline. Consolidated here from
// the former per-target partials (Build.CodeGeneration / .PublicApi / .Licenses /
// .Contributors / .Stargazers / .GlobalSolution) to keep the build project's file
// count down. The attribute-bearing partials (Build.CI.GitHubActions, Build.Terminal)
// stay separate because class-level attributes can't live on an interface or be merged.
partial class Build
{
    // --- Tool wrappers & CLI reference generation -------------------------------

    AbsolutePath SpecificationsDirectory => RootDirectory / "src" / "Fallout.Common" / "Tools";
    AbsolutePath ReferencesDirectory => RootDirectory / "docs" / "cli-tools";

    Target References => _ => _
        .Requires(() => GitHasCleanWorkingCopy())
        .Executes(() =>
        {
            ReferencesDirectory.CreateOrCleanDirectory();

            UpdateReferences(SpecificationsDirectory, ReferencesDirectory);
        });

    Target GenerateTools => _ => _
        .Executes(() =>
        {
            SpecificationsDirectory.GlobFiles("*/*.json").ForEach(x =>
                GenerateCode(
                    x,
                    namespaceProvider: x => $"Fallout.Common.Tools.{x.Name}",
                    sourceFileProvider: x => GitRepository.SetBranch(MainBranch).GetGitHubBrowseUrl(x.SpecificationFile)));
        });

    // --- Public API surface dump ------------------------------------------------

    AbsolutePath PublicApiFile => RootDirectory / "PUBLIC_API.md";

    Target GeneratePublicApi => _ => _
        .Executes(() =>
        {
            var types = typeof(FalloutBuild).Assembly
                .GetTypes()
                .SelectMany(x => x.DescendantsAndSelf(y => y.GetNestedTypes()))
                .Where(x => x.IsPublic || x.IsNestedPublic)
                .Distinct()
                .OrderBy(x => x.FullName).ToList();

            var builder = new StringBuilder();

            builder
                .AppendLine("# Public API")
                .AppendLine()
                .AppendLine("## Namespaces & Types")
                .AppendLine();

            var groups = types.GroupBy(x => x.Namespace);

            foreach (var group in groups)
            {
                builder.AppendLine($"### {group.Key}");
                builder.AppendLine();
                group.ForEach(x => builder.AppendLine($"- {x.GetDisplayName()}"));
                builder.AppendLine();
            }

            builder
                .AppendLine("## Types & Methods")
                .AppendLine();

            foreach (var type in types)
            {
                builder
                    .AppendLine($"### {type.Namespace}.{type.GetDisplayName()}")
                    .AppendLine();

                var memberInfos = type
                    .GetMembers(ReflectionUtility.All | BindingFlags.DeclaredOnly);

                bool DefaultFilter(MemberInfo member)
                {
                    if (member is PropertyInfo)
                        return false;

                    if (member is Type && !member.IsPublic())
                        return false;

                    if (!(member.IsPublic() || member.IsFamily() && !member.DeclaringType.NotNull().IsSealed))
                        return false;

                    if (member is FieldInfo { IsSpecialName: true })
                        return false;

                    return true;
                }

                var members = memberInfos
                    .Where(DefaultFilter)
                    .OrderByDescending(x => x is FieldInfo)
                    .ThenByDescending(x => x is ConstructorInfo)
                    .ThenByDescending(x => x is MethodInfo)
                    .ThenByDescending(x => x.Name.StartsWith("get_") || x.Name.StartsWith("set_"))
                    .ThenBy(x => x.Name);

                foreach (var member in members)
                    builder.AppendLine($"- {member.GetDisplayText()}");

                builder.AppendLine();
            }

            PublicApiFile.WriteAllText(builder.ToString());
        });

    // --- Third-party license bundling (part of Pack) ----------------------------

    AbsolutePath LicensesDirectory => TemporaryDirectory / "licenses";

    IEnumerable<(string Project, string Url)> Licenses
        => new[]
           {
               ("Glob", "https://raw.githubusercontent.com/kthompson/glob/develop/LICENSE"),
               ("ICSharpCode.SharpZipLib", "https://raw.githubusercontent.com/icsharpcode/SharpZipLib/master/LICENSE.txt"),
               ("Microsoft.Build", "https://raw.githubusercontent.com/dotnet/msbuild/main/LICENSE"),
               ("Microsoft.CodeAnalysis", "https://raw.githubusercontent.com/dotnet/roslyn/main/License.txt"),
               ("Newtonsoft.Json", "https://raw.githubusercontent.com/JamesNK/Newtonsoft.Json/master/LICENSE.md"),
               ("NuGet", "https://raw.githubusercontent.com/NuGet/NuGet.Client/dev/LICENSE.txt"),
               ("Octokit", "https://raw.githubusercontent.com/octokit/octokit.net/main/LICENSE.txt"),
               ("Serilog", "https://raw.githubusercontent.com/serilog/serilog/dev/LICENSE"),
               ("Spectre.Console", "https://raw.githubusercontent.com/spectreconsole/spectre.console/main/LICENSE.md"),
               ("YamlDotNet", "https://raw.githubusercontent.com/aaubry/YamlDotNet/master/LICENSE.txt")
           };

    Target DownloadLicenses => _ => _
        .After<ICompile>()
        .DependentFor<IPack>()
        .Executes(() =>
        {
            LicensesDirectory.CreateOrCleanDirectory();

            var downloadTasks = Licenses.Select(async x =>
            {
                await HttpDownloadFileAsync(x.Url, LicensesDirectory / $"{x.Project}.txt");
                Log.Information("Downloaded license for {Project}", x.Project);
            });
            Task.WaitAll(downloadTasks.ToArray());
        });

    // --- Contributors & stargazers ----------------------------------------------

    AbsolutePath ContributorsFile => RootDirectory / "CONTRIBUTORS.md";
    AbsolutePath ContributorsCacheFile => TemporaryDirectory / "contributors.dat";

    Target UpdateContributors => _ => _
        .Executes(() =>
        {
            var previousContributors = ContributorsCacheFile.Existing()?.ReadAllLines() ?? [];

            var repositoryDirectories = new[] { RootDirectory / ".git" }
                .Concat(ExternalRepositoriesDirectory.GlobDirectories("*/.git"));
            var contributors = repositoryDirectories
                .SelectMany(x => Git(@"log --pretty=""%an|%ae%n%cn|%ce""", workingDirectory: x, logOutput: false))
                .Select(x => x.Text)
                .Distinct().ToList()
                .Select(x => x.Split('|'))
                .ForEachLazy(x => Assert.Count(x, length: 2))
                .Select(x => new { Name = x[0], Email = x[1] }).ToList();

            var newContributors = contributors.Where(x => !previousContributors.Contains(x.Email));

            foreach (var newContributor in newContributors)
            {
                var content = (ContributorsFile.Existing()?.ReadAllLines() ?? [])
                    .Concat($"- {newContributor.Name}").OrderBy(x => x);
                ContributorsFile.WriteAllLines(content, Encoding.Default);
                Git($"add {ContributorsFile}");

                var message = $"Add {newContributor.Name} as contributor".DoubleQuote();
                var author = $"{newContributor.Name} <{newContributor.Email}>".DoubleQuote();
                Git($"commit -m {message} --author {author}");
            }

            ContributorsCacheFile.WriteAllLines(contributors.Select(x => x.Email).ToList());
        });

    AbsolutePath StargazersFile => TemporaryDirectory / "stargazers.csv";

    Target UpdateStargazers => _ => _
        .Executes(async () =>
        {
            var stargazerUsers = await GitHubTasks.GitHubClient.Activity.Starring.GetAllStargazers(
                GitRepository.GetGitHubOwner(),
                GitRepository.GetGitHubName());
            var stargazerEntries = stargazerUsers.Select(async x =>
            {
                var user = await GitHubTasks.GitHubClient.User.Get(x.Login);
                return new[]
                       {
                           user.Login.DoubleQuote(),
                           user.Name.DoubleQuote(),
                           user.Company.DoubleQuote(),
                           user.Location.DoubleQuote(),
                           user.Email.DoubleQuote(),
                           user.Blog.DoubleQuote()
                       };
            }).ToList();

            await Task.WhenAll(stargazerEntries);

            StargazersFile.WriteAllLines(
                new[] { new[] { "Login", "Name", "Company", "Location", "Email", "Blog" } }
                    .Concat(stargazerEntries.Select(x => x.Result).OrderBy(x => x.First()))
                    .Select(x => x.JoinComma()));
        });

    // --- External repositories / global solution --------------------------------

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
