using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public sealed class AwaitExpressionTypeInferenceStrategy : AbstractTypeInferenceStrategy
{
    public override bool CanHandle(ExpressionSyntax expression)
    {
        return expression is AwaitExpressionSyntax;
    }

    public override string InferType(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (expression is not AwaitExpressionSyntax awaitExpr)
        {
            throw new ArgumentException("Expression must be AwaitExpressionSyntax", nameof(expression));
        }

        var typeInfo = semanticModel.GetTypeInfo(awaitExpr);
        return GetTypeDisplayString(typeInfo.Type);
    }
}
