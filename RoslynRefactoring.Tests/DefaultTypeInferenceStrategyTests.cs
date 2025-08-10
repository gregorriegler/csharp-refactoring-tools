using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class DefaultTypeInferenceStrategyTests
{
    private DefaultTypeInferenceStrategy strategy;

    [SetUp]
    public void SetUp()
    {
        strategy = new DefaultTypeInferenceStrategy();
    }

    [Test]
    public void InferType_WithAnyExpression_ShouldAlwaysReturnObject()
    {
        var code = @"
public class TestClass
{
    public void TestMethod()
    {
        var result = 42;
    }
}";

        var result = InferTypeFromExpression(code);

        Assert.That(result, Is.EqualTo("object"));
    }

    [Test]
    public void InferType_WithComplexExpression_ShouldReturnObject()
    {
        var code = @"
using System.Linq;
public class TestClass
{
    public void TestMethod()
    {
        var numbers = new int[] { 1, 2, 3 };
        var result = numbers.Where(x => x > 1).Select(x => x * 2).ToList();
    }
}";

        var result = InferTypeFromExpression(code);

        Assert.That(result, Is.EqualTo("object"));
    }

    private string InferTypeFromExpression(string code)
    {
        var (root, semanticModel) = CreateSyntaxTreeAndModel(code);
        var variableDeclaration = root.DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .First(v => v.Identifier.ValueText == "result");

        var expression = variableDeclaration.Initializer!.Value;

        var result = strategy.InferType(expression, semanticModel);
        return result?.ToString() ?? "null";
    }

    private (SyntaxNode root, SemanticModel semanticModel) CreateSyntaxTreeAndModel(string code)
    {
        var document = DocumentTestHelper.CreateDocument(code);
        var root = document.GetSyntaxRootAsync().Result!;
        var semanticModel = document.GetSemanticModelAsync().Result!;
        return (root, semanticModel);
    }
}
