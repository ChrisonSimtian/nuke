namespace Fallout.Common.CI.TeamCity.Configuration;

public enum TeamCityDependencyFailureAction
{
    // TODO: add description from web UI
    AddProblem,
    FailToStart,
    Ignore,
    Cancel
}
