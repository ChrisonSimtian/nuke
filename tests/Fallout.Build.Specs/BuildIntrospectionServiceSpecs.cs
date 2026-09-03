using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fallout.Common;
using Fallout.Common.Execution;
using Fallout.Common.Tooling;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs.Execution;

/// <summary>
/// Covers the read-only introspection requests — <c>--describe</c> and <c>--plan --json</c> — that
/// short-circuit <see cref="BuildManager" /> above <see cref="ToolRequirementService" />. The
/// documents are a machine-facing contract, so they are asserted as parsed JSON rather than text.
/// </summary>
public class BuildIntrospectionServiceSpecs
{
    private const string SampleVersion = "2026.1.0-preview.42";

    // The flags a run resolves once, restated here so each spec builds the service the way
    // BuildManager does. Version is pinned so the asserted document never moves with the assembly.
    private static BuildIntrospectionService Describing()
        => new(describe: true, planAsJson: false, SampleVersion);

    private static BuildIntrospectionService Planning()
        => new(describe: false, planAsJson: true, SampleVersion);

    private static bool IsRequestedFor(FalloutBuild build)
        => BuildIntrospectionService.For(build).IsRequestedForRun;

    [Fact]
    public void Describe_is_requested_by_the_describe_flag_alone()
    {
        IsRequestedFor(new SampleBuild { Describe = true }).Should().BeTrue();
    }

    [Fact]
    public void Plan_json_is_requested_only_when_both_flags_are_set()
    {
        // --plan on its own keeps its existing behaviour: the HTML graph, opened in a browser.
        IsRequestedFor(new SampleBuild { Plan = true }).Should().BeFalse();
        IsRequestedFor(new SampleBuild { Json = true }).Should().BeFalse();
        IsRequestedFor(new SampleBuild { Plan = true, Json = true }).Should().BeTrue();
    }

    [Fact]
    public void An_ordinary_run_requests_no_introspection()
    {
        IsRequestedFor(new SampleBuild()).Should().BeFalse();
    }

    [Theory]
    [InlineData(true, "--describe")]
    [InlineData(true, "-describe")]
    [InlineData(true, "--DESCRIBE")]
    [InlineData(true, "Compile", "--describe")]
    [InlineData(true, "--plan", "--json")]
    [InlineData(false, "--plan")]
    [InlineData(false, "--json")]
    [InlineData(false, "Compile")]
    // A target that merely reads like the flag must not be mistaken for one.
    [InlineData(false, "describe")]
    public void Raw_arguments_are_recognised_the_same_way_the_injected_flags_are(
        bool expected,
        params string[] arguments)
    {
        BuildIntrospectionService.IsRequested(arguments).Should().Be(expected);
    }

    [Fact]
    public void Describe_document_carries_targets_and_parameters_and_parses_as_json()
    {
        var json = Describing().GetDescribeJson(new SampleBuild(), SampleGraph());

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;


        root.GetProperty("version").GetInt32().Should().Be(1);
        root.GetProperty("falloutVersion").GetString().Should().Be(SampleVersion);
        root.GetProperty("targets").EnumerateArray().Select(x => x.GetProperty("name").GetString())
            .Should().Equal("Compile", "Restore");
        root.GetProperty("parameters").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public void Describe_document_projects_the_build_s_own_parameters()
    {
        var json = Describing().GetDescribeJson(new SampleBuild(), SampleGraph());

        using var document = JsonDocument.Parse(json);
        var names = document.RootElement.GetProperty("parameters").EnumerateArray()
            .Select(x => x.GetProperty("name").GetString()).ToList();

        names.Should().Contain("api-key");
    }

    [Fact]
    public void Plan_document_preserves_order_and_never_evaluates_conditions()
    {
        var evaluated = false;
        var restore = new ExecutableTarget { Name = "Restore" };
        var compile = new ExecutableTarget { Name = "Compile", Invoked = true };
        compile.StaticConditions.Add(("IsServerBuild", () => { evaluated = true; return true; }));

        var json = Planning().GetPlanJson(
            new[] { "Compile" }, new[] { restore, compile }, skippedTargets: null);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("version").GetInt32().Should().Be(1);
        root.GetProperty("invokedTargets").EnumerateArray().Select(x => x.GetString())
            .Should().Equal("Compile");

        var entries = root.GetProperty("plan");
        entries[0].GetProperty("name").GetString().Should().Be("Restore");
        entries[0].GetProperty("order").GetInt32().Should().Be(0);
        entries[0].GetProperty("invoked").GetBoolean().Should().BeFalse();
        entries[0].GetProperty("skip").ValueKind.Should().Be(JsonValueKind.Null);

        entries[1].GetProperty("name").GetString().Should().Be("Compile");
        entries[1].GetProperty("order").GetInt32().Should().Be(1);
        entries[1].GetProperty("invoked").GetBoolean().Should().BeTrue();
        entries[1].GetProperty("staticConditions")[0].GetString().Should().Be("IsServerBuild");

        evaluated.Should().BeFalse("the plan reports what gates a target, never the gate's value");
    }

    [Fact]
    public void A_named_skipped_target_carries_the_executor_s_own_reason()
    {
        var restore = new ExecutableTarget { Name = "Restore" };
        var compile = new ExecutableTarget { Name = "Compile" };

        var json = Planning().GetPlanJson(
            new[] { "Compile" }, new[] { restore, compile }, new[] { "re-store" });

        using var document = JsonDocument.Parse(json);
        var entries = document.RootElement.GetProperty("plan");

        // BuildExecutor strips dashes before matching, so --skip re-store hits Restore.
        entries[0].GetProperty("skip").GetString().Should().Be("via parameter");
        entries[1].GetProperty("skip").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void An_empty_skip_list_skips_every_target_except_the_invoked_ones()
    {
        var json = Planning().GetPlanJson(
            new[] { "Compile" },
            new[]
            {
                new ExecutableTarget { Name = "Restore" },
                new ExecutableTarget { Name = "Compile", Invoked = true },
            },
            new string[0]);

        using var document = JsonDocument.Parse(json);
        var entries = document.RootElement.GetProperty("plan");

        entries[0].GetProperty("skip").GetString().Should().Be("via parameter");
        entries[1].GetProperty("skip").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void An_explicitly_invoked_target_is_never_reported_as_skipped()
    {
        // BuildExecutor.MarkTargetSkipped only skips when !target.Invoked, so naming an invoked
        // target in --skip does not stop it running. The predicted plan has to say the same.
        var json = Planning().GetPlanJson(
            new[] { "Compile" },
            new[] { new ExecutableTarget { Name = "Compile", Invoked = true } },
            new[] { "Compile" });

        using var document = JsonDocument.Parse(json);

        document.RootElement.GetProperty("plan")[0]
            .GetProperty("skip").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void Error_envelope_names_the_exception_kind_and_message()
    {
        var json = Planning().GetErrorJson(new InvalidOperationException("boom"));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("version").GetInt32().Should().Be(1);
        var error = root.GetProperty("error");
        error.GetProperty("kind").GetString().Should().Be(nameof(InvalidOperationException));
        error.GetProperty("message").GetString().Should().Be("boom");
    }

    [Fact]
    public void Describe_document_carries_the_build_s_class_level_tool_requirements()
    {
        // Regression guard for a gap the old shape hid. The specs used to call a version-taking
        // overload that projected targets and parameters but NOT BuildRequirements, so the document
        // asserted here was not the one production emitted and this projection went uncovered.
        // Both now go down one path, and a class-level [Requires<T>] reaches the document.
        var json = Describing().GetDescribeJson(new RequiringBuild(), SampleGraph());

        using var document = JsonDocument.Parse(json);
        var requirements = document.RootElement.GetProperty("toolRequirements");

        requirements.EnumerateArray().Select(x => x.GetProperty("packageId").GetString())
            .Should().Equal("GitVersion.Tool");
    }

    private static IReadOnlyCollection<ExecutableTarget> SampleGraph()
    {
        var restore = new ExecutableTarget { Name = "Restore", Listed = true };
        var compile = new ExecutableTarget { Name = "Compile", Listed = true };
        compile.ExecutionDependencies.Add(restore);
        return new[] { restore, compile };
    }

    private class SampleBuild : FalloutBuild
    {
        [Parameter("An API key.")]
        private readonly string ApiKey;
    }

    // A stand-in tool: the specs project references Fallout.Build, not the generated wrappers in
    // Fallout.Common, and [Requires<T>] only needs T to carry a ToolAttribute.
    [NuGetTool(Id = "GitVersion.Tool")]
    private class FakeTool : IRequireNuGetPackage;

    // [Requires<T>] targets Class/Interface only, so this is the sole way a build-level tool
    // requirement can reach the describe document.
    [Requires<FakeTool>(Version = "5.12.0")]
    private class RequiringBuild : FalloutBuild;
}
