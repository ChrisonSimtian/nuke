---
title: Moving off a retired tool package id
description: The Fallout CLI ships as Fallout.GlobalTool. Three other package ids were used along the way and are now retired. This page shows how to move an install or a manifest onto the current one.
---

The Fallout CLI ships as **`Fallout.GlobalTool`**. This is the same package id NUKE users have had
pinned since the rebrand, so most readers have nothing to do.

Three other ids were used along the way and are now retired. If you pin one of them, this page shows
how to move. The **command stays `fallout`** in every case, so build scripts, CI steps, and shell
invocations do not change. Only the install or restore reference moves.

## Do you need to act?

Run `dotnet tool list --global`, and open `.config/dotnet-tools.json` if your repo has one.

| Package id | Status |
|---|---|
| **`Fallout.GlobalTool`** | **Current. All releases. Nothing to do.** |
| `Nuke.GlobalTool` | Retired. The NUKE-era id, not a Fallout package. |
| `Fallout.Cli` | Retired. The `11.0.x` line was withdrawn; see [the note below](#a-note-on-the-110x-versions). |
| `Fallout.GlobalTools` | Retired. Note the trailing **s**. One prerelease, `10.4.0-rc.4`. |

`rollForward: true` in your manifest does not help. It resolves a version *within* one package id, so
it cannot move you to a different id.

## The easy way

`fallout-migrate` rewrites the manifest for you, including the version pin, and switches your global
install:

```sh
dotnet tool install -g Fallout.Migrate
fallout-migrate .
```

Run it with `--dry-run` first if you want to see what it would change without changing anything.

`fallout-migrate` only edits the repository by default. Add `--switch-global-tool` if you also want it
to move a machine-wide (`--global`) install off a retired id.

## Local manifest, by hand

Open `.config/dotnet-tools.json`. Change the tool id **and the version**. The version you had pinned
belongs to the retired id and does not exist under the current one, so renaming the key alone gives
you a manifest that fails to restore.

```diff
 {
   "version": 1,
   "isRoot": true,
   "tools": {
-    "fallout.cli": {
-      "version": "11.0.18",
+    "fallout.globaltool": {
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

`dotnet tool list` should show a `fallout.globaltool` row and no row for the retired id.

## Global install, by hand

Uninstall the retired package first. Two tools claiming the `fallout` command will conflict.

```sh
dotnet tool uninstall -g Fallout.Cli
dotnet tool install -g Fallout.GlobalTool
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
- [#581](https://github.com/Fallout-build/Fallout/pull/581) — settled on `Fallout.GlobalTool` as the id to keep.
- [#582](https://github.com/Fallout-build/Fallout/issues/582) — the rule that stops this happening again.
