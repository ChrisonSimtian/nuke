using System;
using System.Linq;

namespace Fallout.Common.CI.GitHubActions;

/// <summary>
/// The marketplace actions the workflow generator emits, and the versions it pins them to — the single
/// place to bump one. Override any of them per workflow via
/// <see cref="GitHubActionsAttribute.CheckoutAction"/>, <see cref="GitHubActionsAttribute.CacheAction"/>,
/// <see cref="GitHubActionsAttribute.SetupDotNetAction"/>, and
/// <see cref="GitHubActionsAttribute.UploadArtifactAction"/>, so a newer action release never has to wait
/// on a Fallout release.
/// <para/>
/// Deliberately internal: those properties take a plain string, so nothing a consumer writes needs to name
/// this type. Keeping it out of the public surface also keeps it out of the <c>Nuke.*</c> transition shims,
/// which mirror public types but cannot carry <c>const</c> members.
/// </summary>
internal static class GitHubActionsDefaults
{
    public const string CheckoutAction = "actions/checkout@v7";
    public const string CacheAction = "actions/cache@v6";
    public const string SetupDotNetAction = "actions/setup-dotnet@v6";
    public const string UploadArtifactAction = "actions/upload-artifact@v7";
}
