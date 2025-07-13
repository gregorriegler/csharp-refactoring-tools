## Original

```csharp

public class Calculator
{
    public void Plus()
    {
        var a = 1 + 1;
        var b = a + 3;
    }
}
```

---

## Refactored

```csharp
public class Calculator
{
    public void Plus()
    {
        int a = AddOneWithOne();
        var b = a + 3;
    }

    private int AddOneWithOne()
    {
        var a = 1 + 1;
        return a;
    }
}
```