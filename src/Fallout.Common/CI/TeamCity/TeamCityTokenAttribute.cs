using System;

namespace Fallout.Common.CI.TeamCity;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class TeamCityTokenAttribute : Attribute
{
    public TeamCityTokenAttribute(string name, string guid)
    {
        Name = name;
        Guid = guid;
    }

    public string Name { get; }

    public string Guid { get; }
}
