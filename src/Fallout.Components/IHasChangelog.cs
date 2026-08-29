using Fallout.Common;
using static Fallout.Common.ChangeLog.ChangelogTasks;

namespace Fallout.Components;

public interface IHasChangelog : IFalloutBuild
{
    // TODO: assert file exists
    string ChangelogFile => RootDirectory / "CHANGELOG.md";

    string NuGetReleaseNotes => GetNuGetReleaseNotes(ChangelogFile, (this as IHasGitRepository)?.GitRepository);
}
