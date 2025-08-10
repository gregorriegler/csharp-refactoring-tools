using Microsoft.CodeAnalysis;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class ExtractMethodErrorCasesTests
{
    [Test]
    public async Task ThrowsWhenNoValidExtractionTargetFound()
    {
        var code = "class Test;";
        var document = DocumentTestHelper.CreateDocument(code);
        var extractMethod = ExtractMethod.Create(["1:1-1:1", "TestMethod"]);

        try
        {
            await extractMethod.PerformAsync(document);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException ex)
        {
            Assert.That(ex.Message, Does.Contain("Selected statements are not inside a block"));
        }
    }

    [Test]
    public async Task ThrowsWhenNoContainingBlock()
    {
        var code = "class Test;";
        var document = DocumentTestHelper.CreateDocument(code);
        var extractMethod = ExtractMethod.Create(["1:7-1:11", "TestMethod"]);

        try
        {
            await extractMethod.PerformAsync(document);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException ex)
        {
            Assert.That(ex.Message, Does.Contain("Selected statements are not inside a block"));
        }
    }

    [Test]
    public async Task ReturnsOriginalDocumentWhenNoValidExtractionTargetFound()
    {
        var code = "";
        var document = DocumentTestHelper.CreateDocument(code);
        var extractMethod = ExtractMethod.Create(["1:1-1:1", "TestMethod"]);

        var result = await extractMethod.PerformAsync(document);

        Assert.That(result, Is.EqualTo(document));
    }
}
