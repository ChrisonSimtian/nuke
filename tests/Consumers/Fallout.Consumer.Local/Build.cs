//
// Fallout consumer against this repo's local source. Catches breakage of the
// public Fallout surface in the current PR.

using Fallout.Common;
using Fallout.Solutions;
using Serilog;

// was Fallout.Common.ProjectModel; — renamed in #254 (persistence layering + namespace cleanup)

internal class Build : FalloutBuild
{
    public static int Main() => Execute<Build>(x => x.Default);

    [Solution]
    private readonly Solution Solution;

    private Target Default => _ => _
        .Executes(() =>
        {
            Log.Information("hello from fallout consumer (local source)");
            Log.Information("solution name: {Name}", Solution?.Name ?? "<unbound>");
        });
}
