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

    [Test]
    public void ResolveActualTypeForForeachVariable_WithNoMethodBlock_ShouldReturnVar()
    {
        var code = @"
public class TestClass
{
    public void TestMethod()
    {
        var items = new[] { 1, 2, 3 };
        foreach (var item in items)
        {
            // some code
        }
    }
}";

        var document = DocumentTestHelper.CreateDocument(code);
        var root = document.GetSyntaxRootAsync().Result!;
        var semanticModel = document.GetSemanticModelAsync().Result!;

        var foreachStatement = root.DescendantNodes()
            .OfType<ForEachStatementSyntax>()
            .First();

        var containingBlock = foreachStatement.Statement as BlockSyntax;
        var localSymbol = semanticModel.GetDeclaredSymbol(foreachStatement) as ILocalSymbol;

        var typeInferrer = new TypeInferrer();
        var result = typeInferrer.ResolveActualTypeForForeachVariable(localSymbol!, containingBlock!, semanticModel);

        Assert.That(result, Is.EqualTo("var"));
    }
}
