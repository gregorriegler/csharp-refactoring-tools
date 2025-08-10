using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public sealed class MathMaxTypeInferenceStrategy : IExpressionTypeInferenceStrategy
{
    public TypeSyntax? InferType(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (!IsMathMaxInvocation(expression, semanticModel))
        {
            return null;
        }

        return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword));
    }

    private static bool IsMathMaxInvocation(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (expression is not InvocationExpressionSyntax invocation ||
            invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        // First try semantic model approach
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
        {
            return methodSymbol.Name == "Max" &&
                   methodSymbol.ContainingType?.ToDisplayString() == "System.Math";
        }

        // Fallback to string-based pattern matching when semantic model fails
        var expressionText = expression.ToString();
        return expressionText.StartsWith("Math.Max(") && expressionText.EndsWith(")");
    }
}
