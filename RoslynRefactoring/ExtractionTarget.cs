using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace RoslynRefactoring;

public abstract class ExtractionTarget
{
    protected readonly SemanticModel semanticModel;

    protected ExtractionTarget(SemanticModel semanticModel)
    {
        this.semanticModel = semanticModel;
    }

    protected abstract TypeSyntax DetermineReturnType();

    protected abstract BlockSyntax CreateMethodBody();

    public abstract SyntaxNode CreateReplacementNode(string methodName);

    public abstract void ReplaceInEditor(SyntaxEditor editor, SyntaxNode replacementNode);

    public abstract SyntaxNode GetInsertionPoint();


    public static ExtractionTarget CreateFromSelection(SyntaxNode selectedNode, TextSpan span, BlockSyntax block, SemanticModel semanticModel)
    {
        var selectedStatements = FindStatementsInSelection(selectedNode, span);

        if (selectedStatements.Count > 0)
        {
            return new StatementExtractionTarget(selectedStatements, block, semanticModel);
        }

        var selectedExpression = FindExpressionInSelection(selectedNode, span);
        if (selectedExpression == null)
        {
            throw new InvalidOperationException("No statements or expressions selected for extraction.");
        }

        return new ExpressionExtractionTarget(selectedExpression, semanticModel);
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

    protected static InvocationExpressionSyntax CreateMethodCall(string methodName, List<ParameterSyntax> parameters)
    {
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName(methodName),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(parameters.Select(p =>
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(p.Identifier.Text))))));
    }

    public MethodDeclarationSyntax CreateMethodDeclaration(string methodName)
    {
        var methodBody = CreateMethodBody();
        var returnType = DetermineReturnType();
        var parameters = GetParameters();
        var modifiers = new List<SyntaxToken> { SyntaxFactory.Token(SyntaxKind.PrivateKeyword) };

        if (IsAsyncMethod())
        {
            modifiers.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword));
        }

        var methodDeclaration = SyntaxFactory.MethodDeclaration(returnType, methodName)
            .AddModifiers(modifiers.ToArray())
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)))
            .WithBody(methodBody);
        return methodDeclaration;
    }

    protected virtual bool IsAsyncMethod()
    {
        return false;
    }

    protected abstract List<ParameterSyntax> GetParameters();
}
