using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace RoslynRefactoring;

/// <summary>
/// Extract selected code into a new method
/// </summary>
public class ExtractMethod(CodeSelection selection, string newMethodName) : IRefactoring
{
    public static ExtractMethod Create(string[] args)
    {
        var selection = CodeSelection.Parse(args[0]);
        var newMethodName = args[1];
        return new ExtractMethod(selection, newMethodName);
    }

    public async Task<Document> PerformAsync(Document document)
    {
        var span = await GetSpan(document, selection);

        var root = await document.GetSyntaxRootAsync();
        if (root == null)
            throw new InvalidOperationException("SyntaxRoot is null.");

        var spanText = root.GetText().ToString(span);
        Console.WriteLine("\n=== Raw Span Content ===\n" + spanText);

        var selectedNode = root.FindNode(span);

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
            selectedStatements.Add(singleStatement);
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
        }

        var block = selectedNode.AncestorsAndSelf().OfType<BlockSyntax>().FirstOrDefault();
        if (block == null)
            throw new InvalidOperationException("Selected statements are not inside a block.");

        var model = await document.GetSemanticModelAsync();

        DataFlowAnalysis? dataFlow;
        if (selectedExpression != null)
        {
            // For expression extraction, analyze data flow of the expression
            dataFlow = model?.AnalyzeDataFlow(selectedExpression);
        }
        else
        {
            // For statement extraction, analyze data flow of the statements
            dataFlow = model?.AnalyzeDataFlow(selectedStatements.First(), selectedStatements.Last());
        }

        if (dataFlow == null)
            throw new InvalidOperationException("DataFlow is null.");

        var parameters = dataFlow.ReadInside.Except(dataFlow.WrittenInside)
            .OfType<ILocalSymbol>()
            .Select(s => SyntaxFactory.Parameter(SyntaxFactory.Identifier(s.Name))
                .WithType(SyntaxFactory.ParseTypeName(s.Type.ToDisplayString()))).ToList();

        var returns = dataFlow.DataFlowsOut.Intersect(dataFlow.WrittenInside, SymbolEqualityComparer.Default)
            .OfType<ILocalSymbol>()
            .ToList();

        var containsReturnStatements = selectedExpression == null && selectedStatements
            .SelectMany(stmt => stmt.DescendantNodesAndSelf().OfType<ReturnStatementSyntax>())
            .Any();

        var allPathsReturnOrThrow = selectedExpression == null && selectedStatements is [SwitchStatementSyntax switchStatement]
                                    && switchStatement.Sections.All(sec =>
                                        sec.Statements.LastOrDefault() is ReturnStatementSyntax
                                            or ThrowStatementSyntax);

        TypeSyntax returnType;
        BlockSyntax newMethodBody;

        if (selectedExpression != null)
        {
            // For expression extraction, determine return type from the expression
            var expressionType = model?.GetTypeInfo(selectedExpression).Type;
            if (expressionType != null)
            {
                returnType = SyntaxFactory.ParseTypeName(expressionType.ToDisplayString());
            }
            else
            {
                returnType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));
            }

            // Create method body that returns the expression
            var returnStatement = SyntaxFactory.ReturnStatement(selectedExpression);
            newMethodBody = SyntaxFactory.Block(returnStatement);
        }
        else
        {
            // Original logic for statement extraction
            if (containsReturnStatements || allPathsReturnOrThrow)
            {
                var containingMethod = block.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                if (containingMethod?.ReturnType != null)
                {
                    returnType = containingMethod.ReturnType;
                }
                else
                {
                    returnType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
                }
            }
            else if (returns.Count == 0)
            {
                returnType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
            }
            else if (returns.FirstOrDefault() is { } localReturnSymbol)
            {
                returnType = SyntaxFactory.ParseTypeName(localReturnSymbol.Type.ToDisplayString());
            }
            else
            {
                throw new InvalidOperationException("Unsupported return symbol type.");
            }

            newMethodBody = SyntaxFactory.Block(selectedStatements);
        }

        var invocationExpressionSyntax = SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName(newMethodName),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(parameters.Select(p =>
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(p.Identifier.Text))))));

        var methodDeclaration = SyntaxFactory.MethodDeclaration(returnType, newMethodName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)))
            .WithBody(newMethodBody);

        var editor = new SyntaxEditor(root, document.Project.Solution.Workspace.Services);

        if (selectedExpression != null)
        {
            // For expression extraction, replace the expression with the method call
            editor.ReplaceNode(selectedExpression, invocationExpressionSyntax);
        }
        else
        {
            // Original logic for statement extraction
            StatementSyntax callStatement;

            if (containsReturnStatements || allPathsReturnOrThrow)
            {
                callStatement = SyntaxFactory.ReturnStatement(invocationExpressionSyntax);
            }
            else if (returns.Count == 0)
            {
                callStatement = SyntaxFactory.ExpressionStatement(invocationExpressionSyntax);
            }
            else if (returns.FirstOrDefault() is { } localReturnSymbol)
            {
                StatementSyntax returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(localReturnSymbol.Name));

                if (selectedStatements.Count == 1 && selectedStatements.First() is ReturnStatementSyntax)
                {
                    callStatement = SyntaxFactory.ReturnStatement(invocationExpressionSyntax);
                }
                else
                {
                    callStatement = SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(returnType)
                            .WithVariables(SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(localReturnSymbol.Name))
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(invocationExpressionSyntax)))));
                }

                newMethodBody = newMethodBody.AddStatements(returnStatement);
            }
            else
            {
                throw new InvalidOperationException("Unsupported return symbol type.");
            }

            editor.ReplaceNode(selectedStatements.First(), callStatement);
            foreach (var stmt in selectedStatements.Skip(1))
                editor.RemoveNode(stmt);
        }

        var methodNode = block.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodNode != null)
        {
            editor.InsertAfter(methodNode, methodDeclaration);
        }
        else if (selectedExpression != null)
        {
            // For expression extraction, insert after the containing method
            var containingMethod = selectedExpression.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (containingMethod != null)
            {
                editor.InsertAfter(containingMethod, methodDeclaration);
            }
        }
        else
        {
            editor.InsertAfter(selectedStatements.Last(), methodDeclaration);
        }

        var newRoot = editor.GetChangedRoot().NormalizeWhitespace();

        Console.WriteLine($"✅ Extracted method '{newMethodName}'");
        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<TextSpan> GetSpan(Document document, CodeSelection selection)
    {
        var lines = (await document.GetTextAsync()).Lines;
        var span = TextSpan.FromBounds(
            GetPos(selection.Start),
            GetPos(selection.End)
        );
        return span;

        int GetPos(Cursor cursor) => lines[cursor.Line - 1].Start + cursor.Column - 1;
    }
}
