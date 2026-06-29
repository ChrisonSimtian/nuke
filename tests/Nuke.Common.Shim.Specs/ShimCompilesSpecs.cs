using Xunit;

namespace Nuke.Common.Shim.Specs;

// The whole point of this project is to verify that SampleConsumerBuild.cs
// compiles against the Nuke.* shim — the build IS the test. But xUnit's
// `dotnet test` discoverer treats a test project with zero discoverable tests
// as an error on Windows (exit code 1) while emitting only a warning on
// Linux/macOS. That platform-split silently fails the post-merge
// windows-latest validation on main.
//
// This trivial test makes discovery succeed on all platforms and explicitly
// encodes the project's contract: if you can build it, the shim covers the
// surface SampleConsumerBuild exercises.
public class ShimCompilesSpecs
{
    [Fact]
    public void Shim_Compiles_When_Test_Assembly_Loads()
    {
        // Reaching this assertion proves the assembly loaded, which proves
        // SampleConsumerBuild.cs (and therefore the Nuke.* shim it uses) compiled.
        Assert.True(true);
    }
}
