import { useEffect, useMemo, useState } from 'react';
import {
    ReactFlow,
    Background,
    BackgroundVariant,
    Controls,
    type Edge,
    type Node,
    type NodeMouseHandler,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import './theme.css';
import './control.css';
import { TargetNode } from './TargetNode';
import { layoutGraph, type TargetNodeData } from './layout';
import type { BuildGraph } from './model';

const nodeTypes = { target: TargetNode };

export interface GraphControlProps {
    graph: BuildGraph;
    /** Fired when a target card is clicked — the host decides what "run" means. */
    onRunTarget?: (target: string) => void;
}

/**
 * The reusable Fallout graph control. Same component drives the VS Code webview,
 * the static --plan HTML report, and (later) the live CI run graph — only the
 * data source and the onRunTarget handler change.
 */
export function GraphControl({ graph, onRunTarget }: GraphControlProps) {
    const [nodes, setNodes] = useState<Node<TargetNodeData>[]>([]);
    const [edges, setEdges] = useState<Edge[]>([]);
    const [ready, setReady] = useState(false);

    useEffect(() => {
        let cancelled = false;
        setReady(false);
        void layoutGraph(graph).then((laid) => {
            if (cancelled) return;
            setNodes(laid.nodes);
            setEdges(laid.edges);
            setReady(true);
        });
        return () => {
            cancelled = true;
        };
    }, [graph]);

    const onNodeClick = useMemo<NodeMouseHandler>(
        () => (_event, node) => onRunTarget?.(node.id),
        [onRunTarget],
    );

    return (
        <div className="fallout-graph">
            <div className="graph-header">
                <span className="mark" aria-hidden="true">
                    ☢
                </span>
                <span className="graph-title">Build graph</span>
                {graph.falloutVersion && <span className="graph-version">Fallout {graph.falloutVersion}</span>}
                <span className="graph-count">{graph.targets.length} targets</span>
            </div>
            <div className="graph-canvas">
                {ready && (
                    <ReactFlow
                        nodes={nodes}
                        edges={edges}
                        nodeTypes={nodeTypes}
                        onNodeClick={onNodeClick}
                        fitView
                        fitViewOptions={{ padding: 0.2 }}
                        minZoom={0.2}
                        maxZoom={2}
                        proOptions={{ hideAttribution: true }}
                        nodesDraggable={false}
                        nodesConnectable={false}
                        elementsSelectable
                    >
                        <Background variant={BackgroundVariant.Dots} gap={22} size={1} className="graph-bg" />
                        <Controls showInteractive={false} />
                    </ReactFlow>
                )}
            </div>
            <div className="graph-legend">
                <span><i className="k-depends" /> depends on</span>
                <span><i className="k-after" /> runs after</span>
                <span><i className="k-trigger" /> triggers</span>
                <span className="legend-hint">click a target to run it</span>
            </div>
        </div>
    );
}
