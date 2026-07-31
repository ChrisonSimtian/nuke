# Branching and release flow

We aim to follow [Gitlab Flow](https://about.gitlab.com/topics/version-control/what-is-gitlab-flow/), a lightweight Gitflow alternative.
What does that mean for you as a contributor or maintainer of this project?

## How to contribute code

1. You develop on a local fork
2. You raise a PR once your work is ready for review
3. Target `main` on the Fallout-Upstream
4. Your code gets merged

Every push to `main` triggers a pre-release on Github, including publishing nuget packages to Github (but not Nuget.org!).
This is a cheap way to get our hands on pre-release packages without the cost of publishing anything to official Package Repositories.

## How to publish a new release

Sometimes it becomes necessary to create a stabilisation branch to make sure we iron out the worst bugs before pushing a release.
For this purpose Gitlab Flow allows us to create branches, i.e. `release/v1.0`
> [!NOTE]
> While a release branch exists, it becomes necessary to raise some PRs against `release/v1.0` and **then** upmerge those changes against `main` as well

> [!INFO]
> Please make sure to follow our naming pattern for release branches

TODO: Create a documnent describing our naming patterns etc. in detail. Can probably sit with AI instructions.

```mermaid
flowchart TD
    PR(["PR merged into main"]) --> PRE["publish-packages-preview"]
    PRE -->|"env: github-packages"| PREOUT[("GitHub Packages<br/>1.1.0-preview.42.g9f3c1a")]

    TAG(["git tag v1.0.0<br/>pushed on release/v1.0"]) --> VAL{"validate-ref<br/>is the tag on a<br/>release/* or support/* branch?"}
    VAL -->|no| STOP["run fails — nothing published"]
    VAL -->|yes| PACK["test + pack<br/>dotnet fallout Test Pack"]
    PACK --> ART[["artifact: output/packages/*.nupkg"]]

    ART --> JGP["publish → GitHub Packages"]
    ART --> JGR["publish → GitHub Releases"]
    ART -.-> JNO["publish → nuget.org"]

    JGP -->|"env: github-packages"| OGP[("GitHub Packages<br/>every *.nupkg, incl. Nuke.* shims")]
    JGR -->|"env: github-releases"| OGR[("Release page for v1.0.0<br/>nupkgs attached")]
    JNO -->|"env: nuget-org<br/>+ manual approval"| ONO[("nuget.org<br/>Fallout.* only")]

    OPTIN["opt-in only:<br/>workflow_dispatch with<br/>publish-to-nugetorg=true"] -.-> JNO

    classDef optional stroke-dasharray: 5 5
    class JNO,ONO,OPTIN optional
```

### Upmerge (Preferred)

```mermaid
gitGraph
   commit id: "…"
   branch feature/my-contribution
   commit id: "work"
   checkout main
   merge feature/my-contribution
   branch release/v1.0
   commit id: "pin version to 1.0" tag: "v1.0.0-rc.1"
   checkout main
   commit id: "unrelated feature"
   checkout release/v1.0
   branch bugfix/crash-on-startup
   commit id: "fix the crash"
   checkout release/v1.0
   merge bugfix/crash-on-startup tag: "v1.0.0-rc.2"
   commit id: "release notes" tag: "v1.0.0"
   checkout main
   merge release/v1.0 id: "upmerge"
   branch release/v1.1
   commit id: "pin version to 1.1" tag: "v1.1.0"
```

### Cherry Picking

```mermaid
gitGraph
   commit id: "…"
   branch feature/my-contribution
   commit id: "work"
   commit id: "review fixes"
   checkout main
   merge feature/my-contribution
   commit id: "more preview work"
   branch release/v1.0
   commit id: "pin version to 1.0" tag: "v1.0.0-rc.1"
   checkout main
   commit id: "unrelated feature"
   checkout release/v1.0
   branch bugfix/crash-on-startup
   commit id: "fix the crash"
   checkout release/v1.0
   merge bugfix/crash-on-startup tag: "v1.0.0-rc.2"
   checkout main
   cherry-pick id: "fix the crash"
   checkout release/v1.0
   commit id: "release notes" tag: "v1.0.0"
   checkout main
   commit id: "next round of work"
   branch release/v1.1
   commit id: "pin version to 1.1" tag: "v1.1.0"
```

### Support and Retirement of old release/v* branches

We use the `release/v1.0` branch after the release to be able to provide support, i.e. hotfixes to the release but otherwise it stays stagnant. This branch keeps living on until we decided to cut the next release `v1.1` and successfully published it through its branch `release/v1.1`. **THEN** we can delete the old release branch `release/v1.0` and cease support for this release.
Since we're an open source project and work with git tags, people on an older release can always go back in time, branch off an old version and apply their own hotfixes. We are happy to accept those as a PR, re-open the old release branch and publish another hotfix release **if** and **when** we see the need.

> [!WARNING]
> The release branch `release/v1.0` stays alive but stagnant, `main` moves forward. Once we cut release `v1.1` we introduce branch `release/v1.1` and the previous release branch `release/v1.0` can retire

Once we feel comfortable with our release, we can `git tag` our release with the appropiate version, which triggers [`publish-packages-release.yml`](../.github/workflows/publish-packages-release.yml) to run the publish release pipeline.

## References

- [CONTRIBUTING.md](../CONTRIBUTING.md)
- [docs/agents/release-and-versioning.md](agents/release-and-versioning.md)

- TODO: Mermaid diagram showing the branches and maybe a few examples of how to merge
- TODO: cli commands examples for release candidate and actual release
