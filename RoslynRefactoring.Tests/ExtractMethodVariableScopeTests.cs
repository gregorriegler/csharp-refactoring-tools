using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class ExtractMethodVariableScopeTests
{
    [Test]
    public async Task ExtractMethodWithVariableScopeConflicts()
    {
        const string code = """
            public class TestClass
            {
                public void TestMethod()
                {
                    var name = "outer";
                    {
                        var name = "inner";
                        Console.WriteLine(name);
                    }
                }
            }
            """;

        await VerifyExtract(code, CodeSelection.Parse("7:8-8:36"), "ExtractedMethod");
    }

    private static async Task VerifyExtract(string code, CodeSelection codeSelection, string newMethodName)
    {
        var result = await ExtractMethodTestHelper.PerformExtractAndFormat(code, codeSelection, newMethodName);
        await Verify(result);
    }

}
