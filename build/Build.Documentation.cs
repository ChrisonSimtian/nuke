using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using Fallout.Utilities.Text.Yaml;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static Fallout.Common.Tools.Git.GitTasks;

partial class Build
{
    AbsolutePath DocsWebsiteDirectory => RootDirectory / "docs" / "website";

    AbsolutePath LlmsTxtFile => RootDirectory / "docs" / "llms.txt";

    // The public site is built from docs/website by the separate Fallout-build/docs.fallout.build
    // Docusaurus repository, which serves the pages under a /docs/ route prefix. Verified against
    // https://docs.fallout.build/sitemap.xml, not against the README: the README's own links omit
    // the prefix and 404 (see the follow-up on the PR). Hardcoded for the same reason as
    // CanonicalRepositoryIdentifier: the generated file must be identical whichever fork
    // regenerates it.
    const string DocsBaseUrl = "https://docs.fallout.build/docs/";

    const int MaxDescriptionLength = 200;

    // Docusaurus orders pages by a numeric prefix on the directory and file name, and strips that
    // prefix from the served URL. So 01-getting-started/01-installation.md is served at
    // /docs/getting-started/installation.
    static readonly Regex orderPrefix = new(@"^(?<order>\d+)-", RegexOptions.Compiled);

    // Inline markdown links render as "[text](url)". Only the text belongs in a one-line summary.
    static readonly Regex inlineLink = new(@"\[(?<text>[^\]]+)\]\([^)]+\)", RegexOptions.Compiled);

    // Deserialized rather than hand-parsed, so quoting, escaping and block scalars follow the YAML
    // spec instead of a line regex. Underscored, not the repo-default camelCase builder, because
    // Docusaurus spells its keys 'sidebar_position'.
    static readonly DeserializerBuilder frontmatterDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties();

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

    // Only the three keys this index reads. Docusaurus accepts many more (tags, slug, keywords,
    // hide_title...), so unmatched ones are ignored rather than failing a page that adds one.
    sealed record Frontmatter
    {
        public string Title { get; init; }

        public string Description { get; init; }

        public int? SidebarPosition { get; init; }
    }

    Target GenerateLlmsTxt => _ => _
        .Executes(() =>
        {
            var pages = ReadDocPages();
            LlmsTxtFile.WriteAllText(RenderLlmsTxt(pages));

            Log.Information("Wrote {File} with {Count} pages", RootDirectory.GetUnixRelativePathTo(LlmsTxtFile), pages.Count);
        });

    // CI gate, in the shape of VerifyGeneratedTools: GenerateLlmsTxt only runs when a contributor
    // remembers to invoke it, so a page added under docs/website without regenerating would merge
    // with docs/llms.txt silently missing it. `Requires` is asserted for the whole scheduled plan
    // before any target runs, so the "start clean" check below still fires before GenerateLlmsTxt
    // regenerates anything; the explicit re-check afterward catches drift with a message pointing
    // at the fix.
    //
    // Wired into BOTH workflows on purpose. build.yml ignores docs/**, so on its own it would never
    // fire on the change that actually invalidates the file; build-skip.yml is the workflow that
    // handles those PRs, and it runs this target for exactly that reason.
    Target VerifyLlmsTxt => _ => _
        .Requires(() => GitHasCleanWorkingCopy())
        .DependsOn(GenerateLlmsTxt)
        .Executes(() =>
        {
            Assert.True(
                GitHasCleanWorkingCopy(),
                "docs/llms.txt is out of sync with docs/website. Run './build.ps1 GenerateLlmsTxt' "
                + "locally and commit the result.");
        });

    // Docusaurus routes both .md and .mdx, and excludes anything whose file or directory name
    // starts with an underscore (**/_*.md, **/_*/**). docs/website/_snippets/ exists for exactly
    // that reason, so indexing it would emit URLs the site never serves.
    IReadOnlyList<DocPage> ReadDocPages()
    {
        return DocsWebsiteDirectory.GlobFiles("**/*.md", "**/*.mdx")
            .Where(x => !DocsWebsiteDirectory.GetUnixRelativePathTo(x).ToString()
                .Split('/')
                .Any(segment => segment.StartsWith('_')))
            .Select(ReadPage)
            .OrderBy(x => x.SectionOrder)
            .ThenBy(x => x.Order)
            // Ordinal, not the culture-sensitive default: docs/llms.txt is verified byte for byte,
            // so a contributor on another culture must not regenerate a differently ordered file
            // and trip VerifyLlmsTxt with no real drift.
            .ThenBy(x => x.Title, StringComparer.Ordinal)
            .ToList();
    }

    DocPage ReadPage(AbsolutePath file)
    {
        var lines = file.ReadAllLines();
        var frontmatterEnd = GetFrontmatterEnd(lines);
        var frontmatter = ReadFrontmatter(lines, frontmatterEnd);

        // Docusaurus falls back to the first H1 when a page declares no 'title', and docs/website
        // has a page that relies on it: badge.md carries no frontmatter at all and is served as
        // "Badge". Rejecting it would refuse a page the site renders correctly, so the fallback
        // matches Docusaurus. A page with neither still fails, because that leaves no link text.
        var title = frontmatter.Title.IsNullOrWhiteSpace()
            ? GetFirstHeading(lines, frontmatterEnd)
            : frontmatter.Title;
        Assert.NotNullOrWhiteSpace(
            title,
            $"{DocsWebsiteDirectory.GetUnixRelativePathTo(file)} has neither a 'title' in its "
            + "frontmatter nor a top-level heading. One of the two is needed: it is the link text "
            + "in docs/llms.txt.");

        var description = frontmatter.Description.IsNullOrWhiteSpace()
            ? GetFirstProseParagraph(lines, frontmatterEnd)
            : frontmatter.Description;

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
            // Docusaurus lets a page's own 'sidebar_position' override the numeric filename prefix,
            // and docs/website uses it: 07-ide has no prefixes, and rider.md declares position 1 to
            // sort first. Reading only the prefix would order that section by title instead.
            Order: frontmatter.SidebarPosition ?? GetOrder(segments[^1]),
            IsPrimary: isNested || frontmatter.SidebarPosition.HasValue);
    }

    string ToPublicUrl(AbsolutePath page)
    {
        var relative = DocsWebsiteDirectory.GetUnixRelativePathTo(page).ToString();
        var slug = relative[..^Path.GetExtension(relative).Length]
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
            // OrNull, not GetPropertyValue: that one throws when the property is absent, which
            // would make the fallback below unreachable. A _category_.json may legitimately carry
            // only "position" or "collapsed".
            var label = category.ReadJsonObject().GetPropertyValueOrNull<string>("label");
            if (!label.IsNullOrWhiteSpace())
                return label;
        }

        return StripOrderPrefix(directorySlug)
            .Split('-')
            .Where(x => x.Length > 0)
            .Select(x => char.ToUpperInvariant(x[0]) + x[1..])
            .JoinSpace();
    }

    static int GetFrontmatterEnd(string[] lines)
    {
        if (lines.Length == 0 || lines[0].Trim() != "---")
            return 0;

        var end = Array.FindIndex(lines, startIndex: 1, x => x.Trim() == "---");
        // An opened but unclosed block is malformed. Returning 0 would hand the delimiter and the
        // key/value lines to the prose reader and ship them as a description, so fail instead: a
        // wrong entry in a generated index is worse than a build that says what is wrong.
        Assert.True(end >= 0, "Frontmatter is opened with '---' but never closed.");
        return end + 1;
    }

    static Frontmatter ReadFrontmatter(string[] lines, int frontmatterEnd)
    {
        // frontmatterEnd is one past the closing '---', so the block itself is lines 1..end-2.
        // 0 means the page opens with no frontmatter at all; badge.md is one such page.
        if (frontmatterEnd == 0)
            return new Frontmatter();

        var yaml = lines[1..(frontmatterEnd - 1)].JoinNewLine();
        return yaml.GetYaml<Frontmatter>(frontmatterDeserializer) ?? new Frontmatter();
    }

    // Starts after the frontmatter and ignores fenced code, because "# terminal-command" is used as
    // a marker throughout this doc set and would otherwise become a page's link text.
    static string GetFirstHeading(string[] lines, int frontmatterEnd)
    {
        var insideFence = false;
        foreach (var line in lines.Skip(frontmatterEnd))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("```"))
                insideFence = !insideFence;
            else if (!insideFence && trimmed.StartsWith("# "))
                return trimmed[2..].Trim();
        }

        return null;
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
                          !trimmed.StartsWith('!') &&
                          // A page opening with a list or a quote has no lead paragraph to take.
                          // "- "/"* " and not '-'/'*', so a '---' rule or a '**bold**' lead-in
                          // is still read as the prose it is.
                          !trimmed.StartsWith("- ") &&
                          !trimmed.StartsWith("* ") &&
                          !trimmed.StartsWith('>');

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

        var flattened = inlineLink.Replace(text, "${text}").Trim();
        if (flattened.Length <= MaxDescriptionLength)
            return flattened;

        // Cut on a word boundary so the summary never ends mid-word.
        var cut = flattened.LastIndexOf(' ', MaxDescriptionLength);
        return flattened[..(cut > 0 ? cut : MaxDescriptionLength)].TrimEnd(',', ';', ':', '.') + "...";
    }

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
        var introduction = pages.SingleOrDefault(x => x.Url == DocsBaseUrl + "introduction");
        Assert.NotNull(
            introduction,
            "No page resolves to " + DocsBaseUrl + "introduction, which is where the llms.txt summary "
            + "comes from. If introduction.md was renamed, moved into a section or given a 'slug', "
            + "point this lookup at its new location.");

        var summary = introduction.Description;
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

    static string StripOrderPrefix(string segment) => orderPrefix.Replace(segment, string.Empty);

    static int GetOrder(string segment)
    {
        var match = orderPrefix.Match(segment);
        return match.Success ? int.Parse(match.Groups["order"].Value) : int.MaxValue;
    }
}
