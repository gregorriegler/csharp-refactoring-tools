using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class TypeInferrerTests
{
    private TypeInferrer typeInferrer;

    [SetUp]
    public void SetUp()
    {
        typeInferrer = new TypeInferrer();
    }

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

        var result = InferTypeFromAwaitExpression(code);

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

        var result = InferTypeFromAwaitExpression(code);

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

        var result = ResolveTypeForForeachVariable(code);

        Assert.That(result, Is.EqualTo("var"));
    }

    private string InferTypeFromAwaitExpression(string code)
    {
        var (root, semanticModel) = CreateSyntaxTreeAndModel(code);
        var awaitExpression = root.DescendantNodes().OfType<AwaitExpressionSyntax>().First();

        return typeInferrer.InferType(awaitExpression, semanticModel) ?? "null";
    }

    private string ResolveTypeForForeachVariable(string code)
    {
        var (root, semanticModel) = CreateSyntaxTreeAndModel(code);
        var foreachStatement = root.DescendantNodes().OfType<ForEachStatementSyntax>().First();
        var containingBlock = foreachStatement.Statement as BlockSyntax;
        var localSymbol = semanticModel.GetDeclaredSymbol(foreachStatement) as ILocalSymbol;

        return typeInferrer.ResolveActualTypeForForeachVariable(localSymbol!, containingBlock!, semanticModel);
    }

    private (SyntaxNode root, SemanticModel semanticModel) CreateSyntaxTreeAndModel(string code)
    {
        var document = DocumentTestHelper.CreateDocument(code);
        var root = document.GetSyntaxRootAsync().Result!;
        var semanticModel = document.GetSemanticModelAsync().Result!;
        return (root, semanticModel);
    }
}
