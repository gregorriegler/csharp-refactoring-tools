using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace RoslynRefactoring;

public class ExpressionExtractionTarget : ExtractionTarget
    {
        private readonly ExpressionSyntax selectedExpression;

        public ExpressionExtractionTarget(ExpressionSyntax selectedExpression)
        {
            this.selectedExpression = selectedExpression;
        }

        public virtual SyntaxNode GetSelectedNode()
        {
            return selectedExpression;
        }

        public override DataFlowAnalysis AnalyzeDataFlow(SemanticModel model)
        {
            var dataFlow = model?.AnalyzeDataFlow(selectedExpression);
            if (dataFlow == null)
                throw new InvalidOperationException("DataFlow is null.");
            return dataFlow;
        }

        public override TypeSyntax DetermineReturnType(SemanticModel model, DataFlowAnalysis dataFlow)
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

            if (selectedExpression.ToString().StartsWith("Math.Max"))
            {
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword));
            }

            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));
        }

        public override BlockSyntax CreateMethodBody(List<ILocalSymbol> returns)
        {
            var returnStatement = SyntaxFactory.ReturnStatement(selectedExpression);
            return SyntaxFactory.Block(returnStatement);
        }

        public override void ReplaceInEditor(SyntaxEditor editor, InvocationExpressionSyntax methodCall, SemanticModel model, List<ILocalSymbol> returns)
        {
            editor.ReplaceNode(selectedExpression, methodCall);
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
