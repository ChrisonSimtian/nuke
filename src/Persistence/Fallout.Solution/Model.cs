using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Persistence.Solution.Model;
using Fallout.Persistence.Solution.Serializer;

namespace Fallout.Solutions;

public interface IProjectContainer
{
    IProjectContainer Parent { get; }

    IReadOnlyCollection<Project> Projects { get; }

    IReadOnlyCollection<SolutionFolder> SolutionFolders { get; }
}

public static class ProjectContainerExtensions
{
    public static SolutionFolder GetSolutionFolder(this IProjectContainer container, string name)
    {
        return container.SolutionFolders.SingleOrDefault(x => name.Equals(x.Name, StringComparison.Ordinal));
    }

    /// <summary>
    /// Gets a project by its name.
    /// </summary>
    public static Project GetProject(this IProjectContainer container, string name)
    {
        return container.Projects.SingleOrDefault(x => name.Equals(x.Name, StringComparison.Ordinal));
    }
}

public class Solution(SolutionModel model, AbsolutePath path = null) : IProjectContainer, IAbsolutePathHolder
{
    private ConcurrentDictionary<object, object> wrappers { get; } = new();

    internal T GetOrCreate<T>(object model)
    {
        lock (wrappers)
        {
            return (T)wrappers.GetOrAdd(model, x => x switch
            {
                SolutionProjectModel project => new Project(project, this),
                SolutionFolderModel folder => new SolutionFolder(folder, this),
                _ => throw new ArgumentOutOfRangeException(nameof(x), x, null)
            });
        }
    }

    public SolutionModel GetModel() => model;

    public AbsolutePath Path { get; set; } = path;

    public string Name => Path?.NameWithoutExtension;

    public string FileName => Path?.Name;

    public AbsolutePath Directory => Path?.Parent;

    public IReadOnlyCollection<Project> AllProjects => model.SolutionProjects.Select(GetOrCreate<Project>).ToList();

    public IReadOnlyCollection<SolutionFolder> AllSolutionFolders =>
        model.SolutionFolders.Select(GetOrCreate<SolutionFolder>).ToList();

    IProjectContainer IProjectContainer.Parent => null;

    public IReadOnlyCollection<Project> Projects => AllProjects.Where(x => x.Parent == this).ToList();

    public IReadOnlyCollection<SolutionFolder> SolutionFolders => AllSolutionFolders.Where(x => x.Parent == this).ToList();

    public static implicit operator string(Solution solution) => solution.Path;
    public static implicit operator AbsolutePath(Solution solution) => solution.Path;

    public IEnumerable<Project> GetAllProjects(string wildcardPattern)
    {
        wildcardPattern = $"^{wildcardPattern}$";
        var regex = new Regex(wildcardPattern
            .Replace(".", "\\.")
            .Replace("*", ".*"));

        return AllProjects.Where(x => regex.IsMatch(x.Name));
    }

    public void Save(AbsolutePath path = null)
    {
        Path = (path ?? Path).NotNull();
        var serializer = SolutionSerializers.GetSerializerByMoniker(Path).NotNull();
        AsyncHelper.RunSync(() => serializer.SaveAsync(Path, model, CancellationToken.None));
    }
}

public class SolutionItem(SolutionItemModel model, Solution solution)
{
    public string Name => model.ActualDisplayName;

    public Solution Solution => solution;

    public IProjectContainer Parent => (IProjectContainer)model.Parent?.Apply(solution.GetOrCreate<SolutionFolder>) ?? solution;

    public override string ToString() => model.ActualDisplayName;
}

public class SolutionFolder(SolutionFolderModel model, Solution solution) : SolutionItem(model, solution), IProjectContainer
{
    public SolutionFolderModel GetModel() => model;

    public IReadOnlyCollection<Project> Projects => Solution.AllProjects.Where(x => x.Parent == this).ToList();

    public IReadOnlyCollection<SolutionFolder> SolutionFolders =>
        Solution.AllSolutionFolders.Where(x => x.Parent == this).ToList();
}

public class Project(SolutionProjectModel model, Solution solution) : SolutionItem(model, solution), IAbsolutePathHolder
{
    public SolutionProjectModel GetModel() => model;

    public string RelativePath => model.FilePath;

    public AbsolutePath Path => Solution.Directory.NotNull() / model.FilePath;

    public string FileName => System.IO.Path.GetFileName(RelativePath);

    public AbsolutePath Directory => Path?.Parent;

    public static implicit operator string(Project project) => project.Path;
    public static implicit operator AbsolutePath(Project project) => project.Path;
}
