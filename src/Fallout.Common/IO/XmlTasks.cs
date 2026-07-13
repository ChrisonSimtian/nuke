using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Fallout.Common.IO;

public static class XmlTasks
{
    public static IEnumerable<string> XmlPeek(string path, string xpath, params (string prefix, string uri)[] namespaces)
    {
        return XmlPeek(XDocument.Load(path), xpath, namespaces);
    }

    public static IEnumerable<string> XmlPeekFromString(string content, string xpath,
        params (string prefix, string uri)[] namespaces)
    {
        return XmlPeek(XDocument.Parse(content), xpath, namespaces);
    }

    public static IEnumerable<XElement> XmlPeekElements(string path, string xpath,
        params (string prefix, string uri)[] namespaces)
    {
        return XmlPeekElements(XDocument.Load(path), xpath, namespaces);
    }

    public static IEnumerable<XElement> XmlPeekElementsFromString(string content, string xpath,
        params (string prefix, string uri)[] namespaces)
    {
        return XmlPeekElements(XDocument.Parse(content), xpath, namespaces);
    }

    public static string XmlPeekSingle(string path, string xpath, params (string prefix, string uri)[] namespaces)
    {
        return XmlPeekSingle(() => XmlPeek(path, xpath, namespaces));
    }

    public static string XmlPeekSingleFromString(string content, string xpath, params (string prefix, string uri)[] namespaces)
    {
        return XmlPeekSingle(() => XmlPeekFromString(content, xpath, namespaces));
    }

    public static void XmlPoke(string path, string xpath, object value, params (string prefix, string uri)[] namespaces)
    {
        XmlPoke(path, xpath, value, Encoding.UTF8, namespaces);
    }

    public static void XmlPoke(string path, string xpath, object value, Encoding encoding,
        params (string prefix, string uri)[] namespaces)
    {
        var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        var (elements, attributes) = GetObjects(document, xpath, namespaces);
        Assert.True((elements.Count == 1 || attributes.Count == 1) && !(elements.Count == 0 && attributes.Count == 0));

        elements.SingleOrDefault()?.SetValue(value);
        attributes.SingleOrDefault()?.SetValue(value);

        Save(document, path, encoding);
    }

    /// <summary>
    /// Adds a new element or content to the XML structure at the specified XPath.
    /// </summary>
    public static void XmlAdd(string path, string xpath, object content, params (string prefix, string uri)[] namespaces)
    {
        XmlAdd(path, xpath, content, Encoding.UTF8, namespaces);
    }

    /// <summary>
    /// Adds a new element or content to the XML structure at the specified XPath.
    /// </summary>
    public static void XmlAdd(string path, string xpath, object content, Encoding encoding,
        params (string prefix, string uri)[] namespaces)
    {
        var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        var (elements, attributes) = GetObjects(document, xpath, namespaces);

        var suffix = "(the element which should be parent of the newly added one)";
        Assert.True(attributes.Count == 0,
            $"XPath cannot select an attribute, because it must select exactly one element {suffix}.");

        Assert.True(elements.Count == 1,
            $"XPath '{xpath}' must select exactly one element {suffix}.");

        var element = elements.Single();

        var newContent = ContentToObject(content);
        var lastChild = element.Nodes().LastOrDefault();

        if (lastChild is XText lastText && lastText.Value.Contains('\n') && newContent is XElement)
        {
            var text = lastText.Value;
            var lastNewLine = text.LastIndexOf('\n');
            var indentation = text[(lastNewLine + 1)..];

            lastText.Value = text[..(lastNewLine + 1)] + indentation + indentation;
            element.Add(newContent);
            element.Add(new XText("\n" + indentation));
        }
        else
        {
            element.Add(newContent);
        }

        Save(document, path, encoding);
        return;

        XObject ContentToObject(object c)
        {
            return c switch
            {
                XmlElementBuilder builder => builder.Build(),
                XObject xObject => xObject,
                string stringContent when stringContent.TrimStart().StartsWith('<') => XElement.Parse(stringContent),
                _ => new XText(c.ToString() ?? "")
            };
        }
    }

    private static void Save(XDocument document, string path, Encoding encoding)
    {
        var writerSettings = new XmlWriterSettings
        {
            OmitXmlDeclaration = document.Declaration == null,
            Encoding = encoding
        };

        using var xmlWriter = XmlWriter.Create(path, writerSettings);
        document.Save(xmlWriter);
    }

    private static IEnumerable<string> XmlPeek(XDocument document, string xpath, (string prefix, string uri)[] namespaces)
    {
        var (elements, attributes) = GetObjects(document, xpath, namespaces);
        Assert.True(elements.Count == 0 || attributes.Count == 0);
        return elements.Count != 0 ? elements.Select(x => x.Value) : attributes.Select(x => x.Value);
    }

    private static IEnumerable<XElement> XmlPeekElements(XDocument document, string xpath,
        (string prefix, string uri)[] namespaces)
    {
        var (elements, attributes) = GetObjects(document, xpath, namespaces);
        Assert.True(elements.Count == 0 || attributes.Count == 0);
        return elements;
    }

    private static string XmlPeekSingle(Func<IEnumerable<string>> selector)
    {
        var values = selector.Invoke().ToList();
        Assert.True(values.Count <= 1);
        return values.SingleOrDefault();
    }

    private static (IReadOnlyCollection<XElement> Elements, IReadOnlyCollection<XAttribute> Attributes) GetObjects(
        XDocument document,
        string xpath,
        params (string prefix, string uri)[] namespaces)
    {
        XmlNamespaceManager xmlNamespaceManager = null;

        if (namespaces?.Length > 0)
        {
            var reader = document.CreateReader();
            if (reader.NameTable != null)
            {
                xmlNamespaceManager = new XmlNamespaceManager(reader.NameTable);
                foreach (var (prefix, uri) in namespaces)
                    xmlNamespaceManager.AddNamespace(prefix, uri);
            }
        }

        var objects = ((IEnumerable)document.XPathEvaluate(xpath, xmlNamespaceManager)).Cast<XObject>().ToList();
        return (objects.OfType<XElement>().ToList().AsReadOnly(),
            objects.OfType<XAttribute>().ToList().AsReadOnly());
    }
}
