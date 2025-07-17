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
    private BlockSyntax? modifiedMethodBody;
    private TypeSyntax? modifiedReturnType;

    public BlockSyntax? ModifiedMethodBody => modifiedMethodBody;
    public TypeSyntax? ModifiedReturnType => modifiedReturnType;

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
        if (modifiedReturnType != null) return modifiedReturnType;
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
        return ModifiedMethodBody ?? SyntaxFactory.Block(selectedStatements);
    }

    public override void ReplaceInEditor(SyntaxEditor editor, InvocationExpressionSyntax methodCall,
        SemanticModel model, List<ILocalSymbol> returns)
    {
        var newMethodBody = SyntaxFactory.Block(selectedStatements);
        StatementSyntax callStatement;

        if (returnBehavior.RequiresReturnStatement)
        {
            callStatement = SyntaxFactory.ReturnStatement(methodCall);
        }
        else if (returns.Count == 0)
        {
            callStatement = HandleNoReturnsCase(methodCall, model, newMethodBody);
        }
        else if (returns.FirstOrDefault() is { } localReturnSymbol)
        {
            callStatement = HandleLocalReturnCase(methodCall, localReturnSymbol, newMethodBody);
        }
        else
        {
            throw new InvalidOperationException("Unsupported return symbol type.");
        }

        editor.ReplaceNode(selectedStatements.First(), callStatement);
        foreach (var stmt in selectedStatements.Skip(1))
            editor.RemoveNode(stmt);
    }

    private StatementSyntax HandleNoReturnsCase(InvocationExpressionSyntax methodCall, SemanticModel model,
        BlockSyntax newMethodBody)
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

        var variableName = variable.Identifier.Text;
        var variableType = localDecl.Declaration.Type;

        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(variableName));
        modifiedMethodBody = newMethodBody.AddStatements(returnStatement);

        if (variable.Initializer?.Value == null)
            return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(variableType)
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(variableName))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(methodCall)))));
        var typeInfo = model.GetTypeInfo(variable.Initializer.Value);
        if (typeInfo.Type != null)
        {
            modifiedReturnType = SyntaxFactory.ParseTypeName(typeInfo.Type.ToDisplayString());
        }

        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(variableType)
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(variableName))
                        .WithInitializer(SyntaxFactory.EqualsValueClause(methodCall)))));
    }

    private StatementSyntax HandleLocalReturnCase(InvocationExpressionSyntax methodCall, ILocalSymbol localReturnSymbol,
        BlockSyntax newMethodBody)
    {
        var returnType = SyntaxFactory.ParseTypeName(localReturnSymbol.Type.ToDisplayString());
        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(localReturnSymbol.Name));

        modifiedMethodBody = newMethodBody.AddStatements(returnStatement);

        if (selectedStatements.Count == 1 && selectedStatements.First() is ReturnStatementSyntax)
        {
            return SyntaxFactory.ReturnStatement(methodCall);
        }

        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(returnType)
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
