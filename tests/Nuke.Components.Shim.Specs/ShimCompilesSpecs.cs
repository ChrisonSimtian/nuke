using Xunit;

namespace Nuke.Components.Shim.Specs;

// The whole point of this project is to verify that SampleConsumerBuild.cs
// compiles against the Nuke.Components shim — the build IS the test. But
// xUnit's `dotnet test` discoverer treats a test project with zero
// discoverable tests as an error on Windows (exit code 1) while emitting only
// a warning on Linux/macOS. This trivial test makes discovery succeed on all
// platforms.
public class ShimCompilesSpecs
{
    [Fact]
    public void Shim_Compiles_When_Test_Assembly_Loads()
    {
        Assert.True(true);
    }
}
