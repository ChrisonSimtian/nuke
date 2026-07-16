using System.Collections.Generic;

namespace Fallout.Migrate.Common;

/// <summary>
/// Aggregate result of a migration run: how many files changed, how many edits were made, how many
/// directories were renamed, and any warnings that need manual follow-up.
/// </summary>
internal sealed class Summary
{
    /// <summary>The number of files that had at least one edit applied.</summary>
    public int FilesChanged { get; set; }

    /// <summary>The total number of individual edits made across all rewritten files.</summary>
    public int EditCount { get; set; }

    /// <summary>The number of directories renamed (currently just <c>.nuke/</c> → <c>.fallout/</c>).</summary>
    public int DirectoriesRenamed { get; set; }

    /// <summary>Human-readable warnings about conditions the migration could not resolve automatically.</summary>
    public List<string> Warnings { get; } = new();
}
