// Copyright 2026 Maintainers of Fallout.
// Originally based on NUKE by Matthias Koch and contributors.
// Distributed under the MIT License.
// https://github.com/Fallout-build/Fallout/blob/main/LICENSE

using FluentAssertions;
using Fallout.Common.Tools.GitVersion;
using Fallout.Common.Utilities;
using Xunit;

namespace Fallout.Common.Tests;

/// <summary>
/// Regression tests for GitVersion JSON deserialisation (issue #218).
/// GitVersion 6.x emits several fields as bare JSON numbers instead of quoted strings.
/// </summary>
public class GitVersionParseTest
{
    // Exact payload from issue #218 — BuildMetaData, CommitsSinceVersionSource,
    // PreReleaseNumber and WeightedPreReleaseNumber are numbers, not strings.
    private const string GitVersion6Json = """
        {
          "AssemblySemFileVer": "8.10.1.0",
          "AssemblySemVer": "8.10.1.0",
          "BranchName": "switch-to-fallout",
          "BuildMetaData": 7,
          "CommitDate": "2026-05-26",
          "CommitsSinceVersionSource": 7,
          "EscapedBranchName": "switch-to-fallout",
          "FullBuildMetaData": "7.Branch.switch-to-fallout.Sha.6419fa509c7934f0b34a0ea5e5306c44c0a9a259",
          "FullSemVer": "8.10.1-switch-to-fallout.1+7",
          "InformationalVersion": "8.10.1-switch-to-fallout.1+7.Branch.switch-to-fallout.Sha.6419fa509c7934f0b34a0ea5e5306c44c0a9a259",
          "Major": 8,
          "MajorMinorPatch": "8.10.1",
          "Minor": 10,
          "Patch": 1,
          "PreReleaseLabel": "switch-to-fallout",
          "PreReleaseLabelWithDash": "-switch-to-fallout",
          "PreReleaseNumber": 1,
          "PreReleaseTag": "switch-to-fallout.1",
          "PreReleaseTagWithDash": "-switch-to-fallout.1",
          "SemVer": "8.10.1-switch-to-fallout.1",
          "Sha": "6419fa509c7934f0b34a0ea5e5306c44c0a9a259",
          "ShortSha": "6419fa5",
          "UncommittedChanges": 13,
          "VersionSourceDistance": 7,
          "VersionSourceIncrement": "None",
          "VersionSourceSemVer": "8.10.0",
          "VersionSourceSha": "0954811776a71005283221b91aabafc5fec332b7",
          "WeightedPreReleaseNumber": 1
        }
        """;
    
    [Fact]
    public void Can_parse_a_v6_response()
    {
        var result = GitVersion6Json.GetJson<GitVersion>();

        result.Should().BeEquivalentTo(new
        {
            BuildMetaData = "7",
            CommitsSinceVersionSource = "7",
            PreReleaseNumber = "1",
            WeightedPreReleaseNumber = "1",
            Major = 8,
            Minor = 10,
            Patch = 1,
            BranchName = "switch-to-fallout",
            FullSemVer = "8.10.1-switch-to-fallout.1+7",
            UncommittedChanges = 13
        });
    }
}
