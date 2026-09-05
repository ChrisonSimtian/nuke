import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import cssInjectedByJs from 'vite-plugin-css-injected-by-js';

// Library build: one self-contained IIFE (React + React Flow + elkjs bundled,
// CSS injected at runtime) exposing `window.FalloutGraph.mount(...)`. Hosts load
// it as a single <script> — the same way the extension used to load mermaid.
// The runtime <style> injection needs `style-src 'unsafe-inline'` in the host CSP.
export default defineConfig({
    plugins: [react(), cssInjectedByJs()],
    // Vite library mode (unlike app mode) does not substitute process.env.NODE_ENV,
    // so React's reference would throw `process is not defined` in a plain browser
    // <script> host. Bake it in — the bundle ships React's production build.
    define: {
        'process.env.NODE_ENV': JSON.stringify('production'),
    },
    build: {
        target: 'es2022',
        outDir: 'dist-lib',
        emptyOutDir: true,
        lib: {
            entry: 'src/mount.tsx',
            name: 'FalloutGraph',
            formats: ['iife'],
            fileName: () => 'fallout-graph-control.js',
        },
    },
});
