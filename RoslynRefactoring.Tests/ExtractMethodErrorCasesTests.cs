using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class ExtractMethodErrorCasesTests
{
    [Test]
    public void ThrowsWhenNoValidExtractionTargetFound()
    {
        var code = "class Test;";
        var document = DocumentTestHelper.CreateDocument(code);
        var extractMethod = ExtractMethod.Create(["1:1-1:1", "TestMethod"]);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await extractMethod.PerformAsync(document));
    }

    [Test]
    public void ThrowsWhenNoContainingBlock()
    {
        var code = "class Test;";
        var document = DocumentTestHelper.CreateDocument(code);
        var extractMethod = ExtractMethod.Create(["1:7-1:11", "TestMethod"]);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await extractMethod.PerformAsync(document));
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
    public void ThrowsWhenSemanticModelIsNull()
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
        var projectWithoutCompilation = document.Project;
        foreach (var reference in document.Project.MetadataReferences)
        {
            projectWithoutCompilation = projectWithoutCompilation.RemoveMetadataReference(reference);
        }
        var documentWithoutSemanticModel = projectWithoutCompilation.GetDocument(document.Id);
        var extractMethod = ExtractMethod.Create(["5:9-5:18", "TestMethod"]);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await extractMethod.PerformAsync(documentWithoutSemanticModel!));
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
