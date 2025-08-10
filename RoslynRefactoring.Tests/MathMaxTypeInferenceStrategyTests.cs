using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class MathMaxTypeInferenceStrategyTests
{
    private MathMaxTypeInferenceStrategy strategy;

    [SetUp]
    public void SetUp()
    {
        strategy = new MathMaxTypeInferenceStrategy();
    }

    [Test]
    public void InferType_WithMathMaxCall_ShouldReturnInt()
    {
        var code = @"
using System;
public class TestClass
{
    public void TestMethod()
    {
        var result = Math.Max(5, 10);
    }
}";

        var result = InferTypeFromMathMaxExpression(code);

        Assert.That(result, Is.EqualTo("int"));
    }

    [Test]
    public void InferType_WithMathMaxCallWithVariables_ShouldReturnInt()
    {
        var code = @"
using System;
public class TestClass
{
    public void TestMethod()
    {
        int a = 5;
        int b = 10;
        var result = Math.Max(a, b);
    }
}";

        var result = InferTypeFromMathMaxExpression(code);

        Assert.That(result, Is.EqualTo("int"));
    }

    [Test]
    public void InferType_WithMathMaxCallWithComplexExpressions_ShouldReturnInt()
    {
        var code = @"
using System;
public class TestClass
{
    public void TestMethod()
    {
        var result = Math.Max(GetValue1(), GetValue2());
    }

    private int GetValue1() => 5;
    private int GetValue2() => 10;
}";

        var result = InferTypeFromMathMaxExpression(code);

        Assert.That(result, Is.EqualTo("int"));
    }

    [Test]
    public void InferType_WithNonMathMaxExpression_ShouldReturnNull()
    {
        var code = @"
public class TestClass
{
    public void TestMethod()
    {
        var result = SomeMethod();
    }

    private int SomeMethod() => 42;
}";

        var result = InferTypeFromNonMathMaxExpression(code);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void InferType_WithOtherMathMethod_ShouldReturnNull()
    {
        var code = @"
using System;
public class TestClass
{
    public void TestMethod()
    {
        var result = Math.Min(5, 10);
    }
}";

        var result = InferTypeFromNonMathMaxExpression(code);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void InferType_WithCustomMaxMethod_ShouldReturnNull()
    {
        var code = @"
public class TestClass
{
    public void TestMethod()
    {
        var result = CustomMath.Max(5, 10);
    }
}

public static class CustomMath
{
    public static int Max(int a, int b) => a > b ? a : b;
}";

        var result = InferTypeFromNonMathMaxExpression(code);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void InferType_WithNestedMathMaxCall_ShouldReturnInt()
    {
        var code = @"
using System;
public class TestClass
{
    public void TestMethod()
    {
        var result = Math.Max(Math.Max(1, 2), 3);
    }
}";

        var result = InferTypeFromMathMaxExpression(code);

        Assert.That(result, Is.EqualTo("int"));
    }

    private string InferTypeFromMathMaxExpression(string code)
    {
        var (root, semanticModel) = CreateSyntaxTreeAndModel(code);
        var mathMaxInvocation = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(inv => inv.ToString().Contains("Math.Max"));

        var result = strategy.InferType(mathMaxInvocation, semanticModel);
        return result?.ToString() ?? "null";
    }

    private TypeSyntax? InferTypeFromNonMathMaxExpression(string code)
    {
        var (root, semanticModel) = CreateSyntaxTreeAndModel(code);
        var invocation = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(inv => !inv.ToString().Contains("Math.Max"));

        return invocation != null ? strategy.InferType(invocation, semanticModel) : null;
    }

    private (SyntaxNode root, SemanticModel semanticModel) CreateSyntaxTreeAndModel(string code)
    {
        var document = DocumentTestHelper.CreateDocument(code);
        var root = document.GetSyntaxRootAsync().Result!;
        var semanticModel = document.GetSemanticModelAsync().Result!;
        return (root, semanticModel);
    }
}
