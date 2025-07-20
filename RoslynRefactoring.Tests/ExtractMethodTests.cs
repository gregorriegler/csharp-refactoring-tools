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

    [Test]
    public async Task CanExtractTwoStatements()
    {
        const string code = @"
public class Calculator
{
    public void Calculate()
    {
        var x = 10;
        Console.WriteLine(x);
    }
}";

        await VerifyExtract(code, CodeSelection.Parse("6:0-7:29"), "PrintX");
    }

    [Test]
    public async Task CanExtractThreeStatementsWithLocalVariables()
    {
        const string code = @"
public class Calculator
{
    public void Calculate()
    {
        var x = 10;
        var y = 20;
        Console.WriteLine(x + y);
    }
}";

        await VerifyExtract(code, CodeSelection.Parse("6:0-8:33"), "PrintSum");
    }

    [Test]
    public async Task CanExtractSingleReturnValueCalculation()
    {
        const string code = @"
public class Calculator
{
    public int CalculateSum(int[] items)
    {
        var total = 0;
        for (int i = 0; i < items.Length; i++)
        {
            total += items[i];
        }
        return total;
    }
}";

        await VerifyExtract(code, CodeSelection.Parse("6:0-10:21"), "ComputeTotal");
    }

    [Test]
    public async Task CanExtractMethodCallChain()
    {
        const string code = @"
public class DataProcessor
{
    public void ProcessData()
    {
        var data = GetData();
        var result = data
            .Where(x => x.IsActive)
            .Select(x => x.Name)
            .OrderBy(x => x)
            .ToList();
    }

    private IEnumerable<DataItem> GetData() => throw new NotImplementedException();
}

public class DataItem
{
    public bool IsActive { get; set; }
    public string Name { get; set; }
}";

        await VerifyExtract(code, CodeSelection.Parse("7:21-11:28"), "ProcessActiveNames");
    }

    [Test]
    public async Task CanExtractConditionalLogic()
    {
        const string code = @"
public class UserProcessor
{
    public void ProcessUser(User user)
    {
        if (user.Age >= 18)
        {
            user.CanVote = true;
            user.Status = ""Adult"";
        }
        else
        {
            user.CanVote = false;
            user.Status = ""Minor"";
        }
    }
}

public class User
{
    public int Age { get; set; }
    public bool CanVote { get; set; }
    public string Status { get; set; }
}";

        await VerifyExtract(code, CodeSelection.Parse("6:0-14:10"), "SetVotingStatus");
    }

    [Test]
    public async Task CanExtractLoopBody()
    {
        const string code = @"
public class ItemProcessor
{
    public void ProcessItems(List<Item> items, List<string> results)
    {
        foreach (var item in items)
        {
            item.Process();
            item.Validate();
            results.Add(item.GetResult());
        }
    }
}

public class Item
{
    public void Process() { }
    public void Validate() { }
    public string GetResult() => ""result"";
}";

        await VerifyExtract(code, CodeSelection.Parse("8:0-10:42"), "ProcessSingleItem");
    }

    [Test]
    public async Task CanExtractExceptionHandling()
    {
        const string code = @"
public class FileProcessor
{
    public bool ProcessFile(string filename, string outputFile)
    {
        try
        {
            var data = LoadData(filename);
            var processed = ProcessData(data);
            SaveData(processed, outputFile);
        }
        catch (FileNotFoundException ex)
        {
            LogError($""File not found: {ex.Message}"");
            return false;
        }
        return true;
    }

    private string LoadData(string filename) => throw new NotImplementedException();
    private string ProcessData(string data) => throw new NotImplementedException();
    private void SaveData(string data, string filename) => throw new NotImplementedException();
    private void LogError(string message) => throw new NotImplementedException();
}";

        await VerifyExtract(code, CodeSelection.Parse("6:0-17:21"), "ProcessFileWithErrorHandling");
    }

    [Test]
    public async Task CanExtractAsyncMethod()
    {
        const string code = @"
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class ApiClient
{
    public async Task<T> GetDataAsync<T>(string url)
    {
        var client = new HttpClient();
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content);
    }
}";

        await VerifyExtract(code, CodeSelection.Parse("10:0-12:56"), "FetchAndDeserialize");
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
