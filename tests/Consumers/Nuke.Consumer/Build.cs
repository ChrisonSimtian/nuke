//
// Pre-rename NUKE consumer pattern, compiled against the Nuke.Common /
// Nuke.Components transition shims. If a typical NUKE 10.x Build.cs stops
// compiling against the latest Fallout, this fails — protecting upgrading
// users from silent breakage.

using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Components;

// The shim generator skips delegates by C# language limitation (see SHIM002 —
// can't subclass a delegate cross-assembly). `Target` is a delegate in
// Fallout.Common, so NUKE-era code referencing `Target` needs either
// `fallout-migrate` (which flips usings to Fallout.*) or this manual alias.
// Including it here keeps the rest of the file NUKE-shape.
using Target = Fallout.Common.Target;

internal class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Default);

    [Solution]
    private readonly Solution Solution;

    private Target Default => _ => _
        .Executes(() =>
        {
            Serilog.Log.Information("hello from nuke consumer (via shim)");
            Serilog.Log.Information("solution name: {Name}", Solution?.Name ?? "<unbound>");
        });
}
