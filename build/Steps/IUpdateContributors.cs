using System.Linq;
using System.Text;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using static Fallout.Common.Tools.Git.GitTasks;

// Step: append newly-seen authors from this repo (and the external companions) to
// CONTRIBUTORS.md. Inherits IHandleExternalRepositories for ExternalRepositoriesDirectory.
interface IUpdateContributors : IFalloutBuild, IHandleExternalRepositories
{
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
}
