using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace RoslynRefactoring;

public sealed class ExpressionExtractionTarget(ExpressionSyntax selectedExpression, SemanticModel semanticModel) : ExtractionTarget(semanticModel)
{
    private DataFlowAnalysis AnalyzeDataFlow()
    {
        var dataFlow = semanticModel.AnalyzeDataFlow(selectedExpression);
        if (dataFlow == null)
            throw new InvalidOperationException("DataFlow is null.");
        return dataFlow;
    }

    protected override TypeSyntax DetermineReturnType()
    {
        var typeInfo = semanticModel.GetTypeInfo(selectedExpression);
        var expressionType = typeInfo.Type ?? typeInfo.ConvertedType;

        if (expressionType == null || expressionType.TypeKind == TypeKind.Error)
        {
            return TryInferTypeFromExpression();
        }

        var typeName = expressionType.ToDisplayString();
        if (IsValidTypeName(typeName))
        {
            return SyntaxFactory.ParseTypeName(typeName);
        }

        return TryInferTypeFromExpression();
    }

    private TypeSyntax TryInferTypeFromExpression()
    {
        var strategies = new List<IExpressionTypeInferenceStrategy>
        {
            new MethodSymbolTypeInferenceStrategy(),
            new ToListTypeInferenceStrategy(),
            new DefaultTypeInferenceStrategy()
        };

        foreach (var strategy in strategies)
        {
            var result = strategy.InferType(selectedExpression, semanticModel);
            if (result != null)
            {
                return result;
            }
        }

        return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));
    }

    protected override BlockSyntax CreateMethodBody()
    {
        var returnStatement = SyntaxFactory.ReturnStatement(selectedExpression);
        return SyntaxFactory.Block(returnStatement);
    }

    protected override List<ParameterSyntax> GetParameters()
    {
        var dataFlow = AnalyzeDataFlow();
        return dataFlow.ReadInside.Except(dataFlow.WrittenInside)
            .OfType<ILocalSymbol>()
            .Select(s => SyntaxFactory.Parameter(SyntaxFactory.Identifier(s.Name))
                .WithType(SyntaxFactory.ParseTypeName(s.Type.ToDisplayString()))).ToList();
    }

    public override SyntaxNode CreateReplacementNode(string methodName)
    {
        return CreateMethodCall(methodName, GetParameters());
    }

    public override void ReplaceInEditor(SyntaxEditor editor, SyntaxNode replacementNode)
    {
        editor.ReplaceNode(selectedExpression, replacementNode);
    }

    public override SyntaxNode GetInsertionPoint()
    {
        var containingMethod = selectedExpression.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod != null)
        {
            return containingMethod;
        }

        return selectedExpression;
    }

    private static bool IsValidTypeName(string typeName)
    {
        return typeName != "?" && !string.IsNullOrEmpty(typeName);
    }

}
