using Fallout.Common;
using Fallout.Solutions;

namespace Fallout.Components;

public interface IHasSolution : IFalloutBuild
{
    [Solution]
    [Required]
    Solution Solution => TryGetValue(() => Solution);
}
