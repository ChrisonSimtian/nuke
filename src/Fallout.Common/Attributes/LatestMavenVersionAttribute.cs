using System.Linq;
using System.Reflection;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Common.ValueInjection;
using NuGet.Versioning;

namespace Fallout.Common.Tooling;

public class LatestMavenVersionAttribute : ValueInjectionAttributeBase
{
    private readonly string repository;
    private readonly string groupId;
    private readonly string artifactId;

    public LatestMavenVersionAttribute(string repository, string groupId, string artifactId = null)
    {
        this.repository = repository;
        this.groupId = groupId;
        this.artifactId = artifactId;
    }

    public bool IncludePrerelease { get; set; }

    public override object GetValue(MemberInfo member, object instance)
    {
        var endpoint = repository.TrimStart("https").TrimStart("http").TrimStart("://").TrimEnd("/");
        var uri = $"https://{endpoint}/{groupId.Replace(".", "/")}/{artifactId ?? groupId}/maven-metadata.xml";
        var content = HttpTasks.HttpDownloadString(uri);
        var versions = XmlTasks.XmlPeekFromString(content, ".//version").ToList();
        var version = versions
            .Select(NuGetVersion.Parse)
            .OrderByDescending(x => x)
            .FirstOrDefault(x => !x.IsPrerelease || IncludePrerelease);

        return member.GetMemberType() == typeof(string)
            ? version?.ToNormalizedString()
            : version;
    }
}
