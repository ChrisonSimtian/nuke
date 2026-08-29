// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Fallout.Persistence.Solution.Model;

/// <summary>
/// Represents the project configuration and build/deploy settings.
/// This is used to create an expanded mapping of every solution configuration to every project configuration.
/// </summary>
[method: SetsRequiredMembers]
internal readonly struct ProjectConfigMapping(string buildType, string platform, bool build, bool deploy)
{
    internal required string BuildType { get; init; } = buildType;

    internal required string Platform { get; init; } = platform;

    internal bool Build { get; init; } = build;

    internal bool Deploy { get; init; } = deploy;

    internal readonly bool IsValidBuildType => !string.IsNullOrEmpty(BuildType) && BuildType != BuildTypeNames.All;

    internal readonly bool IsValidPlatform => !string.IsNullOrEmpty(Platform) && Platform != PlatformNames.All;

    internal readonly bool IsSame(in ProjectConfigMapping other)
    {
        return other.Build == Build &&
               other.Deploy == Deploy &&
               StringComparer.Ordinal.Equals(BuildType, other.BuildType) &&
               (Platform == other.Platform || StringComparer.Ordinal.Equals(PlatformNames.Canonical(Platform),
                   PlatformNames.Canonical(other.Platform)));
    }
}
