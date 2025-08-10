using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public interface ITypeInferrer
{
    TypeSyntax? InferType(ExpressionSyntax expression, SemanticModel semanticModel);
}
