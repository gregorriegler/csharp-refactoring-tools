using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

internal sealed class DefaultTypeInferenceStrategy : ITypeInferrer
{
    public string? InferType(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        return "object";
    }
}
