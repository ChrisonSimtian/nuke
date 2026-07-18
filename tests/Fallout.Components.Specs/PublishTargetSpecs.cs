using System;
using System.Collections.Generic;
using FluentAssertions;
using Fallout.Common.IO;
using Xunit;

#pragma warning disable FALLOUT005 // exercising the experimental CD model (ADR-0009)

namespace Fallout.Components.Specs;

public class PublishTargetSpecs
{
    private static Artifact Packages() => new(ArtifactKind.Packages, "1.0.0", Array.Empty<AbsolutePath>());
    private static Artifact Docs() => new(ArtifactKind.Documentation, "1.0.0", Array.Empty<AbsolutePath>());

    [Fact]
    public void NuGet_feed_target_accepts_only_package_artifacts()
    {
        var target = new PublishTarget { Name = "nuget.org", Source = "s" };

        target.Accepts(Packages()).Should().BeTrue();
        target.Accepts(Docs()).Should().BeFalse();
    }

    [Fact]
    public void NuGet_feed_target_is_an_IPublishTarget()
        => new PublishTarget { Name = "n", Source = "s" }.Should().BeAssignableTo<IPublishTarget>();

    [Fact]
    public void Validate_throws_when_api_key_is_missing()
    {
        var keyless = new PublishTarget { Name = "nuget.org", Source = "s" };

        keyless.Invoking(x => x.Validate()).Should().Throw<Exception>();
    }

    [Fact]
    public void Validate_passes_when_api_key_is_present()
    {
        var keyed = new PublishTarget { Name = "nuget.org", Source = "s", ApiKey = "k" };

        keyed.Invoking(x => x.Validate()).Should().NotThrow();
    }

    [Theory]
    [InlineData(ArtifactKind.ReleaseNotes, true)]
    [InlineData(ArtifactKind.Packages, true)]
    [InlineData(ArtifactKind.Symbols, true)]
    [InlineData(ArtifactKind.SourceArchive, true)]
    [InlineData(ArtifactKind.Documentation, false)]
    [InlineData(ArtifactKind.Assemblies, false)]
    public void GitHub_release_target_accepts_release_bundle_kinds(ArtifactKind kind, bool expected)
    {
        var target = new GitHubReleaseTarget { Name = "github-releases" };

        target.Accepts(new Artifact(kind, "1.0.0", Array.Empty<AbsolutePath>())).Should().Be(expected);
    }

    [Fact]
    public void Environment_groups_targets_in_order()
    {
        var prod = new DeploymentEnvironment(
            Name: "Production",
            Order: 2,
            RequiresApproval: true,
            Targets: new IPublishTarget[]
            {
                new PublishTarget { Name = "nuget.org", Source = "s" },
                new GitHubReleaseTarget { Name = "github-releases" },
            });

        prod.RequiresApproval.Should().BeTrue();
        prod.Targets.Should().HaveCount(2);
    }

    [Fact]
    public void Channel_lists_eligible_environments()
    {
        var release = new Channel("release", new[] { "Dev", "Staging", "Production" });
        var preview = new Channel("preview", new[] { "Dev", "Staging" });

        release.Environments.Should().Contain("Production");
        preview.Environments.Should().NotContain("Production");
    }
}
