using Fallout.Common.Tooling;

namespace Fallout.Common.CI.GitHubActions;

public enum GitHubActionsPermissions
{
    [EnumValue("actions")]
    Actions,

    [EnumValue("checks")]
    Checks,

    [EnumValue("contents")]
    Contents,

    [EnumValue("deployments")]
    Deployments,

    [EnumValue("id-token")]
    IdToken,

    [EnumValue("issues")]
    Issues,

    [EnumValue("discussions")]
    Discussions,

    [EnumValue("packages")]
    Packages,

    [EnumValue("pages")]
    Pages,

    [EnumValue("pull-requests")]
    PullRequests,

    [EnumValue("repository-projects")]
    RepositoryProjects,

    [EnumValue("security-events")]
    SecurityEvents,

    [EnumValue("statuses")]
    Statuses,
}
