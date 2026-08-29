// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Fallout.Persistence.Solution.Model;
using Fallout.Persistence.Solution.Serializer.Xml.XmlDecorators;

namespace Fallout.Persistence.Solution.Serializer.Xml;

internal sealed partial class SlnXmlSerializer
{
    private sealed class Reader
    {
        private readonly string? fullPath;
        private readonly XmlDocument xmlDocument;

        internal Reader(string? fullPath, Stream readerStream)
        {
            this.fullPath = fullPath;

            // We ideally want to preserver whitespace, but if this is on
            // we need to manually handle preserving all indenting and new lines
            // when elements are added or removed.
            xmlDocument = new LineInfoXmlDocument
            {
                PreserveWhitespace = true
            };

            xmlDocument.Load(readerStream);
        }

        internal SolutionModel Parse()
        {
            SlnxFile slnxFile = new(xmlDocument, new SlnxSerializerSettings(), null, fullPath);
            return slnxFile.ToModel();
        }
    }
}
