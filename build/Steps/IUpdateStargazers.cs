using System.Linq;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tools.GitHub;
using Fallout.Common.Utilities;
using Fallout.Components;

// Step: snapshot the repository's stargazers to a CSV in the temp directory.
interface IUpdateStargazers : IFalloutBuild, IHasGitRepository
{
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
}
