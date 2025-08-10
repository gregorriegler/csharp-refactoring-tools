## Original

```csharp

public class DataProcessor
{
    public void ProcessNumbers()
    {
        var numbers = new[] { 1, 2, 3 };
        var result = numbers.ToList();
    }
}
```

---

## Refactored

```csharp
public class DataProcessor
{
    public void ProcessNumbers()
    {
        var numbers = new[]
        {
            1,
            2,
            3
        };
        var result = GetNumbersList(numbers);
    }

    private object GetNumbersList(int[] numbers)
    {
        return numbers.ToList();
    }
}
```