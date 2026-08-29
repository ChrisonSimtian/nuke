// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Fallout.Persistence.Solution.Model;

namespace Fallout.Persistence.Solution.Serializer.Xml.XmlDecorators;

/// <summary>
/// Represents the root Solution XML element in the slnx file.
/// </summary>
internal sealed partial class XmlSolution(SlnxFile file, XmlElement element) :
    XmlContainerWithProperties(file, element, Keyword.Solution)
{
    private ItemRefList<XmlConfigurations> configurationsSingle = new();
    private ItemRefList<XmlFolder> folders = new(ignoreCase: true);
    private ItemRefList<XmlProject> rootProjects = new(ignoreCase: true);

    internal string? Description
    {
        get => GetXmlAttribute(Keyword.Description);
        set => UpdateXmlAttribute(Keyword.Description, value);
    }

    internal string? Version
    {
        get => GetXmlAttribute(Keyword.Version);
        set => UpdateXmlAttribute(Keyword.Version, value);
    }

#if DEBUG

    internal override string DebugDisplay => $"{base.DebugDisplay} RootProjects={rootProjects} Folders={folders}";

#endif

    /// <inheritdoc/>
    internal override XmlDecorator? ChildDecoratorFactory(XmlElement element, Keyword elementName)
    {
        return elementName switch
        {
            Keyword.Configurations => new XmlConfigurations(Root, element),
            Keyword.Project => CreateProjectDecorator(element, xmlParentFolder: null),
            Keyword.Folder => new XmlFolder(Root, this, element),
            _ => base.ChildDecoratorFactory(element, elementName),
        };
    }

    internal XmlProject CreateProjectDecorator(XmlElement element, XmlFolder? xmlParentFolder)
    {
        return new XmlProject(Root, xmlParentFolder, element);
    }

    /// <inheritdoc/>
    internal override void OnNewChildDecoratorAdded(XmlDecorator childDecorator)
    {
        switch (childDecorator)
        {
            case XmlFolder folder:
                folders.Add(folder);
                break;

            case XmlProject project:
                rootProjects.Add(project);
                break;

            case XmlConfigurations configurations:
                configurationsSingle.Add(configurations);
                break;
        }

        base.OnNewChildDecoratorAdded(childDecorator);
    }

    /// <inheritdoc/>
    internal override XmlDecorator? FindNextDecorator<TDecorator>()
    {
        return typeof(TDecorator).Name switch
        {
            nameof(XmlConfigurations) => folders.FirstOrDefault() ?? FindNextDecorator<XmlFolder>(),
            nameof(XmlFolder) => rootProjects.FirstOrDefault() ?? FindNextDecorator<XmlProject>(),
            nameof(XmlProject) => propertyBags.FirstOrDefault(),
            _ => null,
        };
    }

    #region Deserialize model

    internal SolutionModel ToModel()
    {
        // Ensure the file version is supported.
        string? fileVersion = Version;
        if (!fileVersion.IsNullOrEmpty())
        {
            try
            {
                Root.FileVersion = new Version(fileVersion);
            }
            catch (Exception ex) when (SolutionException.ShouldWrap(ex))
            {
                throw SolutionException.Create(ex, this, string.Format(Errors.InvalidVersion_Args1, fileVersion),
                    SolutionErrorType.InvalidVersion);
            }

            if (Root.FileVersion.Major > SlnxFile.CurrentVersion)
            {
                throw SolutionException.Create(string.Format(Errors.UnsupportedVersion_Args1, fileVersion), this,
                    SolutionErrorType.UnsupportedVersion);
            }
        }

        SolutionModel solutionModel = new()
        {
            StringTable = Root.StringTable,
            Description = Description,

            // Project types are loaded earlier when parsing the XML since they are needed to resolve projects.
            ProjectTypes = Root.ProjectTypes.ProjectTypes,
        };

        List<(XmlProject, SolutionProjectModel)> newProjects = new(rootProjects.ItemsCount);
        foreach (XmlProject project in rootProjects.GetItems())
        {
            newProjects.Add((project, project.AddToModel(solutionModel)));
        }

        foreach (XmlFolder folder in folders.GetItems())
        {
            folder.AddToModel(solutionModel, newProjects);
        }

        // Dependencies need to be added after all the projects are loaded.
        foreach ((XmlProject xmlProject, SolutionProjectModel modelProject) in newProjects)
        {
            xmlProject.AddDependenciesToModel(solutionModel, modelProject);
        }

        foreach (XmlConfigurations configurations in configurationsSingle.GetItems())
        {
            configurations.AddToModel(solutionModel);
        }

        // Create default configurations if they weren't provided by the Configurations section.
        // Add default build types (Debug/Release) if not specified.
        if (solutionModel.BuildTypes.IsNullOrEmpty() && solutionModel.SolutionProjects.Count > 0)
        {
            solutionModel.AddBuildType(BuildTypeNames.Debug);
            solutionModel.AddBuildType(BuildTypeNames.Release);
        }

        // Add default platform (Any CPU) if not specified.
        if (solutionModel.Platforms.IsNullOrEmpty() && solutionModel.SolutionProjects.Count > 0)
        {
            solutionModel.AddPlatform(PlatformNames.AnySpaceCPU);
        }

        foreach (XmlProperties properties in propertyBags.GetItems())
        {
            properties.AddToModel(solutionModel);
        }

        return solutionModel;
    }

    /// <summary>
    /// Create a project type table from the declared project types in this solution.
    /// </summary>
    internal ProjectTypeTable GetProjectTypeTable()
    {
        foreach (XmlConfigurations xmlConfigurations in configurationsSingle.GetItems())
        {
            ProjectTypeTable? propertyTypeTable = xmlConfigurations.GetProjectTypeTable();
            if (propertyTypeTable is not null)
            {
                return propertyTypeTable;
            }
        }

        return new ProjectTypeTable();
    }

    #endregion

    // Try to figure out indentation and line ending default from the XML.
    internal bool TryGetFormatting(out StringSpan newLine, out StringSpan indent)
    {
        foreach (XmlDecorator decorator in folders.GetItems())
        {
            if (TryDecorator(decorator, newLine: out newLine, indent: out indent))
            {
                return true;
            }
        }

        foreach (XmlDecorator decorator in rootProjects.GetItems())
        {
            if (TryDecorator(decorator, newLine: out newLine, indent: out indent))
            {
                return true;
            }
        }

        foreach (XmlDecorator decorator in propertyBags.GetItems())
        {
            if (TryDecorator(decorator, newLine: out newLine, indent: out indent))
            {
                return true;
            }
        }

        foreach (XmlConfigurations configurations in configurationsSingle.GetItems())
        {
            if (TryDecorator(configurations, newLine: out newLine, indent: out indent))
            {
                return true;
            }
        }

        newLine = StringSpan.Empty;
        indent = StringSpan.Empty;
        return false;

        static bool TryDecorator(XmlDecorator decorator, out StringSpan newLine, out StringSpan indent)
        {
            StringSpan both = decorator.GetNewLineAndIndent();
            if (both.IsEmpty)
            {
                newLine = StringSpan.Empty;
                indent = StringSpan.Empty;
                return false;
            }

            indent = both.TrimStart(['\n', '\r']);
            newLine = both.Slice(0, both.Length - indent.Length);
            if (newLine.Length > 1)
            {
                // If the sample line has multiple newlines, just take one.
                bool isCrLf = newLine[0] is '\r' && newLine[1] is '\n';
                newLine = newLine.Slice(0, isCrLf ? 2 : 1);
            }

            return true;
        }
    }
}
