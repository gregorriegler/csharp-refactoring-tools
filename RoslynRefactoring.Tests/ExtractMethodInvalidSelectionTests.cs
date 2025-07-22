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
        var code = CreateTestClassWithMethodCall();
        var selection = CodeSelection.Parse("6:8-8:17");

        await VerifyExtract(code, selection, "ExtractedMethod");
    }

    private static async Task VerifyExtract(string code, CodeSelection codeSelection, string newMethodName)
    {
        var result = await ExtractMethodTestHelper.PerformExtractAndFormat(code, codeSelection, newMethodName);
        await Verify(result);
    }

    [Test]
    public async Task ShouldHandleInvalidSelectionFromMiddleOfParameterToNextStatement()
    {
        var code = CreateTestClassWithMethodCall();
        var selection = CodeSelection.Parse("7:12-8:17");

        await VerifyExtract(code, selection, "ExtractedMethod");
    }

    [Test]
    public async Task ShouldHandleMissingVariableDependencies()
    {
        var code = CreateTestClassWithVariableDependency();
        var selection = CodeSelection.Parse("9:8-9:40");

        await VerifyExtract(code, selection, "ExtractedMethod");
    }

    private static string CreateTestClassWithMethodCall()
    {
        return """
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
    }

    private static string CreateTestClassWithVariableDependency()
    {
        return """
            public class TestClass
            {
                public void TestMethod()
                {
                    var localVar = GetValue();

                    if (true)
                    {
                        Console.WriteLine(localVar);
                    }
                }

                private string GetValue()
                {
                    return "test";
                }
            }
            """;
    }

}
