namespace Fallout.MSBuildTasks.Engine;

/// <summary>A tool file discovered inside a NuGet package, plus the MSBuild item metadata it needs.</summary>
public sealed record PackagedToolFile(string File, string BuildAction, string PackagePath);
