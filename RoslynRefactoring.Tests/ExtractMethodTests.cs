using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class ExtractMethodTests
{
    [Test]
    public async Task CanExtractReturn()
    {
        var code = @"
public class Calculator
{
    public int Plus()
    {
        return 1+1;
    }
}";

        await VerifyExtract(code, CodeSelection.Parse("6:0-6:19"), "AddOneWithOne");
    }

    [Test]
    public async Task CanExtractSimpleSwitchWithReturn()
    {
        const string code = @"
public class Bird
{
    private int kind;

    public int GetSpeed()
    {
        switch (kind)
        {
            case 0: return 10;
            default: throw new ArgumentOutOfRangeException();
        }
    }
}";

        await VerifyExtract(code, CodeSelection.Parse("8:0-12:10"), "ComputeSpeed");
    }

    [Test]
    public async Task CanExtractVoid()
    {
        const string code = @"
public class Console
{
    public void Write()
    {
        Console.WriteLine(""Hello World"");
    }
}";

        await VerifyExtract(code, CodeSelection.Parse("6:0-6:44"), "Write");
    }

    [Test]
    public async Task CanExtractOnlyAPartThatReturns()
    {
        const string code = @"
public class Calculator
{
    public void Plus()
    {
        var a = 1+1;
        var b = a+3;
    }
}";

        await VerifyExtract(code, CodeSelection.Parse("6:17-6:21"), "AddOneWithOne");
    }

    [Test]
    public async Task CanExtractSwitchBodyWithReturn()
    {
        const string code = @"
public class Bird
{
    private int kind;

    public int GetSpeed()
    {
        switch (kind)
        {
            case 0: return 10;
            default: throw new ArgumentOutOfRangeException();
        }
    }
}";

        await VerifyExtract(code, CodeSelection.Parse("10:21-10:31"), "Ten");
    }

    [Test]
    public async Task CanExtractSingleVariable()
    {
        const string code = @"
public class Calculator
{
    public void Calculate()
    {
        var x = 5;
        var result = x;
    }
}";

        await VerifyExtract(code, CodeSelection.Parse("7:21-7:22"), "GetX");
    }

    [Test]
    public async Task CanExtractExpressionWithPrecedence()
    {
        const string code = @"
public class Calculator
{
    public void Calculate()
    {
        var a = 2;
        var b = 3;
        var c = 4;
        var result = a + b * c;
    }
}";

        await VerifyExtract(code, CodeSelection.Parse("9:21-9:30"), "CalculateExpression");
    }

    [Test]
    public async Task CanExtractMethodCall()
    {
        const string code = @"
public class Calculator
{
    public void Calculate()
    {
        var a = 2;
        var b = 3;
        var result = Math.Max(a, b);
    }
}";

        await VerifyExtract(code, CodeSelection.Parse("8:21-8:35"), "GetMaxValue");
    }

    [Test]
    public async Task CanExtractNestedExpressions()
    {
        const string code = @"
public class Calculator
{
    public void Calculate()
    {
        var a = 2;
        var b = 3;
        var result = Math.Max(a + 1, b * 2);
    }
}";

        await VerifyExtract(code, CodeSelection.Parse("8:21-8:43"), "GetMaxOfCalculations");
    }

    private static async Task VerifyExtract(string code, CodeSelection codeSelection, string newMethodName)
    {
        var document = DocumentTestHelper.CreateDocument(code);

        var sourceText = await document.GetTextAsync();
        var lines = sourceText.Lines;
        var startPosition = lines[codeSelection.Start.Line - 1].Start + codeSelection.Start.Column;
        var endPosition = lines[codeSelection.End.Line - 1].Start + codeSelection.End.Column;

        var extractMethod = new ExtractMethod(codeSelection, newMethodName);
        var updatedDocument = await extractMethod.PerformAsync(document);

        var selectedSpan = sourceText.GetSubText(new Microsoft.CodeAnalysis.Text.TextSpan(startPosition, endPosition - startPosition));
        var originalFormatted = Formatter.Format((await document.GetSyntaxRootAsync())!, new AdhocWorkspace());
        var refactoredFormatted = Formatter.Format((await updatedDocument.GetSyntaxRootAsync())!, new AdhocWorkspace());
        var output = $@"## Original

```csharp
{originalFormatted.ToFullString()}
```

## Selected Span

```csharp
{selectedSpan}
```

---

## Refactored

```csharp
{refactoredFormatted.ToFullString()}
```";

        await Verify(output, extension:"md");
    }
}
