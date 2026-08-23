---
name: adding-a-tool-wrapper
description: Recipe for adding or extending a CLI tool wrapper under src/Fallout.Common/Tools/<Tool>/<Tool>.json. Trigger when asked to add a new tool wrapper, add a command/argument to an existing one, or when a Tools/*.json file is being edited.
---

Tool wrapper `.json` files are the source of truth; the `.cs` next to each one
is generated — never hand-edit the generated file.

1. Find the closest existing tool under `src/Fallout.Common/Tools/<Tool>/<Tool>.json`
   and copy its shape.
2. Cover a full command with all its arguments, not just the one option you need.
3. Use formatting tags in `help` text:
   - `<c>` for inline code
   - `<a>` for links
   - `<ul>` / `<ol>` for lists
   - `<em>` for emphasized text
   - `<para/>` between paragraphs (not `<p>...</p>`)
4. Don't write `secret: false` — it's the default.
5. Don't write `default: xxx` — obsolete field, omit it.
6. Run `./build.ps1 GenerateTools` to regenerate the `.cs` output.
7. Commit the regenerated `.cs` alongside the `.json` spec in the same commit —
   `VerifyGeneratedTools` fails CI if they drift.
