using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public interface ITypeInferrer
{
    string? InferType(ExpressionSyntax expression, SemanticModel semanticModel);
}
