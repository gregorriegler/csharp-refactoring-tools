using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public abstract class AbstractTypeInferenceStrategy : ITypeInferenceStrategy
{
    public abstract string InferType(ExpressionSyntax expression, SemanticModel semanticModel);
    public abstract bool CanHandle(ExpressionSyntax expression);

    protected string GetTypeDisplayString(ITypeSymbol? type)
    {
        if (type != null && type.TypeKind != TypeKind.Error)
        {
            return type.ToDisplayString();
        }
        return "string";
    }
}
