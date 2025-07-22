using Microsoft.CodeAnalysis;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class ExtractMethodErrorCasesTests
{
    [Test]
    public async Task ShouldThrowWhenNoValidExtractionTargetFound()
    {
        var documentWithoutBlock = CreateDocumentWithoutBlock();
        var extractMethod = ExtractMethod.Create(["1:1-1:1", "TestMethod"]);

        try
        {
            await extractMethod.PerformAsync(documentWithoutBlock);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException ex)
        {
            Assert.That(ex.Message, Does.Contain("Selected statements are not inside a block"));
        }
    }

    [Test]
    public async Task ShouldThrowWhenNoContainingBlock()
    {
        var documentWithoutBlock = CreateDocumentWithoutBlock();
        var extractMethod = ExtractMethod.Create(["1:7-1:11", "TestMethod"]);

        try
        {
            await extractMethod.PerformAsync(documentWithoutBlock);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException ex)
        {
            Assert.That(ex.Message, Does.Contain("Selected statements are not inside a block"));
        }
    }

    private static Document CreateEmptyDocument()
    {
        var code = "";
        return DocumentTestHelper.CreateDocument(code);
    }

    private static Document CreateDocumentWithoutBlock()
    {
        var code = "class Test;";
        return DocumentTestHelper.CreateDocument(code);
    }
    [Test]
    public async Task ShouldReturnOriginalDocumentWhenNoValidExtractionTargetFound()
    {
        var emptyDocument = CreateEmptyDocument();
        var extractMethod = ExtractMethod.Create(["1:1-1:1", "TestMethod"]);

        var result = await extractMethod.PerformAsync(emptyDocument);

        Assert.That(result, Is.EqualTo(emptyDocument));
    }
}
