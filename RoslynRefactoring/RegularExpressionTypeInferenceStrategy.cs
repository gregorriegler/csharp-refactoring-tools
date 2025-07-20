using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public sealed class RegularExpressionTypeInferenceStrategy : AbstractTypeInferenceStrategy
{
    public override bool CanHandle(ExpressionSyntax expression)
    {
        return expression is not AwaitExpressionSyntax;
    }

    public override string InferType(TypeInferenceContext context)
    {
        var typeInfo = context.SemanticModel.GetTypeInfo(context.Expression);
        if (typeInfo.Type != null && typeInfo.Type.TypeKind != TypeKind.Error)
        {
            return typeInfo.Type.ToDisplayString();
        }

        var expressionText = context.Expression.ToString();
        if (expressionText.Contains(".ToList()"))
        {
            return "List<string>";
        }

        return "object";
    }
}
