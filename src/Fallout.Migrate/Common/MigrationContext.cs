using System;
using System.IO;
using Fallout.Common.IO;

namespace Fallout.Migrate.Common;

/// <summary>
/// Plain data carried between <see cref="Migration"/> and each <see cref="IMigrationStep"/>.
/// Holds no behavior itself; see <see cref="MigrationFileOperations"/> for the shared
/// file-walking / rewrite-application helpers steps call into.
/// </summary>
internal sealed class MigrationContext(AbsolutePath rootDirectory, bool dryRun, TextWriter log)
{
    /// <summary>The repository root being migrated.</summary>
    public AbsolutePath RootDirectory { get; } = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));

    /// <summary>When <c>true</c>, steps must report intended changes without writing them.</summary>
    public bool DryRun { get; } = dryRun;

    /// <summary>The writer steps use to report progress.</summary>
    public TextWriter Log { get; } = log ?? throw new ArgumentNullException(nameof(log));

    /// <summary>
    /// The Fallout version to pin in rewritten package references.
    /// Set by <see cref="Fallout.Migrate.Steps.ResolveFalloutVersionStep"/>, which always runs first;
    /// subsequent steps read it.
    /// </summary>
    public string FalloutVersion { get; internal set; }
}
