using System;
using System.Collections.Generic;
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
    // /docs/getting-started/installation.
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

    // Each section directory carries a Docusaurus _category_.json whose "label" is what the site's
    // sidebar shows, so that is the authoritative section name. It matters: title-casing the slug
    // instead would give "Cicd" and "Ide" where the site says "CI/CD Support" and "IDE Support",
    // and "Common" where it says "Common Tasks". The slug is only a fallback for a directory that
    // has no _category_.json.
    string ToSectionTitle(string directorySlug)
    {
        var category = DocsWebsiteDirectory / directorySlug / "_category_.json";
        if (category.FileExists())
        {
            var label = category.ReadJsonObject().GetPropertyValue<string>("label");
            if (!label.IsNullOrWhiteSpace())
                return label;
        }

        return StripOrderPrefix(directorySlug)
            .Split('-')
            .Select(x => char.ToUpperInvariant(x[0]) + x[1..])
            .JoinSpace();
    }

    sealed record DocPage(string Title, string Description, string Url, string Section, int SectionOrder, int Order);

    const int MaxDescriptionLength = 200;

    // Inline markdown links render as "[text](url)". Only the text belongs in a one-line summary.
    static readonly Regex InlineLink = new(@"\[(?<text>[^\]]+)\]\([^)]+\)", RegexOptions.Compiled);

    static readonly Regex FrontmatterEntry = new(@"^(?<key>[a-zA-Z]+):\s*(?<value>.*)$", RegexOptions.Compiled);

    DocPage ReadPage(AbsolutePath file)
    {
        var lines = file.ReadAllLines();
        var frontmatterEnd = GetFrontmatterEnd(lines);
        var frontmatter = ReadFrontmatter(lines, frontmatterEnd);

        // Docusaurus falls back to the first H1 when a page declares no 'title', and docs/website
        // has a page that relies on it: badge.md carries no frontmatter at all and is served as
        // "Badge". Rejecting it would refuse a page the site renders correctly, so the fallback
        // matches Docusaurus. A page with neither still fails, because that leaves no link text.
        var title = frontmatter.GetValueOrDefault("title") ?? GetFirstHeading(lines);
        Assert.NotNullOrWhiteSpace(
            title,
            $"{DocsWebsiteDirectory.GetUnixRelativePathTo(file)} has neither a 'title' in its "
            + "frontmatter nor a top-level heading. One of the two is needed: it is the link text "
            + "in docs/llms.txt.");

        var description = frontmatter.GetValueOrDefault("description")
                          ?? GetFirstProseParagraph(lines, frontmatterEnd);

        var relative = DocsWebsiteDirectory.GetUnixRelativePathTo(file).ToString();
        var segments = relative.Split('/');
        var isNested = segments.Length > 1;

        return new DocPage(
            Title: title,
            Description: Summarize(description),
            Url: ToPublicUrl(file),
            // Root-level pages have no section. Task 3 collects them under "Optional", which is the
            // part of the llmstxt.org format meant for lower-priority links.
            Section: isNested ? ToSectionTitle(segments[0]) : null,
            SectionOrder: isNested ? GetOrder(segments[0]) : int.MaxValue,
            Order: GetOrder(segments[^1]));
    }

    static string GetFirstHeading(string[] lines)
    {
        return lines
            .Select(x => x.Trim())
            .FirstOrDefault(x => x.StartsWith("# "))
            ?[2..].Trim();
    }

    static int GetFrontmatterEnd(string[] lines)
    {
        if (lines.Length == 0 || lines[0].Trim() != "---")
            return 0;

        var end = Array.FindIndex(lines, startIndex: 1, x => x.Trim() == "---");
        return end < 0 ? 0 : end + 1;
    }

    static Dictionary<string, string> ReadFrontmatter(string[] lines, int frontmatterEnd)
    {
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < Math.Max(frontmatterEnd - 1, 1); i++)
        {
            var match = FrontmatterEntry.Match(lines[i]);
            if (!match.Success)
                continue;

            var value = match.Groups["value"].Value.Trim().TrimMatchingDoubleQuotes().Trim('\'');
            if (!value.IsNullOrWhiteSpace())
                entries[match.Groups["key"].Value] = value;
        }

        return entries;
    }

    // Only introduction.md declares a 'description', so for the other 36 pages the summary falls
    // back to the page's own opening paragraph. Several open with a Docusaurus import or an MDX
    // component instead, and those are not prose, so they are skipped along with headings,
    // admonitions, tables, images and code fences.
    //
    // The whole paragraph is collected, not just its first line: docs/website hard-wraps prose, so
    // stopping at the first newline would cut a sentence mid-way ("... help other" on badge.md).
    static string GetFirstProseParagraph(string[] lines, int frontmatterEnd)
    {
        var paragraph = new List<string>();
        var insideFence = false;

        foreach (var line in lines.Skip(frontmatterEnd))
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("```"))
            {
                insideFence = !insideFence;
                continue;
            }

            var isProse = !insideFence &&
                          !trimmed.IsNullOrWhiteSpace() &&
                          !trimmed.StartsWith("import ") &&
                          !trimmed.StartsWith('<') &&
                          !trimmed.StartsWith(":::") &&
                          !trimmed.StartsWith('#') &&
                          !trimmed.StartsWith('|') &&
                          !trimmed.StartsWith('!');

            if (isProse)
                paragraph.Add(trimmed);
            else if (paragraph.Count > 0)
                break;
        }

        return paragraph.Count > 0 ? paragraph.JoinSpace() : null;
    }

    static string Summarize(string text)
    {
        if (text.IsNullOrWhiteSpace())
            return null;

        var flattened = InlineLink.Replace(text, "${text}").Trim();
        if (flattened.Length <= MaxDescriptionLength)
            return flattened;

        // Cut on a word boundary so the summary never ends mid-word.
        var cut = flattened.LastIndexOf(' ', MaxDescriptionLength);
        return flattened[..(cut > 0 ? cut : MaxDescriptionLength)].TrimEnd(',', ';', ':', '.') + "...";
    }

    IReadOnlyList<DocPage> ReadDocPages()
    {
        return DocsWebsiteDirectory.GlobFiles("**/*.md")
            .Select(ReadPage)
            .OrderBy(x => x.SectionOrder)
            .ThenBy(x => x.Order)
            .ThenBy(x => x.Title)
            .ToList();
    }

    Target GenerateLlmsTxt => _ => _
        .Executes(() =>
        {
            ReadDocPages().ForEach(x => Log.Information(
                "[{Section}] {Title} -> {Url} :: {Description}",
                x.Section ?? "(root)",
                x.Title,
                x.Url,
                x.Description ?? "(none)"));
        });
}
