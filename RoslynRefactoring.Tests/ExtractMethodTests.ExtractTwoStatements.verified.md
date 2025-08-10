## Original

```csharp

public class Calculator
{
    public void Calculate()
    {
        var x = 10;
        Console.WriteLine(x);
    }
}
```

## Selected Span

```csharp
        var x = 10;
        Console.WriteLine(x);
```

---

## Refactored

```csharp
public class Calculator
{
    public void Calculate()
    {
        PrintX();
    }

    private void PrintX()
    {
        var x = 10;
        Console.WriteLine(x);
    }
}
```