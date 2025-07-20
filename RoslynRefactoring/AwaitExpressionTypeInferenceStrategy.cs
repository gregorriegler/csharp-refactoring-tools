using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public sealed class AwaitExpressionTypeInferenceStrategy : AbstractTypeInferenceStrategy
{
    public override bool CanHandle(ExpressionSyntax expression)
    {
        return expression is AwaitExpressionSyntax;
    }

    public override string InferType(TypeInferenceContext context)
    {
        if (context.Expression is not AwaitExpressionSyntax awaitExpr)
        {
            throw new ArgumentException("Expression must be AwaitExpressionSyntax", nameof(context));
        }

        var typeInfo = context.SemanticModel.GetTypeInfo(awaitExpr);
        return GetTypeDisplayString(typeInfo.Type);
    }
}
