## Original

```csharp

public class Calculator
{
    public void Calculate()
    {
        var a = 2;
        var b = 3;
        var c = 4;
        var result = a + b * c;
    }
}
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
        var c = 4;
        var result = CalculateExpression(a, b, c);
    }

    private int CalculateExpression(int a, int b, int c)
    {
        return a + b * c;
    }
}
```
