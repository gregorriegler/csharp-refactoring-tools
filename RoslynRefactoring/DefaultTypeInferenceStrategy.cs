using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public sealed class DefaultTypeInferenceStrategy : IExpressionTypeInferenceStrategy
{
    public TypeSyntax? InferType(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));
    }
}
