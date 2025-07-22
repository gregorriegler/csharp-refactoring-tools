using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class ExtractMethodInvalidSelectionTests
{
    [Test]
    public async Task ShouldHandleInvalidSelectionCrossingStatementBoundaries()
    {
        const string code = """
            public class TestClass
            {
                public void TestMethod()
                {
                    var x = SomeMethod(
                        parameter1,
                        parameter2);
                    var y = x + 1;
                }

                private int SomeMethod(string param1, string param2)
                {
                    return 42;
                }
            }
            """;

        await VerifyExtract(code, CodeSelection.Parse("6:8-8:17"), "ExtractedMethod");
    }

    private static async Task VerifyExtract(string code, CodeSelection codeSelection, string newMethodName)
    {
        var document = DocumentTestHelper.CreateDocument(code);

        var extractMethod = new ExtractMethod(codeSelection, newMethodName);
        var updatedDocument = await extractMethod.PerformAsync(document);

        var refactoredFormatted = Formatter.Format((await updatedDocument.GetSyntaxRootAsync())!, new AdhocWorkspace());
        await Verify(refactoredFormatted.ToFullString());
    }
}
