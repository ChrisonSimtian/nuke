using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fallout.Common;
using Fallout.Common.Execution;
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

    [Fact]
    public void Describe_is_requested_by_the_describe_flag_alone()
    {
        BuildIntrospectionService.IsRequested(new SampleBuild { Describe = true }).Should().BeTrue();
    }

    [Fact]
    public void Plan_json_is_requested_only_when_both_flags_are_set()
    {
        // --plan on its own keeps its existing behaviour: the HTML graph, opened in a browser.
        BuildIntrospectionService.IsRequested(new SampleBuild { Plan = true }).Should().BeFalse();
        BuildIntrospectionService.IsRequested(new SampleBuild { Json = true }).Should().BeFalse();
        BuildIntrospectionService.IsRequested(new SampleBuild { Plan = true, Json = true }).Should().BeTrue();
    }

    [Fact]
    public void An_ordinary_run_requests_no_introspection()
    {
        BuildIntrospectionService.IsRequested(new SampleBuild()).Should().BeFalse();
    }

    [Fact]
    public void Describe_document_carries_targets_and_parameters_and_parses_as_json()
    {
        var json = BuildIntrospectionService.GetDescribeJson(
            new SampleBuild(), SampleGraph(), SampleVersion);

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
        var json = BuildIntrospectionService.GetDescribeJson(
            new SampleBuild(), SampleGraph(), SampleVersion);

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

        var json = BuildIntrospectionService.GetPlanJson(
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

        var json = BuildIntrospectionService.GetPlanJson(
            new[] { "Compile" }, new[] { restore, compile }, new[] { "re-store" });

        using var document = JsonDocument.Parse(json);
        var entries = document.RootElement.GetProperty("plan");

        // BuildExecutor strips dashes before matching, so --skip re-store hits Restore.
        entries[0].GetProperty("skip").GetString().Should().Be("via parameter");
        entries[1].GetProperty("skip").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void An_empty_skip_list_skips_every_target_as_the_executor_does()
    {
        var json = BuildIntrospectionService.GetPlanJson(
            new[] { "Compile" },
            new[] { new ExecutableTarget { Name = "Restore" }, new ExecutableTarget { Name = "Compile" } },
            new string[0]);

        using var document = JsonDocument.Parse(json);

        document.RootElement.GetProperty("plan").EnumerateArray()
            .Select(x => x.GetProperty("skip").GetString())
            .Should().AllBe("via parameter");
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
}
