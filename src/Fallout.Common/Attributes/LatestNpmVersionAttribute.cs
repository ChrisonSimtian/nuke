using System.Reflection;
using Fallout.Common.Utilities;
using Fallout.Common.ValueInjection;
using NuGet.Versioning;

namespace Fallout.Common.Tooling;

public class LatestNpmVersionAttribute : ValueInjectionAttributeBase
{
    private readonly string packageId;

    public LatestNpmVersionAttribute(string packageId)
    {
        this.packageId = packageId;
    }

    public override object GetValue(MemberInfo member, object instance)
    {
        var version = NpmVersionResolver.GetLatestVersion(packageId).GetAwaiter().GetResult();
        return member.GetMemberType() == typeof(string)
            ? version
            : SemanticVersion.Parse(version);
    }
}
