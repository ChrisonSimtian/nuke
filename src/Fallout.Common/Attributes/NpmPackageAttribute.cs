using System.Reflection;

namespace Fallout.Common.Tooling;

public class NpmPackageAttribute : ToolInjectionAttributeBase
{
    private readonly string packageId;
    private readonly string packageExecutable;

    public NpmPackageAttribute(string packageId, string packageExecutable = null)
    {
        this.packageId = packageId;
        this.packageExecutable = packageExecutable;
    }

    public string Version { get; set; }

    public override ToolRequirement GetRequirement(MemberInfo member)
    {
        return new NpmPackageRequirement(packageId, Version);
    }

    public override object GetValue(MemberInfo member, object instance)
    {
        var name = packageExecutable ?? member.Name.ToLowerInvariant();
        return ToolResolver.TryGetEnvironmentTool(name) ??
               ToolResolver.GetNpmTool(name);
    }
}
