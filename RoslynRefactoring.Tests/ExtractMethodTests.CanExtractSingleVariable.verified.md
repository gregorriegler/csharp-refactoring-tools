﻿## Original

```csharp

public class Calculator
{
    public void Calculate()
    {
        var x = 5;
        var result = x;
    }
}
```

## Selected Span

```csharp
x
```

---

## Refactored

```csharp
public class Calculator
{
    public void Calculate()
    {
        var x = 5;
        var result = GetX(x);
    }

    private int GetX(int x)
    {
        return x;
    }
}
```