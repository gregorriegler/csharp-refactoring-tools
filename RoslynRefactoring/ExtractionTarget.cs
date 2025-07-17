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
        /// <param name="returns">The local symbols that flow out and are written inside</param>
        public abstract void ReplaceInEditor(SyntaxEditor editor, InvocationExpressionSyntax methodCall, SemanticModel model, List<ILocalSymbol> returns);

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
        /// <returns>A MethodSignature containing the modified method body and return type</returns>
        public abstract MethodSignature ApplyModifications(BlockSyntax methodBody, TypeSyntax returnType);

        /// <summary>
        /// Creates an appropriate ExtractionTarget based on the selected node and span
        /// </summary>
        /// <param name="selectedNode">The selected syntax node</param>
        /// <param name="span">The text span of the selection</param>
        /// <param name="block">The containing block syntax</param>
        /// <returns>An ExtractionTarget instance (either ExpressionExtractionTarget or StatementExtractionTarget)</returns>
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

            if (selectedNode is StatementSyntax singleStatement && span.OverlapsWith(singleStatement.Span))
            {
                if (span.Contains(singleStatement.Span) || singleStatement.Span.Contains(span))
                {
                    return new List<StatementSyntax> { singleStatement };
                }

                return new List<StatementSyntax>();
            }

            return selectedNode.DescendantNodesAndSelf()
                .OfType<StatementSyntax>()
                .Where(stmt => span.OverlapsWith(stmt.Span))
                .ToList();
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

        public override BlockSyntax CreateMethodBody(DataFlowAnalysis dataFlow)
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

        public override MethodSignature ApplyModifications(BlockSyntax methodBody, TypeSyntax returnType)
        {
            // Empty implementation - return unchanged values
            return MethodSignature.Create(methodBody, returnType);
        }
    }

    /// <summary>
    /// Extraction target for statements
    /// </summary>
    public class StatementExtractionTarget : ExtractionTarget
    {
        private readonly List<StatementSyntax> selectedStatements;
        private readonly BlockSyntax containingBlock;
        private readonly ReturnBehavior returnBehavior;
        private BlockSyntax? modifiedMethodBody;
        private TypeSyntax? modifiedReturnType;

        public StatementExtractionTarget(List<StatementSyntax> selectedStatements, BlockSyntax containingBlock)
        {
            this.selectedStatements = selectedStatements;
            this.containingBlock = containingBlock;
            this.returnBehavior = new ReturnBehavior(selectedStatements);
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

            if (returnBehavior.RequiresReturnStatement)
            {
                var containingMethod = containingBlock.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                return containingMethod?.ReturnType ?? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
            }

            if (returns.Count == 0)
            {
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
            }

            if (returns.FirstOrDefault() is { } localReturnSymbol)
            {
                return SyntaxFactory.ParseTypeName(localReturnSymbol.Type.ToDisplayString());
            }

            throw new InvalidOperationException("Unsupported return symbol type.");
        }

        public override BlockSyntax CreateMethodBody(DataFlowAnalysis dataFlow)
        {
            return SyntaxFactory.Block(selectedStatements);
        }

        public override void ReplaceInEditor(SyntaxEditor editor, InvocationExpressionSyntax methodCall, SemanticModel model, List<ILocalSymbol> returns)
        {
            var newMethodBody = SyntaxFactory.Block(selectedStatements);
            StatementSyntax callStatement;

            if (returnBehavior.RequiresReturnStatement)
            {
                callStatement = SyntaxFactory.ReturnStatement(methodCall);
            }
            else if (returns.Count == 0)
            {
                callStatement = HandleNoReturnsCase(methodCall, model, newMethodBody);
            }
            else if (returns.FirstOrDefault() is { } localReturnSymbol)
            {
                callStatement = HandleLocalReturnCase(methodCall, localReturnSymbol, newMethodBody);
            }
            else
            {
                throw new InvalidOperationException("Unsupported return symbol type.");
            }

            editor.ReplaceNode(selectedStatements.First(), callStatement);
            foreach (var stmt in selectedStatements.Skip(1))
                editor.RemoveNode(stmt);
        }

        private StatementSyntax HandleNoReturnsCase(InvocationExpressionSyntax methodCall, SemanticModel model, BlockSyntax newMethodBody)
        {
            if (selectedStatements.Count != 1 || selectedStatements.First() is not LocalDeclarationStatementSyntax localDecl)
            {
                return SyntaxFactory.ExpressionStatement(methodCall);
            }

            var variable = localDecl.Declaration.Variables.FirstOrDefault();
            if (variable == null)
            {
                return SyntaxFactory.ExpressionStatement(methodCall);
            }

            var variableName = variable.Identifier.Text;
            var variableType = localDecl.Declaration.Type;

            var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(variableName));
            modifiedMethodBody = newMethodBody.AddStatements(returnStatement);

            if (model != null && variable.Initializer?.Value != null)
            {
                var typeInfo = model.GetTypeInfo(variable.Initializer.Value);
                if (typeInfo.Type != null)
                {
                    modifiedReturnType = SyntaxFactory.ParseTypeName(typeInfo.Type.ToDisplayString());
                }
            }

            return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(variableType)
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(variableName))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(methodCall)))));
        }

        private StatementSyntax HandleLocalReturnCase(InvocationExpressionSyntax methodCall, ILocalSymbol localReturnSymbol, BlockSyntax newMethodBody)
        {
            var returnType = SyntaxFactory.ParseTypeName(localReturnSymbol.Type.ToDisplayString());
            var returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(localReturnSymbol.Name));

            modifiedMethodBody = newMethodBody.AddStatements(returnStatement);

            if (selectedStatements.Count == 1 && selectedStatements.First() is ReturnStatementSyntax)
            {
                return SyntaxFactory.ReturnStatement(methodCall);
            }

            return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(returnType)
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

        public override MethodSignature ApplyModifications(BlockSyntax methodBody, TypeSyntax returnType)
        {
            var finalMethodBody = modifiedMethodBody ?? methodBody;
            var finalReturnType = modifiedReturnType ?? returnType;

            return MethodSignature.Create(finalMethodBody, finalReturnType);
        }
    }

    /// <summary>
    /// Value object that encapsulates return behavior analysis logic
    /// </summary>
    public class ReturnBehavior
    {
        private readonly List<StatementSyntax> selectedStatements;

        public ReturnBehavior(List<StatementSyntax> statements)
        {
            selectedStatements = statements;
        }

        public bool HasReturnStatements => selectedStatements
            .SelectMany(stmt => stmt.DescendantNodesAndSelf().OfType<ReturnStatementSyntax>())
            .Any();

        public bool AllPathsReturnOrThrow => selectedStatements is [SwitchStatementSyntax switchStatement]
                                             && switchStatement.Sections.All(sec =>
                                                 sec.Statements.LastOrDefault() is ReturnStatementSyntax
                                                     or ThrowStatementSyntax);

        public bool RequiresReturnStatement => HasReturnStatements || AllPathsReturnOrThrow;
    }

    public record MethodSignature
    {
        public BlockSyntax MethodBody { get; }
        public TypeSyntax ReturnType { get; }

        private MethodSignature(BlockSyntax methodBody, TypeSyntax returnType)
        {
            MethodBody = methodBody;
            ReturnType = returnType;
        }

        public static MethodSignature Create(BlockSyntax methodBody, TypeSyntax returnType)
        {
            return new MethodSignature(methodBody, returnType);
        }
    }
}
