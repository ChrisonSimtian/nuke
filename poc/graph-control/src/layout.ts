import ELK from 'elkjs/lib/elk.bundled.js';
import type { Edge, Node } from '@xyflow/react';
import type { BuildGraph, Relation } from './model';

// elk.bundled.js runs the layout inline (no web worker / no external asset),
// which keeps the single-file HTML build self-contained.
const elk = new ELK();

export const NODE_WIDTH = 248;
export const NODE_HEIGHT = 60;

/** Data carried on every React Flow node — consumed by TargetNode. */
export interface TargetNodeData extends Record<string, unknown> {
    label: string;
    description?: string;
    declaredIn?: string;
    isDefault: boolean;
    listed: boolean;
    status?: string;
}

/** A directed edge plus the relation that produced it (drives styling). */
interface RelationEdge {
    source: string;
    target: string;
    relation: Relation;
}

/**
 * Flattens the four relation arrays into directed edges pointing in
 * execution-flow direction (prerequisite → dependent) — same semantics as the
 * Mermaid/--plan output. `triggeredBy` is the mirror of `triggers`, so we emit
 * `triggers` alone to avoid duplicate edges.
 */
function collectEdges(graph: BuildGraph): RelationEdge[] {
    const edges: RelationEdge[] = [];
    for (const t of graph.targets) {
        for (const d of t.dependsOn) edges.push({ source: d, target: t.name, relation: 'dependsOn' });
        for (const d of t.after) edges.push({ source: d, target: t.name, relation: 'after' });
        for (const d of t.triggers) edges.push({ source: t.name, target: d, relation: 'triggers' });
    }
    return edges;
}

/**
 * Runs elkjs `layered` layout left-to-right and returns React Flow nodes/edges
 * with absolute positions. Async because elk.layout is promise-based.
 */
export async function layoutGraph(graph: BuildGraph): Promise<{ nodes: Node<TargetNodeData>[]; edges: Edge[] }> {
    const relationEdges = collectEdges(graph);
    const known = new Set(graph.targets.map((t) => t.name));

    const elkGraph = {
        id: 'root',
        layoutOptions: {
            'elk.algorithm': 'layered',
            'elk.direction': 'RIGHT',
            'elk.layered.spacing.nodeNodeBetweenLayers': '96',
            'elk.spacing.nodeNode': '28',
            'elk.layered.nodePlacement.strategy': 'NETWORK_SIMPLEX',
            'elk.edgeRouting': 'ORTHOGONAL',
        },
        children: graph.targets.map((t) => ({ id: t.name, width: NODE_WIDTH, height: NODE_HEIGHT })),
        // Drop edges that reference an unknown target — a defensive guard so a
        // malformed graph lays out instead of throwing.
        edges: relationEdges
            .filter((e) => known.has(e.source) && known.has(e.target))
            .map((e, i) => ({ id: `e${i}`, sources: [e.source], targets: [e.target] })),
    };

    const laid = await elk.layout(elkGraph);
    const positions = new Map((laid.children ?? []).map((c) => [c.id, { x: c.x ?? 0, y: c.y ?? 0 }]));

    const nodes: Node<TargetNodeData>[] = graph.targets.map((t) => ({
        id: t.name,
        type: 'target',
        position: positions.get(t.name) ?? { x: 0, y: 0 },
        data: {
            label: t.name,
            description: t.description,
            declaredIn: t.declaredIn,
            isDefault: t.default,
            listed: t.listed,
            status: t.status,
        },
    }));

    const edges: Edge[] = relationEdges
        .filter((e) => known.has(e.source) && known.has(e.target))
        .map((e, i) => ({
            id: `e${i}`,
            source: e.source,
            target: e.target,
            type: 'smoothstep',
            data: { relation: e.relation },
            className: `edge-${e.relation}`,
        }));

    return { nodes, edges };
}
