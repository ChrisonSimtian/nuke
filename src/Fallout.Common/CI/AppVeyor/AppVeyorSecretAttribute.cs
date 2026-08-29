using System;

namespace Fallout.Common.CI.AppVeyor;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AppVeyorSecretAttribute(string parameter, string value) : Attribute
{
    public string Parameter { get; } = parameter;

    public string Value { get; } = value;
}
