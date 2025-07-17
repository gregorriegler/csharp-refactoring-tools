using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace RoslynRefactoring;

public abstract class ExtractionTarget
    {
        public abstract DataFlowAnalysis AnalyzeDataFlow(SemanticModel model);

        public abstract TypeSyntax DetermineReturnType(SemanticModel model, DataFlowAnalysis dataFlow);

        public abstract BlockSyntax CreateMethodBody();

        public abstract void ReplaceInEditor(SyntaxEditor editor, InvocationExpressionSyntax methodCall, SemanticModel model, List<ILocalSymbol> returns);

        public abstract SyntaxNode GetInsertionPoint();

        public abstract MethodSignature ApplyModifications(BlockSyntax methodBody, TypeSyntax returnType);

        public static ExtractionTarget CreateFromSelection(SyntaxNode selectedNode, TextSpan span, BlockSyntax block)
        {
            var selectedStatements = FindStatementsInSelection(selectedNode, span);

            if (selectedStatements.Count > 0)
            {
                return new StatementExtractionTarget(selectedStatements, block);
            }

            var selectedExpression = FindExpressionInSelection(selectedNode, span);
            if (selectedExpression == null)
            {
                throw new InvalidOperationException("No statements or expressions selected for extraction.");
            }

            return new ExpressionExtractionTarget(selectedExpression);
        }

        private static List<StatementSyntax> FindStatementsInSelection(SyntaxNode selectedNode, TextSpan span)
        {
            if (selectedNode is BlockSyntax blockNode)
            {
                return blockNode.Statements
                    .Where(stmt => span.OverlapsWith(stmt.Span))
                    .ToList();
            }

            if (selectedNode is not StatementSyntax singleStatement || !span.OverlapsWith(singleStatement.Span))
                return selectedNode.DescendantNodesAndSelf()
                    .OfType<StatementSyntax>()
                    .Where(stmt => span.OverlapsWith(stmt.Span))
                    .ToList();
            if (span.Contains(singleStatement.Span) || singleStatement.Span.Contains(span))
            {
                return [singleStatement];
            }

            return [];

        }

        private static ExpressionSyntax? FindExpressionInSelection(SyntaxNode selectedNode, TextSpan span)
        {
            if (selectedNode is ExpressionSyntax expression)
            {
                return expression;
            }

            var selectedExpression = FindExpressionInDescendants(selectedNode, span);
            if (selectedExpression != null)
            {
                return selectedExpression;
            }

            selectedExpression = FindExpressionInAncestors(selectedNode, span);
            if (selectedExpression != null)
            {
                return selectedExpression;
            }

            if (selectedNode is EqualsValueClauseSyntax equalsValue)
            {
                return equalsValue.Value;
            }

            return null;
        }

        private static ExpressionSyntax? FindExpressionInDescendants(SyntaxNode selectedNode, TextSpan span)
        {
            var allExpressions = selectedNode.DescendantNodesAndSelf()
                .OfType<ExpressionSyntax>()
                .ToList();

            return allExpressions
                .Where(expr => span.OverlapsWith(expr.Span) || expr.Span.Contains(span))
                .OrderBy(expr => Math.Abs(expr.Span.Length - span.Length))
                .ThenBy(expr => expr.Span.Length)
                .FirstOrDefault();
        }

        private static ExpressionSyntax? FindExpressionInAncestors(SyntaxNode selectedNode, TextSpan span)
        {
            return selectedNode.AncestorsAndSelf()
                .OfType<ExpressionSyntax>()
                .Where(expr => expr.Span.Contains(span) || span.OverlapsWith(expr.Span))
                .OrderBy(expr => Math.Abs(expr.Span.Length - span.Length))
                .ThenBy(expr => expr.Span.Length)
                .FirstOrDefault();
        }
}
