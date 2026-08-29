// Tells the TransitionShimGenerator to emit shims for every public type whose
// namespace begins with "Fallout.Build." into the corresponding "Nuke.Build."
// namespace.

using Fallout.Migrate.Shims;

[assembly: ShimAllPublicTypesUnder(
    fromNamespacePrefix: "Fallout.Build",
    toNamespacePrefix: "Nuke.Build")]
