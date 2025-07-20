## Original

```csharp

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
}
```

## Selected Span

```csharp
data
            .Where(x => x.IsActive)
            .Select(x => x.Name)
            .OrderBy(x => x)
            .ToList();
    }
```

---

## Refactored

```csharp
public class DataProcessor
{
    public void ProcessData()
    {
        var data = GetData();
        var result = ProcessActiveNames(data);
    }

    private void ProcessActiveNames(IEnumerable<DataItem> data)
    {
        var result = data.Where(x => x.IsActive).Select(x => x.Name).OrderBy(x => x).ToList();
        return result;
    }

    private IEnumerable<DataItem> GetData() => throw new NotImplementedException();
}

public class DataItem
{
    public bool IsActive { get; set; }
    public string Name { get; set; }
}
```