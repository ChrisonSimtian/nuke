using System.Diagnostics.CodeAnalysis;

namespace Fallout.Common.CI.TeamCity;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum TeamCityStatus
{
    NORMAL,
    WARNING,
    ERROR,
    FAILURE
}
