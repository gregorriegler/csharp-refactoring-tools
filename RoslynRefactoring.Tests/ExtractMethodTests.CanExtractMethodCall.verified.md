## Original

```csharp

using System;

public class Calculator
{
    public void Calculate()
    {
        var a = 2;
        var b = 3;
        var result = Math.Max(a, b);
    }
}
```

## Selected Span

```csharp
Math.Max(a, b)
```

---

## Refactored

```csharp
using System;

public class Calculator
{
    public void Calculate()
    {
        var a = 2;
        var b = 3;
        var result = GetMaxValue(a, b);
    }

    private int GetMaxValue(int a, int b)
    {
        return Math.Max(a, b);
    }
}
```
