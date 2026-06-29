using System.Runtime.CompilerServices;
using Fallout.Solutions;

namespace Fallout.Common.Specs;

internal static class ModuleInit
{
    // Microsoft.Build is excluded from runtime output (ExcludeAssets="runtime") and resolved
    // at runtime via the AssemblyResolve handler that ProjectModelTasks installs from its own
    // [ModuleInitializer]. If a test method declares a local of a Microsoft.Build type, the
    // JIT resolves Microsoft.Build.dll before any Fallout.ProjectModel code runs — and the
    // resolver is not yet installed. Touching ProjectModelTasks here forces Fallout.ProjectModel
    // to load (and its module initializer to fire) before xUnit JITs the first test.
    [ModuleInitializer]
    public static void EnsureMSBuildResolverRegistered()
    {
        ProjectModelTasks.Initialize();
    }
}
