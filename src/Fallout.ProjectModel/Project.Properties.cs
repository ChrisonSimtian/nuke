using Fallout.Common.Utilities;

namespace Fallout.Solutions;

public static partial class ProjectExtensions
{
    /// <summary>
    /// Returns the value of a project property, including <a href="https://docs.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-properties">
    /// common MSBuild project properties</a> and custom properties defined in
    /// <a href="https://docs.microsoft.com/en-us/visualstudio/msbuild/propertygroup-element-msbuild"><c>PropertyGroup</c> elements</a>.
    /// </summary>
    public static string GetProperty(this Project project, string propertyName)
    {
        var property = project.GetMSBuildProject().GetProperty(propertyName);
        return property?.EvaluatedValue;
    }

    /// <summary>
    /// Returns the converted value of a project property, including <a href="https://docs.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-properties">
    /// common MSBuild project properties</a> and custom properties defined in
    /// <a href="https://docs.microsoft.com/en-us/visualstudio/msbuild/propertygroup-element-msbuild"><c>PropertyGroup</c> elements</a>.
    /// </summary>
    public static T GetProperty<T>(this Project project, string propertyName)
    {
        return ReflectionUtility.Convert<T>(project.GetProperty(propertyName));
    }
}
