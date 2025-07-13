using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace RoslynRefactoring
{
    /// <summary>
    /// Abstract base class for different types of extraction targets (statements, expressions, etc.)
    /// </summary>
    public abstract class ExtractionTarget
    {
        /// <summary>
        /// Gets the selected syntax node to be extracted
        /// </summary>
        /// <returns>The syntax node representing the code to extract</returns>
        public abstract SyntaxNode GetSelectedNode();

        /// <summary>
        /// Analyzes data flow for the selected code to determine parameters and return values
        /// </summary>
        /// <param name="model">The semantic model for analysis</param>
        /// <returns>Data flow analysis results</returns>
        public abstract DataFlowAnalysis AnalyzeDataFlow(SemanticModel model);

        /// <summary>
        /// Determines the return type for the extracted method based on data flow analysis
        /// </summary>
        /// <param name="model">The semantic model for type resolution</param>
        /// <param name="dataFlow">The data flow analysis results</param>
        /// <returns>The return type syntax for the extracted method</returns>
        public abstract TypeSyntax DetermineReturnType(SemanticModel model, DataFlowAnalysis dataFlow);

        /// <summary>
        /// Creates the method body for the extracted method
        /// </summary>
        /// <param name="dataFlow">The data flow analysis results</param>
        /// <returns>The block syntax representing the method body</returns>
        public abstract BlockSyntax CreateMethodBody(DataFlowAnalysis dataFlow);

        /// <summary>
        /// Replaces the original code in the editor with a method call
        /// </summary>
        /// <param name="editor">The syntax editor for making changes</param>
        /// <param name="methodCall">The method call expression to replace the original code</param>
        public abstract void ReplaceInEditor(SyntaxEditor editor, InvocationExpressionSyntax methodCall);
    }
    /// <summary>
    /// Extraction target for expressions
    /// </summary>
    public class ExpressionExtractionTarget : ExtractionTarget
    {
        private readonly ExpressionSyntax selectedExpression;

        public ExpressionExtractionTarget(ExpressionSyntax selectedExpression)
        {
            this.selectedExpression = selectedExpression;
        }

        public override SyntaxNode GetSelectedNode()
        {
            return selectedExpression;
        }

        public override DataFlowAnalysis AnalyzeDataFlow(SemanticModel model)
        {
            // For expression extraction, analyze data flow of the expression
            var dataFlow = model?.AnalyzeDataFlow(selectedExpression);
            if (dataFlow == null)
                throw new InvalidOperationException("DataFlow is null.");
            return dataFlow;
        }

        public override TypeSyntax DetermineReturnType(SemanticModel model, DataFlowAnalysis dataFlow)
        {
            // For expression extraction, determine return type from the expression
            var expressionType = model.GetTypeInfo(selectedExpression).Type;
            if (expressionType != null)
            {
                return SyntaxFactory.ParseTypeName(expressionType.ToDisplayString());
            }
            else
            {
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));
            }
        }

        public override BlockSyntax CreateMethodBody(DataFlowAnalysis dataFlow)
        {
            // For simple identifiers, return directly. For complex expressions, create a local variable first.
            if (selectedExpression is IdentifierNameSyntax)
            {
                // Simple variable reference - return directly
                var returnStatement = SyntaxFactory.ReturnStatement(selectedExpression);
                return SyntaxFactory.Block(returnStatement);
            }
            else
            {
                // Complex expression - create local variable and return it
                var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                        .WithVariables(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("a"))
                                .WithInitializer(SyntaxFactory.EqualsValueClause(selectedExpression)))));

                var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("a"));
                return SyntaxFactory.Block(variableDeclaration, returnStatement);
            }
        }

        public override void ReplaceInEditor(SyntaxEditor editor, InvocationExpressionSyntax methodCall)
        {
            // For expression extraction, replace the expression with the method call
            editor.ReplaceNode(selectedExpression, methodCall);
        }
    }
}
