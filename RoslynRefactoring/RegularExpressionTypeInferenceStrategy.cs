using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public sealed class RegularExpressionTypeInferenceStrategy : AbstractTypeInferenceStrategy
{
    public override bool CanHandle(ExpressionSyntax expression)
    {
        return expression is not AwaitExpressionSyntax;
    }

    public override string InferType(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        var typeInfo = semanticModel.GetTypeInfo(expression);
        if (typeInfo.Type != null && typeInfo.Type.TypeKind != TypeKind.Error)
        {
            return typeInfo.Type.ToDisplayString();
        }

        var expressionText = expression.ToString();
        if (expressionText.Contains(".ToList()"))
        {
            return "List<string>";
        }

        return "object";
    }
}
