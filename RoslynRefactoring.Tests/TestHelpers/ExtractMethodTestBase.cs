using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;

namespace RoslynRefactoring.Tests.TestHelpers;

public static class ExtractMethodTestHelper
{
    public static async Task<string> PerformExtractAndFormat(string code, CodeSelection codeSelection, string newMethodName)
    {
        var document = DocumentTestHelper.CreateDocument(code);

        var extractMethod = new ExtractMethod(codeSelection, newMethodName);
        var updatedDocument = await extractMethod.PerformAsync(document);

        var refactoredFormatted = Formatter.Format((await updatedDocument.GetSyntaxRootAsync())!, new AdhocWorkspace());
        return refactoredFormatted.ToFullString();
    }
}
