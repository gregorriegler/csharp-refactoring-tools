using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace RoslynRefactoring;

public sealed class TypeInferrer
{
    private const string ToListPattern = ".ToList()";
    private const string ToArrayPattern = ".ToArray()";
    private const string ToDictionaryPattern = ".ToDictionary(";
    private const string SelectPattern = ".Select(";
    private const string WherePattern = ".Where(";
    private const string ListStringType = "List<string>";
    private const string StringArrayType = "string[]";
    private const string DictionaryType = "Dictionary<string, object>";
    private const string EnumerableType = "IEnumerable<object>";
    private const string StringType = "string";
    private const string ObjectType = "object";

    private static readonly Dictionary<string, string> PatternTypeMapping = new()
    {
        { ToListPattern, ListStringType },
        { ToArrayPattern, StringArrayType },
        { ToDictionaryPattern, DictionaryType },
        { SelectPattern, EnumerableType },
        { WherePattern, EnumerableType }
    };

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

        return InferTypeFromExpressionText(expression);
    }


    private string InferTypeFromExpressionText(ExpressionSyntax expression)
    {
        var expressionText = expression.ToString();

        foreach (var (pattern, type) in PatternTypeMapping)
        {
            if (expressionText.Contains(pattern))
            {
                return type;
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
}
