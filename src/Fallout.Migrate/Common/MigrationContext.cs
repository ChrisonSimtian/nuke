using System;
using System.IO;
using Fallout.Common.IO;

namespace Fallout.Migrate.Common;

/// <summary>
/// Plain data carried between <see cref="Migration"/> and each <see cref="IMigrationStep"/>.
/// Holds no behavior itself; see <see cref="MigrationFileOperations"/> for the shared
/// file-walking / rewrite-application helpers steps call into.
/// </summary>
internal sealed class MigrationContext(
    AbsolutePath rootDirectory,
    bool dryRun,
    TextWriter log,
    bool switchGlobalTool = false)
{
    /// <summary>The repository root being migrated.</summary>
    public AbsolutePath RootDirectory { get; } = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));

    /// <summary>When <c>true</c>, steps must report intended changes without writing them.</summary>
    public bool DryRun { get; } = dryRun;

    /// <summary>
    /// When <c>true</c>, <see cref="Fallout.Migrate.Steps.SwitchGlobalToolStep"/> may change which
    /// tools are installed machine-wide. Off unless the user passed <c>--switch-global-tool</c>:
    /// migrating a repository must not install or uninstall software by surprise, and the specs run
    /// the real pipeline with <c>dryRun: false</c>.
    /// </summary>
    public bool SwitchGlobalTool { get; } = switchGlobalTool;

    /// <summary>The writer steps use to report progress.</summary>
    public TextWriter Log { get; } = log ?? throw new ArgumentNullException(nameof(log));

    /// <summary>
    /// The Fallout version to pin in rewritten package references.
    /// Set by <see cref="Fallout.Migrate.Steps.ResolveFalloutVersionStep"/>, which always runs first;
    /// subsequent steps read it.
    /// </summary>
    public string FalloutVersion { get; internal set; }

    /// <summary>
    /// The version to pin for the <c>fallout.globaltool</c> dotnet tool in a rewritten
    /// <c>dotnet-tools.json</c>, or <c>null</c> when it could not be resolved. Tracked separately from
    /// <see cref="FalloutVersion"/> because the tool ships under its own package id, which was
    /// introduced later and so has a different set of published versions (#575). Set by
    /// <see cref="Fallout.Migrate.Steps.ResolveFalloutVersionStep"/>; read by
    /// <see cref="Fallout.Migrate.Steps.RewriteToolManifestStep"/>.
    /// </summary>
    public string ToolVersion { get; internal set; }
}
