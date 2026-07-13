using System;
using System.Linq;
using System.Xml;
using Fallout.Common.IO;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs;

public class XmlTasksSpecs
{
    [Fact]
    public void Loading_from_xml_string_works()
    {
        var content = @"<root><element>value</element></root>";
        var elements = XmlTasks.XmlPeekElementsFromString(content, "/root/element").ToList();

        elements.Should().HaveCount(1);
        elements.Single().Value.Should().Be("value");
    }

    [Fact]
    public void Loading_from_file_path_throws()
    {
        var content = "C:\\temp\\test.xml";

        Action action = () => XmlTasks.XmlPeekElementsFromString(content, "/root/element");

        action.Should().Throw<XmlException>();
    }

    [Fact]
    public void Loading_from_url_throws()
    {
        var content = "https://example.com/test.xml";

        Action action = () => XmlTasks.XmlPeekElementsFromString(content, "/root/element");

        action.Should().Throw<XmlException>();
    }
}
