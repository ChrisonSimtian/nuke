using System;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.CI.GitHubActions.Configuration;
using Fallout.Common.Execution;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs.CI;

public class GitHubActionsActionReferenceSpecs
{
    private const string Sha = "11bd7190b47010a048f0e0e5c8ea9e6b2e0b5d3a";

    // Stands in for what a real caller passes: the property carrying the value, and the workflow it's on.
    private const string Origin = "'CheckoutAction' in workflow 'test'";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Unset_value_falls_back_to_the_default(string value)
    {
        var reference = GitHubActionsActionReference.Resolve(GitHubActionsDefaults.CheckoutAction, value, Origin);

        reference.Should().Be(GitHubActionsDefaults.CheckoutAction);
    }

    [Theory]
    [InlineData("v8")]
    [InlineData("@v8")]
    [InlineData(" v8 ")]
    public void Bare_ref_is_appended_to_the_default_action_name(string value)
    {
        var reference = GitHubActionsActionReference.Resolve(GitHubActionsDefaults.CheckoutAction, value, Origin);

        reference.Should().Be("actions/checkout@v8");
    }

    [Fact]
    public void Bare_sha_with_a_trailing_comment_keeps_the_comment()
    {
        var reference = GitHubActionsActionReference.Resolve(
            GitHubActionsDefaults.CheckoutAction, $"{Sha} # v7.1.0", Origin);

        reference.Should().Be($"actions/checkout@{Sha} # v7.1.0");
    }

    // A comment is split off before the reference is classified, so a slash or an '@' inside it can't be
    // mistaken for an owner/repo or a version.
    [Fact]
    public void Comment_is_not_taken_into_account_when_classifying()
    {
        var value = $"{Sha} # https://github.com/actions/checkout/releases/tag/v7.1.0";

        var reference = GitHubActionsActionReference.Resolve(GitHubActionsDefaults.CheckoutAction, value, Origin);

        reference.Should().Be($"actions/checkout@{value}");
    }

    [Theory]
    [InlineData("actions/checkout@v8")]
    [InlineData("my-org/checkout@v9")]
    [InlineData("my-org/actions/checkout@v9")]
    public void Complete_reference_is_emitted_verbatim(string value)
    {
        var reference = GitHubActionsActionReference.Resolve(GitHubActionsDefaults.CheckoutAction, value, Origin);

        reference.Should().Be(value);
    }

    [Fact]
    public void Complete_reference_with_a_sha_pin_is_emitted_verbatim()
    {
        var value = $"actions/checkout@{Sha} # v7.1.0";

        var reference = GitHubActionsActionReference.Resolve(GitHubActionsDefaults.CheckoutAction, value, Origin);

        reference.Should().Be(value);
    }

    // A slash-bearing ref reads exactly like an owner/repo, so it needs the explicit '@' marker.
    [Theory]
    [InlineData("@releases/v1", "actions/checkout@releases/v1")]
    [InlineData("@feature/node24", "actions/checkout@feature/node24")]
    public void Explicit_bare_ref_may_contain_a_slash(string value, string expected)
    {
        var reference = GitHubActionsActionReference.Resolve(GitHubActionsDefaults.CheckoutAction, value, Origin);

        reference.Should().Be(expected);
    }

    [Theory]
    [InlineData("./.github/actions/checkout")]
    [InlineData("docker://alpine:3.20")]
    public void Local_and_container_references_are_passed_through(string value)
    {
        var reference = GitHubActionsActionReference.Resolve(GitHubActionsDefaults.CheckoutAction, value, Origin);

        reference.Should().Be(value);
    }

    [Theory]
    [InlineData("actions/checkout")] // a complete reference missing its version
    [InlineData("releases/v1")] // a bare ref that reads like an owner/repo
    public void Slash_without_a_ref_is_rejected_as_ambiguous(string value)
    {
        var act = () => GitHubActionsActionReference.Resolve(GitHubActionsDefaults.CheckoutAction, value, Origin);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("my-org/checkout # pinned by @ops-team")] // the '@' lives in the comment, not the ref
    [InlineData("# v7.1.0")] // comment only, no reference at all
    [InlineData("actions/checkout@")] // '@' with nothing after it
    [InlineData("@")]
    public void Reference_without_a_usable_ref_throws(string value)
    {
        var act = () => GitHubActionsActionReference.Resolve(GitHubActionsDefaults.CheckoutAction, value, Origin);

        act.Should().Throw<ArgumentException>();
    }

    // Emitted into an unquoted YAML scalar, so a second ': ' would corrupt the whole workflow file.
    [Theory]
    [InlineData("actions/cache@v6 with: enableCrossOsArchive")]
    [InlineData("actions/checkout@v8\nmalicious: true")]
    [InlineData("v8\r  with:")]
    [InlineData("v8\ttrailing")]
    public void Reference_that_would_corrupt_the_yaml_throws(string value)
    {
        var act = () => GitHubActionsActionReference.Resolve(GitHubActionsDefaults.CheckoutAction, value, Origin);

        act.Should().Throw<ArgumentException>();
    }

    // A rejected reference names the property and workflow that carry it, so the message points at the
    // declaration to fix rather than just the offending string.
    [Fact]
    public void Rejection_message_names_the_origin_of_the_value()
    {
        var act = () => GitHubActionsActionReference.Resolve(
            GitHubActionsDefaults.CheckoutAction, "releases/v1", "'CheckoutAction' in workflow 'build'");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*'CheckoutAction' in workflow 'build'*");
    }

    [Fact]
    public void Each_step_resolves_against_its_own_default()
    {
        new GitHubActionsCheckoutStep
        {
            Uses = "v8"
        }.Uses.Should().Be("actions/checkout@v8");

        new GitHubActionsCacheStep
        {
            Uses = "v4"
        }.Uses.Should().Be("actions/cache@v4");

        new GitHubActionsArtifactStep
        {
            Uses = "v8"
        }.Uses.Should().Be("actions/upload-artifact@v8");

        new GitHubActionsRunStep
        {
            SetupDotNetAction = "v7"
        }.SetupDotNetAction.Should().Be("actions/setup-dotnet@v7");
    }

    [Fact]
    public void Steps_default_to_the_pinned_versions()
    {
        new GitHubActionsCheckoutStep().Uses.Should().Be(GitHubActionsDefaults.CheckoutAction);
        new GitHubActionsCacheStep().Uses.Should().Be(GitHubActionsDefaults.CacheAction);
        new GitHubActionsArtifactStep().Uses.Should().Be(GitHubActionsDefaults.UploadArtifactAction);
        new GitHubActionsRunStep().SetupDotNetAction.Should().Be(GitHubActionsDefaults.SetupDotNetAction);
    }

    [Fact]
    public void Resetting_a_step_to_null_restores_its_default()
    {
        var step = new GitHubActionsCheckoutStep
        {
            Uses = "v8"
        };

        step.Uses = null;

        step.Uses.Should().Be(GitHubActionsDefaults.CheckoutAction);
    }

    // The cache and artifact steps are conditional, so their overrides must still be validated when the
    // step isn't emitted — otherwise the typo only surfaces when caching or publishing is re-enabled.
    [Fact]
    public void Cache_override_is_validated_even_when_no_cache_step_is_emitted()
    {
        var act = () => GenerateWorkflow(attribute =>
        {
            attribute.CacheKeyFiles = new string[0];
            attribute.CacheAction = "actions/cache";
        });

        act.Should().Throw<ArgumentException>().WithMessage($"*{nameof(GitHubActionsAttribute.CacheAction)}*");
    }

    [Fact]
    public void Artifact_override_is_validated_even_when_artifacts_are_disabled()
    {
        var act = () => GenerateWorkflow(attribute =>
        {
            attribute.PublishArtifacts = false;
            attribute.UploadArtifactAction = "my-org/upload-artifact";
        });

        act.Should().Throw<ArgumentException>()
            .WithMessage($"*{nameof(GitHubActionsAttribute.UploadArtifactAction)}*");
    }

    // The failing property and workflow are named, so the message points at the edit that broke it.
    [Fact]
    public void Validation_message_names_the_workflow()
    {
        var act = () => GenerateWorkflow(attribute => attribute.CheckoutAction = "releases/v1");

        act.Should().Throw<ArgumentException>().WithMessage("*'test'*");
    }

    [Fact]
    public void Well_formed_overrides_do_not_throw()
    {
        var act = () => GenerateWorkflow(attribute =>
        {
            attribute.CheckoutAction = "@releases/v1";
            attribute.CacheAction = "v4";
            attribute.SetupDotNetAction = "actions/setup-dotnet@v7";
            attribute.UploadArtifactAction = $"my-org/upload-artifact@{Sha} # v7.1.0";
        });

        act.Should().NotThrow();
    }

    /// <summary>
    /// Runs the generator over a minimal push-triggered workflow named <c>test</c>, after
    /// <paramref name="configureAttribute"/> has set the properties under test. Generation is what triggers
    /// validation, so this is how a spec observes an override being rejected — or accepted.
    /// </summary>
    private static void GenerateWorkflow(Action<TestGitHubActionsAttribute> configureAttribute)
    {
        var build = new ConfigurationGenerationSpecs.TestBuild();
        var relevantTargets = ExecutableTargetFactory.CreateAll(build, x => x.Compile);

        var attribute = new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
        {
            On = new[]
            {
                GitHubActionsTrigger.Push
            },
            InvokedTargets = new[]
            {
                nameof(ConfigurationGenerationSpecs.TestBuild.Test)
            }
        };

        configureAttribute(attribute);
        attribute.Build = build;

        attribute.GetConfiguration(relevantTargets);
    }
}
