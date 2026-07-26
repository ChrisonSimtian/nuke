using Xunit;

namespace Fallout.Common.Specs;

/// <summary>
/// Serialises the specs that read or mutate the process-wide singletons a build run touches — the
/// in-memory sink, the value-injection cache, the tool-path resolver config. xUnit runs test classes
/// in parallel by default, and these singletons are shared across every flow, so without a common
/// collection one class's reset can land in the middle of another's arrange/assert.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ProcessGlobalStateCollection
{
    public const string Name = "process-global state";
}
