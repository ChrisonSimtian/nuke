using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Fallout.Build")]
[assembly: InternalsVisibleTo("Fallout.Build.Shared")]
[assembly: InternalsVisibleTo("Fallout.Build.Specs")]
[assembly: InternalsVisibleTo("Fallout.Common")]
[assembly: InternalsVisibleTo("Fallout.Common.Specs")]
[assembly: InternalsVisibleTo("Fallout.Cli")]
[assembly: InternalsVisibleTo("Fallout.Cli.Specs")]
[assembly: InternalsVisibleTo("Fallout.ProjectModel.Specs")]
[assembly: InternalsVisibleTo("Fallout.SourceGenerators")]
[assembly: InternalsVisibleTo("Fallout.Solution.Codegen")]
[assembly: InternalsVisibleTo("Fallout.Solution.Codegen.Specs")]
[assembly: InternalsVisibleTo("Fallout.Solution")]
[assembly: InternalsVisibleTo("Fallout.Solution.Specs")]
[assembly: InternalsVisibleTo("Fallout.Persistence.Solution")]
[assembly: InternalsVisibleTo("Fallout.Persistence.Solution.Tests")]
[assembly: InternalsVisibleTo("Fallout.Tooling")]
[assembly: InternalsVisibleTo("Fallout.Tooling.Specs")]
[assembly: InternalsVisibleTo("Fallout.Utilities.IO.Globbing")]
[assembly: InternalsVisibleTo("Fallout.Utilities.Specs")]

// External extensions — kept as Nuke.* until those projects rebrand independently.
[assembly: InternalsVisibleTo("Nuke.VisualStudio")]
[assembly: InternalsVisibleTo("ReSharper.Nuke")]
[assembly: InternalsVisibleTo("ReSharper.Nuke.Rider")]

// External functions — same: outside this repo's rebrand scope.
[assembly: InternalsVisibleTo("Nuke.Remote.Functions")]
[assembly: InternalsVisibleTo("Nuke.Website.Functions")]
