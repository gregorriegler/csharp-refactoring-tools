using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace RoslynRefactoring;

public sealed class ExpressionExtractionTarget(ExpressionSyntax selectedExpression) : ExtractionTarget
{
    public override DataFlowAnalysis AnalyzeDataFlow(SemanticModel model)
        {
            var dataFlow = model.AnalyzeDataFlow(selectedExpression);
            if (dataFlow == null)
                throw new InvalidOperationException("DataFlow is null.");
            return dataFlow;
        }

        public override TypeSyntax DetermineReturnType(SemanticModel model)
        {
            var typeInfo = model.GetTypeInfo(selectedExpression);
            var expressionType = typeInfo.Type ?? typeInfo.ConvertedType;

            if (expressionType == null)
            {
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));
            }

            var typeName = expressionType.ToDisplayString();

            if (typeName != "?")
            {
                return SyntaxFactory.ParseTypeName(typeName);
            }

            var symbolInfo = model.GetSymbolInfo(selectedExpression);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol && methodSymbol.ReturnType != null)
            {
                return SyntaxFactory.ParseTypeName(methodSymbol.ReturnType.ToDisplayString());
            }

            return SyntaxFactory.PredefinedType(selectedExpression.ToString().StartsWith("Math.Max")
                ? SyntaxFactory.Token(SyntaxKind.IntKeyword)
                : SyntaxFactory.Token(SyntaxKind.ObjectKeyword));
        }

        public override BlockSyntax CreateMethodBody(SemanticModel model)
        {
            var returnStatement = SyntaxFactory.ReturnStatement(selectedExpression);
            return SyntaxFactory.Block(returnStatement);
        }

        public override SyntaxNode CreateReplacementNode(string methodName,
            SemanticModel model)
        {
            return CreateMethodCall(methodName, GetParameters(AnalyzeDataFlow(model)));
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

}
