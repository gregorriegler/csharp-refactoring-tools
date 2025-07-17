using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace RoslynRefactoring;

public class StatementExtractionTarget : ExtractionTarget
{
    private readonly List<StatementSyntax> selectedStatements;
    private readonly BlockSyntax containingBlock;
    private readonly ReturnBehavior returnBehavior;
    private List<ILocalSymbol>? cachedReturns;
    private SemanticModel? cachedModel;


    public StatementExtractionTarget(List<StatementSyntax> selectedStatements, BlockSyntax containingBlock)
    {
        this.selectedStatements = selectedStatements;
        this.containingBlock = containingBlock;
        this.returnBehavior = new ReturnBehavior(selectedStatements);
    }

    public virtual SyntaxNode GetSelectedNode()
    {
        return selectedStatements.First();
    }

    public override DataFlowAnalysis AnalyzeDataFlow(SemanticModel model)
    {
        var dataFlow = model?.AnalyzeDataFlow(selectedStatements.First(), selectedStatements.Last());
        if (dataFlow == null)
            throw new InvalidOperationException("DataFlow is null.");
        return dataFlow;
    }

    public override TypeSyntax DetermineReturnType(SemanticModel model, DataFlowAnalysis dataFlow)
    {
        if (returnBehavior.RequiresReturnStatement)
        {
            var containingMethod = containingBlock.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            return containingMethod?.ReturnType ??
                   SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
        }

        var returns = dataFlow.DataFlowsOut.Intersect(dataFlow.WrittenInside, SymbolEqualityComparer.Default)
            .OfType<ILocalSymbol>()
            .ToList();

        if (returns.Count == 0)
        {
            if (selectedStatements.Count == 1 &&
                selectedStatements.First() is LocalDeclarationStatementSyntax localDecl)
            {
                var variable = localDecl.Declaration.Variables.FirstOrDefault();
                if (variable?.Initializer?.Value != null)
                {
                    var typeInfo = model.GetTypeInfo(variable.Initializer.Value);
                    if (typeInfo.Type != null)
                    {
                        return SyntaxFactory.ParseTypeName(typeInfo.Type.ToDisplayString());
                    }
                }
            }
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
        }

        if (returns.FirstOrDefault() is { } localReturnSymbol)
        {
            return SyntaxFactory.ParseTypeName(localReturnSymbol.Type.ToDisplayString());
        }

        throw new InvalidOperationException("Unsupported return symbol type.");
    }

    public override BlockSyntax CreateMethodBody()
    {
        if (cachedReturns == null || cachedModel == null)
        {
            return SyntaxFactory.Block(selectedStatements);
        }

        if (cachedReturns.Count == 0)
        {
            var newMethodBody = SyntaxFactory.Block(selectedStatements);
            if (selectedStatements.Count == 1 &&
                selectedStatements.First() is LocalDeclarationStatementSyntax localDecl)
            {
                var variable = localDecl.Declaration.Variables.FirstOrDefault();
                if (variable != null)
                {
                    return newMethodBody.AddStatements(
                        SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(variable.Identifier.Text)));
                }
            }
            return newMethodBody;
        }

        if (cachedReturns.FirstOrDefault() is { } returnSymbol)
        {
            return SyntaxFactory.Block(selectedStatements).AddStatements(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(returnSymbol.Name)));
        }

        return SyntaxFactory.Block(selectedStatements);
    }

    public override void ReplaceInEditor(SyntaxEditor editor, InvocationExpressionSyntax methodCall, SemanticModel model, List<ILocalSymbol> returns)
    {
        cachedReturns = returns;
        cachedModel = model;
        var callStatement = StatementSyntax(methodCall, returns);

        editor.ReplaceNode(selectedStatements.First(), callStatement);
        foreach (var stmt in selectedStatements.Skip(1))
            editor.RemoveNode(stmt);
    }


    private StatementSyntax StatementSyntax(InvocationExpressionSyntax methodCall, List<ILocalSymbol> returns)
    {
        if (returnBehavior.RequiresReturnStatement)
        {
            return SyntaxFactory.ReturnStatement(methodCall);
        }
        if (returns.Count == 0)
        {
            return GetCallStatement(methodCall);
        }
        if (returns.FirstOrDefault() is { } localReturnSymbol)
        {
            return CreateLocalReturnStatement(methodCall, localReturnSymbol);
        }
        throw new InvalidOperationException("Unsupported return symbol type.");
    }

    private StatementSyntax GetCallStatement(InvocationExpressionSyntax methodCall)
    {
        if (selectedStatements.Count != 1 ||
            selectedStatements.First() is not LocalDeclarationStatementSyntax localDecl)
        {
            return SyntaxFactory.ExpressionStatement(methodCall);
        }

        var variable = localDecl.Declaration.Variables.FirstOrDefault();
        if (variable == null)
        {
            return SyntaxFactory.ExpressionStatement(methodCall);
        }

        var variableType = localDecl.Declaration.Type;

        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(variableType)
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(variable.Identifier.Text))
                        .WithInitializer(SyntaxFactory.EqualsValueClause(methodCall)))));
    }

    private StatementSyntax CreateLocalReturnStatement(InvocationExpressionSyntax methodCall, ILocalSymbol localReturnSymbol)
    {
        if (selectedStatements.Count == 1 && selectedStatements.First() is ReturnStatementSyntax)
        {
            return SyntaxFactory.ReturnStatement(methodCall);
        }

        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(localReturnSymbol.Type.ToDisplayString()))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(localReturnSymbol.Name))
                        .WithInitializer(SyntaxFactory.EqualsValueClause(methodCall)))));
    }

    public override SyntaxNode GetInsertionPoint()
    {
        var methodNode = containingBlock.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodNode != null)
        {
            return methodNode;
        }

        return selectedStatements.Last();
    }

}
