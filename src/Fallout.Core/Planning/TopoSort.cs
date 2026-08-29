using System;
using System.Collections.Generic;
using System.Linq;

namespace Fallout.Core.Planning;

/// <summary>
/// The outcome of a topological ordering: the ordered nodes, any dependency cycles that were
/// detected, and — in strict mode — the first set of mutually-independent nodes that made the
/// ordering ambiguous. Pure data; computing it never throws and never mutates the input.
/// </summary>
public sealed class PlanResult<T>
{
    public PlanResult(
        IReadOnlyList<T> ordered,
        IReadOnlyList<IReadOnlyList<T>> cycles,
        IReadOnlyList<T> ambiguousStep)
    {
        Ordered = ordered;
        Cycles = cycles;
        AmbiguousStep = ambiguousStep;
    }

    /// <summary>
    /// Nodes in dependency order (roots — nodes nothing depends on — first). Empty when
    /// <see cref="HasCycles"/> is <c>true</c>.
    /// </summary>
    public IReadOnlyList<T> Ordered { get; }

    /// <summary>The strongly-connected components of size &gt; 1 — i.e. the dependency cycles.</summary>
    public IReadOnlyList<IReadOnlyList<T>> Cycles { get; }

    /// <summary>Whether any dependency cycle was detected.</summary>
    public bool HasCycles => Cycles.Count > 0;

    /// <summary>
    /// In strict mode, the first ordering step at which more than one node was independent
    /// (i.e. the order was under-specified). Empty when ordering was unambiguous or strict was off.
    /// </summary>
    public IReadOnlyList<T> AmbiguousStep { get; }

    /// <summary>Whether a strict-mode ambiguity was detected.</summary>
    public bool IsAmbiguous => AmbiguousStep.Count > 0;
}

/// <summary>
/// Pure topological sort with strongly-connected-component cycle detection. It takes the set of
/// nodes and an edge function and returns a <see cref="PlanResult{T}"/>. It never throws on a
/// cycle, never reads ambient state, and never mutates the nodes — callers decide what to do with
/// the result. Node identity is the caller's equality (reference equality for reference types by
/// default), matching how the build orchestrator keys targets.
/// </summary>
public static class TopoSort
{
    /// <param name="nodes">All nodes to order, in their canonical input order.</param>
    /// <param name="dependencies">The nodes a given node depends on. Edges to nodes outside
    /// <paramref name="nodes"/> are ignored.</param>
    /// <param name="strict">When <c>true</c>, records the first ambiguous ordering step in the result.</param>
    public static PlanResult<T> Order<T>(
        IReadOnlyCollection<T> nodes,
        Func<T, IEnumerable<T>> dependencies,
        bool strict = false)
    {
        // Build the vertex graph, preserving input order so a non-strict tie breaks identically
        // to the original orchestrator (first-by-input-order).
        var vertexByNode = new Dictionary<T, Vertex<T>>();
        var vertices = new List<Vertex<T>>();
        foreach (var node in nodes)
        {
            var vertex = new Vertex<T>(node);
            vertexByNode[node] = vertex;
            vertices.Add(vertex);
        }

        foreach (var node in nodes)
        {
            var vertex = vertexByNode[node];
            foreach (var dependency in dependencies(node))
            {
                if (vertexByNode.TryGetValue(dependency, out var dependencyVertex))
                {
                    vertex.Dependencies.Add(dependencyVertex);
                }
            }
        }

        var cycles = new StronglyConnectedComponentFinder<T>()
            .DetectCycle(vertices)
            .Cycles()
            .Select(scc => (IReadOnlyList<T>)scc.Select(v => v.Value).ToList())
            .ToList();

        if (cycles.Count > 0)
        {
            return new PlanResult<T>(Array.Empty<T>(), cycles, Array.Empty<T>());
        }

        // Peel roots — vertices nothing remaining depends on — one at a time. On an acyclic graph
        // this always terminates with at least one root per step.
        var graph = vertices.ToList();
        var ordered = new List<T>();
        IReadOnlyList<T> ambiguousStep = Array.Empty<T>();
        while (graph.Count > 0)
        {
            var independents = graph.Where(x => !graph.Any(y => y.Dependencies.Contains(x))).ToList();
            if (strict && ambiguousStep.Count == 0 && independents.Count > 1)
            {
                ambiguousStep = independents.Select(x => x.Value).ToList();
            }

            var independent = independents.First();
            graph.Remove(independent);
            ordered.Add(independent.Value);
        }

        return new PlanResult<T>(ordered, cycles, ambiguousStep);
    }
}
