using Fallout.Migrate.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Fallout.Migrate.Analyzers.Specs;

// Convenience aliases so the test bodies stay focused on what they assert,
// not on plumbing.
//
// Note: we use DefaultVerifier rather than the deprecated XUnitVerifier — the
// latter is incompatible with current xunit (its EqualException ctor was
// removed). DefaultVerifier throws InvalidOperationException on assertion
// failure, which xunit still reports as a test failure with the diff message
// intact.

internal static class AnalyzerTestHarness
{
    // Adds a Fallout.* metadata reference to satisfy the analyzer's "must
    // reference Fallout.* to fire" guard. Without this every test would silently
    // pass because the guard short-circuits.
    public static readonly MetadataReference FalloutMarkerReference =
        MetadataReference.CreateFromFile(typeof(global::Fallout.Common.Utilities.CompletionUtility).Assembly.Location);
}

internal sealed class AnalyzerSpecs : CSharpAnalyzerTest<NukeMigrationAnalyzer, DefaultVerifier>
{
    public AnalyzerSpecs()
    {
        TestState.AdditionalReferences.Add(AnalyzerTestHarness.FalloutMarkerReference);
    }
}

internal sealed class CodeFixSpecs : CSharpCodeFixTest<NukeMigrationAnalyzer, NukeMigrationCodeFix, DefaultVerifier>
{
    public CodeFixSpecs()
    {
        TestState.AdditionalReferences.Add(AnalyzerTestHarness.FalloutMarkerReference);
        FixedState.AdditionalReferences.Add(AnalyzerTestHarness.FalloutMarkerReference);
    }
}
