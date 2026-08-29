// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Fallout.Persistence.Solution.Model;
using Fallout.Persistence.Solution.Serializer.Xml.XmlDecorators;

namespace Fallout.Persistence.Solution.Serializer.Xml;

/// <summary>
/// Initializes a new instance of the <see cref="SlnXmlModelExtension"/> class.
/// </summary>
[method: SetsRequiredMembers]
internal sealed class SlnXmlModelExtension(ISolutionSerializer serializer, SlnxSerializerSettings settings)
    : ISerializerModelExtension<SlnxSerializerSettings>
{
    [SetsRequiredMembers]
    internal SlnXmlModelExtension(ISolutionSerializer serializer, SlnxSerializerSettings settings, SlnxFile root)
        : this(serializer, settings)
    {
        Root = root;
    }

    /// <inheritdoc/>
    public required ISolutionSerializer Serializer { get; init; } = serializer;

    /// <inheritdoc/>
    public required SlnxSerializerSettings Settings { get; init; } = settings;

    /// <inheritdoc/>
    public bool Tarnished => Root?.Tarnished ?? false;

    internal SlnxFile? Root { get; init; }

    internal string? SolutionFileFullPath => Root?.FullPath;

    internal Version? Version => Root?.FileVersion;
}
