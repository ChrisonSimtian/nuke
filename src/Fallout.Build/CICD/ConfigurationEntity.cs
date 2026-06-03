using System;
using System.Linq;
using Fallout.Common.Utilities;

namespace Fallout.Common.CI;

/// <summary>
/// Base for a CI/CD config node that writes itself through a <see cref="CustomFileWriter"/>.
/// </summary>
/// <remarks>
/// The CI config writers are hand-rolled on purpose (Kotlin DSL targets + exact comment/quote/indent
/// control on YAML targets); see <c>CustomFileWriter</c> and
/// <see href="https://github.com/ChrisonSimtian/Fallout/blob/main/docs/dependencies-kept.md">docs/dependencies-kept.md</see>.
/// </remarks>
public abstract class ConfigurationEntity
{
    public abstract void Write(CustomFileWriter writer);
}
