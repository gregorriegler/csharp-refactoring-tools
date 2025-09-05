using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public abstract class ReturnValueStrategy
{
    protected readonly IReadOnlyList<ILocalSymbol> returns;
    protected readonly TypeInferrer typeInferrer;
    protected readonly SemanticModel semanticModel;

    protected ReturnValueStrategy(IReadOnlyList<ILocalSymbol> returns, TypeInferrer typeInferrer, SemanticModel semanticModel)
    {
        this.returns = returns;
        this.typeInferrer = typeInferrer;
        this.semanticModel = semanticModel;
    }

    public static ReturnValueStrategy Create(IReadOnlyList<ILocalSymbol> returns, TypeInferrer typeInferrer, SemanticModel semanticModel, List<StatementSyntax> selectedStatements)
    {
        return returns.Count switch
        {
            0 => new VoidReturnStrategy(returns, typeInferrer, semanticModel, selectedStatements),
            1 => new SingleReturnStrategy(returns, typeInferrer, semanticModel, selectedStatements),
            _ => new TupleReturnStrategy(returns, typeInferrer, semanticModel, selectedStatements)
        };
    }

    public abstract TypeSyntax CreateReturnType();
    public abstract BlockSyntax AddReturnStatementToBody(BlockSyntax body);
    public abstract StatementSyntax CreateReplacementStatement(ExpressionSyntax methodCall);
}
