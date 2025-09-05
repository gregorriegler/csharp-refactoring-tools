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
    private readonly TypeInferrer typeInferrer;
    private readonly bool containsAwaitExpressions;
    private readonly ReturnValueStrategy returnValueStrategy;

    public StatementExtractionTarget(
        List<StatementSyntax> selectedStatements,
        BlockSyntax containingBlock,
        SemanticModel semanticModel
    ) : base(semanticModel)
    {
        this.selectedStatements = selectedStatements;
        this.containingBlock = containingBlock;
        returnBehavior = new ReturnBehavior(selectedStatements);

        var dataFlow = semanticModel.AnalyzeDataFlow(selectedStatements.First(), selectedStatements.Last());
        if (dataFlow == null)
            throw new InvalidOperationException("SemanticModel is null.");

        extractedCodeDataFlow = new ExtractedCodeDataFlow(dataFlow);
        typeInferrer = new TypeInferrer();
        containsAwaitExpressions = selectedStatements.Any(stmt =>
            stmt.DescendantNodesAndSelf().OfType<AwaitExpressionSyntax>().Any());

        var returns = extractedCodeDataFlow.GetReturns();
        returnValueStrategy = ReturnValueStrategy.Create(returns, typeInferrer, semanticModel, selectedStatements);
    }

    private TypeSyntax GetBaseReturnType()
    {
        if (returnBehavior.RequiresReturnStatement)
        {
            return (containingBlock.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault())?.ReturnType ??
                   SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
        }

        var returns = extractedCodeDataFlow.GetReturns();

        if (returns.Count == 0)
        {
            return DetermineVoidReturnType();
        }

        return DetermineLocalReturnType(returns);
    }

    protected override TypeSyntax DetermineReturnType()
    {
        var baseReturnType = GetBaseReturnType();

        if (ContainsAwaitExpressions)
        {
            return WrapInTaskType(baseReturnType);
        }

        return baseReturnType;
    }

    private bool ContainsAwaitExpressions => containsAwaitExpressions;

    private TypeSyntax WrapInTaskType(TypeSyntax baseType)
    {
        if (IsTaskType(baseType))
        {
            return baseType;
        }

        return SyntaxFactory.GenericName("Task")
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList(baseType)));
    }

    private bool IsTaskType(TypeSyntax type)
    {
        var typeString = type.ToString();
        return typeString == "Task" || typeString.StartsWith("Task<");
    }

    private TypeSyntax DetermineVoidReturnType()
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

    private TypeSyntax DetermineLocalReturnType(IReadOnlyList<ILocalSymbol> returns)
    {
        var strategy = ReturnValueStrategy.Create(returns, typeInferrer, semanticModel, selectedStatements);
        return strategy.CreateReturnType();
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

    protected override BlockSyntax CreateMethodBody()
    {
        var newMethodBody = SyntaxFactory.Block(selectedStatements);

        if (returnBehavior.RequiresReturnStatement)
        {
            var hasReturnStatement = selectedStatements
                .SelectMany(stmt => stmt.DescendantNodesAndSelf().OfType<ReturnStatementSyntax>())
                .Any();
        }

        return returnValueStrategy.AddReturnStatementToBody(newMethodBody);
    }

    protected override List<ParameterSyntax> GetParameters()
    {
        return extractedCodeDataFlow.ReadInside.Except(extractedCodeDataFlow.WrittenInside)
            .Where(s => s is ILocalSymbol or IParameterSymbol)
            .Where(s => s is not IFieldSymbol)
            .Where(s => s.Name != "this")
            .Select(CreateParameterFromSymbol)
            .ToList();
    }

    private ParameterSyntax CreateParameterFromSymbol(ISymbol symbol)
    {
        var symbolType = GetSymbolType(symbol);
        var typeDisplayString = symbolType.ToDisplayString();

        if (typeDisplayString == "var" && symbol is ILocalSymbol localSymbol)
        {
            typeDisplayString = typeInferrer.ResolveActualTypeForForeachVariable(localSymbol, containingBlock, semanticModel);
        }

        return SyntaxFactory.Parameter(SyntaxFactory.Identifier(symbol.Name))
            .WithType(SyntaxFactory.ParseTypeName(typeDisplayString));
    }


    private static ITypeSymbol GetSymbolType(ISymbol symbol)
    {
        return symbol switch
        {
            ILocalSymbol local => local.Type,
            IParameterSymbol parameter => parameter.Type,
            _ => ((ILocalSymbol)symbol).Type
        };
    }

    public override SyntaxNode CreateReplacementNode(string methodName)
    {
        var methodCall = CreateMethodCall(methodName, GetParameters());
        var awaitedCall = ContainsAwaitExpressions ? (ExpressionSyntax)SyntaxFactory.AwaitExpression(methodCall) : methodCall;

        if (returnBehavior.RequiresReturnStatement)
        {
            return SyntaxFactory.ReturnStatement(awaitedCall);
        }

        return returnValueStrategy.CreateReplacementStatement(awaitedCall);
    }

    public override void ReplaceInEditor(SyntaxEditor editor, SyntaxNode replacementNode)
    {
        editor.ReplaceNode(selectedStatements.First(), replacementNode);
        foreach (var stmt in selectedStatements.Skip(1))
            editor.RemoveNode(stmt);
    }



    public override SyntaxNode GetInsertionPoint()
    {
        var methodNode = containingBlock.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        return methodNode!;
    }

    protected override bool IsAsyncMethod()
    {
        return ContainsAwaitExpressions;
    }
}
