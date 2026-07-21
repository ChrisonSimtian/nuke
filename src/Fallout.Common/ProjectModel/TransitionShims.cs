using Fallout.Common.IO;
using Fallout.Persistence.Solution.Model;

namespace Fallout.Common.ProjectModel;

// Transition shims for the solution entry-point types, which shipped under
// `Fallout.Common.ProjectModel` in Fallout 11.0.1–11.0.12 and moved to the
// dedicated `Fallout.Solutions` namespace in 11.0.13 (#248 / #257). Mirroring the
// pair below keeps the canonical consumer pattern
//
//     using Fallout.Common.ProjectModel;
//     [Solution] readonly Solution Solution;
//
// compiling for consumers upgrading from those releases, without their having to
// run `fallout-migrate` first.
//
// SHALLOW BY DESIGN. This is an entry-point grace period, not a full alias of the
// old namespace. Navigating the graph (e.g. `solution.Projects`) hands back
// canonical `Fallout.Solutions.*` instances, so intermediate variables typed
// against `Fallout.Common.ProjectModel` will not bind, and `GenerateProjects`
// source-generation keys off the canonical attribute type and won't fire through
// the shim. `fallout-migrate` (or the Nuke→Fallout codefix) is the complete
// migration path — it rewrites every reference, shallow and deep. This mirrors the
// same ceiling the generated `Nuke.Common.ProjectModel` transition shim has.

/// <summary>
/// Transition shim for the relocated <see cref="Fallout.Solutions.Solution"/>. See the file-level
/// remarks — shallow by design; run <c>fallout-migrate</c> for a complete rewrite.
/// </summary>
public class Solution(SolutionModel model, AbsolutePath path = null)
    : global::Fallout.Solutions.Solution(model, path)
{
    // C# does not inherit user-defined conversion operators onto a subclass, so the
    // canonical Solution's string / AbsolutePath coercions would silently disappear
    // on the shim. Re-expose them so `string s = Solution;` and `AbsolutePath p = Solution;`
    // keep working. The parameter type (shim Solution) is more specific than the
    // canonical's, so these win over the inherited operators without ambiguity.
    public static implicit operator string(Solution solution) => (global::Fallout.Solutions.Solution)solution;
    public static implicit operator AbsolutePath(Solution solution) => (global::Fallout.Solutions.Solution)solution;
}

/// <summary>
/// Transition shim for the relocated <see cref="Fallout.Solutions.SolutionAttribute"/>. Deserializes
/// into the annotated member's declared type, so <c>[Solution] readonly Solution Solution;</c> against
/// the shim <see cref="Solution"/> above resolves correctly.
/// </summary>
public class SolutionAttribute(string relativePath)
    : global::Fallout.Solutions.SolutionAttribute(relativePath)
{
    public SolutionAttribute()
        : this(relativePath: null)
    {
    }
}
