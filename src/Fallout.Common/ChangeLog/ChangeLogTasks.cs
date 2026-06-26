using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NuGet.Versioning;
using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Common.Tools.GitHub;
using Fallout.Common.Utilities;
using Serilog;

// ReSharper disable ArgumentsStyleLiteral
namespace Fallout.Common.ChangeLog;

/// <summary>
/// Provides a set of tasks and utility methods for working with changelogs in a software repository.
/// This class includes methods for reading, extracting, and finalizing changelog entries, as well as
/// preparing release notes for publishing on platforms such as NuGet.
/// </summary>
public static class ChangelogTasks
{
    public static string GetNuGetReleaseNotes(string changelogFile, GitRepository repository = null)
    {
        // URL-encode characters MSBuild treats as command-line/property metacharacters.
        // Without this, MSBuild's property parser splits on ; (turning a long release note
        // into multiple bogus arguments) and chokes on stray " in the value.
        var changelogSectionNotes = ExtractChangelogSectionNotes(changelogFile)
            .Select(x => x.Replace("- ", "\u2022 ")
                .Replace("* ", "\u2022 ")
                .Replace("+ ", "\u2022 ")
                .Replace("`", string.Empty)
                .Replace(",", "%2C")
                .Replace(";", "%3B")).ToList();

        if (repository.IsGitHubRepository())
        {
            changelogSectionNotes.Add(string.Empty);
            changelogSectionNotes.Add($"Full changelog at {repository.GetGitHubBrowseUrl(changelogFile)}");
        }

        return changelogSectionNotes.JoinNewLine();
    }

    /// <summary>
    /// Reads the release notes from the given changelog file and returns the result.
    /// </summary>
    /// <param name="changelogFile">The path to the changelog file.</param>
    /// <returns>A readonly list of the release sections contained in the changelog.</returns>
    public static IReadOnlyList<ReleaseNotes> ReadReleaseNotes(AbsolutePath changelogFile)
    {
        var lines = changelogFile.ReadAllLines().ToList();
        var releaseSections = GetReleaseSections(lines).ToList();

        Assert.True(releaseSections.Any(), "Changelog should have at least one release note section");
        return releaseSections.Select(Parse).ToList().AsReadOnly();

        ReleaseNotes Parse(ReleaseSection section)
        {
            var releaseNotes = lines
                .Skip(section.StartIndex + 1)
                .Take(section.EndIndex - section.StartIndex)
                .ToList()
                .AsReadOnly();

            return NuGetVersion.TryParse(section.Caption, out var version)
                ? new ReleaseNotes(version, releaseNotes, section.StartIndex, section.EndIndex)
                : new ReleaseNotes(releaseNotes, section.StartIndex, section.EndIndex);
        }
    }

    /// <summary>
    /// Reads the specified changelog.
    /// </summary>
    /// <param name="changelogFile">The path to the changelog file.</param>
    /// <returns>A <see cref="ChangeLog"/> object to work with the changelog.</returns>
    public static ChangeLog ReadChangelog(string changelogFile)
    {
        var releaseNotes = ReadReleaseNotes(changelogFile);
        var unreleased = releaseNotes.Where(x => x.Unreleased).ToArray();

        if (unreleased.Length > 0)
        {
            Assert.True(unreleased.Length == 1, "Changelog should have only one draft section");
            return new ChangeLog(changelogFile, unreleased.First(), releaseNotes);
        }

        Assert.True(releaseNotes.Any(x => !x.Unreleased), "Changelog should have at lease one released version section");
        return new ChangeLog(changelogFile, releaseNotes);
    }

    /// <summary>
    /// Finalizes the changelog by moving all entries from `[vNext]` to the version specified by release.
    /// <p>If <paramref name="repository"/> is specified a summary with all versions and links to list the changes directly on GitHub is appended to the end of the changelog.</p>
    /// </summary>
    /// <param name="changelogFile">The path to the changelog file.</param>
    /// <param name="tag">The <see cref="NuGetVersion"/> to finalize the changelog.</param>
    /// <param name="repository">The repository to create the version overview for.</param>
    /// <seealso cref="FinalizeChangelog(ChangeLog,NuGetVersion,GitRepository)"/>
    public static void FinalizeChangelog(ChangeLog changelogFile, NuGetVersion tag, GitRepository repository = null)
    {
        Log.Information("Finalizing {File} for {Tag} ...", PathConstruction.GetRelativePath(FalloutBuild.RootDirectory, changelogFile.Path), tag);

        var unreleasedNotes = changelogFile.Unreleased;
        var releaseNotes = changelogFile.ReleaseNotes;
        var lastReleased = changelogFile.LatestVersion;

        Assert.True(unreleasedNotes != null, "Changelog should have draft section");
        Assert.True(releaseNotes.Any(x => x.Version != null && x.Version.Equals(tag)), $"Tag '{tag}' already exists");
        Assert.True(lastReleased != null && tag.CompareTo(lastReleased.Version) > 0,
            $"Tag '{tag}' is not greater compared to last tag '{lastReleased.NotNull().Version}'");

        var content = changelogFile.Path.ReadAllLines().ToList();

        content.Insert(unreleasedNotes.StartIndex + 1, string.Empty);
        content.Insert(unreleasedNotes.EndIndex + 2, $"## [{tag}] / {DateTime.Now:yyyy-MM-dd}");

        UpdateVersionSummary(tag.ToString(), content, repository);

        content.Add(string.Empty);

        changelogFile.Path.WriteAllLines(content);
    }

    /// <summary>
    /// Finalizes the changelog by moving all entries from `[vNext]` to the version specified by release.
    /// <p>If <paramref name="repository"/> is specified a summary with all versions and links to list the changes directly on GitHub is appended to the end of the changelog.</p>
    /// </summary>
    /// <param name="changelogFile">The path to the changelog file.</param>
    /// <param name="tag">The version to finalize the changelog.</param>
    /// <param name="repository">The repository to create the version overview for.</param>
    /// <seealso cref="FinalizeChangelog(ChangeLog,NuGetVersion,GitRepository)"/>
    public static void FinalizeChangelog(AbsolutePath changelogFile, string tag, GitRepository repository = null)
    {
        Log.Information("Finalizing {File} for {Tag} ...", PathConstruction.GetRelativePath(FalloutBuild.RootDirectory, changelogFile), tag);

        var content = changelogFile.ReadAllLines().ToList();
        var sections = GetReleaseSections(content).ToList();
        var firstSection = sections.First();
        var secondSection = sections.Skip(1).FirstOrDefault();

        Assert.True(firstSection.Caption.All(char.IsLetter), "Cannot find a draft section");
        Assert.True(sections.All(x => !x.Caption.EqualsOrdinalIgnoreCase(tag)), $"Tag '{tag}' already exists");
        Assert.True(firstSection.EndIndex > firstSection.StartIndex, $"Draft section '{firstSection.Caption}' does not contain any information");
        Assert.True(secondSection == null || NuGetVersion.Parse(tag).CompareTo(NuGetVersion.Parse(secondSection.Caption)) > 0,
            $"Tag '{tag}' is not greater compared to last tag '{secondSection?.Caption}'");

        content.Insert(firstSection.StartIndex + 1, string.Empty);
        content.Insert(firstSection.StartIndex + 2, $"## [{tag}] / {DateTime.Now:yyyy-MM-dd}");

        UpdateVersionSummary(tag, content, repository);

        content.Add(string.Empty);

        changelogFile.WriteAllLines(content);
    }

    /// <summary>
    /// Extracts the notes from a specific section of the given changelog file, optionally filtered by a section tag.
    /// </summary>
    /// <param name="changelogFile">The path to the changelog file.</param>
    /// <param name="tag">The optional tag to identify the specific section to extract notes from. If null, the first valid section is used.</param>
    /// <returns>An enumerable collection of strings representing the notes in the specified section. If the file does not exist or the section is not found, an empty collection is returned.</returns>
    public static IEnumerable<string> ExtractChangelogSectionNotes(AbsolutePath changelogFile, string tag = null)
    {
        if (!changelogFile.FileExists())
        {
            // We treat a non-existing changelog file as empty.
            Log.Information("Changelog file {File} does not exist, so skipping section extraction", changelogFile);
            return Array.Empty<string>();
        }

        List<string> content = changelogFile.ReadAllLines().Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        List<ReleaseSection> sections = GetReleaseSections(content).ToList();
        if (!sections.Any())
        {
            // We treat an empty changelog as if the section does not exist.
            Log.Information("Changelog file {File} doesn't contain any sections, so skipping section extraction", changelogFile);

            return Array.Empty<string>();
        }

        ReleaseSection section;
        if (tag == null)
        {
            section = sections.FirstOrDefault(x => x.StartIndex < x.EndIndex);
            if (section == null)
            {
                return Array.Empty<string>();
            }
        }
        else
        {
            section = sections.FirstOrDefault(x => x.Caption.EqualsOrdinalIgnoreCase(tag));
            if (section == null)
            {
                throw new Exception($"Could not find release section for '{tag}'.");
            }
        }

        return content
            .Skip(section.StartIndex + 1)
            .Take(section.EndIndex - section.StartIndex);
    }

    internal static IEnumerable<ReleaseSection> GetReleaseSections(List<string> content)
    {
        static bool IsReleaseHead(string str)
            => str.StartsWith("## ");

        static bool IsReleaseContent(string str)
            => str.StartsWith("###")
               || str.Trim().StartsWith("-")
               || str.Trim().StartsWith("*")
               || str.Trim().StartsWith("+");

        static string GetCaption(string str)
            => str
                .TrimStart('#', ' ', '[')
                .Split(' ')
                .First()
                .TrimEnd(']');

        var index = content.FindIndex(IsReleaseHead);
        if (index < 0)
            yield break;

        while (index < content.Count)
        {
            var line = content[index];
            if (!IsReleaseHead(line))
            {
                index++;
                continue;
            }

            var caption = GetCaption(line);
            var nextReleaseHeadIndex = content.FindIndex(index + 1,  x => IsReleaseHead(x) || !IsReleaseContent(x));

            var releaseData =
                new ReleaseSection
                {
                    Caption = caption,
                    StartIndex = index,
                    EndIndex = nextReleaseHeadIndex >= 0
                        ? nextReleaseHeadIndex - 1
                        : content.Count - 1
                };

            yield return releaseData;
            Log.Verbose("Found section '{Caption}' [{Start}-{End}]", caption, index, releaseData.EndIndex);

            index = releaseData.EndIndex + 1;
        }
    }

    private static void UpdateVersionSummary(string tag, List<string> content, GitRepository repository)
    {
        if (repository != null && repository.IsGitHubRepository())
        {
            var sections = GetReleaseSections(content).ToList();
            var firstSection = sections.First();
            var lastSection = sections.Last();

            content.RemoveRange(lastSection.EndIndex + 1, content.Count - lastSection.EndIndex - 1);

            content.Add(string.Empty);
            content.Add($"[{firstSection.Caption}]: {repository.GetGitHubCompareTagToHeadUrl(tag)}");
            for (var i = 1; i + 1 < sections.Count; i++)
                content.Add($"[{sections[i].Caption}]: {repository.GetGitHubCompareTagsUrl(sections[i].Caption, sections[i + 1].Caption)}");
            content.Add($"[{lastSection.Caption}]: {repository.GetGitHubBrowseUrl(branch: lastSection.Caption)}");
        }
    }

    [DebuggerDisplay("{" + nameof(Caption) + "} [{" + nameof(StartIndex) + "}-{" + nameof(EndIndex) + "}]")]
    internal class ReleaseSection
    {
        public string Caption;
        public int StartIndex;
        public int EndIndex;
    }
}
