using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Fallout.Migrate.Common;
using Spectre.Console;

namespace Fallout.Migrate.Steps;

// Pinned into migrated `<PackageReference Include="Fallout.X" Version="..." />` lines.

// Prefers the latest published, non-prerelease Fallout.Common version from NuGet within the
// running tool's own major (calendar year), so a migration always pins to what's actually
// installable without jumping to a newer, potentially breaking yearly major the tool wasn't
// built against. Falls back to the running migrate tool's own SemVer (Nerdbank.GitVersioning,
// set on AssemblyInformationalVersion) when NuGet can't be reached (offline, corporate proxy,
// etc.) or has no matching-major stable release yet. For dev/local builds without a `+` in
// InformationalVersion (i.e. no build-metadata suffix), falls back further to a
// known-published floor so we never emit a bogus pin like Version="LOCAL". Inlined to keep
// Fallout.Migrate dependency-free.

/// <summary>
/// Resolves the Fallout version to pin and stores it on <see cref="MigrationContext.FalloutVersion"/>.
/// Must run first in <see cref="Migration.steps"/>: <see cref="Steps.RewriteCsprojsStep"/> reads the
/// resolved version to pin rewritten package references.
/// </summary>
internal sealed class ResolveFalloutVersionStep : IMigrationStep
{
    /// <summary>
    /// The version to fall back to when the assembly carries no build-metadata suffix.
    /// </summary>
    private const string Fallback = "10.3.49";

    /// <summary>
    /// The NuGet package whose latest stable version is used to pin migrated references.
    /// </summary>
    private const string PackageId = "fallout.common";

    /// <summary>
    /// NuGet's flat-container index for <see cref="PackageId"/>: a JSON document listing every
    /// published version (stable and prerelease), oldest first.
    /// </summary>
    private static readonly Uri FlatContainerIndex = new($"https://api.nuget.org/v3-flatcontainer/{PackageId}/index.json");

    private static readonly HttpClient httpClient = CreateHttpClient();

    /// <inheritdoc />
    public async Task ExecuteAsync(MigrationContext context, Summary summary)
    {
        string localVersion = ResolveFromAssembly();
        int localMajor = ExtractMajor(localVersion);

        string nuGetVersion = await ResolveFromNuGetAsync(localMajor);
        if (nuGetVersion != null)
        {
            AnsiConsole.MarkupLineInterpolated(
                $"[grey]Resolved Fallout version [bold]{nuGetVersion}[/] from NuGet (latest stable {localMajor}.x release).[/]");

            context.FalloutVersion = nuGetVersion;
        }
        else
        {
            AnsiConsole.MarkupLineInterpolated(
                $"[grey]No stable {localMajor}.x release found on NuGet; using the running tool's own version [bold]{localVersion}[/].[/]");

            context.FalloutVersion = localVersion;
        }
    }

    /// <summary>
    /// Reads the tool assembly's <see cref="AssemblyInformationalVersionAttribute"/> and strips the
    /// build-metadata suffix, falling back to <see cref="Fallback"/> when none is present.
    /// </summary>
    /// <returns>The Fallout version to pin into rewritten package references.</returns>
    private static string ResolveFromAssembly()
    {
        string informational = typeof(ResolveFalloutVersionStep).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (string.IsNullOrEmpty(informational))
        {
            return Fallback;
        }

        int plusIndex = informational.IndexOf('+');
        if (plusIndex == -1)
        {
            return Fallback;
        }

        return informational[..plusIndex];
    }

    /// <summary>
    /// Extracts the major version segment (e.g. the calendar year) from a version string, ignoring
    /// any prerelease or build-metadata suffix.
    /// </summary>
    /// <param name="version">A version string such as <c>"2026.1.0-preview.134"</c>.</param>
    /// <returns>The parsed major version.</returns>
    private static int ExtractMajor(string version)
    {
        int dotIndex = version.IndexOf('.');
        string majorSegment = dotIndex == -1 ? version : version[..dotIndex];

        return int.Parse(majorSegment, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Queries NuGet for the latest non-prerelease version of <see cref="PackageId"/> whose major
    /// version matches <paramref name="major"/>.
    /// </summary>
    /// <param name="major">The major version (calendar year) to restrict the lookup to.</param>
    /// <returns>The latest published stable version within <paramref name="major"/>, or <c>null</c>
    /// if NuGet couldn't be reached or has no matching-major stable version.</returns>
    private static async Task<string> ResolveFromNuGetAsync(int major)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(FlatContainerIndex);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using Stream stream = await response.Content.ReadAsStreamAsync();
            using JsonDocument document = await JsonDocument.ParseAsync(stream);
            if (!document.RootElement.TryGetProperty("versions", out JsonElement versions))
            {
                return null;
            }

            Version latest = null;
            foreach (JsonElement element in versions.EnumerateArray())
            {
                string raw = element.GetString();

                // Skip prerelease versions (e.g. "2026.1.0-preview.12+g1a2b3c") — only stable
                // releases are pinned into migrated package references.
                if (string.IsNullOrEmpty(raw) || raw.Contains('-'))
                {
                    continue;
                }

                if (Version.TryParse(raw, out Version parsed) && parsed.Major == major &&
                    (latest is null || parsed > latest))
                {
                    latest = parsed;
                }
            }

            return latest?.ToString();
        }
        catch (Exception ex)
        {
            // Best-effort: offline, proxy, DNS failure, malformed response, etc. all fall back
            // to the running tool's own version above.
            AnsiConsole.MarkupLineInterpolated($"[grey]NuGet lookup failed ({ex.Message}); falling back.[/]");
            return null;
        }
    }

    /// <summary>
    /// Creates the <see cref="HttpClient"/> used for the NuGet flat-container lookup, identifying
    /// this tool via a <c>User-Agent</c> header so NuGet.org doesn't treat the request as anonymous
    /// script traffic.
    /// </summary>
    /// <returns>A configured <see cref="HttpClient"/>.</returns>
    private static HttpClient CreateHttpClient()
    {
        HttpClient client = new() { Timeout = TimeSpan.FromSeconds(3) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("fallout-migrate");

        return client;
    }
}
