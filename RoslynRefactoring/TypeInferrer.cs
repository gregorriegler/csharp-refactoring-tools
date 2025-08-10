using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace RoslynRefactoring;

public sealed class TypeInferrer
{
    private const string StringType = "string";
    private const string ObjectType = "object";

    public string? InferType(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (expression is AwaitExpressionSyntax awaitExpr)
        {
            var typeInfo = semanticModel.GetTypeInfo(awaitExpr);
            if (typeInfo.Type != null && typeInfo.Type.TypeKind != TypeKind.Error)
            {
                return typeInfo.Type.ToDisplayString();
            }

            if (IsErrorTypeExpression(awaitExpr.Expression, semanticModel))
            {
                return ObjectType;
            }

            return StringType;
        }

        var regularTypeInfo = semanticModel.GetTypeInfo(expression);
        if (regularTypeInfo.Type != null && regularTypeInfo.Type.TypeKind != TypeKind.Error)
        {
            return regularTypeInfo.Type.ToDisplayString();
        }

        return InferTypeUsingInlinedStrategies(expression, semanticModel);
    }

    private string InferTypeUsingInlinedStrategies(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        // ToList strategy
        var toListResult = TryInferToListType(expression, semanticModel);
        if (toListResult != null)
        {
            return toListResult;
        }

        // Method symbol strategy
        var methodSymbolResult = TryInferMethodSymbolType(expression, semanticModel);
        if (methodSymbolResult != null)
        {
            return methodSymbolResult;
        }

        // Default fallback
        return ObjectType;
    }

    private string? TryInferToListType(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (!IsToListInvocation(expression, semanticModel))
        {
            return null;
        }

        if (expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var collectionExpression = memberAccess.Expression;
            var collectionTypeInfo = semanticModel.GetTypeInfo(collectionExpression);

            if (collectionTypeInfo.Type is IArrayTypeSymbol arrayType)
            {
                var elementType = arrayType.ElementType.ToDisplayString();
                return $"List<{elementType}>";
            }

            if (collectionTypeInfo.Type is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
            {
                var elementType = namedType.TypeArguments[0].ToDisplayString();
                return $"List<{elementType}>";
            }

            if (collectionTypeInfo.Type?.SpecialType == SpecialType.System_Object)
            {
                return "List<object>";
            }
        }

        return "List<string>";
    }

    private static bool IsToListInvocation(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (expression is not InvocationExpressionSyntax invocation ||
            invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        // First try semantic model approach
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
        {
            return methodSymbol.Name == "ToList" &&
                   methodSymbol.ContainingType?.ToDisplayString() == "System.Linq.Enumerable";
        }

        // Fallback to string-based pattern matching when semantic model fails
        var expressionText = expression.ToString();
        return expressionText.EndsWith(".ToList()");
    }

    private string? TryInferMethodSymbolType(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(expression);

        if (symbolInfo.Symbol is IMethodSymbol methodSymbol && methodSymbol.ReturnType != null)
        {
            var returnTypeName = methodSymbol.ReturnType.ToDisplayString();
            if (IsValidTypeName(returnTypeName))
            {
                return returnTypeName;
            }
        }

        return null;
    }

    private static bool IsValidTypeName(string typeName)
    {
        return typeName != "?" && !string.IsNullOrEmpty(typeName);
    }

    private bool IsErrorTypeExpression(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (expression is InvocationExpressionSyntax invocation)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol == null && symbolInfo.CandidateSymbols.IsEmpty)
            {
                var diagnostics = semanticModel.GetDiagnostics(invocation.Span);
                if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public string ResolveActualTypeForForeachVariable(ILocalSymbol localSymbol, BlockSyntax containingBlock, SemanticModel semanticModel)
    {
        var methodBlock = FindMethodBlock(containingBlock);
        if (methodBlock == null)
        {
            return "var";
        }

        var foreachStatement = FindForeachStatementForVariable(methodBlock, localSymbol.Name);
        if (foreachStatement == null)
        {
            return "var";
        }

        return ExtractElementTypeFromCollection(foreachStatement, semanticModel);
    }

    private BlockSyntax? FindMethodBlock(BlockSyntax containingBlock)
    {
        return containingBlock.Parent?.AncestorsAndSelf().OfType<BlockSyntax>().FirstOrDefault();
    }

    private ForEachStatementSyntax? FindForeachStatementForVariable(BlockSyntax methodBlock, string variableName)
    {
        var allForeachStatements = methodBlock
            .DescendantNodesAndSelf()
            .OfType<ForEachStatementSyntax>()
            .ToList();

        return allForeachStatements
            .FirstOrDefault(fs => fs.Identifier.Text == variableName);
    }

    private string ExtractElementTypeFromCollection(ForEachStatementSyntax foreachStatement, SemanticModel semanticModel)
    {
        var collectionTypeInfo = semanticModel.GetTypeInfo(foreachStatement.Expression);

        if (collectionTypeInfo.Type is INamedTypeSymbol namedType &&
            namedType.TypeArguments.Length > 0)
        {
            return namedType.TypeArguments[0].ToDisplayString();
        }

        return "var";
    }
}
