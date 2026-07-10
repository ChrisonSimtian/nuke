namespace Fallout.Common.CI.GitHubActions;

/// <summary>
/// Named insertion points for custom steps, anchored to the always-present checkout and run block so
/// they stay well-defined when the optional cache / artifact steps are absent.
/// </summary>
public enum GitHubActionsStepPosition
{
    /// <summary>After checkout, before the cache step (if any).</summary>
    PostCheckout,

    /// <summary>After the cache step (if any), before the setup-dotnet / restore / <c>dotnet fallout</c> block.</summary>
    PreRun,

    /// <summary>After the run block, before the built-in artifact upload (if any).</summary>
    PostRun,

    /// <summary>After the built-in artifact upload — the end of the job.</summary>
    JobEnd,
}
