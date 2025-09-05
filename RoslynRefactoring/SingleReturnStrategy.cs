using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public sealed class SingleReturnStrategy : ReturnValueStrategy
{
    private readonly List<StatementSyntax> selectedStatements;

    public SingleReturnStrategy(IReadOnlyList<ILocalSymbol> returns, TypeInferrer typeInferrer, SemanticModel semanticModel, List<StatementSyntax> selectedStatements)
        : base(returns, typeInferrer, semanticModel)
    {
        this.selectedStatements = selectedStatements;
    }

    public override TypeSyntax CreateReturnType()
    {
        var localReturnSymbol = returns.FirstOrDefault()!;
        if (localReturnSymbol.Type.TypeKind != TypeKind.Error)
        {
            return SyntaxFactory.ParseTypeName(localReturnSymbol.Type.ToDisplayString());
        }

        var typeFromDeclaration = InferTypeFromVariableDeclaration(localReturnSymbol.Name);
        return SyntaxFactory.ParseTypeName(typeFromDeclaration);
    }

    public override BlockSyntax AddReturnStatementToBody(BlockSyntax body)
    {
        var returnSymbol = returns.FirstOrDefault()!;
        return body.AddStatements(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(returnSymbol.Name)));
    }

    public override StatementSyntax CreateReplacementStatement(ExpressionSyntax methodCall)
    {
        var localReturnSymbol = returns.FirstOrDefault()!;
        var typeToUse = DetermineVariableType(localReturnSymbol);

        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(typeToUse))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(localReturnSymbol.Name))
                        .WithInitializer(SyntaxFactory.EqualsValueClause(methodCall)))));
    }

    private string InferTypeFromVariableDeclaration(string variableName)
    {
        foreach (var statement in selectedStatements)
        {
            if (statement is LocalDeclarationStatementSyntax localDecl)
            {
                var variable = localDecl.Declaration.Variables.FirstOrDefault(v => v.Identifier.Text == variableName);
                if (variable?.Initializer?.Value != null)
                {
                    return typeInferrer.InferType(variable.Initializer.Value, semanticModel) ?? "object";
                }
            }
        }

        return "var";
    }

    private string DetermineVariableType(ILocalSymbol localReturnSymbol)
    {
        if (localReturnSymbol.Type.TypeKind != TypeKind.Error)
        {
            return localReturnSymbol.Type.ToDisplayString();
        }

        return "var";
    }
}
