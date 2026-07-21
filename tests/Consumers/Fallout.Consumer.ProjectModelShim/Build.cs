//
// Pre-rename Fallout consumer pattern (Fallout 11.0.1–11.0.12), compiled against
// the Fallout.Common.ProjectModel transition shim. If a Build.cs from those
// releases stops compiling against the latest Fallout, this fails — protecting
// users upgrading across the SolutionModel → Solution / Fallout.Solutions rename.

using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.ProjectModel;  // the interim namespace, now served by the transition shim

internal class Build : FalloutBuild
{
    public static int Main() => Execute<Build>(x => x.Default);

    [Solution]
    private readonly Solution Solution;

    private Target Default => _ => _
        .Executes(() =>
        {
            Serilog.Log.Information("hello from fallout consumer (Fallout.Common.ProjectModel shim)");
            Serilog.Log.Information("solution name: {Name}", Solution?.Name ?? "<unbound>");
        });
}
