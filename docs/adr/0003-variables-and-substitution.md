# ADR-0003 — Variables and `${…}` substitution layer

- **Status:** Proposed
- **Date:** 2026-05-27
- **Deciders:** Fallout maintainers
- **Relates to:** [ADR-0002](0002-cross-provider-auth-and-secret-conventions.md) (secret model this layers on), issue [#213](https://github.com/Fallout-build/Fallout/issues/213) (feature ask), milestone [v11](https://github.com/Fallout-build/Fallout/milestone/6) (delivery), milestone [v12](https://github.com/Fallout-build/Fallout/milestone/7) (plugin SDK seam promotion)

## Context

ADR-0002 codified how *secrets* flow through the build:

```
[Parameter, Secret] readonly string OctopusApiKey;
```

…with a six-step resolution chain (CLI → env → value-provider → params-file → keychain → prompt) and one canonical wire name per declaration. That covers the *sensitive* half of the story well.

What it does **not** cover, and what consumers keep working around in their own build code, is the *non-sensitive* half:

- **Named non-secret values per environment** — `apiBaseUrl`, `slackChannel`, `feedUrl`. Today modelled as `[Parameter] readonly string ApiBaseUrl = "https://dev.api.example.com"` with C# expressions for any per-environment switch (`IsServerBuild ? prod : dev`).
- **Composition between named values** — `apiBaseUrl = "https://${env}.api.example.com"`. Today every consumer rolls their own string interpolation in C#, often computed lazily on a property getter. Each build re-invents the wheel.
- **Reference from tool argument strings** — `DockerTasks.DockerRun(_ => _.SetImage("$(Registry)/myapp:$(Version)"))`. Today this is plain C# string interpolation at the call site; nothing in the framework resolves a templating syntax for the tool runner.
- **Reference from CI-config emit** — the `[GitHubActions]` generator hardcodes env-var injection for secrets but has no story for variables. If a consumer wants `image: ${{ vars.IMAGE_PREFIX }}-${{ vars.ENV }}` in an emitted workflow step, they hand-write the workflow.
- **Layered configuration with explicit precedence** — `parameters.json` (defaults) + `parameters.<profile>.json` (overrides) + CLI args + env vars. The mechanism exists, but only for `[Parameter]`-declared fields with concrete C# names. There's no consumer-extensible "configuration variables" namespace that doesn't require a C# field declaration per entry.

Three concrete signals that the gap matters:

1. **`Build.cs` files in the wild grow string-interpolation boilerplate.** Search any sizeable Fallout (formerly NUKE) consumer's `Build.cs` and you'll find ten variants of `$"{baseUrl}/{path}"` and `IsServerBuild ? ... : ...` constructing what is, structurally, layered configuration.
2. **CI-config generators leave variables on the table.** ADR-0002 covered `ImportSecrets`. There's no `ImportVariables` and no substitution into the generated YAML. Consumers regenerate workflows and then hand-edit, which is what `[AutoGenerate(false)]` and hand-written `release.yml` are: escape hatches around the gap.
3. **Plugin SDK foreshadowing** (#213, milestone v12). When plugins ship, they need a clean way to consume both secrets and non-secret configuration without each plugin reinventing the resolver. The cleanest path is to extend ADR-0002's resolution chain to non-secret values *now*, then promote the seam to the public SDK in v12.

Variables and secrets are not separate features. They're the same feature with a `Sensitive=true` flag:

- Both are named-value declarations consumers manage per repo / profile / environment.
- Both want the same substitution syntax in the same places (`${name}` in tool args, in CI config, in artifact paths).
- Both flow through the same resolution chain (CLI > env > file > defaults).
- The only meaningful runtime difference is "is this value sensitive": secrets get registered with the masking layer ADR-0002 §6 calls out, never echoed into committed YAML, and stored encrypted in `parameters.json`. Variables don't.

Treating them as one feature with a `Sensitive` toggle keeps the consumer mental model small and reuses ADR-0002's resolution infrastructure end-to-end.

## Decision

### Core rules

1. **Substitution syntax is `${Name}`.** Single delimiter for the whole framework. Escaped via `$${literal}` (the engine emits `${literal}` and does not recurse). Inspired by Bash and Docker Compose — the two formats consumers most often already know. Mustache (`{{name}}`) was considered and rejected (see Alternatives §A).

2. **Substitution targets are *any string-valued parameter and any tool argument string.*** The substitution engine runs in two places:

   1. **Parameter injection time.** When a `[Parameter] readonly string ApiBaseUrl` is being resolved and the resolved value is `"https://${Env}.api.example.com"`, the engine substitutes `${Env}` before the value lands on the field. Recursion is depth-limited (default 5; tunable via the engine for tests) with cycle detection.
   2. **Tool argument construction time.** Tool wrappers (`DockerTasks`, `MSBuildTasks`, etc.) run any string argument through the same engine before passing it to the subprocess. This is the path that lets `DockerTasks.DockerRun(_ => _.SetImage("$(Registry)/${App}:${Version}"))` work without consumer-side interpolation.

   CI-config-emit substitution is **explicitly not** in scope for v11 — the generator emits the raw `${VarName}` string into the YAML for the CI engine itself to resolve (where `vars.VARNAME` is supported), or substitutes at emit time when targeting an engine that doesn't support runtime variables. See Open Question §1.

3. **Variables live alongside parameters in `parameters.json` under a `Variables` block.** New top-level key, parallel to the existing flat `[Parameter]` map:

   ```json5
   {
     "$schema": "./build.schema.json",
     "Configuration": "Release",         // existing [Parameter] resolution, unchanged
     "Variables": {
       "Env": "dev",
       "Registry": "ghcr.io/example",
       "App": "myapp",
       "ApiBaseUrl": "https://${Env}.api.example.com"
     },
     "NuGetApiKey": "v2:..."             // secret, ADR-0002
   }
   ```

   Variables are *named-value entries with substitution support*. They differ from `[Parameter]` in three ways:

   - **No C# field required.** Variables exist purely as named entries in `parameters.json` / profile files. Consumers reference them via `${Name}` in other strings.
   - **They participate in substitution chains.** Variable values themselves can contain `${...}` references to other variables and to `[Parameter]` values, resolved at first-use with cycle detection.
   - **They never appear as method-callable build properties.** Access from C# code is via `FalloutBuild.GetVariable(name)` or a strongly-typed `Variables` indexer; not a generated property. (Strongly-typed access could ship as a v12-era source generator — see Open Question §3.)

4. **The resolution chain extends ADR-0002, in this order:**

   1. CLI argument (`-EnvVar value` or `--var Env=dev` for top-level variables — syntax under Open Question §2)
   2. Environment variable (`FALLOUT_VAR_<NAME>` for variables to avoid colliding with `[Parameter]`'s `<NAME>`)
   3. Profile-layered `parameters.<profile>.json` Variables block (in profile-stack order, last wins)
   4. Default `parameters.json` Variables block
   5. *(For secret-flagged entries only)* the value-provider attribute output (e.g. `[AzureKeyVaultSecret]`)
   6. *(For secret-flagged entries only)* `CredentialStore` lookup
   7. *(For secret-flagged entries only)* Interactive prompt

   Non-secret variables stop at step 4. Secret variables continue down the chain exactly as ADR-0002 specifies for `[Parameter, Secret]`. This means the chain is *unified* but truncated for non-secret values — the same code path with different terminal conditions based on the `Sensitive` flag.

5. **`Sensitive` is a first-class property on variable declarations.** A consumer marks a variable sensitive via the same JSON shape used for parameters today (`v2:` prefix in the value; encrypted at rest; registered with the masking layer; redacted in logs and emitted YAML):

   ```json5
   "Variables": {
     "ApiBaseUrl": "https://${Env}.api.example.com",
     "DeployKey":  "v2:base64(salt||nonce||tag||ciphertext)"  // ADR-0002 §2 v2 format
   }
   ```

   The engine treats `v2:`-prefixed (or `v1:`-prefixed legacy) values as sensitive automatically. No separate `Sensitive=true` toggle in the JSON — the encryption envelope IS the toggle. (Equivalent to how today's encrypted `[Parameter, Secret]` values self-identify by their `v2:` prefix in `parameters.json`.)

6. **Public surface for v11 is read-only, narrow, and stable.** Consumers get:

   - **`FalloutBuild.GetVariable(string name)` → `string`** — returns the resolved (substituted) value, or throws if the variable is not defined and no default was provided.
   - **`FalloutBuild.TryGetVariable(string name, out string value)` → `bool`** — non-throwing variant.
   - **`[Variable("Env")] readonly string Env`** — declarative injection for variables that have a C# field, parallel to `[Parameter]`. Optional; the indexer/GetVariable forms work without it.
   - **`${Name}` substitution in any `string`-typed `[Parameter]` value, any `[Variable]` value, and any tool argument.** Engine is internal in v11.

   The substitution engine (`ISubstitutionEngine`), the variable source chain (`IVariableSource`), and any future plugin-author seams stay **internal** for v11, wired via `InternalsVisibleTo` to first-party assemblies and tests only. They become candidates for public promotion in v12 alongside `Fallout.Plugin.Sdk` (see §7).

7. **Plugin SDK seams are reserved for v12 but designed in v11.** The internal interfaces shipped in v11 are written as if they were public — XML docs, stable signatures, no leaky abstractions over MEF / reflection / DI primitives — so that v12's plugin SDK work is purely "promote internal to public via attribute changes" rather than "redesign the API first":

   ```csharp
   // Internal in v11; public in v12 as Fallout.Plugin.Sdk.IVariableResolver
   internal interface IVariableResolver
   {
       /// <summary>Returns null when the variable is unknown to this resolver.</summary>
       string TryResolve(string variableName, ResolutionContext context);
   }
   ```

   ADR-0002 §5 already locked in that plugins receive *resolved* values, not raw stores. The same rule applies here: a plugin can implement `IVariableResolver` to source values from an external system (Vault, 1Password) but cannot read `parameters.json` or `CredentialStore` directly.

### Worked example

A consumer's `Build.cs`:

```csharp
[Variable] readonly string Env;
[Parameter] readonly string ApiBaseUrl;
[Parameter, Secret] readonly string DeployKey;

Target Deploy => _ => _
    .Requires(() => DeployKey)
    .Executes(() =>
    {
        DockerTasks.DockerRun(_ => _
            .SetImage($"ghcr.io/example/myapp:${{Version}}")  // substituted at tool call time
            .SetEnv("API_BASE_URL", ApiBaseUrl)              // substituted at parameter injection time
            .SetEnv("DEPLOY_KEY", DeployKey));               // resolved + masked end-to-end
    });
```

`parameters.json`:

```json5
{
  "Variables": {
    "Env": "prod",
    "Version": "10.4.0",
    "ApiBaseUrl": "https://${Env}.api.example.com"
  },
  "DeployKey": "v2:..."
}
```

At injection time:

- `Env` resolves to `"prod"`.
- `Version` resolves to `"10.4.0"`.
- `ApiBaseUrl` resolves to `"https://${Env}.api.example.com"`, which the substitution engine reduces to `"https://prod.api.example.com"` (depth 1).
- `DeployKey` resolves via ADR-0002's chain, registered with the masking layer.

At tool call time:

- `SetImage($"ghcr.io/example/myapp:${{Version}}")` — the literal `${Version}` string is substituted to `"ghcr.io/example/myapp:10.4.0"` before the Docker subprocess sees it.

Single declaration per concept. Substitution is consistent across both injection-time and call-time paths.

### CI-config generator interaction

For v11, the `[GitHubActions]` generator (and siblings) emits two behaviours, distinguished by the `Sensitive` flag:

- **Non-sensitive variables that are statically known at generation time** (i.e., the value is literal — no nested `${...}` references that aren't yet bound) are substituted *at emit time* and baked into the YAML. The generated workflow contains the literal value.
- **Sensitive values are never substituted at emit time.** ADR-0002 §6's masking discipline applies — the workflow YAML emits `${{ secrets.DEPLOY_KEY }}`-style placeholders that the CI engine resolves at runtime.
- **Variables with unresolved `${...}` references at emit time** (because they depend on a runtime-resolved input) are passed through verbatim into the YAML. The expectation is that the consumer wires those into the CI's own variable system. v12 may grow first-class `ImportVariables` on `[GitHubActions]` parallel to today's `ImportSecrets`; **out of scope for v11**.

### Anti-patterns

Same shape as ADR-0002 §"Anti-patterns" but specific to the variable layer:

1. **Hardcoded environment-dependent strings in `Build.cs`.** `var baseUrl = IsServerBuild ? "https://prod" : "https://dev";` — the variable layer exists exactly to replace this. Move into `parameters.json` / profiles.
2. **`${...}` in committed YAML for a variable that has no CI-engine equivalent.** Emit-time substitution covers static cases; for runtime cases the consumer needs to ensure the CI provides the variable. Otherwise the workflow has a dangling reference.
3. **Plugins reading `parameters.json` directly to harvest variables.** Same rule as secrets (ADR-0002 §5) — plugins consume *resolved* values via the SDK-exposed build context. Plugins that need a new variable source register an `IVariableResolver` (v12 public surface).
4. **Substitution cycles.** `A = ${B}`, `B = ${A}`. The engine fails fast on detected cycles with a clear error including the cycle path. Banned in source review; caught at runtime.
5. **`${UNDEFINED_VAR}` references.** Default behaviour: throw with a clear "variable not defined; declared in: …" error. Escape hatch: `${VarName:-fallback}` Bash-style default (see Open Question §2).

## Open questions

These don't block accepting the ADR; they're follow-ups during implementation.

1. **CI-config emit-time substitution scope.** v11 emits literals where possible and passes `${...}` through verbatim where not. v12 grows `[GitHubActions(ImportVariables = ...)]` parallel to `ImportSecrets`. **Action:** track v12 work under #213 once this ADR lands; v11 ships the emit-time-substitution behaviour as the floor.

2. **CLI override syntax for variables.** `[Parameter]` overrides use `-Name value`. For variables, options:
   - `--var Env=prod` (explicit prefix, no collision risk)
   - `-Env prod` (same shape as `[Parameter]`, but means we need to dispatch by-name across both name spaces — feasible since variables and parameters can't share a name, but adds resolution complexity)
   - Reject CLI overrides for variables; profiles are the override mechanism.
   
   **Recommendation:** `--var Env=prod` for v11. Explicit, no collisions, future-proof. Reject the third option (consumers want CLI overrides for one-off runs).

3. **Strongly-typed access generator.** Today `[Parameter] readonly string Env;` injects via reflection. A source generator could emit `partial class Build { public string Env { get; } }` for every variable declared in `parameters.json`. **Action:** v12+. Out of scope for v11; the `[Variable]` attribute + `GetVariable(name)` indexer is the public surface.

4. **`${VarName:-default}` Bash-style fallback syntax.** Convenient; risks complexity creep. **Recommendation:** ship the simple `${Name}` syntax in v11, add `:-` fallback in a follow-up if demand exists. The substitution-fails-with-clear-error behaviour is friendlier than silent fallback for the common case.

5. **Variable visibility across plugin boundaries.** A v12 plugin might declare its own variables (e.g., `Fallout.Plugin.Octopus` exposes `OctopusEnvironment`). Convention: plugin variables are prefixed by plugin name (`Octopus_Environment`) to avoid collisions. Enforced by the plugin loader. **Action:** v12 plugin SDK design.

6. **Where does the substitution engine live in the assembly graph?** Today, parameter resolution is in `Fallout.Common.ValueInjection`. Adding substitution there avoids cross-assembly deps. Plugin SDK promotion in v12 may want it in a smaller seam-only assembly (`Fallout.Plugin.Sdk.Abstractions`). **Action:** ship in `Fallout.Common.ValueInjection` for v11; revisit when the v12 SDK assembly carve-out happens.

## Consequences

### Positive

- **One mental model across secrets and variables.** Consumers learn ADR-0002's resolution chain once; the variable layer reuses the same chain with a sensitivity flag controlling the bottom four steps. Reduces concept surface.
- **Eliminates per-consumer string-interpolation boilerplate.** The "I have a base URL and an env" pattern stops being a `Build.cs` problem and becomes a `parameters.json` problem. Same data in one place.
- **CI-portable configuration.** `Variables` in `parameters.json` works identically locally, in GitHub Actions, in any future CI provider — same way `[Parameter]` already does. No CI-specific config files.
- **Plugin-SDK-ready in v11.** The internal `IVariableResolver` / `ISubstitutionEngine` seams are designed for promotion. v12 turns them public via attribute changes, not a redesign. Concretely de-risks v12.
- **Tool-argument substitution is "free" for tool wrappers.** Wrappers don't need to opt in — the substitution engine runs over their `string` arguments before subprocess invocation. One pass; covers all 70+ tools.

### Negative

- **More features means more failure modes.** Substitution cycles, undefined references, depth limits — each is a new error class consumers can hit. The error messages need to be excellent; a confusing "Could not resolve `${Foo}`" is a worse footgun than the C# string interpolation it replaces. **Mitigated by clear error messages with the cycle path / resolution chain attempted.**
- **JSON schema for `parameters.json` grows.** Today `build.schema.json` is generated from `[Parameter]` declarations. The schema generator now also emits `Variables` as a nested object, with sensitivity-aware entries. The generator (`SchemaUtility.cs`) needs an extension; tracked as implementation work.
- **Two ways to declare a value.** `[Parameter]` (existing) vs `Variables` block (new). Consumers must learn when to use which. **Guidance**: if the value needs strong-typed C# access from a `Target`, declare it as `[Parameter]`. If it's purely a configuration string referenced from other config or tool args, put it in `Variables`. Both compose via substitution.
- **Tool-argument substitution is a behavior change.** Any tool argument string a consumer passes today that *happens* to contain a literal `${...}` substring gets eagerly substituted in v11. **Mitigation:** the engine substitutes only `${Name}` where `Name` is a known variable — unknown names throw. Literal-`${...}` strings in tool args that don't match a variable are a pre-existing typo, not a new failure mode. Escape via `$${Name}` if the literal is intentional.

### Neutral

- **Schema migration is automatic.** Consumers who don't add a `Variables` block see no behaviour change. The block is opt-in.

## Alternatives considered

### A. `{{name}}` Mustache syntax instead of `${name}`

**Rejected.** Same reasoning ADR-0002 used for `SCREAMING_SNAKE`: pick the form consumers already know. Bash and Docker Compose's `${name}` is more familiar to the build-automation audience than Mustache/Handlebars-style `{{name}}`. `${...}` also avoids collision with the C# raw-string-literal `{{}}` escape and Razor / Liquid templates that consumers may have committed elsewhere in their repo.

### B. Treat variables as a separate subsystem with its own resolution chain

I.e., variables have a different chain (e.g., no CredentialStore terminal step, no encrypted storage) and a separate substitution engine.

**Rejected.** Doubles the surface area for consumers and plugins. The "secrets are variables with `Sensitive=true`" framing is the integration that actually pays off — one chain, one engine, one mental model. The terminal steps that don't apply to non-sensitive entries simply don't fire; the chain is unified, not bifurcated.

### C. Use Microsoft.Extensions.Configuration (`IConfiguration`)

Lean on `Microsoft.Extensions.Configuration`'s layered providers and `${}` substitution via custom binders.

**Deferred.** `IConfiguration` is a strong fit for long-running hosted apps; build automation has different lifetime / interactivity needs (e.g., the CLI-level `:secrets` flow, the encrypted parameters file format). ADR-0002's deferral on the same grounds applies. **Reevaluate** if maintenance cost rises or if v12 plugin work surfaces a configuration shape that maps cleanly. For v11, keep ownership of the engine in `Fallout.Common.ValueInjection`.

### D. Source-generator-only public surface

Skip the `IVariableResolver` interface; rely on a source generator to bake variables into compile-time symbols.

**Rejected for v11.** Source generators can't reach values that live in `parameters.json` at *consumer* repos — only the generator runs at the framework build, not at the consumer build. To support `parameters.<profile>.json` at consumer build time we need runtime resolution, which is what `IVariableResolver` is for. The strongly-typed source generator from Open Question §3 *layers on top* of runtime resolution; it's not a replacement.

## References

- [ADR-0002](0002-cross-provider-auth-and-secret-conventions.md) — the secret resolution chain this layers on
- Issue [#213](https://github.com/Fallout-build/Fallout/issues/213) — feature ask + design questions that motivated this ADR
- Issue [#212](https://github.com/Fallout-build/Fallout/issues/212) — corrected `v2:` encryption format that underpins sensitive variable storage
- `src/Fallout.Build/ParameterAttribute.cs:34` — existing `[Parameter]` attribute (extended, not replaced)
- `src/Fallout.Common/ValueInjection/` — current parameter-resolution code path; substitution engine lands alongside
- `src/Fallout.Build/Utilities/SchemaUtility.cs` — `build.schema.json` generator; gains a `Variables` block emitter as part of implementation
- Future SDK work tracked under milestone [v12](https://github.com/Fallout-build/Fallout/milestone/7) — `IVariableResolver` / `ISubstitutionEngine` public promotion, plugin-author `Fallout.Plugin.Sdk.Abstractions` carve-out
