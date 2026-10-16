using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WayFinder.DevTools.Infrastructure.Repositories;

internal static class CSharpDeclarationNames
{
    public static string? GetName(SyntaxNode node)
    {
        return node switch
        {
            BaseNamespaceDeclarationSyntax declaration =>
                declaration.Name.ToString(),

            BaseTypeDeclarationSyntax declaration =>
                declaration.Identifier.ValueText,

            MethodDeclarationSyntax declaration =>
                declaration.Identifier.ValueText,

            ConstructorDeclarationSyntax declaration =>
                declaration.Identifier.ValueText,

            PropertyDeclarationSyntax declaration =>
                declaration.Identifier.ValueText,

            VariableDeclaratorSyntax declaration =>
                declaration.Identifier.ValueText,

            EventDeclarationSyntax declaration =>
                declaration.Identifier.ValueText,

            DelegateDeclarationSyntax declaration =>
                declaration.Identifier.ValueText,

            IndexerDeclarationSyntax =>
                "this[]",

            _ => null
        };
    }

    public static string? GetQualifiedName(SyntaxNode node)
    {
        var name = GetName(node);

        if (name is null)
        {
            return null;
        }

        var parents = node
            .Ancestors()
            .OfType<MemberDeclarationSyntax>()
            .Select(GetName)
            .Where(name => name is not null)
            .Reverse();

        return string.Join(".", parents.Append(name));
    }
}
