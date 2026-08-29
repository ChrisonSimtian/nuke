using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Fallout.Common.Tools.MSBuild;

public class MSBuildProject : DynamicObject
{
    internal MSBuildProject(
        bool isSdkProject,
        IReadOnlyDictionary<string, string> properties,
        ILookup<string, string> itemGroups)
    {
        IsSdkProject = isSdkProject;
        Properties = properties;
        ItemGroups = itemGroups;
    }

    public bool IsSdkProject { get; }

    public bool IsLegacyProject => !IsSdkProject;

    public IReadOnlyDictionary<string, string> Properties { get; }

    public ILookup<string, string> ItemGroups { get; }
}
