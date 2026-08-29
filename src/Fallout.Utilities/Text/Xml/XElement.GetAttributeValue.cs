using System.Xml.Linq;

namespace Fallout.Common.Utilities;

public static class XElementExtensions
{
    public static string GetAttributeValue(this XElement element, string name)
    {
        return element.Attribute(name).NotNull().Value;
    }
}
