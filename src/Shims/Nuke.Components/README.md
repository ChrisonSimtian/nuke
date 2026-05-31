# Nuke.Components transition shim

Transition shim for projects mid-migration from NUKE to Fallout. Published to **GitHub Packages** only — `Nuke.Components` on nuget.org is owned by the original NUKE maintainer.

Mirrors the canonical `Fallout.Components` interface family (`ICompile`, `IRestore`, `IPack`, `ITest`, `IPublish`, the `IHas*` family, etc.) under the `Nuke.Components` namespace. Consumer code that says `class Build : NukeBuild, IPack, ITest` continues to compile.

Limitations are the same as the `Nuke.Common` shim — see [`../Nuke.Common/README.md`](../Nuke.Common/README.md).

## Consumer setup

Add this fork's GitHub Packages feed to your `nuget.config`:

```xml
<add key="fallout-shims" value="https://nuget.pkg.github.com/Fallout-build/index.json" />
```

Then bump your `Nuke.Components` package reference to the latest 10.3.x or later.

Full migration walkthrough: [`docs/migration/from-nuke.md`](../../../docs/migration/from-nuke.md).
