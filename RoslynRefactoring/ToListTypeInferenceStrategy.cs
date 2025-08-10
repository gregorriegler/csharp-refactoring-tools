using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public sealed class ToListTypeInferenceStrategy : ITypeInferrer
{
    public TypeSyntax? InferType(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (!IsToListInvocation(expression, semanticModel))
        {
            return null;
        }

        if (expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var collectionExpression = memberAccess.Expression;
            var collectionTypeInfo = semanticModel.GetTypeInfo(collectionExpression);

            if (collectionTypeInfo.Type is IArrayTypeSymbol arrayType)
            {
                var elementType = arrayType.ElementType.ToDisplayString();
                return SyntaxFactory.ParseTypeName($"List<{elementType}>");
            }

            if (collectionTypeInfo.Type is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
            {
                var elementType = namedType.TypeArguments[0].ToDisplayString();
                return SyntaxFactory.ParseTypeName($"List<{elementType}>");
            }

            // If the collection type is object, return List<object>
            if (collectionTypeInfo.Type?.SpecialType == SpecialType.System_Object)
            {
                return SyntaxFactory.ParseTypeName("List<object>");
            }
        }

        return SyntaxFactory.ParseTypeName("List<string>");
    }

    private static bool IsToListInvocation(ExpressionSyntax expression, SemanticModel semanticModel)
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
            return methodSymbol.Name == "ToList" &&
                   methodSymbol.ContainingType?.ToDisplayString() == "System.Linq.Enumerable";
        }

        // Fallback to string-based pattern matching when semantic model fails
        var expressionText = expression.ToString();
        return expressionText.EndsWith(".ToList()");
    }
}
