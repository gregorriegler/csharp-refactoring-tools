using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public interface IExpressionTypeInferenceStrategy
{
    TypeSyntax? InferType(ExpressionSyntax expression, SemanticModel semanticModel);
}
