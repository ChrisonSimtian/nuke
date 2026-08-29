// Tells the TransitionShimGenerator to emit shims for every public type whose
// namespace begins with "Fallout.Common." into the corresponding "Nuke.Common."
// namespace. The generator walks all referenced Fallout.* assemblies; both
// Fallout.Common and Fallout.Build participate (FalloutBuild itself lives in
// the Fallout.Common namespace despite being declared in the Fallout.Build
// project).

// ProjectModel is excluded here: those solution types relocated to
// Fallout.Solutions in v11 and are shimmed by the marker below. Fallout.Common
// also re-exports Solution/SolutionAttribute under Fallout.Common.ProjectModel as
// a Fallout-side entry-point grace shim (#257 mitigation) — without this
// exclusion, Rule 1 and the Fallout.Solutions rule would both emit
// Nuke.Common.ProjectModel.Solution, colliding (CS0263/CS0111).

using Fallout.Migrate.Shims;

[assembly: ShimAllPublicTypesUnder(
    fromNamespacePrefix: "Fallout.Common",
    toNamespacePrefix: "Nuke.Common",
    ExceptNamespacePrefixes = new[]
    {
        "Fallout.Common.ProjectModel"
    })]

// The solution-handling types moved from Fallout.Common.ProjectModel to the
// dedicated Fallout.Solutions namespace in v11 (see #248 and the broader
// onion-layering work). For NUKE-era consumers, mirror them into the legacy
// Nuke.Common.ProjectModel namespace so existing `using Nuke.Common.ProjectModel;`
// + `[Solution] readonly Solution Solution;` keep compiling.
[assembly: ShimAllPublicTypesUnder(
    fromNamespacePrefix: "Fallout.Solutions",
    toNamespacePrefix: "Nuke.Common.ProjectModel")]
