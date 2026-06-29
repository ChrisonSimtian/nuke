using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Fallout.Core.Planning;
using Xunit;

namespace Fallout.Core.Specs;

public class TopoSortSpecs
{
    // Edges read as "depends on". Anything not listed has no dependencies.
    private static PlanResult<string> Order(
        IReadOnlyCollection<string> nodes,
        Dictionary<string, string[]> dependencies,
        bool strict = false) =>
        TopoSort.Order(
            nodes,
            n => dependencies.TryGetValue(n, out var deps) ? deps : Enumerable.Empty<string>(),
            strict);

    [Fact]
    public void Orders_a_chain_roots_first()
    {
        // C depends on B depends on A.
        var plan = Order(
            new[] { "A", "B", "C" },
            new Dictionary<string, string[]> { ["B"] = new[] { "A" }, ["C"] = new[] { "B" } });

        plan.HasCycles.Should().BeFalse();
        // Roots (nothing depends on them) come first; a node always precedes its dependencies.
        plan.Ordered.Should().Equal("C", "B", "A");
    }

    [Fact]
    public void Detects_a_cycle()
    {
        var plan = Order(
            new[] { "A", "B" },
            new Dictionary<string, string[]> { ["A"] = new[] { "B" }, ["B"] = new[] { "A" } });

        plan.HasCycles.Should().BeTrue();
        plan.Ordered.Should().BeEmpty();
        plan.Cycles.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new[] { "A", "B" });
    }

    [Fact]
    public void Ignores_edges_to_unknown_nodes()
    {
        var plan = Order(
            new[] { "A" },
            new Dictionary<string, string[]> { ["A"] = new[] { "ghost" } });

        plan.HasCycles.Should().BeFalse();
        plan.Ordered.Should().Equal("A");
    }

    [Fact]
    public void Strict_mode_flags_an_ambiguous_step()
    {
        // Two independent roots, no ordering between them.
        var plan = Order(
            new[] { "A", "B" },
            new Dictionary<string, string[]>(),
            strict: true);

        plan.IsAmbiguous.Should().BeTrue();
        plan.AmbiguousStep.Should().BeEquivalentTo(new[] { "A", "B" });
    }

    [Fact]
    public void Non_strict_mode_never_flags_ambiguity()
    {
        var plan = Order(
            new[] { "A", "B" },
            new Dictionary<string, string[]>());

        plan.IsAmbiguous.Should().BeFalse();
        plan.Ordered.Should().BeEquivalentTo(new[] { "A", "B" });
    }
}
