// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Fallout.Persistence.Solution.Serializer.SlnV12;
using Fallout.Persistence.Solution.Serializer.Xml;

namespace Fallout.Persistence.Solution.Model;

/// <summary>
/// Represents a solution.
/// This contains a list of projects and folders and the information
/// required to build the solution in different configurations.
/// </summary>
public sealed class SolutionModel : PropertyContainerModel
{
#if NETFRAMEWORK || NETSTANDARD
    private const string InvalidNameChars = @"?:\/*""<>|";
#else
    private static readonly SearchValues<char> InvalidNameChars = SearchValues.Create(@"?:\/*""<>|");
#endif

    private readonly VisualStudioProperties visualStudioProperties;
    private readonly Dictionary<Guid, SolutionItemModel> solutionItemsById;
    private readonly List<SolutionItemModel> solutionItems;
    private readonly List<SolutionProjectModel> solutionProjects;
    private readonly List<SolutionFolderModel> solutionFolders;
    private readonly List<string> solutionBuildTypes;
    private readonly List<string> solutionPlatforms;
    private readonly List<ProjectType> projectTypes;
    private ProjectTypeTable? projectTypeTable;
    private bool suspendProjectValidation;

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionModel"/> class.
    /// Creates a new empty solution.
    /// </summary>
    public SolutionModel()
    {
        visualStudioProperties = new VisualStudioProperties(this);
        StringTable = new StringTable().WithSolutionConstants();
        solutionItemsById = [];
        solutionItems = [];
        solutionProjects = [];
        solutionFolders = [];
        solutionBuildTypes = [];
        solutionPlatforms = [];
        projectTypes = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionModel"/> class.
    /// Creates a deep copy of the solution.
    /// </summary>
    /// <param name="solutionModel">Instance of the <see cref="SolutionModel"/> to copy.</param>
    public SolutionModel(SolutionModel solutionModel)
        : base(solutionModel ?? throw new ArgumentNullException(nameof(solutionModel)))
    {
        visualStudioProperties = new VisualStudioProperties(this);
        StringTable = solutionModel.StringTable;
        int itemCount = solutionModel.solutionItems.Count;
        int folderCount = solutionModel.solutionItems.Count(x => x is SolutionFolderModel);
        solutionItems = new List<SolutionItemModel>(itemCount);
        solutionItemsById = new Dictionary<Guid, SolutionItemModel>(itemCount);
        solutionFolders = new List<SolutionFolderModel>(folderCount);
        solutionProjects = new List<SolutionProjectModel>(itemCount - folderCount);
        foreach (SolutionItemModel item in solutionModel.solutionItems)
        {
            SolutionItemModel newItem = item switch
            {
                SolutionFolderModel folder => new SolutionFolderModel(this, folder),
                SolutionProjectModel project => new SolutionProjectModel(this, project),
                _ => throw new InvalidOperationException(),
            };

            solutionItems.Add(newItem);
            solutionFolders.AddIfNotNull(newItem as SolutionFolderModel);
            solutionProjects.AddIfNotNull(newItem as SolutionProjectModel);
            solutionItemsById[newItem.Id] = newItem;
        }

        // Replace the shallow-parent models with the new folders.
        foreach (SolutionItemModel item in solutionItems)
        {
            if (item.Parent is not null)
            {
                item.MoveToFolder(FindFolder(item.Parent.ItemRef) ?? throw new InvalidOperationException());
            }
        }

        Description = solutionModel.Description;
        solutionBuildTypes = [.. solutionModel.solutionBuildTypes];
        solutionPlatforms = [.. solutionModel.solutionPlatforms];
        projectTypes = [.. solutionModel.projectTypes];
    }

    /// <summary>
    /// Gets or sets the string table used by the solution model.
    /// This is used to reduce string duplication.
    /// </summary>
    public StringTable StringTable { get; set; }

    /// <summary>
    /// Gets or sets the serializer extension model that can be used to
    /// get or specify settings specific to a serializer.
    /// This can be created by a serializer.
    /// </summary>
    public ISerializerModelExtension? SerializerExtension { get; set; }

    /// <summary>
    /// Gets or sets a user visible comment describing the solution.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets the list of solution items in the solution.
    /// This is all of the solution folders and projects in the solution.
    /// </summary>
    public IReadOnlyList<SolutionItemModel> SolutionItems => solutionItems;

    /// <summary>
    /// Gets the list of projects in the solution.
    /// </summary>
    public IReadOnlyList<SolutionProjectModel> SolutionProjects => solutionProjects;

    /// <summary>
    /// Gets the list of solution folders in the solution.
    /// </summary>
    public IReadOnlyList<SolutionFolderModel> SolutionFolders => solutionFolders;

    /// <summary>
    /// Gets the list of build types in the solution. (e.g Debug/Release).
    /// </summary>
    public IReadOnlyList<string> BuildTypes => solutionBuildTypes;

    /// <summary>
    /// Gets the list of platforms in the solution. (e.g. x64/Any CPU).
    /// </summary>
    public IReadOnlyList<string> Platforms => solutionPlatforms;

    /// <summary>
    /// Gets or sets the list of project types in the solution.
    /// </summary>
    /// <remarks>
    /// These can be defined to provide information about a project type used in the solution.
    /// It can be associated with a file extension or a friendly name.
    /// It contains the project type id and and default configuration mapping rules.
    /// </remarks>
    public IReadOnlyList<ProjectType> ProjectTypes
    {
        get => projectTypes;
        set
        {
            projectTypes.Clear();
            projectTypes.AddRange(value);
            projectTypeTable = null;
        }
    }

    /// <summary>
    /// Gets a helper to get and set Visual Studio specific properties.
    /// </summary>
    /// <returns>A helper to get and set Visual Studio properties.</returns>
    public ref readonly VisualStudioProperties VisualStudioProperties => ref visualStudioProperties;

    internal ProjectTypeTable ProjectTypeTable => projectTypeTable ??= new ProjectTypeTable(projectTypes);

    /// <summary>
    /// Gets or adds a solution folder to the solution.
    /// </summary>
    /// <param name="path">
    /// The full path of the solution folder. The path must start and end with a forward slash, with subfolders separated by forward slashes.
    /// Folders will be created as needed.
    /// </param>
    /// <returns>The model for the new folder.</returns>
    public SolutionFolderModel AddFolder(string path)
    {
        Argument.ThrowIfNullOrEmpty(path, nameof(path));
        if (!path.StartsWith('/') || !path.EndsWith('/'))
        {
            throw new SolutionArgumentException(string.Format(Errors.InvalidFolderPath_Args1, path), nameof(path),
                SolutionErrorType.InvalidFolderPath);
        }

        SolutionFolderModel? existingFolder = FindFolder(path);
        if (existingFolder is not null)
        {
            return existingFolder;
        }

        // Process the folder name
        StringSpan folderPath = path.AsSpan(0, path.Length - 1);

        int lastSlash = folderPath.LastIndexOf('/');
        string? parentItemRef = lastSlash > 0 ? folderPath.Slice(0, lastSlash + 1).ToString() : null;
        StringSpan newName = lastSlash > 0 ? folderPath.Slice(lastSlash + 1) : folderPath.Slice(1);

        SolutionFolderModel folder = AddFolder(newName, parentItemRef);

        // Ensure the project type is in the project type table, if it is not already.
        solutionItemsById[folder.Id] = folder;

        return folder;
    }

    /// <summary>
    /// Adds a project to the solution.
    /// </summary>
    /// <param name="filePath">The relative path to the project.</param>
    /// <param name="projectTypeName">The project type name of the project.
    /// This can be null if the project type can be determined from the project's file extension.
    /// </param>
    /// <param name="folder">The parent solution folder to add the project to.</param>
    /// <returns>The model for the new project.</returns>
    public SolutionProjectModel AddProject(string filePath, string? projectTypeName = null, SolutionFolderModel? folder = null)
    {
        Argument.ThrowIfNullOrEmpty(filePath, nameof(filePath));
        ValidateInModel(folder);

        Guid projectTypeId =
            Guid.TryParse(projectTypeName, out Guid projectTypeGuid)
                ? projectTypeGuid
                : ProjectTypeTable.GetProjectTypeId(projectTypeName, Path.GetExtension(filePath.AsSpan())) ??
                  throw new SolutionArgumentException(string.Format(Errors.InvalidProjectTypeReference_Args1, projectTypeName),
                      nameof(projectTypeName), SolutionErrorType.InvalidProjectTypeReference);

        return AddProject(filePath, projectTypeName ?? string.Empty, projectTypeId, folder);
    }

    /// <summary>
    /// Remove a solution folder from the solution model. This includes any child folders and projects.
    /// </summary>
    /// <param name="folder">The folder to remove.</param>
    /// <returns><see langword="true"/> if the folder was found and removed.</returns>
    public bool RemoveFolder(SolutionFolderModel folder)
    {
        Argument.ThrowIfNull(folder, nameof(folder));
        ValidateInModel(folder);

        return RemoveFolder(folder, SolutionItems.ToArray());
    }

    /// <summary>
    /// Remove a project from the solution model.
    /// </summary>
    /// <param name="project">The item to remove.</param>
    /// <returns><see langword="true"/> if the project was found and removed.</returns>
    public bool RemoveProject(SolutionProjectModel project)
    {
        Argument.ThrowIfNull(project, nameof(project));
        ValidateInModel(project);
        _ = solutionProjects.Remove(project);

        // Remove any dependencies to this project.
        foreach (SolutionProjectModel existingProject in SolutionProjects)
        {
            _ = existingProject.RemoveDependency(project);
        }

        return RemoveItem(project);
    }

    /// <summary>
    /// Adds a build type to the solution.
    /// </summary>
    /// <param name="buildType">The build type to add.</param>
    public void AddBuildType(string buildType)
    {
        Argument.ThrowIfNullOrEmpty(buildType, nameof(buildType));

        ValidateName(buildType.AsSpan());

        if (!solutionBuildTypes.Contains(buildType, StringComparer.OrdinalIgnoreCase))
        {
            buildType = StringTable.GetString(buildType);
            solutionBuildTypes.Add(buildType);
        }
    }

    /// <summary>
    /// Removes a build type from the solution.
    /// </summary>
    /// <param name="buildType">The build type to remove.</param>
    /// <returns><see langword="true"/> if the build type was found and removed.</returns>
    public bool RemoveBuildType(string buildType)
    {
        Argument.ThrowIfNullOrEmpty(buildType, nameof(buildType));
        return solutionBuildTypes.Remove(buildType);
    }

    /// <summary>
    /// Adds a platform to the solution.
    /// </summary>
    /// <param name="platform">The platform to add.</param>
    public void AddPlatform(string platform)
    {
        Argument.ThrowIfNullOrEmpty(platform, nameof(platform));

        ValidateName(platform.AsSpan());

        if (!solutionPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
        {
            platform = StringTable.GetString(platform);
            solutionPlatforms.Add(platform);
        }
    }

    /// <summary>
    /// Removes a platform from the solution.
    /// </summary>
    /// <param name="platform">The platform to remove.</param>
    /// <returns><see langword="true"/> if the platform was found and removed.</returns>
    public bool RemovePlatform(string platform)
    {
        Argument.ThrowIfNullOrEmpty(platform, nameof(platform));
        return solutionPlatforms.Remove(platform);
    }

    /// <summary>
    /// Find a solution folder or project by id.
    /// </summary>
    /// <param name="id">The id of the item to look for.</param>
    /// <returns>The item if found.</returns>
    public SolutionItemModel? FindItemById(Guid id)
    {
        return solutionItemsById.TryGetValue(id, out SolutionItemModel? item) ? item : null;
    }

    /// <summary>
    /// Find a solution folder by unique path.
    /// </summary>
    /// <param name="path">The folder path to look for.</param>
    /// <returns>The folder if found.</returns>
    public SolutionFolderModel? FindFolder(string path)
    {
        Argument.ThrowIfNullOrEmpty(path, nameof(path));
        if (!path.StartsWith('/') || !path.EndsWith('/'))
        {
            throw new SolutionArgumentException(string.Format(Errors.InvalidFolderPath_Args1, path), nameof(path),
                SolutionErrorType.InvalidFolderPath);
        }

        return solutionFolders.FindByItemRef(path);
    }

    /// <summary>
    /// Find a solution project by path.
    /// </summary>
    /// <param name="path">The project path to look for.</param>
    /// <returns>The project if found.</returns>
    public SolutionProjectModel? FindProject(string path)
    {
        Argument.ThrowIfNullOrEmpty(path, nameof(path));

        return solutionProjects.FindByItemRef(path);
    }

    /// <summary>
    /// Regenerates all of the project configuration rules. If rules are added
    /// to project types, or possible redundant rules are added to projects this
    /// can be called to recalculate the rules.
    /// </summary>
    public void DistillProjectConfigurations()
    {
        SolutionConfigurationMap cfgMap = new(this);

        // Load all of the current rules for the project and recalculate a new
        // set of configuration rules.
        cfgMap.DistillProjectConfigurations();
    }

    // Throws if the solution folder or project name is not valid.
    internal static void ValidateName(StringSpan name)
    {
        if (name.IsEmpty || name.IsWhiteSpace())
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (name.Length > 260)
        {
            throw new ArgumentOutOfRangeException(nameof(name));
        }

        foreach (char c in name)
        {
            if (char.IsControl(c) || InvalidNameChars.Contains(c))
            {
                throw new SolutionArgumentException(Errors.InvalidName, nameof(name), SolutionErrorType.InvalidName);
            }
        }

        if (IsDosWord(name))
        {
            throw new SolutionArgumentException(Errors.InvalidName, nameof(name), SolutionErrorType.InvalidName);
        }

        static bool IsDosWord(scoped StringSpan name)
        {
            if (name is "." or "..")
            {
                return true;
            }

            // Only care about part before extension
            name = Path.GetFileNameWithoutExtension(name);
            switch (name.Length)
            {
                case 3:
                    return
                        name.EqualsOrdinalIgnoreCase("nul") ||
                        name.EqualsOrdinalIgnoreCase("con") ||
                        name.EqualsOrdinalIgnoreCase("aux") ||
                        name.EqualsOrdinalIgnoreCase("prn");

                case 4:
                    // disallow com? and lpt? where ? can be any number from 1 to 9
                    name = name.TrimEnd("123456789".AsSpan());
                    return name.EqualsOrdinalIgnoreCase("com") || name.EqualsOrdinalIgnoreCase("lpt");

                case 6:
                    return name.EqualsOrdinalIgnoreCase("clock$");

                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Remove any unneccessary VS properties from the model.
    /// This removes project and solution guid ids plus any properties removed by <see cref="RemoveObsoleteProperties"/>.
    /// </summary>
    internal void TrimVisualStudioProperties()
    {
        // Set project id to default.
        foreach (SolutionItemModel item in SolutionItems)
        {
            item.Id = Guid.Empty;
        }

        VisualStudioProperties.SolutionId = null;
        visualStudioProperties.OpenWith = null;

        RemoveObsoleteProperties();
    }

    /// <summary>
    /// Remove any obsolete VS properties from the model.
    /// This removes minimum version older than Dev17, shared project properties, and
    /// removes any CPS project types ids that were accidentally used in .sln files.
    /// </summary>
    internal void RemoveObsoleteProperties()
    {
        // Remove CPS project type ids.
        // This explicitly checks for the built-in CPS type names, so a slnx file can still
        // use the CPS project ids by creating a custom ProjectType.
        foreach (SolutionProjectModel project in SolutionProjects)
        {
            // Remove CPS project type that were used by .sln for many years due to a bug.
            if (StringComparer.OrdinalIgnoreCase.Equals(project.Type, "Common C#"))
            {
                project.Type = "C#";
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals(project.Type, "Common VB"))
            {
                project.Type = "VB";
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals(project.Type, "Common F#"))
            {
                project.Type = "F#";
            }
        }

        _ = RemoveProperties(SectionName.SharedMSBuildProjectFiles);

        VisualStudioProperties vsProperties = VisualStudioProperties;
        vsProperties.Version = null;
#pragma warning disable CS0618 // Type or member is obsolete
        vsProperties.HideSolutionNode = null;
#pragma warning restore CS0618 // Type or member is obsolete

        if (vsProperties.MinimumVersion is not null &&
            vsProperties.MinimumVersion < new Version(18, 0))
        {
            vsProperties.MinimumVersion = null;
        }
    }

    internal SolutionProjectModel AddProject(string filePath, string projectTypeName, Guid projectTypeId,
        SolutionFolderModel? folder)
    {
        SolutionProjectModel project = new(this, filePath, projectTypeId, projectTypeName, folder);

        // Project is already in the solution.
        if (FindProject(project.FilePath) is not null)
        {
            throw new SolutionArgumentException(string.Format(Errors.DuplicateProjectPath_Arg1, project.ItemRef),
                nameof(filePath), SolutionErrorType.DuplicateProjectPath);
        }

        ValidateProjectName(project);

        solutionProjects.Add(project);
        solutionItems.Add(project);

        // Ensure the project type is in the project type table, if it is not already.
        solutionItemsById[project.Id] = project;

        return project;
    }

    /// <summary>
    /// Always adds a solution folder to the solution.
    /// </summary>
    /// <param name="name">The name of the new solution folder.</param>
    /// <returns>The model for the new folder.</returns>
    internal SolutionFolderModel CreateFolder(string name)
    {
        Argument.ThrowIfNullOrEmpty(name, nameof(name));

        // Validate the name.
        ValidateName(name.AsSpan());

        return AddFolder(name.AsSpan(), parentItemRef: null);
    }

    /// <summary>
    /// Suspends project validation while adding multiple projects without
    /// solution folder information.
    /// This must be called in a using block to properly resume validation.
    /// </summary>
    /// <returns>Use to scope suspension, call <see cref="IDisposable.Dispose"/> to reenable validation.</returns>
    internal IDisposable SuspendProjectValidation()
    {
        suspendProjectValidation = true;
        return new ValidationScope(this);
    }

    internal void ResumeProjectValidation()
    {
        suspendProjectValidation = false;
        foreach (SolutionProjectModel project in solutionProjects)
        {
            ValidateProjectName(project);
        }
    }

    internal void ThrowIfProjectValidationSuspended()
    {
        if (suspendProjectValidation)
        {
            throw new InvalidOperationException();
        }
    }

    internal bool IsConfigurationImplicit()
    {
        return
            IsBuildTypeImplicit() &&
            IsPlatformImplicit() &&
            ProjectTypeTable.ProjectTypes.Count == 0;
    }

    internal bool IsBuildTypeImplicit()
    {
        // Has 0 build types, or just Debug/Release.
        return
            BuildTypes.Count == 0 ||
            (BuildTypes.Count == 2 &&
             BuildTypes.Contains(BuildTypeNames.Debug) &&
             BuildTypes.Contains(BuildTypeNames.Release));
    }

    internal bool IsPlatformImplicit()
    {
        return
            Platforms.Count == 0 ||
            (Platforms.Count == 1 &&
             Platforms[0] == PlatformNames.AnySpaceCPU);
    }

    internal void OnUpdateId(SolutionItemModel solutionItemModel, Guid? oldId)
    {
        if (oldId is not null)
        {
            _ = solutionItemsById.Remove(oldId.Value);
        }

        solutionItemsById[solutionItemModel.Id] = solutionItemModel;
    }

    internal void ValidateProjectName(SolutionProjectModel project)
    {
        if (suspendProjectValidation)
        {
            return;
        }

        string displayName = project.ActualDisplayName;
        string folderPath = project.Parent?.Path ?? "Root";

        foreach (SolutionProjectModel existingProject in SolutionProjects)
        {
            if (!ReferenceEquals(existingProject.Parent, project.Parent) || ReferenceEquals(existingProject, project))
            {
                continue;
            }

            if (existingProject.ActualDisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase))
            {
                throw new SolutionArgumentException(string.Format(Errors.DuplicateProjectName_Arg2, displayName, folderPath),
                    SolutionErrorType.DuplicateProjectName);
            }
        }
    }

    internal void ValidateInModel(SolutionItemModel? item)
    {
        if (item is not null && item.Solution != this)
        {
            throw new SolutionArgumentException(Errors.InvalidModelItem, nameof(item), SolutionErrorType.InvalidModelItem);
        }
    }

    // Moves the project to the first position in the solution so that it is used as the default startup project.
    internal void MoveProjectFirst(SolutionProjectModel projectModel)
    {
        int projectIndex = solutionProjects.IndexOf(projectModel);
        if (projectIndex > 0)
        {
            (solutionProjects[projectIndex], solutionProjects[0]) = (solutionProjects[0], solutionProjects[projectIndex]);
        }

        int itemIndex = solutionItems.IndexOf(projectModel);
        if (itemIndex > 0)
        {
            (solutionItems[itemIndex], solutionItems[0]) = (solutionItems[0], solutionItems[itemIndex]);
        }
    }

    // Creates a new solution folder. Assumes name has been validated and deduplicated.
    private SolutionFolderModel AddFolder(StringSpan name, string? parentItemRef)
    {
        // Validate the name before creating any parent nodes.
        ValidateName(name);

        SolutionFolderModel? parentFolder =
            parentItemRef is null ? null : FindFolder(parentItemRef) ?? AddFolder(parentItemRef);

        SolutionFolderModel folder = new(this, StringTable.GetString(name), parentFolder);

        solutionFolders.Add(folder);
        solutionItems.Add(folder);

        return folder;
    }

    // Remove a solution folder from the solution model. This includes any child folders and projects.
    // Recursive call reuses the solutionItems array to avoid creating a new array for each recursive call.
    private bool RemoveFolder(SolutionFolderModel folder, SolutionItemModel[] solutionItems)
    {
        _ = solutionFolders.Remove(folder);

        // Remove any children of this folder.
        foreach (SolutionItemModel existingItem in solutionItems)
        {
            if (ReferenceEquals(existingItem.Parent, folder))
            {
                _ = existingItem switch
                {
                    SolutionFolderModel childFolder => RemoveFolder(childFolder, solutionItems),
                    SolutionProjectModel childProject => RemoveProject(childProject),
                    _ => throw new InvalidOperationException(),
                };
            }
        }

        return RemoveItem(folder);
    }

    private bool RemoveItem(SolutionItemModel item)
    {
        _ = solutionItemsById.Remove(item.Id);
        return solutionItems.Remove(item);
    }

    private sealed class ValidationScope(SolutionModel model) : IDisposable
    {
        public void Dispose()
        {
            model.ResumeProjectValidation();
        }
    }
}
