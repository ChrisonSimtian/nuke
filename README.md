<p align="center">
  <img width="320" src=".assets/fallout-logo.svg" alt="Fallout — .NET build system" />
</p>

<p align="center">
  <strong>📖 Documentation: <a href="https://docs.fallout.build/">docs.fallout.build</a></strong>
</p>

> 📦 **Fallout is the successor to NUKE.** [Migrating from NUKE →](docs/Migration/from-nuke.md)

# Fallout

> Build automation for C#/.NET — the hard-fork successor to NUKE.

[![built with Fallout](https://img.shields.io/badge/built%20with-Fallout-F5C800?logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI%2BPGNpcmNsZSBjeD0iMTIiIGN5PSIxMiIgcj0iMTIiIGZpbGw9IiNGNUM4MDAiLz48ZyBmaWxsPSIjMTExIj48Y2lyY2xlIGN4PSIxMiIgY3k9IjEyIiByPSIzLjEiLz48cGF0aCBkPSJNOS45OSA5LjAyQTMuNiAzLjYgMCAwIDEgMTQuMDEgOS4wMkwxOC40OSAyLjM4QTExLjYgMTEuNiAwIDAgMCA1LjUxIDIuMzhaTTE1LjU5IDExLjc1QTMuNiAzLjYgMCAwIDEgMTMuNTggMTUuMjRMMTcuMDkgMjIuNDNBMTEuNiAxMS42IDAgMCAwIDIzLjU3IDExLjE5Wk0xMC40MiAxNS4yNEEzLjYgMy42IDAgMCAxIDguNDEgMTEuNzVMMC40MyAxMS4xOUExMS42IDExLjYgMCAwIDAgNi45MSAyMi40M1oiLz48L2c%2BPC9zdmc%2B)](docs/website/badge.md)
[![Docs](https://img.shields.io/badge/docs-docs.fallout.build-blue?logo=readthedocs&logoColor=white)](https://docs.fallout.build/)
[![CI](https://img.shields.io/github/actions/workflow/status/Fallout-build/Fallout/publish-packages-preview.yml?branch=main&label=CI&logo=githubactions&logoColor=white)](https://github.com/Fallout-build/Fallout/actions/workflows/publish-packages-preview.yml)
[![NuGet](https://img.shields.io/nuget/v/Fallout.Common?label=Fallout.Common)](https://www.nuget.org/packages/Fallout.Common)
[![NuGet downloads](https://img.shields.io/nuget/dt/Fallout.Common?label=downloads)](https://www.nuget.org/packages/Fallout.Common)
[![Latest release](https://img.shields.io/github/v/release/Fallout-build/Fallout?label=release)](https://github.com/Fallout-build/Fallout/releases/latest)
[![GitHub last commit](https://img.shields.io/github/last-commit/Fallout-build/Fallout)](https://github.com/Fallout-build/Fallout/commits/main)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dot.net)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Open issues](https://img.shields.io/github/issues/Fallout-build/Fallout)](https://github.com/Fallout-build/Fallout/issues)
[![Open PRs](https://img.shields.io/github/issues-pr/Fallout-build/Fallout)](https://github.com/Fallout-build/Fallout/pulls)
[![GitHub Sponsors](https://img.shields.io/github/sponsors/ChrisonSimtian?label=sponsor&logo=githubsponsors&color=EA4AAA)](https://github.com/sponsors/ChrisonSimtian)

## Based on NUKE

Fallout is the successor to **[NUKE](https://github.com/nuke-build/nuke)**, originally created by **Matthias Koch** ([matkoch](https://github.com/matkoch)) and many contributors. Fallout continues NUKE's mission as a C#-first build automation framework for .NET — under new maintenance, with an enterprise-CI/CD focus.

The original NUKE code is preserved here under the MIT License with attribution. Major version 10.x was the last NUKE release; everything from this fork forward carries the Fallout identity.

### Migrating from NUKE

If you maintain a NUKE-based build, **[docs/Migration/from-nuke.md](docs/Migration/from-nuke.md)** walks you through it. The short version:

```sh
dotnet tool install -g Fallout.Migrate
cd path/to/your-nuke-repo
fallout-migrate
```

## Install

```sh
dotnet tool install -g Fallout.GlobalTool
```

The CLI installs as `fallout`. Verify with `fallout --help`.

> [!NOTE]
> **Coming from NUKE's `10.x` tool?** Nothing to do — `Fallout.GlobalTool` is the same package id you already have pinned, so `dotnet tool update` just works. The only exception is the short-lived `Fallout.GlobalTools` (plural) id: if you installed `10.4.0-rc.4` from it, uninstall it and reinstall from `Fallout.GlobalTool` so you don't have two tools claiming the `fallout` command.
>
> ```sh
> dotnet tool uninstall -g Fallout.GlobalTools
> ```

For per-repo manifest pinning (`.config/dotnet-tools.json`), project setup, and shell completion, see the [Installation guide on docs.fallout.build](https://docs.fallout.build/getting-started/installation).

> [!NOTE]
> **Channels.** Stable releases ship on **calendar versions** (`YYYY.MINOR.PATCH`, e.g. `2026.1.3`; the major is the year) from the `release/YYYY` production line — published to GitHub Packages, with nuget.org publishing opt-in per release. The `main` integration trunk publishes a faster `…-preview.…` prerelease to **GitHub Packages only**; opt in by adding the GitHub Packages feed and a prerelease version range. The legacy NUKE `10.x` line (`support/v10`) stays on semver and receives security/critical fixes only. See [ADR-0004](docs/adr/0004-calendar-versioning-and-dual-pace-channels.md) (as amended by [ADR-0008](docs/adr/0008-collapse-experimental-into-main.md)) and [docs/branching-and-release.md](docs/branching-and-release.md) for the full model.

## Table of Contents

- [Elevator Pitch](#elevator-pitch)
- [Build Status](#build-status)
- [Activity](#activity)
- [Contribute](#contribute)
- [Sponsorship](#sponsorship)

## Elevator Pitch

Solid and scalable CI/CD pipelines are an essential pillar for being competitive and creating a great product. But why are most of us a little afraid of touching YAML files and don't even dare to look at build scripts? Much of this is because C# developers are spoiled with a great language and smart IDEs, and they don't like missing their buddy for code-completion, ease of debugging, refactorings, and code formatting.

Fallout (NUKE's successor) brings your build automation to an even level with every other .NET project. How? It's a regular console application allowing all the OOP goodness! Besides, it solves many common problems in build automation, like parameter injection, path separator abstraction, access to solution and project models, and build step sharing across repositories. Fallout can also generate CI/CD configurations (YAML, etc.) that automatically parallelize build steps on multiple agents to optimize throughput!

## Build Status

CI runs on every PR targeting `main`, `release/*`, or `support/*` on `build.yml` (Linux) — its `ubuntu-latest` job is the only required status check. Cross-platform Test+Pack (Windows + macOS) is gated to release intent — PRs into `release/*` / `support/*` and `v*` tag pushes — via `build-cross-platform.yml`; it does not run on routine `main` work. A `…-preview` prerelease is published to **GitHub Packages** under the reserved `Fallout.*` prefix on every push to `main`. **Stable** releases fire from `release/YYYY` tags via `.github/workflows/publish-packages-release.yml` (GitHub Packages + GitHub Releases by default; nuget.org opt-in per release). Docs-only PRs are served by a no-op companion workflow (`build-skip`) so branch protection is satisfied without spending CI minutes on a real build.

| Workflow | Status | Trigger |
|---|---|---|
| [`build`](.github/workflows/build.yml) | [![build](https://img.shields.io/github/actions/workflow/status/Fallout-build/Fallout/build.yml?event=pull_request&label=&logo=ubuntu&logoColor=white&style=flat-square)](https://github.com/Fallout-build/Fallout/actions/workflows/build.yml) | PR to `main` / `release/*` / `support/*` (code paths) — job `ubuntu-latest` is the **required check** |
| [`build-cross-platform`](.github/workflows/build-cross-platform.yml) | [![build-cross-platform](https://img.shields.io/github/actions/workflow/status/Fallout-build/Fallout/build-cross-platform.yml?event=pull_request&label=&logo=github&logoColor=white&style=flat-square)](https://github.com/Fallout-build/Fallout/actions/workflows/build-cross-platform.yml) | PR to `release/*` / `support/*` or `v*` tag push — Windows + macOS (release intent) |
| [`publish-packages-preview`](.github/workflows/publish-packages-preview.yml) | [![publish-packages-preview](https://img.shields.io/github/actions/workflow/status/Fallout-build/Fallout/publish-packages-preview.yml?branch=main&label=&logo=githubactions&logoColor=white&style=flat-square)](https://github.com/Fallout-build/Fallout/actions/workflows/publish-packages-preview.yml) | push to `main` → `…-preview` prerelease to GitHub Packages |
| [`publish-packages-release`](.github/workflows/publish-packages-release.yml) | [![publish-packages-release](https://img.shields.io/github/actions/workflow/status/Fallout-build/Fallout/publish-packages-release.yml?event=push&label=&logo=nuget&logoColor=white&style=flat-square)](https://github.com/Fallout-build/Fallout/actions/workflows/publish-packages-release.yml) | tag push on `release/YYYY` (stable) or `support/*` (legacy/retired) — nuget.org opt-in |

Multi-provider CI support (Azure Pipelines, GitLab, TeamCity, AppVeyor) was removed during the takeover and is being revived demand-driven — see [#8](https://github.com/Fallout-build/Fallout/issues/8).

## Activity

### Commits, issues, PRs (rolling 30 days)

![Repobeats analytics image](https://repobeats.axiom.co/api/embed/c4ea2e2211409a86c7dba874c3ed6aa629efe700.svg "Repobeats analytics image")

Generated by [Repobeats](https://repobeats.axiom.co).

### Stars over time

[![RepoStars](https://repostars.dev/api/embed?repo=Fallout-build%2FFallout&theme=terminal)](https://repostars.dev/?repos=Fallout-build%2FFallout&theme=terminal)

Generated by [repostars.dev](https://www.repostars.dev/). Auto-updates as new stargazers arrive.

## Contribute

Two things help the project and take about a minute:

- **Star the repo.** It is how most people find Fallout.
- **Show the badge.** If you build with Fallout, link back from your own README. Fallout's own
  build is a Fallout build, so the badge sits at the top of this page too.

[![built with Fallout](https://img.shields.io/badge/built%20with-Fallout-F5C800?logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI%2BPGNpcmNsZSBjeD0iMTIiIGN5PSIxMiIgcj0iMTIiIGZpbGw9IiNGNUM4MDAiLz48ZyBmaWxsPSIjMTExIj48Y2lyY2xlIGN4PSIxMiIgY3k9IjEyIiByPSIzLjEiLz48cGF0aCBkPSJNOS45OSA5LjAyQTMuNiAzLjYgMCAwIDEgMTQuMDEgOS4wMkwxOC40OSAyLjM4QTExLjYgMTEuNiAwIDAgMCA1LjUxIDIuMzhaTTE1LjU5IDExLjc1QTMuNiAzLjYgMCAwIDEgMTMuNTggMTUuMjRMMTcuMDkgMjIuNDNBMTEuNiAxMS42IDAgMCAwIDIzLjU3IDExLjE5Wk0xMC40MiAxNS4yNEEzLjYgMy42IDAgMCAxIDguNDEgMTEuNzVMMC40MyAxMS4xOUExMS42IDExLjYgMCAwIDAgNi45MSAyMi40M1oiLz48L2c%2BPC9zdmc%2B)](https://github.com/Fallout-build/Fallout)

```markdown
[![built with Fallout](https://img.shields.io/badge/built%20with-Fallout-F5C800?logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI%2BPGNpcmNsZSBjeD0iMTIiIGN5PSIxMiIgcj0iMTIiIGZpbGw9IiNGNUM4MDAiLz48ZyBmaWxsPSIjMTExIj48Y2lyY2xlIGN4PSIxMiIgY3k9IjEyIiByPSIzLjEiLz48cGF0aCBkPSJNOS45OSA5LjAyQTMuNiAzLjYgMCAwIDEgMTQuMDEgOS4wMkwxOC40OSAyLjM4QTExLjYgMTEuNiAwIDAgMCA1LjUxIDIuMzhaTTE1LjU5IDExLjc1QTMuNiAzLjYgMCAwIDEgMTMuNTggMTUuMjRMMTcuMDkgMjIuNDNBMTEuNiAxMS42IDAgMCAwIDIzLjU3IDExLjE5Wk0xMC40MiAxNS4yNEEzLjYgMy42IDAgMCAxIDguNDEgMTEuNzVMMC40MyAxMS4xOUExMS42IDExLjYgMCAwIDAgNi45MSAyMi40M1oiLz48L2c%2BPC9zdmc%2B)](https://github.com/Fallout-build/Fallout)
```

More styles and wordings, plus the logo source, are in [docs/website/badge.md](docs/website/badge.md).

Want to contribute code, docs, or triage? Start with [CONTRIBUTING.md](CONTRIBUTING.md).

## Sponsorship

Fallout is volunteer-run, and we want to be transparent about what running the project costs — see [`costs.md`](https://github.com/Fallout-build/.github/blob/main/costs.md) for the full list. It lives with the organisation's other shared project files, since the spend covers every repository rather than this one.

If you or your organisation would like to help offset those costs, use the sponsor button on any Fallout repository, or open an issue and we'll work out the details.

## Credits

- [Matthias Koch](https://github.com/matkoch) and the [NUKE contributors](https://github.com/nuke-build/nuke/graphs/contributors) — for creating and maintaining NUKE through version 10.x.

If you maintained or contributed to NUKE and want to be credited differently here, please open an issue.
