using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using RoslynRefactoring.Tests.TestHelpers;

namespace RoslynRefactoring.Tests;

[TestFixture]
public class ExtractMethodAsyncTests
{
    [Test]
    public async Task ShouldExtractAsyncMethodWithHttpClientCall()
    {
        const string code = """
            using System.Net.Http;
            using System.Text.Json;
            using System.Threading.Tasks;

            public class TestClass
            {
                public async Task<T> ProcessDataAsync<T>(string url)
                {
                    var client = new HttpClient();
                    var response = await client.GetAsync(url);
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<T>(content);
                }
            }
            """;

        await VerifyExtract(code, CodeSelection.Parse("9:9-12:57"), "ExtractHttpRequestLogic");
    }

    private static async Task VerifyExtract(string code, CodeSelection codeSelection, string newMethodName)
    {
        var result = await ExtractMethodTestHelper.PerformExtractAndFormat(code, codeSelection, newMethodName);
        await Verify(result);
    }
}
