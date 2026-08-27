using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    /// <param name="IsPrimary">
    /// Whether the site places this page in its sidebar. Only meaningful for the pages at the root
    /// of docs/website, which have no section to sort them: introduction.md declares a
    /// 'sidebar_position' and badge.md does not, and that is exactly the split between a link that
    /// belongs in the main body and one that belongs under "Optional".
    /// </param>
    sealed record DocPage(
        string Title,
        string Description,
        string Url,
        string Section,
        int SectionOrder,
        int Order,
        bool IsPrimary);

    const int MaxDescriptionLength = 200;

    // Inline markdown links render as "[text](url)". Only the text belongs in a one-line summary.
    static readonly Regex InlineLink = new(@"\[(?<text>[^\]]+)\]\([^)]+\)", RegexOptions.Compiled);

    static readonly Regex FrontmatterEntry = new(@"^(?<key>[a-zA-Z_]+):\s*(?<value>.*)$", RegexOptions.Compiled);

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
            // Root-level pages have no section, so they are rendered either above the first one or
            // under "Optional", depending on IsPrimary.
            Section: isNested ? ToSectionTitle(segments[0]) : null,
            SectionOrder: isNested ? GetOrder(segments[0]) : int.MaxValue,
            Order: GetOrder(segments[^1]),
            IsPrimary: isNested || frontmatter.ContainsKey("sidebar_position"));
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

    AbsolutePath LlmsTxtFile => RootDirectory / "docs" / "llms.txt";

    // https://llmstxt.org: an H1, an optional blockquote summary, then H2 sections of link lines.
    // A list may also sit between the blockquote and the first H2, which is where the pages that
    // live at the root of docs/website go when the site gives them a sidebar position.
    string RenderLlmsTxt(IReadOnlyList<DocPage> pages)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Fallout");
        builder.AppendLine();

        // Single source for the summary: introduction.md's own 'description', which is what the
        // site serves as its meta description. Generating it from anywhere else would let the two
        // drift apart.
        var summary = pages.Single(x => x.Url == DocsBaseUrl + "introduction").Description;
        builder.AppendLine($"> {summary}");
        builder.AppendLine();
        builder.AppendLine(
            "Generated from the documentation sources by './build.ps1 GenerateLlmsTxt'. Do not edit by hand.");
        builder.AppendLine();

        foreach (var page in pages.Where(x => x.Section == null && x.IsPrimary))
            builder.AppendLine(RenderEntry(page));

        foreach (var section in pages.Where(x => x.Section != null).GroupBy(x => x.Section))
        {
            builder.AppendLine();
            builder.AppendLine($"## {section.Key}");
            builder.AppendLine();
            section.ForEach(x => builder.AppendLine(RenderEntry(x)));
        }

        // llms.txt reserves "Optional" for links a consumer may skip when it needs a shorter
        // context. Root pages the site does not place in the sidebar belong there.
        var optional = pages.Where(x => x.Section == null && !x.IsPrimary).ToList();
        if (optional.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Optional");
            builder.AppendLine();
            optional.ForEach(x => builder.AppendLine(RenderEntry(x)));
        }

        return builder.ToString();
    }

    static string RenderEntry(DocPage page)
    {
        return page.Description.IsNullOrWhiteSpace()
            ? $"- [{page.Title}]({page.Url})"
            : $"- [{page.Title}]({page.Url}): {page.Description}";
    }

    Target GenerateLlmsTxt => _ => _
        .Executes(() =>
        {
            var pages = ReadDocPages();
            LlmsTxtFile.WriteAllText(RenderLlmsTxt(pages));

            Log.Information("Wrote {File} with {Count} pages", RootDirectory.GetUnixRelativePathTo(LlmsTxtFile), pages.Count);
        });
}
