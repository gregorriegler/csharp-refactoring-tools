using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class TypeInferrerTests
{
    [Test]
    public void InferType_WithAwaitErrorType_ShouldReturnObjectInsteadOfString()
    {
        var code = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task TestMethod()
    {
        var result = await NonExistentAsyncMethod();
    }
}";

        var document = DocumentTestHelper.CreateDocument(code);
        var root = document.GetSyntaxRootAsync().Result!;
        var semanticModel = document.GetSemanticModelAsync().Result!;

        var awaitExpression = root.DescendantNodes()
            .OfType<AwaitExpressionSyntax>()
            .First();

        var typeInferrer = new TypeInferrer();
        var result = typeInferrer.InferType(awaitExpression, semanticModel);

        Assert.That(result, Is.EqualTo("object"));
    }

    [Test]
    public void InferType_WithAwaitValidType_ShouldReturnTypeDisplayString()
    {
        var code = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task TestMethod()
    {
        var result = await Task.FromResult(42);
    }
}";

        var document = DocumentTestHelper.CreateDocument(code);
        var root = document.GetSyntaxRootAsync().Result!;
        var semanticModel = document.GetSemanticModelAsync().Result!;

        var awaitExpression = root.DescendantNodes()
            .OfType<AwaitExpressionSyntax>()
            .First();

        var typeInferrer = new TypeInferrer();
        var result = typeInferrer.InferType(awaitExpression, semanticModel);

        Assert.That(result, Is.EqualTo("int"));
    }
}
