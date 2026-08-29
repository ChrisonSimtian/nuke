// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Fallout.Persistence.Solution.Model;

/// <summary>
/// Helper to convert full list of solution to project configuration mappings to model rules and vice versa.
/// </summary>
internal sealed partial class SolutionConfigurationMap
{
    private readonly SolutionModel solutionModel;
    private readonly Dictionary<string, int> buildTypesIndex;
    private readonly Dictionary<string, int> platformsIndex;

    private readonly Dictionary<SolutionProjectModel, SolutionToProjectMappings> perProjectCurrent = [];

    private readonly int matrixSize;

    internal SolutionConfigurationMap(SolutionModel solutionModel)
    {
        this.solutionModel = solutionModel;
        buildTypesIndex = new Dictionary<string, int>(solutionModel.BuildTypes.Count);
        for (int i = 0; i < solutionModel.BuildTypes.Count; i++)
        {
            buildTypesIndex.Add(solutionModel.BuildTypes[i], i);
        }

        platformsIndex = new Dictionary<string, int>(solutionModel.Platforms.Count);
        for (int i = 0; i < solutionModel.Platforms.Count; i++)
        {
            platformsIndex.Add(PlatformNames.Canonical(solutionModel.Platforms[i]), i);
        }

        matrixSize = BuildTypesCount * PlatformsCount;
    }

    internal int BuildTypesCount => buildTypesIndex.Count;

    internal int PlatformsCount => platformsIndex.Count;

    internal int GetBuildTypeIndex(string buildType)
    {
        return !string.IsNullOrEmpty(buildType) && buildTypesIndex.TryGetValue(buildType, out int index)
            ? index
            : ScopedRules.All;
    }

    internal int GetPlatformIndex(string platform)
    {
        return !string.IsNullOrEmpty(platform) && platformsIndex.TryGetValue(PlatformNames.Canonical(platform), out int index)
            ? index
            : ScopedRules.All;
    }

    /// <summary>
    /// Used to convert this model to a full list of all solution to project configurations.
    /// </summary>
    internal void GetProjectConfigMap(
        SolutionProjectModel projectModel,
        out SolutionToProjectMappings projectMappings,
        out bool supportsConfigs)
    {
        projectMappings = new SolutionToProjectMappings(this, projectModel, out bool isBuildable);
        supportsConfigs = isBuildable || !projectModel.ProjectConfigurationRules.IsNullOrEmpty();

        foreach (ConfigurationRule rule in projectModel.ProjectConfigurationRules.GetStructEnumerable())
        {
            int buildTypeIndex = GetBuildTypeIndex(rule.SolutionBuildType);
            int platformIndex = GetPlatformIndex(rule.SolutionPlatform);

            if ((!string.IsNullOrEmpty(rule.SolutionBuildType) && buildTypeIndex < 0) ||
                (!string.IsNullOrEmpty(rule.SolutionPlatform) && platformIndex < 0))
            {
                continue;
            }

            ApplyRules(in projectMappings, new ScopedRules(buildTypeIndex, platformIndex, [rule]));
        }
    }

    /// <summary>
    /// This just create a mapping of all solution configurations to indexes.
    /// Solution configurations are every combination of buildType and platform.
    /// </summary>
    internal (string SlnKey, SolutionConfigIndex Index)[] CreateMatrixAnnotation()
    {
        (string SlnKey, SolutionConfigIndex Index)[] ret = new (string SlnKey, SolutionConfigIndex Index)[matrixSize];
        for (int buildTypeIndex = 0; buildTypeIndex < solutionModel.BuildTypes.Count; buildTypeIndex++)
        {
            string buildType = solutionModel.BuildTypes[buildTypeIndex];

            for (int platformIndex = 0; platformIndex < solutionModel.Platforms.Count; platformIndex++)
            {
                string platform = solutionModel.Platforms[platformIndex];

                SolutionConfigIndex idx = new(this, buildTypeIndex, platformIndex);
                ret[idx.MatrixIndex] = ($"{buildType}|{platform}", idx);
            }
        }

        return ret;
    }

    /// <summary>
    /// Interprets all of the current project configurations to a full list of all mappings.
    /// Then recalculates a distilled set of rules for each project.
    /// </summary>
    internal void DistillProjectConfigurations()
    {
        foreach (SolutionProjectModel projectModel in solutionModel.SolutionProjects)
        {
            // Cache list of all project configurations for all solution configuration mappings.
            GetProjectConfigMap(projectModel, out SolutionToProjectMappings mappings, out bool supportsConfigs);
            if (supportsConfigs)
            {
                perProjectCurrent.Add(projectModel, mappings);
            }

            // Converts cached mappings into simpler rules.
            projectModel.ProjectConfigurationRules = CreateProjectRules(projectModel);
        }
    }

    private SolutionConfigIndex ToIndex(int iBuildType, int iPlatform) => new(this, iBuildType, iPlatform);

    // This should only be called from ConfigIndex
    private string BuildTypeFromIndex(SolutionConfigIndex index)
    {
        if (index.MatrixIndex < 0 || index.MatrixIndex >= matrixSize)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int config = index.MatrixIndex / PlatformsCount;
        return solutionModel.BuildTypes[config];
    }

    // This should only be called from ConfigIndex
    private string PlatformFromIndex(SolutionConfigIndex index)
    {
        if (index.MatrixIndex < 0 || index.MatrixIndex >= matrixSize)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int plat = index.MatrixIndex % PlatformsCount;
        return solutionModel.Platforms[plat];
    }

    /// <summary>
    /// Represents all project configurations that are mapped from
    /// all solution configurations for a single project.
    /// </summary>
    internal readonly struct SolutionToProjectMappings
    {
#if DEBUG
        // For debugging, to know which project this is for.
        private readonly SolutionProjectModel projectModel;
#endif
        private readonly ProjectConfigMapping[] mappings;

        internal SolutionToProjectMappings(
            SolutionConfigurationMap configMap,
            SolutionProjectModel projectModel,
            out bool isConfigurable,
            bool forceExclude = false)
        {
#if DEBUG
            this.projectModel = projectModel;
#endif
            mappings = new ProjectConfigMapping[configMap.matrixSize];

            ConfigurationRuleFollower projectTypeRules =
                configMap.solutionModel.ProjectTypeTable.GetProjectConfigurationRules(projectModel,
                    excludeProjectSpecificRules: true);

            isConfigurable = projectTypeRules.GetIsBuildable() ?? true;

            for (int iPlatform = 0; iPlatform < configMap.PlatformsCount; iPlatform++)
            {
                string solutionPlatform = PlatformNames.Canonical(configMap.solutionModel.Platforms[iPlatform]);

                for (int iBuildType = 0; iBuildType < configMap.BuildTypesCount; iBuildType++)
                {
                    string solutionBuildType = configMap.solutionModel.BuildTypes[iBuildType];

                    bool build = projectTypeRules.GetIsBuildable(solutionBuildType, solutionPlatform) ?? true;
                    bool deploy = projectTypeRules.GetIsDeployable(solutionBuildType, solutionPlatform) ?? false;
                    string projectBuildType = projectTypeRules.GetProjectBuildType(solutionBuildType, solutionPlatform) ??
                                              solutionBuildType;

                    string projectPlatform = projectTypeRules.GetProjectPlatform(solutionBuildType, solutionPlatform) ??
                                             solutionPlatform;

                    this[configMap.ToIndex(iBuildType, iPlatform)] =
                        new ProjectConfigMapping(projectBuildType, projectPlatform, !forceExclude && build,
                            !forceExclude && deploy);
                }
            }
        }

        internal ProjectConfigMapping this[SolutionConfigIndex index]
        {
            get => mappings[index.MatrixIndex];
            set => mappings[index.MatrixIndex] = value;
        }

#if DEBUG
        /// <inheritdoc/>
        public override string ToString()
        {
            return projectModel.DisplayName ?? string.Empty;
        }
#endif
    }

    /// <summary>
    /// Represents an index into the matrix of solution to project configurations mappings.
    /// </summary>
    internal readonly struct SolutionConfigIndex
    {
        public SolutionConfigIndex() => MatrixIndex = -1;

        internal SolutionConfigIndex(SolutionConfigurationMap map, string buildType, string platform)
            : this(map, map.GetBuildTypeIndex(buildType), map.GetPlatformIndex(platform))
        {
        }

        internal SolutionConfigIndex(SolutionConfigurationMap map, int buildType, int platForm)
        {
            bool unknown = buildType < 0 || buildType >= map.BuildTypesCount || platForm < 0 || platForm >= map.PlatformsCount;
            MatrixIndex = unknown ? -1 : buildType * map.PlatformsCount + platForm;
        }

        internal int MatrixIndex { get; }

        internal string BuildType(SolutionConfigurationMap map) => map.BuildTypeFromIndex(this);

        internal string Platform(SolutionConfigurationMap map) => map.PlatformFromIndex(this);
    }
}
