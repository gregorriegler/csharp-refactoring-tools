using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public record MethodDeclaration(
    string MethodName,
    List<ParameterSyntax> Parameters,
    BlockSyntax MethodBody,
    TypeSyntax ReturnType
)
{
    public MethodDeclarationSyntax Create()
    {
        return SyntaxFactory.MethodDeclaration(ReturnType, MethodName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(Parameters)))
            .WithBody(MethodBody);
    }
}
