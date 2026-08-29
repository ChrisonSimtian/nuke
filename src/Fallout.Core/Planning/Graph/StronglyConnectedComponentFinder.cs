using System;
using System.Collections.Generic;

namespace Fallout.Core.Planning;

internal class StronglyConnectedComponentFinder<T>
{
    private StronglyConnectedComponentList<T> stronglyConnectedComponents;
    private Stack<Vertex<T>> stack;
    private int index;

    /// <summary>
    /// Calculates the sets of strongly connected vertices.
    /// </summary>
    /// <param name="graph">Graph to detect cycles within.</param>
    /// <returns>Set of strongly connected components (sets of vertices)</returns>
    public StronglyConnectedComponentList<T> DetectCycle(IEnumerable<Vertex<T>> graph)
    {
        stronglyConnectedComponents = new StronglyConnectedComponentList<T>();
        index = 0;
        stack = new Stack<Vertex<T>>();
        foreach (var v in graph)
        {
            if (v.Index < 0)
            {
                StrongConnect(v);
            }
        }

        return stronglyConnectedComponents;
    }

    private void StrongConnect(Vertex<T> v)
    {
        v.Index = index;
        v.LowLink = index;
        index++;
        stack.Push(v);

        foreach (var w1 in v.Dependencies)
        {
            if (w1.Index < 0)
            {
                StrongConnect(w1);
                v.LowLink = Math.Min(v.LowLink, w1.LowLink);
            }
            else if (stack.Contains(w1))
            {
                v.LowLink = Math.Min(v.LowLink, w1.Index);
            }
        }

        if (v.LowLink != v.Index)
        {
            return;
        }

        var scc = new StronglyConnectedComponent<T>();
        Vertex<T> w2;
        do
        {
            w2 = stack.Pop();
            scc.Add(w2);
        }
        while (v != w2);

        stronglyConnectedComponents.Add(scc);
    }
}
