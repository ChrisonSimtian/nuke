using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyXunit;
using Xunit;

namespace Fallout.SourceGenerators.Specs;

public class TransitionShimGeneratorSpecs
{
    // Each kind in the Easy tier (regular class, abstract class, interface,
    // attribute, generic class, nested type) emits a representative shim. The
    // Hard tier kinds (sealed class, static class, enum, delegate, class with
    // no public/protected ctor) emit SHIM001 diagnostics instead. Captures both
    // as a Verify snapshot.
    [Fact]
    public Task EmitsShimsForEachKindAndSkipsHardTier()
    {
        var canonical = CompileCanonicalAssembly("""
            namespace Fallout.Common
            {
                // Easy tier
                public class Regular { public Regular(string a) {} public Regular() {} }
                public abstract class Abstr { protected Abstr() {} }
                public interface IFoo { }
                [System.AttributeUsage(System.AttributeTargets.Class)]
                public class MyAttr : System.Attribute { public MyAttr(int n) {} }
                public class Generic<T> where T : class { public Generic(T item) {} }
                public class WithNested
                {
                    public WithNested() {}
                    public class Nested { public Nested() {} }
                }

                // Hard tier — sealed-class still skipped (deferred to session 2b)
                public sealed class SealedThing { public SealedThing() {} }
                public enum MyEnum { A, B }
                public class PrivateCtorOnly { private PrivateCtorOnly(string x) {} }

                // Static-class with the various method shapes that need delegation
                public static class StaticHelpers
                {
                    public static int Plain(int a) { return a; }
                    public static void VoidReturn(string s) { }
                    public static T Generic<T>(T input) where T : class { return input; }
                    public static int WithOptional(int a, int b = 7, string s = "hello") { return a + b; }
                    public static int Sum(params int[] nums) { return 0; }
                    public static int TryParse(string s, out int value) { value = 0; return 0; }
                    public static string AsHex(this byte b) { return b.ToString("x2"); }
                }
            }
            """);

        var shimCompilation = CSharpCompilation.Create("Nuke.TestShim",
            new[] { CSharpSyntaxTree.ParseText("""
                [assembly: Fallout.Migrate.Shims.ShimAllPublicTypesUnder("Fallout.Common", "Nuke.Common")]
                """) },
            Basic.Reference.Assemblies.NetStandard20.References.All
                .Concat(new[] { canonical }),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new TransitionShimGenerator());
        var result = driver.RunGenerators(shimCompilation);
        return Verifier.Verify(result);
    }

    // Hand-bridge suppression: when the consuming compilation declares a type at
    // the target shim FQN, the generator treats that as the authoritative bridge —
    // no emission, no SHIM001 (even for canonical kinds that would otherwise be
    // skipped as Hard tier). Mirrors the session-4 CI host pattern in Nuke.Common.
    [Fact]
    public void SkipsCanonicalTypesAlreadyHandBridgedByConsumer()
    {
        var canonical = CompileCanonicalAssembly("""
            namespace Fallout.Common
            {
                public sealed class HandBridgedSealed { public HandBridgedSealed() {} }
                public class HandBridgedRegular { public HandBridgedRegular() {} }
            }
            """);

        var shimCompilation = CSharpCompilation.Create("Nuke.HandBridged",
            new[]
            {
                CSharpSyntaxTree.ParseText("""
                    [assembly: Fallout.Migrate.Shims.ShimAllPublicTypesUnder("Fallout.Common", "Nuke.Common")]
                    """),
                CSharpSyntaxTree.ParseText("""
                    namespace Nuke.Common
                    {
                        public static class HandBridgedSealed { }
                        public static class HandBridgedRegular { }
                    }
                    """),
            },
            Basic.Reference.Assemblies.NetStandard20.References.All
                .Concat(new[] { canonical }),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new TransitionShimGenerator());
        var result = driver.RunGenerators(shimCompilation).GetRunResult();

        // No source emitted for the hand-bridged canonical types.
        result.GeneratedTrees
            .Where(t => !t.FilePath.EndsWith("ShimAllPublicTypesUnderAttribute.g.cs", System.StringComparison.Ordinal))
            .Should().BeEmpty();

        // And no SHIM001 diagnostic for either hand-bridged type — the sealed
        // one would normally warn, the regular one would normally emit.
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void EmitsNothingWhenNoMarkerAttributePresent()
    {
        var canonical = CompileCanonicalAssembly("""
            namespace Fallout.Common
            {
                public class Whatever { public Whatever() {} }
            }
            """);

        var shimCompilation = CSharpCompilation.Create("Nuke.NoMarker",
            new[] { CSharpSyntaxTree.ParseText("// no marker") },
            Basic.Reference.Assemblies.NetStandard20.References.All
                .Concat(new[] { canonical }),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new TransitionShimGenerator());
        var result = driver.RunGenerators(shimCompilation).GetRunResult();

        // The post-init attribute definition is the only output expected.
        result.GeneratedTrees
            .Where(t => !t.FilePath.EndsWith("ShimAllPublicTypesUnderAttribute.g.cs", System.StringComparison.Ordinal))
            .Should().BeEmpty();
    }

    private static MetadataReference CompileCanonicalAssembly(string source)
    {
        var compilation = CSharpCompilation.Create("Fallout.TestCanonical",
            new[] { CSharpSyntaxTree.ParseText(source) },
            Basic.Reference.Assemblies.NetStandard20.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var stream = new MemoryStream();
        var emit = compilation.Emit(stream);
        emit.Success.Should().BeTrue(
            because: "canonical test compilation should compile: {0}",
            string.Join("; ", emit.Diagnostics.Select(d => d.GetMessage())));
        stream.Position = 0;
        return MetadataReference.CreateFromStream(stream);
    }
}
