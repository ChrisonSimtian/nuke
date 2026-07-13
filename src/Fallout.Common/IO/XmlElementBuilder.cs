using System;
using System.Xml.Linq;

namespace Fallout.Common.IO;

/// <summary>
/// A builder for creating <see cref="XElement"/> structures fluently.
/// </summary>
public class XmlElementBuilder(string name)
{
    private readonly XElement element = new(name);

    public XmlElementBuilder SetAttribute(string name, object value)
    {
        var attribute = element.Attribute(name);
        if (attribute == null)
        {
            if (value != null)
                element.Add(new XAttribute(name, value));
        }
        else
        {
            if (value == null)
                attribute.Remove();
            else
                attribute.SetValue(value);
        }
        return this;
    }

    public XmlElementBuilder SetValue(object value)
    {
        element.SetValue(value);
        return this;
    }

    public XmlElementBuilder AddChild(string name, Action<XmlElementBuilder> configurator = null)
    {
        var childBuilder = new XmlElementBuilder(name);
        configurator?.Invoke(childBuilder);
        element.Add(childBuilder.Build());
        return this;
    }

    public XmlElementBuilder AddChild(XElement element)
    {
        this.element.Add(element);
        return this;
    }

    public XElement Build()
    {
        return element;
    }

    public override string ToString()
    {
        return element.ToString();
    }

    public static implicit operator XElement(XmlElementBuilder builder)
    {
        return builder.Build();
    }
}
