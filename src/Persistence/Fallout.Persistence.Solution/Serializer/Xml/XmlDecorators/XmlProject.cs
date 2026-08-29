// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Fallout.Persistence.Solution.Model;
using Fallout.Persistence.Solution.Utilities;

namespace Fallout.Persistence.Solution.Serializer.Xml.XmlDecorators;

/// <summary>
/// Child of a Solution or Folder that represents a project in the solution.
/// </summary>
internal sealed partial class XmlProject(SlnxFile root, XmlFolder? xmlParentFolder, XmlElement element) :
    XmlContainerWithProperties(root, element, Keyword.Project),
    IItemRefDecorator
{
    private ItemRefList<XmlBuildDependency> buildDependencies = new(ignoreCase: true);
    private ItemConfigurationRulesList configurationRules = new();

    public Keyword ItemRefAttribute => Keyword.Path;

    internal string Path => ItemRef;

    internal StringSpan DefaultDisplayName => PathExtensions.GetStandardDisplayName(PathExtensions.ConvertToModel(Path));

    internal Guid Id
    {
        get => GetXmlAttributeGuid(Keyword.Id);
        set => UpdateXmlAttributeGuid(Keyword.Id, value);
    }

    internal string? DisplayName
    {
        get => GetXmlAttribute(Keyword.DisplayName);
        set => UpdateXmlAttribute(Keyword.DisplayName, value);
    }

    internal string? Type
    {
        get => GetXmlAttribute(Keyword.Type);
        set => UpdateXmlAttribute(Keyword.Type, value);
    }

    internal bool DefaultStartup
    {
        get => GetXmlAttributeBool(Keyword.DefaultStartup, defaultValue: false);
        set => UpdateXmlAttributeBool(Keyword.DefaultStartup, value);
    }

    internal XmlFolder? ParentFolder { get; } = xmlParentFolder;

    /// <inheritdoc/>
    internal override XmlDecorator? ChildDecoratorFactory(XmlElement element, Keyword elementName)
    {
        return elementName switch
        {
            Keyword.BuildDependency => new XmlBuildDependency(Root, element),
            Keyword.BuildType => new XmlConfigurationBuildType(Root, element),
            Keyword.Platform => new XmlConfigurationPlatform(Root, element),
            Keyword.Build => new XmlConfigurationBuild(Root, element),
            Keyword.Deploy => new XmlConfigurationDeploy(Root, element),
            _ => base.ChildDecoratorFactory(element, elementName),
        };
    }

    /// <inheritdoc/>
    internal override void OnNewChildDecoratorAdded(XmlDecorator childDecorator)
    {
        switch (childDecorator)
        {
            case XmlBuildDependency buildDependency:
                buildDependencies.Add(buildDependency);
                break;

            case XmlConfiguration configuration:
                configurationRules.Add(configuration);
                break;
        }

        base.OnNewChildDecoratorAdded(childDecorator);
    }

    /// <inheritdoc/>
    internal override XmlDecorator? FindNextDecorator<TDecorator>()
    {
        return typeof(TDecorator).Name switch
        {
            nameof(XmlBuildDependency) => configurationRules.FirstOrDefault() ?? FindNextDecorator<XmlConfiguration>(),
            nameof(XmlConfiguration) or nameof(XmlConfigurationBuildType) or nameof(XmlConfigurationPlatform)
                or nameof(XmlConfigurationBuild) or nameof(XmlConfigurationDeploy) =>
                configurationRules.FindNextDecorator<TDecorator>() ?? propertyBags.FirstOrDefault(),
            _ => null,
        };
    }

    #region Deserialize model

    internal SolutionProjectModel AddToModel(SolutionModel solution)
    {
        try
        {
            SolutionFolderModel? parentFolder = null;
            if (ParentFolder is not null)
            {
                SolutionFolderModel? foundParentFolder = solution.FindFolder(ParentFolder.ItemRef);
                if (foundParentFolder is not null)
                {
                    parentFolder = foundParentFolder;
                }
                else
                {
                    throw SolutionException.Create(string.Format(Errors.InvalidFolderReference_Args1, ParentFolder.Name), this,
                        SolutionErrorType.InvalidFolderReference);
                }
            }

            SolutionProjectModel projectModel = solution.AddProject(
                filePath: PathExtensions.ConvertToModel(Path),
                projectTypeName: Type ?? string.Empty,
                folder: parentFolder);

            projectModel.Id = Id;
            projectModel.DisplayName = DisplayName;

            foreach (ConfigurationRule configurationRule in configurationRules.ToModel())
            {
                projectModel.AddProjectConfigurationRule(configurationRule);
            }

            foreach (XmlProperties properties in propertyBags.GetItems())
            {
                properties.AddToModel(projectModel);
            }

            if (DefaultStartup)
            {
                solution.MoveProjectFirst(projectModel);
            }

            Root.UserPaths[projectModel.FilePath] = Path;

            return projectModel;
        }
        catch (Exception ex) when (SolutionException.ShouldWrap(ex))
        {
            throw SolutionException.Create(ex, this);
        }
    }

    internal void AddDependenciesToModel(SolutionModel solution, SolutionProjectModel projectModel)
    {
        foreach (XmlBuildDependency buildDependency in buildDependencies.GetItems())
        {
            string dependencyItemRef = PathExtensions.ConvertToModel(buildDependency.Project);
            SolutionProjectModel? dependencyProject = solution.FindProject(dependencyItemRef);
            if (dependencyProject is not null)
            {
                try
                {
                    projectModel.AddDependency(dependencyProject);
                }
                catch (Exception ex) when (SolutionException.ShouldWrap(ex))
                {
                    throw SolutionException.Create(ex, buildDependency);
                }
            }
            else
            {
                throw SolutionException.Create(string.Format(Errors.InvalidProjectReference_Args1, dependencyItemRef),
                    buildDependency, SolutionErrorType.InvalidProjectReference);
            }
        }
    }

    #endregion
}
