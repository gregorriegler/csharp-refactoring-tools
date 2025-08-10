using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class MethodSymbolTypeInferenceStrategyTests
{
    private MethodSymbolTypeInferenceStrategy strategy;

    [SetUp]
    public void SetUp()
    {
        strategy = new MethodSymbolTypeInferenceStrategy();
    }

    [Test]
    public void InferType_WithMethodReturningInt_ShouldReturnInt()
    {
        var code = @"
public class TestClass
{
    public void TestMethod()
    {
        var result = GetIntValue();
    }

    private int GetIntValue() => 42;
}";

        var result = InferTypeFromMethodInvocation(code, "GetIntValue");

        Assert.That(result, Is.EqualTo("int"));
    }

    [Test]
    public void InferType_WithMethodReturningString_ShouldReturnString()
    {
        var code = @"
public class TestClass
{
    public void TestMethod()
    {
        var result = GetStringValue();
    }

    private string GetStringValue() => ""hello"";
}";

        var result = InferTypeFromMethodInvocation(code, "GetStringValue");

        Assert.That(result, Is.EqualTo("string"));
    }

    [Test]
    public void InferType_WithMethodReturningGenericType_ShouldReturnGenericType()
    {
        var code = @"
using System.Collections.Generic;
public class TestClass
{
    public void TestMethod()
    {
        var result = GetListOfInts();
    }

    private List<int> GetListOfInts() => new List<int>();
}";

        var result = InferTypeFromMethodInvocation(code, "GetListOfInts");

        Assert.That(result, Is.EqualTo("System.Collections.Generic.List<int>"));
    }

    [Test]
    public void InferType_WithMethodReturningCustomType_ShouldReturnCustomType()
    {
        var code = @"
public class TestClass
{
    public void TestMethod()
    {
        var result = GetCustomObject();
    }

    private CustomClass GetCustomObject() => new CustomClass();
}

public class CustomClass
{
}";

        var result = InferTypeFromMethodInvocation(code, "GetCustomObject");

        Assert.That(result, Is.EqualTo("CustomClass"));
    }

    [Test]
    public void InferType_WithMethodReturningVoid_ShouldReturnVoid()
    {
        var code = @"
public class TestClass
{
    public void TestMethod()
    {
        DoSomething();
    }

    private void DoSomething() { }
}";

        var result = InferTypeFromMethodInvocation(code, "DoSomething");

        Assert.That(result, Is.EqualTo("void"));
    }

    [Test]
    public void InferType_WithStaticMethodCall_ShouldReturnCorrectType()
    {
        var code = @"
public class TestClass
{
    public void TestMethod()
    {
        var result = StaticHelper.GetValue();
    }
}

public static class StaticHelper
{
    public static double GetValue() => 3.14;
}";

        var result = InferTypeFromMethodInvocation(code, "GetValue");

        Assert.That(result, Is.EqualTo("double"));
    }

    [Test]
    public void InferType_WithNonMethodExpression_ShouldReturnNull()
    {
        var code = @"
public class TestClass
{
    public void TestMethod()
    {
        var result = 42;
    }
}";

        var result = InferTypeFromNonMethodExpression(code);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void InferType_WithMethodReturningNullableType_ShouldReturnNullableType()
    {
        var code = @"
public class TestClass
{
    public void TestMethod()
    {
        var result = GetNullableInt();
    }

    private int? GetNullableInt() => null;
}";

        var result = InferTypeFromMethodInvocation(code, "GetNullableInt");

        Assert.That(result, Is.EqualTo("int?"));
    }

    [Test]
    public void InferType_WithMethodWithParameters_ShouldReturnCorrectType()
    {
        var code = @"
public class TestClass
{
    public void TestMethod()
    {
        var result = CalculateSum(1, 2);
    }

    private int CalculateSum(int a, int b) => a + b;
}";

        var result = InferTypeFromMethodInvocation(code, "CalculateSum");

        Assert.That(result, Is.EqualTo("int"));
    }

    private string InferTypeFromMethodInvocation(string code, string methodName)
    {
        var (root, semanticModel) = CreateSyntaxTreeAndModel(code);
        var methodInvocation = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(inv => inv.ToString().Contains(methodName));

        var result = strategy.InferType(methodInvocation, semanticModel);
        return result?.ToString() ?? "null";
    }

    private TypeSyntax? InferTypeFromNonMethodExpression(string code)
    {
        var (root, semanticModel) = CreateSyntaxTreeAndModel(code);
        var literalExpression = root.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .First();

        return strategy.InferType(literalExpression, semanticModel);
    }

    private (SyntaxNode root, SemanticModel semanticModel) CreateSyntaxTreeAndModel(string code)
    {
        var document = DocumentTestHelper.CreateDocument(code);
        var root = document.GetSyntaxRootAsync().Result!;
        var semanticModel = document.GetSemanticModelAsync().Result!;
        return (root, semanticModel);
    }
}
