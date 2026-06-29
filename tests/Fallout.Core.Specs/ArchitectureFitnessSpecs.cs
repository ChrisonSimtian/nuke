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

    private static string FailingTypes(TestResult result) =>
        result.FailingTypeNames is null ? "(none reported)" : string.Join(", ", result.FailingTypeNames);
}
