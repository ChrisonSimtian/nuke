using Fallout.Common;
using Fallout.Common.Tools.NerdbankGitVersioning;

namespace Fallout.Components;

public interface IHasNerdbankGitVersioning : IFalloutBuild
{
    [NerdbankGitVersioning]
    [Required]
    NerdbankGitVersioning Versioning => TryGetValue(() => Versioning);
}
