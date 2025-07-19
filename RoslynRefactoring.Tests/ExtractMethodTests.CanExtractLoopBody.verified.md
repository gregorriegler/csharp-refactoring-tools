## Original

```csharp

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
    public string GetResult() => "result";
}
```

## Selected Span

```csharp
            item.Process();
            item.Validate();
            results.Add(item.GetResult());
```

---

## Refactored

```csharp
public class ItemProcessor
{
    public void ProcessItems(List<Item> items, List<string> results)
    {
        foreach (var item in items)
        {
            ProcessSingleItem(results, item);
        }
    }

    private void ProcessSingleItem(List<string> results, Item item)
    {
        item.Process();
        item.Validate();
        results.Add(item.GetResult());
    }
}

public class Item
{
    public void Process()
    {
    }

    public void Validate()
    {
    }

    public string GetResult() => "result";
}
```