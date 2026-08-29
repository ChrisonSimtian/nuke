// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Fallout.Persistence.Solution.Model;

namespace Fallout.Persistence.Solution.Serializer.Xml.XmlDecorators;

/// <summary>
/// Child of a Project that represents a configuration mapping from a solution configuration to a project configuration.
/// </summary>
internal abstract class XmlConfiguration(SlnxFile root, XmlElement element, Keyword elementName) :
    XmlDecorator(root, element, elementName),
    IItemRefDecorator
{
    public Keyword ItemRefAttribute => Keyword.Solution;

    internal abstract BuildDimension Dimension { get; }

    internal string Solution
    {
        get => GetXmlAttribute(Keyword.Solution) ?? string.Empty;
        set => UpdateXmlAttribute(Keyword.Solution, value);
    }

    internal string Project
    {
        get => GetXmlAttribute(Keyword.Project) ?? string.Empty;
        set => UpdateXmlAttribute(Keyword.Project, value);
    }

    private protected override bool AllowEmptyItemRef => true;

    #region Deserialize model

    internal ConfigurationRule? ToModel()
    {
        BuildDimension dimension = Dimension;

        // Set default value for build rule to 'true' and deploy rule to 'false'.
        string projectValue =
            Project.NullIfEmpty() ??
            dimension switch
            {
                BuildDimension.Build or BuildDimension.Deploy => bool.TrueString,
                _ => string.Empty,
            };

        if (string.IsNullOrEmpty(projectValue))
        {
            throw SolutionException.Create(Errors.MissingProjectValue, this, SolutionErrorType.MissingProjectValue);
        }

        if (!ModelHelper.TrySplitFullConfiguration(Root.StringTable, Solution, out string? solutionBuildType,
                out string? solutionPlatform) &&
            !Solution.IsNullOrEmpty())
        {
            throw SolutionException.Create(string.Format(Errors.InvalidConfiguration_Args1, Solution), this,
                SolutionErrorType.InvalidConfiguration);
        }

        if (solutionBuildType is BuildTypeNames.All or null)
        {
            solutionBuildType = string.Empty;
        }

        if (solutionPlatform is PlatformNames.All or null)
        {
            solutionPlatform = string.Empty;
        }

        // A configuration element represents a "Configuration" mapping rule.
        return new ConfigurationRule(
            dimension,
            solutionBuildType: solutionBuildType,
            solutionPlatform: solutionPlatform,
            projectValue: GetTableString(projectValue));
    }

    #endregion

    // Update the Xml DOM with changes from the model.
    internal bool ApplyModelToXml(ConfigurationRule configurationRule)
    {
        string value = configurationRule.Dimension switch
        {
            // For build or deploy the default value is 'true'. Use lowercase 'false' to match the XML boolean.
            BuildDimension.Build or BuildDimension.Deploy => bool.Parse(configurationRule.ProjectValue)
                ? string.Empty
                : Keywords.XmlFalse,
            _ => configurationRule.ProjectValue,
        };

        if (StringComparer.Ordinal.Equals(Project, value))
        {
            return false;
        }

        Project = value;
        return true;
    }
}
