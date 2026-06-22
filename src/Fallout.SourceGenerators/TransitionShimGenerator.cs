#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fallout.SourceGenerators;

/// <summary>
/// Source generator that produces transition shims under the Nuke.* namespaces by
/// mirroring public types from canonical Fallout.* assemblies. See the
/// <see cref="ShimAttributeSource"/> marker emitted via PostInitializationOutput.
/// </summary>
/// <remarks>
/// Session 1 scope (Easy tier): regular classes, abstract classes, interfaces,
/// attributes. Skips sealed classes, static classes, enums, delegates — those
/// surface as SHIM001 diagnostics so we can track what's still uncovered.
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class TransitionShimGenerator : IIncrementalGenerator
{
    private const string AttributeNamespace = "Fallout.Migrate.Shims";
    private const string AttributeName = "ShimAllPublicTypesUnderAttribute";
    private const string AttributeFullName = AttributeNamespace + "." + AttributeName;

    // SHIM001: actionable skips. Something COULD still bridge these — un-seal
    // the canonical, promote a ctor's visibility, dedup across assemblies, or
    // hand-write a shim. Warning by default.
    private static readonly DiagnosticDescriptor ActionableSkipRule = new(
        id: "SHIM001",
        title: "Transition-shim type skipped (actionable)",
        messageFormat: "Type '{0}' was skipped by the transition-shim generator (kind: {1}). Consumers relying on this via the Nuke.* shim need a hand-written bridge, a canonical-side fix (un-seal / widen ctor visibility), or to migrate via 'fallout-migrate'.",
        category: "Migration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Covers skip kinds where an action is available: sealed-class (un-seal canonical or hand-bridge), no-accessible-ctor (widen ctor visibility, hand-bridge static surface, or fallout-migrate), and ambiguous-across-assemblies (dedup at canonical or fallout-migrate).");

    // SHIM002: language-limit skips. C# can't subclass these kinds — they're
    // intentionally not bridged and are documented fallout-migrate territory.
    // Info severity, OFF by default; enable in .editorconfig if you want the
    // full skip catalogue.
    private static readonly DiagnosticDescriptor LanguageLimitSkipRule = new(
        id: "SHIM002",
        title: "Transition-shim type skipped (language limit; fallout-migrate territory)",
        messageFormat: "Type '{0}' was skipped by the transition-shim generator (kind: {1}). This kind cannot be subclassed in C# — use 'fallout-migrate' to rewrite the consumer's reference to the canonical Fallout.* name.",
        category: "Migration",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "Covers skip kinds bounded by C# language limits: enum, delegate, and struct cannot be subclassed cross-assembly, so a transparent shim is impossible. The consumer-side fix is always 'fallout-migrate'. Off by default to keep the build console quiet; opt in via dotnet_diagnostic.SHIM002.severity if you want the full inventory.");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Emit the marker attribute into the consuming compilation so shim
        // projects can declare `[assembly: ShimAllPublicTypesUnder(...)]` without
        // depending on a separate runtime assembly.
        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource("ShimAllPublicTypesUnderAttribute.g.cs", ShimAttributeSource));

        // Find marker assembly attribute uses, then combine with the full
        // compilation so we can walk referenced assemblies.
        var markers = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeFullName,
                predicate: static (_, _) => true,
                transform: static (syntaxCtx, _) => ExtractMarkers(syntaxCtx.TargetSymbol))
            .SelectMany(static (markers, _) => markers);

        var combined = markers.Collect().Combine(context.CompilationProvider);

        context.RegisterSourceOutput(combined, static (ctx, input) =>
        {
            var (markers, compilation) = input;
            if (markers.IsDefaultOrEmpty)
                return;

            foreach (var marker in markers)
            {
                EmitShimsForMarker(ctx, compilation, marker);
            }
        });
    }

    // ───────────────────────────────────────────────────────────────────────
    // Marker extraction
    // ───────────────────────────────────────────────────────────────────────

    private readonly struct ShimMarker
    {
        public ShimMarker(string fromPrefix, string toPrefix)
        {
            FromPrefix = fromPrefix;
            ToPrefix = toPrefix;
        }
        public string FromPrefix { get; }
        public string ToPrefix { get; }
    }

    private static ImmutableArray<ShimMarker> ExtractMarkers(ISymbol target)
    {
        // The target is the assembly itself (since this is an assembly-targeted
        // attribute). Pull the attributes off and extract the two string args.
        var results = ImmutableArray.CreateBuilder<ShimMarker>();
        foreach (var attr in target.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != AttributeFullName)
                continue;
            if (attr.ConstructorArguments.Length < 2)
                continue;
            var from = attr.ConstructorArguments[0].Value as string;
            var to = attr.ConstructorArguments[1].Value as string;
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                continue;
            results.Add(new ShimMarker(from!, to!));
        }
        return results.ToImmutable();
    }

    // ───────────────────────────────────────────────────────────────────────
    // Walk + emit
    // ───────────────────────────────────────────────────────────────────────

    private static void EmitShimsForMarker(SourceProductionContext ctx, Compilation compilation, ShimMarker marker)
    {
        // Pre-pass: find type FQNs that are ambiguous across Fallout.* assemblies.
        // If the same fully-qualified name exists in two different assemblies, a
        // shim delegating to that name produces CS0433 at the consumer's compile.
        // Skip those entirely.
        var fqnCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var assemblyRef in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (!assemblyRef.Name.StartsWith("Fallout.", StringComparison.Ordinal))
                continue;
            CountFqnsInNamespace(assemblyRef.GlobalNamespace, marker, fqnCounts);
        }
        var ambiguous = new HashSet<string>(
            fqnCounts.Where(kv => kv.Value > 1).Select(kv => kv.Key),
            StringComparer.Ordinal);

        // Pre-pass: collect FQNs of every top-level public type the consuming
        // compilation already declares under the target shim prefix. A hand-
        // written bridge at the target FQN is the consumer's authoritative
        // answer — the generator skips that canonical type silently (no
        // emission, no SHIM001).
        var handBridged = new HashSet<string>(StringComparer.Ordinal);
        CollectHandBridgedFqns(compilation.SourceModule.GlobalNamespace, marker.ToPrefix, handBridged);

        // The same logical type can be reached via multiple referenced assemblies.
        // Dedupe hint names so AddSource doesn't throw.
        var emittedHints = new HashSet<string>(StringComparer.Ordinal);

        foreach (var assemblyRef in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            // Cheap filter: only walk Fallout.* assemblies. Avoids touching the
            // BCL and other irrelevant deps.
            if (!assemblyRef.Name.StartsWith("Fallout.", StringComparison.Ordinal))
                continue;

            VisitNamespace(ctx, assemblyRef.GlobalNamespace, marker, emittedHints, ambiguous, handBridged);
        }
    }

    private static void CollectHandBridgedFqns(INamespaceSymbol ns, string toPrefix, HashSet<string> sink)
    {
        var fullNs = ns.ToDisplayString();
        var inScope = !string.IsNullOrEmpty(fullNs)
            && (fullNs == toPrefix || fullNs.StartsWith(toPrefix + ".", StringComparison.Ordinal));
        if (inScope)
        {
            foreach (var type in ns.GetTypeMembers())
            {
                if (type.DeclaredAccessibility != Accessibility.Public) continue;
                // Compose the metadata-style FQN: namespace + "." + MetadataName.
                // MetadataName encodes arity (e.g. `Foo`1` for `Foo<T>`), so this
                // matches arity correctly when we look it up later.
                sink.Add(fullNs + "." + type.MetadataName);
            }
        }
        foreach (var child in ns.GetNamespaceMembers())
            CollectHandBridgedFqns(child, toPrefix, sink);
    }

    private static void CountFqnsInNamespace(INamespaceSymbol ns, ShimMarker marker, Dictionary<string, int> counts)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            if (type.DeclaredAccessibility != Accessibility.Public) continue;
            var fullNamespace = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            if (string.IsNullOrEmpty(fullNamespace)) continue;
            var matches = fullNamespace == marker.FromPrefix
                || fullNamespace.StartsWith(marker.FromPrefix + ".", StringComparison.Ordinal);
            if (!matches) continue;
            var fqn = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            counts[fqn] = counts.TryGetValue(fqn, out var existing) ? existing + 1 : 1;
        }
        foreach (var child in ns.GetNamespaceMembers())
            CountFqnsInNamespace(child, marker, counts);
    }

    private static void VisitNamespace(SourceProductionContext ctx, INamespaceSymbol ns, ShimMarker marker, HashSet<string> emittedHints, HashSet<string> ambiguousFqns, HashSet<string> handBridgedFqns)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            if (type.DeclaredAccessibility != Accessibility.Public)
                continue;

            var fullNamespace = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            // Match `Fallout.Common` exactly OR any sub-namespace `Fallout.Common.X.Y`.
            // The marker stores the prefix without trailing dot.
            if (string.IsNullOrEmpty(fullNamespace)) continue;
            var matches = fullNamespace == marker.FromPrefix
                || fullNamespace.StartsWith(marker.FromPrefix + ".", StringComparison.Ordinal);
            if (!matches) continue;

            EmitOrSkipType(ctx, type, marker, emittedHints, ambiguousFqns, handBridgedFqns);
        }
        foreach (var child in ns.GetNamespaceMembers())
            VisitNamespace(ctx, child, marker, emittedHints, ambiguousFqns, handBridgedFqns);
    }

    private static void EmitOrSkipType(SourceProductionContext ctx, INamedTypeSymbol type, ShimMarker marker, HashSet<string> emittedHints, HashSet<string> ambiguousFqns, HashSet<string> handBridgedFqns)
    {
        // Skip nested types at top level — they get emitted inside their
        // declaring shim's source.
        if (type.ContainingType is not null)
            return;

        // If the consumer hand-wrote a shim at the target FQN, treat that as the
        // authoritative bridge. Skip both emission and the SHIM001 diagnostic —
        // the canonical type is covered, just not by us.
        var targetNs = SwapNamespace(type.ContainingNamespace.ToDisplayString(), marker);
        var targetMetadataFqn = targetNs + "." + type.MetadataName;
        if (handBridgedFqns.Contains(targetMetadataFqn))
            return;

        var fqn = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (ambiguousFqns.Contains(fqn))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                ActionableSkipRule,
                location: Location.None,
                type.ToDisplayString(),
                "ambiguous-across-assemblies"));
            return;
        }

        var skipReason = ClassifyForSkip(type);
        if (skipReason is not null)
        {
            var rule = IsLanguageLimitKind(skipReason) ? LanguageLimitSkipRule : ActionableSkipRule;
            ctx.ReportDiagnostic(Diagnostic.Create(
                rule,
                location: Location.None,
                type.ToDisplayString(),
                skipReason));
            return;
        }

        var hint = $"{HintName(type)}.g.cs";
        if (!emittedHints.Add(hint))
            return;  // Already emitted via another referenced assembly.

        var source = EmitTopLevelShim(type, marker);
        ctx.AddSource(hint, source);
    }

    private static bool IsLanguageLimitKind(string skipReason)
        => skipReason is "enum" or "delegate" or "struct";

    /// <summary>
    /// Returns a one-word kind label if this type should be skipped, otherwise null.
    /// </summary>
    private static string? ClassifyForSkip(INamedTypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum) return "enum";
        if (type.TypeKind == TypeKind.Delegate) return "delegate";
        // Class record falls through as Easy tier — handled in EmitTypeBody.
        if (type.TypeKind == TypeKind.Struct) return "struct";
        // Static classes are now session-2 supported via method-by-method
        // delegation — no longer skipped.
        if (type.IsSealed && type.TypeKind == TypeKind.Class && !type.IsRecord) return "sealed-class";
        // For classes (interfaces don't have constructors), require at least one
        // public or protected instance ctor — otherwise we can't subclass cross-assembly.
        // Static classes don't apply here (they don't get instantiated).
        if (type.TypeKind == TypeKind.Class && !type.IsStatic && !HasAnyAccessibleInstanceCtor(type))
            return "no-accessible-ctor";
        return null;
    }

    private static bool HasAnyAccessibleInstanceCtor(INamedTypeSymbol type)
    {
        // For classes with only non-public/non-protected ctors (e.g. all
        // private or internal), a shim subclass can't compile: the implicit
        // default of the subclass would chain to base() which doesn't exist.
        // We require at least one ctor accessible to a cross-assembly subclass.
        foreach (var ctor in type.InstanceConstructors)
        {
            if (ctor.DeclaredAccessibility == Accessibility.Public
                || ctor.DeclaredAccessibility == Accessibility.Protected
                || ctor.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
                return true;
        }
        return false;
    }

    // ───────────────────────────────────────────────────────────────────────
    // Emission
    // ───────────────────────────────────────────────────────────────────────

    private static string EmitTopLevelShim(INamedTypeSymbol type, ShimMarker marker)
    {
        var targetNs = SwapNamespace(type.ContainingNamespace.ToDisplayString(), marker);

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by Fallout.SourceGenerators.TransitionShimGenerator.");
        sb.AppendLine("// Mirrors a canonical Fallout.* type as a transition shim.");
        sb.AppendLine();
        sb.AppendLine($"namespace {targetNs}");
        sb.AppendLine("{");
        EmitTypeBody(sb, type, indentLevel: 1);
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void EmitTypeBody(StringBuilder sb, INamedTypeSymbol type, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);

        // Modifiers. We also add `new` when this is a nested type whose name
        // shadows a same-named nested type on the base (otherwise CS0108).
        // C# convention is `public new` order, not `new public`.
        var newKeyword = (type.ContainingType is not null && BaseHasNestedTypeNamed(type)) ? "new " : string.Empty;
        string modifiers;
        if (type.IsStatic)
            modifiers = $"public {newKeyword}static partial";
        else if (type.TypeKind == TypeKind.Interface)
            modifiers = $"public {newKeyword}partial";
        else if (type.IsAbstract)
            modifiers = $"public {newKeyword}abstract partial";
        else
            modifiers = $"public {newKeyword}partial";

        // Records keep their record-ness so the inheritance rules line up.
        // "Only records may inherit from records" (CS8865).
        string kind;
        if (type.IsStatic)
            kind = "class";  // static class
        else if (type.TypeKind == TypeKind.Interface)
            kind = "interface";
        else if (type.IsRecord)
            kind = "record class";
        else
            kind = "class";

        var genericParams = FormatGenericParameters(type);
        var canonicalFqn = FormatCanonicalReference(type);

        sb.Append(indent).Append(modifiers).Append(' ').Append(kind).Append(' ').Append(type.Name).Append(genericParams);

        // Static classes don't have a base type. Everything else inherits from the canonical.
        if (!type.IsStatic)
            sb.Append(" : ").Append(canonicalFqn);

        // Generic constraints
        var constraints = FormatGenericConstraints(type);
        if (!string.IsNullOrEmpty(constraints))
        {
            sb.AppendLine();
            sb.Append(indent).Append("    ").Append(constraints);
        }

        sb.AppendLine();
        sb.Append(indent).AppendLine("{");

        // Constructors (only for non-static classes — interfaces / static classes don't have them)
        if (type.TypeKind == TypeKind.Class && !type.IsStatic)
            EmitConstructors(sb, type, indentLevel + 1);

        // Static method delegation (only for static classes).
        // Non-static types inherit their instance methods from the canonical
        // base. Static methods on regular classes are inherited too, so we
        // don't redeclare those either.
        if (type.IsStatic)
            EmitStaticMethodDelegates(sb, type, indentLevel + 1);

        // Nested public types
        foreach (var nested in type.GetTypeMembers())
        {
            if (nested.DeclaredAccessibility != Accessibility.Public) continue;
            if (ClassifyForSkip(nested) is not null) continue;  // skip Hard-tier nesteds silently for now
            sb.AppendLine();
            EmitTypeBody(sb, nested, indentLevel + 1);
        }

        sb.Append(indent).AppendLine("}");
    }

    private static void EmitStaticMethodDelegates(StringBuilder sb, INamedTypeSymbol type, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        var canonicalFqn = FormatCanonicalReference(type);
        var emittedSig = new HashSet<string>(StringComparer.Ordinal);

        foreach (var member in type.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;
            if (method.DeclaredAccessibility != Accessibility.Public) continue;
            if (!method.IsStatic) continue;
            if (method.MethodKind != MethodKind.Ordinary) continue;  // skip operators, accessors, etc.
            if (method.IsImplicitlyDeclared) continue;

            // Dedupe by signature so we don't emit two identical method
            // delegations if the canonical has the same shape twice (shouldn't
            // happen but defensive).
            var sigKey = method.Name + "(" + string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))) + ")";
            if (!emittedSig.Add(sigKey)) continue;

            EmitOneStaticMethodDelegate(sb, method, canonicalFqn, indent);
        }
    }

    private static void EmitOneStaticMethodDelegate(StringBuilder sb, IMethodSymbol method, string canonicalFqn, string indent)
    {
        var returnType = method.ReturnsVoid
            ? "void"
            : method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var methodGenericParams = method.IsGenericMethod
            ? "<" + string.Join(", ", method.TypeParameters.Select(tp => tp.Name)) + ">"
            : string.Empty;

        var paramStrings = method.Parameters.Select(FormatParameter).ToList();
        if (method.IsExtensionMethod && paramStrings.Count > 0)
            paramStrings[0] = "this " + paramStrings[0];
        var parameterList = string.Join(", ", paramStrings);

        var argumentList = string.Join(", ", method.Parameters.Select(p =>
        {
            var prefix = p.RefKind switch
            {
                RefKind.Ref => "ref ",
                RefKind.Out => "out ",
                RefKind.In => "in ",
                _ => string.Empty,
            };
            return prefix + SafeIdentifier(p.Name);
        }));

        var constraints = FormatTypeParameterConstraints(method.TypeParameters);

        sb.Append(indent).Append("public static ").Append(returnType).Append(' ').Append(method.Name)
            .Append(methodGenericParams).Append('(').Append(parameterList).Append(')');
        if (!string.IsNullOrEmpty(constraints))
            sb.Append(' ').Append(constraints);
        sb.Append(" => ").Append(canonicalFqn).Append('.').Append(method.Name).Append(methodGenericParams)
            .Append('(').Append(argumentList).Append(");").AppendLine();
    }

    private static void EmitConstructors(StringBuilder sb, INamedTypeSymbol type, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        var emittedSig = new HashSet<string>(StringComparer.Ordinal);
        var emittedAny = false;
        foreach (var ctor in type.InstanceConstructors)
        {
            // Mirror both public and protected ctors — both are accessible
            // from a subclass in a different assembly. Skip internal/private.
            var accessor = ctor.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Protected => "protected",
                Accessibility.ProtectedOrInternal => "protected",  // collapse internal half
                _ => null,
            };
            if (accessor is null) continue;
            if (ctor.IsImplicitlyDeclared && ctor.Parameters.Length == 0)
            {
                // Implicit default — subclass's implicit default chains to it
                // automatically. Skip.
                continue;
            }

            var parameters = ctor.Parameters;
            var parameterList = string.Join(", ", parameters.Select(FormatParameter));
            var argumentList = string.Join(", ", parameters.Select(p => SafeIdentifier(p.Name)));

            var sigKey = string.Join(",", parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            if (!emittedSig.Add(sigKey))
                continue;

            sb.Append(indent).Append(accessor).Append(' ').Append(type.Name).Append('(').Append(parameterList).Append(')');
            if (parameters.Length > 0)
                sb.Append(" : base(").Append(argumentList).Append(')');
            sb.AppendLine(" { }");
            emittedAny = true;
        }
        _ = emittedAny;  // reserved for future diagnostic
    }

    // ───────────────────────────────────────────────────────────────────────
    // Formatting helpers
    // ───────────────────────────────────────────────────────────────────────

    private static bool BaseHasNestedTypeNamed(INamedTypeSymbol type)
    {
        // For nested types: check if any base of the OUTER shim type has a
        // nested type with the same name. The outer shim type's base is the
        // canonical containing type, so walking ContainingType.BaseType chain
        // works.
        var outer = type.ContainingType;
        if (outer is null) return false;
        var canonicalBase = outer;  // the canonical type the outer shim inherits from
        for (var b = canonicalBase; b is not null; b = b.BaseType)
        {
            foreach (var nested in b.GetTypeMembers())
            {
                if (nested.Name == type.Name && nested.DeclaredAccessibility == Accessibility.Public)
                    return true;
            }
        }
        return false;
    }

    private static string SwapNamespace(string original, ShimMarker marker)
    {
        if (!original.StartsWith(marker.FromPrefix, StringComparison.Ordinal))
            return original;
        return marker.ToPrefix + original.Substring(marker.FromPrefix.Length);
    }

    private static string FormatGenericParameters(INamedTypeSymbol type)
    {
        if (type.TypeParameters.IsDefaultOrEmpty || type.TypeParameters.Length == 0)
            return string.Empty;
        return "<" + string.Join(", ", type.TypeParameters.Select(p => p.Name)) + ">";
    }

    private static string FormatGenericConstraints(INamedTypeSymbol type)
        => FormatTypeParameterConstraints(type.TypeParameters);

    private static string FormatTypeParameterConstraints(ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        if (typeParameters.IsDefaultOrEmpty || typeParameters.Length == 0)
            return string.Empty;

        var clauses = new List<string>();
        foreach (var tp in typeParameters)
        {
            var parts = new List<string>();
            if (tp.HasReferenceTypeConstraint) parts.Add("class");
            if (tp.HasValueTypeConstraint) parts.Add("struct");
            if (tp.HasNotNullConstraint) parts.Add("notnull");
            if (tp.HasUnmanagedTypeConstraint) parts.Add("unmanaged");
            foreach (var ct in tp.ConstraintTypes)
                parts.Add(ct.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            if (tp.HasConstructorConstraint) parts.Add("new()");
            if (parts.Count > 0)
                clauses.Add($"where {tp.Name} : {string.Join(", ", parts)}");
        }
        return string.Join(" ", clauses);
    }

    private static string FormatCanonicalReference(INamedTypeSymbol type)
    {
        // For nested types we need to walk up the ContainingType chain so we
        // emit `global::Outer.Inner` rather than `global::<ns>.Inner`. The latter
        // is wrong because `Inner` is a nested type, not a top-level type.
        var segments = new List<string>();
        var t = type;
        while (t is not null)
        {
            segments.Insert(0, FormatTypeSegmentWithGenericArgs(t));
            if (t.ContainingType is null) break;
            t = t.ContainingType;
        }
        var ns = t!.ContainingNamespace.ToDisplayString();
        return $"global::{ns}." + string.Join(".", segments);
    }

    private static string FormatTypeSegmentWithGenericArgs(INamedTypeSymbol t)
    {
        if (t.TypeParameters.IsDefaultOrEmpty || t.TypeParameters.Length == 0)
            return t.Name;
        return $"{t.Name}<{string.Join(", ", t.TypeParameters.Select(p => p.Name))}>";
    }

    private static string FormatParameter(IParameterSymbol p)
    {
        var refKind = p.RefKind switch
        {
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            _ => string.Empty,
        };
        var paramsKeyword = p.IsParams ? "params " : string.Empty;
        var typeName = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var defaultClause = p.HasExplicitDefaultValue
            ? " = " + FormatDefaultValue(p.ExplicitDefaultValue, p.Type)
            : string.Empty;
        return $"{refKind}{paramsKeyword}{typeName} {SafeIdentifier(p.Name)}{defaultClause}";
    }

    private static string FormatDefaultValue(object? value, ITypeSymbol type)
    {
        // Type parameters (T) can't accept literal null; use `default`.
        if (type.TypeKind == TypeKind.TypeParameter)
            return "default";

        if (value is null)
            return type.IsValueType && type.NullableAnnotation != NullableAnnotation.Annotated
                ? "default"
                : "null";

        // Enums: Roslyn surfaces the value as the underlying integer. Without
        // a cast or member-name lookup, the literal isn't convertible to the
        // enum type. Cast via the fully-qualified enum type.
        if (type.TypeKind == TypeKind.Enum)
        {
            var enumFqn = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return "(" + enumFqn + ")" + (value.ToString() ?? "0");
        }

        if (value is string s)
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        if (value is bool b)
            return b ? "true" : "false";
        if (value is char c)
            return "'" + c + "'";
        return value.ToString() ?? "default";
    }

    private static string SafeIdentifier(string name)
    {
        // Add @ prefix if the parameter name collides with a C# keyword. We
        // don't enumerate all keywords; use the Roslyn parser to check.
        var kind = SyntaxFacts.GetKeywordKind(name);
        return kind == SyntaxKind.None ? name : "@" + name;
    }

    private static string HintName(INamedTypeSymbol type)
    {
        // Produce a filesystem-safe, unique hint name for AddSource.
        var raw = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var sb = new StringBuilder();
        foreach (var ch in raw)
        {
            sb.Append(ch switch
            {
                '<' or '>' or ',' or ' ' or ':' => '_',
                _ => ch,
            });
        }
        return sb.ToString();
    }

    // ───────────────────────────────────────────────────────────────────────
    // Marker attribute source (emitted via PostInit)
    // ───────────────────────────────────────────────────────────────────────

    private const string ShimAttributeSource = """
        // <auto-generated/>
        #nullable enable
        namespace Fallout.Migrate.Shims;

        /// <summary>
        /// Marks the consuming assembly as a transition-shim project. The
        /// TransitionShimGenerator walks referenced Fallout.* assemblies and emits
        /// shim types under <c>toNamespacePrefix</c> mirroring the
        /// public types whose namespace begins with <c>fromNamespacePrefix</c>.
        /// </summary>
        [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
        internal sealed class ShimAllPublicTypesUnderAttribute : System.Attribute
        {
            public ShimAllPublicTypesUnderAttribute(string fromNamespacePrefix, string toNamespacePrefix)
            {
                FromNamespacePrefix = fromNamespacePrefix;
                ToNamespacePrefix = toNamespacePrefix;
            }

            public string FromNamespacePrefix { get; }
            public string ToNamespacePrefix { get; }
        }
        """;
}
