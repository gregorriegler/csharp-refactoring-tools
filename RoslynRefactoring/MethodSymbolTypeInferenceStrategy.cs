using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

internal sealed class MethodSymbolTypeInferenceStrategy : ITypeInferrer
{
    public string? InferType(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(expression);

        if (symbolInfo.Symbol is IMethodSymbol methodSymbol && methodSymbol.ReturnType != null)
        {
            var returnTypeName = methodSymbol.ReturnType.ToDisplayString();
            if (IsValidTypeName(returnTypeName))
            {
                return returnTypeName;
            }
        }

        return null;
    }

    private static bool IsValidTypeName(string typeName)
    {
        return typeName != "?" && !string.IsNullOrEmpty(typeName);
    }
}
