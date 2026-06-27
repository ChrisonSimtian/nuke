using System.Linq;
using System.Reflection;
using Xunit;

namespace Nuke.Common.Shim.Tests;

// Runtime counterpart to the compile-only SampleConsumerBuild: actually exercises host
// auto-detection with the shim loaded. The shim emits a public Host subclass
// (`Nuke.Common.Host`) that doesn't follow the `IsRunning{Name}` convention; detection
// used to assert-throw on it inside FalloutBuild's static ctor, aborting every build that
// referenced the shim. Compile-only tests never caught it because they never run a build.
// See Fallout.Canary#3.
public class HostDetectionTests
{
    [Fact]
    public void FalloutBuildHost_Resolves_DespiteConventionLessShimHostSubclass()
    {
        // Force the Nuke.Common shim assembly to load so detection sees its types.
        var shimAssembly = typeof(global::Nuke.Common.NukeBuild).Assembly;

        // Precondition: the shim really does emit a public, convention-less Host subclass.
        // Without one, this regression test has no teeth.
        var offenders = shimAssembly.GetTypes()
            .Where(t => t.IsPublic && t.IsSubclassOf(typeof(global::Fallout.Common.Host)))
            .Where(t => t.GetProperty($"IsRunning{t.Name}", BindingFlags.Public | BindingFlags.Static) is null)
            .ToList();
        Assert.NotEmpty(offenders);

        // Touching FalloutBuild runs its static ctor -> Host.Default, scanning every public
        // Host subclass. Resolving the property (not throwing) is the assertion.
        var host = global::Fallout.Common.FalloutBuild.Host;
        Assert.NotNull(host);
    }
}
