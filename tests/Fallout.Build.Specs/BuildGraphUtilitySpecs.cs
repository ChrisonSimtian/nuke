using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Fallout.Build.Execution.Extensions;
using Fallout.Common.Execution;
using FluentAssertions;
using VerifyXunit;
using Xunit;

namespace Fallout.Common.Specs.Execution;

/// <summary>
/// Contract tests for <c>build-graph.json</c>. This JSON is consumed by the VS Code extension, so the
/// shape must not drift silently — the verified snapshots below are the contract. A change that fails
/// them is a schema change and demands bumping <see cref="BuildGraphUtility.SchemaVersion"/> plus a
/// matching update on the extension side.
/// </summary>
public class BuildGraphUtilitySpecs
{
    private const string SampleVersion = "2026.1.0-preview.42";

    // A representative graph exercising every emitted field and relation kind.
    private static IReadOnlyCollection<ExecutableTarget> SampleGraph()
    {
        var restore = new ExecutableTarget { Name = "Restore", Listed = true };
        var compile = new ExecutableTarget
                      {
                          Name = "Compile",
                          Description = "Builds all projects",
                          Listed = true,
                          Member = MemberOf(nameof(SampleBuild.Compile)),
                      };
        var test = new ExecutableTarget { Name = "Test", Listed = true, IsDefault = true };
        var publish = new ExecutableTarget { Name = "Publish", Listed = false };

        compile.ExecutionDependencies.Add(restore);
        test.ExecutionDependencies.Add(compile);
        test.OrderDependencies.Add(restore);
        publish.TriggerDependencies.Add(test);
        compile.Triggers.Add(publish);

        // Deliberately unsorted so the ordinal ordering guarantee is exercised.
        return new[] { test, publish, compile, restore };
    }

    [Fact]
    public Task Sample_graph_matches_the_contract_snapshot()
        => Verifier.Verify(BuildGraphUtility.GetJsonString(SampleGraph(), SampleVersion), "json");

    [Fact]
    public Task Empty_graph_matches_the_contract_snapshot()
        => Verifier.Verify(BuildGraphUtility.GetJsonString(new ExecutableTarget[0], falloutVersion: null), "json");

    [Fact]
    public void Schema_version_is_1()
    {
        // A change here is a breaking contract change — update the VS Code extension's
        // SUPPORTED_SCHEMA_VERSION and this guard together, deliberately.
        BuildGraphUtility.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void Targets_are_ordered_by_name_ordinally()
    {
        var model = BuildGraphUtility.GetModel(SampleGraph(), SampleVersion);

        model.Targets.Select(x => x.Name).Should().Equal("Compile", "Publish", "Restore", "Test");
    }

    [Fact]
    public void Relation_kinds_map_to_their_own_fields()
    {
        var compile = ModelFor("Compile");
        var test = ModelFor("Test");
        var publish = ModelFor("Publish");

        compile.DependsOn.Should().Equal("Restore");
        compile.Triggers.Should().Equal("Publish");
        test.DependsOn.Should().Equal("Compile");
        test.After.Should().Equal("Restore");
        publish.TriggeredBy.Should().Equal("Test");
    }

    [Fact]
    public void Dependency_lists_are_ordered_ordinally()
    {
        var target = new ExecutableTarget { Name = "Root" };
        target.ExecutionDependencies.Add(new ExecutableTarget { Name = "Zebra" });
        target.ExecutionDependencies.Add(new ExecutableTarget { Name = "Alpha" });
        target.ExecutionDependencies.Add(new ExecutableTarget { Name = "Mango" });

        var model = BuildGraphUtility.GetModel(new[] { target }, SampleVersion).Targets.Single();

        model.DependsOn.Should().Equal("Alpha", "Mango", "Zebra");
    }

    [Fact]
    public void Default_and_listed_flags_are_projected()
    {
        var test = ModelFor("Test");
        var publish = ModelFor("Publish");

        test.Default.Should().BeTrue();
        test.Listed.Should().BeTrue();
        publish.Default.Should().BeFalse();
        publish.Listed.Should().BeFalse();
    }

    [Fact]
    public void DeclaredIn_is_the_declaring_type_simple_name_or_null()
    {
        ModelFor("Compile").DeclaredIn.Should().Be(nameof(SampleBuild));
        // Restore has no backing member, so there is nothing to disambiguate go-to-definition with.
        ModelFor("Restore").DeclaredIn.Should().BeNull();
    }

    [Fact]
    public void Optional_string_fields_are_emitted_as_null_rather_than_omitted()
    {
        // The extension's Target interface marks description/declaredIn optional; we keep the keys
        // present (as null) for a stable shape, so consumers can rely on the property existing.
        using var doc = JsonDocument.Parse(BuildGraphUtility.GetJsonString(SampleGraph(), falloutVersion: null));

        doc.RootElement.GetProperty("falloutVersion").ValueKind.Should().Be(JsonValueKind.Null);

        var restore = doc.RootElement.GetProperty("targets").EnumerateArray()
            .Single(x => x.GetProperty("name").GetString() == "Restore");
        restore.GetProperty("description").ValueKind.Should().Be(JsonValueKind.Null);
        restore.GetProperty("declaredIn").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void Root_and_target_property_names_are_camelCase()
    {
        using var doc = JsonDocument.Parse(BuildGraphUtility.GetJsonString(SampleGraph(), SampleVersion));

        doc.RootElement.EnumerateObject().Select(x => x.Name)
            .Should().Equal("version", "falloutVersion", "targets");

        var firstTarget = doc.RootElement.GetProperty("targets").EnumerateArray().First();
        firstTarget.EnumerateObject().Select(x => x.Name)
            .Should().Equal(
                "name", "description", "declaredIn", "default", "listed",
                "dependsOn", "after", "triggeredBy", "triggers");
    }

    [Theory]
    [InlineData("2026.1.0-preview.42+abc123", "2026.1.0-preview.42")]
    [InlineData("2026.1.0", "2026.1.0")]
    [InlineData("10.0.0-rc.1", "10.0.0-rc.1")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void NormalizeVersion_strips_build_metadata(string input, string expected)
    {
        BuildGraphUtility.NormalizeVersion(input).Should().Be(expected);
    }

    private static BuildGraphUtility.TargetModel ModelFor(string name)
        => BuildGraphUtility.GetModel(SampleGraph(), SampleVersion).Targets.Single(x => x.Name == name);

    private static MemberInfo MemberOf(string name)
        => typeof(SampleBuild).GetProperty(name, BindingFlags.Instance | BindingFlags.Public);

    // Backing type whose name flows into `declaredIn`.
    private class SampleBuild
    {
        public object Compile => null;
    }
}
