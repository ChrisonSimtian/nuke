using System.Collections.Generic;
using System.Linq;
using Fallout.Common.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Fallout.Cli.Rewriting.Cake;

internal class RenameFieldIdentifierRewriter : SafeSyntaxRewriter
{
    private readonly Dictionary<string, string> renames = new();

    public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        if (node.Parent is not CompilationUnitSyntax or ClassDeclarationSyntax)
        {
            return node;
        }

        string CreateRename(string name)
            => renames[name] = name.Capitalize().ReplaceKnownWords();

        var renamedVariables = node.Declaration.Variables
            .Select(x => x.WithIdentifier(Identifier(CreateRename(x.Identifier.Text))));

        return node.WithDeclaration(
            node.Declaration.WithVariables(
                SeparatedList(renamedVariables)));
    }

    public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
    {
        return renames.TryGetValue(node.Identifier.Text, out var rename)
            ? IdentifierName(rename)
            : node;
    }
}
