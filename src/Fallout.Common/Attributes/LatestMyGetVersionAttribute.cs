using System.Linq;
using System.Reflection;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Common.ValueInjection;

namespace Fallout.Common.Tooling;

public class LatestMyGetVersionAttribute : ValueInjectionAttributeBase
{
    private readonly string feed;
    private readonly string package;

    public LatestMyGetVersionAttribute(string feed, string package)
    {
        this.feed = feed;
        this.package = package;
    }

    public override object GetValue(MemberInfo member, object instance)
    {
        var content = HttpTasks.HttpDownloadString($"https://www.myget.org/RSS/{feed}");
        return XmlTasks.XmlPeekFromString(content, ".//title")
            // TODO: regex?
            .First(x => x.Contains($"/{package} "))
            .Split('(').Last()
            .Split(')').First()
            .TrimStart("version ");
    }
}
