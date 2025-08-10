using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class ExpressionExtractionTargetToListTests
{
    [Test]
    public async Task ShouldInferCorrectGenericTypeForToListOnIntegerArray()
    {
        const string code = @"
public class DataProcessor
{
    public void ProcessNumbers()
    {
        var numbers = new[] { 1, 2, 3 };
        var result = numbers.ToList();
    }
}";

        await VerifyExtract(code, CodeSelection.Parse("7:21-7:37"), "GetNumbersList");
    }

    private static async Task VerifyExtract(string code, CodeSelection codeSelection, string newMethodName)
    {
        var document = DocumentTestHelper.CreateDocument(code);

        var extractMethod = new ExtractMethod(codeSelection, newMethodName);
        var updatedDocument = await extractMethod.PerformAsync(document);

        var originalFormatted = Formatter.Format((await document.GetSyntaxRootAsync())!, new AdhocWorkspace());
        var refactoredFormatted = Formatter.Format((await updatedDocument.GetSyntaxRootAsync())!, new AdhocWorkspace());
        var output = $@"## Original

```csharp
{originalFormatted.ToFullString()}
```

---

## Refactored

```csharp
{refactoredFormatted.ToFullString()}
```";

        await Verify(output, extension:"md");
    }
}
