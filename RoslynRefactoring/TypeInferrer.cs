using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public sealed class TypeInferrer
{
    public string InferType(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (expression is AwaitExpressionSyntax awaitExpr)
        {
            var typeInfo = semanticModel.GetTypeInfo(awaitExpr);
            return GetTypeDisplayString(typeInfo.Type);
        }

        var regularTypeInfo = semanticModel.GetTypeInfo(expression);
        if (regularTypeInfo.Type != null && regularTypeInfo.Type.TypeKind != TypeKind.Error)
        {
            return regularTypeInfo.Type.ToDisplayString();
        }

        return InferTypeFromStringPattern(expression);
    }

    private string GetTypeDisplayString(ITypeSymbol? type)
    {
        if (type != null && type.TypeKind != TypeKind.Error)
        {
            return type.ToDisplayString();
        }
        return "string";
    }

    private string InferTypeFromStringPattern(ExpressionSyntax expression)
    {
        var expressionText = expression.ToString();
        if (expressionText.Contains(".ToList()"))
        {
            return "List<string>";
        }

        return "object";
    }
}
