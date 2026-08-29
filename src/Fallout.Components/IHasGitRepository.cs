using Fallout.Common;
using Fallout.Common.Git;

namespace Fallout.Components;

public interface IHasGitRepository : IFalloutBuild
{
    [GitRepository]
    [Required]
    GitRepository GitRepository => TryGetValue(() => GitRepository);
}
