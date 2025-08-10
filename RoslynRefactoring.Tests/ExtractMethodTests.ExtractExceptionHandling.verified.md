## Original

```csharp

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
            LogError($"File not found: {ex.Message}");
            return false;
        }
        return true;
    }

    private string LoadData(string filename) => throw new NotImplementedException();
    private string ProcessData(string data) => throw new NotImplementedException();
    private void SaveData(string data, string filename) => throw new NotImplementedException();
    private void LogError(string message) => throw new NotImplementedException();
}
```

## Selected Span

```csharp
        try
        {
            var data = LoadData(filename);
            var processed = ProcessData(data);
            SaveData(processed, outputFile);
        }
        catch (FileNotFoundException ex)
        {
            LogError($"File not found: {ex.Message}");
            return false;
        }
        return true;

```

---

## Refactored

```csharp
public class FileProcessor
{
    public bool ProcessFile(string filename, string outputFile)
    {
        return ProcessFileWithErrorHandling(filename, outputFile);
    }

    private bool ProcessFileWithErrorHandling(string filename, string outputFile)
    {
        try
        {
            var data = LoadData(filename);
            var processed = ProcessData(data);
            SaveData(processed, outputFile);
        }
        catch (FileNotFoundException ex)
        {
            LogError($"File not found: {ex.Message}");
            return false;
        }

        return true;
    }

    private string LoadData(string filename) => throw new NotImplementedException();
    private string ProcessData(string data) => throw new NotImplementedException();
    private void SaveData(string data, string filename) => throw new NotImplementedException();
    private void LogError(string message) => throw new NotImplementedException();
}
```