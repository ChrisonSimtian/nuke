<p align="center">
  <img width="320" src=".assets/fallout-logo.svg" alt="Fallout — .NET build system" />
</p>

<p align="center">
  <strong>📖 Documentation: <a href="https://docs.fallout.build/">docs.fallout.build</a></strong>
</p>

> 📦 **Fallout is the successor to NUKE.** [Migrating from NUKE →](docs/migration/from-nuke.md)

# Fallout

> Build automation for C#/.NET — the hard-fork successor to NUKE.

[![Docs](https://img.shields.io/badge/docs-docs.fallout.build-blue?logo=readthedocs&logoColor=white)](https://docs.fallout.build/)
[![CI](https://github.com/Fallout-build/Fallout/actions/workflows/ubuntu-latest.yml/badge.svg)](https://github.com/Fallout-build/Fallout/actions/workflows/ubuntu-latest.yml)
[![NuGet](https://img.shields.io/nuget/v/Fallout.Common?label=Fallout.Common)](https://www.nuget.org/packages/Fallout.Common)
[![NuGet downloads](https://img.shields.io/nuget/dt/Fallout.Common?label=downloads)](https://www.nuget.org/packages/Fallout.Common)
[![Latest release](https://img.shields.io/github/v/release/Fallout-build/Fallout?label=release)](https://github.com/Fallout-build/Fallout/releases/latest)
[![GitHub last commit](https://img.shields.io/github/last-commit/Fallout-build/Fallout)](https://github.com/Fallout-build/Fallout/commits/main)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dot.net)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Open issues](https://img.shields.io/github/issues/Fallout-build/Fallout)](https://github.com/Fallout-build/Fallout/issues)
[![Open PRs](https://img.shields.io/github/issues-pr/Fallout-build/Fallout)](https://github.com/Fallout-build/Fallout/pulls)
[![GitHub Sponsors](https://img.shields.io/github/sponsors/ChrisonSimtian?label=sponsor&logo=githubsponsors&color=EA4AAA)](https://github.com/sponsors/ChrisonSimtian)

> [!IMPORTANT]
> **Rebrand in progress + roadmap published.** This repository is being renamed from **NUKE** to **Fallout** as part of a hard fork. URLs, package names, and namespaces are migrating in stages.
>
> **Versioning & channels.** Fallout ships on **calendar versions** (`YYYY.MINOR.PATCH`; the major is the year) via a maturity ladder: `main` is the deliberate **preview** trunk where work stabilises; the `release/YYYY` **production line** carries non-breaking minors/patches after the yearly cut. GitHub Packages = test/preview; nuget.org = production. See [ADR-0004](docs/adr/0004-calendar-versioning-and-dual-pace-channels.md) (as amended by [ADR-0008](docs/adr/0008-collapse-experimental-into-main.md)) and [docs/branching-and-release.md](docs/branching-and-release.md).
>
> **What's next:** the rebrand completes and the internal foundation for a plugin architecture lands on the 2026 line; a later major ships the public `Fallout.Plugin.Sdk`. The full plan is in [**docs/roadmap.md**](docs/roadmap.md). Five RFCs are open now to shape the SDK — your input matters most before it firms up.
>
> Track the work in [milestone #6](https://github.com/Fallout-build/Fallout/milestone/6) (rebrand + plugin foundation) and [milestone #7](https://github.com/Fallout-build/Fallout/milestone/7) (public plugin SDK).

## Based on NUKE

Fallout is the successor to **[NUKE](https://github.com/nuke-build/nuke)**, originally created by **Matthias Koch** ([@matkoch](https://github.com/matkoch)) and many contributors. Fallout continues NUKE's mission as a C#-first build automation framework for .NET — under new maintenance, with an enterprise-CI/CD focus.

The original NUKE code is preserved here under the MIT License with attribution. Major version 10.x was the last NUKE release; everything from this fork forward carries the Fallout identity.

### Migrating from NUKE

If you maintain a NUKE-based build, **[docs/migration/from-nuke.md](docs/migration/from-nuke.md)** walks you through it. The short version:

```sh
dotnet tool install -g Fallout.Migrate
cd path/to/your-nuke-repo
fallout-migrate
```

## Install

```sh
dotnet tool install -g Fallout.GlobalTools
```

The CLI installs as `fallout`. Verify with `fallout --help`.

> [!NOTE]
> **Upgrading from `Fallout.GlobalTool`?** The dotnet-tool package is now `Fallout.GlobalTools` — same `fallout` command. Uninstall the old one first so you don't end up with two tools claiming the same command:
>
> ```sh
> dotnet tool uninstall -g Fallout.GlobalTool
> ```

For per-repo manifest pinning (`.config/dotnet-tools.json`), project setup, and shell completion, see the [Installation guide on docs.fallout.build](https://docs.fallout.build/getting-started/installation).

> [!NOTE]
> **Channels.** Stable releases ship on **calendar versions** (`YYYY.MINOR.PATCH`, e.g. `2026.1.3`; the major is the year) from the `release/YYYY` production line — published to GitHub Packages, with nuget.org publishing opt-in per release. The `main` integration trunk publishes a faster `…-preview.…` prerelease to **GitHub Packages only**; opt in by adding the GitHub Packages feed and a prerelease version range. The legacy NUKE `10.x` line (`support/v10`) stays on semver and receives security/critical fixes only. See [ADR-0004](docs/adr/0004-calendar-versioning-and-dual-pace-channels.md) (as amended by [ADR-0008](docs/adr/0008-collapse-experimental-into-main.md)) and [docs/branching-and-release.md](docs/branching-and-release.md) for the full model.

## Table of Contents

- [Elevator Pitch](#elevator-pitch)
- [Build Status](#build-status)
- [Activity](#activity)
- [Sponsorship](#sponsorship)

## Elevator Pitch

Solid and scalable CI/CD pipelines are an essential pillar for being competitive and creating a great product. But why are most of us a little afraid of touching YAML files and don't even dare to look at build scripts? Much of this is because C# developers are spoiled with a great language and smart IDEs, and they don't like missing their buddy for code-completion, ease of debugging, refactorings, and code formatting.

Fallout (NUKE's successor) brings your build automation to an even level with every other .NET project. How? It's a regular console application allowing all the OOP goodness! Besides, it solves many common problems in build automation, like parameter injection, path separator abstraction, access to solution and project models, and build step sharing across repositories. Fallout can also generate CI/CD configurations (YAML, etc.) that automatically parallelize build steps on multiple agents to optimize throughput!

## Build Status

CI runs on every PR targeting `main`, `release/*`, or `support/*` across `ubuntu-latest` — the only required status check. After merge, post-merge validation runs on `windows-latest` and `macos-latest`, and a `…-preview` prerelease is published to **GitHub Packages** under the reserved `Fallout.*` prefix from `main`. **Stable** releases fire from `release/YYYY` tags via `.github/workflows/release.yml` (GitHub Packages + GitHub Releases by default; nuget.org opt-in per release). Docs-only PRs are served by a no-op companion workflow (`ubuntu-latest-docs`) so branch protection is satisfied without spending CI minutes on a real build.

| Workflow | Status | Trigger |
|---|---|---|
| [`ubuntu-latest`](.github/workflows/ubuntu-latest.yml) | [![ubuntu-latest](https://img.shields.io/github/actions/workflow/status/Fallout-build/Fallout/ubuntu-latest.yml?branch=main&label=&logo=ubuntu&logoColor=white&style=flat-square)](https://github.com/Fallout-build/Fallout/actions/workflows/ubuntu-latest.yml) | PR to `main` / `release/*` / `support/*` (code paths) — **required check** |
| [`windows-latest`](.github/workflows/windows-latest.yml) | [![windows-latest](https://img.shields.io/github/actions/workflow/status/Fallout-build/Fallout/windows-latest.yml?branch=main&label=&logo=windows&logoColor=white&style=flat-square)](https://github.com/Fallout-build/Fallout/actions/workflows/windows-latest.yml) | push to those branches (post-merge validation) |
| [`macos-latest`](.github/workflows/macos-latest.yml) | [![macos-latest](https://img.shields.io/github/actions/workflow/status/Fallout-build/Fallout/macos-latest.yml?branch=main&label=&logo=apple&logoColor=white&style=flat-square)](https://github.com/Fallout-build/Fallout/actions/workflows/macos-latest.yml) | push to those branches (post-merge validation) |
| [`preview`](.github/workflows/preview.yml) | [![preview](https://img.shields.io/github/actions/workflow/status/Fallout-build/Fallout/preview.yml?branch=main&label=&logo=githubactions&logoColor=white&style=flat-square)](https://github.com/Fallout-build/Fallout/actions/workflows/preview.yml) | push to `main` → `…-preview` prerelease to GitHub Packages |
| [`release`](.github/workflows/release.yml) | [![release](https://img.shields.io/github/actions/workflow/status/Fallout-build/Fallout/release.yml?branch=main&label=&logo=nuget&logoColor=white&style=flat-square)](https://github.com/Fallout-build/Fallout/actions/workflows/release.yml) | tag push on `release/YYYY` (stable) or `support/*` (legacy/retired) — nuget.org opt-in |

Multi-provider CI support (Azure Pipelines, GitLab, TeamCity, AppVeyor) was removed during the takeover and is being revived demand-driven — see [#8](https://github.com/Fallout-build/Fallout/issues/8).

## Activity

### Commits, issues, PRs (rolling 30 days)

![Repobeats analytics image](https://repobeats.axiom.co/api/embed/13bc62ac87b75cea7be4bdbbcba90af620665333.svg "Repobeats analytics image")

Generated by [Repobeats](https://repobeats.axiom.co).

### Stars over time

[![RepoStars](https://repostars.dev/api/embed?repo=Fallout-build%2FFallout&theme=terminal)](https://repostars.dev/?repos=Fallout-build%2FFallout&theme=terminal)

Generated by [repostars.dev](https://www.repostars.dev/). Auto-updates as new stargazers arrive.

## Sponsorship

Fallout is volunteer-run. There's no donation channel yet, but we want to be transparent about what running the project costs — see [`costs.md`](costs.md) for the full list. If you or your organisation would like to help offset those costs, open an issue and we'll work out the details.

## Credits

- [Matthias Koch](https://github.com/matkoch) and the [NUKE contributors](https://github.com/nuke-build/nuke/graphs/contributors) — for creating and maintaining NUKE through version 10.x.

If you maintained or contributed to NUKE and want to be credited differently here, please open an issue.
