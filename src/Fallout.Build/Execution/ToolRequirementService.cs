using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using Serilog;

namespace Fallout.Common.Execution;

internal static class ToolRequirementService
{
    public static void EnsureToolRequirements(IFalloutBuild build, IReadOnlyCollection<ExecutableTarget> executionPlan)
    {
        var requirements = build.GetType().GetCustomAttributes<RequiresAttribute>().Select(x => x.GetRequirement())
            .Concat(executionPlan.SelectMany(x => x.ToolRequirements)).ToList();

        InstallNuGetPackages(requirements.OfType<NuGetPackageRequirement>().ToList(), build);
        InstallNpmPackages(requirements.OfType<NpmPackageRequirement>().ToList(), build);
        InstallAptGetPackages(requirements.OfType<AptGetPackageRequirement>().ToList(), build);
    }

    private static void InstallNuGetPackages(IReadOnlyCollection<NuGetPackageRequirement> requirements, IFalloutBuild build)
    {
        if (requirements.Count == 0)
        {
            return;
        }

        var projectFile = build.TemporaryDirectory / "nuget.csproj";
        NuGetToolPathResolver.NuGetPackagesConfigFile = projectFile;
        NuGetToolPathResolver.NuGetAssetsConfigFile = projectFile.Parent / "obj" / "project.assets.json";

        var packages = requirements.OrderBy(x => x.PackageId).ThenBy(x => x.Version).ToList();
        var groupedPackages = packages.GroupBy(x => x.PackageId, x => $"[{x.Version}]");

        var content = $"""
                       <Project Sdk="Microsoft.NET.Sdk">

                         <PropertyGroup>
                           <TargetFramework>net10.0</TargetFramework>
                         </PropertyGroup>

                         <Import Project="{build.BuildProjectFile}" />

                         <ItemGroup>
                       {groupedPackages.Select(x => $"""    <PackageDownload Include="{x.Key}" Version="{x.JoinSemicolon()}" Exclude="@(PackageDownload)" />""").JoinNewLine()}
                         </ItemGroup>

                       </Project>
                       """;

        if (projectFile.Exists() && projectFile.ReadAllText().StartsWith(content))
        {
            return;
        }

        Log.Information("Installing NuGet packages...");
        packages.ForEach(x => Log.Verbose("Installing {Id} ({Version})...", x.PackageId, x.Version));

        projectFile.WriteAllText(content);
        var dotnet = ToolResolver.GetEnvironmentOrPathTool("dotnet");
        dotnet.Invoke($"restore", workingDirectory: projectFile.Parent, logInvocation: false, logOutput: false);
    }

    private static void InstallNpmPackages(IReadOnlyCollection<NpmPackageRequirement> requirements, IFalloutBuild build)
    {
        if (requirements.Count == 0)
        {
            return;
        }

        var packageJsonFile = build.TemporaryDirectory / "package.json";
        NpmToolPathResolver.NpmPackageJsonFile = packageJsonFile;

        var packages = requirements.OrderBy(x => x.PackageId).ToList();

        var content = $$"""
                        {
                          "dependencies": {
                        {{packages.Select(x => $"""    "{x.PackageId}": "{x.Version}",""").JoinNewLine().TrimEnd(',')}}
                          }
                        }
                        """;

        if (packageJsonFile.Exists() && packageJsonFile.ReadAllText().StartsWith(content))
        {
            return;
        }

        Log.Information("Installing NPM packages...");
        packages.ForEach(x => Log.Verbose("Installing {Id} ({Version})...", x.PackageId, x.Version));

        packageJsonFile.WriteAllText(content);
        var npm = ToolResolver.GetEnvironmentOrPathTool("npm");
        npm.Invoke("install", workingDirectory: packageJsonFile.Parent, logInvocation: false, logOutput: false);
    }

    private static void InstallAptGetPackages(IReadOnlyCollection<AptGetPackageRequirement> requirements, IFalloutBuild build)
    {
        if (requirements.Count == 0)
        {
            return;
        }

        var packages = requirements.OrderBy(x => x.PackageId).ToList();
        Assert.True(EnvironmentInfo.IsLinux, "AptGet is only available on Linux");

        var installScript = build.TemporaryDirectory / "apt-get.sh";

        var content = $"""
                       apt-get update
                       apt-get install -y \
                       {packages.Select(x => $"  {x.PackageId} \\").JoinNewLine().TrimEnd("\\")}
                       """;

        if (installScript.Exists() && installScript.ReadAllText().StartsWith(content))
        {
            return;
        }

        Log.Information("Installing AptGet packages...");
        packages.ForEach(x => Log.Verbose("Installing {Id}...", x.PackageId));

        installScript.WriteAllText(content);
        ProcessTasks.StartShell($"sudo {installScript}", logInvocation: false, logOutput: false).AssertZeroExitCode();
    }
}
