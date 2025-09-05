using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public sealed class VoidReturnStrategy : ReturnValueStrategy
{
    private readonly List<StatementSyntax> selectedStatements;

    public VoidReturnStrategy(IReadOnlyList<ILocalSymbol> returns, TypeInferrer typeInferrer, SemanticModel semanticModel, List<StatementSyntax> selectedStatements)
        : base(returns, typeInferrer, semanticModel)
    {
        this.selectedStatements = selectedStatements;
    }

    public override TypeSyntax CreateReturnType()
    {
        var lastStatement = selectedStatements.Last();
        if (lastStatement is not LocalDeclarationStatementSyntax lastLocalDecl)
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
        var variable = lastLocalDecl.Declaration.Variables.FirstOrDefault();
        if (variable?.Initializer?.Value == null)
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
        var inferredType = typeInferrer.InferType(variable.Initializer.Value, semanticModel);
        return SyntaxFactory.ParseTypeName(inferredType ?? "object");
    }

    public override BlockSyntax AddReturnStatementToBody(BlockSyntax body)
    {
        var lastStatement = selectedStatements.Last();
        if (lastStatement is LocalDeclarationStatementSyntax lastLocalDecl)
        {
            var variable = lastLocalDecl.Declaration.Variables.FirstOrDefault();
            if (variable != null)
            {
                return body.AddStatements(
                    SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(variable.Identifier.Text)));
            }
        }

        return body;
    }

    public override StatementSyntax CreateReplacementStatement(ExpressionSyntax methodCall)
    {
        var lastStatement = selectedStatements.Last();
        if (lastStatement is LocalDeclarationStatementSyntax lastLocalDecl)
        {
            var variable = lastLocalDecl.Declaration.Variables.FirstOrDefault();
            if (variable != null)
            {
                return SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                        .WithVariables(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(variable.Identifier.Text))
                                .WithInitializer(SyntaxFactory.EqualsValueClause(methodCall)))));
            }
        }

        return SyntaxFactory.ExpressionStatement(methodCall);
    }
}
