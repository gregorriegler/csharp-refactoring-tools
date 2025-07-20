using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public sealed class TypeInferenceService
{
    public string InferType(ExpressionSyntax expression, SemanticModel semanticModel, string? variableName = null)
    {
        var context = new TypeInferenceContext(expression, semanticModel, variableName);
        var strategy = TypeInferenceStrategyFactory.GetStrategy(expression);
        return strategy.InferType(context);
    }
}
