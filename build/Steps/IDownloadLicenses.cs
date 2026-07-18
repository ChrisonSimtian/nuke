using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Components;
using Serilog;
using static Fallout.Common.IO.HttpTasks;

// Step: fetch the third-party licenses bundled into the packages. Hooks into Pack via
// DependentFor<IPack> so it runs as part of the default pipeline, not just on demand.
interface IDownloadLicenses : IFalloutBuild
{
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
}
