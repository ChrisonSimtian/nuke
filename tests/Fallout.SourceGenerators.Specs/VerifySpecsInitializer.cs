using System;
using System.Runtime.CompilerServices;
using VerifyTests;

namespace Fallout.SourceGenerators.Specs;

public static class VerifyTestsInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        Environment.SetEnvironmentVariable("DiffEngine_Disabled", "true");
        Environment.SetEnvironmentVariable("Verify_DisableClipboard", "true");
        VerifyDiffPlex.Initialize();
        VerifySourceGenerators.Initialize();
    }
}
