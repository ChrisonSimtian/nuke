# ADR-0002 — Cross-provider auth and secret conventions

- **Status:** Proposed
- **Date:** 2026-05-24
- **Deciders:** Fallout maintainers
- **Relates to:** [ADR-0001](0001-cd-primitives-attributes-vs-tasks.md), RFC [#106](https://github.com/Fallout-build/Fallout/issues/106), milestone [v12](https://github.com/Fallout-build/Fallout/milestone/7) (plugin SDK), milestone [v13](https://github.com/Fallout-build/Fallout/milestone/8) (CD vision)

## Context

[ADR-0001](0001-cd-primitives-attributes-vs-tasks.md) introduces multiple CD providers — first-party GitHub adapters in-tree, third-party adapters (Octopus, GitLab, …) shipped as v12 plugins. Each provider has its own secret store with its own naming conventions, scoping rules, and injection mechanisms. Without a shared convention, every provider invents its own and consumers end up writing the same field three times with three names.

Fallout already has mature primitives that are scattered and worth surfacing:

- **`[Parameter]`** (`src/Fallout.Build/ParameterAttribute.cs:34`) — declarative field injection from CLI args + env vars.
- **`[Secret]`** (`src/Fallout.Build/ParameterAttribute.cs:76`) — marker attribute; semantically "do not log this value." Currently a passive marker — actual log-scrubbing behaviour needs explicit framework-side support (see Open Questions).
- **`CredentialStore`** (`src/Fallout.Build/Utilities/CredentialStore.cs`) — macOS Keychain wrapper. Linux/Windows fall back to prompt-only.
- **Encrypted parameters file** — `parameters.json` (+ named profiles `parameters.<profile>.json`), AES-encrypted with a password held in the Keychain or prompted for. Managed interactively via `dotnet fallout :secrets` (`src/Fallout.Cli/Program.Secrets.cs`).
- **`ImportSecrets`** on `[GitHubActions]` (`src/Fallout.Common/CI/GitHubActions/GitHubActionsAttribute.cs:65`) — declares which `[Parameter, Secret]` fields the workflow should inject from GitHub's secret store, mapped via `SplitCamelHumpsWithKnownWords().JoinUnderscore().ToUpperInvariant()` (line 217).
- **Provider-sourced injection** — `AzureKeyVaultSecretAttribute`, `AppVeyorSecretAttribute` already exist. They're a third category: **runtime-resolved value providers** that look up the secret from an external store on startup, no env-var hop required.

The drivers for codifying this now:

1. Octopus 2019 (in-flight as v12 plugin) brings a fourth secret-store flavour (Sensitive Variables) on top of GitHub Actions secrets, encrypted parameters file, and Azure Key Vault. A consistent story across all four is cheaper to establish before the second provider lands than after the fifth.
2. Plugins (v12) introduce a **trust boundary**. A third-party plugin must not be free to invent its own secret-handling shortcut, leak values through logs, or pull from arbitrary stores. The convention is also a security policy.
3. CI/CD asymmetry: CI secrets (registry credentials, signing certs) and CD secrets (deploy keys, target-host credentials) often want different scopes. Today both flow through the same `[Parameter, Secret]` — fine, but the *attribute* should be able to express the difference if a consumer asks for it.

## Decision

### Core rules

1. **The C# field is the canonical identifier.** A secret is declared exactly once, as a `[Parameter, Secret]` field on the build class. Everything else — env-var names, CI-provider lookups, plugin access — derives from this name.

   ```csharp
   [Parameter, Secret] readonly string OctopusApiKey;
   ```

2. **One canonical wire form: `SCREAMING_SNAKE`.** Derived from the C# name via the existing `SplitCamelHumpsWithKnownWords().JoinUnderscore().ToUpperInvariant()` helper (today on `GitHubActionsAttribute.GetSecretValue`, line 217). `OctopusApiKey` → `OCTOPUS_API_KEY` everywhere — GitHub secret name, env-var key, Octopus Variable Set entry, GitLab CI/CD Variable. **Mandatory across providers.** Escape hatch: `[Parameter("CUSTOM_NAME")]` if a downstream system locks you to a non-derivable name (this is exactly why the maintainer's `release.yml` is hand-written today — see `build/Build.CI.GitHubActions.cs:41-46`).

3. **Resolution order is provider-independent.** When the build starts, every `[Parameter]` is resolved by walking this chain in order, taking the first non-null:

   1. CLI argument (`-OctopusApiKey <value>`)
   2. Environment variable (`OCTOPUS_API_KEY`)
   3. Value-provider attribute output (e.g. `[AzureKeyVaultSecret(...)]` fetches from Key Vault)
   4. Encrypted parameters file (`parameters.json` / `parameters.<profile>.json`)
   5. `CredentialStore` lookup (macOS Keychain today; Windows DPAPI / Linux libsecret are open work)
   6. Interactive prompt (only when `Host.IsInteractive` — never in CI)

   Same chain regardless of which CI runs the build. Same chain regardless of whether the provider integration is first-party or plugin.

4. **Provider attributes handle *injection*, never *storage*.** `[GitHubActions(ImportSecrets = new[] { nameof(OctopusApiKey) })]` writes the env-var injection into the workflow YAML. That's the provider's entire role. The framework's value-injection layer picks it up via step 2 of the chain above. Symmetric for any other CI: `[GitLabPipeline(ImportSecrets = ...)]`, `[AzurePipelines(ImportSecrets = ...)]`, etc.

5. **Plugins receive resolved values, never raw stores.** The v12 plugin SDK exposes secrets to plugins as `[Parameter, Secret]`-typed values on the build object, already resolved. A plugin **cannot**:
   - Read the encrypted parameters file directly.
   - Call `CredentialStore.TryGetPassword` (or its successors).
   - Add a step-3 value provider that calls out to a network store under its own auth.

   A plugin that needs a new secret declares it as `[Parameter, Secret]` on the build class, and the framework resolves it through the standard chain before the plugin sees it. This is enforced by the SDK surface — `IPlugin` (or equivalent) receives an `IBuildContext` that doesn't expose the raw stores. Plugins authored *for* a remote secret store (a hypothetical `Fallout.Plugin.HashiCorpVault`) can add a value-provider attribute (step 3), but the value still flows through the resolution chain — it does not bypass it.

6. **Log masking is a framework-level service, not provider-level.** Every value resolved into a `[Secret]`-marked field is registered with a central `SensitiveValueRegistry` at injection time. The logging middleware scrubs registered values from all output streams (stdout, stderr, target logs, build summary) before they're written. **This needs verification** — see Open Questions.

### Cross-provider mapping

For a single declaration:

```csharp
[Parameter, Secret] readonly string OctopusApiKey;
```

…the canonical name is `OCTOPUS_API_KEY` (derived once, used everywhere):

| Surface | Where the value lives | How the build sees it |
|---|---|---|
| **Local dev** | `parameters.json` (encrypted, password in Keychain) | Decrypted at startup, injected by `[Parameter]` |
| **CLI override** | passed inline | `-OctopusApiKey <value>` or env `OCTOPUS_API_KEY=…` |
| **GitHub Actions** | Repo or environment secret named `OCTOPUS_API_KEY` | `[GitHubActions(ImportSecrets = new[] { nameof(OctopusApiKey) })]` emits `env: OctopusApiKey: ${{ secrets.OCTOPUS_API_KEY }}` |
| **Octopus Deploy** | Sensitive Variable in the project's variable set | `[OctopusProject(..., ImportSecrets = new[] { nameof(OctopusApiKey) })]` emits the variable mapping when project syncs |
| **GitLab CI** | CI/CD Variable (project or group scope), masked + protected | `[GitLabPipeline(ImportSecrets = ...)]` writes the `variables:` block |
| **Azure Pipelines** | Variable group (or pipeline secret) | `[AzurePipelines(ImportSecrets = ...)]` writes the `secret: true` declaration |
| **Azure Key Vault** | Vault secret named `OCTOPUS_API_KEY` (or arbitrary, with explicit `[AzureKeyVaultSecret(SecretName = "...")]`) | Resolved at startup (step 3 of the chain) — no env-var hop |

One declaration, one canonical name, one resolution chain. Adding a new provider doesn't change the build code — only the attribute on the build class.

### Anti-patterns

The convention only works if these stay banned:

1. **Hardcoded secret in code** — `var key = "abc123";`. Trivially detectable; should fail review.
2. **Secret committed to git in any form except the encrypted parameters file.** That file is encrypted; nothing else is. `.env` files, `secrets.yaml` without `git-crypt`/`sops`, comment-block "for local dev only" — all banned.
3. **Plugin calling its own HTTP client with hard-coded provider auth.** Plugins consume `[Parameter]` values; if a plugin needs Octopus API access, the consuming build declares `[Parameter, Secret] OctopusApiKey` and the plugin reads it via the SDK-exposed build object.
4. **Plaintext CLI arg in CI logs.** Treat `-OctopusApiKey <value>` as a footgun. CI runs should always go through `ImportSecrets`, never positional args. Local dev can use `--from-stdin` if the value isn't already in the parameters file.
5. **Value-provider attributes that mask intent.** `[AzureKeyVaultSecret] readonly string OctopusApiKey` is fine if the secret really is in Key Vault. It is not fine to use it as a way to silently change the resolution source per environment — that just hides the dependency.
6. **Re-deriving the canonical name in user code.** If you find yourself writing `.ToUpperInvariant().Replace("…", "_")`, you're rebuilding the convention. The framework owns the derivation; consumers reference it via `nameof(MyField)` only.

## Open questions

These are decisions the ADR does not close; they need follow-up work or a follow-up ADR.

- **Log-masking implementation status.** The `[Secret]` attribute exists as a marker; I did not find an explicit `RegisterSensitiveValue` / output scrubber. **Action:** confirm whether masking is wired up (and where), or implement it as a small middleware that hooks `IOnBuildCreated` to register every `[Secret]`-marked value with the logging layer. Tracked under v13 alongside the first CD task work.
- **Windows + Linux credential stores.** `CredentialStore.SavePassword` / `TryGetPassword` only handle `PlatformFamily.OSX`. Windows DPAPI (`ProtectedData`) and Linux libsecret are missing — non-macOS users currently fall back to prompts every run. **Action:** separate issue, not blocking the convention but worth a tracking entry under v12 (since plugin authors on Windows will hit this first).
- **Per-environment scoping.** GitHub Environments and Octopus environments both support env-scoped secrets. The convention above treats secret name as flat. Future: `[GitHubEnvironment("production", ImportSecrets = new[] { nameof(ProdDeployKey) })]` declares the secret as environment-scoped on the GitHub side; the C# field stays one declaration. Not in scope for this ADR; flagged for the CD work to bake in from day one.
- **Secret rotation.** No framework concept today for "this secret should be rotated every N days." Probably never the framework's job (the secret store owns lifecycle) — but worth documenting that the build does not enforce rotation; that's a consumer policy.
- **External secret-manager integrations.** Step 3 of the resolution chain is the extension point for remote stores. Azure Key Vault is the reference implementation today; 1Password, Bitwarden Secrets Manager, HashiCorp Vault, AWS Secrets Manager, GCP Secret Manager, Doppler, Infisical are tracked candidates. Each ships as a v12 plugin, not as in-tree code. Running list at issue [#168](https://github.com/Fallout-build/Fallout/issues/168).

## Consequences

### Positive

- **One mental model.** Declare a `[Parameter, Secret]` field, optionally add it to `ImportSecrets` on the relevant provider attribute. Done. The chain handles everything else.
- **Provider portability.** Switching from GitHub Actions to GitLab (or running the same build locally) requires zero code changes; only the provider attribute changes.
- **Plugin sandbox.** The plugin SDK can present a narrow `IBuildContext` that exposes resolved parameter values but no secret stores. Third-party plugins have a clear, auditable surface. (Critical for the Octopus-plugin-as-validation point in ADR-0001.)
- **Composes with existing infrastructure.** `dotnet fallout :secrets`, the encrypted parameters file, the Keychain integration, and `AzureKeyVaultSecretAttribute` all keep their existing semantics. The ADR codifies what's there + adds the cross-provider naming rule.

### Negative

- **Naming inflexibility.** Some consumers will hit a downstream system that *requires* a specific secret name not derivable from PascalCase. They pay for the escape hatch (`[Parameter("custom_name")]`) and an inline comment. Acceptable cost.
- **Plugin authors hit guardrails they may not expect.** "I want to call Vault from my plugin" needs a value-provider attribute, not raw plugin code. This is a feature, not a bug, but needs to be documented in the plugin authoring guide.
- **Linux/Windows local dev is worse than macOS.** Until DPAPI / libsecret support lands, those platforms prompt for the parameters-file password on every run. Surface this in CONTRIBUTING.md so it doesn't surprise contributors.
- **Two attributes for the same secret on the same field.** `[Parameter, Secret] readonly string OctopusApiKey` plus `ImportSecrets = new[] { nameof(OctopusApiKey) }` on the CI attribute. Slightly verbose. The alternative ("the field's `[Secret]` flag auto-imports into all CI providers") is rejected: it makes scope implicit and surprises consumers when a PR-triggered workflow can read a production deploy key.

## Alternatives considered

### A. Provider-namespaced Secret attributes (`[GitHubSecret]`, `[OctopusSecret]`)

Make secret declarations explicitly provider-scoped: `[GitHubSecret] readonly string OctopusApiKey` would only resolve when running under GitHub Actions.

**Rejected.** The same secret often serves multiple uses — `OctopusApiKey` is a GitHub Actions secret (for CI runs that push to Octopus), an Octopus Sensitive Variable (for deployment-time scripts that call back to Octopus), and a local-dev secret. Provider-namespacing forces multiple declarations or arbitrary "primary provider" choices. The canonical-name approach gets the same effect (one declaration, multiple injection points) without the duplication.

### B. Plugins manage their own secrets

Trust plugins to call the credential store / value providers directly.

**Rejected.** Trust boundary. A plugin that handles its own secrets can leak them through logs, telemetry, or unintended HTTP requests, and the build host has no audit point. Forcing all secret access through the resolution chain gives the framework one place to put masking, audit, and access policy.

### C. Native provider naming (no normalisation)

Each provider names its secrets however it wants; the build attribute maps explicitly. `[GitHubActions(ImportSecrets = new Dictionary<string, string> { { nameof(OctopusApiKey), "OCTO_DEPLOY_KEY" } })]`.

**Rejected.** Loses the property that consumers can search the codebase for `OctopusApiKey` and find every place it's used. Adds a name-mapping table per provider. The escape hatch (`[Parameter("explicit_name")]`) covers the rare cases where it matters, without forcing every secret through a mapping.

### D. Adopt a third-party secret library (DotNetEnv, Microsoft.Extensions.Configuration secrets)

Skip the existing infrastructure; layer on a standard .NET configuration provider.

**Deferred.** The existing `CredentialStore` + encrypted parameters file is the right shape for build automation specifically — it survives the absence of a host (.NET Generic Host isn't appropriate for a build). The existing system also has a UX (`dotnet fallout :secrets`) that a generic config library wouldn't match. Revisit if maintenance cost rises; otherwise keep.

## References

- [ADR-0001](0001-cd-primitives-attributes-vs-tasks.md) — the two CD patterns this layers on
- `src/Fallout.Build/ParameterAttribute.cs:34,76` — `ParameterAttribute` and `SecretAttribute` definitions
- `src/Fallout.Build/Utilities/CredentialStore.cs` — macOS Keychain wrapper
- `src/Fallout.Cli/Program.Secrets.cs` — encrypted parameters file CLI
- `src/Fallout.Common/CI/GitHubActions/GitHubActionsAttribute.cs:217` — canonical name derivation (`SplitCamelHumpsWithKnownWords().JoinUnderscore().ToUpperInvariant()`)
- `src/Fallout.Common/Tools/AzureKeyVault/AzureKeyVaultSecretAttribute.cs` — runtime value-provider precedent (step 3 of the resolution chain)
- RFC [#113](https://github.com/Fallout-build/Fallout/issues/113) — deployment agent (needs the resolution chain to authenticate to coordinators)
