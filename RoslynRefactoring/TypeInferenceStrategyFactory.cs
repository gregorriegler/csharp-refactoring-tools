using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public static class TypeInferenceStrategyFactory
{
    private static readonly ITypeInferenceStrategy[] strategies =
    [
        new AwaitExpressionTypeInferenceStrategy(),
        new RegularExpressionTypeInferenceStrategy()
    ];

    public static ITypeInferenceStrategy GetStrategy(ExpressionSyntax expression)
    {
        foreach (var strategy in strategies)
        {
            if (strategy.CanHandle(expression))
            {
                return strategy;
            }
        }

        throw new InvalidOperationException($"No strategy found for expression type: {expression.GetType().Name}");
    }
}
