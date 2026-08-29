// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Fallout.Persistence.Solution.Model;
using Fallout.Persistence.Solution.Utilities;

namespace Fallout.Persistence.Solution.Serializer.Xml;

internal enum Keyword
{
    Unknown,

    // Root element
    Solution,

    // Solution properties
    Description,
    Version,

    // Solution sections
    Configurations,

    // item (folders and project) properties
    Folder,
    Project,
    Id,
    Name,
    Path,
    Type,
    DefaultStartup,
    DisplayName,
    File,
    BuildDependency,

    // ProjectType properties
    ProjectType,
    TypeId,
    Extension,
    BasedOn,
    IsBuildable,
    SupportsPlatform,

    // Configuration properties
    Configuration,
    Dimension,
    BuildType,
    Platform,
    Build,
    Deploy,

    // Properties
    Property,
    Properties,
    Scope,
    PostLoad,
    PreLoad,
    Value,

    MaxProp,
}

internal static class Keywords
{
    internal const string XmlTrue = "true";
    internal const string XmlFalse = "false";

    private static readonly string[] KeywordToString;
    private static readonly Lictionary<string, Keyword> StringToKeyword;

    static Keywords()
    {
        StringToKeyword = new Lictionary<string, Keyword>(
            [
                new KeyValuePair<string, Keyword>(nameof(Keyword.Solution), Keyword.Solution),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Description), Keyword.Description),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Version), Keyword.Version),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Configurations), Keyword.Configurations),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Folder), Keyword.Folder),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Project), Keyword.Project),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Id), Keyword.Id),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Name), Keyword.Name),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Path), Keyword.Path),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Type), Keyword.Type),
                new KeyValuePair<string, Keyword>(nameof(Keyword.DefaultStartup), Keyword.DefaultStartup),
                new KeyValuePair<string, Keyword>(nameof(Keyword.DisplayName), Keyword.DisplayName),
                new KeyValuePair<string, Keyword>(nameof(Keyword.File), Keyword.File),
                new KeyValuePair<string, Keyword>(nameof(Keyword.BuildDependency), Keyword.BuildDependency),
                new KeyValuePair<string, Keyword>(nameof(Keyword.ProjectType), Keyword.ProjectType),
                new KeyValuePair<string, Keyword>(nameof(Keyword.TypeId), Keyword.TypeId),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Extension), Keyword.Extension),
                new KeyValuePair<string, Keyword>(nameof(Keyword.BasedOn), Keyword.BasedOn),
                new KeyValuePair<string, Keyword>(nameof(Keyword.IsBuildable), Keyword.IsBuildable),
                new KeyValuePair<string, Keyword>(nameof(Keyword.SupportsPlatform), Keyword.SupportsPlatform),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Configuration), Keyword.Configuration),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Dimension), Keyword.Dimension),
                new KeyValuePair<string, Keyword>(nameof(Keyword.BuildType), Keyword.BuildType),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Platform), Keyword.Platform),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Build), Keyword.Build),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Deploy), Keyword.Deploy),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Property), Keyword.Property),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Properties), Keyword.Properties),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Scope), Keyword.Scope),
                new KeyValuePair<string, Keyword>(nameof(Keyword.PostLoad), Keyword.PostLoad),
                new KeyValuePair<string, Keyword>(nameof(Keyword.PreLoad), Keyword.PreLoad),
                new KeyValuePair<string, Keyword>(nameof(Keyword.Value), Keyword.Value),
            ],
            StringComparer.OrdinalIgnoreCase);

        KeywordToString = new string[(int)Keyword.MaxProp];
        foreach ((string keywordStr, Keyword keyword) in StringToKeyword)
        {
            KeywordToString[(int)keyword] = keywordStr;
        }
    }

    internal static string ToXmlString(this Keyword keyword) => KeywordToString[(int)keyword]; // let it throw

    internal static string ToXmlBool(this bool value) => value ? XmlTrue : XmlFalse;

    internal static Keyword ToKeyword(string name) =>
        !string.IsNullOrEmpty(name) && StringToKeyword.TryGetValue(name, out Keyword ret) ? ret : Keyword.Unknown;

    // Adds common solution constants to string table.
    internal static StringTable WithSolutionConstants(this StringTable stringTable)
    {
        // Try to use the interned strings for common solution values.
        stringTable.AddString(XmlTrue);
        stringTable.AddString(XmlFalse);
        stringTable.AddString(BuildTypeNames.Debug);
        stringTable.AddString(BuildTypeNames.Release);
        stringTable.AddString(PlatformNames.All);
        stringTable.AddString(PlatformNames.Missing);
        stringTable.AddString(PlatformNames.Default);
        stringTable.AddString(PlatformNames.AnyCPU);
        stringTable.AddString(PlatformNames.AnySpaceCPU);
        stringTable.AddString(PlatformNames.Win32);
        stringTable.AddString(PlatformNames.x64);
        stringTable.AddString(PlatformNames.x86);
        stringTable.AddString(PlatformNames.arm);
        stringTable.AddString(PlatformNames.arm64);
        stringTable.AddString(PlatformNames.ARM);
        stringTable.AddString(PlatformNames.ARM64);

        foreach (string propertyName in KeywordToString)
        {
            if (!string.IsNullOrEmpty(propertyName))
            {
                stringTable.AddString(propertyName);
            }
        }

        return stringTable;
    }
}
