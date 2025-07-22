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

        return InferTypeFromExpressionText(expression);
    }

    private string GetTypeDisplayString(ITypeSymbol? type)
    {
        if (type != null && type.TypeKind != TypeKind.Error)
        {
            return type.ToDisplayString();
        }
        return "string";
    }

    private string InferTypeFromExpressionText(ExpressionSyntax expression)
    {
        var expressionText = expression.ToString();

        return expressionText switch
        {
            var text when text.Contains(".ToList()") => "List<string>",
            var text when text.Contains(".ToArray()") => "string[]",
            var text when text.Contains(".ToDictionary(") => "Dictionary<string, object>",
            var text when text.Contains(".Select(") => "IEnumerable<object>",
            var text when text.Contains(".Where(") => "IEnumerable<object>",
            _ => "object"
        };
    }
}
