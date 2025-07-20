using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynRefactoring;

public sealed record TypeInferenceContext(
    ExpressionSyntax Expression,
    SemanticModel SemanticModel,
    string? VariableName = null
);
