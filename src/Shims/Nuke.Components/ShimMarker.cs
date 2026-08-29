// Tells the TransitionShimGenerator to emit shims for every public type whose
// namespace begins with "Fallout.Components" into the corresponding
// "Nuke.Components" namespace. The bulk of this assembly is the component
// interface family (ICompile, IRestore, IPack, ITest, IPublish, IHas*).

using Fallout.Migrate.Shims;

[assembly: ShimAllPublicTypesUnder(
    fromNamespacePrefix: "Fallout.Components",
    toNamespacePrefix: "Nuke.Components")]
