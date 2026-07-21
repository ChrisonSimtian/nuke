# @fallout/graph-control (PoC)

Fallout-branded build/run graph control. Not a GitHub clone — its own dark,
radioactive-amber identity, built to become the run-graph control for the VS
Code extension, the `--plan` HTML report, and eventually the Fallout CI platform.

**Stack:** [elkjs](https://github.com/kieler/elkjs) (layered layout) +
[React Flow](https://reactflow.dev) (`@xyflow/react`, interaction/pan/zoom).
Layout and render are deliberately separate layers — the layout is
surface-independent, the renderer is swappable.

## Run it

```bash
cd poc/graph-control
npm install
npm run dev        # dev server, hot reload — opens the demo fixture
npm run build      # → dist/index.html, a single self-contained file you can double-click
npm run build:lib  # → dist-lib/fallout-graph-control.js, an IIFE for host embedding
```

## Embedding in a host (webview / HTML report)

`build:lib` emits one self-contained IIFE (React + React Flow + elkjs bundled,
CSS injected at runtime) exposing a global. Load it as a single `<script>` and:

```js
FalloutGraph.mount(document.getElementById('graph'), buildGraph, {
    onRunTarget: (name) => { /* host decides what "run" means */ },
});
```

Re-calling `mount` on the same element reconciles in place — that's how the VS
Code extension does its live refresh. The runtime `<style>` injection needs
`style-src 'unsafe-inline'` in the host's CSP.

## How it maps to Fallout

- Data model mirrors `build-graph.json` (see `src/model.ts` ↔
  `poc/vscode-fallout/src/model.ts` and `SerializeBuildGraphAttribute.cs`).
- Edge semantics match the Mermaid/`--plan` output: solid = `dependsOn`,
  dashed = `after`, thick = `triggers`.
- A host injects a real graph via `window.__FALLOUT_GRAPH__`; otherwise the
  bundled demo fixture renders.
- `status` on a target (`queued`/`running`/`succeeded`/`failed`/`skipped`) is
  **reserved for the live/CI phase** and absent from schema v1.

## Phases

1. **This PoC** — layout + brand look, static, standalone HTML. ✅
2. Wire into the VS Code extension (replace Mermaid) + emit the static HTML report.
3. Live: `BuildManager` streams per-target status → the control animates the run.
