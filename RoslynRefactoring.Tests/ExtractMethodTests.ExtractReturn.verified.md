## Original

```csharp

public class Calculator
{
    public int Plus()
    {
        return 1 + 1;
    }
}
```

## Selected Span

```csharp
        return 1+1;
```

---

## Refactored

```csharp
public class Calculator
{
    public int Plus()
    {
        return AddOneWithOne();
    }

    private int AddOneWithOne()
    {
        return 1 + 1;
    }
}
```