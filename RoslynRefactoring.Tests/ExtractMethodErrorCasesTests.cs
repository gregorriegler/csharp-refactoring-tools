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

    [Test]
    public async Task ThrowsWhenSemanticModelIsNull()
    {
        var code = """
            class Test
            {
                void Method()
                {
                    var x = 1;
                }
            }
            """;
        var document = DocumentTestHelper.CreateDocument(code);

        // Create a document without compilation to simulate null semantic model
        var projectWithoutCompilation = document.Project;
        foreach (var reference in document.Project.MetadataReferences)
        {
            projectWithoutCompilation = projectWithoutCompilation.RemoveMetadataReference(reference);
        }
        var documentWithoutSemanticModel = projectWithoutCompilation.GetDocument(document.Id);

        var extractMethod = ExtractMethod.Create(["5:9-5:18", "TestMethod"]);

        try
        {
            await extractMethod.PerformAsync(documentWithoutSemanticModel!);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException ex)
        {
            Assert.That(ex.Message, Does.Contain("SemanticModel is null"));
        }
    }

    [Test]
    public async Task ExtractMethodWithFieldReference()
    {
        var code = """
            class Test
            {
                void Method()
                {
                    var field = this.someField;
                    var x = field + 1;
                }

                private int someField = 42;
            }
            """;
        var document = DocumentTestHelper.CreateDocument(code);
        var extractMethod = ExtractMethod.Create(["6:9-6:26", "TestMethod"]);

        var result = await extractMethod.PerformAsync(document);

        Assert.That(result, Is.Not.EqualTo(document));
    }
}
