// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;

namespace Fallout.Persistence.Solution.Serializer.Xml;

/// <summary>
/// Provides a way to enumerate over xml child elements.
/// </summary>
internal ref struct XmlElementSubElements(XmlNode? element, string? filterByName)
{
    private XmlNode? child;

    public readonly XmlElement Current => (ReferenceEquals(child, element) ? null : child as XmlElement)!;

    public bool MoveNext()
    {
        // use element as "sentinel end value", null as before first. (if element is null it is also an end as coincidence).
        if (ReferenceEquals(child, element) || element is null)
        {
            return false;
        }

        do
        {
            child = child is null ? element.FirstChild : child.NextSibling;
            if (child is XmlElement)
            {
                if (filterByName is null || child.Name == filterByName)
                {
                    return true;
                }
            }
        }
        while (child is not null);

        child = element;
        return false;
    }
}
