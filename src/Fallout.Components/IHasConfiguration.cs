using Fallout.Common;

namespace Fallout.Components;

public interface IHasConfiguration : IFalloutBuild
{
    [Parameter]
    Configuration Configuration => TryGetValue(() => Configuration) ??
                                   (IsLocalBuild ? Configuration.Debug : Configuration.Release);
}
