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
        /// <param name="model">The semantic model for analysis</param>
        /// <param name="dataFlow">The data flow analysis results</param>
        public abstract void ReplaceInEditor(SyntaxEditor editor, InvocationExpressionSyntax methodCall, SemanticModel model, DataFlowAnalysis dataFlow);
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

        /// <summary>
        /// Gets the modified method body if it was updated during replacement logic
        /// </summary>
        public BlockSyntax? GetModifiedMethodBody() => modifiedMethodBody;

        /// <summary>
        /// Gets the modified return type if it was updated during replacement logic
        /// </summary>
        public TypeSyntax? GetModifiedReturnType() => modifiedReturnType;

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
    }
}
