// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Fallout.Persistence.Solution.Serializer.SlnV12;
using Fallout.Persistence.Solution.Serializer.Xml;

namespace Fallout.Persistence.Solution.Model;

/// <summary>
/// A helper to get and set Visual Studio specific properties in the solution model.
/// </summary>
public readonly struct VisualStudioProperties
{
    private readonly SolutionModel solution;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualStudioProperties"/> struct.
    /// </summary>
    /// <param name="solution">The solution model.</param>
    internal VisualStudioProperties(SolutionModel solution)
    {
        this.solution = solution;
    }

    /// <summary>
    /// Gets or sets extra info for VS to open the solution with a specific installed version.
    /// (e.g. # Visual Studio Version 17 in SLN file).
    /// </summary>
    public string? OpenWith
    {
        get => GetProperty(nameof(OpenWith));
        set => SetProperty(nameof(OpenWith), value, string.Empty);
    }

    /// <summary>
    /// Gets or sets the solution file property that is used to determine if the solution should be shown
    /// in Visual Studio's solution explorer.
    /// </summary>
    [Obsolete("This setting is not supported.")]
    public bool? HideSolutionNode
    {
        get => bool.TryParse(GetProperty(nameof(HideSolutionNode)), out bool hideSolutionNode) ? hideSolutionNode : null;
        set => SetProperty(nameof(HideSolutionNode), value == true ? Keywords.XmlTrue : null, Keywords.XmlFalse);
    }

    /// <summary>
    /// Gets or sets the minimum version of Visual Studio required to open this solution.
    /// </summary>
    public Version? MinimumVersion
    {
        get => SlnV12Extensions.TryParseVSVersion(GetProperty(nameof(MinimumVersion)));
        set => SetProperty(nameof(MinimumVersion), value?.ToString(), string.Empty);
    }

    /// <summary>
    /// Gets or sets the version of Visual Studio that was used to save this solution.
    /// </summary>
    /// <remarks>
    /// This value is only for reference and does not impact any behavior.
    /// </remarks>
    public Version? Version
    {
        get => SlnV12Extensions.TryParseVSVersion(GetProperty(nameof(Version)));
        set => SetProperty(nameof(Version), value?.ToString(), string.Empty);
    }

    /// <summary>
    /// Gets or sets an id for the solution.
    /// </summary>
    public Guid? SolutionId
    {
        get => Guid.TryParse(GetProperty(nameof(SolutionId)), out Guid solutionId) ? solutionId : null;
        set => SetProperty(nameof(SolutionId), value == Guid.Empty ? null : value.ToString(), string.Empty);
    }

    private string? GetProperty(string propertyName)
    {
        Argument.ThrowIfNull(solution, nameof(solution));
        SolutionPropertyBag? vsProperties = solution.FindProperties(SectionName.VisualStudio);
        return vsProperties is null ? null : vsProperties.TryGetValue(propertyName, out string? value) ? value : null;
    }

    private void SetProperty(string propertyName, string? value, string defaultValue)
    {
        Argument.ThrowIfNull(solution, nameof(solution));
        if (value is null || StringComparer.OrdinalIgnoreCase.Equals(value, defaultValue))
        {
            SolutionPropertyBag? vsProperties = solution.FindProperties(SectionName.VisualStudio);
            if (vsProperties is null)
            {
                return;
            }

            _ = vsProperties.Remove(propertyName);
            if (vsProperties.Count == 0)
            {
                _ = solution.RemoveProperties(SectionName.VisualStudio);
            }
        }
        else
        {
            solution.AddProperties(SectionName.VisualStudio).Add(propertyName, value);
        }
    }
}
