namespace Fallout.Solutions;

public static partial class ProjectExtensions
{
    /// <summary>
    /// Loads the project through the <a href="https://github.com/dotnet/msbuild">Microsoft Build Engine</a>.
    /// </summary>
    public static Microsoft.Build.Evaluation.Project GetMSBuildProject(
        this Project project,
        string configuration = null,
        string targetFramework = null)
    {
        return ProjectModelTasks.ParseProject(project.Path, configuration, targetFramework);
    }
}
