using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public interface ITypeInferenceStrategy
{
    string InferType(ExpressionSyntax expression, SemanticModel semanticModel);
    bool CanHandle(ExpressionSyntax expression);
}
