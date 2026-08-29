using Fallout.Common;
using Fallout.Common.IO;

namespace Fallout.Components;

public interface IHasArtifacts : IFalloutBuild
{
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
}
