## Original

```csharp

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
}
```

## Selected Span

```csharp
        var total = 0;
        for (int i = 0; i < items.Length; i++)
        {
            total += items[i];
        }
        ret
```

---

## Refactored

```csharp
public class Calculator
{
    public int CalculateSum(int[] items)
    {
        return ComputeTotal(items);
    }

    private int ComputeTotal(int[] items)
    {
        var total = 0;
        for (int i = 0; i < items.Length; i++)
        {
            total += items[i];
        }

        return total;
    }
}
```