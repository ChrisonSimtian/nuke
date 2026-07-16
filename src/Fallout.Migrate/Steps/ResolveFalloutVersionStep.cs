using System.Reflection;
using System.Threading.Tasks;
using Fallout.Migrate.Common;

namespace Fallout.Migrate.Steps;

// Pinned into migrated `<PackageReference Include="Fallout.X" Version="..." />` lines.

// Uses the running migrate tool's own SemVer (Nerdbank.GitVersioning, set on

// AssemblyInformationalVersion) so the migration output aligns with the tool the user

// just installed. For dev/local builds without a `+` in InformationalVersion (i.e. no

// build-metadata suffix), falls back to a known-published floor so we never emit a

// bogus pin like Version="LOCAL". Inlined to keep Fallout.Migrate dependency-free.
/// <summary>
/// Resolves the running tool's own Fallout version and stores it on <see cref="MigrationContext.FalloutVersion"/>.
/// Must run first in <see cref="Migration.steps"/>: <see cref="Steps.RewriteCsprojsStep"/> reads the
/// resolved version to pin rewritten package references.
/// </summary>
internal sealed class ResolveFalloutVersionStep : IMigrationStep
{
    /// <summary>The version to fall back to when the assembly carries no build-metadata suffix.</summary>
    private const string Fallback = "10.3.49";

    /// <inheritdoc />
    public Task ExecuteAsync(MigrationContext context, Summary summary)
    {
        context.FalloutVersion = Resolve();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads the tool assembly's <see cref="AssemblyInformationalVersionAttribute"/> and strips the
    /// build-metadata suffix, falling back to <see cref="Fallback"/> when none is present.
    /// </summary>
    /// <returns>The Fallout version to pin into rewritten package references.</returns>
    private static string Resolve()
    {
        var informational = typeof(ResolveFalloutVersionStep).Assembly
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
}
