---
title: Navigation
---

Over time, you might accumulate more and more projects that are built using Fallout. Some of these might even form a hierarchical structure, where one root directory contains several other root directories, and so on.

## Configuration

Add the following functions to your shell configuration (similar as for [shell completion](00-shell-completion.md)):

```
function fallout/ { fallout :PushWithChosenRootDirectory; cd $(fallout :GetNextDirectory) }
function fallout. { fallout :PushWithCurrentRootDirectory; cd $(fallout :GetNextDirectory) }
function fallout.. { fallout :PushWithParentRootDirectory; cd $(fallout :GetNextDirectory) }
function fallout- { fallout :PopDirectory; cd $(fallout :GetNextDirectory) }
```

## Usage

The global tool comes with a handful of functions for improved navigation:

| Command     | Function                                       |
|:------------|:-----------------------------------------------|
| `fallout.`  | Navigates to the current root directory        |
| `fallout..` | Navigates to the parent root directory         |
| `fallout/`  | Lists subdirectories that are root directories |
| `fallout-`  | Navigates to the last root directory           |

:::note
The `fallout-` command is only supported on shells that set the `TERM_SESSION_ID` or `WT_SESSION` environment variable. As of now, this includes [iTerm](https://iterm2.com/) and the [Windows Terminal](https://github.com/microsoft/terminal).
:::
