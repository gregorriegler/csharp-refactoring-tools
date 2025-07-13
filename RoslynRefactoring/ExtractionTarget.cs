using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

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
        /// <param name="model">The semantic model for analysis</param>
        /// <param name="dataFlow">The data flow analysis results</param>
        public abstract void ReplaceInEditor(SyntaxEditor editor, InvocationExpressionSyntax methodCall, SemanticModel model, DataFlowAnalysis dataFlow);

        /// <summary>
        /// Gets the syntax node where the new extracted method should be inserted
        /// </summary>
        /// <returns>The syntax node to insert the new method after</returns>
        public abstract SyntaxNode GetInsertionPoint();

        /// <summary>
        /// Applies any necessary modifications to the extracted method body and return type
        /// </summary>
        /// <param name="methodBody">The initial method body</param>
        /// <param name="returnType">The initial return type</param>
        /// <returns>A tuple containing the modified method body and return type</returns>
        public abstract (BlockSyntax methodBody, TypeSyntax returnType) ApplyModifications(BlockSyntax methodBody, TypeSyntax returnType);

        /// <summary>
        /// Creates an appropriate ExtractionTarget based on the selected node and span
        /// </summary>
        /// <param name="selectedNode">The selected syntax node</param>
        /// <param name="span">The text span of the selection</param>
        /// <param name="block">The containing block syntax</param>
        /// <returns>An ExtractionTarget instance (either ExpressionExtractionTarget or StatementExtractionTarget)</returns>
        public static ExtractionTarget CreateFromSelection(SyntaxNode selectedNode, TextSpan span, BlockSyntax block)
        {
            var selectedStatements = new List<StatementSyntax>();
            ExpressionSyntax? selectedExpression = null;

            if (selectedNode is BlockSyntax blockNode)
            {
                selectedStatements = blockNode.Statements
                    .Where(stmt => span.OverlapsWith(stmt.Span))
                    .ToList();
            }
            else if (selectedNode is StatementSyntax singleStatement && span.OverlapsWith(singleStatement.Span))
            {
                // Only treat as statement extraction if the span covers the entire statement
                // If it's a partial selection, look for expressions instead
                if (span.Contains(singleStatement.Span) || singleStatement.Span.Contains(span))
                {
                    selectedStatements.Add(singleStatement);
                }
            }
            else
            {
                selectedStatements = selectedNode.DescendantNodesAndSelf()
                    .OfType<StatementSyntax>()
                    .Where(stmt => span.OverlapsWith(stmt.Span))
                    .ToList();
            }

            // If no statements found, check if we're selecting an expression
            if (selectedStatements.Count == 0)
            {
                if (selectedNode is ExpressionSyntax expression)
                {
                    selectedExpression = expression;
                }
                else
                {
                    // Get all expressions in the selected node and its descendants
                    var allExpressions = selectedNode.DescendantNodesAndSelf()
                        .OfType<ExpressionSyntax>()
                        .ToList();

                    // Try to find an expression that overlaps with or contains the span
                    selectedExpression = allExpressions
                        .Where(expr => span.OverlapsWith(expr.Span) || expr.Span.Contains(span))
                        .OrderBy(expr => expr.Span.Length) // Prefer smaller, more specific expressions
                        .FirstOrDefault();

                    // If still not found, try looking at ancestors for expressions that contain the span
                    if (selectedExpression == null)
                    {
                        selectedExpression = selectedNode.AncestorsAndSelf()
                            .OfType<ExpressionSyntax>()
                            .Where(expr => expr.Span.Contains(span) || span.OverlapsWith(expr.Span))
                            .OrderBy(expr => expr.Span.Length) // Prefer smaller, more specific expressions
                            .FirstOrDefault();
                    }

                    // Special case: if we have an EqualsValueClauseSyntax, look for the expression inside it
                    if (selectedExpression == null && selectedNode is EqualsValueClauseSyntax equalsValue)
                    {
                        selectedExpression = equalsValue.Value;
                    }
                }

                if (selectedExpression == null)
                    throw new InvalidOperationException("No statements or expressions selected for extraction.");

                return new ExpressionExtractionTarget(selectedExpression);
            }
            else
            {
                return new StatementExtractionTarget(selectedStatements, block);
            }
        }
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

        public override void ReplaceInEditor(SyntaxEditor editor, InvocationExpressionSyntax methodCall, SemanticModel model, DataFlowAnalysis dataFlow)
        {
            // For expression extraction, replace the expression with the method call
            editor.ReplaceNode(selectedExpression, methodCall);
        }

        public override SyntaxNode GetInsertionPoint()
        {
            // Find the containing method using selectedExpression.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()
            var containingMethod = selectedExpression.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (containingMethod != null)
            {
                return containingMethod;
            }

            // Fallback to the selected expression itself if no method found
            return selectedExpression;
        }

        public override (BlockSyntax methodBody, TypeSyntax returnType) ApplyModifications(BlockSyntax methodBody, TypeSyntax returnType)
        {
            // Empty implementation - return unchanged values
            return (methodBody, returnType);
        }
    }

    /// <summary>
    /// Extraction target for statements
    /// </summary>
    public class StatementExtractionTarget : ExtractionTarget
    {
        private readonly List<StatementSyntax> selectedStatements;
        private readonly BlockSyntax containingBlock;
        private BlockSyntax? modifiedMethodBody;
        private TypeSyntax? modifiedReturnType;

        public StatementExtractionTarget(List<StatementSyntax> selectedStatements, BlockSyntax containingBlock)
        {
            this.selectedStatements = selectedStatements;
            this.containingBlock = containingBlock;
        }

        public override SyntaxNode GetSelectedNode()
        {
            return selectedStatements.First();
        }

        public override DataFlowAnalysis AnalyzeDataFlow(SemanticModel model)
        {
            // For statement extraction, analyze data flow of the statements
            var dataFlow = model?.AnalyzeDataFlow(selectedStatements.First(), selectedStatements.Last());
            if (dataFlow == null)
                throw new InvalidOperationException("DataFlow is null.");
            return dataFlow;
        }

        public override TypeSyntax DetermineReturnType(SemanticModel model, DataFlowAnalysis dataFlow)
        {
            var returns = dataFlow.DataFlowsOut.Intersect(dataFlow.WrittenInside, SymbolEqualityComparer.Default)
                .OfType<ILocalSymbol>()
                .ToList();

            var containsReturnStatements = selectedStatements
                .SelectMany(stmt => stmt.DescendantNodesAndSelf().OfType<ReturnStatementSyntax>())
                .Any();

            var allPathsReturnOrThrow = selectedStatements is [SwitchStatementSyntax switchStatement]
                                        && switchStatement.Sections.All(sec =>
                                            sec.Statements.LastOrDefault() is ReturnStatementSyntax
                                                or ThrowStatementSyntax);

            if (containsReturnStatements || allPathsReturnOrThrow)
            {
                var containingMethod = containingBlock.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                if (containingMethod?.ReturnType != null)
                {
                    return containingMethod.ReturnType;
                }
                else
                {
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
                }
            }
            else if (returns.Count == 0)
            {
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
            }
            else if (returns.FirstOrDefault() is { } localReturnSymbol)
            {
                return SyntaxFactory.ParseTypeName(localReturnSymbol.Type.ToDisplayString());
            }
            else
            {
                throw new InvalidOperationException("Unsupported return symbol type.");
            }
        }

        public override BlockSyntax CreateMethodBody(DataFlowAnalysis dataFlow)
        {
            return SyntaxFactory.Block(selectedStatements);
        }

        public override void ReplaceInEditor(SyntaxEditor editor, InvocationExpressionSyntax methodCall, SemanticModel model, DataFlowAnalysis dataFlow)
        {
            var returns = dataFlow.DataFlowsOut.Intersect(dataFlow.WrittenInside, SymbolEqualityComparer.Default)
                .OfType<ILocalSymbol>()
                .ToList();

            var containsReturnStatements = selectedStatements
                .SelectMany(stmt => stmt.DescendantNodesAndSelf().OfType<ReturnStatementSyntax>())
                .Any();

            var allPathsReturnOrThrow = selectedStatements is [SwitchStatementSyntax switchStatement]
                                        && switchStatement.Sections.All(sec =>
                                            sec.Statements.LastOrDefault() is ReturnStatementSyntax
                                                or ThrowStatementSyntax);

            StatementSyntax callStatement;
            var newMethodBody = SyntaxFactory.Block(selectedStatements);

            if (containsReturnStatements || allPathsReturnOrThrow)
            {
                callStatement = SyntaxFactory.ReturnStatement(methodCall);
            }
            else if (returns.Count == 0)
            {
                // Check if we have a single variable declaration statement
                if (selectedStatements.Count == 1 && selectedStatements.First() is LocalDeclarationStatementSyntax localDecl)
                {
                    var variable = localDecl.Declaration.Variables.FirstOrDefault();
                    if (variable != null)
                    {
                        // This is a variable declaration - we should return the variable and replace with assignment
                        var variableName = variable.Identifier.Text;
                        var variableType = localDecl.Declaration.Type;

                        // Add return statement to the extracted method
                        var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(variableName));
                        newMethodBody = newMethodBody.AddStatements(returnStatement);

                        // Replace with assignment to the method call
                        callStatement = SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(variableType)
                                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(variableName))
                                        .WithInitializer(SyntaxFactory.EqualsValueClause(methodCall)))));

                        // Update return type to match the variable type
                        if (model != null && variable.Initializer?.Value != null)
                        {
                            var typeInfo = model.GetTypeInfo(variable.Initializer.Value);
                            if (typeInfo.Type != null)
                            {
                                modifiedReturnType = SyntaxFactory.ParseTypeName(typeInfo.Type.ToDisplayString());
                            }
                        }

                        // Store the modified method body
                        modifiedMethodBody = newMethodBody;
                    }
                    else
                    {
                        callStatement = SyntaxFactory.ExpressionStatement(methodCall);
                    }
                }
                else
                {
                    callStatement = SyntaxFactory.ExpressionStatement(methodCall);
                }
            }
            else if (returns.FirstOrDefault() is { } localReturnSymbol)
            {
                var returnType = SyntaxFactory.ParseTypeName(localReturnSymbol.Type.ToDisplayString());
                StatementSyntax returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(localReturnSymbol.Name));

                if (selectedStatements.Count == 1 && selectedStatements.First() is ReturnStatementSyntax)
                {
                    callStatement = SyntaxFactory.ReturnStatement(methodCall);
                }
                else
                {
                    callStatement = SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(returnType)
                            .WithVariables(SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(localReturnSymbol.Name))
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(methodCall)))));
                }

                newMethodBody = newMethodBody.AddStatements(returnStatement);

                // Store the modified method body
                modifiedMethodBody = newMethodBody;
            }
            else
            {
                throw new InvalidOperationException("Unsupported return symbol type.");
            }

            editor.ReplaceNode(selectedStatements.First(), callStatement);
            foreach (var stmt in selectedStatements.Skip(1))
                editor.RemoveNode(stmt);
        }

        public override SyntaxNode GetInsertionPoint()
        {
            // Find the method node using containingBlock.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()
            var methodNode = containingBlock.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (methodNode != null)
            {
                return methodNode;
            }

            // Fall back to the last selected statement if no method found
            return selectedStatements.Last();
        }

        public override (BlockSyntax methodBody, TypeSyntax returnType) ApplyModifications(BlockSyntax methodBody, TypeSyntax returnType)
        {
            // Check if the extraction target modified the method body or return type
            var finalMethodBody = modifiedMethodBody ?? methodBody;
            var finalReturnType = modifiedReturnType ?? returnType;

            return (finalMethodBody, finalReturnType);
        }
    }
}
