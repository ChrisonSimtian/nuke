// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Fallout.Persistence.Solution.Utilities;

namespace Fallout.Persistence.Solution.Model;

/// <summary>
/// Represents a project in the solution model.
/// </summary>
public sealed class SolutionProjectModel : SolutionItemModel
{
    private Guid typeId;
    private string type;
    private string filePath;
    private List<SolutionProjectModel>? dependencies;
    private List<ConfigurationRule>? projectConfigurationRules;

    internal SolutionProjectModel(SolutionModel solutionModel, string filePath, Guid typeId, string type,
        SolutionFolderModel? parent)
        : base(solutionModel, parent)
    {
        this.typeId = typeId;
        this.type = type;
        FilePath = filePath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionProjectModel"/> class.
    /// Copy constructor.
    /// </summary>
    /// <param name="solutionModel">The new solution model parent.</param>
    /// <param name="projectModel">The project model to copy.</param>
    internal SolutionProjectModel(SolutionModel solutionModel, SolutionProjectModel projectModel)
        : base(solutionModel, projectModel)
    {
        typeId = projectModel.TypeId;
        type = projectModel.Type;
        FilePath = projectModel.FilePath;
        DisplayName = projectModel.DisplayName;
        if (projectModel.dependencies is not null)
        {
            dependencies = [.. projectModel.dependencies];
        }

        if (projectModel.projectConfigurationRules is not null)
        {
            projectConfigurationRules = [.. projectModel.projectConfigurationRules];
        }
    }

    /// <inheritdoc/>
    public override Guid TypeId => typeId;

    /// <summary>
    /// Gets or sets the project type.
    /// This can be empty if the project file extension is known.
    /// This can be a type name of a defined project type.
    /// This can be a project type id (Guid).
    /// </summary>
    public string Type
    {
        get => type;

        set
        {
            // Attempt to resolve the type name,
            if (Guid.TryParse(value, out Guid typeId))
            {
                if (typeId == Guid.Empty)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                // Type looks like a project type id and try to lookup the type name.
                this.typeId = typeId;
                type = Solution.ProjectTypeTable.GetConciseType(this.typeId, string.Empty, Extension);
            }
            else
            {
                // Type looks like a name, lookup the project type id and simplify name if possible.
                this.typeId = Solution.ProjectTypeTable.GetProjectTypeId(value, Extension.AsSpan()) ?? Guid.Empty;
                type = Solution.ProjectTypeTable.GetConciseType(this.typeId, value, Extension);
            }
        }
    }

    /// <summary>
    /// Gets or sets the path to the project file.
    /// </summary>
    public string FilePath
    {
        get => filePath;

        [MemberNotNull(nameof(filePath), nameof(Extension))]
        set
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(filePath, value) || Extension is null)
            {
                if (Solution.FindProject(value) is not null)
                {
                    throw new SolutionArgumentException(string.Format(Errors.DuplicateItemRef_Args2, value, "Project"),
                        nameof(value), SolutionErrorType.DuplicateItemRef);
                }

                string oldPath = filePath!;
                string oldExtension = Extension!;
                try
                {
                    filePath = value;
                    Extension = Solution.StringTable.GetString(PathExtensions.GetExtension(value));
                    OnItemRefChanged();

                    Solution.ValidateProjectName(this);
                }
                catch (Exception)
                {
                    filePath = oldPath;
                    Extension = oldExtension;
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Gets the file extension of the project file.
    /// </summary>
    /// <remarks>
    /// Some project types, like web site projects, do not have a file extension.
    /// </remarks>
    public string Extension { get; private set; }

    /// <summary>
    /// Gets or sets the display name of the project.
    /// </summary>
    /// <remarks>
    /// This will be ignored if the project path is a file name.
    /// </remarks>
    public string? DisplayName { get; set; }

    /// <inheritdoc/>
    public override string ActualDisplayName
    {
        get
        {
            // If the project has a file name, use that as the display name.
            // This historically takes precedence over the DisplayName property.
            StringSpan fileName = PathExtensions.GetStandardDisplayName(FilePath.AsSpan());
            if (fileName.IsEmpty)
            {
                return DisplayName ?? string.Empty;
            }

            return Solution.StringTable.GetString(fileName);
        }
    }

    /// <summary>
    /// Gets the list of the dependencies of this project.
    /// </summary>
    /// <remarks>
    /// Project to project dependencies are normally stored in the project file itself,
    /// this is used for solution level dependencies.
    /// </remarks>
    public IReadOnlyList<SolutionProjectModel>? Dependencies => dependencies;

    /// <summary>
    /// Gets or sets a list of configuration rules for this project.
    /// These rules can be simplified to essential rules by calling <see cref="SolutionModel.DistillProjectConfigurations"/>.
    /// </summary>
    public IReadOnlyList<ConfigurationRule>? ProjectConfigurationRules
    {
        get => projectConfigurationRules;
        set => projectConfigurationRules = value is null ? null : [.. value];
    }

    /// <inheritdoc/>
    internal override string ItemRef => FilePath;

    /// <summary>
    /// Gets the project configuration for the given solution configuration.
    /// </summary>
    /// <param name="solutionBuildType">The solution build type. (e.g. Debug).</param>
    /// <param name="solutionPlatform">The solution platform. (e.g. x64).</param>
    /// <returns>
    /// The project configuration for the given solution configuration.
    /// BuildType and Platform will be null if the configuration information is missing.
    /// </returns>
    public (string? BuildType, string? Platform, bool Build, bool Deploy) GetProjectConfiguration(string solutionBuildType,
        string solutionPlatform)
    {
        ConfigurationRuleFollower projectTypeRules = Solution.ProjectTypeTable.GetProjectConfigurationRules(this);

        string? buildType =
            MissingToNull(projectTypeRules.GetProjectBuildType(solutionBuildType, solutionPlatform) ?? solutionBuildType);

        string? platform =
            MissingToNull(projectTypeRules.GetProjectPlatform(solutionBuildType, solutionPlatform) ?? solutionPlatform);

        bool build = projectTypeRules.GetIsBuildable(solutionBuildType, solutionPlatform) ?? true;
        bool deploy = projectTypeRules.GetIsDeployable(solutionBuildType, solutionPlatform) ?? false;

        return (buildType, platform, build, deploy);

        static string? MissingToNull(string value) => value == BuildTypeNames.Missing ? null : value;
    }

    /// <summary>
    /// Adds a dependency to this project.
    /// </summary>
    /// <param name="dependency">The dependency to add.</param>
    public void AddDependency(SolutionProjectModel dependency)
    {
        Argument.ThrowIfNull(dependency, nameof(dependency));
        Solution.ValidateInModel(dependency);

        if (ReferenceEquals(dependency, this))
        {
            throw new SolutionArgumentException(string.Format(Errors.InvalidLoop_Args1, dependency.ItemRef), nameof(dependency),
                SolutionErrorType.InvalidLoop);
        }

        dependencies ??= [];

        if (!dependencies.Contains(dependency))
        {
            dependencies.Add(dependency);
        }
    }

    /// <summary>
    /// Removes a dependency from this project.
    /// </summary>
    /// <param name="dependency">The dependency to remove.</param>
    /// <returns><see langword="true"/> if the dependency was found and removed.</returns>
    public bool RemoveDependency(SolutionProjectModel dependency)
    {
        Argument.ThrowIfNull(dependency, nameof(dependency));
        Solution.ValidateInModel(dependency);

        return
            dependencies is not null &&
            dependencies.Remove(dependency);
    }

    /// <summary>
    /// Adds a configuration rule to this project.
    /// </summary>
    /// <param name="rule">The rule to add.</param>
    public void AddProjectConfigurationRule(ConfigurationRule rule)
    {
        Argument.ThrowIfNull(rule, nameof(rule));
        projectConfigurationRules ??= [];
        projectConfigurationRules.Add(rule);
    }

    private protected override Guid GetDefaultId()
    {
        return DefaultIdGenerator.CreateIdFrom(FilePath);
    }
}
