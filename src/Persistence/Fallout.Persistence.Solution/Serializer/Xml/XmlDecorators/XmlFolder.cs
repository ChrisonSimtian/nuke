// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Fallout.Persistence.Solution.Model;
using Fallout.Persistence.Solution.Utilities;

namespace Fallout.Persistence.Solution.Serializer.Xml.XmlDecorators;

/// <summary>
/// Child of a Solution that represents a solution folder.
/// </summary>
internal sealed class XmlFolder(SlnxFile root, XmlSolution xmlSolution, XmlElement element) :
    XmlContainerWithProperties(root, element, Keyword.Folder),
    IItemRefDecorator
{
    private readonly XmlSolution xmlSolution = xmlSolution;
    private ItemRefList<XmlFile> files = new(ignoreCase: true);
    private ItemRefList<XmlProject> folderProjects = new(ignoreCase: true);

    public Keyword ItemRefAttribute => Keyword.Name;

    internal string Name => ItemRef;

    internal Guid Id
    {
        get => GetXmlAttributeGuid(Keyword.Id);
        set => UpdateXmlAttributeGuid(Keyword.Id, value);
    }

#if DEBUG

    internal override string DebugDisplay => $"{base.DebugDisplay} FolderProjects={folderProjects} Files={files}";

#endif

    /// <inheritdoc/>
    internal override XmlDecorator? ChildDecoratorFactory(XmlElement element, Keyword elementName)
    {
        return elementName switch
        {
            // Forward project handling to the solution decorator.
            Keyword.Project => xmlSolution.CreateProjectDecorator(element, xmlParentFolder: this),
            Keyword.File => new XmlFile(Root, element),
            _ => base.ChildDecoratorFactory(element, elementName),
        };
    }

    /// <inheritdoc/>
    internal override void OnNewChildDecoratorAdded(XmlDecorator childDecorator)
    {
        switch (childDecorator)
        {
            case XmlFile file:
                files.Add(file);
                break;

            case XmlProject project:
                folderProjects.Add(project);
                break;
        }

        base.OnNewChildDecoratorAdded(childDecorator);
    }

    /// <inheritdoc/>
    internal override XmlDecorator? FindNextDecorator<TDecorator>()
    {
        return typeof(TDecorator).Name switch
        {
            nameof(XmlFile) => folderProjects.FirstOrDefault() ?? FindNextDecorator<XmlProject>(),
            nameof(XmlProject) => propertyBags.FirstOrDefault(),
            _ => null,
        };
    }

    #region Deserialize model

    internal void AddToModel(SolutionModel solutionModel,
        List<(XmlProject XmlProject, SolutionProjectModel ModelProject)> newProjects)
    {
        try
        {
            SolutionFolderModel folderModel = solutionModel.AddFolder(Name);
            folderModel.Id = Id;

            foreach (XmlFile file in files.GetItems())
            {
                string modelPath = PathExtensions.ConvertToModel(file.Path);
                folderModel.AddFile(modelPath);
                Root.UserPaths[modelPath] = file.Path;
            }

            foreach (XmlProperties properties in propertyBags.GetItems())
            {
                properties.AddToModel(folderModel);
            }

            foreach (XmlProject project in folderProjects.GetItems())
            {
                newProjects.Add((project, project.AddToModel(solutionModel)));
            }
        }
        catch (Exception ex) when (SolutionException.ShouldWrap(ex))
        {
            throw SolutionException.Create(ex, this);
        }
    }

    #endregion

    // Update the Xml DOM with changes from the model.
    internal bool ApplyModelToXml(SolutionFolderModel modelFolder)
    {
        SolutionModel modelSolution = modelFolder.Solution;
        bool modified = false;

        // Attributes
        Guid id = modelFolder.IsDefaultId ? Guid.Empty : modelFolder.Id;
        if (Id != id)
        {
            Id = id;
            modified = true;
        }

        // Files
        modified |= ApplyModelItemsToXml(
            itemRefs: modelFolder.Files?.ToList(Root.ConvertToUserPath),
            decoratorItems: ref files,
            decoratorElementName: Keyword.File);

        // Projects
        List<(string ItemRef, SolutionProjectModel Item)> projectsInFolder = modelSolution.SolutionProjects.WhereToList(
            (project, solutionFolderModel) => ReferenceEquals(project.Parent, solutionFolderModel),
            (project, _) => (ItemRef: Root.ConvertToUserPath(project.ItemRef), Item: project),
            modelFolder);

        modified |= ApplyModelItemsToXml(
            modelItems: projectsInFolder,
            ref folderProjects,
            Keyword.Project,
            applyModelToXml: static (newProject, modelProject) => newProject.ApplyModelToXml(modelProject));

        // Properties
        modified |= ApplyModelToXml(modelFolder.Properties);

        return modified;
    }
}
