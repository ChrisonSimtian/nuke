// Mirrors the build-graph.json schema the Fallout framework emits
// (see poc/vscode-fallout/src/model.ts and SerializeBuildGraphAttribute.cs).
// The only addition is `status`, which is absent in schema v1 and reserved for
// the live/CI phase — treat it as optional everywhere.

/** How one target relates to another in the build graph. */
export type Relation = 'dependsOn' | 'after' | 'triggeredBy' | 'triggers';

/** Live execution state of a target. Absent in build-graph.json v1 (static). */
export type Status = 'queued' | 'running' | 'succeeded' | 'failed' | 'skipped';

/** A single build target, as emitted into build-graph.json. */
export interface Target {
    name: string;
    description?: string;
    default: boolean;
    listed: boolean;
    /** Declaring C# type (class or interface) — disambiguates go-to-definition. */
    declaredIn?: string;
    dependsOn: string[];
    after: string[];
    triggeredBy: string[];
    triggers: string[];
    /** Live-only; unset for a static graph. */
    status?: Status;
}

/** Parsed contents of build-graph.json. */
export interface BuildGraph {
    version: number;
    falloutVersion?: string;
    targets: Target[];
}
