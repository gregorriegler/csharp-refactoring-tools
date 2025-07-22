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
        typeInferrer = new TypeInferrer();
        containsAwaitExpressions = selectedStatements.Any(stmt =>
            stmt.DescendantNodesAndSelf().OfType<AwaitExpressionSyntax>().Any());
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
        if (baseType.IsKind(SyntaxKind.PredefinedType) &&
            ((PredefinedTypeSyntax)baseType).Keyword.IsKind(SyntaxKind.VoidKeyword))
        {
            return SyntaxFactory.ParseTypeName("Task");
        }

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
            return !lastLocalDecl.Declaration.Type.IsVar
                ? lastLocalDecl.Declaration.Type
                : SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
        var inferredType = typeInferrer.InferType(variable.Initializer.Value, semanticModel);
        return SyntaxFactory.ParseTypeName(inferredType);

    }

    private TypeSyntax DetermineLocalReturnType(IReadOnlyList<ILocalSymbol> returns)
    {
        if (returns.Count > 1)
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

        if (returns.FirstOrDefault() is { } localReturnSymbol)
        {
            if (localReturnSymbol.Type.TypeKind != TypeKind.Error)
            {
                return SyntaxFactory.ParseTypeName(localReturnSymbol.Type.ToDisplayString());
            }

            var typeFromDeclaration = InferTypeFromVariableDeclaration(localReturnSymbol.Name);
            return SyntaxFactory.ParseTypeName(typeFromDeclaration);
        }

        throw new InvalidOperationException("Unsupported return symbol type.");
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
                    return typeInferrer.InferType(variable.Initializer.Value, semanticModel);
                }
            }
        }

        return "object";
    }

    protected override BlockSyntax CreateMethodBody()
    {
        var returns = extractedCodeDataFlow.GetReturns();
        if (returns.Count > 1)
        {
            var tupleElements = returns.Select(symbol =>
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(symbol.Name)));
            var tupleExpression = SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(tupleElements));
            return SyntaxFactory.Block(selectedStatements)
                .AddStatements(SyntaxFactory.ReturnStatement(tupleExpression));
        }

        if (returns.Count == 1)
        {
            return returns.FirstOrDefault() is { } returnSymbol
                ? SyntaxFactory.Block(selectedStatements)
                    .AddStatements(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(returnSymbol.Name)))
                : SyntaxFactory.Block(selectedStatements);
        }

        var newMethodBody = SyntaxFactory.Block(selectedStatements);

        if (returnBehavior.RequiresReturnStatement)
        {
            var hasReturnStatement = selectedStatements
                .SelectMany(stmt => stmt.DescendantNodesAndSelf().OfType<ReturnStatementSyntax>())
                .Any();

            if (!hasReturnStatement)
            {
                var returnType = DetermineReturnType();
                if (returnType.IsKind(SyntaxKind.PredefinedType) &&
                    ((PredefinedTypeSyntax)returnType).Keyword.IsKind(SyntaxKind.BoolKeyword))
                {
                    return newMethodBody.AddStatements(
                        SyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)));
                }
            }
        }

        var lastStatement = selectedStatements.Last();
        if (lastStatement is LocalDeclarationStatementSyntax lastLocalDecl)
        {
            var variable = lastLocalDecl.Declaration.Variables.FirstOrDefault();
            if (variable != null)
            {
                return newMethodBody.AddStatements(
                    SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(variable.Identifier.Text)));
            }
        }

        return newMethodBody;
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
            _ => throw new InvalidOperationException($"Unsupported symbol type: {symbol.GetType()}")
        };
    }

    public override SyntaxNode CreateReplacementNode(string methodName)
    {
        if (returnBehavior.RequiresReturnStatement)
        {
            var methodCall = CreateMethodCall(methodName, GetParameters());
            var awaitedCall = ContainsAwaitExpressions ? (ExpressionSyntax)SyntaxFactory.AwaitExpression(methodCall) : methodCall;
            return SyntaxFactory.ReturnStatement(awaitedCall);
        }

        var returns = extractedCodeDataFlow.GetReturns();
        if (returns.Count == 0)
        {
            var methodCall = CreateMethodCall(methodName, GetParameters());
            var awaitedCall = ContainsAwaitExpressions ? (ExpressionSyntax)SyntaxFactory.AwaitExpression(methodCall) : methodCall;
            return GetCallStatement(awaitedCall);
        }

        if (returns.Count > 1)
        {
            var methodCall = CreateMethodCall(methodName, GetParameters());
            var awaitedCall = ContainsAwaitExpressions ? (ExpressionSyntax)SyntaxFactory.AwaitExpression(methodCall) : methodCall;
            return CreateTupleDestructuringStatement(awaitedCall, returns);
        }

        if (returns.FirstOrDefault() is { } localReturnSymbol)
        {
            var methodCall = CreateMethodCall(methodName, GetParameters());
            var awaitedCall = ContainsAwaitExpressions ? (ExpressionSyntax)SyntaxFactory.AwaitExpression(methodCall) : methodCall;
            return CreateLocalReturnStatement(awaitedCall, localReturnSymbol);
        }

        throw new InvalidOperationException("Unsupported return symbol type.");
    }

    public override void ReplaceInEditor(SyntaxEditor editor, SyntaxNode replacementNode)
    {
        editor.ReplaceNode(selectedStatements.First(), replacementNode);
        foreach (var stmt in selectedStatements.Skip(1))
            editor.RemoveNode(stmt);
    }


    private StatementSyntax GetCallStatement(ExpressionSyntax methodCall)
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

    private StatementSyntax CreateLocalReturnStatement(ExpressionSyntax methodCall,
        ILocalSymbol localReturnSymbol)
    {
        if (selectedStatements.Count == 1 && selectedStatements.First() is ReturnStatementSyntax)
        {
            return SyntaxFactory.ReturnStatement(methodCall);
        }

        var typeToUse = DetermineVariableType(localReturnSymbol);

        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(typeToUse))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(localReturnSymbol.Name))
                        .WithInitializer(SyntaxFactory.EqualsValueClause(methodCall)))));
    }

    private string DetermineVariableType(ILocalSymbol localReturnSymbol)
    {
        if (localReturnSymbol.Type.TypeKind != TypeKind.Error)
        {
            return localReturnSymbol.Type.ToDisplayString();
        }

        return "var";
    }

    private StatementSyntax CreateTupleDestructuringStatement(ExpressionSyntax methodCall, IReadOnlyList<ILocalSymbol> returns)
    {
        var variableNames = string.Join(", ", returns.Select(r => r.Name));
        var tuplePattern = $"({variableNames})";

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.ParseExpression(tuplePattern),
                methodCall));
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

    protected override bool IsAsyncMethod()
    {
        return ContainsAwaitExpressions;
    }
}
