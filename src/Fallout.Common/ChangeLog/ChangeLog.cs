using System.Collections.Generic;
using System.Linq;
using Fallout.Common.IO;
using NuGet.Versioning;

namespace Fallout.Common.ChangeLog;

public class ChangeLog
{
    /// <summary>
    /// The path to the changelog file.
    /// </summary>
    public AbsolutePath Path { get; }

    /// <summary>
    /// The unreleased release notes section.
    /// </summary>
    public ReleaseNotes Unreleased { get; }

    /// <summary>
    /// Release notes sorted by version.
    /// </summary>
    public IReadOnlyList<ReleaseNotes> ReleaseNotes { get; }

    /// <summary>
    /// The latest release notes section. Returns null if the changelog does not contain a release section.
    /// </summary>
    public NuGetVersion LatestVersion => ReleaseNotes.FirstOrDefault()?.Version;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeLog"/> class.
    /// </summary>
    /// <param name="path">The path to the changelog file.</param>
    /// <param name="unreleased">The unreleased notes sectioon.</param>
    /// <param name="releaseNotes">The release notes of the changelog.</param>
    public ChangeLog(string path, ReleaseNotes unreleased, IReadOnlyList<ReleaseNotes> releaseNotes)
    {
        Path = path;
        Unreleased = unreleased;
        ReleaseNotes = releaseNotes.Where(x => !x.Unreleased).OrderBy(x => x.Version).ToList().AsReadOnly();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeLog"/> class.
    /// </summary>
    /// <param name="path">The path to the changelog file.</param>
    /// <param name="releaseNotes">The release notes of the changelog.</param>
    public ChangeLog(string path, IReadOnlyList<ReleaseNotes> releaseNotes)
        : this(path, unreleased: null, releaseNotes)
    {
    }
}
