using System;
using System.Linq;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Tools.MSBuild;
using Fallout.Common.Tools.NuGet;
using Fallout.Common.Tools.SignTool;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Fallout.Cli.Rewriting.Cake;

internal class ClassRewriter : SafeSyntaxRewriter
{
    private static readonly SyntaxTrivia BeginMultilineComment = SyntaxTrivia(SyntaxKind.MultiLineCommentTrivia, "/*");
    private static readonly SyntaxTrivia EndMultilineComment = SyntaxTrivia(SyntaxKind.MultiLineCommentTrivia, "*/");

    private static string[] NamespaceImports =>
        new[]
        {
            "System",
            "System.Collections.Generic",
            "System.IO",
            "System.Linq",
            "System.Linq.Expressions",
            "System.Security.Cryptography.X509Certificates",
            "System.Xml",
            "Fallout.Common",
            "Fallout.Common.Execution",
            "Fallout.Common.IO",
            "Fallout.Solutions",
            "Fallout.Common.Tooling",
            "Fallout.Common.Tools.DotNet",
            "Fallout.Common.Tools.GitVersion",
            "Fallout.Common.Tools.SignTool",
            "Fallout.Common.Utilities.Collections",
        };

    private static Type[] StaticClassImports =>
        new[]
        {
            typeof(ControlFlow),
            typeof(DotNetTasks),
            typeof(MSBuildTasks),
            typeof(SignToolTasks),
            typeof(NuGetTasks),
            typeof(TextTasks),
            typeof(XmlTasks),
            typeof(EnvironmentInfo),
        };

    private string defaultTargetFieldName;
    private string defaultTargetName;

    public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
    {
        node = (CompilationUnitSyntax)base.VisitCompilationUnit(node).NotNull();

        var defaultTargetField = node.Members.OfType<FieldDeclarationSyntax>()
            .SingleOrDefault(x => x.GetSingleDeclarator().Identifier.Text == defaultTargetFieldName);

        if (defaultTargetField != null)
        {
            var literalExpression = defaultTargetField.GetSingleDeclarator().Initializer?.Value as LiteralExpressionSyntax;
            var defaultTarget = literalExpression.GetConstantValue<string>();
            var mainMethodDeclaration =
                ParseMemberDeclaration($"public static int Main() => Execute<Build>(x => x.{defaultTarget});");

            node = node.WithMembers(List(new[]
            {
                mainMethodDeclaration
            }.Concat(node.Members.Except(new[]
            {
                defaultTargetField
            }))));
        }
        else if (defaultTargetName != null)
        {
            var mainMethodDeclaration =
                ParseMemberDeclaration($"public static int Main() => Execute<Build>(x => x.{defaultTargetName});");

            node = node.WithMembers(List(new[]
            {
                mainMethodDeclaration
            }.Concat(node.Members)));
        }

        var baseTypes = SeparatedList(new BaseTypeSyntax[]
        {
            SimpleBaseType(ParseTypeName(nameof(FalloutBuild)))
        });

        var classDeclaration = ClassDeclaration("Build")
            .WithBaseList(BaseList(baseTypes))
            .WithMembers(List(node.Members.Where(x => x != defaultTargetField)));

        var staticTypes = StaticClassImports;
        var staticUsings = staticTypes
            .Select(x => x.FullName)
            .Select(IdentifierName)
            .Select(UsingDirective)
            .Select(x => x.WithStaticKeyword(Token(SyntaxKind.StaticKeyword)));

        var namespaceUsings = NamespaceImports
            .Concat(staticTypes.Select(x => x.Namespace))
            .Select(IdentifierName)
            .Select(UsingDirective);

        return CompilationUnit()
            .WithUsings(List(namespaceUsings.Concat(staticUsings)))
            .WithMembers(List(new MemberDeclarationSyntax[]
            {
                classDeclaration
            }));
    }

    public override SyntaxNode VisitGlobalStatement(GlobalStatementSyntax node)
    {
        node = (GlobalStatementSyntax)base.VisitGlobalStatement(node).NotNull();

        if (node.Statement is not ExpressionStatementSyntax expressionStatement)
        {
            return node
                .WithLeadingTrivia(BeginMultilineComment)
                .WithTrailingTrivia(EndMultilineComment);
        }

        var invocationExpression = expressionStatement.Expression as InvocationExpressionSyntax;
        var identifierName = invocationExpression?.GetIdentifierName();
        if (identifierName == "Setup" ||
            identifierName == "Teardown")
        {
            return node
                .WithLeadingTrivia(BeginMultilineComment)
                .WithTrailingTrivia(EndMultilineComment);
        }

        if (identifierName == "RunTarget")
        {
            var expression = invocationExpression.GetSingleArgument<ExpressionSyntax>();
            if (expression is IdentifierNameSyntax targetIdentifier)
            {
                defaultTargetFieldName = targetIdentifier.Identifier.Text;
            }

            if (expression is LiteralExpressionSyntax literalExpression)
            {
                defaultTargetName = literalExpression.GetConstantValue<string>();
            }

            return null;
        }

        return node;
    }
}
