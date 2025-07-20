using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public interface ITypeInferenceStrategy
{
    string InferType(TypeInferenceContext context);
    bool CanHandle(ExpressionSyntax expression);
}
