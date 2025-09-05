using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public sealed class TupleReturnStrategy : ReturnValueStrategy
{
    private readonly List<StatementSyntax> selectedStatements;

    public TupleReturnStrategy(IReadOnlyList<ILocalSymbol> returns, TypeInferrer typeInferrer, SemanticModel semanticModel, List<StatementSyntax> selectedStatements)
        : base(returns, typeInferrer, semanticModel)
    {
        this.selectedStatements = selectedStatements;
    }

    public override TypeSyntax CreateReturnType()
    {
        var tupleElements = returns.Select(symbol =>
        {
            var typeName = symbol.Type.TypeKind != TypeKind.Error
                ? symbol.Type.ToDisplayString()
                : InferTypeFromVariableDeclaration(symbol.Name);
            return SyntaxFactory.TupleElement(SyntaxFactory.ParseTypeName(typeName));
        });

        return SyntaxFactory.TupleType(SyntaxFactory.SeparatedList(tupleElements));
    }

    public override BlockSyntax AddReturnStatementToBody(BlockSyntax body)
    {
        var tupleElements = returns.Select(symbol =>
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(symbol.Name)));
        var tupleExpression = SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(tupleElements));
        return body.AddStatements(SyntaxFactory.ReturnStatement(tupleExpression));
    }

    public override StatementSyntax CreateReplacementStatement(ExpressionSyntax methodCall)
    {
        var variableNames = string.Join(", ", returns.Select(r => r.Name));
        var tuplePattern = $"({variableNames})";

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.ParseExpression(tuplePattern),
                methodCall));
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

        return "object";
    }
}
