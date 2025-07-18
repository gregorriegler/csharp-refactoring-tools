using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace RoslynRefactoring;

public sealed class StatementExtractionTarget : ExtractionTarget
{
    private readonly List<StatementSyntax> selectedStatements;
    private readonly BlockSyntax containingBlock;
    private readonly ReturnBehavior returnBehavior;
    private readonly ExtractedCodeDataFlow extractedCodeDataFlow;

    public StatementExtractionTarget(
        List<StatementSyntax> selectedStatements,
        BlockSyntax containingBlock,
        SemanticModel semanticModel
    ) : base(semanticModel)
    {
        this.selectedStatements = selectedStatements;
        this.containingBlock = containingBlock;
        returnBehavior = new ReturnBehavior(selectedStatements);
        extractedCodeDataFlow = new ExtractedCodeDataFlow(
            semanticModel.AnalyzeDataFlow(selectedStatements.First(), selectedStatements.Last())
            ?? throw new InvalidOperationException("DataFlow is null."));
    }

    protected override TypeSyntax DetermineReturnType()
    {
        if (returnBehavior.RequiresReturnStatement)
        {
            return (containingBlock.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault())?.ReturnType ??
                   SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
        }

        var returns = extractedCodeDataFlow.GetReturns();

        if (returns.Count == 0)
        {
            if (selectedStatements.Count != 1 ||
                selectedStatements.First() is not LocalDeclarationStatementSyntax localDecl)
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
            var variable = localDecl.Declaration.Variables.FirstOrDefault();
            if (variable?.Initializer?.Value == null)
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
            var typeInfo = semanticModel.GetTypeInfo(variable.Initializer.Value);
            return typeInfo.Type != null
                ? SyntaxFactory.ParseTypeName(typeInfo.Type.ToDisplayString())
                : SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
        }

        if (returns.FirstOrDefault() is { } localReturnSymbol)
        {
            return SyntaxFactory.ParseTypeName(localReturnSymbol.Type.ToDisplayString());
        }

        throw new InvalidOperationException("Unsupported return symbol type.");
    }

    protected override BlockSyntax CreateMethodBody()
    {
        var returns = extractedCodeDataFlow.GetReturns();
        if (returns.Count != 0)
        {
            return returns.FirstOrDefault() is { } returnSymbol
                ? SyntaxFactory.Block(selectedStatements)
                    .AddStatements(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(returnSymbol.Name)))
                : SyntaxFactory.Block(selectedStatements);
        }

        var newMethodBody = SyntaxFactory.Block(selectedStatements);
        if (selectedStatements.Count != 1 ||
            selectedStatements.First() is not LocalDeclarationStatementSyntax localDecl) return newMethodBody;

        var variable = localDecl.Declaration.Variables.FirstOrDefault();
        if (variable != null)
        {
            return newMethodBody.AddStatements(
                SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(variable.Identifier.Text)));
        }

        return newMethodBody;
    }

    protected override List<ParameterSyntax> GetParameters()
    {
        return extractedCodeDataFlow.ReadInside.Except(extractedCodeDataFlow.WrittenInside)
            .Where(s => s is ILocalSymbol or IParameterSymbol)
            .Where(s => s is not IFieldSymbol)
            .Where(s => s.Name != "this")
            .Select(s => SyntaxFactory.Parameter(SyntaxFactory.Identifier(s.Name))
                .WithType(SyntaxFactory.ParseTypeName(GetSymbolType(s).ToDisplayString()))).ToList();
    }

    private static ITypeSymbol GetSymbolType(ISymbol symbol)
    {
        return symbol switch
        {
            ILocalSymbol local => local.Type,
            IParameterSymbol parameter => parameter.Type,
            _ => throw new InvalidOperationException($"Unsupported symbol type: {symbol.GetType()}")
        };
    }

    public override SyntaxNode CreateReplacementNode(string methodName)
    {
        if (returnBehavior.RequiresReturnStatement)
        {
            var methodCall = CreateMethodCall(methodName, GetParameters());
            return SyntaxFactory.ReturnStatement(methodCall);
        }

        var returns = extractedCodeDataFlow.GetReturns();
        if (returns.Count == 0)
        {
            var methodCall = CreateMethodCall(methodName, GetParameters());
            return GetCallStatement(methodCall);
        }

        if (returns.FirstOrDefault() is { } localReturnSymbol)
        {
            var methodCall = CreateMethodCall(methodName, GetParameters());
            return CreateLocalReturnStatement(methodCall, localReturnSymbol);
        }

        throw new InvalidOperationException("Unsupported return symbol type.");
    }

    public override void ReplaceInEditor(SyntaxEditor editor, SyntaxNode replacementNode)
    {
        editor.ReplaceNode(selectedStatements.First(), replacementNode);
        foreach (var stmt in selectedStatements.Skip(1))
            editor.RemoveNode(stmt);
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

    private StatementSyntax CreateLocalReturnStatement(InvocationExpressionSyntax methodCall,
        ILocalSymbol localReturnSymbol)
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
