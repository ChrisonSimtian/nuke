using System;
using System.Linq;
using System.Text.RegularExpressions;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using Serilog;

partial class Build
{
    AbsolutePath DocsWebsiteDirectory => RootDirectory / "docs" / "website";

    // The public site is built from docs/website by the separate Fallout-build/docs.fallout.build
    // Docusaurus repository, which serves the pages under a /docs/ route prefix. Verified against
    // https://docs.fallout.build/sitemap.xml, not against the README: the README's own links omit
    // the prefix and 404 (see the follow-up on the PR). Hardcoded for the same reason as
    // CanonicalRepositoryIdentifier: the generated file must be identical whichever fork
    // regenerates it.
    const string DocsBaseUrl = "https://docs.fallout.build/docs/";

    // Docusaurus orders pages by a numeric prefix on the directory and file name, and strips that
    // prefix from the served URL. So 01-getting-started/01-installation.md is served at
    // /getting-started/installation, which is the URL the README already links.
    static readonly Regex OrderPrefix = new(@"^(?<order>\d+)-", RegexOptions.Compiled);

    static string StripOrderPrefix(string segment) => OrderPrefix.Replace(segment, string.Empty);

    static int GetOrder(string segment)
    {
        var match = OrderPrefix.Match(segment);
        return match.Success ? int.Parse(match.Groups["order"].Value) : int.MaxValue;
    }

    string ToPublicUrl(AbsolutePath page)
    {
        var relative = DocsWebsiteDirectory.GetUnixRelativePathTo(page).ToString();
        var slug = relative[..^".md".Length]
            .Split('/')
            .Select(StripOrderPrefix)
            .JoinSlash();

        return DocsBaseUrl + slug;
    }

    // "01-getting-started" becomes "Getting Started". The directory slug is the only section name
    // available: the docs tree has no per-directory metadata file.
    static string ToSectionTitle(string directorySlug)
    {
        return StripOrderPrefix(directorySlug)
            .Split('-')
            .Select(x => char.ToUpperInvariant(x[0]) + x[1..])
            .JoinSpace();
    }

    Target GenerateLlmsTxt => _ => _
        .Executes(() =>
        {
            DocsWebsiteDirectory.GlobFiles("**/*.md")
                .OrderBy(x => x.ToString())
                .ForEach(x => Log.Information(
                    "{Relative} -> {Url}",
                    DocsWebsiteDirectory.GetUnixRelativePathTo(x),
                    ToPublicUrl(x)));
        });
}
