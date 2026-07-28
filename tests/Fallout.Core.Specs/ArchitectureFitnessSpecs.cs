using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Fallout.Core.Planning;
using NetArchTest.Rules;
using Xunit;

namespace Fallout.Core.Specs;

/// <summary>
/// The acceptance criterion for issue #88: Fallout.Core is the pure reactor core. It depends on
/// nothing in the repo and never touches I/O, processes, the console, or logging. The broader
/// architecture-fitness suite lands in #95; these two tests guard the Core invariant specifically.
/// </summary>
public class ArchitectureFitnessSpecs
{
    private static readonly Assembly CoreAssembly = typeof(TopoSort).Assembly;

    [Fact]
    public void Core_has_no_io_process_console_or_logging_dependency()
    {
        // Scope to our own Fallout.* types only. This excludes build-tool noise injected into the
        // assembly that we don't author and can't keep pure: the generated `ThisAssembly`
        // (Nerdbank.GitVersioning, no namespace) and `Coverlet.Core.Instrumentation.Tracker.*`
        // (coverage instrumentation under `./build.ps1 Test`, which legitimately touches System.IO).
        // Precise tokens (e.g. "System.Diagnostics.Process") rather than the broad "System.Diagnostics"
        // namespace also avoid NetArchTest false-positives on generic types.
        var result = Types.InAssembly(CoreAssembly)
            .That().ResideInNamespaceStartingWith("Fallout")
            .Should()
            .NotHaveDependencyOnAny(
                "System.IO",
                "System.Diagnostics.Process",
                "System.Console",
                "Serilog")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Fallout.Core must stay pure; offending types: " + FailingTypes(result));
    }

    [Fact]
    public void Core_does_not_depend_on_higher_fallout_layers()
    {
        var result = Types.InAssembly(CoreAssembly)
            .That().ResideInNamespaceStartingWith("Fallout")
            .Should()
            .NotHaveDependencyOnAny(
                "Fallout.Build",
                "Fallout.Common.Tooling",
                "Fallout.Common.Utilities",
                "Fallout.ProjectModel",
                "Fallout.Tooling",
                "Fallout.Utilities")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Fallout.Core sits at the bottom and must reference no other Fallout project; " +
                     "offending types: " + FailingTypes(result));
    }

    /// <summary>
    /// Namespaces that predate this rule and cannot be corrected yet. <c>Fallout.Common.Execution</c>
    /// holds <c>ExecutionStatus</c> and <c>ITargetModel</c>, both lifted into Core by the
    /// de-statification work and both <c>public</c> — renaming their namespace is a breaking change,
    /// so it is batched to the yearly major cut (AGENTS.md rule #1). The list is frozen: it is a
    /// baseline to shrink, never to grow.
    /// </summary>
    private static readonly string[] GrandfatheredNamespaces = ["Fallout.Common.Execution"];

    /// <summary>
    /// Core is the innermost ring, so it must not declare a type under an outer layer's namespace.
    /// It owns <c>Fallout.Core.*</c>, plus the root <c>Fallout</c> namespace for genuinely
    /// cross-cutting values such as <c>Fallout.Constants</c>. A type sitting in, say,
    /// <c>Fallout.Common</c> while shipping in <c>Fallout.Core.dll</c> inverts the onion in naming
    /// even when the reference direction is legal, which the dependency tests above cannot see.
    /// </summary>
    [Fact]
    public void Core_declares_no_type_in_an_outer_layer_namespace()
    {
        var offenders = CoreAssembly.GetTypes()
            .Select(x => x.Namespace)
            .Where(x => x is not null && x.StartsWith("Fallout", StringComparison.Ordinal))
            .Where(x => x != "Fallout" && !x!.StartsWith("Fallout.Core", StringComparison.Ordinal))
            .Where(x => !GrandfatheredNamespaces.Contains(x))
            .Distinct()
            .ToList();

        offenders.Should().BeEmpty(
            because: "Fallout.Core may only declare types under Fallout.Core.* or the root Fallout " +
                     "namespace; offending namespaces: " + string.Join(", ", offenders));
    }

    private static string FailingTypes(TestResult result) =>
        result.FailingTypeNames is null ? "(none reported)" : string.Join(", ", result.FailingTypeNames);
}
