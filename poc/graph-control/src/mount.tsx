import { createRoot, type Root } from 'react-dom/client';
import { GraphControl } from './GraphControl';
import type { BuildGraph } from './model';

// Library entry for host embedding (VS Code webview, static HTML report).
// The IIFE build exposes these as `window.FalloutGraph`.

export interface MountOptions {
    onRunTarget?: (target: string) => void;
}

// One React root per container, reused across graph updates so re-rendering with
// a fresh graph reconciles in place (drives the extension's live refresh).
const roots = new WeakMap<HTMLElement, Root>();

export function mount(el: HTMLElement, graph: BuildGraph, options: MountOptions = {}): void {
    let root = roots.get(el);
    if (!root) {
        root = createRoot(el);
        roots.set(el, root);
    }
    root.render(<GraphControl graph={graph} onRunTarget={options.onRunTarget} />);
}

export function unmount(el: HTMLElement): void {
    roots.get(el)?.unmount();
    roots.delete(el);
}
