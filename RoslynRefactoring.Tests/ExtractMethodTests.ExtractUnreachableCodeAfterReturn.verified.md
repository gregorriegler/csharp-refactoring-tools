## Original

```csharp

public class TestClass
{
    public void TestMethod(bool condition, string result)
    {
        if (condition)
            return;
        Console.WriteLine("This might be unreachable");
    }
}
```

## Selected Span

```csharp
        if (condition)
            return;
        Console.WriteLine("This might be unreachable")
```

---

## Refactored

```csharp
public class TestClass
{
    public void TestMethod(bool condition, string result)
    {
        return HandleConditionalReturn(condition);
    }

    private void HandleConditionalReturn(bool condition)
    {
        if (condition)
            return;
        Console.WriteLine("This might be unreachable");
    }
}
```