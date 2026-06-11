# Local NuGet server (Tier 3 release channel)

A Docker-based NuGet server used to verify that `Fallout.*` changes work in a
real consumer project **before** they merge — without polluting the
GitHub Packages feed (Tier 2) with abandoned PR builds.

Tracking issue: [#279](https://github.com/Fallout-build/Fallout/issues/279).
Channel taxonomy context: [#267](https://github.com/Fallout-build/Fallout/issues/267).

| Tier | Channel              | Audience            | Status                          |
| ---- | -------------------- | ------------------- | ------------------------------- |
| 1    | nuget.org            | production / users  | wired (#272, #273)              |
| 2    | GitHub Packages      | beta / bleeding     | env exists                      |
| 3    | **this local feed**  | pre-merge testing   | **minimum-viable (this PR)**    |

## What this gives you

A locally-running NuGet v3 feed at `http://localhost:5555/v3/index.json` that
accepts `dotnet nuget push` (with an API key) and serves restores. Useful for
manually validating a `Fallout.*` change against a real consumer shape before
opening or merging a PR.

The server is [BaGet](https://github.com/loic-sharma/BaGet) — an open-source
.NET NuGet server with an official Docker image. Note: the BaGet repo is
stale (last commit Jan 2023) but the image and NuGet v3 protocol surface
remain functional. A follow-up may swap to a maintained alternative; the
client-side workflow below is feed-agnostic.

## Prerequisites

- Docker / Docker Desktop installed and running.
- .NET SDK matching `global.json` (for `dotnet pack` / `dotnet nuget push`).

## Start the server

```powershell
# From the repo root:
docker compose -f tests/integration/docker-compose.yml up -d

# Verify it's up:
curl http://localhost:5555/v3/index.json
```

The feed URL is `http://localhost:5555/v3/index.json`. Package data persists
in the named Docker volume `fallout-local-nuget-data` across restarts.

The API key defaults to `FALLOUT_LOCAL_DEV` (static, dev-only). Override via
the `FALLOUT_LOCAL_NUGET_API_KEY` environment variable before `docker compose
up` if you need a different one.

## Push a package

```powershell
# Pack a Fallout.* project (example: Fallout.Common):
dotnet pack src/Fallout.Common/Fallout.Common.csproj -c Release -o artifacts/

# Push to the local feed:
dotnet nuget push artifacts/Fallout.Common.*.nupkg `
  --source http://localhost:5555/v3/index.json `
  --api-key FALLOUT_LOCAL_DEV
```

To push every `.nupkg` produced by a full `./build.ps1 Pack`:

```powershell
Get-ChildItem output/*.nupkg | ForEach-Object {
  dotnet nuget push $_.FullName `
    --source http://localhost:5555/v3/index.json `
    --api-key FALLOUT_LOCAL_DEV `
    --skip-duplicate
}
```

## Consume from a project

Pick one:

**Ad-hoc on the command line:**

```powershell
dotnet restore --source http://localhost:5555/v3/index.json `
               --source https://api.nuget.org/v3/index.json
```

**Via `nuget.config` in the consumer project:**

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="fallout-local" value="http://localhost:5555/v3/index.json" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

For PR-scoped builds, use a pre-release version suffix (e.g.
`12.0.0-pr.NNN.YYYYMMDD.SHA`) so different PR runs don't collide on the
same slot. Versioning automation is a follow-up — see "Deferred", below.

## Stop the server

```powershell
docker compose -f tests/integration/docker-compose.yml down

# Also remove the package data volume:
docker compose -f tests/integration/docker-compose.yml down -v
```

## Deferred to follow-ups (tracked in [#279](https://github.com/Fallout-build/Fallout/issues/279))

- Wiring `tests/Consumers/Fallout.Consumer.Local/` to restore from this feed
  automatically.
- A `dotnet fallout` target (or `push-local.ps1` script) that packs all
  `Fallout.*` projects and pushes them to the local feed in one command.
- PR-scoped versioning convention (`Fallout.X.M.P-pr.NNN.YYYYMMDD.SHA`).
- CI integration: GitHub Actions job that stands this up per-PR in an
  ephemeral container and runs the consumer sentinel against it.
- Devcontainer / Codespaces integration so this server is preconfigured
  alongside the .NET SDK.
