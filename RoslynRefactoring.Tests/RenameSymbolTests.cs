using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class RenameSymbolTests
{
    [Test]
    public async Task RenameUnusedLocalVariable()
    {
        var code = @"
public class Test
{
    public void Method()
    {
        int unused = 5;
        Console.WriteLine(""Hello"");
    }
}";

        await VerifyRename(code, Cursor.Parse("6:13"), "temp");
    }

    [Test]
    public async Task RenameVariableWithOneUsage()
    {
        var code = @"
public class Test
{
    public void Method()
    {
        int count = 0;
        return count;
    }
}";

        await VerifyRename(code, Cursor.Parse("6:13"), "total");
    }

    [Test]
    public async Task RenameVariableWithManyUsages()
    {
        var code = @"
public class Test
{
    public void Method()
    {
        int value = 10;
        int result = value + value * 2;
        Console.WriteLine(value);
        return result + value;
    }
}";

        await VerifyRename(code, Cursor.Parse("6:13"), "number");
    }

    [Test]
    public async Task RenameVariableInDifferentScopes()
    {
        var code = @"
public class Test
{
    public void Method()
    {
        int value = 10;
        {
            int value = 20; // inner scope variable
            Console.WriteLine(value);
        }
        Console.WriteLine(value); // outer scope variable
    }
}";

        await VerifyRename(code, Cursor.Parse("8:17"), "innerValue");
    }

    [Test]
    public async Task RenameVariableInLoop()
    {
        var code = @"
public class Test
{
    public void Method()
    {
        for(int i = 0; i < 10; i++)
        {
            Console.WriteLine(i);
        }
    }
}";

        await VerifyRename(code, Cursor.Parse("6:17"), "index");
    }

    [Test]
    public async Task RenameUnusedPrivateMethod()
    {
        var code = @"
public class Test
{
    private void DoSomething()
    {
        Console.WriteLine(""Hello"");
    }

    public void Main()
    {
        Console.WriteLine(""Main method"");
    }
}";

        await VerifyRename(code, Cursor.Parse("4:18"), "ProcessData");
    }

    [Test]
    public async Task RenamePrivateMethodWithOneUsage()
    {
        var code = @"
public class Test
{
    private void DoSomething()
    {
        Console.WriteLine(""Hello"");
    }

    public void Main()
    {
        DoSomething();
    }
}";

        await VerifyRename(code, Cursor.Parse("4:18"), "ProcessData");
    }

    [Test]
    public async Task RenamePrivateMethodWithManyUsages()
    {
        var code = @"
public class Test
{
    private void DoSomething()
    {
        Console.WriteLine(""Hello"");
    }

    public void Main()
    {
        DoSomething();
        DoSomething();
        DoSomething();
        DoSomething();
        DoSomething();
    }
}";

        await VerifyRename(code, Cursor.Parse("4:18"), "ProcessData");
    }

    [Test]
    public async Task RenameMethodWithParameters()
    {
        var code = @"
public class Test
{
    private int Calculate(int x, int y)
    {
        return x + y;
    }

    public void Main()
    {
        int result = Calculate(5, 3);
        Console.WriteLine(Calculate(10, 20));
    }
}";

        await VerifyRename(code, Cursor.Parse("4:17"), "Sum");
    }

    private static async Task VerifyRename(string code, Cursor cursor, string newName)
    {
        var document = DocumentTestHelper.CreateDocument(code);
        var renameSymbol = new RenameSymbol(cursor, newName);
        var updatedDocument = await renameSymbol.PerformAsync(document);
        var formatted = Formatter.Format((await updatedDocument.GetSyntaxRootAsync())!, new AdhocWorkspace());
        await Verify(formatted.ToFullString());
    }
}
