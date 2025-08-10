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

        return SyntaxFactory.ParseTypeName("int");
    }

    private static bool IsMathMaxInvocation(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (expression is not InvocationExpressionSyntax invocation ||
            invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        return methodSymbol.Name == "Max" &&
               methodSymbol.ContainingType?.ToDisplayString() == "System.Math";
    }
}
