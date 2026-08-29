// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Fallout.Persistence.Solution.Model;

/// <summary>
/// Base model for models that contain property bags.
/// </summary>
public abstract class PropertyContainerModel
{
    private List<SolutionPropertyBag>? properties;

    private protected PropertyContainerModel()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyContainerModel"/> class.
    /// Copy constructor.
    /// </summary>
    /// <param name="propertyContainer">The property container model to copy.</param>
    private protected PropertyContainerModel(PropertyContainerModel propertyContainer)
    {
        if (!propertyContainer.properties.IsNullOrEmpty())
        {
            properties = new List<SolutionPropertyBag>(propertyContainer.properties.Count);
            foreach (SolutionPropertyBag property in propertyContainer.properties)
            {
                properties.Add(new SolutionPropertyBag(property));
            }
        }
    }

    /// <summary>
    /// Gets properties associated with this model.
    /// </summary>
    public IReadOnlyList<SolutionPropertyBag> Properties => properties.IReadOnlyList() ?? [];

    /// <summary>
    /// Gets a property bag by its id.
    /// </summary>
    /// <param name="id">The property bag id.</param>
    /// <returns>The property bag if found.</returns>
    public SolutionPropertyBag? FindProperties(string id)
    {
        return properties.FindByItemRef(id);
    }

    /// <summary>
    /// Adds or gets a property bag by its id.
    /// </summary>
    /// <param name="id">The property bag id.</param>
    /// <param name="scope">The scope to create a new property bag with.</param>
    /// <returns>The property bag.</returns>
    public SolutionPropertyBag AddProperties(string id, PropertiesScope scope = PropertiesScope.PreLoad)
    {
        properties ??= [];
        return FindProperties(id) ?? properties.AddAndReturn(new SolutionPropertyBag(id, scope));
    }

    /// <summary>
    /// Removes a property bag by its id.
    /// </summary>
    /// <param name="id">The property bag id.</param>
    /// <returns><see langword="true"/> if the property bag was found and removed.</returns>
    public bool RemoveProperties(string id)
    {
        return
            this.properties is not null &&
            FindProperties(id) is SolutionPropertyBag properties &&
            this.properties.Remove(properties);
    }
}
