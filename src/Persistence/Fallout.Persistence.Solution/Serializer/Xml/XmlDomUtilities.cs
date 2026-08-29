// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;

namespace Fallout.Persistence.Solution.Serializer.Xml;

internal static class XmlDomUtilities
{
    public static XmlElementAttributes Attributes(this XmlElement? element) => new(element?.Attributes);

    public static XmlElementSubElementsEnumerable ChildElements(this XmlNode? element) => new(element, filterByName: null);
}
