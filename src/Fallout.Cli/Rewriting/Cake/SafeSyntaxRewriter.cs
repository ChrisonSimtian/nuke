using System;
using Fallout.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fallout.Cli.Rewriting.Cake;

internal class SafeSyntaxRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode Visit(SyntaxNode node)
    {
        try
        {
            return base.Visit(node);
        }
        catch (Exception)
        {
            Host.Warning($"Could not handle fragment '{node.ToFullString().Trim()}', skipping ...");
            return node;
        }
    }
}
