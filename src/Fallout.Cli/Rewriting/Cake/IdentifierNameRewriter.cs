using System.Collections.Generic;
using Fallout.Common;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Tools.MSBuild;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fallout.Cli.Rewriting.Cake;

internal class IdentifierNameRewriter : SafeSyntaxRewriter
{
    private static Dictionary<string, string> Replacements =>
        new()
        {
            ["DotNetCoreVerbosity"] = nameof(DotNetVerbosity),
            ["MSBuildToolVersion"] = nameof(MSBuildToolsVersion),
            ["PlatformTarget"] = nameof(MSBuildTargetPlatform),
            ["IsRunningOnUnix"] = nameof(EnvironmentInfo.IsUnix),
            ["IsRunningOnWindows"] = nameof(EnvironmentInfo.IsWin),
            ["EnvironmentVariable"] = "GetVariable<string>",
        };

    public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
    {
        return Replacements.TryGetValue(node.Identifier.Text, out var replacement)
            ? node.WithIdentifier(SyntaxFactory.Identifier(replacement))
            : node;
    }
}
