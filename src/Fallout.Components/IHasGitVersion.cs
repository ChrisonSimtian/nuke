using Fallout.Common;
using Fallout.Common.Tools.GitVersion;

namespace Fallout.Components;

public interface IHasGitVersion : IFalloutBuild
{
    [GitVersion(NoFetch = true, Framework = "net8.0")]
    [Required]
    GitVersion Versioning => TryGetValue(() => Versioning);
}
