import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { viteSingleFile } from 'vite-plugin-singlefile';

// `viteSingleFile` inlines JS/CSS into one index.html so the built artifact is a
// double-clickable, offline, self-contained file — the same shape the eventual
// `fallout` --plan HTML report will take. `npm run dev` keeps a normal dev server.
export default defineConfig({
    plugins: [react(), viteSingleFile()],
    build: {
        target: 'es2022',
        assetsInlineLimit: 100_000_000,
        cssCodeSplit: false,
    },
});
