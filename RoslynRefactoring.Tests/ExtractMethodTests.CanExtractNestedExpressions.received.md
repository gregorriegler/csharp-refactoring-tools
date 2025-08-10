## Original

```csharp

public class Calculator
{
    public void Calculate()
    {
        var a = 2;
        var b = 3;
        var result = Math.Max(a + 1, b * 2);
    }
}
```

## Selected Span

```csharp
Math.Max(a + 1, b * 2)
```

---

## Refactored

```csharp
public class Calculator
{
    public void Calculate()
    {
        var a = 2;
        var b = 3;
        var result = GetMaxOfCalculations(a, b);
    }

    private object GetMaxOfCalculations(int a, int b)
    {
        return Math.Max(a + 1, b * 2);
    }
}
```