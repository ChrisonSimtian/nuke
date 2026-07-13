using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using FluentAssertions;
using Fallout.Common.IO;
using VerifyXunit;
using Xunit;

namespace Fallout.Common.Specs;

public class XmlTasksSpecs : IDisposable
{
    private readonly string _tempFile = Path.GetTempFileName();

    public void Dispose()
    {
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }

    [Fact]
    public void Loading_from_xml_string_works()
    {
        var elements = XmlTasks.XmlPeekElementsFromString(NugetConfig, "/configuration/packageSources").ToList();

        elements.Should().HaveCount(1);
        elements.Single().Elements().Should().HaveCount(2);
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

    [Fact]
    public void Adding_element_via_builder_updates_xml_file()
    {
        File.WriteAllText(_tempFile, @"<root><child>value</child></root>");

        XmlTasks.XmlAdd(_tempFile, "/root", new XmlElementBuilder("new").SetValue("element"));

        var content = File.ReadAllText(_tempFile);
        content.Should().Be(@"<root><child>value</child><new>element</new></root>");
    }

    [Fact]
    public void Adding_element_via_xml_string_updates_xml_file()
    {
        File.WriteAllText(_tempFile, @"<root><child>value</child></root>");

        XmlTasks.XmlAdd(_tempFile, "/root", "<new>element</new>");

        var content = File.ReadAllText(_tempFile);
        content.Should().Be(@"<root><child>value</child><new>element</new></root>");
    }

    [Fact]
    public void Fluent_api_builds_expected_xml_structure()
    {
        var builder = new XmlElementBuilder("root")
            .SetAttribute("attr", "val")
            .AddChild("child", c => c.SetValue("inner"));

        var element = builder.Build();
        element.Name.LocalName.Should().Be("root");
        element.Attribute("attr")?.Value.Should().Be("val");
        element.Element("child")?.Value.Should().Be("inner");
    }

    [Fact]
    public void Attributes_maintain_addition_order()
    {
        var builder = new XmlElementBuilder("root")
            .SetAttribute("z", "1")
            .SetAttribute("a", "2")
            .SetAttribute("m", "3");

        var element = builder.Build();
        var attributes = element.Attributes().Select(a => a.Name.LocalName).ToList();

        attributes.Should().ContainInOrder("z", "a", "m");
    }

    [Fact]
    public void Setting_attribute_to_null_removes_it()
    {
        var element = new XmlElementBuilder("root")
            .SetAttribute("a", "1")
            .SetAttribute("a", null)
            .Build();

        element.Attribute("a").Should().BeNull();
    }

    [Fact]
    public async Task A_real_world_use_case()
    {
        await File.WriteAllTextAsync(_tempFile, NugetConfig);

        XmlTasks.XmlAdd(_tempFile, "/configuration/packageSources", new XmlElementBuilder("add")
            .SetAttribute("key", "local")
            .SetAttribute("value", "../my-local-intermediate-nuget-feed")
            .SetAttribute("allowInsecureConnections", "true"));

        var content = await File.ReadAllTextAsync(_tempFile);
        await Verifier.Verify(content);
    }

    private static string NugetConfig =>
        """
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <packageSources>
            <clear />
            <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
          </packageSources>
          <packageSourceMapping>
            <clear />
            <packageSource key="nuget.org">
              <package pattern="*" />
            </packageSource>
          </packageSourceMapping>
        </configuration>
        """;
}
