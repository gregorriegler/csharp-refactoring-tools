using Microsoft.CodeAnalysis;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class ExtractMethodErrorCasesTests
{
    [Test]
    public async Task ShouldThrowWhenNoValidExtractionTargetFound()
    {
        var emptyDocument = CreateEmptyDocument();
        var extractMethod = ExtractMethod.Create(["1:1-1:1", "TestMethod"]);

        try
        {
            await extractMethod.PerformAsync(emptyDocument);
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
}
