using System;
using System.Linq;
using Fallout.Common.Utilities;

namespace Fallout.Common.CI.GitHubActions.Configuration;

/// <summary>
/// Resolves a user-supplied action override against the version the generator pins by default.
/// </summary>
internal static class GitHubActionsActionReference
{
    /// <summary>
    /// Resolves a user-supplied action override into the <c>uses:</c> value to emit. The consumer-facing
    /// contract — the accepted forms and what each means — is documented on the public properties that feed
    /// this, <see cref="GitHubActionsAttribute.CheckoutAction"/> and its siblings; this is the
    /// implementation behind them.
    /// <para/>
    /// A trailing <c># comment</c> is split off and set aside before classifying, so it never sways the
    /// decision. What remains is a complete reference when it contains an <c>@</c> (emitted verbatim),
    /// otherwise a bare ref appended to <paramref name="defaultAction"/>'s <c>owner/repo</c> — with a
    /// leading <c>@</c> required to mark a slash-bearing value as a ref rather than an <c>owner/repo</c>.
    /// Local (<c>./path</c>) and container (<c>docker://</c>) references pass through untouched.
    /// </summary>
    /// <param name="defaultAction">The <c>owner/repo@ref</c> the generator pins, used both as the fallback
    /// and as the action a bare ref is appended to.</param>
    /// <param name="value">The override to resolve; null or whitespace yields <paramref name="defaultAction"/>.</param>
    /// <param name="origin">Names what carries <paramref name="value"/> — a property and its workflow — so a
    /// rejected reference points at the declaration to fix. Prefixed onto every message.</param>
    /// <exception cref="ArgumentException">Thrown via <see cref="Assert"/> when <paramref name="value"/> is
    /// not a reference the generator can emit.</exception>
    public static string Resolve(string defaultAction, string value, string origin)
    {
        if (value.IsNullOrWhiteSpace())
        {
            return defaultAction;
        }

        Assert.True(value.All(x => !char.IsControl(x)),
            $"{origin}: action reference '{value}' must be a single line without control characters");

        var (reference, comment) = SplitComment(value.Trim());

        Assert.True(!reference.IsNullOrWhiteSpace(),
            $"{origin}: action reference '{value}' is only a comment and names no action");

        // The reference is emitted into an unquoted YAML scalar, where whitespace would let a second key
        // (': ') or a stray token through and corrupt the whole workflow file.
        Assert.True(reference.All(x => !char.IsWhiteSpace(x)),
            $"{origin}: action reference '{reference}' must not contain whitespace — expected 'owner/repo@ref' or a bare ref");

        if (!reference.StartsWith("./", StringComparison.Ordinal) &&
            !reference.StartsWith("docker://", StringComparison.Ordinal))
        {
            reference = ResolveMarketplaceReference(defaultAction, reference, origin);
        }

        return comment == null ? reference : $"{reference} {comment}";
    }

    /// <summary>
    /// Resolves a comment-free reference to an action on GitHub — the <c>owner/repo@ref</c> case and the
    /// bare-ref case — rejecting anything that names no ref or is ambiguous between the two. Local and
    /// container references never reach here; they carry no ref to resolve.
    /// </summary>
    private static string ResolveMarketplaceReference(string defaultAction, string reference, string origin)
    {
        // A leading '@' marks a bare ref explicitly — the only way to say that a slash-bearing value like
        // 'releases/v1' is a branch rather than an owner/repo.
        var isExplicitBareRef = reference.StartsWith("@", StringComparison.Ordinal);
        if (isExplicitBareRef)
        {
            reference = reference.Substring(startIndex: 1);
        }

        var separatorIndex = reference.IndexOf('@');
        if (!isExplicitBareRef && separatorIndex >= 0)
        {
            var name = reference.Substring(startIndex: 0, separatorIndex);
            var gitReference = reference.Substring(separatorIndex + 1);

            Assert.True(name.Contains('/'),
                $"{origin}: action reference '{reference}' must name the action as 'owner/repo@ref'");

            Assert.True(!gitReference.IsNullOrWhiteSpace() && !gitReference.Contains('@'),
                $"{origin}: action reference '{reference}' must name exactly one version or commit after the '@'");

            return reference;
        }

        Assert.True(!reference.IsNullOrWhiteSpace() && !reference.Contains('@'),
            $"{origin}: action reference '{reference}' must name exactly one version or commit");

        Assert.True(isExplicitBareRef || !reference.Contains('/'),
            $"{origin}: action reference '{reference}' is ambiguous — write 'owner/repo@ref' for a complete " +
            $"reference, or '@{reference}' to use it as a ref on '{GetActionName(defaultAction)}'");

        return $"{GetActionName(defaultAction)}@{reference}";
    }

    /// <summary>
    /// Takes the <c>owner/repo</c> half of a pinned default, which is what a bare ref gets appended to.
    /// </summary>
    private static string GetActionName(string defaultAction)
    {
        var separatorIndex = defaultAction.IndexOf('@');
        return separatorIndex > 0 ? defaultAction.Substring(startIndex: 0, separatorIndex) : defaultAction;
    }

    /// <summary>
    /// Splits a trailing YAML comment off the reference, so neither classification nor validation can be
    /// swayed by a <c>/</c> or <c>@</c> that only appears inside it. A <c>#</c> opens a comment only at the
    /// start of the scalar or after whitespace; anywhere else it is an ordinary character and stays part of
    /// the reference.
    /// </summary>
    private static (string Reference, string Comment) SplitComment(string value)
    {
        if (value.StartsWith("#", StringComparison.Ordinal))
        {
            return (string.Empty, value);
        }

        var commentIndex = value.IndexOf(" #", StringComparison.Ordinal);
        return commentIndex < 0
            ? (value, null)
            : (value.Substring(startIndex: 0, commentIndex).TrimEnd(), value.Substring(commentIndex + 1));
    }
}
