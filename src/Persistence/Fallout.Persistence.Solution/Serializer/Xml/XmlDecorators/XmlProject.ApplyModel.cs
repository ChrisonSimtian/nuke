// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Fallout.Persistence.Solution.Model;

namespace Fallout.Persistence.Solution.Serializer.Xml.XmlDecorators;

internal sealed partial class XmlProject
{
    // Update the Xml DOM with changes from the model.
    internal bool ApplyModelToXml(SolutionProjectModel modelProject)
    {
        bool modified = false;

        // Attributes
        string type = Root.ProjectTypes.GetConciseType(modelProject);
        if (!StringComparer.Ordinal.Equals(Type, type))
        {
            Type = type.NullIfEmpty();
            modified = true;
        }

        string? displayName =
            modelProject.DisplayName is null || DefaultDisplayName.EqualsOrdinal(modelProject.ActualDisplayName)
                ? null
                : modelProject.DisplayName;

        if (!StringComparer.Ordinal.Equals(DisplayName, displayName))
        {
            DisplayName = displayName;
            modified = true;
        }

        Guid id = modelProject.IsDefaultId ? Guid.Empty : modelProject.Id;
        if (Id != id)
        {
            Id = id;
            modified = true;
        }

        // BuildDependencies
        modified |= ApplyModelItemsToXml(
            itemRefs: modelProject.Dependencies?.ToList(dependencyProject => Root.ConvertToUserPath(dependencyProject.FilePath)),
            decoratorItems: ref buildDependencies,
            decoratorElementName: Keyword.BuildDependency);

        // Configurations
        modified |= configurationRules.ApplyModelToXml(this, modelProject.ProjectConfigurationRules);

        // Properties
        modified |= ApplyModelToXml(modelProject.Properties);

        return modified;
    }
}
