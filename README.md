<p align="center">
  <img width="320" src=".assets/fallout-logo.svg" alt="Fallout — .NET build system" />
</p>

<p align="center">
  <strong>📖 Documentation: <a href="https://docs.fallout.build/">docs.fallout.build</a></strong>
</p>

[![NuGet downloads](https://img.shields.io/nuget/dt/Fallout.Common?label=downloads)](https://www.nuget.org/packages/Fallout.Common)
[![GitHub last commit](https://img.shields.io/github/last-commit/Fallout-build/Fallout)](https://github.com/Fallout-build/Fallout/commits/main)
[![Open issues](https://img.shields.io/github/issues/Fallout-build/Fallout)](https://github.com/Fallout-build/Fallout/issues)
[![Open PRs](https://img.shields.io/github/issues-pr/Fallout-build/Fallout)](https://github.com/Fallout-build/Fallout/pulls)
[![GitHub Sponsors](https://img.shields.io/github/sponsors/ChrisonSimtian?label=sponsor&logo=githubsponsors&color=EA4AAA)](https://github.com/sponsors/Fallout-Build)</br>
[![CI](https://img.shields.io/github/actions/workflow/status/Fallout-build/Fallout/publish-packages-preview.yml?branch=main&label=CI&logo=githubactions&logoColor=white)](https://github.com/Fallout-build/Fallout/actions/workflows/publish-packages-preview.yml)
[![Latest release](https://img.shields.io/github/v/release/Fallout-build/Fallout?label=release)](https://github.com/Fallout-build/Fallout/releases/latest)
[![NuGet](https://img.shields.io/nuget/v/Fallout.Common?label=Fallout.Common)](https://www.nuget.org/packages/Fallout.Common)</br>
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dot.net)
[![built with Fallout](https://img.shields.io/badge/built%20with-Fallout-F5C800?logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI%2BPGNpcmNsZSBjeD0iMTIiIGN5PSIxMiIgcj0iMTIiIGZpbGw9IiNGNUM4MDAiLz48ZyBmaWxsPSIjMTExIj48Y2lyY2xlIGN4PSIxMiIgY3k9IjEyIiByPSIzLjEiLz48cGF0aCBkPSJNOS45OSA5LjAyQTMuNiAzLjYgMCAwIDEgMTQuMDEgOS4wMkwxOC40OSAyLjM4QTExLjYgMTEuNiAwIDAgMCA1LjUxIDIuMzhaTTE1LjU5IDExLjc1QTMuNiAzLjYgMCAwIDEgMTMuNTggMTUuMjRMMTcuMDkgMjIuNDNBMTEuNiAxMS42IDAgMCAwIDIzLjU3IDExLjE5Wk0xMC40MiAxNS4yNEEzLjYgMy42IDAgMCAxIDguNDEgMTEuNzVMMC40MyAxMS4xOUExMS42IDExLjYgMCAwIDAgNi45MSAyMi40M1oiLz48L2c%2BPC9zdmc%2B)](docs/badge.md)
[![Docs](https://img.shields.io/badge/docs-docs.fallout.build-blue?logo=readthedocs&logoColor=white)](https://docs.fallout.build/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

# Fallout

> Build automation for C#/.NET — survived the NUKE.

## Based on NUKE

> 📦 **Fallout is the successor to NUKE.** [Migrating from NUKE →](docs/migration/from-nuke.md)

Fallout is the successor to **[NUKE](https://github.com/nuke-build/nuke)**, originally created by **Matthias Koch** ([matkoch](https://github.com/matkoch)) and many contributors. Fallout continues NUKE's mission as a C#-first build automation framework for .NET — under new maintenance, with an enterprise-CI/CD focus.

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
dotnet tool install -g Fallout.GlobalTool
```

The CLI installs as `fallout`. Verify with `fallout --help`.

For per-repo manifest pinning (`.config/dotnet-tools.json`), project setup, and shell completion, see the [Installation guide on docs.fallout.build](https://docs.fallout.build/getting-started/installation).

## Table of Contents

- [Project Description](#project-description)
- [Build Status](#build-status)
- [Contribute](#contribute)
- [Sponsorship](#sponsorship)
- [Activity](#activity)

## Project Description

We all have to deal with CI/CD on our day-to-day, but for some reason those pipelines come with their own language, their own terms, their own platform and every single one works ever so slightly different. And even worse, half of them need to be tested in production.
The promise of Fallout, just like its predecessor NUKE, is to deliver a C#-based pipeline system that is not just local-first (i.e. you can run it on your machine and it does exactly the same thing as it would in the CI. No magic, no emulators, no batteries needed). It also allows you to stay in your beloved dotnet language ecosystem! We aim to provide a highly flexible CI/CD pipeline platform that lets you test, run and expand your builds and deployments the way YOU want.

## Build Status

| Workflow | Status | Trigger |
|---|---|---|
| [`build`](.github/workflows/build.yml) | [![build](https://img.shields.io/github/actions/workflow/status/Fallout-build/Fallout/build.yml?event=pull_request&label=&logo=ubuntu&logoColor=white&style=flat-square)](https://github.com/Fallout-build/Fallout/actions/workflows/build.yml) | PR to `main` / `release/*` / `support/*` (code paths) — job `ubuntu-latest` is the **required check** |
| [`publish-packages-preview`](.github/workflows/publish-packages-preview.yml) | [![publish-packages-preview](https://img.shields.io/github/actions/workflow/status/Fallout-build/Fallout/publish-packages-preview.yml?branch=main&label=&logo=githubactions&logoColor=white&style=flat-square)](https://github.com/Fallout-build/Fallout/actions/workflows/publish-packages-preview.yml) | push to `main` → `…-preview` prerelease to GitHub Packages |
| [`publish-packages-release`](.github/workflows/publish-packages-release.yml) | [![publish-packages-release](https://img.shields.io/github/actions/workflow/status/Fallout-build/Fallout/publish-packages-release.yml?event=push&label=&logo=nuget&logoColor=white&style=flat-square)](https://github.com/Fallout-build/Fallout/actions/workflows/publish-packages-release.yml) | tag push on `release/YYYY` (stable) or `support/*` (legacy/retired) — nuget.org opt-in |

Multi-provider CI support (Azure Pipelines, GitLab, TeamCity, AppVeyor) was removed during the takeover and is being revived demand-driven — see [#8](https://github.com/Fallout-build/Fallout/issues/8).

## Contribute

Want to contribute code, docs, or triage? Start with [CONTRIBUTING.md](CONTRIBUTING.md).

Two things help the project and take about a minute:

- **Star the repo.** It is how most people find Fallout.
- **Show the badge.** If you build with Fallout, link back from your own README. Fallout's own
  build is a Fallout build, so the badge sits at the top of this page too.

[![built with Fallout](https://img.shields.io/badge/built%20with-Fallout-F5C800?logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI%2BPGNpcmNsZSBjeD0iMTIiIGN5PSIxMiIgcj0iMTIiIGZpbGw9IiNGNUM4MDAiLz48ZyBmaWxsPSIjMTExIj48Y2lyY2xlIGN4PSIxMiIgY3k9IjEyIiByPSIzLjEiLz48cGF0aCBkPSJNOS45OSA5LjAyQTMuNiAzLjYgMCAwIDEgMTQuMDEgOS4wMkwxOC40OSAyLjM4QTExLjYgMTEuNiAwIDAgMCA1LjUxIDIuMzhaTTE1LjU5IDExLjc1QTMuNiAzLjYgMCAwIDEgMTMuNTggMTUuMjRMMTcuMDkgMjIuNDNBMTEuNiAxMS42IDAgMCAwIDIzLjU3IDExLjE5Wk0xMC40MiAxNS4yNEEzLjYgMy42IDAgMCAxIDguNDEgMTEuNzVMMC40MyAxMS4xOUExMS42IDExLjYgMCAwIDAgNi45MSAyMi40M1oiLz48L2c%2BPC9zdmc%2B)](https://github.com/Fallout-build/Fallout)

```markdown
[![built with Fallout](https://img.shields.io/badge/built%20with-Fallout-F5C800?logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI%2BPGNpcmNsZSBjeD0iMTIiIGN5PSIxMiIgcj0iMTIiIGZpbGw9IiNGNUM4MDAiLz48ZyBmaWxsPSIjMTExIj48Y2lyY2xlIGN4PSIxMiIgY3k9IjEyIiByPSIzLjEiLz48cGF0aCBkPSJNOS45OSA5LjAyQTMuNiAzLjYgMCAwIDEgMTQuMDEgOS4wMkwxOC40OSAyLjM4QTExLjYgMTEuNiAwIDAgMCA1LjUxIDIuMzhaTTE1LjU5IDExLjc1QTMuNiAzLjYgMCAwIDEgMTMuNTggMTUuMjRMMTcuMDkgMjIuNDNBMTEuNiAxMS42IDAgMCAwIDIzLjU3IDExLjE5Wk0xMC40MiAxNS4yNEEzLjYgMy42IDAgMCAxIDguNDEgMTEuNzVMMC40MyAxMS4xOUExMS42IDExLjYgMCAwIDAgNi45MSAyMi40M1oiLz48L2c%2BPC9zdmc%2B)](https://github.com/Fallout-build/Fallout)
```

More styles and wordings, plus the logo source, are in [docs/badge.md](docs/badge.md).

## Sponsorship

Fallout is volunteer-run. We happily accept sponsorship via the sponsor button on any Fallout repository, or if you want to financially support us through other channels, please reach out to us via [Email](mailto:funding@fallout.build). We will try our best to be as transparent as possible about the running [`Costs`](https://github.com/Fallout-build/.github/blob/main/costs.md) of this project and where your contributions go — they live with the organisation's other shared files, since the spend covers every repository rather than this one. We currently cant offer you a not-for-profit donation statement, as we're not set up as a Not-For-Profit organisation (yet).

## Credits

- [Matthias Koch](https://github.com/matkoch) and the [NUKE contributors](https://github.com/nuke-build/nuke/graphs/contributors) — for creating and maintaining NUKE through version `10.1.0`.

If you maintained or contributed to NUKE and want to be credited differently here, please open an issue.

## Activity

### Commits, issues, PRs (rolling 30 days)

![Repobeats analytics image](https://repobeats.axiom.co/api/embed/c4ea2e2211409a86c7dba874c3ed6aa629efe700.svg "Repobeats analytics image")

Generated by [Repobeats](https://repobeats.axiom.co).

### Stars over time

[![RepoStars](https://repostars.dev/api/embed?repo=Fallout-build%2FFallout&theme=terminal)](https://repostars.dev/?repos=Fallout-build%2FFallout&theme=terminal)

Generated by [repostars.dev](https://www.repostars.dev/). Auto-updates as new stargazers arrive.
