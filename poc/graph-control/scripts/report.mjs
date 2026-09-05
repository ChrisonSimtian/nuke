// Emits a self-contained static HTML build-graph report: the control's IIFE and
// the graph JSON are inlined into one file — double-clickable, offline, no server.
// This is the shape the `fallout` --plan HTML report will take.
//
//   node scripts/report.mjs [build-graph.json] [out.html]
//
// With no graph path a small sample is used, so `npm run report` works out of the
// box. Dark by default (data-theme="dark"), with a toggle that remembers the
// viewer's choice — a shared report stays dark regardless of the viewer's OS.
import { execSync } from 'node:child_process';
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const root = join(here, '..');
const bundle = join(root, 'dist-lib', 'fallout-graph-control.js');

const graphArg = process.argv[2];
const outArg = process.argv[3] ?? join(root, 'dist-lib', 'report.html');

// Ensure the library bundle exists (build it once if missing).
if (!existsSync(bundle)) {
    console.log('[report] building library bundle…');
    execSync('npm run build:lib', { cwd: root, stdio: 'inherit' });
}

const sample = {
    version: 1,
    falloutVersion: '2026.1.0-preview.412.g8f3a1c',
    targets: [
        { name: 'Restore', description: 'dotnet restore', default: false, listed: true, dependsOn: [], after: ['Clean'], triggeredBy: [], triggers: [] },
        { name: 'Clean', description: 'Wipe artifacts', default: false, listed: false, dependsOn: [], after: [], triggeredBy: [], triggers: [] },
        { name: 'Compile', description: 'Build all projects', default: false, listed: true, dependsOn: ['Restore'], after: [], triggeredBy: [], triggers: [] },
        { name: 'Test', description: 'xUnit suite', default: false, listed: true, dependsOn: ['Compile'], after: [], triggeredBy: [], triggers: [] },
        { name: 'Pack', description: 'NuGet pack (default)', default: true, listed: true, dependsOn: ['Compile'], after: [], triggeredBy: [], triggers: ['Canary'] },
        { name: 'Canary', description: 'Smoke-test the package', default: false, listed: true, dependsOn: [], after: [], triggeredBy: ['Pack'], triggers: [] },
    ],
};

const graph = graphArg ? JSON.parse(readFileSync(resolve(graphArg), 'utf8')) : sample;
const bundleJs = readFileSync(bundle, 'utf8');

// JSON is embedded as a JS value; escape </script> so a target name can't break out.
const graphLiteral = JSON.stringify(graph).replace(/<\/script>/gi, '<\\/script>');

const html = `<!doctype html>
<html lang="en" data-theme="dark">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Fallout — Build Graph Report</title>
<style>
    html, body { height: 100%; margin: 0; padding: 0; }
    #graph { height: 100vh; }
    /* keep the header's target count clear of the fixed toggle button.
       Higher specificity than the control's own .graph-header rule, which is
       injected at runtime (after this <style>) and would otherwise win. */
    .fallout-graph .graph-header { padding-right: 56px; }
    #theme-toggle {
        position: fixed; top: 12px; right: 14px; z-index: 10;
        width: 30px; height: 30px; border-radius: 8px; cursor: pointer;
        display: grid; place-items: center; font-size: 15px; line-height: 1;
        background: var(--surface-raised); color: var(--text);
        border: 1px solid var(--border);
    }
    #theme-toggle:hover { border-color: var(--fallout-yellow-line); }
</style>
</head>
<body>
<div id="graph"></div>
<button id="theme-toggle" title="Toggle light / dark" aria-label="Toggle light / dark"></button>
<script>window.__FALLOUT_GRAPH__ = ${graphLiteral};</script>
<script>${bundleJs}</script>
<script>
    FalloutGraph.mount(document.getElementById('graph'), window.__FALLOUT_GRAPH__, {});
    (function () {
        var root = document.documentElement, btn = document.getElementById('theme-toggle');
        function apply(t) {
            root.dataset.theme = t;
            btn.textContent = t === 'dark' ? '☀' : '☾';
            try { localStorage.setItem('fallout-report-theme', t); } catch (e) {}
        }
        var saved;
        try { saved = localStorage.getItem('fallout-report-theme'); } catch (e) {}
        apply(saved || 'dark');
        btn.addEventListener('click', function () {
            apply(root.dataset.theme === 'dark' ? 'light' : 'dark');
        });
    })();
</script>
</body>
</html>
`;

writeFileSync(resolve(outArg), html);
const kb = Math.round(Buffer.byteLength(html) / 1024);
console.log(`[report] wrote ${resolve(outArg)} (${kb} KB, self-contained, ${graph.targets.length} targets)`);
