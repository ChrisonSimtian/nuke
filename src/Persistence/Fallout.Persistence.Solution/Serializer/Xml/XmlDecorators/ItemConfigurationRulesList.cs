// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Fallout.Persistence.Solution.Model;

namespace Fallout.Persistence.Solution.Serializer.Xml.XmlDecorators;

/// <summary>
/// Helper to serialize all of the different types of configuration rules.
/// This is used to share logic for ProjectTypes and Projects.
/// </summary>
internal struct ItemConfigurationRulesList
{
    private ItemRefList<XmlConfigurationBuildType> buildTypeRules = new(ignoreCase: true);
    private ItemRefList<XmlConfigurationPlatform> platformRules = new(ignoreCase: true);
    private ItemRefList<XmlConfigurationBuild> buildRules = new(ignoreCase: true);
    private ItemRefList<XmlConfigurationDeploy> deployRules = new(ignoreCase: true);

    public ItemConfigurationRulesList()
    {
    }

    internal readonly void Add(XmlConfiguration configuration)
    {
        switch (configuration)
        {
            case XmlConfigurationBuildType buildType:
                buildTypeRules.Add(buildType);
                break;

            case XmlConfigurationPlatform platform:
                platformRules.Add(platform);
                break;

            case XmlConfigurationBuild build:
                buildRules.Add(build);
                break;

            case XmlConfigurationDeploy deploy:
                deployRules.Add(deploy);
                break;

            default:
                throw new InvalidOperationException();
        }
    }

    internal readonly XmlDecorator? FindNextDecorator<TDecorator>()
    {
        return typeof(TDecorator).Name switch
        {
            nameof(XmlConfigurationBuildType) or nameof(XmlConfiguration) => platformRules.FirstOrDefault() ??
                                                                             FindNextDecorator<XmlConfigurationPlatform>(),
            nameof(XmlConfigurationPlatform) => buildRules.FirstOrDefault() ?? FindNextDecorator<XmlConfigurationBuild>(),
            nameof(XmlConfigurationBuild) => deployRules.FirstOrDefault(),
            nameof(XmlConfigurationDeploy) => null,
            _ => null,
        };
    }

    internal readonly XmlDecorator? FirstOrDefault()
    {
        return buildTypeRules.FirstOrDefault() ?? platformRules.FirstOrDefault() ??
            buildRules.FirstOrDefault() ?? (XmlDecorator?)deployRules.FirstOrDefault();
    }

    internal bool ApplyModelToXml(XmlContainer xmlContainer, IReadOnlyList<ConfigurationRule>? configurationRules)
    {
        bool modified = false;

        configurationRules ??= [];
        modified |= ApplyModelToXml(xmlContainer, configurationRules, BuildDimension.BuildType, Keyword.BuildType,
            ref buildTypeRules);

        modified |= ApplyModelToXml(xmlContainer, configurationRules, BuildDimension.Platform, Keyword.Platform,
            ref platformRules);

        modified |= ApplyModelToXml(xmlContainer, configurationRules, BuildDimension.Build, Keyword.Build, ref buildRules);
        modified |= ApplyModelToXml(xmlContainer, configurationRules, BuildDimension.Deploy, Keyword.Deploy, ref deployRules);
        return modified;

        static bool ApplyModelToXml<T>(XmlContainer xmlContainer, IReadOnlyList<ConfigurationRule> configurationRules,
            BuildDimension dimension, Keyword dimensionElementName, ref ItemRefList<T> configurations)
            where T : XmlConfiguration
        {
            List<(string ItemRef, ConfigurationRule Item)> dimensionRules = configurationRules.WhereToList(
                static (x, dimension) => x.Dimension == dimension,
                static (x, _) => (ItemRef: x.GetSolutionConfiguration(), Item: x),
                dimension);

            return xmlContainer.ApplyModelItemsToXml(
                modelItems: dimensionRules,
                decoratorItems: ref configurations,
                decoratorElementName: dimensionElementName,
                applyModelToXml: static (newConfiguration, modelConfiguration) =>
                    newConfiguration.ApplyModelToXml(modelConfiguration));
        }
    }

    internal readonly List<ConfigurationRule> ToModel()
    {
        List<ConfigurationRule> rules = new(
            buildTypeRules.ItemsCount +
            platformRules.ItemsCount +
            buildRules.ItemsCount +
            deployRules.ItemsCount);

        foreach (XmlConfiguration configuration in buildTypeRules.GetItems())
        {
            AddRule(rules, configuration);
        }

        foreach (XmlConfiguration configuration in platformRules.GetItems())
        {
            AddRule(rules, configuration);
        }

        foreach (XmlConfiguration configuration in buildRules.GetItems())
        {
            AddRule(rules, configuration);
        }

        foreach (XmlConfiguration configuration in deployRules.GetItems())
        {
            AddRule(rules, configuration);
        }

        return rules;

        static void AddRule(List<ConfigurationRule> rules, XmlConfiguration configuration)
        {
            ConfigurationRule? rule = configuration.ToModel();
            if (rule is not null)
            {
                rules.Add(rule.Value);
            }
        }
    }
}
