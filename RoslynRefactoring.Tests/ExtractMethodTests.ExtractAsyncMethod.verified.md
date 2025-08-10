## Original

```csharp

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
}
```

## Selected Span

```csharp
        var client = new HttpClient();
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStrin
```

---

## Refactored

```csharp
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class ApiClient
{
    public async Task<T> GetDataAsync<T>(string url)
    {
        var content = await FetchAndDeserialize(url);
        return JsonSerializer.Deserialize<T>(content);
    }

    private async Task<string> FetchAndDeserialize(string url)
    {
        var client = new HttpClient();
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        return content;
    }
}
```