import type { BuildGraph } from './model';

// A representative Fallout build+publish graph. Statuses are included to show
// the live/CI look — a real static build-graph.json (schema v1) omits them.
// Structure mirrors the actual pipeline: Restore → Compile → Test/Pack →
// publish fan-out, plus an `after` order-edge and a `triggers` edge to exercise
// all three edge styles.
export const demoGraph: BuildGraph = {
    version: 1,
    falloutVersion: '2026.1.0-preview.412.g8f3a1c',
    targets: [
        { name: 'Clean', description: 'Wipe artifacts + bin/obj', default: false, listed: true, declaredIn: 'Build.Common', dependsOn: [], after: [], triggeredBy: [], triggers: [], status: 'succeeded' },
        { name: 'Restore', description: 'dotnet restore fallout.slnx', default: false, listed: true, declaredIn: 'Build.Common', dependsOn: [], after: ['Clean'], triggeredBy: [], triggers: [], status: 'succeeded' },
        { name: 'Compile', description: 'Build all projects', default: false, listed: true, declaredIn: 'Build.Common', dependsOn: ['Restore'], after: [], triggeredBy: [], triggers: [], status: 'succeeded' },
        { name: 'GenerateTools', description: 'Regenerate tool wrappers', default: false, listed: false, declaredIn: 'Build.CodeGen', dependsOn: ['Restore'], after: [], triggeredBy: [], triggers: [], status: 'skipped' },
        { name: 'Test', description: 'xUnit across the suite', default: false, listed: true, declaredIn: 'Build.Common', dependsOn: ['Compile'], after: [], triggeredBy: [], triggers: [], status: 'running' },
        { name: 'Pack', description: 'NuGet pack (default target)', default: true, listed: true, declaredIn: 'Build.Pack', dependsOn: ['Compile'], after: [], triggeredBy: [], triggers: [], status: 'running' },
        { name: 'ValidateRef', description: 'Assert release/vX.Y ref', default: false, listed: true, declaredIn: 'Build.Publish', dependsOn: [], after: [], triggeredBy: [], triggers: [], status: 'succeeded' },
        { name: 'PublishGitHubPackages', description: 'Push preview to GH Packages', default: false, listed: true, declaredIn: 'Build.Publish', dependsOn: ['Test', 'Pack'], after: ['ValidateRef'], triggeredBy: [], triggers: ['Canary'], status: 'queued' },
        { name: 'PublishGitHubReleases', description: 'Create GitHub Release', default: false, listed: true, declaredIn: 'Build.Publish', dependsOn: ['Pack'], after: [], triggeredBy: [], triggers: [], status: 'queued' },
        { name: 'PublishNuGet', description: 'Push GA to nuget.org', default: false, listed: true, declaredIn: 'Build.Publish', dependsOn: ['Test', 'Pack'], after: ['ValidateRef'], triggeredBy: [], triggers: [], status: 'queued' },
        { name: 'Canary', description: 'Smoke-test published package', default: false, listed: true, declaredIn: 'Build.Publish', dependsOn: [], after: [], triggeredBy: ['PublishGitHubPackages'], triggers: [], status: 'queued' },
    ],
};
