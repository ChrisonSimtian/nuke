# Deliberately hand-rolled code (do not "use a library")

Companion to [dependencies.md](dependencies.md). That page lists the libraries we *do* pull in;
this page records the areas we keep hand-rolled **on purpose**, with the reason, so they aren't
re-litigated in future "replace the reinvented wheel" passes.

These keeps come out of the dependency-consolidation audit (2026-06-02, [#358](https://github.com/ChrisonSimtian/Fallout/issues/358)).
Each area also carries the same rationale as an XML-doc remark on the type itself.

**Before proposing a library swap for anything below, read the reason and the linked discussion first.**

| Area | Files | Why it stays hand-rolled |
|---|---|---|
| Secret encryption | `src/Fallout.Utilities/Security/EncryptionUtility.cs` | Built directly on BCL crypto (`AesGcm` + `Rfc2898DeriveBytes`): AES-GCM, random salt/nonce, PBKDF2-SHA256 at 600,000 iterations. OWASP-aligned and security-audited under [#212](https://github.com/ChrisonSimtian/Fallout/issues/212). This is correct BCL crypto — a third-party crypto library would add risk, not remove it. |
| Path API | `src/Fallout.Utilities/IO/AbsolutePath.cs`, `src/Fallout.Utilities/IO/PathConstruction.cs` (~500 LOC) | Type-safe, OS-independent path model — a core selling point of the framework. The BCL `System.IO.Path` API is string-typed and OS-dependent; our type captures the rooted/relative distinction, the `/` and `+` append operators, and automatic normalization. No general-purpose NuGet path library matches this surface. |
| CI/CD config writers | `src/Fallout.Build/CICD/CustomFileWriter.cs`, `src/Fallout.Build/CICD/ConfigurationEntity.cs`, `src/Fallout.Common/CI/**/Configuration/*Configuration.cs` (~1,800 LOC) | TeamCity and SpaceAutomation emit a **Kotlin DSL** — no YAML/JSON serializer can produce that. The YAML targets (GitHub Actions, Azure Pipelines, AppVeyor) need exact control over comments, quoting, and indentation; a serializer like YamlDotNet would silently rewrite those and break round-trips users depend on. The hand-rolled `CustomFileWriter` gives us that control. |
| Parameter schema | `src/Fallout.Build/Utilities/SchemaUtility.cs` (~393 LOC) | Emits the draft-04 JSON Schema envelope (definitions block + `allOf[user, base]`) that the NUKE ecosystem has emitted since day one. The exact output shape is a contract consumed by `Fallout.Cli` for editor autocomplete/validation of `parameters.json`. A schema library (NJsonSchema, Newtonsoft) would change the shape. Already hand-rolled on `System.Text.Json` nodes — no extra dependency. |

## When does a keep get revisited?

A keep is not permanent. Re-open the question only when the reason no longer holds — e.g. a CI target
drops its Kotlin DSL, the schema contract is versioned so the shape can change, or a vetted crypto/path
library covers the exact surface with no behavioral drift. In that case, raise it on the issue tracker
referencing this page rather than silently swapping in a dependency.
