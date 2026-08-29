namespace Fallout.Common.CI.AzurePipelines;

// https://docs.microsoft.com/en-us/azure/devops/pipelines/release/caching?view=azure-devops
public static class AzurePipelinesCachePaths
{
    public const string Nuke = ".fallout/temp";
    public const string NuGet = "~/.nuget/packages";
    public const string Npm = "~/.npm";
    public const string Gradle = "~/.gradle";
    public const string Docker = "~/docker";
}
