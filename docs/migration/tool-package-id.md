---
title: Fallout.GlobalTool → Fallout.GlobalTools
description: The dotnet-tool NuGet package id changed. The fallout command did not. This page shows how to move an existing install or manifest.
---

The dotnet-tool NuGet package id is now **`Fallout.GlobalTools`**. The **command stays `fallout`**, so
build scripts, CI steps, and shell invocations do not change. Only the install or restore reference
moves.

If you have never installed the tool, you do not need this page. Follow
[Installation](../01-getting-started/01-installation.md).

## Why you need to act

The older package ids are still on nuget.org, still listed, and frozen. `dotnet tool update` on an old
id reports that you are already on the latest version, because you are — for that id. The newer
releases are published under the new id.

| Package id | Last version | Status |
|---|---|---|
| `Nuke.GlobalTool` | NUKE-era | Frozen. Not a Fallout package. |
| `Fallout.GlobalTool` | `10.3.49` | Frozen. Receives no further releases. |
| `Fallout.Cli` | `10.3.47`, `11.0.18` | Frozen. The `11.0.x` line was withdrawn; see [v11 is defunct](#a-note-on-the-110x-versions). |
| **`Fallout.GlobalTools`** | current | **Active. All future tool releases.** |

`rollForward: true` in your manifest does not help. It resolves a version *within* one package id, so
it cannot move you to a different id.

## The easy way

`fallout-migrate` rewrites the manifest for you, including the version pin:

```sh
dotnet tool install -g Fallout.Migrate
fallout-migrate .
```

## Local manifest, by hand

Open `.config/dotnet-tools.json`. Change the tool id **and the version**. The version you had pinned
belongs to the old id and does not exist under the new one, so renaming the key alone gives you a
manifest that fails to restore.

```diff
 {
   "version": 1,
   "isRoot": true,
   "tools": {
-    "fallout.globaltool": {
-      "version": "10.3.49",
+    "fallout.globaltools": {
+      "version": "<current version>",
       "commands": [ "fallout" ]
     }
   }
 }
```

Then restore:

```sh
dotnet tool restore
```

`dotnet tool list` should show a `fallout.globaltools` row and no `fallout.globaltool` row.

## Global install, by hand

Uninstall the old package first. Two tools claiming the `fallout` command will conflict.

```sh
dotnet tool uninstall -g Fallout.GlobalTool
dotnet tool install -g Fallout.GlobalTools
```

`dotnet tool list -g` confirms the result.

## CI

Nothing changes if your workflow runs `dotnet tool restore` and then `dotnet fallout <target>`. The
restore reads whatever your manifest pins, and the command name is unchanged. Update the manifest as
above and commit it.

The thin `build.sh` and `build.ps1` bootstrappers also need no change. They call `dotnet fallout "$@"`,
which resolves by command name rather than by package id.

## A note on the 11.0.x versions

The `11.0.x` releases of `Fallout.Cli` were published in error and have been unlisted. Fallout went
from `10.x` to calendar versioning without a v11 line. Do not pin `11.0.x`.

## Refs

- [#575](https://github.com/Fallout-build/Fallout/issues/575) — the upgrade path this page documents.
