using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class ToListTypeInferenceStrategyTests
{
    private ToListTypeInferenceStrategy strategy;

    [SetUp]
    public void SetUp()
    {
        strategy = new ToListTypeInferenceStrategy();
    }

    [Test]
    public void InferType_WithIntArrayToList_ShouldReturnListOfInt()
    {
        var code = @"
using System.Linq;
public class TestClass
{
    public void TestMethod()
    {
        var array = new int[] { 1, 2, 3 };
        var result = array.ToList();
    }
}";

        var result = InferTypeFromToListExpression(code);

        Assert.That(result, Is.EqualTo("List<int>"));
    }

    [Test]
    public void InferType_WithStringArrayToList_ShouldReturnListOfString()
    {
        var code = @"
using System.Linq;
public class TestClass
{
    public void TestMethod()
    {
        var array = new string[] { ""a"", ""b"", ""c"" };
        var result = array.ToList();
    }
}";

        var result = InferTypeFromToListExpression(code);

        Assert.That(result, Is.EqualTo("List<string>"));
    }

    [Test]
    public void InferType_WithGenericCollectionToList_ShouldReturnListOfElementType()
    {
        var code = @"
using System.Collections.Generic;
using System.Linq;
public class TestClass
{
    public void TestMethod()
    {
        var collection = new List<double> { 1.0, 2.0, 3.0 };
        var result = collection.ToList();
    }
}";

        var result = InferTypeFromToListExpression(code);

        Assert.That(result, Is.EqualTo("List<double>"));
    }

    [Test]
    public void InferType_WithIEnumerableToList_ShouldReturnListOfElementType()
    {
        var code = @"
using System.Collections.Generic;
using System.Linq;
public class TestClass
{
    public void TestMethod()
    {
        IEnumerable<bool> enumerable = new List<bool> { true, false };
        var result = enumerable.ToList();
    }
}";

        var result = InferTypeFromToListExpression(code);

        Assert.That(result, Is.EqualTo("List<bool>"));
    }

    [Test]
    public void InferType_WithFallbackCase_ShouldReturnListOfString()
    {
        var code = @"
using System.Linq;
public class TestClass
{
    public void TestMethod()
    {
        var unknownCollection = GetUnknownCollection();
        var result = unknownCollection.ToList();
    }

    private object GetUnknownCollection() => null;
}";

        var result = InferTypeFromToListExpression(code);

        Assert.That(result, Is.EqualTo("List<string>"));
    }

    [Test]
    public void InferType_WithNonToListExpression_ShouldReturnNull()
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

        var result = InferTypeFromNonToListExpression(code);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void InferType_WithToListOnComplexExpression_ShouldReturnCorrectType()
    {
        var code = @"
using System.Linq;
public class TestClass
{
    public void TestMethod()
    {
        var numbers = new int[] { 1, 2, 3, 4, 5 };
        var result = numbers.Where(x => x > 2).ToList();
    }
}";

        var result = InferTypeFromToListExpression(code);

        Assert.That(result, Is.EqualTo("List<string>"));
    }

    private string InferTypeFromToListExpression(string code)
    {
        var (root, semanticModel) = CreateSyntaxTreeAndModel(code);
        var toListInvocation = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(inv => inv.ToString().Contains("ToList"));

        var result = strategy.InferType(toListInvocation, semanticModel);
        return result?.ToString() ?? "null";
    }

    private TypeSyntax? InferTypeFromNonToListExpression(string code)
    {
        var (root, semanticModel) = CreateSyntaxTreeAndModel(code);
        var invocation = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(inv => !inv.ToString().Contains("ToList"));

        return strategy.InferType(invocation, semanticModel);
    }

    private (SyntaxNode root, SemanticModel semanticModel) CreateSyntaxTreeAndModel(string code)
    {
        var document = DocumentTestHelper.CreateDocument(code);
        var root = document.GetSyntaxRootAsync().Result!;
        var semanticModel = document.GetSemanticModelAsync().Result!;
        return (root, semanticModel);
    }
}
