using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace RoslynRefactoring;

public sealed class TypeInferrer
{
    private const string StringType = "string";
    private const string ObjectType = "object";

    private readonly List<ITypeInferrer> strategies;

    public TypeInferrer()
    {
        strategies = new List<ITypeInferrer>
        {
            new ToListTypeInferenceStrategy(),
            new MethodSymbolTypeInferenceStrategy(),
            new DefaultTypeInferenceStrategy()
        };
    }

    public string InferType(ExpressionSyntax expression, SemanticModel semanticModel)
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

        return InferTypeUsingStrategies(expression, semanticModel);
    }

    private string InferTypeUsingStrategies(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        foreach (var strategy in strategies)
        {
            var result = strategy.InferType(expression, semanticModel);
            if (result != null)
            {
                return result.ToString();
            }
        }

        return ObjectType;
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
