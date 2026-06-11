# CLI tool references

Plain-text snapshots of the upstream documentation for each CLI tool that Fallout wraps. One file per `<Tool>.ref.<NNN>.txt`, indexed against the `references` array in the matching `src/Fallout.Common/Tools/<Tool>/<Tool>.json` spec.

## Why they're here

Originally checked into `build/references/` and used by the `References` build target as a validation aid — the target downloads the upstream pages, normalises them, and writes them here, so reviewers can spot when a tool's CLI has drifted from what the JSON spec expects.

Moved to `docs/cli-tools/` because (a) they're documentation, not part of the build's working state, and (b) Markdown'd guides for commonly-used tools can grow next to them if we ever want pretty docs.

## Regenerating

```pwsh
./build.ps1 References
```

Pulls the latest content from each tool's reference URL and overwrites the matching `.ref.NNN.txt` here. Run on demand — not part of the regular build flow.

## What's in each file

Plain text scraped from the upstream HTML. Mostly raw `dotnet`, `git`, `paket`, etc. command help output. Useful as a quick diff signal when a tool gets a new flag; **not** intended as user-facing tutorial content.

If you're looking for tutorial-style usage docs for a Fallout-wrapped tool, those will land here too — separately authored under each tool's name (e.g. `dotnet.md`) once we get around to writing them. See [#41](https://github.com/Fallout-build/Fallout/issues/41) for the broader docs effort.
