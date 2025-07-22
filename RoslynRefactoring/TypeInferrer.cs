using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public sealed class TypeInferrer
{
    private const string ToListPattern = ".ToList()";
    private const string ToArrayPattern = ".ToArray()";
    private const string ToDictionaryPattern = ".ToDictionary(";
    private const string SelectPattern = ".Select(";
    private const string WherePattern = ".Where(";
    private const string ListStringType = "List<string>";
    private const string StringArrayType = "string[]";
    private const string DictionaryType = "Dictionary<string, object>";
    private const string EnumerableType = "IEnumerable<object>";
    private const string StringType = "string";
    private const string ObjectType = "object";

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
        return StringType;
    }

    private string InferTypeFromExpressionText(ExpressionSyntax expression)
    {
        var expressionText = expression.ToString();

        return expressionText switch
        {
            var text when text.Contains(ToListPattern) => ListStringType,
            var text when text.Contains(ToArrayPattern) => StringArrayType,
            var text when text.Contains(ToDictionaryPattern) => DictionaryType,
            var text when text.Contains(SelectPattern) => EnumerableType,
            var text when text.Contains(WherePattern) => EnumerableType,
            _ => ObjectType
        };
    }
}
