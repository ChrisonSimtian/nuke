// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Fallout.Persistence.Solution.Model;

namespace Fallout.Persistence.Solution.Serializer.Xml.XmlDecorators;

/// <summary>
/// Child of a Solution that represents a project type not implicitly know about.
/// Allows the file to specify a friendly name or associate and extension with a project type guid.
/// </summary>
internal sealed class XmlProjectType(SlnxFile root, XmlElement element) :
    XmlContainer(root, element, Keyword.ProjectType),
    IItemRefDecorator
{
    private ItemConfigurationRulesList configurationRules = new();

    public Keyword ItemRefAttribute => Keyword.TypeId;

    /// <inheritdoc cref="ProjectType.ProjectTypeId"/>
    internal Guid TypeId
    {
        get => GetXmlAttributeGuid(Keyword.TypeId);
        set => UpdateXmlAttributeGuid(Keyword.TypeId, value);
    }

    /// <inheritdoc cref="ProjectType.Name"/>
    internal string? Name
    {
        get => GetXmlAttribute(Keyword.Name);
        set => UpdateXmlAttribute(Keyword.Name, value);
    }

    /// <inheritdoc cref="ProjectType.Extension"/>
    internal string? Extension
    {
        get => GetXmlAttribute(Keyword.Extension);
        set => UpdateXmlAttribute(Keyword.Extension, value);
    }

    /// <inheritdoc cref="ProjectType.BasedOn"/>
    internal string? BasedOn
    {
        get => GetXmlAttribute(Keyword.BasedOn);
        set => UpdateXmlAttribute(Keyword.BasedOn, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the project type is buildable.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="true"/>.
    /// When <see langword="false"/> automatically sets configuration rules to never build.
    /// </remarks>
    internal bool IsBuildable
    {
        get => GetXmlAttributeBool(Keyword.IsBuildable, defaultValue: true);
        set => UpdateXmlAttributeBool(Keyword.IsBuildable, value, defaultValue: true);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the project type supports platform configurations.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="true"/>.
    /// When <see langword="false"/> automatically adds configuration rule to remove platform mappings.
    /// This setting is ignored if <see cref="IsBuildable"/> is <see langword="false"/>.
    /// </remarks>
    internal bool SupportsPlatform
    {
        get => GetXmlAttributeBool(Keyword.SupportsPlatform, defaultValue: true);
        set => UpdateXmlAttributeBool(Keyword.SupportsPlatform, value, defaultValue: true);
    }

    private protected override bool AllowEmptyItemRef => true;

    /// <summary>
    /// Gets or sets although every project type should have a TypeId, there may be multiple project types with the same TypeId.
    /// So use the Name and TypeId to uniquely identify a project type.
    /// </summary>
    private protected override string RawItemRef
    {
        get => GetItemRef(Name, Extension, TypeId);
        set
        {
            if (value.IsNullOrEmpty())
            {
                Name = null;
                Extension = null;
                TypeId = Guid.Empty;
            }
            else if (value.EndsWith('⁂'))
            {
                Name = null;
                Extension = value.Substring(0, value.Length - 1);
            }
            else
            {
                Name = value;
            }
        }
    }

    internal static string GetItemRef(string? name, string? extension, Guid typeId)
    {
        // Return empty string for default project type ItemRef.
        return name is null && extension is null && typeId == Guid.Empty ? string.Empty : name ?? $"{extension}⁂";
    }

    /// <inheritdoc/>
    internal override XmlDecorator? ChildDecoratorFactory(XmlElement element, Keyword elementName)
    {
        return elementName switch
        {
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
            case XmlConfiguration configuration:
                configurationRules.Add(configuration);
                break;
        }

        base.OnNewChildDecoratorAdded(childDecorator);
    }

    /// <inheritdoc/>
    internal override XmlDecorator? FindNextDecorator<TDecorator>()
    {
        return configurationRules.FindNextDecorator<TDecorator>();
    }

    internal override bool IsValid()
    {
        return base.IsValid();
    }

    internal ProjectType ToModel()
    {
        ConfigurationRule[] rules =
            !IsBuildable ? ProjectTypeTable.NoBuildRules :
            !SupportsPlatform ? [ProjectTypeTable.NoPlatformsRule, .. configurationRules.ToModel()] :
            /*default*/ [.. configurationRules.ToModel()];

        return new ProjectType(TypeId, rules)
        {
            Name = GetTableString(Name),
            Extension = Extension,
            BasedOn = BasedOn,
        };
    }

    // Update the Xml DOM with changes from the model.
    internal bool ApplyModelToXml(ProjectType modelProjectType)
    {
        bool modified = false;
        if (!StringComparer.Ordinal.Equals(Name, modelProjectType.Name))
        {
            Name = modelProjectType.Name;
            modified = true;
        }

        if (!StringComparer.Ordinal.Equals(Extension, modelProjectType.Extension))
        {
            Extension = modelProjectType.Extension;
            modified = true;
        }

        if (TypeId != modelProjectType.ProjectTypeId)
        {
            TypeId = modelProjectType.ProjectTypeId;
            modified = true;
        }

        if (BasedOn != modelProjectType.BasedOn)
        {
            BasedOn = modelProjectType.BasedOn;
            modified = true;
        }

        ConfigurationRuleFollower rules = new(modelProjectType.ConfigurationRules);
        bool isBuildable = rules.GetIsBuildable() ?? true;
        bool supportsPlatform = rules.GetProjectPlatform() != PlatformNames.Missing;

        if (IsBuildable != isBuildable)
        {
            IsBuildable = isBuildable;
            modified = true;
        }

        if (SupportsPlatform != supportsPlatform)
        {
            SupportsPlatform = supportsPlatform;
            modified = true;
        }

        // Determine which rules to serizlize. Remove rules implied by IsBuildable and SupportsPlatform.
        IReadOnlyList<ConfigurationRule>? rulesToApply =
            !isBuildable ? [] :
            !supportsPlatform ? RemovePlatformRules(modelProjectType.ConfigurationRules) :
            modelProjectType.ConfigurationRules;

        modified |= configurationRules.ApplyModelToXml(this, rulesToApply);
        return modified;

        // Remove any platform rules from the list.
        static List<ConfigurationRule> RemovePlatformRules(IReadOnlyList<ConfigurationRule> rules) =>
            rules.WhereToList(
                predicate: static (rule, _) => rule.Dimension != BuildDimension.Platform,
                selector: static (rule, _) => rule,
                (object?)null);
    }
}
