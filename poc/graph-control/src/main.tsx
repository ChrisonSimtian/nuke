import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { GraphControl } from './GraphControl';
import { demoGraph } from './fixture';
import type { BuildGraph } from './model';

// The host can inject a real build-graph.json as window.__FALLOUT_GRAPH__
// (this is exactly how the static --plan HTML report will embed it). Falls back
// to the bundled demo graph when nothing is injected.
declare global {
    interface Window {
        __FALLOUT_GRAPH__?: BuildGraph;
    }
}

const graph = window.__FALLOUT_GRAPH__ ?? demoGraph;

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <GraphControl graph={graph} onRunTarget={(t) => console.log('run target:', t)} />
    </StrictMode>,
);
