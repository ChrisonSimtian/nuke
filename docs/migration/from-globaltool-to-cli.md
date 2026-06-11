---
title: Fallout.GlobalTool → Fallout.Cli
description: Migrating from the renamed Fallout CLI NuGet package. Short guide — the command name didn't change, just the package ID.
draft: true
---

<!-- DRAFT / unpublished. The `Fallout.Cli` package id this guide describes was never
     released to nuget.org and is being superseded by `Fallout.GlobalTools` before first
     public release, so there is nobody to migrate. Kept out of the published site until
     the rename settles; do not publish a Fallout.Cli → Fallout.GlobalTools migration. -->


In v11, the dotnet-tool NuGet package id was renamed: **`Fallout.GlobalTool` → `Fallout.Cli`**. The **command name stays `fallout`**, so build scripts and shell invocations don't change. The only thing that moves is the install/restore reference.

> If you've never installed `Fallout.GlobalTool`, you don't need this page — install `Fallout.Cli` directly per the [Install section in the README](https://github.com/Fallout-build/Fallout#install).

## TL;DR

**Global install:**

```sh
dotnet tool uninstall -g Fallout.GlobalTool
dotnet tool install -g Fallout.Cli
```

**Local manifest (`.config/dotnet-tools.json`):** open the file, replace the `fallout.globaltool` entry with `fallout.cli`, restore.

```diff
 {
   "version": 1,
   "isRoot": true,
   "tools": {
-    "fallout.globaltool": {
+    "fallout.cli": {
       "version": "11.0.0",
       "commands": [ "fallout" ]
     }
   }
 }
```

```sh
dotnet tool restore
```

That's it. `fallout :setup`, `fallout Compile`, etc. all work unchanged because the command name (`fallout`) is the same.

## Why the rename

`dotnet tool install Fallout.Cli` is easier to type and remember than `dotnet tool install Fallout.GlobalTool`. The "GlobalTool" suffix was a NUKE-era artefact distinguishing the dotnet-tool wrapper from the framework libraries — but every consumer's first encounter with the tool is at install-time, and `Fallout.Cli` is what they reach for naturally.

The change is purely cosmetic at the NuGet metadata layer. No source-level API changed; the C# namespace inside the tool moved from `Fallout.GlobalTool.*` to `Fallout.Cli.*` but those types are `internal` so external consumers never referenced them.

## Affected versions

| Package | Last published | Status |
|---|---|---|
| `Fallout.GlobalTool` | `10.3.40` | Frozen on nuget.org. **Unlisted** as part of the v11 semver cleanup (see [#220](https://github.com/Fallout-build/Fallout/pull/220)). Existing installs keep working; `dotnet tool update` won't find newer versions. |
| `Fallout.Cli` | `11.0.x` (current) | Active. Receives all future tool releases. |

The `10.3.41` through `10.3.47` patch releases of `Fallout.Cli` are **also unlisted** — they shipped under a patch number that hid breaking changes, fixed by the v11 major bump. Pin to **`11.0.0`** or later.

## Migration scenarios

### You installed `Fallout.GlobalTool` globally on your machine

```sh
dotnet tool uninstall -g Fallout.GlobalTool
dotnet tool install -g Fallout.Cli
```

`dotnet tool list -g` confirms only `Fallout.Cli` remains. The `fallout` command on your PATH now resolves to the renamed package.

### Your repo has a local `.config/dotnet-tools.json` manifest

Edit the manifest, replace `fallout.globaltool` with `fallout.cli` (both the key and any references). Bump the version pin to the current `Fallout.Cli` release (`11.0.x` and above).

```sh
dotnet tool restore
```

Confirm with `dotnet tool list` — the `fallout.cli` row should appear, the `fallout.globaltool` row should not.

### Your repo uses the thin `build.sh` / `build.ps1` shims

Nothing to do. The shims call `dotnet fallout "$@"` — they look up the command by name, not by package ID, and `dotnet tool restore` resolves whatever your manifest pins.

If your repo is still on the **old fat bootstrappers** (with the `BUILD_PROJECT_FILE` config block + explicit `dotnet build` + `dotnet run --project`), those don't depend on the global tool at all and keep working unchanged. To adopt the new shape, re-run `fallout :setup --force` after upgrading. See [the v11 CHANGELOG entry for #204](https://github.com/Fallout-build/Fallout/blob/main/CHANGELOG.md) for what the new shape looks like.

### Your repo's CI calls `dotnet fallout` directly (no shim)

Update the workflow's `dotnet tool restore` step's manifest to reference `fallout.cli`. The actual `dotnet fallout <target>` lines stay identical.

## Stuck on 10.3.40 of `Fallout.GlobalTool`?

The package is unlisted but still downloadable if you have an explicit pin in your manifest. You can keep running on `10.3.40` indefinitely. When you're ready to upgrade, follow the steps above — there is no required intermediate step. `Fallout.GlobalTool 10.3.40` and `Fallout.Cli 11.0.0` are the same code-base; the only consequential difference is the package ID and the semver-correct breaking changes that have accumulated.

## Refs

- [#206](https://github.com/Fallout-build/Fallout/pull/206) — the rename PR.
- [#210](https://github.com/Fallout-build/Fallout/pull/210) — README install section + the prompt to uninstall old CLI.
- [#220](https://github.com/Fallout-build/Fallout/pull/220) — the v11 semver-policy bump that catalysed unlisting the 10.3.41-47 range.
- [`from-nuke.md`](from-nuke.md) — if you're also migrating from NUKE, do that first; this guide is downstream of it.
