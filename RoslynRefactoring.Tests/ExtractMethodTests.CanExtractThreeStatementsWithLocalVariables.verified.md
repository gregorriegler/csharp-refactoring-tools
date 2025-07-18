## Original

```csharp

public class Calculator
{
    public void Calculate()
    {
        var x = 10;
        var y = 20;
        Console.WriteLine(x + y);
    }
}
```

## Selected Span

```csharp
        var x = 10;
        var y = 20;
        Console.WriteLine(x + y);
```

---

## Refactored

```csharp
public class Calculator
{
    public void Calculate()
    {
        PrintSum();
    }

    private void PrintSum()
    {
        var x = 10;
        var y = 20;
        Console.WriteLine(x + y);
    }
}
```