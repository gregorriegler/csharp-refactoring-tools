using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public abstract class AbstractTypeInferenceStrategy : ITypeInferenceStrategy
{
    public abstract string InferType(TypeInferenceContext context);
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
