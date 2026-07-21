import { Handle, Position, type NodeProps, type Node } from '@xyflow/react';
import type { TargetNodeData } from './layout';

const STATUS_LABEL: Record<string, string> = {
    queued: 'Queued',
    running: 'Running',
    succeeded: 'Succeeded',
    failed: 'Failed',
    skipped: 'Skipped',
};

/**
 * One build target, rendered as a Fallout-branded card. Not a GitHub clone:
 * dark radioactive-amber surface, a status rail on the left, connection dots on
 * the accent border. The default target gets a yellow ring; unlisted targets dim.
 */
export function TargetNode({ data }: NodeProps<Node<TargetNodeData>>) {
    const status = data.status;
    const classes = [
        'target-node',
        data.isDefault ? 'is-default' : '',
        data.listed ? '' : 'is-unlisted',
        status ? `status-${status}` : 'status-none',
    ]
        .filter(Boolean)
        .join(' ');

    const subtitle = data.description ?? data.declaredIn;

    return (
        <div className={classes} title={data.declaredIn ? `${data.label} — ${data.declaredIn}` : data.label}>
            <Handle type="target" position={Position.Left} className="target-handle" />
            <span className="status-rail" aria-hidden="true" />
            <span className={`status-dot ${status ? '' : 'idle'}`} aria-hidden="true">
                {status === 'running' && <span className="spinner" />}
            </span>
            <div className="target-body">
                <div className="target-name">
                    {data.label}
                    {data.isDefault && <span className="default-tag">default</span>}
                </div>
                {subtitle && <div className="target-sub">{subtitle}</div>}
            </div>
            {status && <span className="status-label">{STATUS_LABEL[status] ?? status}</span>}
            <Handle type="source" position={Position.Right} className="source-handle" />
        </div>
    );
}
